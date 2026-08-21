# MuPDF.NET.PDF4LLM

LLM/RAG helpers for [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET): convert PDFs to Markdown or plain text, analyze page layout, export structure as JSON, and load documents for retrieval pipelines.

The public API lives in the **`MuPDF.NET.PDF4LLM`** namespace. The main entry point is the static class **`MuPDF4LLM`**.

**API naming:** Public members use C# conventions — PascalCase methods and properties, camelCase parameters (for example `writeImages`, `includeXrefs`, `useLayout`). Python-style names from [pymupdf4llm](https://pypi.org/project/pymupdf4llm/) appear only in internal port alignment and in the optional Python layout worker; customer-facing docs and IntelliSense use the C# names below.

## Documentation

| Resource | URL |
|----------|-----|
| Full documentation | https://docs.pdf4llm.com/ |
| .NET getting started | https://docs.pdf4llm.com/dotnet/getting-started/installation |
| MuPDF.NET API reference | https://mupdfnet.readthedocs.io/ |

## Installation

```bash
dotnet add package MuPDF.NET.PDF4LLM
```

[MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET) is installed automatically as a dependency — you do not need to add it separately. If your project already references MuPDF.NET, add `MuPDF.NET.PDF4LLM` anyway; NuGet will resolve a compatible MuPDF.NET version.

## PyMuPDF Layout (recommended)

AI-based layout analysis uses the Python package [pymupdf-layout](https://pypi.org/project/pymupdf-layout/) through a small external worker process. When layout is available, `MuPDF4LLM` enables it automatically on first use.

### One-time setup (NuGet consumers)

Requires **Python 3.10+** on `PATH`. From your project directory:

```bash
dotnet msbuild -t:MuPDFNetPDF4LLMSetupLayoutPython
```

This creates a per-user venv and installs pinned `pymupdf` / `pymupdf-layout` wheels:

| OS | Venv location |
|----|---------------|
| Windows | `%LOCALAPPDATA%\MuPDF.NET.PDF4LLM\.venv-layout` |
| Linux / macOS | `~/.local/share/mupdf4llm.net/.venv-layout` |

MuPDF.NET.PDF4LLM discovers that venv automatically. No environment variables are required.

### Alternatives

- Set **`MuPDF4LLM_NET_PYTHON`** to any Python interpreter that has `pymupdf-layout` installed.
- Project-local venv (also auto-discovered):

  ```bash
  python path/to/setup_layout_python.py --venv .mupdf4llm-net-venv
  ```

- Monorepo / source checkout:

  ```bash
  python MuPDF.NET.PDF4LLM/scripts/setup_layout_python.py
  ```

If layout is not installed, MuPDF.NET.PDF4LLM falls back to classic MuPDF text extraction. Check availability at runtime:

```csharp
using MuPDF.NET.PDF4LLM;
using MuPDF.NET.PDF4LLM.Layout;

bool layoutReady = PyMuPdfLayout.IsAvailable;      // Python import probe
bool layoutActive = MuPDF4LLM.LayoutAvailable;  // provider registered
```

## Quick start

```csharp
using MuPDF.NET;
using MuPDF.NET.PDF4LLM;

// Path or open Document — both work
string markdown = MuPDF4LLM.ToMarkdown(@"C:\docs\report.pdf");

using Document doc = new Document("report.pdf");
string text  = MuPDF4LLM.ToText(doc);
string json  = MuPDF4LLM.ToJson(doc);
var parsed   = MuPDF4LLM.ParseDocument(doc);
var formData = MuPDF4LLM.GetKeyValues(doc);
```

### Selected pages and images

```csharp
string md = MuPDF4LLM.ToMarkdown(
    doc,
    pages: new List<int> { 0, 1, 2 },
    writeImages: true,
    imagePath: @"C:\output\images",
    imageFormat: "png");
```

### Interactive form fields

```csharp
// includeXrefs: true adds each widget's PDF xref (for Page.LoadWidget)
var fields = MuPDF4LLM.GetKeyValues(doc, includeXrefs: true);
foreach (var kv in fields)
    Console.WriteLine($"{kv.Key}: {kv.Value["value"]}");
```

### LlamaIndex-style loading

```csharp
var reader = MuPDF4LLM.LlamaMarkdownReader();
var docs = reader.LoadData("report.pdf", extraInfo: new Dictionary<string, object>());
foreach (var d in docs)
    Console.WriteLine($"Page {d.ExtraInfo["page"]}: {d.Text}");
```

### Markdown to PDF

```csharp
using Document pdf = MuPDF4LLM.MarkdownToPdf(@"C:\docs\readme.md");
pdf.Save("readme.pdf");
```

### Layout on / off

```csharp
MuPDF4LLM.SetUseLayout(useLayout: true);   // default when pymupdf-layout is installed
MuPDF4LLM.SetUseLayout(useLayout: false);  // legacy header detection (IdentifyHeaders, TocHeaders)
```

### Optional Office / HWP support

`MuPDF.NET.PDF4LLM` does not bundle commercial Office natives. Install the sibling package
[MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office), unlock once,
then pass Office/HWP paths to the same extractors:

```csharp
using MuPDF.NET.Office;
using MuPDF.NET.PDF4LLM;

MuPDFOffice.Unlock("YOUR-LICENSE-OR-TRIAL-KEY", fontPathAuto: true);

string markdown = MuPDF4LLM.ToMarkdown(@"C:\docs\report.docx");
string json     = MuPDF4LLM.ToJson(@"C:\docs\report.hwpx");
string text     = MuPDF4LLM.ToText(@"C:\docs\report.pptx");
```

Supported Office formats follow MuPDF.NET.Office (DOC/DOCX, XLS/XLSX, PPT/PPTX,
HWP/HWPX, and related SmartOffice inputs). Layout mode still works: MuPDF.NET.PDF4LLM
snapshots non-PDF pages to a temporary PDF for the external layout worker while
keeping the original document for extraction metadata.

### OCR

When layout mode is active, OCR is selected automatically via `LayoutParseHelpers.SelectOcrFunction()` when Tesseract or RapidOCR is available. Control behavior with `useOcr`, `forceOcr`, `ocrLanguage`, and optional `ocrFunction`:

```csharp
using MuPDF.NET.PDF4LLM.Ocr;

string md = MuPDF4LLM.ToMarkdown(
    doc,
    useOcr: true,
    forceOcr: false,
    ocrLanguage: "eng");
```

`OcrMode` values (layout pipeline): `Never`, `SelectDropOld`, `SelectKeepOld` (default), `ForceDropOld`, `ForceKeepOld`.

## API overview

| Member | Description |
|--------|-------------|
| `MuPDF4LLM.ToMarkdown()` | Document → Markdown (tables, headers, images) |
| `MuPDF4LLM.ToText()` | Document → plain text with the same layout pipeline |
| `MuPDF4LLM.ToJson()` | Layout structure as JSON |
| `MuPDF4LLM.ParseDocument()` | `ParsedDocument` with pages, boxes, tables, images |
| `MuPDF4LLM.GetKeyValues()` | Interactive form field names, values, and locations |
| `MuPDF4LLM.MarkdownToPdf()` | Markdown file → `Document` via MuPDF Story |
| `MuPDF4LLM.LlamaMarkdownReader()` | LlamaIndex-compatible page loader |
| `MuPDF4LLM.SetUseLayout()` | Enable or disable the layout pipeline |
| `MuPDF4LLM.SetLayoutProvider()` | Plug in a custom `Func<Page, object>` layout source |
| `MuPDF4LLM.LoadAiAsync()` | **net8.0 only** — chunk, embed, and index PDFs for RAG (`MuPDF.NET.PDF4LLM.AI`) |

Lower-level layout control: **`MuPDF.NET.PDF4LLM.Layout.PyMuPdfLayout`** (`Activate`, `Deactivate`, `IsAvailable`, `Version`).

Additional public helpers: `LayoutParseHelpers.ReadPageLayoutRaw`, `LayoutParseHelpers.SelectOcrFunction`, `GetTextLines.GetRawLines`, `Utils.Iou`, `Utils.TableToMarkdown`.

## Common options

`MuPDF4LLM.ToMarkdown`, `MuPDF4LLM.ToText`, and `MuPDF4LLM.ToJson` accept optional parameters including:

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

`MuPDF4LLM.GetKeyValues` also accepts `includeXrefs` to include widget xref numbers in the result.

## Requirements

- **.NET:** netstandard2.0, net461, net472, net48, net5.0–net8.0
- **MuPDF.NET:** 3.28.0 or newer (MuPDF bind **1.28.0** must match `MuPDF4LLM` at runtime)
- **Layout (optional):** Python 3.10+ with [pymupdf-layout](https://pypi.org/project/pymupdf-layout/) 1.28.0
- **AI/RAG helpers:** net8.0 + `Microsoft.Extensions.AI` (included in the net8.0 package build)

## License

MuPDF.NET.PDF4LLM is part of MuPDF.NET and is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. For commercial licensing, [contact Artifex](https://artifex.com/contact/mupdf-net-inquiry.php).
