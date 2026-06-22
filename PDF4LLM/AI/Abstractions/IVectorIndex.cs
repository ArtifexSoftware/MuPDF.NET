using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Abstractions;

/// <summary>Vector store for semantic search (Phase 2 — Azure AI Search or in-memory).</summary>
public interface IVectorIndex
{
    /// <param name="chunks">Embedded chunks to upsert into the index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexAsync(IReadOnlyList<AiChunk> chunks, CancellationToken cancellationToken = default);

    /// <param name="queryEmbedding">Query vector produced by the embedding model.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="sourceFileName">Optional file-name filter to restrict results to one PDF.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK = 5,
        string? sourceFileName = null,
        CancellationToken cancellationToken = default);
}
