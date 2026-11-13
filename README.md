# MuPDF.NET

[![Docs](https://img.shields.io/badge/docs-live-brightgreen)](https://mupdfnet.readthedocs.io)
[![Github Stars](https://img.shields.io/github/stars/artifexsoftware/mupdf.net?style=social)](https://github.com/artifexsoftware/mupdf.net/stargazers)
[![Discord](https://img.shields.io/discord/770681584617652264?color=6A7EC2&logo=discord&logoColor=ffffff)](https://pymupdf.io/discord/artifex/)
[![Forum](https://img.shields.io/badge/Forum-ff6600?logo=python&logoColor=ffffff)](https://forum.mupdf.com/c/general/4)

**MuPDF.NET** is a C# library for reading, writing, rendering, and editing PDF, XPS, EPUB, and other document formats. It is a port of [PyMuPDF](https://pymupdf.readthedocs.io) to .NET, built on the [MuPDF](https://mupdf.com) engine, with idiomatic APIs for C#, F#, and Visual Basic.

```bash
dotnet add package MuPDF.NET
```

Runnable samples (one project per feature) are in **[MuPDF.NET.Examples](https://github.com/ArtifexSoftware/MuPDF.NET.Examples)**.

> **Markdown / JSON for LLM and RAG** is in this package: `Document.ToMarkdown`, `ToJson`, `ToText`, `ToChunks`, and `Utils.LoadAiAsync`. Optional layout: [Layout analysis](#layout-analysis-optional). Office and HWP files: [MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office). Do **not** add `MuPDF.NET.PDF4LLM` — that package is deprecated.

---

## Contents

- [Why MuPDF.NET](#why-mupdfnet)
- [Requirements](#requirements)
- [Installation](#installation)
- [Layout analysis](#layout-analysis-optional)
- [Quick start](#quick-start)
- [Key capabilities](#key-capabilities)
- [Code examples](#code-examples)
- [Examples](#examples)
- [API overview](#api-overview)
- [Supported formats](#supported-formats)
- [Building from source](#building-from-source)
- [Documentation](#documentation)
- [License](#license)

---

## Why MuPDF.NET

- **Fast** — built on MuPDF, with no mandatory GPU dependency
- **Accurate** — text extraction with font, colour, and position metadata
- **Versatile** — read, write, annotate, redact, merge, split, convert, and render
- **Multi-language** — C#, F#, and Visual Basic; the API mirrors [PyMuPDF](https://pymupdf.readthedocs.io) so Python experience transfers directly
- **Barcodes** — read and generate QR, Code 128, Data Matrix, PDF417, and more
- **LLM-ready** — Markdown / JSON / text extract in this package; optional RAG helpers on .NET 8

---

## Requirements

| Requirement | Version |
|---|---|
| .NET | 8.0 or later (also .NET Standard 2.0 and .NET Framework 4.6.1+) |
| Visual Studio | 2019 or 2022 (Windows) |
| OS | Windows, Linux |

Linux CLI install steps: [Getting Started](https://mupdfnet.readthedocs.io/en/latest/getting-started/index.html).

---

## Installation

```bash
dotnet add package MuPDF.NET
```

Or in Visual Studio: NuGet Package Manager → search **MuPDF.NET**.

```powershell
Install-Package MuPDF.NET
```

```csharp
using MuPDF.NET;
```

The package includes the native MuPDF binaries. No separate download is required.

**Office / HWP** (DOC/DOCX, XLS/XLSX, PPT/PPTX, HWP/HWPX) is a separate commercial package:

```bash
dotnet add package MuPDF.NET.Office
```

Call `MuPDFOffice.Unlock` once, then open those files with `Document` and the same `ToMarkdown` / `ToJson` / `ToText` APIs.

---

## Layout analysis (optional)

`GetLayout`, `FindTables`, and layout-aware `ToMarkdown` can use [pymupdf](https://pypi.org/project/pymupdf/) **2.1** as an optional layout worker (`import pymupdf.layout`). Without it, MuPDF.NET uses classic text extraction.

Requires **Python 3.11+** on `PATH`. From any project that references MuPDF.NET:

```bash
dotnet msbuild -t:MuPDFNetSetupLayoutPython
```

That creates a per-user virtual environment and installs `pymupdf==2.1`. MuPDF.NET finds it automatically.

| OS | Location |
|----|----------|
| Windows | `%LOCALAPPDATA%\MuPDF.NET\.venv-layout` |
| Linux / macOS | `~/.local/share/mupdf.net/.venv-layout` |

Check the install:

```bash
dotnet msbuild -t:MuPDFNetVerifyLayoutPython
```

On Ubuntu 22.04, install Python 3.11 first (`python3` is 3.10 there):

```bash
sudo apt install python3.11 python3.11-venv python3.11-full
```

To use another Python 3.11+ interpreter that already has pymupdf 2.1, set **`MUPDF_NET_PYTHON`**.

```csharp
bool layoutReady = Utils.LayoutPythonAvailable;  // worker can be imported
bool layoutActive = Utils.LayoutAvailable;       // provider is registered
Utils.UseLayout(false);                          // classic extraction
```

---

## Quick start

**Extract text**

```csharp
using MuPDF.NET;

Document doc = new Document("document.pdf");
Page page = doc[0];
TextPage tpage = page.GetTextPage();
Console.WriteLine(tpage.ExtractText());
doc.Close();
```

**Convert to Markdown**

```csharp
using MuPDF.NET;

using Document doc = new Document("document.pdf");
Utils.UseLayout(false);                         // omit this line to use layout when installed
string markdown = doc.ToMarkdown(showProgress: false);
File.WriteAllText("document.md", markdown);
```

**Search and highlight**

```csharp
using MuPDF.NET;

Document doc = new Document("document.pdf");
Page page = doc[0];
List<Rect> matches = page.SearchFor("invoice");
if (matches.Count > 0)
    page.AddHighlightAnnot(matches);
doc.Save("highlighted.pdf");
doc.Close();
```

**Render a page to PNG**

```csharp
using MuPDF.NET;

Document doc = new Document("document.pdf");
Page page = doc[0];
Pixmap pix = page.GetPixmap();               // 72 dpi by default
pix.Save("page_0.png");
doc.Close();
```

---

## Key capabilities

| Area | What you can do |
|---|---|
| **Text extraction** | Plain text, JSON, XML, HTML, or structured dict with position and font metadata |
| **Rendering** | PNG, JPEG, TIFF, BMP, or in-memory bitmaps at any DPI |
| **Annotation** | Highlights, underlines, stamps, links, comments, and shapes |
| **Redaction** | Permanently remove text, images, and graphics from a region |
| **Merging & splitting** | Join PDFs, insert, reorder, rotate, and delete pages |
| **Encryption** | Encrypt, decrypt, passwords, and permissions |
| **Forms** | Read and write PDF form fields |
| **OCR** | Image-heavy pages via Tesseract |
| **Images** | Extract, insert, replace, and recompress (`RewriteImages`) |
| **Barcodes** | Scan and generate QR, Code 128, Data Matrix, PDF417 |
| **LLM / RAG** | `ToMarkdown` / `ToJson` / `ToText` / `ToChunks`; `Utils.LoadAiAsync`; layout when the worker is installed |
| **Office / HWP** | [MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office); unlock, then use `Document` as usual |

---

## Code examples

Copy-paste snippets. Full apps are in **[MuPDF.NET.Examples](https://github.com/ArtifexSoftware/MuPDF.NET.Examples)**.

### Add a text watermark

```csharp
using MuPDF.NET;

Document doc = new Document("input.pdf");
for (int i = 0; i < doc.PageCount; i++)
{
    Page page = doc[i];
    Font font = new Font("helv");
    TextWriter tw = new TextWriter(page.Rect);
    tw.Append(new Point(50, 100), "CONFIDENTIAL", font);
    tw.WriteText(page);
}
doc.Save("watermarked.pdf");
doc.Close();
```

### Redact a region

```csharp
using MuPDF.NET;

Document doc = new Document("input.pdf");
Page page = doc[0];
Rect region = new Rect(0, 0, 200, 200);
page.AddRedactAnnot(region, fill: new float[] { 1, 0, 0 });
page.ApplyRedactions();
doc.Save("redacted.pdf");
doc.Close();
```

### Merge two PDFs

```csharp
using MuPDF.NET;

Document docA = new Document("first.pdf");
Document docB = new Document("second.pdf");
docA.InsertPdf(docB);
docA.Save("merged.pdf");
docA.Close();
docB.Close();
```

### Insert an image on every page

```csharp
using MuPDF.NET;

Document doc = new Document("input.pdf");
for (int i = 0; i < doc.PageCount; i++)
{
    Page page = doc[i];
    page.InsertImage(page.GetBound(), filename: "watermark.png", overlay: false);
}
doc.Save("watermarked.pdf");
doc.Close();
```

### Extract text with positions

```csharp
using MuPDF.NET;

Document doc = new Document("input.pdf");
Page page = doc[0];
var blocks = page.GetTextPage().ExtractBLOCKS();
foreach (var block in blocks)
    Console.WriteLine(block);
doc.Close();
```

### OCR a scanned page

```csharp
using MuPDF.NET;

Document doc = new Document("scanned.pdf");
Page page = doc[0];
TextPage tpage = page.GetTextPageOCR(language: "eng", dpi: 300, full: true);
Console.WriteLine(tpage.ExtractText());
doc.Close();
```

### F#

```fsharp
#r "MuPDF.NET.dll"
let doc   = MuPDF.NET.Document("test.pdf")
let page  = doc.LoadPage(0)
let tpage = page.GetTextPage()
printfn $"{tpage.ExtractText()}"
```

### Visual Basic

```vb
Imports MuPDF.NET
Module Program
    Sub Main()
        Dim doc   As Document = New Document("test.pdf")
        Dim page  As Page     = doc.LoadPage(0)
        Dim tpage As TextPage = page.GetTextPage()
        Console.WriteLine(tpage.ExtractText())
    End Sub
End Module
```

---

## Examples

**[MuPDF.NET.Examples](https://github.com/ArtifexSoftware/MuPDF.NET.Examples)** is a separate repo of small console apps. Each one references [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET) (and [MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office) where needed) from nuget.org.

```powershell
git clone https://github.com/ArtifexSoftware/MuPDF.NET.Examples.git
cd MuPDF.NET.Examples
dotnet restore
dotnet run --project MuPDF.NET\01-OpenSave
```

```bash
dotnet run --project MuPDF.NET/01-OpenSave
```

| Folder | Contents |
|--------|----------|
| `MuPDF.NET/` | Open/save, pages, render, text, annotations, forms, images, tables, barcodes, encrypt, Markdown / JSON / text, OCR, RAG |
| `MuPDF.NET.Office/` | Unlock, open DOCX/HWPX, export PDF / Markdown / JSON |

- Layout samples (`33-ToJsonLayout`, `34-ToText`): Python 3.11+, then `dotnet msbuild -t:MuPDFNetSetupLayoutPython`.
- Office samples: set `MUPDF_OFFICE_KEY` for a full unlock; without a key they open the first 3 pages only.

Batch-run with `run-all.cmd` / `run-all.ps1` / `run-all.sh`. See the [Examples README](https://github.com/ArtifexSoftware/MuPDF.NET.Examples#readme) for the full project list.

---

## API overview

Typical workflow: open → get a page → operate → save → close.

### Core classes

| Class | Purpose |
|---|---|
| `Document` | Open, create, save, and manipulate documents |
| `Page` | Text, images, render, annotate, redact |
| `TextPage` | Structured text (plain, JSON, XML, HTML, dict) |
| `Pixmap` | Raster image; save as PNG, JPEG, TIFF, and more |
| `Rect` / `IRect` | Regions, bounds, clip boxes |
| `Point` / `Quad` | Points and quads |
| `Annot` | Highlights, stamps, shapes, links |
| `Widget` | Form fields |
| `Font` | Font loading and metrics |
| `TextWriter` | Write formatted text onto a page |
| `Outline` | Table of contents |
| `Barcode` | Scan and generate barcodes |
| `Story` | Flow HTML onto pages |
| `Matrix` | Rotation, scaling, translation |

### `Document`

| Method | Description |
|---|---|
| `new Document(filename)` | Open from a path |
| `new Document(stream: byte[])` | Open from a byte array |
| `doc[n]` / `doc.LoadPage(n)` | Page at zero-based index `n` |
| `doc[(chapter, page)]` | Page by chapter location |
| `doc.PageCount` | Number of pages |
| `doc.Save(filename)` | Save to disk |
| `doc.InsertPdf(src)` | Insert pages from another PDF |
| `doc.InsertFile(infile)` | Append a supported file as PDF pages |
| `doc.GetToc()` / `doc.SetToc(...)` | Table of contents |
| `doc.SetMetadata(dict)` | Title, author, and other metadata |
| `doc.Encrypt(...)` | Password and permissions |
| `doc.RewriteImages(...)` | Downsample or recompress images |
| `doc.ToMarkdown(...)` | Convert to Markdown |
| `doc.ToJson(...)` | Layout structure as JSON |
| `doc.ToText(...)` | Convert to plain text |
| `doc.ToChunks(...)` | Retrieval-ready chunks |
| `doc.GetKeyValues(...)` | Form field names and values |
| `doc.Close()` | Release resources |

### `Utils`

| Member | Description |
|---|---|
| `Utils.UseLayout(bool)` | Enable or disable layout analysis |
| `Utils.LayoutAvailable` | Whether a layout provider is registered |
| `Utils.LoadAiAsync(...)` | **net8.0** — chunk, embed, and index PDFs for RAG |
| `Utils.MarkdownToPdf(...)` | Markdown file → PDF |
| `Utils.GetKeyValues(...)` | Form fields from a `Document` or path |
| `new PDFMarkdownReader()` | LlamaIndex-compatible page loader |

### `Page`

| Method | Description |
|---|---|
| `page.GetTextPage()` | `TextPage` for extraction |
| `page.GetTextPageOCR(language, dpi, full)` | OCR with Tesseract |
| `page.GetText(option)` | `"text"`, `"blocks"`, `"html"`, `"json"`, `"xml"` |
| `page.SearchFor(needle)` | List of `Rect` matches |
| `page.GetPixmap(...)` | Render to a `Pixmap` |
| `page.GetImages()` | Images on the page |
| `page.InsertImage(rect, filename)` | Insert an image |
| `page.AddHighlightAnnot(rects)` | Highlight annotation |
| `page.AddRedactAnnot(rect, fill)` | Mark a region for redaction |
| `page.ApplyRedactions()` | Apply redaction marks |
| `page.GetBound()` | Page bounding `Rect` |

---

## Supported formats

**Input:** PDF, XPS, EPUB, MOBI, FB2, CBZ, SVG, TXT, JPG/JPEG, PNG, BMP, GIF, TIFF, PNM, PGM, PBM, PPM, PAM, JXR, JPX/JP2, PSD

**Output:** PDF, PNG, JPEG, TIFF, BMP, SVG, and other raster formats via `Pixmap`

---

## Building from source

Use NuGet unless you need to change the bindings or the MuPDF engine.

1. `git clone https://github.com/ArtifexSoftware/MuPDF.NET.git`
2. Open `MuPDF.NET.sln` in Visual Studio 2019 or 2022.
3. **Release** / **x64**, then **Build → Build Solution**.

Linux: see [Getting Started](https://mupdfnet.readthedocs.io/en/latest/getting-started/index.html).

---

## Documentation

| Resource | URL |
|---|---|
| API reference | https://mupdfnet.readthedocs.io |
| Getting started | https://mupdfnet.readthedocs.io/en/latest/getting-started/index.html |
| Cookbook | https://mupdfnet.readthedocs.io/en/latest/the-basics/index.html |
| LLM / RAG | https://docs.pdf4llm.com/dotnet/getting-started/installation |
| Sample apps | https://github.com/ArtifexSoftware/MuPDF.NET.Examples |

---

## License

MuPDF.NET, [PyMuPDF](https://pymupdf.readthedocs.io), and [MuPDF](https://mupdf.com) are developed by [Artifex Software, Inc.](https://artifex.com)

MuPDF.NET is available under two licences:

- **[Artifex Community License](./LICENSE.md)** — free for non-commercial use (personal, educational, open-source projects)
- **Commercial licence** — required for any commercial use; [contact Artifex](https://artifex.com/contact/mupdf-net)

> **Commercial use** includes any use that directly or indirectly generates revenue — internal business tooling, SaaS, and enterprise workflows. See [LICENSE.md](./LICENSE.md).
