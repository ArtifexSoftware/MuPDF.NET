namespace MuPDF.NET.PDF4LLM.AI.Models;

/// <summary>Ask result: answer text, excerpts, and citations that passed validation.</summary>
public sealed class GroundedAnswer
{
    public const string InsufficientMessage =
        "The provided excerpts do not contain enough information to answer the question.";

    public required string Text { get; init; }
    public bool InsufficientEvidence { get; init; }
    public IReadOnlyList<RetrievedPassage> Evidence { get; init; } = Array.Empty<RetrievedPassage>();
    public IReadOnlyList<Citation> Citations { get; init; } = Array.Empty<Citation>();
    public string? Warning { get; init; }
    public bool CitationsValid { get; init; }

    public static GroundedAnswer Insufficient(
        IReadOnlyList<RetrievedPassage>? evidence = null,
        string? warning = null) =>
        new()
        {
            Text = InsufficientMessage,
            InsufficientEvidence = true,
            Evidence = evidence ?? Array.Empty<RetrievedPassage>(),
            Warning = warning,
            CitationsValid = false,
        };
}
