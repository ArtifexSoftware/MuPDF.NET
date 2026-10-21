using Microsoft.Extensions.AI;
using MuPDF.NET.PDF4LLM.AI.Abstractions;
using MuPDF.NET.PDF4LLM.AI.Models;
using MuPDF.NET.PDF4LLM.AI.Options;
using MuPDF.NET.PDF4LLM.AI.Services;

namespace MuPDF.NET.PDF4LLM.AI;

/// <summary>
/// Loads PDFs into an <see cref="AiDocumentCollection"/>: extract, chunk, embed, index.
/// </summary>
public static class MsAIConnector
{
    /// <summary>
    /// Extract, chunk, embed, and index one or more PDFs.
    /// </summary>
    public static async Task<AiDocumentCollection> LoadAsync(
        IEnumerable<string> pdfPaths,
        MicrosoftAIConnectorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (pdfPaths == null)
            throw new ArgumentNullException(nameof(pdfPaths));

        var paths = pdfPaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (paths.Count == 0)
            throw new ArgumentException("At least one PDF path is required.", nameof(pdfPaths));

        options ??= MicrosoftAIConnectorOptions.CreateForDevelopment();
        options.Validate();

        IEmbeddingGenerator<string, Embedding<float>> embeddings = options.EmbeddingGenerator
            ?? throw new InvalidOperationException(
                "EmbeddingGenerator is required. Set MicrosoftAIConnectorOptions.EmbeddingGenerator or use CreateForDevelopment().");

        List<AiChunk> chunks = await PrepareChunksAsync(paths, options, cancellationToken).ConfigureAwait(false);

        int dimensions = chunks.Count == 0 ? options.EmbeddingDimensions : chunks[0].Embedding.Length;
        if (options.EmbeddingDimensions > 0 && chunks.Count > 0 && options.EmbeddingDimensions != dimensions)
        {
            throw new InvalidOperationException(
                $"EmbeddingDimensions is {options.EmbeddingDimensions}, but the embedding model returned {dimensions}. Set EmbeddingDimensions to {dimensions}.");
        }

        IVectorIndex vectorIndex = ResolveVectorIndex(options, dimensions);
        await vectorIndex.IndexAsync(chunks, cancellationToken).ConfigureAwait(false);
        if (vectorIndex is FileVectorIndex fileIndex)
            chunks = fileIndex.Snapshot().ToList();
        options.Trace?.Invoke($"Indexed {chunks.Count} chunks from {paths.Count} file(s).");

        return new AiDocumentCollection(
            vectorIndex,
            embeddings,
            options.ChatClient,
            chunks,
            Math.Max(1, options.DefaultTopK),
            Math.Max(1, options.AskExcerptCount),
            options.AskContextCharacters,
            options.EmbedAskQuery,
            options.MaxOutputTokens,
            options.AskMinScore,
            options.SearchMinScore,
            options);
    }

    internal static async Task<List<AiChunk>> PrepareChunksAsync(
        IEnumerable<string> pdfPaths,
        MicrosoftAIConnectorOptions options,
        CancellationToken cancellationToken)
    {
        var paths = pdfPaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (paths.Count == 0)
            throw new ArgumentException("At least one PDF path is required.", nameof(pdfPaths));

        IEmbeddingGenerator<string, Embedding<float>> embeddings = options.EmbeddingGenerator
            ?? throw new InvalidOperationException("EmbeddingGenerator is required.");

        IDocumentExtractor extractor = options.DocumentExtractor ?? new PageExtractor(options.UseLayout);
        var pages = new List<ExtractedPage>();
        foreach (string path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                pages.AddRange(extractor.Extract(path, cancellationToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException($"Failed to extract '{path}'.", ex);
            }
        }

        List<AiChunk> chunks = TextChunker.Chunk(pages, options.MaxChunkCharacters, options.ChunkOverlapCharacters);
        await EmbeddingIndexer.EmbedAsync(chunks, embeddings, options.EmbedBatchSize, cancellationToken)
            .ConfigureAwait(false);
        return chunks;
    }

    static IVectorIndex ResolveVectorIndex(MicrosoftAIConnectorOptions options, int dimensions)
    {
        if (options.VectorIndex != null)
            return options.VectorIndex;

        if (!string.IsNullOrWhiteSpace(options.AzureSearchEndpoint)
            && !string.IsNullOrWhiteSpace(options.AzureSearchApiKey)
            && !string.IsNullOrWhiteSpace(options.AzureSearchIndexName))
        {
            return new AzureSearchVectorIndex(
                new Uri(options.AzureSearchEndpoint),
                options.AzureSearchApiKey,
                options.AzureSearchIndexName,
                dimensions > 0 ? dimensions : 1536);
        }

        if (!string.IsNullOrWhiteSpace(options.LocalIndexPath))
            return new FileVectorIndex(options.LocalIndexPath);

        return new InMemoryVectorIndex();
    }
}
