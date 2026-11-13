# MuPDF.NET.PDF4LLM

**Deprecated.** This package **will be removed**. 2.1 is an empty type-forward stub (same pattern as PyPI [pymupdf4llm 2.1](https://pypi.org/project/pymupdf4llm/)).

**Do not add this package.** Use [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET) **4.1** (`Document.ToMarkdown` / `ToJson` / `ToText` / `ToChunks`, `Utils.UseLayout`, `Utils.LoadAiAsync`) and [MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office) **2.1** for Office/HWP. Until removal, existing `PackageReference Include="MuPDF.NET.PDF4LLM"` lines still type-forward from `MuPDF.NET.dll`.

**API naming:** Public members use C# conventions — PascalCase methods and properties, camelCase parameters (for example `writeImages`, `includeXrefs`, `useLayout`). Python-style names from [pymupdf4llm](https://pypi.org/project/pymupdf4llm/) appear only in internal port alignment and in the optional Python layout worker; customer-facing docs and IntelliSense use the C# names below.

## Documentation

| Resource | URL |
|----------|-----|
| Full documentation | https://docs.pdf4llm.com/ |
| .NET getting started | https://docs.pdf4llm.com/dotnet/getting-started/installation |
| MuPDF.NET API reference | https://mupdfnet.readthedocs.io/ |
| Sample apps (`MuPDF.NET.Examples`) | https://github.com/ArtifexSoftware/MuPDF.NET.Examples |

## Installation

```bash
dotnet add package MuPDF.NET
```

LLM extract is in MuPDF.NET 4.1. Do **not** `dotnet add package MuPDF.NET.PDF4LLM`.

## PyMuPDF Layout

Layout setup ships in **MuPDF.NET** (scripts + `MuPDFNetSetupLayoutPython` MSBuild target). See **[MuPDF.NET README — Layout analysis](../README.md#layout-analysis-optional)**.

Deprecated type-forward names (migrate off them): `PyMuPdfLayout.IsAvailable` / `MuPDF4LLM.LayoutAvailable`. Prefer `Utils.LayoutPythonAvailable` / `Utils.LayoutAvailable`.

## Examples

Runnable console samples live in **[MuPDF.NET.Examples](https://github.com/ArtifexSoftware/MuPDF.NET.Examples)**. LLM/RAG apps are under `MuPDF.NET/` (`32-ToMarkdown` … `42-OllamaMagazines`) and **PackageReference MuPDF.NET**, not this deprecated package. Office + Markdown is `MuPDF.NET.Office/05-ToMarkdown`.

```powershell
git clone https://github.com/ArtifexSoftware/MuPDF.NET.Examples.git
cd MuPDF.NET.Examples
dotnet restore
dotnet run --project MuPDF.NET\32-ToMarkdown
```

## Quick start (new code)

```csharp
using MuPDF.NET;

using Document doc = new Document("report.pdf");
string markdown = doc.ToMarkdown();
string text     = doc.ToText();
string json     = doc.ToJson();
ChunkedDocument chunks = doc.ToChunks();
var formData    = Utils.GetKeyValues(doc);
```

Migrate existing `using MuPDF.NET.PDF4LLM` code to `using MuPDF.NET` (`Utils.GetKeyValues` / `LlamaMarkdownReader` / `MarkdownToPdf` / `LoadAiAsync`). Type-forwards still compile until this package is removed.

### Retrieval chunks (layout mode)

```csharp
ChunkedDocument chunks = doc.ToChunks(maxTokens: 400, minTokens: 120);
foreach (Chunk c in chunks)
    Console.WriteLine($"{c.Id} ({c.Metadata.TokenCount}): {c.Text}");
```

### Selected pages and images

```csharp
string md = doc.ToMarkdown(
    pages: new List<int> { 0, 1, 2 },
    writeImages: true,
    imagePath: @"C:\output\images",
    imageFormat: "png");
```

### Interactive form fields

```csharp
// includeXrefs: true adds each widget's PDF xref (for Page.LoadWidget)
var fields = Utils.GetKeyValues(doc, includeXrefs: true);
foreach (var kv in fields)
    Console.WriteLine($"{kv.Key}: {kv.Value["field_value"]}");
```

### LlamaIndex-style loading

```csharp
var reader = new PDFMarkdownReader();
var docs = reader.LoadData("report.pdf", extraInfo: new Dictionary<string, object>());
foreach (var d in docs)
    Console.WriteLine($"Page {d.ExtraInfo["page"]}: {d.Text}");
```

### Markdown to PDF

```csharp
using Document pdf = Utils.MarkdownToPdf(@"C:\docs\readme.md");
pdf.Save("readme.pdf");
```

### Layout on / off

```csharp
Utils.UseLayout(true);    // default when the pymupdf 2.1 worker is installed
Utils.UseLayout(false);   // classic RAG markdown (IdentifyHeaders, TocHeaders)
```

### Optional Office / HWP support

This stub does not bundle commercial Office natives. Install
[MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office), unlock once,
then pass Office/HWP paths to the same extractors:

```csharp
using MuPDF.NET;
using MuPDF.NET.Office;

MuPDFOffice.Unlock("YOUR-LICENSE-OR-TRIAL-KEY", fontPathAuto: true);

using Document docx = new Document(@"C:\docs\report.docx");
string markdown = docx.ToMarkdown();
using Document hwpx = new Document(@"C:\docs\report.hwpx");
string json     = hwpx.ToJson();
using Document pptx = new Document(@"C:\docs\report.pptx");
string text     = pptx.ToText();
```

Supported Office formats follow MuPDF.NET.Office (DOC/DOCX, XLS/XLSX, PPT/PPTX,
HWP/HWPX, and related SmartOffice inputs). Layout mode still works: MuPDF.NET
snapshots non-PDF pages to a temporary PDF for the external layout worker while
keeping the original document for extraction metadata.

### OCR

When layout mode is active, OCR is selected automatically when Tesseract or RapidOCR is available. Control behavior with `useOcr`, `forceOcr`, `ocrLanguage`, and optional `ocrFunction`:

```csharp
using MuPDF.NET;

string md = doc.ToMarkdown(
    useOcr: true,
    forceOcr: false,
    ocrLanguage: "eng");
```

`OcrMode` values (layout pipeline): `Never`, `SelectDropOld`, `SelectKeepOld` (default), `ForceDropOld`, `ForceKeepOld`.

## API overview

| Member | Description |
|--------|-------------|
| `Document.ToMarkdown()` | Document → Markdown (tables, headers, images) |
| `Document.ToText()` | Document → plain text with the same layout pipeline |
| `Document.ToJson()` | Layout structure as JSON |
| `Utils.UseLayout()` | Enable or disable the layout pipeline |
| `Utils.GetKeyValues()` | Interactive form field names, values, and locations |
| `Utils.MarkdownToPdf()` | Markdown file → `Document` via MuPDF Story |
| `new PDFMarkdownReader()` / `Utils.LlamaMarkdownReader()` | LlamaIndex-compatible page loader |
| `Utils.LoadAiAsync()` | **net8.0 only** — chunk, embed, and index PDFs for RAG |

Deprecated type-forward names (remove before this package is deleted): `MuPDF4LLM.ToMarkdown` / `ToText` / `ToJson` / `ParseDocument` / `GetKeyValues` / `MarkdownToPdf` / `LlamaMarkdownReader` / `SetUseLayout` / `SetLayoutProvider` / `LoadAiAsync`. Lower-level layout: `PyMuPdfLayout` (`Activate`, `Deactivate`, `IsAvailable`, `Version`).

Additional public helpers: `LayoutParseHelpers.ReadPageLayoutRaw`, `LayoutParseHelpers.SelectOcrFunction`, `GetTextLines.GetRawLines`, `Utils.Iou`, `Utils.TableToMarkdown`.

## Common options

`Document.ToMarkdown` / `ToText` / `ToJson` accept optional parameters including:

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

`Utils.GetKeyValues` also accepts `includeXrefs` to include widget xref numbers in the result.

## Requirements

- **.NET:** netstandard2.0, net461, net472, net48, net5.0–net8.0
- **MuPDF.NET:** **4.1.0** (MuPDF bind **1.28.6**)
- **Layout (optional):** see [MuPDF.NET README — Layout analysis](../README.md#layout-analysis-optional)
- **Office / HWP:** [MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office) **2.1.0**
- **AI/RAG helpers:** net8.0 + `Microsoft.Extensions.AI` (included in the net8.0 package build)

## License

MuPDF.NET.PDF4LLM is part of MuPDF.NET and is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. For commercial licensing, [contact Artifex](https://artifex.com/contact/mupdf-net-inquiry.php).
