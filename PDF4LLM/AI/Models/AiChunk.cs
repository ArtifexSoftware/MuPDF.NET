namespace PDF4LLM.AI.Models;

/// <summary>A text segment indexed for retrieval (RAG chunk).</summary>
public sealed class AiChunk
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required string SourceFilePath { get; init; }
    public required string SourceFileName { get; init; }
    public int PageNumber { get; init; }
    public int ChunkIndex { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    public ReadOnlyMemory<float> Embedding { get; set; }
}
