using PDF4LLM.AI.Abstractions;
using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Services;

/// <summary>In-memory cosine-similarity index for development and tests.</summary>
public sealed class InMemoryVectorIndex : IVectorIndex
{
    private readonly List<AiChunk> _chunks = new();
    private readonly object _lock = new();

    public Task IndexAsync(IReadOnlyList<AiChunk> chunks, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
            _chunks.AddRange(chunks);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK = 5,
        string? sourceFileName = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<AiChunk> candidates;
        lock (_lock)
            candidates = _chunks.ToList();

        if (!string.IsNullOrEmpty(sourceFileName))
        {
            candidates = candidates.Where(c =>
                string.Equals(c.SourceFileName, sourceFileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileName(c.SourceFilePath), sourceFileName, StringComparison.OrdinalIgnoreCase));
        }

        var results = candidates
            .Select(c => new SearchResult
            {
                Chunk = c,
                Score = CosineSimilarity(queryEmbedding.Span, c.Embedding.Span),
            })
            .OrderByDescending(r => r.Score)
            .Take(Math.Max(1, topK))
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }

    internal int Count
    {
        get
        {
            lock (_lock)
                return _chunks.Count;
        }
    }

    private static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            return 0;

        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        if (na == 0 || nb == 0)
            return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
