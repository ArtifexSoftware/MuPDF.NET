namespace MuPDF.NET.PDF4LLM.AI.Models;

/// <summary>A quote the model attributed to a retrieved excerpt.</summary>
public sealed class Citation
{
    public required string SourceId { get; init; }
    public required string SourceFileName { get; init; }
    public int PageNumber { get; init; }
    public required string Quote { get; init; }
}
