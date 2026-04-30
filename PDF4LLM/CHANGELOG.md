# Changelog

All notable changes for `PDF4LLM` are documented in this file.

## [1.27.2.8]
- Improved Tesseract OCR stability by auto-adjusting OCR DPI to keep page pixmap memory under `maxOcrPixmapBytes`.

## [1.27.2.4]
- Fixed `PDFMarkdownReader` to keep page `extraInfo` isolated per page.

## [1.27.2.3]
- Fixed `ToMarkdown`, `ToJson`, and `ToText` to support file path string input parameters.

## [1.27.2.2]
- Initial release (port of `pymudfl4llm` `1.27.2.2`).
