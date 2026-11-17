# Changelog

Notable changes in each MuPDF.NET release.

### [4.1.0] - 2026-11-11

- Built on MuPDF **1.28.6**. Ported from [PyMuPDF](https://pypi.org/project/pymupdf/) **2.1**.
- `Document.MovePage` is more reliable on Linux.
- Page labels, text-trace character bounds, and `ShowPdfPage` on rotated pages match PyMuPDF 2.1.
- Optional layout analysis uses pymupdf **2.1** and requires **Python 3.11+**. Run `dotnet msbuild -t:MuPDFNetSetupLayoutPython` once; `MuPDFNetVerifyLayoutPython` checks the install and prints setup help if layout is missing.
- Markdown, JSON, and text extract (`Document.ToMarkdown` / `ToJson` / `ToText` / `ToChunks`) stay in this package. **MuPDF.NET.PDF4LLM is deprecated** — do not add it to new projects.
- Office and HWP files: use [MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office) **2.1.0**.

### [4.0.1] - 2026-11-05

- `Page.DeleteAnnot` no longer keeps the PDF file locked after the document is closed.

### [4.0.0] - 2026-10-22

- Built on MuPDF **1.28.5**. Package version **4.0.0**. Port from **PyMuPDF 2.0**.
- `Pixmap.FlipRotate` (`Rotate90` / `Rotate270` / `Rotate180` / `FlipLeftRight` / `FlipTopBottom`).
- Form fields: repair radio groups, keep field hierarchy on add/delete, and cleaner `GetKeyValues` results.
- Table detection: faster drawings pass, `union` / `refine` options, and better handling of stacked tables.
- LLM extract is in this package: `Document.ToMarkdown` / `ToJson` / `ToText` / `ToChunks`, `Utils.UseLayout`, `Utils.ConvertBatch`.
- Optional layout analysis is **on by default** when the pymupdf **2.0** worker is installed (`GetLayout` / `FindTables` enable it on first use).
- `Document.SetToc` accepts the result of `GetToc()`, including destinations when `simple: false` (PyMuPDF `set_toc(get_toc(simple=False))`).
- `Pixmap.PilToBytes` / `PilSave` / `PilImage` match PyMuPDF `pil_tobytes` / `pil_save` / `pil_image` (JPEG quality default 75).
- `doc[(chapter, page)]` matches PyMuPDF `doc[(chapter, pno)]`.
- `Page.RemoveRotation` matches PyMuPDF 2.0: annotation and link appearance stay correct on rotated pages.
- Office and HWP files: [MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office) **2.0.2**.
- `MuPDF.Fonts` **1.0.5** pins SharpCompress to **0.50.7**.

### [3.28.2.4] - 2026-09-23

- Parallel `GetPixmap` across **different** documents (the same document is still serialized).
- Native resource leaks fixed in widgets, text search, xref helpers, annotations, and related paths.

### [3.28.2] - 2026-08-14

- Aligned with **PyMuPDF 1.28.2** and MuPDF **1.28.2**.
- `FindTables`: `useLayout`, `union`, and `refine` options; `Table.ToHtml()` and placement metadata on the refine path.
- `Document.RewriteImages`: `setToGray` and `options` (`PdfImageRewriterOptions`).
- Fixes: hidden-text scrub, named destinations / TOC links, empty-cell table bbox, `ClipToRect`, and square-image `InsertImage(keepProportion: true)`.

### [3.28.1.2] - 2026-07-14

- Native RID detection on Linux/macOS ARM.
- Text extraction keeps full-page OCR images and matches infinite-clip behavior.

### [3.28.1] - 2026-07-10

- `FindTables` can use layout table boxes when a layout provider is set.
- Table extraction is safe across threads on separate documents.
- `ToMarkdown` table-header regression (`Col1`, `Col2`, …) fixed.

### [3.28.0] - 2026-07-02

- Aligned with **PyMuPDF 1.28.0** and MuPDF **1.28.0**.
- Open Markdown files. Optional `archive` on open for HTML/Markdown with embedded assets. `Document.ApplyCss()` for reflowable documents.
- Saving a non-PDF document converts it to PDF first, including links (PyMuPDF 1.28 parity).

### [3.2.17.9] - 2026-06-23

- Built on MuPDF **1.28.0**. PyMuPDF bind **1.27.2.3**.
- `Matrix.Concat` / `ConcatInto` / `Inverted()`.
- `Page.GetLayout()`, `LayoutInformation`, and `GetLayoutProvider` for layout-aware table detection.

### [3.2.17] - 2026-06-11

- `FindTables` on rotated pages (0°, 90°, 180°, 270°).
- Table cell matching uses visual top/bottom coordinates.

### [3.2.16] - 2026-04-24

- Improved thread safety around native MuPDF calls.
- More stable Tesseract OCR helpers.

### [3.2.15] - 2026-04-17

- Helper package renamed to `MuPDF.NET.PDF4LLM`.
- File-path overloads for `ToMarkdown`, `ToJson`, and `ToText`.

### [3.2.14] - 2026-03-23

- Fixed issue #234 in page/text utilities.

### [3.2.13] - 2026-03-18

- LLM/RAG helpers as a separate NuGet package (`ToMarkdown`, layout, LlamaIndex).
- `IDisposable` on core types; `DocumentWriter` and `Story` link-writing leaks fixed.

### [3.2.12] - 2025-11-25

- `ImageFilterPipeline` (SkiaSharp) integrated into OCR.
- Improved `Pixmap` disposal.

### [3.2.11] - 2025-10-20

- Fixes for older Linux and deleting an already-managed document.

### [3.2.10] - 2025-10-07

- Windows drawing uses SkiaSharp instead of System.Drawing.
- 32-bit .NET projects in Visual Studio 2019.
- Unicode file names.
- New barcode rendering engine (`Utils.GetBarcodePixmap`, per-side margins, `narrowBarWidth`).

### [3.2.9] - 2025-08-28

- PDF file-handle leak in `DisplayList` fixed.
- `DocumentWriter` memory leak fixed.
- `Dispose()` on `Story` and `TextPage`.
- Barcode reader improved on low-quality images.
