using System.Text.RegularExpressions;
using MuPDF.NET.PDF4LLM.AI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MuPDF.NET.PDF4LLM.AI.Services;

internal sealed class ParsedModelAnswer
{
    public string Answer { get; init; } = "";
    public bool InsufficientEvidence { get; init; }
    public string? Warning { get; init; }
    public IReadOnlyList<ParsedCitation> Citations { get; init; } = Array.Empty<ParsedCitation>();
}

internal sealed class ParsedCitation
{
    public string SourceId { get; init; } = "";
    public int? Page { get; init; }
    public string Quote { get; init; } = "";
}

/// <summary>Reads a JSON object from the model, including fenced or leading prose.</summary>
internal static class AnswerParser
{
    static readonly Regex Fence = new(@"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static ParsedModelAnswer? TryParse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        string? json = ExtractJson(raw);
        if (json == null)
            return null;

        try
        {
            JObject obj = JObject.Parse(json);
            var citations = new List<ParsedCitation>();
            if (obj["citations"] is JArray array)
            {
                foreach (JToken token in array)
                {
                    if (token is not JObject item)
                        continue;
                    citations.Add(new ParsedCitation
                    {
                        SourceId = item["sourceId"]?.ToString() ?? item["source_id"]?.ToString() ?? "",
                        Page = item["page"]?.Value<int?>() ?? item["pageNumber"]?.Value<int?>(),
                        Quote = item["quote"]?.ToString() ?? "",
                    });
                }
            }

            return new ParsedModelAnswer
            {
                Answer = obj["answer"]?.ToString()?.Trim() ?? "",
                InsufficientEvidence = obj["insufficientEvidence"]?.Value<bool>()
                    ?? obj["insufficient_evidence"]?.Value<bool>()
                    ?? false,
                Warning = NullIfEmpty(obj["warning"]?.ToString()),
                Citations = citations,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static string? ExtractJson(string raw)
    {
        Match fence = Fence.Match(raw);
        if (fence.Success)
            return fence.Groups[1].Value.Trim();

        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;
        return raw.Substring(start, end - start + 1);
    }

    static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
