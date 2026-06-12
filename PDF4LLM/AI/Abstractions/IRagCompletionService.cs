using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Abstractions;

/// <summary>LLM reasoning for Q&amp;A and summaries (Phase 3 — Azure OpenAI / Microsoft.Extensions.AI).</summary>
public interface IRagCompletionService
{
    Task<string> AskAsync(
        string question,
        IReadOnlyList<SearchResult> context,
        CancellationToken cancellationToken = default);

    Task<string> SummarizeAsync(
        string documentName,
        IReadOnlyList<AiChunk> documentChunks,
        CancellationToken cancellationToken = default);
}
