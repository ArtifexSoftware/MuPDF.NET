namespace MuPDF.NET.PDF4LLM.AI.Models;

/// <summary>One excerpt sent to the chat model, with provenance for citations.</summary>
public sealed class RetrievedPassage
{
    public required string SourceId { get; init; }
    public required string SourceFileName { get; init; }
    public required string SourceFilePath { get; init; }
    public int PageNumber { get; init; }
    public required string Text { get; init; }
}
