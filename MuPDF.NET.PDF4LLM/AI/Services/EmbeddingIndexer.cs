using Microsoft.Extensions.AI;
using MuPDF.NET.PDF4LLM.AI.Models;

namespace MuPDF.NET.PDF4LLM.AI.Services;

/// <summary>Embeds chunk text in batches so one request is not the entire corpus.</summary>
internal static class EmbeddingIndexer
{
    public const int DefaultBatchSize = 4;

    public static async Task EmbedAsync(
        IReadOnlyList<AiChunk> chunks,
        IEmbeddingGenerator<string, Embedding<float>> generator,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
            return;

        int size = Math.Max(1, batchSize);
        for (int offset = 0; offset < chunks.Count; offset += size)
        {
            int take = Math.Min(size, chunks.Count - offset);
            await EmbedSliceAsync(chunks, offset, take, generator, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task<ReadOnlyMemory<float>> EmbedQueryAsync(
        string query,
        IEmbeddingGenerator<string, Embedding<float>> generator,
        CancellationToken cancellationToken = default)
    {
        GeneratedEmbeddings<Embedding<float>> result = await GenerateAsync(generator, new[] { query }, cancellationToken)
            .ConfigureAwait(false);
        return result[0].Vector;
    }

    static async Task EmbedSliceAsync(
        IReadOnlyList<AiChunk> chunks,
        int offset,
        int take,
        IEmbeddingGenerator<string, Embedding<float>> generator,
        CancellationToken cancellationToken)
    {
        var batch = new string[take];
        for (int i = 0; i < take; i++)
            batch[i] = chunks[offset + i].Text;

        GeneratedEmbeddings<Embedding<float>> embeddings;
        try
        {
            embeddings = await GenerateAsync(generator, batch, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (take > 1 && TransportRetry.IsReset(ex))
        {
            for (int i = 0; i < take; i++)
                await EmbedSliceAsync(chunks, offset + i, 1, generator, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (embeddings.Count != take)
        {
            throw new InvalidOperationException(
                $"Embedding model returned {embeddings.Count} vectors for {take} chunks.");
        }

        for (int i = 0; i < take; i++)
        {
            ReadOnlyMemory<float> vector = embeddings[i].Vector;
            if (vector.Length == 0)
                throw new InvalidOperationException("Embedding model returned an empty vector.");
            chunks[offset + i].Embedding = vector;
        }
    }

    static Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        IEnumerable<string> values,
        CancellationToken cancellationToken)
    {
        return TransportRetry.RunAsync(
            () => generator.GenerateAsync(values, cancellationToken: cancellationToken),
            cancellationToken);
    }
}
