using PDF4LLM.AI.Abstractions;
using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Services;

/// <summary>
/// Phase 2 text chunking with overlap (Kernel Memory–compatible partition sizes).
/// </summary>
public sealed class TextChunkingService : IChunkingService
{
    private readonly int _maxChars;
    private readonly int _overlapChars;

    /// <param name="maxChars">Maximum characters per chunk.</param>
    /// <param name="overlapChars">Character overlap between consecutive chunks.</param>
    public TextChunkingService(int maxChars = 1000, int overlapChars = 100)
    {
        _maxChars = Math.Max(200, maxChars);
        _overlapChars = Math.Clamp(overlapChars, 0, _maxChars / 2);
    }

    public IReadOnlyList<AiChunk> Chunk(IReadOnlyList<ExtractedPage> pages)
    {
        var chunks = new List<AiChunk>();

        foreach (ExtractedPage page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text))
                continue;

            IReadOnlyList<string> parts = SplitWithOverlap(page.Text, _maxChars, _overlapChars);
            for (int i = 0; i < parts.Count; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0)
                    continue;

                chunks.Add(new AiChunk
                {
                    Id = $"{page.SourceFileName}|p{page.PageNumber}|c{i}",
                    Text = part,
                    SourceFilePath = page.SourceFilePath,
                    SourceFileName = page.SourceFileName,
                    PageNumber = page.PageNumber,
                    ChunkIndex = i,
                    Metadata = page.Metadata,
                });
            }
        }

        return chunks;
    }

    /// <param name="text">Source text to split.</param>
    /// <param name="maxChars">Maximum characters per part.</param>
    /// <param name="overlapChars">Character overlap between consecutive parts.</param>
    public static IReadOnlyList<string> SplitWithOverlap(string text, int maxChars, int overlapChars)
    {
        if (text.Length <= maxChars)
            return new[] { text };

        var parts = new List<string>();
        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(maxChars, text.Length - start);
            int end = start + length;

            if (end < text.Length)
            {
                int breakAt = text.LastIndexOf('\n', end - 1, Math.Min(length, 200));
                if (breakAt > start + maxChars / 3)
                    end = breakAt + 1;
            }

            parts.Add(text.Substring(start, end - start).Trim());
            if (end >= text.Length)
                break;

            start = Math.Max(end - overlapChars, start + 1);
        }

        return parts;
    }
}
