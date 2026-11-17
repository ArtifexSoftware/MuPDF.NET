namespace MuPDF.NET.PDF4LLM.AI.Evaluation;

/// <summary>Metrics for one <see cref="QaCase"/>.</summary>
public sealed class QaCaseResult
{
    public required string CaseId { get; init; }
    public bool RetrievalRecall { get; init; }
    public bool AbstentionCorrect { get; init; }
    public bool ClaimsCorrect { get; init; }
    public bool NoForbiddenClaims { get; init; }
    public bool CitationsValid { get; init; }
    public double SearchMs { get; init; }
    public double AskMs { get; init; }
    public string? Detail { get; init; }

    public bool Passed =>
        RetrievalRecall && AbstentionCorrect && ClaimsCorrect && NoForbiddenClaims;
}
