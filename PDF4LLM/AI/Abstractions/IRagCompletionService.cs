using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Abstractions;

/// <summary>LLM reasoning for Q&amp;A and summaries (Phase 3 — Azure OpenAI / Microsoft.Extensions.AI).</summary>
public interface IRagCompletionService
{
    /// <param name="question">User question to answer from retrieved context.</param>
    /// <param name="context">Retrieved chunks used as grounding context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> AskAsync(
        string question,
        IReadOnlyList<SearchResult> context,
        CancellationToken cancellationToken = default);

    /// <param name="documentName">Display name of the document to summarize.</param>
    /// <param name="documentChunks">Indexed chunks belonging to that document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> SummarizeAsync(
        string documentName,
        IReadOnlyList<AiChunk> documentChunks,
        CancellationToken cancellationToken = default);
}
