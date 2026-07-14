# PDF4LLM

LLM/RAG helpers for [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET): convert PDFs to Markdown or plain text, analyze page layout, export structure as JSON, and load documents for retrieval pipelines.

The public API lives in the **`PDF4LLM`** namespace. The main entry point is the static class **`PdfExtractor`**.

**API naming:** Public members use C# conventions — PascalCase methods and properties, camelCase parameters (for example `writeImages`, `includeXrefs`, `useLayout`). Python-style names from [pymupdf4llm](https://pypi.org/project/pymupdf4llm/) appear only in internal port alignment and in the optional Python layout worker; customer-facing docs and IntelliSense use the C# names below.

## Installation

```bash
dotnet add package PDF4LLM
```

[MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET) is installed automatically as a dependency — you do not need to add it separately. If your project already references MuPDF.NET, add `PDF4LLM` anyway; NuGet will resolve a compatible MuPDF.NET version.

## PyMuPDF Layout (recommended)

AI-based layout analysis uses the Python package [pymupdf-layout](https://pypi.org/project/pymupdf-layout/) through a small external worker process. When layout is available, `PdfExtractor` enables it automatically on first use.

### One-time setup (NuGet consumers)

Requires **Python 3.10+** on `PATH`. From your project directory:

```bash
dotnet msbuild -t:PDF4LLMSetupLayoutPython
```

This creates a per-user venv and installs pinned `pymupdf` / `pymupdf-layout` wheels:

| OS | Venv location |
|----|---------------|
| Windows | `%LOCALAPPDATA%\PDF4LLM\.venv-layout` |
| Linux / macOS | `~/.local/share/pdf4llm/.venv-layout` |

PDF4LLM discovers that venv automatically. No environment variables are required.

### Alternatives

- Set **`PDF4LLM_PYTHON`** to any Python interpreter that has `pymupdf-layout` installed.
- Project-local venv (also auto-discovered):

  ```bash
  python path/to/setup_layout_python.py --venv .pdf4llm-venv
  ```

- Monorepo / source checkout:

  ```bash
  python PDF4LLM/scripts/setup_layout_python.py
  ```

If layout is not installed, PDF4LLM falls back to classic MuPDF text extraction. Check availability at runtime:

```csharp
using PDF4LLM;
using PDF4LLM.Layout;

bool layoutReady = PyMuPdfLayout.IsAvailable;      // Python import probe
bool layoutActive = PdfExtractor.LayoutAvailable;  // provider registered
```

## Quick start

```csharp
using MuPDF.NET;
using PDF4LLM;

// Path or open Document — both work
string markdown = PdfExtractor.ToMarkdown(@"C:\docs\report.pdf");

using Document doc = new Document("report.pdf");
string text  = PdfExtractor.ToText(doc);
string json  = PdfExtractor.ToJson(doc);
var parsed   = PdfExtractor.ParseDocument(doc);
var formData = PdfExtractor.GetKeyValues(doc);
```

### Selected pages and images

```csharp
string md = PdfExtractor.ToMarkdown(
    doc,
    pages: new List<int> { 0, 1, 2 },
    writeImages: true,
    imagePath: @"C:\output\images",
    imageFormat: "png");
```

### Interactive form fields

```csharp
// includeXrefs: true adds each widget's PDF xref (for Page.LoadWidget)
var fields = PdfExtractor.GetKeyValues(doc, includeXrefs: true);
foreach (var kv in fields)
    Console.WriteLine($"{kv.Key}: {kv.Value["value"]}");
```

### LlamaIndex-style loading

```csharp
var reader = PdfExtractor.LlamaMarkdownReader();
var docs = reader.LoadData("report.pdf", extraInfo: new Dictionary<string, object>());
foreach (var d in docs)
    Console.WriteLine($"Page {d.ExtraInfo["page"]}: {d.Text}");
```

### Markdown to PDF

```csharp
using Document pdf = PdfExtractor.MarkdownToPdf(@"C:\docs\readme.md");
pdf.Save("readme.pdf");
```

### Layout on / off

```csharp
PdfExtractor.SetUseLayout(useLayout: true);   // default when pymupdf-layout is installed
PdfExtractor.SetUseLayout(useLayout: false);  // legacy header detection (IdentifyHeaders, TocHeaders)
```

### OCR

When layout mode is active, OCR is selected automatically via `LayoutParseHelpers.SelectOcrFunction()` when Tesseract or RapidOCR is available. Control behavior with `useOcr`, `forceOcr`, `ocrLanguage`, and optional `ocrFunction`:

```csharp
using PDF4LLM.Ocr;

string md = PdfExtractor.ToMarkdown(
    doc,
    useOcr: true,
    forceOcr: false,
    ocrLanguage: "eng");
```

`OcrMode` values (layout pipeline): `Never`, `SelectDropOld`, `SelectKeepOld` (default), `ForceDropOld`, `ForceKeepOld`.

## API overview

| Member | Description |
|--------|-------------|
| `ToMarkdown()` | Document → Markdown (tables, headers, images) |
| `ToText()` | Document → plain text with the same layout pipeline |
| `ToJson()` | Layout structure as JSON |
| `ParseDocument()` | `ParsedDocument` with pages, boxes, tables, images |
| `GetKeyValues()` | Interactive form field names, values, and locations |
| `MarkdownToPdf()` | Markdown file → `Document` via MuPDF Story |
| `LlamaMarkdownReader()` | LlamaIndex-compatible page loader |
| `SetUseLayout()` | Enable or disable the layout pipeline |
| `SetLayoutProvider()` | Plug in a custom `Func<Page, object>` layout source |
| `LoadAiAsync()` | **net8.0 only** — chunk, embed, and index PDFs for RAG (`PDF4LLM.AI`) |

Lower-level layout control: **`PDF4LLM.Layout.PyMuPdfLayout`** (`Activate`, `Deactivate`, `IsAvailable`, `Version`).

Additional public helpers: `LayoutParseHelpers.ReadPageLayoutRaw`, `LayoutParseHelpers.SelectOcrFunction`, `GetTextLines.GetRawLines`, `Utils.Iou`, `Utils.TableToMarkdown`.

## Common options

`ToMarkdown`, `ToText`, and `ToJson` accept optional parameters including:

| Parameter | Purpose |
|-----------|---------|
| `pages` | Restrict to specific pages (0-based) |
| `writeImages` / `embedImages` | Save image files or embed as base64 |
| `imagePath`, `imageFormat`, `filename` | Image output location and naming |
| `useOcr`, `ocrLanguage`, `forceOcr`, `ocrFunction` | OCR for scanned pages |
| `forceText` | Extract text even from picture regions (layout mode) |
| `pageChunks`, `pageSeparators` | Chunked or separated page output |
| `showProgress` | Log processing progress |
| `header`, `footer` | Include page header/footer text (layout mode) |

`GetKeyValues` also accepts `includeXrefs` to include widget xref numbers in the result.

## Requirements

- **.NET:** netstandard2.0, net461, net472, net48, net5.0–net8.0
- **MuPDF.NET:** 3.28.0 or newer (MuPDF bind **1.28.0** must match `PdfExtractor` at runtime)
- **Layout (optional):** Python 3.10+ with [pymupdf-layout](https://pypi.org/project/pymupdf-layout/) 1.28.0
- **AI/RAG helpers:** net8.0 + `Microsoft.Extensions.AI` (included in the net8.0 package build)

## License

PDF4LLM is part of MuPDF.NET and is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. For commercial licensing, [contact Artifex](https://artifex.com/contact/mupdf-net-inquiry.php).
