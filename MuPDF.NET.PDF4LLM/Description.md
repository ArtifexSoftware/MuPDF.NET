## About

**MuPDF.NET.PDF4LLM** provides LLM/RAG helpers for [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET): PDF-to-Markdown conversion, layout parsing, and document structure analysis. It is designed for use with RAG (Retrieval-Augmented Generation) pipelines and integration with LLMs.

**Documentation:** [docs.pdf4llm.com](https://docs.pdf4llm.com/) · [.NET installation](https://docs.pdf4llm.com/dotnet/getting-started/installation)

This package extends MuPDF.NET with:

- **PDF-to-Markdown** — Convert PDF pages to Markdown with layout awareness (tables, headers, images)
- **Layout parsing** — Extract document structure (pages, boxes, tables, images) as JSON or structured objects
- **LlamaIndex integration** — `PDFMarkdownReader` for compatibility with LlamaIndex document loading
- **OCR support** — Optional OCR for scanned or image-heavy pages
- **Form fields** — Extract key/value pairs from interactive PDF forms

Install with `dotnet add package MuPDF.NET.PDF4LLM`. MuPDF.NET is installed automatically as a dependency.

For Office/HWP documents (DOC/DOCX, XLS/XLSX, PPT/PPTX, HWP/HWPX), also install
[MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office), call `MuPDFOffice.Unlock(...)`,
then pass those paths to `MuPDF4LLM.ToMarkdown` / `MuPDF4LLM.ToJson` / `MuPDF4LLM.ToText`.

## PyMuPDF Layout (optional)

AI-based page layout uses the Python package [pymupdf-layout](https://pypi.org/project/pymupdf-layout/) via an external worker. Install it once per machine:

```bash
dotnet msbuild -t:MuPDFNetPDF4LLMSetupLayoutPython
```

This creates a per-user Python venv (Windows: `%LOCALAPPDATA%\MuPDF.NET.PDF4LLM\.venv-layout`, Linux/macOS: `~/.local/share/mupdf4llm.net/.venv-layout`) and installs pinned `pymupdf` / `pymupdf-layout` wheels. MuPDF.NET.PDF4LLM discovers that venv automatically.

On Debian/Ubuntu, install system packages first:

```bash
sudo apt install python3-venv python3-pip
```

Alternatively, install `pymupdf-layout` into any Python 3.10+ environment and set `MuPDF4LLM_NET_PYTHON` to that interpreter. If layout is unavailable, MuPDF.NET.PDF4LLM falls back to classic text extraction.

Project-local venv: run `python path/to/setup_layout_python.py --venv .mupdf4llm-net-venv` in your project directory (also auto-discovered).

## License and Copyright

**MuPDF.NET.PDF4LLM** is part of MuPDF.NET and is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. If you determine you cannot meet the requirements of the Artifex Community License, please [contact Artifex](https://artifex.com/contact/mupdf-net-inquiry.php) for more information regarding a commercial license.
