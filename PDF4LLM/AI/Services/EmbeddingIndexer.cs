using Microsoft.Extensions.AI;
using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Services;

/// <summary>Generates embeddings for chunks via Microsoft.Extensions.AI.</summary>
internal static class EmbeddingIndexer
{
    public static async Task EmbedChunksAsync(
        IReadOnlyList<AiChunk> chunks,
        IEmbeddingGenerator<string, Embedding<float>> generator,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
            return;

        GeneratedEmbeddings<Embedding<float>> embeddings = await generator.GenerateAsync(
            chunks.Select(c => c.Text),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < chunks.Count; i++)
            chunks[i].Embedding = embeddings[i].Vector;
    }

    public static async Task<ReadOnlyMemory<float>> EmbedQueryAsync(
        string query,
        IEmbeddingGenerator<string, Embedding<float>> generator,
        CancellationToken cancellationToken = default)
    {
        GeneratedEmbeddings<Embedding<float>> result = await generator.GenerateAsync(
            new[] { query },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return result[0].Vector;
    }
}
