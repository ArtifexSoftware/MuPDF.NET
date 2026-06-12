using Microsoft.Extensions.AI;
using PDF4LLM.AI.Abstractions;
using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Services;

/// <summary>Phase 3 Q&amp;A and summarization via <see cref="IChatClient"/> (Azure OpenAI / GPT-4o).</summary>
public sealed class ExtensionsAiRagCompletionService : IRagCompletionService
{
    private readonly IChatClient _chatClient;

    public ExtensionsAiRagCompletionService(IChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    public async Task<string> AskAsync(
        string question,
        IReadOnlyList<SearchResult> context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));

        string contextBlock = FormatContext(context);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "You answer questions using only the provided document excerpts. " +
                "If the answer is not in the excerpts, say you do not have enough information."),
            new(ChatRole.User,
                $"Document excerpts:\n{contextBlock}\n\nQuestion: {question}"),
        };

        ChatResponse response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Text?.Trim() ?? "";
    }

    public async Task<string> SummarizeAsync(
        string documentName,
        IReadOnlyList<AiChunk> documentChunks,
        CancellationToken cancellationToken = default)
    {
        if (documentChunks.Count == 0)
            return $"No content found for {documentName}.";

        string body = string.Join(
            "\n---\n",
            documentChunks.OrderBy(c => c.PageNumber).ThenBy(c => c.ChunkIndex).Select(c => c.Text));

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "Summarize the following document content in clear prose for a business reader."),
            new(ChatRole.User, $"Document: {documentName}\n\n{body}"),
        };

        ChatResponse response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Text?.Trim() ?? "";
    }

    private static string FormatContext(IReadOnlyList<SearchResult> context)
    {
        if (context.Count == 0)
            return "(no relevant excerpts)";

        return string.Join(
            "\n\n",
            context.Select((r, i) =>
                $"[{i + 1}] ({r.Chunk.SourceFileName}, page {r.Chunk.PageNumber}, score {r.Score:F3})\n{r.Chunk.Text}"));
    }
}
