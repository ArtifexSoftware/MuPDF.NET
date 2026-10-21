using System.Text;
using MuPDF.NET.PDF4LLM.AI.Models;

namespace MuPDF.NET.PDF4LLM.AI.Services;

internal readonly struct CitationCheck
{
    public CitationCheck(bool ok, string? error, IReadOnlyList<Citation> citations)
    {
        Ok = ok;
        Error = error;
        Citations = citations;
    }

    public bool Ok { get; }
    public string? Error { get; }
    public IReadOnlyList<Citation> Citations { get; }
}

/// <summary>
/// Checks that each cited source id, page, and quote exist in the excerpts sent to the model.
/// Valid JSON is not treated as proof that the answer is true.
/// </summary>
internal static class CitationValidator
{
    public static CitationCheck Validate(ParsedModelAnswer parsed, IReadOnlyList<RetrievedPassage> excerpts)
    {
        var byId = new Dictionary<string, RetrievedPassage>(StringComparer.Ordinal);
        foreach (RetrievedPassage excerpt in excerpts)
            byId[excerpt.SourceId] = excerpt;

        var citations = new List<Citation>();
        foreach (ParsedCitation item in parsed.Citations)
        {
            if (string.IsNullOrWhiteSpace(item.SourceId))
                return Fail("A citation is missing sourceId.");
            if (!byId.TryGetValue(item.SourceId, out RetrievedPassage? excerpt))
                return Fail($"Cited sourceId '{item.SourceId}' was not in the excerpts.");
            if (item.Page is int page && page != excerpt.PageNumber)
                return Fail($"Cited page {page} does not match excerpt {excerpt.SourceId} (page {excerpt.PageNumber}).");
            string quote = item.Quote.Trim();
            if (quote.Length < 3)
                return Fail("A citation quote is empty or too short.");
            if (!ContainsQuote(excerpt.Text, quote))
                return Fail($"Quoted text was not found in excerpt '{excerpt.SourceId}'.");

            citations.Add(new Citation
            {
                SourceId = excerpt.SourceId,
                SourceFileName = excerpt.SourceFileName,
                PageNumber = excerpt.PageNumber,
                Quote = quote,
            });
        }

        return new CitationCheck(true, null, citations);
    }

    static CitationCheck Fail(string error) =>
        new(false, error, Array.Empty<Citation>());

    public static bool ContainsQuote(string excerpt, string quote)
    {
        if (excerpt.IndexOf(quote, StringComparison.Ordinal) >= 0)
            return true;
        return Fold(excerpt).IndexOf(Fold(quote), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static string Fold(string text)
    {
        var buffer = new StringBuilder(text.Length);
        bool space = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                space = true;
                continue;
            }

            if (space && buffer.Length > 0)
                buffer.Append(' ');
            space = false;
            buffer.Append(c);
        }

        return buffer.ToString();
    }
}
