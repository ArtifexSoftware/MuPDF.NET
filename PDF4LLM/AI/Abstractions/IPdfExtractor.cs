using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Abstractions;

/// <summary>Extracts structured text from PDF files (Phase 1).</summary>
public interface IPdfExtractor
{
    Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        string pdfPath,
        CancellationToken cancellationToken = default);
}
