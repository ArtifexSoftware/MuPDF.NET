using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Abstractions;

/// <summary>Vector store for semantic search (Phase 2 — Azure AI Search or in-memory).</summary>
public interface IVectorIndex
{
    Task IndexAsync(IReadOnlyList<AiChunk> chunks, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK = 5,
        string? sourceFileName = null,
        CancellationToken cancellationToken = default);
}
