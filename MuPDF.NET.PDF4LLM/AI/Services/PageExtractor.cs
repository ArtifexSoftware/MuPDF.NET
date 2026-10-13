using Newtonsoft.Json.Linq;
using MuPDF.NET.PDF4LLM.AI.Abstractions;
using MuPDF.NET.PDF4LLM.AI.Models;

namespace MuPDF.NET.PDF4LLM.AI.Services;

/// <summary>
/// Reads one PDF into per-page text using <see cref="MuPDF4LLM.ToMarkdown"/> or
/// <see cref="MuPDF4LLM.ToText"/> (layout). Empty pages are kept; missing files fail.
/// </summary>
internal sealed class PageExtractor : IDocumentExtractor
{
    static readonly object LayoutGate = new();

    readonly bool _useLayout;

    public PageExtractor(bool useLayout) => _useLayout = useLayout;

    public IReadOnlyList<ExtractedPage> Extract(string pdfPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Extract(pdfPath, _useLayout);
    }

    public static IReadOnlyList<ExtractedPage> Extract(string pdfPath, bool useLayout)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
            throw new ArgumentException("PDF path is required.", nameof(pdfPath));
        if (!File.Exists(pdfPath))
            throw new FileNotFoundException("PDF not found.", pdfPath);

        string fullPath = Path.GetFullPath(pdfPath);
        lock (LayoutGate)
        {
            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(useLayout);
                string payload = useLayout
                    ? MuPDF4LLM.ToText(fullPath, pageChunks: true, header: true, footer: true, showProgress: false)
                    : MuPDF4LLM.ToMarkdown(fullPath, pageChunks: true, header: true, footer: true, showProgress: false);
                return ParsePages(payload, fullPath);
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }
    }

    static List<ExtractedPage> ParsePages(string payload, string pdfPath)
    {
        string fileName = Path.GetFileName(pdfPath);
        var result = new List<ExtractedPage>();
        if (string.IsNullOrWhiteSpace(payload))
            return result;

        JArray chunks;
        try
        {
            chunks = JArray.Parse(payload);
        }
        catch (Newtonsoft.Json.JsonException)
        {
            result.Add(Page(pdfPath, fileName, 1, payload, metadata: null));
            return result;
        }

        foreach (JToken token in chunks)
        {
            var metadata = token["metadata"] as JObject;
            int pageNumber = metadata?["page_number"]?.Value<int>()
                ?? metadata?["page"]?.Value<int>()
                ?? result.Count + 1;
            string text = token["text"]?.ToString() ?? "";
            result.Add(Page(pdfPath, fileName, pageNumber, text, metadata));
        }

        return result;
    }

    static ExtractedPage Page(string pdfPath, string fileName, int pageNumber, string text, JObject? metadata)
    {
        var bag = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["file_path"] = pdfPath,
            ["page_number"] = pageNumber,
        };
        if (metadata != null)
        {
            foreach (JProperty property in metadata.Properties())
            {
                if (property.Value.Type is JTokenType.Null or JTokenType.Undefined)
                    continue;
                bag[property.Name] = property.Value.Type == JTokenType.Integer
                    ? property.Value.Value<int>()
                    : property.Value.ToString();
            }
        }

        return new ExtractedPage
        {
            SourceFilePath = pdfPath,
            SourceFileName = fileName,
            PageNumber = pageNumber,
            Text = text,
            Metadata = bag,
        };
    }
}
