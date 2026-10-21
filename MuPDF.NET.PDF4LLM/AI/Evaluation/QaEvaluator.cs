using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using MuPDF.NET.PDF4LLM.AI.Models;
using MuPDF.NET.PDF4LLM.AI.Services;

namespace MuPDF.NET.PDF4LLM.AI.Evaluation;

/// <summary>
/// Runs a dataset against an indexed collection. Retrieval and claim checks do
/// not require a live model: the chat client answers from the excerpts in the prompt.
/// </summary>
public static class QaEvaluator
{
    static readonly Regex ExcerptHeader = new(
        @"BEGIN EXCERPT id=""(?<id>[^""]+)"" file=""(?<file>[^""]+)"" page=(?<page>\d+)",
        RegexOptions.Compiled);

    public static async Task<QaReport> RunAsync(
        AiDocumentCollection docs,
        IReadOnlyList<QaCase> cases,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<QaCaseResult>(cases.Count);
        foreach (QaCase item in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(await RunOneAsync(docs, item, cancellationToken).ConfigureAwait(false));
        }

        return new QaReport { Cases = rows };
    }

    static async Task<QaCaseResult> RunOneAsync(
        AiDocumentCollection docs,
        QaCase item,
        CancellationToken cancellationToken)
    {
        var searchWatch = Stopwatch.StartNew();
        IReadOnlyList<SearchResult> hits = await docs
            .SearchAsync(item.Question, item.SearchTopK, item.SourceFileName, cancellationToken)
            .ConfigureAwait(false);
        searchWatch.Stop();

        bool recall = item.ExpectedPassageContains.Count == 0
            || item.ExpectedPassageContains.All(needle =>
                hits.Any(h => h.Chunk.Text.Contains(needle, StringComparison.OrdinalIgnoreCase)));

        var askWatch = Stopwatch.StartNew();
        GroundedAnswer answer = await docs
            .AskDetailedAsync(item.Question, item.AskTopK, item.SourceFileName, cancellationToken)
            .ConfigureAwait(false);
        askWatch.Stop();

        bool abstention = item.ExpectInsufficient
            ? answer.InsufficientEvidence
            : !answer.InsufficientEvidence;

        bool claims = item.ExpectInsufficient
            || item.ExpectedClaims.All(c => answer.Text.Contains(c, StringComparison.OrdinalIgnoreCase));

        bool clean = item.ForbiddenClaims.All(c =>
            answer.Text.IndexOf(c, StringComparison.OrdinalIgnoreCase) < 0);

        var missing = new List<string>();
        if (!recall)
            missing.Add("retrieval");
        if (!abstention)
            missing.Add("abstention");
        if (!claims)
            missing.Add("claims");
        if (!clean)
            missing.Add("forbidden");

        return new QaCaseResult
        {
            CaseId = item.Id,
            RetrievalRecall = recall,
            AbstentionCorrect = abstention,
            ClaimsCorrect = claims,
            NoForbiddenClaims = clean,
            CitationsValid = answer.CitationsValid || item.ExpectInsufficient,
            SearchMs = searchWatch.Elapsed.TotalMilliseconds,
            AskMs = askWatch.Elapsed.TotalMilliseconds,
            Detail = missing.Count == 0 ? null : string.Join(",", missing),
        };
    }

    /// <summary>
    /// Chat stand-in for unit evaluation: answers only from excerpt text in the prompt.
    /// </summary>
    public sealed class EvidenceOracleChat : IChatClient
    {
        readonly IReadOnlyList<QaCase> _cases;

        public EvidenceOracleChat(IReadOnlyList<QaCase> cases) => _cases = cases;

        public ChatClientMetadata Metadata { get; } = new("eval-oracle");

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            string user = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";
            QaCase? item = _cases.FirstOrDefault(c =>
                user.IndexOf(c.Question, StringComparison.Ordinal) >= 0);
            string json = Reply(user, item);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }

        static string Reply(string user, QaCase? item)
        {
            string body = ExcerptBody(user);
            if (item == null)
                return "{\"answer\":\"\",\"insufficientEvidence\":true,\"citations\":[]}";

            bool hasClaims = item.ExpectedClaims.Count > 0
                && item.ExpectedClaims.All(c => body.Contains(c, StringComparison.OrdinalIgnoreCase));
            bool contradiction = item.ExpectInsufficient
                && item.ExpectedClaims.Count == 0
                && item.ExpectedPassageContains.Count >= 2
                && item.ExpectedPassageContains.All(c => body.Contains(c, StringComparison.OrdinalIgnoreCase));

            if (item.ExpectInsufficient && (!hasClaims || contradiction || item.ExpectedClaims.Count == 0))
                return "{\"answer\":\"\",\"insufficientEvidence\":true,\"citations\":[]}";

            if (!hasClaims)
                return "{\"answer\":\"\",\"insufficientEvidence\":true,\"citations\":[]}";

            string quote = item.ExpectedClaims[0];
            string sourceId = "";
            int page = 1;
            foreach (Match header in ExcerptHeader.Matches(user))
            {
                int line = user.IndexOf('\n', header.Index);
                int end = line < 0 ? -1 : user.IndexOf("END EXCERPT", line, StringComparison.Ordinal);
                string span = line >= 0 && end > line ? user.Substring(line + 1, end - line - 1) : "";
                if (!span.Contains(quote, StringComparison.OrdinalIgnoreCase))
                    continue;
                sourceId = header.Groups["id"].Value;
                if (int.TryParse(header.Groups["page"].Value, out int p))
                    page = p;
                break;
            }

            string answer = string.Join("; ", item.ExpectedClaims);
            return
                "{\"answer\":\"" + JsonEscape(answer) + "\",\"insufficientEvidence\":false,\"citations\":[{" +
                "\"sourceId\":\"" + JsonEscape(sourceId) + "\",\"page\":" + page +
                ",\"quote\":\"" + JsonEscape(quote) + "\"}]}";
        }

        static string ExcerptBody(string user)
        {
            var parts = new List<string>();
            int i = 0;
            while (true)
            {
                int begin = user.IndexOf("BEGIN EXCERPT", i, StringComparison.Ordinal);
                if (begin < 0)
                    break;
                int line = user.IndexOf('\n', begin);
                int end = user.IndexOf("END EXCERPT", line < 0 ? begin : line, StringComparison.Ordinal);
                if (line >= 0 && end > line)
                    parts.Add(user.Substring(line + 1, end - line - 1));
                i = begin + 12;
            }

            return string.Join("\n", parts);
        }

        static string JsonEscape(string value) =>
            value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
