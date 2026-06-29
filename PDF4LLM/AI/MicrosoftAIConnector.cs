using Microsoft.Extensions.AI;
using PDF4LLM.AI.Abstractions;
using PDF4LLM.AI.Models;
using PDF4LLM.AI.Options;
using PDF4LLM.AI.Services;

namespace PDF4LLM.AI;

/// <summary>
/// Unified connector linking PDF4LLM extraction with Microsoft's AI ecosystem
/// (Kernel Memory chunking, Microsoft.Extensions.AI embeddings, Azure AI Search, Azure OpenAI).
/// </summary>
public static class MsAIConnector
{
    /// <summary>
    /// Extract, chunk, embed, and index one or more PDFs.
    /// Returns a collection supporting <see cref="AiDocumentCollection.AskAsync"/>,
    /// <see cref="AiDocumentCollection.SummarizeAsync"/>, and <see cref="AiDocumentCollection.SearchAsync"/>.
    /// </summary>
    /// <param name="pdfPaths">Paths to PDF files (e.g. national-capitals.pdf, nato-members.pdf).</param>
    /// <param name="options">Pipeline configuration. When null, uses in-memory development defaults.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

        IPdfExtractor extractor = options.PdfExtractor
            ?? new Pdf4LlmExtractor(options.UseLayout);

        IChunkingService chunking = options.ChunkingService
            ?? new TextChunkingService(options.MaxChunkCharacters, options.ChunkOverlapCharacters);

        IEmbeddingGenerator<string, Embedding<float>> embeddings = options.EmbeddingGenerator
            ?? throw new InvalidOperationException(
                "EmbeddingGenerator is required. Configure Azure OpenAI or use CreateForDevelopment().");

        IVectorIndex vectorIndex = ResolveVectorIndex(options);

        IRagCompletionService? completion = options.ChatClient != null
            ? new ExtensionsAiRagCompletionService(options.ChatClient)
            : null;

        var allPages = new List<ExtractedPage>();
        foreach (string path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string fullPath = Path.GetFullPath(path);
            IReadOnlyList<ExtractedPage> pages = await extractor.ExtractAsync(fullPath, cancellationToken)
                .ConfigureAwait(false);
            allPages.AddRange(pages);
        }

        IReadOnlyList<AiChunk> chunks = chunking.Chunk(allPages);
        var mutableChunks = chunks.ToList();
        await EmbeddingIndexer.EmbedChunksAsync(mutableChunks, embeddings, cancellationToken).ConfigureAwait(false);
        await vectorIndex.IndexAsync(mutableChunks, cancellationToken).ConfigureAwait(false);

        return new AiDocumentCollection(
            vectorIndex,
            embeddings,
            completion,
            mutableChunks,
            options.DefaultTopK);
    }

    private static IVectorIndex ResolveVectorIndex(MicrosoftAIConnectorOptions options)
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
                options.EmbeddingDimensions);
        }

        return new InMemoryVectorIndex();
    }
}
