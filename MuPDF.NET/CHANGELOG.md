# Changelog

### [3.28.1.2] - 2026-07-14

- Depends on **`MuPDF.NativeAssets` 1.28.0.1**.
- Fixed native RID detection on Linux/macOS ARM (no longer defaults to `linux-x64` when `PROCESSOR_ARCHITECTURE` is unset).
- Text extraction: infinite clip and image-block filtering match PyMuPDF (keeps full-page OCR images).

### [3.28.1] - 2026-07-10

- **`FindTables()` / `Utils.GetTables()`**: layout-guided detection when `Page.GetLayoutProvider` is set (pymupdf-layout / **PDF4LLM**); supports tuple and raw layout `table` boxes.
- **Concurrency**: per-thread scratch buffers and detection settings replace shared `TableModule` state — safe for parallel extraction across separate documents.
- **`ToMarkdown()`**: fixed header regression (`Col1`, `Col2`, …) after the thread-safety refactor.

### [3.28.0] - 2026-07-02

MuPDF.NET now supports loading of Markdown files.  
Aligned MuPDF.NET with **PyMuPDF 1.28.0** and **MuPDF 1.28.0**.

#### New APIs and behavior

- **`Document`**: optional `archive` on open (uses `fz_open_document_with_stream_and_dir` for reflowable/HTML/Markdown documents with embedded assets).
- **`Document.ApplyCss()`**: apply user CSS to reflowable documents (`fz_style_document`).
- **`Document.Save()` / `Write()`**: non-PDF documents are converted via `ConvertToPdf()` before writing (PyMuPDF 1.28 parity).
- **`ConvertToPdf()`**: copies external and internal links into the generated PDF (`JmConvertToPdf`).

#### Bug fixes (table extraction / concurrency)

- **`TableHelpers.FindTables()` / `Utils.GetTables()`**: replaced process-wide shared table scratch buffers (`TableModule.CHARS`, `TableModule.EDGES`) with **per-thread reusable lists**, so concurrent table detection on separate `Document`/`Page` instances no longer corrupts extracted content.
- **Table detection settings**: `FindTables()` now uses thread-local small-glyph-height and skip-quad-correction overrides instead of toggling global `Tools.SetSmallGlyphHeights()` / `Helpers.SkipQuadCorrections`, avoiding cross-thread interference during parallel extraction.
- **`Table` header / `ToMarkdown()`**: `Table` now receives character data before header resolution, fixing a regression where markdown headers fell back to `Col1`, `Col2`, … after the thread-safety refactor.
- **`TableHelpers.CharsInRect`**: hot-path scan uses a direct loop instead of LINQ `.Any()`.

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