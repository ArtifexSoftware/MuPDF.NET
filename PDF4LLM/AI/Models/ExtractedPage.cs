namespace PDF4LLM.AI.Models;

/// <summary>Structured page content extracted from a PDF via PDF4LLM.</summary>
public sealed class ExtractedPage
{
    public required string SourceFilePath { get; init; }
    public required string SourceFileName { get; init; }
    public int PageNumber { get; init; }
    public required string Text { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
