using Microsoft.Extensions.AI;
using PDF4LLM.AI.Abstractions;
using PDF4LLM.AI.Services;

namespace PDF4LLM.AI.Options;

/// <summary>Configuration for <see cref="MsAIConnector"/>.</summary>
public sealed class MicrosoftAIConnectorOptions
{
    /// <summary>Use PDF4LLM layout pipeline when a layout provider is registered.</summary>
    public bool UseLayout { get; set; }

    /// <summary>Maximum characters per RAG chunk (Kernel Memory text partition size).</summary>
    public int MaxChunkCharacters { get; set; } = 1000;

    /// <summary>Overlap between consecutive chunks.</summary>
    public int ChunkOverlapCharacters { get; set; } = 100;

    /// <summary>Default number of chunks retrieved for <c>AskAsync</c>.</summary>
    public int DefaultTopK { get; set; } = 8;

    /// <summary>Optional custom PDF extractor (defaults to <see cref="Pdf4LlmExtractor"/>).</summary>
    public IPdfExtractor? PdfExtractor { get; set; }

    /// <summary>Optional custom chunking service (defaults to overlap text partitioning).</summary>
    public IChunkingService? ChunkingService { get; set; }

    /// <summary>Embedding generator (Microsoft.Extensions.AI). Required for indexing and search.</summary>
    public IEmbeddingGenerator<string, Embedding<float>>? EmbeddingGenerator { get; set; }

    /// <summary>Chat client for Ask/Summarize (Microsoft.Extensions.AI). Required for reasoning operations.</summary>
    public IChatClient? ChatClient { get; set; }

    /// <summary>Vector index. When null, an in-memory index is used.</summary>
    public IVectorIndex? VectorIndex { get; set; }

    /// <summary>Azure AI Search endpoint (optional — builds <see cref="AzureSearchVectorIndex"/>).</summary>
    public string? AzureSearchEndpoint { get; set; }

    /// <summary>Azure AI Search API key.</summary>
    public string? AzureSearchApiKey { get; set; }

    /// <summary>Azure AI Search index name.</summary>
    public string? AzureSearchIndexName { get; set; }

    /// <summary>Embedding vector dimensions (must match the embedding model).</summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// In-memory pipeline for development and unit tests (no Azure credentials required).
    /// Uses a deterministic hash-based embedding generator; Ask/Summarize require <see cref="ChatClient"/>.
    /// </summary>
    /// <param name="chatClient">Optional chat client for Ask/Summarize operations.</param>
    /// <param name="embeddingDimensions">Vector size for the hash-based development embedding generator.</param>
    public static MicrosoftAIConnectorOptions CreateForDevelopment(
        IChatClient? chatClient = null,
        int embeddingDimensions = 128)
    {
        return new MicrosoftAIConnectorOptions
        {
            EmbeddingGenerator = new HashEmbeddingGenerator(embeddingDimensions),
            ChatClient = chatClient,
            VectorIndex = new InMemoryVectorIndex(),
            EmbeddingDimensions = embeddingDimensions,
        };
    }
}
