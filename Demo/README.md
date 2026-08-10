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
dotnet run -- rag-markdown    # one MuPDF.NET.PDF4LLM sample by name
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

Input PDFs and images live in `TestDocuments/Demo/` (and `TestDocuments/Demo/Llm/`). Generated PDFs are written to `TestDocuments/Demo/_Output/` (gitignored).
