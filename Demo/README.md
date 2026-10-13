# MuPDF.NET Demo

Runnable samples for **MuPDF.NET** and **MuPDF.NET.PDF4LLM**, grouped by API area under `Samples/`.

## Run

From this directory:

```bash
dotnet run                    # all samples in SampleMenu.cs (default, includes [diag])
dotnet run -- user            # user-facing samples only (skips [diag])
dotnet run -- diagnostics     # [diag] samples only
dotnet run -- help            # list all samples
dotnet run -- hello-new-pdf   # one MuPDF.NET sample by name
dotnet run -- rag-markdown       # one MuPDF.NET.PDF4LLM sample by name
dotnet run -- capitals-extract   # ToMarkdown national-capitals.pdf
dotnet run -- ollama             # local Ollama RAG on national-capitals.pdf
dotnet run -- ollama-magazines   # local Ollama RAG on Magazines-PDF.pdf
```

## Layout

| Folder | Topics |
|--------|--------|
| `Samples/Document/` | Open, save, metadata, streams |
| `Samples/TextDrawing/` | Story, TextWriter, text extraction |
| `Samples/Annotations/` | Annotations, free text, redaction |
| `Samples/PageContent/` | Images, recolor, widgets, OCR |
| `Samples/ImageFilters/` | Skia image filters |
| `Samples/Barcodes/` | Barcode read/write |
| `Samples/Regression/` | Issue repros (diagnostics only) |
| `Samples/Llm/` | MuPDF.NET.PDF4LLM: Markdown, tables, OCR, RAG |

Input PDFs and images live in `TestDocuments/Demo/` (and `TestDocuments/Demo/Llm/`). Generated files are written to `TestDocuments/Demo/_Output/` (gitignored). `capitals-extract` writes `national-capitals.md` there.

## Ollama

`ollama` indexes `TestDocuments/Demo/Llm/national-capitals.pdf` with `LoadAiAsync`, then Ask / Search through a local [Ollama](https://ollama.com) `IChatClient`. The canned Ask list covers several pages (South Sudan, Canada, Gibraltar, Tanzania, Vatican City, the United States) plus one question the PDF cannot answer.

`ollama-magazines` does the same for `TestDocuments/Demo/Llm/Magazines-PDF.pdf`, a primer on magazine types, uses, parts, sizes, and how they differ from newspapers, journals, and catalogs. Ask uses facts from that text (definition, ten types, Rolling Stone, masthead, paper, US/UK sizes) plus one question the PDF cannot answer. It is opt-in (`dotnet run -- ollama-magazines`).

The chat model and the embedding model must be separate; `mistral` cannot embed.

```powershell
ollama pull mistral
ollama pull nomic-embed-text
dotnet run -- ollama
dotnet run -- ollama-magazines
```

| Variable | Default | Meaning |
|--|--|--|
| `OLLAMA_HOST` | `http://localhost:11434` | Ollama API base URL |
| `OLLAMA_MODEL` | `mistral` | Chat model (8k context) |
| `OLLAMA_EMBED_MODEL` | `nomic-embed-text` | Embedding model |
| `PDF4LLM_INDEX` | (none) | Optional JSON path for a reusable local index |

Ask sends at most two excerpts that clear a relevance floor, validates citations, and can abstain. See `MuPDF.NET.PDF4LLM/AI/README.md`.
