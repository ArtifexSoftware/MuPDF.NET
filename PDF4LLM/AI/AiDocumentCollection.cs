using Microsoft.Extensions.AI;
using PDF4LLM.AI.Abstractions;
using PDF4LLM.AI.Models;
using PDF4LLM.AI.Services;

namespace PDF4LLM.AI;

/// <summary>
/// Indexed PDF corpus returned by <see cref="MsAIConnector.LoadAsync"/>.
/// Supports multi-PDF Q&amp;A, summarization, and semantic search.
/// </summary>
public sealed class AiDocumentCollection
{
    private readonly IVectorIndex _vectorIndex;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IRagCompletionService? _completionService;
    private readonly IReadOnlyList<AiChunk> _allChunks;
    private readonly int _defaultTopK;

    internal AiDocumentCollection(
        IVectorIndex vectorIndex,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IRagCompletionService? completionService,
        IReadOnlyList<AiChunk> allChunks,
        int defaultTopK)
    {
        _vectorIndex = vectorIndex;
        _embeddingGenerator = embeddingGenerator;
        _completionService = completionService;
        _allChunks = allChunks;
        _defaultTopK = defaultTopK;
    }

    /// <summary>Number of indexed chunks across all loaded PDFs.</summary>
    public int ChunkCount => _allChunks.Count;

    /// <summary>Source file names that were indexed.</summary>
    public IReadOnlyList<string> SourceFiles =>
        _allChunks.Select(c => c.SourceFileName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>
    /// Phase 3: retrieve relevant chunks and ask GPT-4o (or configured chat model) for an answer.
    /// </summary>
    /// <param name="question">Natural-language question to answer from indexed PDF content.</param>
    /// <param name="topK">Maximum number of chunks to retrieve; uses the connector default when <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<string> AskAsync(
        string question,
        int? topK = null,
        CancellationToken cancellationToken = default)
    {
        if (_completionService == null)
            throw new InvalidOperationException(
                "AskAsync requires a chat client. Set MsAIConnectorOptions.ChatClient or configure Azure OpenAI.");

        IReadOnlyList<SearchResult> hits = await SearchAsync(question, topK, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return await _completionService.AskAsync(question, hits, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Phase 3: summarize one of the loaded documents by file name or path.</summary>
    /// <param name="filePathOrName">Source file path or file name matching an indexed PDF.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<string> SummarizeAsync(
        string filePathOrName,
        CancellationToken cancellationToken = default)
    {
        if (_completionService == null)
            throw new InvalidOperationException(
                "SummarizeAsync requires a chat client. Set MsAIConnectorOptions.ChatClient or configure Azure OpenAI.");

        string name = Path.GetFileName(filePathOrName);
        var docChunks = _allChunks
            .Where(c =>
                string.Equals(c.SourceFileName, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.SourceFilePath, filePathOrName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return await _completionService.SummarizeAsync(name, docChunks, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Phase 2/3: semantic search — retrieval only, no LLM reasoning.
    /// </summary>
    /// <param name="query">Natural-language search query.</param>
    /// <param name="topK">Maximum number of results; uses the connector default when <see langword="null"/>.</param>
    /// <param name="sourceFileName">Optional file-name filter to restrict results to one PDF.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int? topK = null,
        string? sourceFileName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query is required.", nameof(query));

        ReadOnlyMemory<float> queryVector = await EmbeddingIndexer.EmbedQueryAsync(
            query,
            _embeddingGenerator,
            cancellationToken).ConfigureAwait(false);

        return await _vectorIndex.SearchAsync(
            queryVector,
            topK ?? _defaultTopK,
            sourceFileName,
            cancellationToken).ConfigureAwait(false);
    }
}
