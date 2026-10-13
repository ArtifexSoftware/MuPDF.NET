using MuPDF.NET.PDF4LLM.AI.Abstractions;
using MuPDF.NET.PDF4LLM.AI.Models;
using Newtonsoft.Json;

namespace MuPDF.NET.PDF4LLM.AI.Services;

/// <summary>
/// JSON file of chunks and embeddings plus an in-memory search index.
/// Survives process restarts without a vector database.
/// </summary>
public sealed class FileVectorIndex : IVectorIndex
{
    readonly InMemoryVectorIndex _memory = new();
    readonly string _path;
    readonly object _gate = new();

    public FileVectorIndex(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Index path is required.", nameof(path));

        _path = Directory.Exists(path)
            || path.EndsWith(Path.DirectorySeparatorChar)
            || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? Path.Combine(path, "pdf4llm-ai-index.json")
            : path;
        Load();
    }

    public string FilePath => _path;

    public IReadOnlyList<AiChunk> Snapshot() => _memory.Snapshot();

    public async Task IndexAsync(IReadOnlyList<AiChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _memory.IndexAsync(chunks, cancellationToken).ConfigureAwait(false);
        Save();
    }

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK = 5,
        string? sourceFileName = null,
        CancellationToken cancellationToken = default)
        => _memory.SearchAsync(queryEmbedding, topK, sourceFileName, cancellationToken);

    public async Task RemoveBySourceAsync(string sourceFileName, CancellationToken cancellationToken = default)
    {
        await _memory.RemoveBySourceAsync(sourceFileName, cancellationToken).ConfigureAwait(false);
        Save();
    }

    void Load()
    {
        if (!File.Exists(_path))
            return;

        string json = File.ReadAllText(_path);
        StoredFile? stored = JsonConvert.DeserializeObject<StoredFile>(json);
        if (stored?.Chunks == null || stored.Chunks.Count == 0)
            return;

        var chunks = new List<AiChunk>(stored.Chunks.Count);
        foreach (StoredChunk row in stored.Chunks)
        {
            chunks.Add(new AiChunk
            {
                Id = row.Id ?? "",
                Text = row.Text ?? "",
                SourceFilePath = row.SourceFilePath ?? "",
                SourceFileName = row.SourceFileName ?? "",
                PageNumber = row.PageNumber,
                ChunkIndex = row.ChunkIndex,
                Embedding = row.Embedding ?? Array.Empty<float>(),
            });
        }

        _memory.IndexAsync(chunks).GetAwaiter().GetResult();
    }

    void Save()
    {
        lock (_gate)
        {
            string? dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var rows = new List<StoredChunk>();
            foreach (AiChunk chunk in _memory.Snapshot())
            {
                rows.Add(new StoredChunk
                {
                    Id = chunk.Id,
                    Text = chunk.Text,
                    SourceFilePath = chunk.SourceFilePath,
                    SourceFileName = chunk.SourceFileName,
                    PageNumber = chunk.PageNumber,
                    ChunkIndex = chunk.ChunkIndex,
                    Embedding = chunk.Embedding.ToArray(),
                });
            }

            File.WriteAllText(_path, JsonConvert.SerializeObject(new StoredFile { Chunks = rows }));
        }
    }

    sealed class StoredFile
    {
        public List<StoredChunk> Chunks { get; set; } = new();
    }

    sealed class StoredChunk
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public string? SourceFilePath { get; set; }
        public string? SourceFileName { get; set; }
        public int PageNumber { get; set; }
        public int ChunkIndex { get; set; }
        public float[]? Embedding { get; set; }
    }
}
