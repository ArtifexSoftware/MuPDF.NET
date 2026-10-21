using MuPDF.NET.PDF4LLM.AI.Models;

namespace MuPDF.NET.PDF4LLM.AI.Abstractions;

/// <summary>
/// Turns one PDF into per-page text. Extraction is independent of chunking and indexing.
/// </summary>
public interface IDocumentExtractor
{
    IReadOnlyList<ExtractedPage> Extract(string pdfPath, CancellationToken cancellationToken = default);
}
