# MuPDF.NET AI / RAG

RAG helpers on **net8.0**: extract PDFs with **MuPDF.NET**, chunk, embed, then Ask / Search / Summarize through [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai).

Chat and embedding models are injected (`IChatClient`, `IEmbeddingGenerator`). Local [Ollama](https://ollama.com) is one host; Azure OpenAI is another. This library does not bundle model weights.

New code uses `using MuPDF.NET` and `Utils.LoadAiAsync`. The `MuPDF.NET.PDF4LLM` package is **deprecated** and will be removed; do not add it.

## Load

```csharp
using MuPDF.NET;

var options = Utils.CreateAiOptions(
    chatClient: chat,                 // e.g. OllamaSharp OllamaApiClient
    embeddingGenerator: embed,        // a model that can embed (not the chat model)
    useLayout: true,
    askExcerptCount: 2,
    askMinScore: 0.35);

var docs = await Utils.LoadAiAsync(pdfPaths, options);
var answer = await docs.AskDetailedAsync(question);
string pageSummary = await docs.SummarizeAsync("national-capitals.pdf", pageNumber: 5);
```

In-memory / test pipeline: `Utils.CreateDevelopmentAiOptions(chatClient)`. Azure: `Utils.CreateAzureAiOptions(...)`.

`options.Validate()` runs at load. `AddDocumentsAsync` / `RemoveDocumentAsync` update the same collection.

## Storage

| Option | When to use |
|--------|-------------|
| In-memory (default) | Tests and one-shot sessions |
| `LocalIndexPath` | JSON file of chunks + embeddings; no extra database |
| Azure AI Search | Set endpoint, key, and index name together |
| `IVectorIndex` | Your own store |

Do not log excerpt text. Set `Trace` for counts and file names only.

## Ollama (typical local setup)

```text
ollama pull mistral
ollama pull nomic-embed-text
```

| Setting | Suggested local value | Why |
|---------|----------------------|-----|
| Chat | `mistral` | 8k context; set `OLLAMA_MODEL` to override |
| Embed | `nomic-embed-text` | Chat models often cannot embed |
| `EmbedAskQuery` | `false` | Avoid swapping embed/chat on one GPU |
| `EmbedBatchSize` | `4` | Smaller batches on local HTTP |
| `MaxOutputTokens` | `512` | Short factual answers |
| HTTP timeout | several minutes | Indexing and chat can be slow |

Demo: `dotnet run --project Demo -- ollama` (national-capitals.pdf) or `dotnet run --project Demo -- ollama-magazines` (Magazines-PDF.pdf). Environment: `OLLAMA_HOST`, `OLLAMA_MODEL`, `OLLAMA_EMBED_MODEL`, optional `MUPDF_NET_INDEX` for `LocalIndexPath` (`PDF4LLM_INDEX` is a deprecated alias).

Standard unit tests do not start Ollama.
