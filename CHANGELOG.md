# Changelog

### [3.28.0] - 2026-07-02

Aligned MuPDF.NET with **PyMuPDF 1.28.0** and **MuPDF 1.28.0**.

#### New APIs and behavior

- **`Document`**: optional `archive` on open (uses `fz_open_document_with_stream_and_dir` for reflowable/HTML/Markdown documents with embedded assets).
- **`Document.ApplyCss()`**: apply user CSS to reflowable documents (`fz_style_document`).
- **`Document.Save()` / `Write()`**: non-PDF documents are converted via `ConvertToPdf()` before writing (PyMuPDF 1.28 parity).
- **`ConvertToPdf()`**: copies external and internal links into the generated PDF (`JmConvertToPdf`).

#### Bug fixes (PyMuPDF parity)

- **`Widget.ChoiceValues`**: reads `/Opt` with `pdf_dict_get_inheritable` ([#4114](https://github.com/pymupdf/PyMuPDF/issues/4114)).
- **`Page.RemoveRotation()`**: skips widgets with empty or infinite rectangles ([#4950](https://github.com/pymupdf/PyMuPDF/issues/4950)).
- **`Page.Annots()`**: ignores null annotation handles ([#4928](https://github.com/pymupdf/PyMuPDF/issues/4928)).
- **`Page.GetDrawings()`**: `lineJoin` is no longer scaled by path factor; stroke width still uses path factor ([#4954](https://github.com/pymupdf/PyMuPDF/issues/4954)).
- **`Document.InsertPdf()`**: link rectangles are transformed by the inverse page CTM when copying rotated pages ([#4958](https://github.com/pymupdf/PyMuPDF/issues/4958)).
- **`Shape`**: PDF `lineJoin` operator is prepended correctly (fixes invalid `lineJoin j` content generation from 1.27.2.2).
- **`Annot.SetRotation()` / `Update()`**: rotation round-trip works without errors ([#5033](https://github.com/pymupdf/PyMuPDF/issues/5033)).
- **`AsPdfDocument()` / `SaveCore()`**: non-PDF documents no longer appear as empty PDFs during save, fixing corrupted Markdown-to-PDF output.

#### Tests

- Expanded `MuPDF.NET.Test` for PyMuPDF 1.28.0 regression coverage, including markdown/archive support (`TestMarkdownSupport`), clip+links ([#4942](https://github.com/pymupdf/PyMuPDF/issues/4942)), threading stress (`TestThreads`), object streams, formula rendering ([#5001](https://github.com/pymupdf/PyMuPDF/issues/5001)), and `fz_load_system_font` callback tracing (`test_load_system_font`).

### [3.2.17.9] - 2026-06-23
- Depends on stable **`MuPDF.NativeAssets` 1.28.0**.
- Updated PyMuPDF bind to **1.27.2.3** (`VersionBind` / `pymupdf_version`).
- **`Matrix`**: added static `Matrix.Concat(one, two)`; renamed the in-place PyMuPDF `concat` equivalent to `ConcatInto(one, two)` (avoids C# static/instance signature clashes); added instance `Inverted()` returning a new matrix or `null` when singular.
- **`Page`**: added `GetLayout()`, `LayoutInformation`, and `GetLayoutProvider` so external layout engines (e.g. pymupdf-layout via **PDF4LLM**) can supply `layout_information` boxes consumed by `Page.find_tables()`.
- Expanded `MuPDF.NET.Test` matrix/geometry coverage aligned with PyMuPDF `test_geometry.py`.

### [3.2.17] - 2026-06-11
- Fixed `Page.find_tables()` on rotated pages by porting PyMuPDF `page_rotation_set0` / `page_rotation_reset`: temporary derotation via `Tools.InsertContents`, MediaBox/rotation updates, and page reload through `doc[page.Number]` so vector graphics and table detection work at 0°, 90°, 180°, and 270°.
- Hardened `Tools.InsertContents` and related PDF page edits with `AsPdfPageFresh` and cached `PdfPage` invalidation to avoid stale native handles after `/Contents` changes.
- Fixed `TableHelpers.CharsInRect` to use visual `top`/`bottom` coordinates when matching characters to table cells (pdfplumber / PyMuPDF parity).
- Expanded `MuPDF.NET.Test` table coverage with ports from PyMuPDF `test_tables.py`, including rotation-independent extraction (`test_2812`), glyph-height handling (`test_2979`), and additional edge/strategy cases.

### [3.2.16] - 2026-04-24
- Added global `Utils.MuPDFLock` and synchronized MuPDF native calls for improved thread safety.
- Improved Tesseract OCR stability in the `PDF4LLM` OCR pipeline and hardened OCR helper behavior.
- Fixed a regression in Llama `LoadData` and added a new `TableExtract` demo sample.
- Updated `PDF4LLM` package metadata and NuGet project files.

### [3.2.15] - 2026-04-17
- Migrated the helper package from `MuPDF.NET4LLM` to `PDF4LLM` and refreshed the package layout, demos, and documentation.
- Added file-path overloads for `ToMarkdown`, `ToJson`, and `ToText` helpers.
- Updated `PDF4LLM` package support for the latest MuPDF bindings and metadata.

### [3.2.14] - 2026-03-23
- Fixed issue #234 in page/text utilities and added a regression test in `UtilsTest`.
- Minor `PDF4LLM` documentation and comment updates.

### [3.2.13] - 2026-03-18
- Added **MuPDF.NET4LLM** as a separate NuGet package: LLM/RAG helpers for PDF-to-Markdown conversion, layout parsing, document structure analysis, and LlamaIndex integration. Install via `dotnet add package MuPDF.NET4LLM`; depends on MuPDF.NET.
- Fixed `DocumentWriter` leak in `Story.WriteWithLinks` and `Story.WriteStabilizedWithLinks` (dispose via `using`).
- Made `DocumentWriter.Dispose()` idempotent; added `ObjectDisposedException` for `BeginPage`/`EndPage` after dispose.
- Hardened exception safety: added try/finally for `Document.Save`, `Document.Object2Buffer`, `Document.ObjString`, `Document.JournalSave`, `Pixmap.ToBytes`, `TextPage.ExtractText`, `Page.GetSvgImage`, and `Utils.Object2Buffer` so native resources are released on exception.

### [3.2.13-rc.14] - 2026-02-11
- Upgraded `MuPDF.NativeAssets` to 1.27.2 and refreshed generated MuPDF bindings for Windows and Linux.
- Implemented `IDisposable` on core types (`Document`, `Page`, `TextPage`, `Story`, `DocumentWriter`, `DisplayList`, `Font`, `GraftMap`, `DeviceWrapper`, `Outline`) and made `Document.Dispose()` idempotent.
- Hardened native resource handling (e.g. `Document.Convert2Pdf`, `Pixmap.InvertIrect`) to be exception-safe, fixed `Pixmap.InvertIrect` null/stencil handling, and added tests for table extraction and disposal patterns.

### [3.2.13-rc.6] - 2025-12-26
- Fixed the issues in `ResolveNames` method.
- Fixed the issues in `DrawShape`

### [3.2.12] - 2025-11-25
- Added `ImageFilterPipeline` with SkiaSharp-based filters and integrated it into OCR.
- Improved `Pixmap` disposal, OCR helpers, and image filter application.

### [3.2.11] - 2025-10-20
- Fixed issues related to the older Linux version.
- Resolved the issue that occurred when deleting an already managed document.

### [3.2.10] - 2025-10-07
- Replaced all Windows System.Drawing dependencies with SkiaSharp.
- Added support for 32-bit .NET projects in Visual Studio 2019.
- Fixed issues with Unicode file names.

### [3.2.10-rc.5] - 2025-09-16
- Removed margin parameter in write barcode.
- Added new marginLeft,marginTop,marginRight,marginBottom parameters to barcode creation.

### [3.2.10-rc.3] - 2025-09-12
- Upgrade MuPDF.NativeAssets to 1.26.8.
- Added a new method "Utils.GetBarcodePixmap" into barcode write module.

### [3.2.10-rc.2] - 2025-09-11
- Added a new parameter "narrowBarWidth" into barcode write module.

### [3.2.10-rc.1] - 2025-09-03
- Added a new barcode rendering engine.
- Removed all dependencies on ZXing.

### [3.2.9] - 2025-08-28
- Deployed new release

### [3.2.9-rc.15] - 2025-08-22
- Fixed PDF file handle leak caused by unreleased FzPage in DisplayList.
- Resolved memory leak in DocumentWriter (caused by missing FilePtrOutput).
- Added Dispose() in Story.
- Added Dispose() in TextPage.

### [3.2.9-rc.11] - 2025-08-12
- Updated barcode reader for low quality images/pages.