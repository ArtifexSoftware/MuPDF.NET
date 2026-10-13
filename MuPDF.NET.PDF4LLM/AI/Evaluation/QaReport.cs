namespace MuPDF.NET.PDF4LLM.AI.Evaluation;

/// <summary>Aggregate evaluation over a dataset.</summary>
public sealed class QaReport
{
    public IReadOnlyList<QaCaseResult> Cases { get; init; } = Array.Empty<QaCaseResult>();
    public int Passed => Cases.Count(c => c.Passed);
    public int Total => Cases.Count;
    public double RetrievalRecall => Rate(c => c.RetrievalRecall);
    public double AbstentionAccuracy => Rate(c => c.AbstentionCorrect);
    public double ClaimAccuracy => Rate(c => c.ClaimsCorrect);
    public double CitationValidity => Rate(c => c.CitationsValid);
    public double MeanSearchMs => Cases.Count == 0 ? 0 : Cases.Average(c => c.SearchMs);
    public double MeanAskMs => Cases.Count == 0 ? 0 : Cases.Average(c => c.AskMs);

    double Rate(Func<QaCaseResult, bool> ok) =>
        Cases.Count == 0 ? 0 : Cases.Count(ok) / (double)Cases.Count;
}
