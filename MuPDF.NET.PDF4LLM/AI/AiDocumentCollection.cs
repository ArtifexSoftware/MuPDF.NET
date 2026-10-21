using System.Text;
using Microsoft.Extensions.AI;
using MuPDF.NET.PDF4LLM.AI.Abstractions;
using MuPDF.NET.PDF4LLM.AI.Models;
using MuPDF.NET.PDF4LLM.AI.Options;
using MuPDF.NET.PDF4LLM.AI.Services;

namespace MuPDF.NET.PDF4LLM.AI;

/// <summary>
/// PDFs loaded by <see cref="MsAIConnector.LoadAsync"/>.
/// Ask sends a small number of ranked excerpts (default two) with a grounded prompt.
/// Search ranks chunks by query words, then vector similarity.
/// </summary>
public sealed class AiDocumentCollection
{
    const int SummarizeBatchCharacters = 8_000;
    const int DefaultAskContextCharacters = 12_000;
    const int DefaultMaxOutputTokens = 1024;

    readonly IVectorIndex _vectorIndex;
    readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    readonly IChatClient? _chatClient;
    readonly MicrosoftAIConnectorOptions? _options;
    readonly Action<string>? _trace;
    readonly object _gate = new();
    readonly List<AiChunk> _allChunks;
    readonly int _defaultTopK;
    readonly int _askExcerptCount;
    readonly int _askContextCharacters;
    readonly bool _embedAskQuery;
    readonly int _maxOutputTokens;
    readonly double _askMinScore;
    readonly double _searchMinScore;
    readonly bool _askAllContext;

    internal AiDocumentCollection(
        IVectorIndex vectorIndex,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IChatClient? chatClient,
        IReadOnlyList<AiChunk> allChunks,
        int defaultTopK,
        int askExcerptCount = 2,
        int askContextCharacters = DefaultAskContextCharacters,
        bool embedAskQuery = true,
        int maxOutputTokens = DefaultMaxOutputTokens,
        double askMinScore = 0.35,
        double searchMinScore = 0,
        MicrosoftAIConnectorOptions? options = null)
    {
        _vectorIndex = vectorIndex;
        _embeddingGenerator = embeddingGenerator;
        _chatClient = chatClient;
        _options = options;
        _trace = options?.Trace;
        _allChunks = allChunks.ToList();
        _defaultTopK = defaultTopK;
        _askExcerptCount = Math.Max(1, askExcerptCount);
        _askContextCharacters = askContextCharacters <= 0 ? int.MaxValue : askContextCharacters;
        _embedAskQuery = embedAskQuery;
        _maxOutputTokens = maxOutputTokens;
        _askMinScore = Math.Max(0, askMinScore);
        _searchMinScore = Math.Max(0, searchMinScore);
        _askAllContext = options?.AskAllContext ?? false;
    }

    /// <summary>Number of indexed chunks across all loaded PDFs.</summary>
    public int ChunkCount
    {
        get
        {
            lock (_gate)
                return _allChunks.Count;
        }
    }

    /// <summary>Source file names that were indexed.</summary>
    public IReadOnlyList<string> SourceFiles
    {
        get
        {
            lock (_gate)
            {
                return _allChunks.Select(c => c.SourceFileName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }
    }

    /// <summary>Extract, embed, and upsert PDFs, replacing any previous chunks from the same files.</summary>
    public async Task AddDocumentsAsync(
        IEnumerable<string> pdfPaths,
        CancellationToken cancellationToken = default)
    {
        if (_options == null)
            throw new InvalidOperationException("AddDocumentsAsync requires a collection created by LoadAiAsync.");

        List<AiChunk> added = await MsAIConnector.PrepareChunksAsync(pdfPaths, _options, cancellationToken)
            .ConfigureAwait(false);
        await _vectorIndex.IndexAsync(added, cancellationToken).ConfigureAwait(false);
        var names = new HashSet<string>(added.Select(c => c.SourceFileName), StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            _allChunks.RemoveAll(c => names.Contains(c.SourceFileName));
            _allChunks.AddRange(added);
        }

        _trace?.Invoke($"Added {added.Count} chunks from {names.Count} file(s).");
    }

    /// <summary>Remove one source PDF from the collection and the vector index.</summary>
    public async Task RemoveDocumentAsync(string filePathOrName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePathOrName))
            throw new ArgumentException("File name is required.", nameof(filePathOrName));

        string name = Path.GetFileName(filePathOrName);
        await _vectorIndex.RemoveBySourceAsync(name, cancellationToken).ConfigureAwait(false);
        lock (_gate)
        {
            _allChunks.RemoveAll(c =>
                string.Equals(c.SourceFileName, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.SourceFilePath, filePathOrName, StringComparison.OrdinalIgnoreCase));
        }

        _trace?.Invoke($"Removed '{name}'.");
    }

    /// <summary>Answer a question from retrieved excerpts. Returns the answer text.</summary>
    public async Task<string> AskAsync(
        string question,
        int? topK = null,
        string? sourceFileName = null,
        CancellationToken cancellationToken = default,
        bool allContext = false)
    {
        GroundedAnswer answer = await AskDetailedAsync(
                question, topK, sourceFileName, cancellationToken, allContext)
            .ConfigureAwait(false);
        return answer.Text;
    }

    /// <summary>
    /// Answer a question using only the top ranked excerpts that clear
    /// <c>AskMinScore</c>. Does not call the chat model when none qualify.
    /// Pass <paramref name="allContext"/> to send every indexed chunk instead.
    /// </summary>
    public async Task<GroundedAnswer> AskDetailedAsync(
        string question,
        int? topK = null,
        string? sourceFileName = null,
        CancellationToken cancellationToken = default,
        bool allContext = false)
    {
        if (_chatClient == null)
            throw new InvalidOperationException(
                "AskAsync requires a chat client. Set MicrosoftAIConnectorOptions.ChatClient.");

        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));

        IReadOnlyList<RetrievedPassage> excerpts = allContext || _askAllContext
            ? TakeAllContext(sourceFileName)
            : TakeExcerpts(
                await RankOrLexicalAsync(question, cancellationToken, _embedAskQuery, sourceFileName)
                    .ConfigureAwait(false),
                topK ?? _askExcerptCount,
                _askContextCharacters,
                _askMinScore);
        if (excerpts.Count == 0)
            return GroundedAnswer.Insufficient();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, GroundedPrompt.System),
            new(ChatRole.User, GroundedPrompt.UserMessage(excerpts, question)),
        };

        string raw = await CompleteAsync(messages, cancellationToken).ConfigureAwait(false);
        return Interpret(raw, excerpts);
    }

    /// <summary>Summarize one loaded document. Long documents are summarized in batches, then combined.</summary>
    public Task<string> SummarizeAsync(
        string filePathOrName,
        CancellationToken cancellationToken = default,
        int? pageNumber = null)
        => SummarizeAsync(filePathOrName, progress: null, cancellationToken, pageNumber);

    /// <summary>
    /// Summarize one loaded document, or one page when <paramref name="pageNumber"/> is set.
    /// Long content is summarized in batches, then combined.
    /// </summary>
    public async Task<string> SummarizeAsync(
        string filePathOrName,
        IProgress<string>? progress,
        CancellationToken cancellationToken = default,
        int? pageNumber = null)
    {
        if (_chatClient == null)
            throw new InvalidOperationException(
                "SummarizeAsync requires a chat client. Set MicrosoftAIConnectorOptions.ChatClient.");

        if (pageNumber is int invalidPage && invalidPage < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1.");

        string name = Path.GetFileName(filePathOrName);
        string label = pageNumber is int page ? $"{name} p{page}" : name;
        var docChunks = SnapshotChunks()
            .Where(c =>
                string.Equals(c.SourceFileName, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.SourceFilePath, filePathOrName, StringComparison.OrdinalIgnoreCase))
            .Where(c => pageNumber == null || c.PageNumber == pageNumber)
            .OrderBy(c => c.PageNumber)
            .ThenBy(c => c.ChunkIndex)
            .ToList();

        if (docChunks.Count == 0)
            return $"No content found for {label}.";

        List<string> batches = Pack(docChunks.Select(c => c.Text), SummarizeBatchCharacters);
        if (batches.Count == 1)
        {
            progress?.Report($"Summarizing {label} in one request …");
            return await CompleteAsync(
                "Summarize the following document content in clear prose.",
                $"Document: {label}\n\n{batches[0]}",
                cancellationToken).ConfigureAwait(false);
        }

        var partials = new List<string>(batches.Count);
        for (int i = 0; i < batches.Count; i++)
        {
            progress?.Report($"Summarizing {label}: part {i + 1} of {batches.Count} …");
            string part = await CompleteAsync(
                "Summarize this part of a document. Keep the facts, names, and figures.",
                $"Document: {label}\nPart {i + 1} of {batches.Count}\n\n{batches[i]}",
                cancellationToken).ConfigureAwait(false);
            partials.Add(part);
        }

        progress?.Report($"Combining {batches.Count} partial summaries …");
        return await CompleteAsync(
            "Combine these partial summaries into one clear summary. Keep the facts and figures.",
            $"Document: {label}\n\n" + string.Join("\n\n", partials),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Return the best-matching chunks. No chat completion.</summary>
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int? topK = null,
        string? sourceFileName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query is required.", nameof(query));

        IReadOnlyList<SearchResult> ranked = await RankOrLexicalAsync(
                query, cancellationToken, embedQuery: true, sourceFileName)
            .ConfigureAwait(false);
        if (_searchMinScore > 0)
            ranked = ranked.Where(h => h.Score >= _searchMinScore).ToList();
        int take = Math.Max(1, topK ?? _defaultTopK);
        if (ranked.Count <= take)
            return ranked;
        return ranked.Take(take).ToList();
    }

    async Task<IReadOnlyList<SearchResult>> RankOrLexicalAsync(
        string query,
        CancellationToken cancellationToken,
        bool embedQuery,
        string? sourceFileName = null)
    {
        try
        {
            return await RankAsync(query, cancellationToken, sourceFileName, embedQuery)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (embedQuery && TransportRetry.IsReset(ex))
        {
            return await RankAsync(query, cancellationToken, sourceFileName, embedQuery: false)
                .ConfigureAwait(false);
        }
    }

    async Task<IReadOnlyList<SearchResult>> RankAsync(
        string query,
        CancellationToken cancellationToken,
        string? sourceFileName = null,
        bool embedQuery = true)
    {
        List<AiChunk> pool = SnapshotChunks().Where(c => SourceMatches(c, sourceFileName)).ToList();
        if (pool.Count == 0)
            return Array.Empty<SearchResult>();

        ReadOnlyMemory<float> queryVector = default;
        var vectorScore = new Dictionary<string, double>(StringComparer.Ordinal);
        if (embedQuery)
        {
            queryVector = await EmbeddingIndexer
                .EmbedQueryAsync(query, _embeddingGenerator, cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<SearchResult> vectorHits = await _vectorIndex
                .SearchAsync(queryVector, pool.Count, sourceFileName, cancellationToken)
                .ConfigureAwait(false);
            foreach (SearchResult hit in vectorHits)
                vectorScore[hit.Chunk.Id] = hit.Score;
        }

        var ranked = new List<SearchResult>(pool.Count);
        foreach (AiChunk chunk in pool)
        {
            double lexical = LexicalScore.Score(query, chunk.Text);
            double vector = 0;
            if (embedQuery)
            {
                vectorScore.TryGetValue(chunk.Id, out vector);
                if (vector == 0)
                    vector = Cosine(queryVector.Span, chunk.Embedding.Span);
            }

            ranked.Add(new SearchResult
            {
                Chunk = chunk,
                Score = LexicalScore.Combine(lexical, vector),
            });
        }

        ranked.Sort(static (a, b) =>
        {
            int byScore = b.Score.CompareTo(a.Score);
            if (byScore != 0)
                return byScore;
            int byPage = a.Chunk.PageNumber.CompareTo(b.Chunk.PageNumber);
            if (byPage != 0)
                return byPage;
            return a.Chunk.ChunkIndex.CompareTo(b.Chunk.ChunkIndex);
        });
        return ranked;
    }

    IReadOnlyList<RetrievedPassage> TakeAllContext(string? sourceFileName)
    {
        return SnapshotChunks()
            .Where(c => SourceMatches(c, sourceFileName) && c.Text.Length > 0)
            .OrderBy(c => c.SourceFileName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.PageNumber)
            .ThenBy(c => c.ChunkIndex)
            .Select(c => new RetrievedPassage
            {
                SourceId = c.Id,
                SourceFileName = c.SourceFileName,
                SourceFilePath = c.SourceFilePath,
                PageNumber = c.PageNumber,
                Text = c.Text,
            })
            .ToList();
    }

    static IReadOnlyList<RetrievedPassage> TakeExcerpts(
        IReadOnlyList<SearchResult> ranked,
        int count,
        int maxCharacters,
        double minScore)
    {
        int take = Math.Max(1, count);
        var eligible = new List<SearchResult>();
        foreach (SearchResult hit in ranked)
        {
            if (hit.Score < minScore || hit.Chunk.Text.Length <= 0)
                continue;
            eligible.Add(hit);
        }

        var chosen = new List<RetrievedPassage>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int used = 0;

        bool TryAdd(SearchResult hit)
        {
            if (!seen.Add(hit.Chunk.Id))
                return false;
            int extra = hit.Chunk.Text.Length;
            if (chosen.Count > 0 && extra > maxCharacters - used)
                return false;
            chosen.Add(GroundedPrompt.FromHit(hit));
            used += extra;
            return true;
        }

        var queues = eligible
            .GroupBy(h => h.Chunk.SourceFileName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new Queue<SearchResult>(g))
            .ToList();

        int rounds = 0;
        while (chosen.Count < take && queues.Count > 0 && rounds < eligible.Count)
        {
            rounds++;
            bool progress = false;
            for (int i = 0; i < queues.Count && chosen.Count < take;)
            {
                SearchResult hit = queues[i].Dequeue();
                if (queues[i].Count == 0)
                    queues.RemoveAt(i);
                else
                    i++;
                if (TryAdd(hit))
                    progress = true;
            }

            if (!progress)
                break;
        }

        if (chosen.Count < take)
        {
            foreach (SearchResult hit in eligible)
            {
                if (chosen.Count >= take)
                    break;
                TryAdd(hit);
            }
        }

        return chosen;
    }

    static List<string> Pack(IEnumerable<string> parts, int budget)
    {
        var batches = new List<string>();
        var current = new StringBuilder();
        foreach (string part in parts)
        {
            if (current.Length > 0 && current.Length + part.Length > budget)
            {
                batches.Add(current.ToString());
                current.Clear();
            }

            if (current.Length > 0)
                current.Append("\n---\n");
            current.Append(part);
        }

        if (current.Length > 0)
            batches.Add(current.ToString());
        return batches;
    }

    static GroundedAnswer Interpret(string raw, IReadOnlyList<RetrievedPassage> excerpts)
    {
        ParsedModelAnswer? parsed = AnswerParser.TryParse(raw);
        if (parsed == null)
        {
            if (GroundedPrompt.LooksInsufficient(raw) || string.IsNullOrWhiteSpace(raw))
                return GroundedAnswer.Insufficient(excerpts);
            return new GroundedAnswer
            {
                Text = raw.Trim(),
                Evidence = excerpts,
                Warning = "The model did not return verifiable citations.",
                CitationsValid = false,
            };
        }

        if (parsed.InsufficientEvidence || GroundedPrompt.LooksInsufficient(parsed.Answer))
            return GroundedAnswer.Insufficient(excerpts, parsed.Warning);

        CitationCheck check = CitationValidator.Validate(parsed, excerpts);
        if (parsed.Citations.Count > 0 && !check.Ok)
        {
            return new GroundedAnswer
            {
                Text = parsed.Answer,
                Evidence = excerpts,
                Warning = check.Error,
                CitationsValid = false,
            };
        }

        string answer = string.IsNullOrWhiteSpace(parsed.Answer) ? raw.Trim() : parsed.Answer;
        return new GroundedAnswer
        {
            Text = answer,
            Evidence = excerpts,
            Citations = check.Citations,
            Warning = parsed.Warning ?? (check.Citations.Count == 0
                ? "The model did not cite a retrieved excerpt."
                : null),
            CitationsValid = check.Ok && check.Citations.Count > 0,
        };
    }

    async Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        ChatResponse response = await TransportRetry.RunAsync(
                () => _chatClient!.GetResponseAsync(
                    messages,
                    ChatCompletionOptions(),
                    cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);
        return response.Text?.Trim() ?? "";
    }

    async Task<string> CompleteAsync(string system, string user, CancellationToken cancellationToken)
    {
        return await CompleteAsync(
            new List<ChatMessage>
            {
                new(ChatRole.System, system),
                new(ChatRole.User, user),
            },
            cancellationToken).ConfigureAwait(false);
    }

    ChatOptions ChatCompletionOptions()
    {
        var options = new ChatOptions { Temperature = 0 };
        if (_maxOutputTokens > 0)
            options.MaxOutputTokens = _maxOutputTokens;
        return options;
    }

    List<AiChunk> SnapshotChunks()
    {
        lock (_gate)
            return _allChunks.ToList();
    }

    static bool SourceMatches(AiChunk chunk, string? sourceFileName)
    {
        if (string.IsNullOrEmpty(sourceFileName))
            return true;
        return string.Equals(chunk.SourceFileName, sourceFileName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetFileName(chunk.SourceFilePath), sourceFileName, StringComparison.OrdinalIgnoreCase);
    }

    static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            return 0;

        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        if (na == 0 || nb == 0)
            return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
