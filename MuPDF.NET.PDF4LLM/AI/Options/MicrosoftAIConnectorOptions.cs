using Microsoft.Extensions.AI;
using MuPDF.NET.PDF4LLM.AI.Abstractions;
using MuPDF.NET.PDF4LLM.AI.Services;

namespace MuPDF.NET.PDF4LLM.AI.Options;

/// <summary>Configuration for <see cref="MsAIConnector.LoadAsync"/>.</summary>
public sealed class MicrosoftAIConnectorOptions
{
    /// <summary>Use the MuPDF.NET.PDF4LLM layout pipeline when extracting pages.</summary>
    public bool UseLayout { get; set; }

    /// <summary>Maximum characters per prose chunk. Table rows are kept whole.</summary>
    public int MaxChunkCharacters { get; set; } = 1000;

    /// <summary>Overlap between consecutive prose chunks.</summary>
    public int ChunkOverlapCharacters { get; set; } = 100;

    /// <summary>Default number of chunks <c>SearchAsync</c> returns.</summary>
    public int DefaultTopK { get; set; } = 8;

    /// <summary>
    /// How many ranked excerpts Ask sends. Default is two so a question that
    /// needs two documents can be answered, but only hits above
    /// <see cref="AskMinScore"/> are sent.
    /// </summary>
    public int AskExcerptCount { get; set; } = 2;

    /// <summary>
    /// Ask ignores ranked hits below this hybrid score (lexical + 0.5×vector).
    /// Unrelated passages are not sent to the chat model. Zero disables the floor.
    /// </summary>
    public double AskMinScore { get; set; } = 0.35;

    /// <summary>
    /// Search drops hits below this hybrid score after ranking. Zero (default)
    /// returns the top <see cref="DefaultTopK"/> regardless of score.
    /// </summary>
    public double SearchMinScore { get; set; }

    /// <summary>
    /// Maximum characters of excerpts on Ask. Zero means no cap. Extra ranked
    /// hits are not added just to fill this budget.
    /// </summary>
    public int AskContextCharacters { get; set; } = 12_000;

    /// <summary>
    /// When true, Ask sends every indexed chunk (optionally one file) in
    /// document order. Ranking, <see cref="AskExcerptCount"/>,
    /// <see cref="AskMinScore"/>, and <see cref="AskContextCharacters"/> are
    /// not applied. Override per call with <c>AskDetailedAsync(..., allContext: true)</c>.
    /// </summary>
    public bool AskAllContext { get; set; }

    /// <summary>PDF extractor. When null, the built-in MuPDF.NET.PDF4LLM page extractor is used.</summary>
    public IDocumentExtractor? DocumentExtractor { get; set; }

    /// <summary>
    /// How many chunk texts are sent in one embedding request. Smaller batches
    /// are safer for local servers; larger batches are faster on hosted APIs.
    /// </summary>
    public int EmbedBatchSize { get; set; } = EmbeddingIndexer.DefaultBatchSize;

    /// <summary>
    /// When true, Ask embeds the question so ranking can use vector similarity
    /// as well as word overlap. Set false when the chat and embedding models
    /// cannot stay loaded together (typical of some local servers).
    /// </summary>
    public bool EmbedAskQuery { get; set; } = true;

    /// <summary>
    /// Maximum tokens the chat model may generate for Ask and Summarize.
    /// Zero leaves the model's default.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 1024;

    /// <summary>Embedding generator. Required.</summary>
    public IEmbeddingGenerator<string, Embedding<float>>? EmbeddingGenerator { get; set; }

    /// <summary>Chat client for <c>AskAsync</c> and <c>SummarizeAsync</c>.</summary>
    public IChatClient? ChatClient { get; set; }

    /// <summary>Vector index. When null, an in-memory index or <see cref="LocalIndexPath"/> is used.</summary>
    public IVectorIndex? VectorIndex { get; set; }

    /// <summary>
    /// JSON file or directory for a local chunk index. Used when
    /// <see cref="VectorIndex"/> is null and Azure Search is not configured.
    /// </summary>
    public string? LocalIndexPath { get; set; }

    /// <summary>Optional trace callback. Do not log excerpt text here.</summary>
    public Action<string>? Trace { get; set; }

    /// <summary>Azure AI Search endpoint. When set with the key and index name, chunks are also uploaded there.</summary>
    public string? AzureSearchEndpoint { get; set; }

    /// <summary>Azure AI Search API key.</summary>
    public string? AzureSearchApiKey { get; set; }

    /// <summary>Azure AI Search index name.</summary>
    public string? AzureSearchIndexName { get; set; }

    /// <summary>
    /// Expected embedding width. Zero means "use the width the model returns".
    /// A non-zero value that disagrees with the model fails the load.
    /// </summary>
    public int EmbeddingDimensions { get; set; }

    /// <summary>Throws if numeric options are out of range.</summary>
    public void Validate()
    {
        if (MaxChunkCharacters < 200)
            throw new ArgumentOutOfRangeException(nameof(MaxChunkCharacters), "Must be at least 200.");
        if (ChunkOverlapCharacters < 0)
            throw new ArgumentOutOfRangeException(nameof(ChunkOverlapCharacters), "Must be zero or greater.");
        if (DefaultTopK < 1)
            throw new ArgumentOutOfRangeException(nameof(DefaultTopK), "Must be at least 1.");
        if (AskExcerptCount < 1)
            throw new ArgumentOutOfRangeException(nameof(AskExcerptCount), "Must be at least 1.");
        if (EmbedBatchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(EmbedBatchSize), "Must be at least 1.");
        if (AskMinScore < 0)
            throw new ArgumentOutOfRangeException(nameof(AskMinScore), "Must be zero or greater.");
        if (SearchMinScore < 0)
            throw new ArgumentOutOfRangeException(nameof(SearchMinScore), "Must be zero or greater.");
        if (MaxOutputTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxOutputTokens), "Must be zero or greater.");
        if (AskContextCharacters < 0)
            throw new ArgumentOutOfRangeException(nameof(AskContextCharacters), "Must be zero or greater.");
        if (EmbeddingDimensions < 0)
            throw new ArgumentOutOfRangeException(nameof(EmbeddingDimensions), "Must be zero or greater.");

        bool azureAny = !string.IsNullOrWhiteSpace(AzureSearchEndpoint)
            || !string.IsNullOrWhiteSpace(AzureSearchApiKey)
            || !string.IsNullOrWhiteSpace(AzureSearchIndexName);
        bool azureAll = !string.IsNullOrWhiteSpace(AzureSearchEndpoint)
            && !string.IsNullOrWhiteSpace(AzureSearchApiKey)
            && !string.IsNullOrWhiteSpace(AzureSearchIndexName);
        if (azureAny && !azureAll)
        {
            throw new InvalidOperationException(
                "Azure AI Search requires AzureSearchEndpoint, AzureSearchApiKey, and AzureSearchIndexName together.");
        }
    }

    /// <summary>
    /// In-memory pipeline for tests. Embeddings are a hash of the text, so they do not
    /// rank by meaning. <c>AskAsync</c> and <c>SearchAsync</c> still match by the words
    /// in the question. Pass <paramref name="chatClient"/> for Ask and Summarize.
    /// </summary>
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
