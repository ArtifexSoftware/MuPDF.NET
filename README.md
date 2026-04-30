# MuPDF.NET

[![Docs](https://img.shields.io/badge/docs-live-brightgreen)](https://mupdfnet.readthedocs.io)
[![Github Stars](https://img.shields.io/github/stars/artifexsoftware/mupdf.net?style=social)](https://github.com/artifexsoftware/mupdf.net/stargazers)
[![Discord](https://img.shields.io/discord/770681584617652264?color=6A7EC2&logo=discord&logoColor=ffffff)](https://pymupdf.io/discord/artifex/)
[![Forum](https://img.shields.io/badge/Forum-ff6600?logo=python&logoColor=ffffff)](https://forum.mupdf.com/c/general/4)

**MuPDF.NET** is a high-performance C# library for reading, writing, rendering, and manipulating PDF, XPS, EPUB, and other document formats. It exposes the full power of the [MuPDF](https://mupdf.com) C engine — as clean, idiomatic .NET bindings for C#, F#, and Visual Basic.

```bash
dotnet add package MuPDF.NET
```

> **LLM / RAG use?** See the companion package [`PDF4LLM`](./PDF4LLM/README.md) for PDF-to-Markdown conversion and LlamaIndex integration, or install it separately: `dotnet add package PDF4LLM`

---

## Contents

- [Why MuPDF.NET](#why-mupdfnet)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Key capabilities](#key-capabilities)
- [Code examples](#code-examples)
- [API overview](#api-overview)
- [Supported formats](#supported-formats)
- [Building from source](#building-from-source)
- [Documentation](#documentation)
- [License](#license)

---

## Why MuPDF.NET

- **Fast** — built on MuPDF, a best-in-class C rendering engine with no mandatory GPU dependency
- **Accurate** — pixel-perfect text extraction with font, colour, and position metadata
- **Versatile** — read, write, annotate, redact, merge, split, convert, and render documents
- **Multi-language** — works in C#, F#, and Visual Basic; mirrors the PyMuPDF API so Python experience transfers directly
- **Barcode support** — read and generate QR, Code 128, Data Matrix, PDF417, and more
- **LLM-ready** — via the `PDF4LLM` companion for Markdown output, layout parsing, and LlamaIndex loading

---

## Requirements

| Requirement | Version |
|---|---|
| .NET | 8.0 or later (.NET Standard 2.0 via `PDF4LLM`) |
| Visual Studio | 2019 or 2022 (Windows build path) |
| OS | Windows, Linux |

> **Note:** While MuPDF.NET is portable, the build instructions in this README target Windows and Visual Studio. See [Getting Started](https://mupdfnet.readthedocs.io/en/latest/getting-started/index.html) for Linux CLI instructions.

---

## Installation

**Option 1 — NuGet (recommended)**

```bash
dotnet add package MuPDF.NET
```

Or in the Visual Studio NuGet Package Manager, search for `MuPDF.NET`.

**Option 2 — Package Manager Console**

```powershell
Install-Package MuPDF.NET
```

Then add the using directive at the top of your file:

```csharp
using MuPDF.NET;
```

The NuGet package bundles the native MuPDF binaries (`mupdfcpp64.dll`, `mupdfcsharp.dll`) — no separate download is required.

---

## Quick start

**Extract text from a PDF**

```csharp
using MuPDF.NET;

Document doc = new Document("document.pdf");
Page page = doc[0];                          // zero-based page index
TextPage tpage = page.GetTextPage();
Console.WriteLine(tpage.ExtractText());
doc.Close();
```

**Search and highlight text**

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
| **Text extraction** | Extract plain text, JSON, XML, HTML, or raw dict with position/font metadata |
| **Rendering** | Render pages to PNG, JPEG, TIFF, BMP, or in-memory bitmaps at any DPI |
| **Annotation** | Add, edit, and delete highlights, underlines, stamps, links, comments, and shapes |
| **Redaction** | Permanently remove text, images, and vector graphics from specified regions |
| **Merging & splitting** | Join multiple PDFs, insert pages, reorder, rotate, and delete pages |
| **Encryption** | Encrypt, decrypt, set passwords, and control permissions |
| **Forms** | Read and write PDF form field values (widgets) |
| **OCR** | OCR image-heavy pages via integrated Tesseract support |
| **Images** | Extract, insert, and replace embedded images and vector graphics |
| **Barcodes** | Scan and generate QR codes, Code 128, Data Matrix, PDF417 |
| **LLM / RAG** | Convert to Markdown with layout awareness via `PDF4LLM` companion package |

---

## Code examples

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
page.AddRedactAnnot(region, fill: new float[] { 1, 0, 0 }); // red fill
page.ApplyRedactions();
doc.Save("redacted.pdf");
doc.Close();
```

### Merge two PDFs

```csharp
using MuPDF.NET;

Document docA = new Document("first.pdf");
Document docB = new Document("second.pdf");
docA.InsertPdf(docB);                        // append all pages from docB
docA.Save("merged.pdf");
docA.Close();
docB.Close();
```

### Insert an image watermark on every page

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

### Extract text with position metadata

```csharp
using MuPDF.NET;

Document doc = new Document("input.pdf");
Page page = doc[0];
// Returns structured blocks: (x0, y0, x1, y1, text, block_no, block_type)
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
// Requires Tesseract installed; language codes follow ISO 639-2
TextPage tpage = page.GetTextPageOCR(language: "eng", dpi: 300, full: true);
Console.WriteLine(tpage.ExtractText());
doc.Close();
```

### F# example

```fsharp
#r "MuPDF.NET.dll"
let doc   = MuPDF.NET.Document("test.pdf")
let page  = doc.LoadPage(0)
let tpage = page.GetTextPage()
printfn $"{tpage.ExtractText()}"
```

### Visual Basic example

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

## API overview

The library's primary entry points are `Document` and `Page`. Most workflows follow the pattern: open → get page → operate → save → close.

### Core classes

| Class | Purpose |
|---|---|
| `Document` | Open, create, save, and manipulate documents; access pages |
| `Page` | Extract text/images, render, annotate, and redact individual pages |
| `TextPage` | Structured text extraction (plain text, JSON, XML, HTML, raw dict) |
| `Pixmap` | Raster image representation; save to PNG, JPEG, TIFF, and more |
| `Rect` / `IRect` | Rectangle geometry used for regions, bounds, and clip boxes |
| `Point` / `Quad` | Point and quadrilateral geometry for precise positioning |
| `Annot` | PDF annotations (highlights, underlines, stamps, shapes, links) |
| `Widget` | Interactive PDF form fields (text boxes, checkboxes, dropdowns) |
| `Font` | Font loading and metrics |
| `TextWriter` | Construct and write formatted text blocks onto pages |
| `Outline` | Access and modify the document table of contents |
| `Barcode` | Scan and generate barcodes and QR codes |
| `Story` | Flow and render HTML content onto pages |
| `Matrix` | Affine transformations for rotation, scaling, and translation |

### Key `Document` methods

| Method | Description |
|---|---|
| `new Document(filename)` | Open a document from a file path |
| `new Document(stream: byte[])` | Open a document from a byte array |
| `doc[n]` / `doc.LoadPage(n)` | Load page at zero-based index `n` |
| `doc.PageCount` | Total number of pages |
| `doc.Save(filename)` | Save to disk |
| `doc.InsertPdf(src)` | Insert all or selected pages from another PDF |
| `doc.InsertFile(infile)` | Append any supported document type as PDF pages |
| `doc.GetToc()` | Return the table of contents |
| `doc.SetMetadata(dict)` | Set document metadata (title, author, etc.) |
| `doc.Encrypt(...)` | Encrypt with password and permissions |
| `doc.Close()` | Release resources |

### Key `Page` methods

| Method | Description |
|---|---|
| `page.GetTextPage()` | Return a `TextPage` for text extraction |
| `page.GetTextPageOCR(language, dpi, full)` | OCR the page using Tesseract |
| `page.GetText(option)` | Shorthand: `"text"`, `"blocks"`, `"html"`, `"json"`, `"xml"` |
| `page.SearchFor(needle)` | Return list of `Rect` matches for a string |
| `page.GetPixmap(matrix, colorspace, dpi)` | Render to a `Pixmap` |
| `page.GetImages()` | List all images on the page |
| `page.InsertImage(rect, filename)` | Insert an image into a region |
| `page.AddHighlightAnnot(rects)` | Add a highlight annotation |
| `page.AddRedactAnnot(rect, fill)` | Mark a region for redaction |
| `page.ApplyRedactions()` | Permanently apply all redaction marks |
| `page.GetBound()` | Return the page's bounding `Rect` |

---

## Supported formats

**Input:** PDF, XPS, EPUB, MOBI, FB2, CBZ, SVG, TXT, JPG/JPEG, PNG, BMP, GIF, TIFF, PNM, PGM, PBM, PPM, PAM, JXR, JPX/JP2, PSD

**Output:** PDF, PNG, JPEG, TIFF, BMP, SVG, and other raster formats via `Pixmap`

---

## Building from source

Building from source is only required if you want to modify the C# bindings or the underlying MuPDF C engine. **To use MuPDF.NET in an application, use NuGet instead** (see [Installation](#installation)).

**Steps (Windows / Visual Studio)**

1. Clone this repository:
   ```bash
   git clone https://github.com/ArtifexSoftware/MuPDF.NET.git
   ```

2. Open `MuPDF.NET/MuPDF.NET.sln` in Visual Studio 2019 or 2022.

3. Set the configuration to **Release** and the platform to **x64**.

4. Select **Build → Build Solution**. If Visual Studio shows missing component warnings in Solution Explorer, click **Install** to resolve them, then build again.

5. The `MuPDF.NET` folder will contain the output DLLs:
   - `mupdfcpp64.dll` — the MuPDF C engine with C++ binding
   - `mupdfcsharp.dll` — the C# bindings layer

   Place both DLLs either in a system folder on your `PATH`, or in the `bin` folder of your application project.

**Linux (command line)**

See the [Getting Started](https://mupdfnet.readthedocs.io/en/latest/getting-started/index.html) guide for `dotnet` CLI instructions on Ubuntu/Debian.

---

## Documentation

| Resource | URL |
|---|---|
| Full API reference | https://mupdfnet.readthedocs.io |
| Getting started guide | https://mupdfnet.readthedocs.io/en/latest/getting-started/index.html |
| The Basics (cookbook) | https://mupdfnet.readthedocs.io/en/latest/the-basics/index.html |
| LLM/RAG companion (`PDF4LLM`) | https://docs.pdf4llm.com/dotnet/getting-started/installation |

---

## License

MuPDF.NET is available under two licences:

- **[Artifex Community License](./LICENSE.md)** — free for non-commercial use (personal, educational, open-source projects)
- **Commercial licence** — required for any commercial use; [contact Artifex](https://artifex.com/contact/mupdf-net)

> **What counts as commercial use?** Any use that directly or indirectly generates revenue — including internal business tooling, SaaS products, and enterprise workflows. See [LICENSE.md](./LICENSE.md) for the complete definition.