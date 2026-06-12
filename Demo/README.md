# MuPDF.NET Demo

Runnable samples for **MuPDF.NET** and **PDF4LLM**, grouped by API area under `Samples/`.

## Run

From this directory:

```bash
dotnet run                    # all samples in SampleMenu.cs (default, includes [diag])
dotnet run -- user            # user-facing samples only (skips [diag])
dotnet run -- diagnostics     # [diag] samples only
dotnet run -- help            # list all samples
dotnet run -- hello-new-pdf   # one sample by name
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
| `Samples/Llm/` | PDF4LLM markdown, tables, AI connector |
| `Samples/Regression/` | Issue repros (diagnostics only) |

Input PDFs and images live in `TestDocuments/Demo/`. Generated PDFs are written to `TestDocuments/Demo/_Output/` (gitignored).

## PDF4LLM AI sample

The `ai-connector` sample uses `PdfExtractor.LoadAiAsync`. Set `AZURE_OPENAI_ENDPOINT` and related variables for Azure OpenAI, or run without them to use the in-memory demo pipeline.
