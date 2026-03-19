## About

**MuPDF.NET4LLM** provides LLM/RAG helpers for [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET): PDF-to-Markdown conversion, layout parsing, and document structure analysis. It is designed for use with RAG (Retrieval-Augmented Generation) pipelines and integration with LLMs.

This package extends MuPDF.NET with:

- **PDF-to-Markdown** — Convert PDF pages to Markdown with layout awareness (tables, headers, images)
- **Layout parsing** — Extract document structure (pages, boxes, tables, images) as JSON or structured objects
- **LlamaIndex integration** — `PDFMarkdownReader` for compatibility with LlamaIndex document loading
- **OCR support** — Optional OCR for scanned or image-heavy pages
- **Form fields** — Extract key/value pairs from interactive PDF forms

Install with `dotnet add package MuPDF.NET4LLM`. MuPDF.NET is installed automatically as a dependency.

## License and Copyright

**MuPDF.NET4LLM** is part of MuPDF.NET and is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. If you determine you cannot meet the requirements of the Artifex Community License, please [contact Artifex](https://artifex.com/contact/mupdf-net-inquiry.php) for more information regarding a commercial license.
