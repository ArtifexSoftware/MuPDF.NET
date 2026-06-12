using Newtonsoft.Json.Linq;
using PDF4LLM.AI.Abstractions;
using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Services;

/// <summary>Phase 1 extraction via PDF4LLM <c>to_text(page_chunks=true)</c>.</summary>
public sealed class Pdf4LlmExtractor : IPdfExtractor
{
    private readonly bool _useLayout;

    public Pdf4LlmExtractor(bool useLayout = false) => _useLayout = useLayout;

    public Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        string pdfPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(pdfPath))
            throw new ArgumentException("PDF path is required.", nameof(pdfPath));
        if (!File.Exists(pdfPath))
            throw new FileNotFoundException("PDF not found.", pdfPath);

        bool priorLayout = PdfExtractor.UseLayout;
        try
        {
            PdfExtractor.SetUseLayout(_useLayout);

            string json = PdfExtractor.ToText(
                pdfPath,
                pageChunks: true,
                header: true,
                footer: true,
                showProgress: false);

            var pages = ParsePageChunks(json, pdfPath);
            return Task.FromResult<IReadOnlyList<ExtractedPage>>(pages);
        }
        catch (NotSupportedException) when (!_useLayout)
        {
            // Layout disabled: fall back to legacy RAG markdown per page.
            return Task.FromResult<IReadOnlyList<ExtractedPage>>(ExtractLegacyPages(pdfPath));
        }
        finally
        {
            PdfExtractor.SetUseLayout(priorLayout);
        }
    }

    private static List<ExtractedPage> ParsePageChunks(string json, string pdfPath)
    {
        var result = new List<ExtractedPage>();
        string fileName = Path.GetFileName(pdfPath);
        var chunks = JArray.Parse(json);

        foreach (JToken token in chunks)
        {
            var metadata = token["metadata"] as JObject ?? new JObject();
            int pageNumber = metadata["page_number"]?.Value<int>() ?? result.Count + 1;
            string text = token["text"]?.ToString() ?? "";

            var meta = new Dictionary<string, object>();
            foreach (var prop in metadata.Properties())
                meta[prop.Name] = prop.Value?.ToObject<object>() ?? "";

            result.Add(new ExtractedPage
            {
                SourceFilePath = pdfPath,
                SourceFileName = fileName,
                PageNumber = pageNumber,
                Text = text,
                Metadata = meta,
            });
        }

        return result;
    }

    private static List<ExtractedPage> ExtractLegacyPages(string pdfPath)
    {
        var result = new List<ExtractedPage>();
        string fileName = Path.GetFileName(pdfPath);

        using var doc = new MuPDF.NET.Document(pdfPath);
        for (int i = 0; i < doc.PageCount; i++)
        {
            string text = PdfExtractor.ToMarkdown(
                doc,
                pages: new List<int> { i },
                filename: pdfPath,
                showProgress: false);

            result.Add(new ExtractedPage
            {
                SourceFilePath = pdfPath,
                SourceFileName = fileName,
                PageNumber = i + 1,
                Text = text,
                Metadata = new Dictionary<string, object>
                {
                    ["file_path"] = pdfPath,
                    ["page_number"] = i + 1,
                    ["page_count"] = doc.PageCount,
                },
            });
        }

        return result;
    }
}
