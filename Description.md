## About

**MuPDF.NET** 4.1 adds **.NET** bindings and abstractions to [MuPDF](https://mupdf.com) **1.28.6**, matching [PyMuPDF](https://pymupdf.io) 2.1.

Convert documents to Markdown, JSON, plain text, or chunks for LLM and RAG pipelines (`Document.ToMarkdown`, `ToJson`, `ToText`, `ToChunks`). `Utils.LoadAiAsync` indexes PDFs and `Utils.MarkdownToPdf` writes Markdown to PDF.

**Layout (recommended/optional):** `dotnet msbuild -t:MuPDFNetSetupLayoutPython` (Python 3.11+ on `PATH`) creates a per-user venv and pip-installs **PyMuPDF** `pymupdf==2.1` (the worker; layout is `pymupdf.layout` in that package) plus the empty stub `pymupdf-layout==2.1`. That is PyMuPDF in your solution, with [its own license](https://pymupdf.io); skip setup and use classic extract if you do not want that. Check with `dotnet msbuild -t:MuPDFNetVerifyLayoutPython`. Full steps: [MuPDF.NET README — Layout analysis](https://github.com/ArtifexSoftware/MuPDF.NET#layout-analysis-optional).

Office/HWP is the separate commercial package [MuPDF.NET.Office](https://mupdf.com/mupdf-dotnet/office) **2.1.0** ([NuGet](https://www.nuget.org/packages/MuPDF.NET.Office)). [MuPDF.NET.PDF4LLM](https://www.nuget.org/packages/MuPDF.NET.PDF4LLM) is **deprecated** and will be removed; do not add it to new projects.

Programs written in any .NET language (C#, Visual Basic .NET, F#) use the same API shape as PyMuPDF.

Refer to the [MuPDF.NET product page](https://mupdf.com/mupdf-dotnet) and [API reference](https://mupdfnet.readthedocs.io/en/latest/) for usage.

Runnable samples are in **[MuPDF.NET.Examples](https://github.com/ArtifexSoftware/MuPDF.NET.Examples)** (`dotnet run --project MuPDF.NET/01-OpenSave`).

MuPDF.NET, PyMuPDF and MuPDF are maintained and developed by [Artifex Software, Inc.](https://artifex.com)


## License and Copyright

**MuPDF.NET** is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. If you determine you cannot meet the requirements of the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md), please [contact Artifex](https://artifex.com/contact/mupdf-net) for more information regarding a commercial license.