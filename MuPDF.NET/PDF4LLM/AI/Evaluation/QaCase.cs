namespace MuPDF.NET.PDF4LLM.AI.Evaluation;

/// <summary>One evaluation item: question, expected claims, and passages that should be retrieved.</summary>
public sealed class QaCase
{
    public required string Id { get; init; }
    public required string Question { get; init; }
    public string? SourceFileName { get; init; }
    public int AskTopK { get; init; } = 2;
    public int SearchTopK { get; init; } = 8;
    public bool ExpectInsufficient { get; init; }
    public IReadOnlyList<string> ExpectedClaims { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ForbiddenClaims { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ExpectedPassageContains { get; init; } = Array.Empty<string>();
}
