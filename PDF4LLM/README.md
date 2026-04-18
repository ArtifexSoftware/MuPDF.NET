# PDF4LLM

LLM/RAG helpers for [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET): PDF-to-Markdown conversion, layout parsing, document structure analysis. Designed for use with RAG pipelines and integration with LLMs.

## Installation

```bash
dotnet add package PDF4LLM
```

PDF4LLM depends on [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET); it is installed automatically.

## Features

- **PDF-to-Markdown** — Convert PDF pages to Markdown with layout awareness (tables, headers, images)
- **Layout parsing** — Extract document structure (pages, boxes, tables, images) as JSON or structured objects
- **Plain text extraction** — Same layout analysis as Markdown, without syntax
- **LlamaIndex integration** — `PDFMarkdownReader` for compatibility with LlamaIndex document loading
- **OCR support** — Optional OCR for scanned or image-heavy pages
- **Form fields** — Extract key/value pairs from interactive PDF forms

## Quick Start

API types live in the **`PDF4LLM`** namespace (including **`PDF4LLM.Helpers`**, **`PDF4LLM.Llama`**, **`PDF4LLM.Ocr`**). The static entry type is **`PdfExtractor`**. Add `using PDF4LLM;` and call **`PdfExtractor.ToMarkdown(...)`**, **`PdfExtractor.ToJson(...)`**, etc.

### Convert PDF to Markdown

```csharp
using MuPDF.NET;
using PDF4LLM;

Document doc = new Document("document.pdf");
string markdown = PdfExtractor.ToMarkdown(doc);
doc.Close();

// Or pass a path (file is opened only for the call)
string markdown2 = PdfExtractor.ToMarkdown(@"C:\path\to\document.pdf");
```

### Convert to plain text

```csharp
string text = PdfExtractor.ToText(doc);
```

### Get layout as JSON

```csharp
string json = PdfExtractor.ToJson(doc);
```

### Use with LlamaIndex-style loading

```csharp
var reader = PdfExtractor.LlamaMarkdownReader();
var docs = reader.LoadData("document.pdf", extraInfo: new Dictionary<string, object>());
foreach (var d in docs)
{
    Console.WriteLine($"Page {d.ExtraInfo["page"]}: {d.Text}");
}
```

### Extract form field values

```csharp
var keyValues = PdfExtractor.GetKeyValues(doc);
```

## API Overview

| Method | Description |
|--------|-------------|
| `ToMarkdown()` | Convert document (or selected pages) to Markdown with optional images |
| `ToText()` | Convert to plain text using layout analysis |
| `ToJson()` | Export layout structure as JSON |
| `ParseDocument()` | Return a `ParsedDocument` with pages, boxes, tables, images |
| `LlamaMarkdownReader()` | Create a LlamaIndex-compatible PDF reader |
| `GetKeyValues()` | Extract form field name/value pairs and page locations |

## Options

`ToMarkdown`, `ToText`, and `ToJson` support options such as:

- `pages` — Restrict to specific pages (0-based)
- `writeImages` / `embedImages` — Save or embed images
- `imagePath`, `imageFormat` — Where and how to store images
- `useOcr`, `ocrLanguage` — OCR for scanned content
- `showProgress` — Log progress while processing
- `forceText` — Prefer text extraction over image backgrounds

## Requirements

- .NET Standard 2.0 or later (net461, net472, net48, net5.0, net6.0, net7.0, net8.0)
- [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET) 3.2.13 or newer

**Note:** If you see "An assembly with the same simple name 'PDF4LLM' has already been imported", the MuPDF.NET package you have includes PDF4LLM. Use either MuPDF.NET alone (which has 4LLM bundled) or add only PDF4LLM (which brings MuPDF.NET). Do not add both if MuPDF.NET already bundles 4LLM. A future MuPDF.NET release will exclude the bundle so PDF4LLM can be used as a separate package without conflict.

## License

PDF4LLM is part of MuPDF.NET and is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. For commercial use, please [contact Artifex](https://artifex.com/contact/mupdf-net).
