using MuPDF.NET.PDF4LLM.AI.Models;

namespace MuPDF.NET.PDF4LLM.AI.Services;

/// <summary>
/// Splits page text into retrieval units. Table rows stay whole (header repeated).
/// Prose splits on paragraphs, then sentences, then the character limit, with overlap.
/// </summary>
internal static class TextChunker
{
    public static List<AiChunk> Chunk(IReadOnlyList<ExtractedPage> pages, int maxChars, int overlapChars)
    {
        int max = Math.Max(200, maxChars);
        int overlap = Math.Clamp(overlapChars, 0, max / 2);
        var chunks = new List<AiChunk>();

        foreach (ExtractedPage page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text))
                continue;

            int indexOnPage = 0;
            foreach (ChunkPart part in SplitPage(page.Text, max, overlap))
            {
                string text = part.Text.Trim();
                if (text.Length == 0)
                    continue;

                chunks.Add(new AiChunk
                {
                    Id = $"{page.SourceFileName}|p{page.PageNumber}|c{indexOnPage}",
                    Text = text,
                    SourceFilePath = page.SourceFilePath,
                    SourceFileName = page.SourceFileName,
                    PageNumber = page.PageNumber,
                    ChunkIndex = indexOnPage,
                    Metadata = CopyMetadata(page, part.Heading, indexOnPage),
                });
                indexOnPage++;
            }
        }

        return chunks;
    }

    public static List<ChunkPart> SplitPage(string text, int maxChars, int overlapChars)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<ChunkPart>();

        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var parts = new List<ChunkPart>();
        var prose = new List<string>();
        string? heading = null;
        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i];
            if (TryMarkdownHeading(line, out string? nextHeading))
            {
                FlushProse(prose, parts, maxChars, overlapChars, heading);
                heading = nextHeading;
                prose.Add(line);
                i++;
                continue;
            }

            if (!IsDelimited(line))
            {
                prose.Add(line);
                i++;
                continue;
            }

            string? title = TakeTitle(prose) ?? heading;
            FlushProse(prose, parts, maxChars, overlapChars, heading);
            if (!string.IsNullOrEmpty(title))
                heading = title;
            int start = i;
            while (i < lines.Length && ContinuesTable(lines, i))
                i++;
            AddTableRows(lines, start, i, title, parts);
        }

        FlushProse(prose, parts, maxChars, overlapChars, heading);
        return parts;
    }

    static Dictionary<string, object> CopyMetadata(ExtractedPage page, string? heading, int chunkIndex)
    {
        var bag = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["page_number"] = page.PageNumber,
            ["chunk_index"] = chunkIndex,
            ["source_file"] = page.SourceFileName,
        };
        foreach (KeyValuePair<string, object> pair in page.Metadata)
            bag[pair.Key] = pair.Value;
        if (!string.IsNullOrEmpty(heading))
            bag["heading"] = heading;
        return bag;
    }

    static void AddTableRows(string[] lines, int start, int end, string? title, List<ChunkPart> parts)
    {
        string? header = null;
        for (int i = start; i < end; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || IsRule(line))
                continue;
            if (header == null)
            {
                header = line;
                continue;
            }

            var row = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(title))
            {
                row.Append(title);
                row.Append('\n');
            }

            row.Append(header);
            row.Append('\n');
            row.Append(line);
            parts.Add(new ChunkPart(row.ToString(), title));
        }
    }

    static string? TakeTitle(List<string> prose)
    {
        for (int i = prose.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(prose[i]))
                continue;
            string line = prose[i].Trim();
            if (TryMarkdownHeading(line, out string? heading))
            {
                prose.RemoveAt(i);
                return heading;
            }

            if (line.Length > 120)
                return null;
            prose.RemoveAt(i);
            return line;
        }

        return null;
    }

    static bool TryMarkdownHeading(string line, out string? heading)
    {
        heading = null;
        string trimmed = line.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '#')
            return false;
        int i = 0;
        while (i < trimmed.Length && trimmed[i] == '#' && i < 6)
            i++;
        if (i >= trimmed.Length || trimmed[i] != ' ')
            return false;
        heading = trimmed.Substring(i).Trim();
        return heading.Length > 0;
    }

    static void FlushProse(List<string> prose, List<ChunkPart> parts, int maxChars, int overlapChars, string? heading)
    {
        if (prose.Count == 0)
            return;

        string text = string.Join("\n", prose).Trim();
        prose.Clear();
        if (text.Length == 0)
            return;

        foreach (string slice in PackProse(text, maxChars, overlapChars))
            parts.Add(new ChunkPart(slice, heading));
    }

    static List<string> PackProse(string text, int maxChars, int overlapChars)
    {
        var parts = new List<string>();
        if (text.Length <= maxChars)
        {
            parts.Add(text);
            return parts;
        }

        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(maxChars, text.Length - start);
            int end = start + length;
            if (end < text.Length)
                end = PreferLogicalBreak(text, start, end, maxChars);

            string slice = text.Substring(start, end - start).Trim();
            if (slice.Length > 0 && (parts.Count == 0 || parts[parts.Count - 1] != slice))
                parts.Add(slice);
            if (end >= text.Length)
                break;
            int next = overlapChars == 0 ? end : Math.Max(end - overlapChars, start + 1);
            if (next <= start)
                next = start + 1;
            start = next;
        }

        return parts;
    }

    static int PreferLogicalBreak(string text, int start, int end, int maxChars)
    {
        int window = Math.Min(end - start, Math.Max(maxChars / 3, 80));
        int paragraph = text.LastIndexOf("\n\n", end - 1, window, StringComparison.Ordinal);
        if (paragraph > start + maxChars / 4)
            return paragraph + 2;

        int line = text.LastIndexOf('\n', end - 1, window);
        int sentence = LastSentenceBreak(text, start, end, window);
        int breakAt = Math.Max(line, sentence);
        if (breakAt > start + maxChars / 3)
            return line == breakAt ? breakAt + 1 : breakAt;
        return end;
    }

    static int LastSentenceBreak(string text, int start, int end, int window)
    {
        int from = Math.Max(start, end - window);
        int best = -1;
        for (int i = end - 2; i >= from; i--)
        {
            char c = text[i];
            if (c is not '.' and not '?' and not '!')
                continue;
            if (i + 1 < text.Length && (text[i + 1] == ' ' || text[i + 1] == '\n'))
            {
                best = i + 2;
                break;
            }
        }

        return best;
    }

    static bool ContinuesTable(string[] lines, int i)
    {
        if (IsDelimited(lines[i]) || IsRule(lines[i]))
            return true;
        if (!string.IsNullOrWhiteSpace(lines[i]))
            return false;

        for (int j = i + 1; j < lines.Length; j++)
        {
            if (string.IsNullOrWhiteSpace(lines[j]))
                continue;
            return IsDelimited(lines[j]) || IsRule(lines[j]);
        }

        return false;
    }

    static bool IsDelimited(string line)
    {
        int pipes = 0;
        int semis = 0;
        foreach (char c in line)
        {
            if (c == '|')
                pipes++;
            else if (c == ';')
                semis++;
        }

        return pipes >= 2 || semis >= 2;
    }

    static bool IsRule(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;
        foreach (char c in line)
        {
            if (c is '|' or '-' or ':' or ' ' or '\t' or '+')
                continue;
            return false;
        }

        return line.IndexOf('-') >= 0 || line.IndexOf('+') >= 0;
    }
}

internal readonly struct ChunkPart
{
    public ChunkPart(string text, string? heading)
    {
        Text = text;
        Heading = heading;
    }

    public string Text { get; }
    public string? Heading { get; }
}
