namespace PDF4LLM.AI.Models;

/// <summary>Chunk returned by semantic search with relevance score.</summary>
public sealed class SearchResult
{
    public required AiChunk Chunk { get; init; }
    public double Score { get; init; }
}
