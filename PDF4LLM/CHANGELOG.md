# Changelog

All notable changes for `PDF4LLM` are documented in this file.

## [1.27.2.27]
- Requires **MuPDF.NET 3.2.17.9** (pymupdf4llm **1.27.2.3** bind).
- Ported pymupdf4llm OCR decision pipeline: `AnalyzePage`, `ComputeOcrFeatures`, and `PredictOcrProbability()` using the bundled `ocr_decision_model.onnx` (shipped as a separate NuGet file and copied to output via MSBuild targets).
- Hardened the pymupdf-layout Python worker bridge: prefixed `RESULT` responses, redirect stray library stdout to stderr, skip non-JSON lines, and return `null` on parse failure so `Page.find_tables()` and layout parsing degrade gracefully instead of throwing.
- Improved pipeline status messages: print **Using pymupdf-layout** when the layout worker is active; show setup instructions when pymupdf-layout is missing (`dotnet msbuild -t:PDF4LLMSetupLayoutPython` or `python PDF4LLM/scripts/setup_layout_python.py`); show Tesseract install guidance when OCR is requested but tessdata is unavailable; print **Using Tesseract for OCR processing** when OCR is available.
- Improved Tesseract discovery (`TESSDATA_PREFIX`, `tesseract --list-langs`) and removed the misleading static `TesseractApi` warning at class load.

## [1.27.2.17]
- Synced with **pymupdf4llm** 1.27.2.3: added `MarkdownToPdf()`, `GetKeyValues()`, and automatic OCR plugin selection (`SelectOcrFunction`) so `forceOcr=True` works without an explicit `ocrFunction` when Tesseract or RapidOCR is available.
- Added `PyMuPdfLayout` and the Python layout bridge (`PDF4LLMSetupLayoutPython` MSBuild target) for optional [pymupdf-layout](https://pypi.org/project/pymupdf-layout/) integration.
- Added `PdfExtractor.LoadAiAsync` and the `PDF4LLM.AI` pipeline (**net8.0**): chunk PDFs, generate embeddings, index vectors, and run Ask / Summarize / Search workflows via `Microsoft.Extensions.AI` (optional Azure OpenAI and Azure AI Search).
- Raised the minimum **MuPDF.NET** dependency to **3.2.17** (improved table detection on rotated pages).
- Centralized package version metadata in `VersionInfo` (sourced from `Versions.props` / `Artifex.Versions`).

## [1.27.2.8]
- Improved Tesseract OCR stability by auto-adjusting OCR DPI to keep page pixmap memory under `maxOcrPixmapBytes`.

## [1.27.2.4]
- Fixed `PDFMarkdownReader` to keep page `extraInfo` isolated per page.

## [1.27.2.3]
- Fixed `ToMarkdown`, `ToJson`, and `ToText` to support file path string input parameters.

## [1.27.2.2]
- Initial release (port of `pymupdf4llm` 1.27.2.2).
