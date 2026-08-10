using MuPDF.NET.PDF4LLM.AI.Models;

namespace MuPDF.NET.PDF4LLM.AI.Abstractions;

/// <summary>Extracts structured text from PDF files (Phase 1).</summary>
public interface IPdfExtractor
{
    /// <param name="pdfPath">Path to the PDF file to extract.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        string pdfPath,
        CancellationToken cancellationToken = default);
}
