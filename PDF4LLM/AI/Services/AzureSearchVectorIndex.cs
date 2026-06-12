using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using PDF4LLM.AI.Abstractions;
using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Services;

/// <summary>Phase 2 vector indexing via Azure AI Search.</summary>
public sealed class AzureSearchVectorIndex : IVectorIndex
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    private readonly string _indexName;
    private readonly int _vectorDimensions;

    public AzureSearchVectorIndex(
        Uri endpoint,
        string apiKey,
        string indexName,
        int vectorDimensions = 1536)
    {
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key is required.", nameof(apiKey));
        if (string.IsNullOrWhiteSpace(indexName))
            throw new ArgumentException("Index name is required.", nameof(indexName));

        _indexName = indexName;
        _vectorDimensions = vectorDimensions;
        var credential = new AzureKeyCredential(apiKey);
        _indexClient = new SearchIndexClient(endpoint, credential);
        _searchClient = _indexClient.GetSearchClient(indexName);
    }

    public async Task IndexAsync(IReadOnlyList<AiChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
            return;

        await EnsureIndexAsync(cancellationToken).ConfigureAwait(false);

        var documents = chunks.Select(c => new SearchDocument
        {
            ["id"] = SanitizeKey(c.Id),
            ["text"] = c.Text,
            ["sourceFilePath"] = c.SourceFilePath,
            ["sourceFileName"] = c.SourceFileName,
            ["pageNumber"] = c.PageNumber,
            ["chunkIndex"] = c.ChunkIndex,
            ["contentVector"] = c.Embedding.ToArray(),
        }).ToList();

        IndexDocumentsBatch<SearchDocument> batch = IndexDocumentsBatch.Upload(documents);
        await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK = 5,
        string? sourceFileName = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureIndexAsync(cancellationToken).ConfigureAwait(false);

        var options = new SearchOptions
        {
            Size = Math.Max(1, topK),
            Select = { "id", "text", "sourceFilePath", "sourceFileName", "pageNumber", "chunkIndex" },
        };

        if (!string.IsNullOrWhiteSpace(sourceFileName))
            options.Filter = $"sourceFileName eq '{EscapeFilter(sourceFileName)}'";

        var vectorQuery = new VectorizedQuery(queryEmbedding.ToArray())
        {
            KNearestNeighborsCount = Math.Max(1, topK),
            Fields = { "contentVector" },
        };
        options.VectorSearch = new VectorSearchOptions();
        options.VectorSearch.Queries.Add(vectorQuery);

        SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(
            null,
            options,
            cancellationToken).ConfigureAwait(false);

        var results = new List<SearchResult>();
        await foreach (SearchResult<SearchDocument> hit in response.GetResultsAsync())
        {
            SearchDocument doc = hit.Document;
            results.Add(new Models.SearchResult
            {
                Score = hit.Score ?? 0,
                Chunk = new AiChunk
                {
                    Id = doc["id"]?.ToString() ?? "",
                    Text = doc["text"]?.ToString() ?? "",
                    SourceFilePath = doc["sourceFilePath"]?.ToString() ?? "",
                    SourceFileName = doc["sourceFileName"]?.ToString() ?? "",
                    PageNumber = doc.TryGetValue("pageNumber", out object? pn) ? Convert.ToInt32(pn) : 0,
                    ChunkIndex = doc.TryGetValue("chunkIndex", out object? ci) ? Convert.ToInt32(ci) : 0,
                },
            });
        }

        return results;
    }

    private async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _indexClient.GetIndexAsync(_indexName, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            var fields = new List<SearchField>
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("text") { IsFilterable = false },
                new SimpleField("sourceFilePath", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("sourceFileName", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("pageNumber", SearchFieldDataType.Int32) { IsFilterable = true },
                new SimpleField("chunkIndex", SearchFieldDataType.Int32) { IsFilterable = true },
                new VectorSearchField("contentVector", _vectorDimensions, "default-vector-profile"),
            };

            var index = new SearchIndex(_indexName)
            {
                Fields = fields,
                VectorSearch = new VectorSearch
                {
                    Profiles =
                    {
                        new VectorSearchProfile("default-vector-profile", "default-algorithm"),
                    },
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration("default-algorithm"),
                    },
                },
            };

            await _indexClient.CreateIndexAsync(index, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string SanitizeKey(string id) =>
        id.Replace('|', '_').Replace(' ', '_');

    private static string EscapeFilter(string value) =>
        value.Replace("'", "''");
}
