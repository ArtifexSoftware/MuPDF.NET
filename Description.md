## About

**MuPDF.NET** 4.1 adds **.NET** bindings and abstractions to [MuPDF](https://mupdf.com) **1.28.6**, matching [PyMuPDF](https://pymupdf.io) 2.1.

Programs written in any .NET language (C#, Visual Basic .NET, F#) use the same API shape as PyMuPDF.

Refer to the [MuPDF.NET documentation](https://mupdfnet.readthedocs.io/en/latest/) for usage (see [The Basics](https://mupdfnet.readthedocs.io/en/latest/the-basics/index.html) for common use cases).

Runnable samples are in **[MuPDF.NET.Examples](https://github.com/ArtifexSoftware/MuPDF.NET.Examples)** (`dotnet run --project MuPDF.NET/01-OpenSave`).


## Include PyMuPDF Layout

This is an optional, but **highly recommended**, step to enhance your solution by providing superior layout analysis for your documents.

[PyMuPDF](https://pymupdf.io) includes a layout-aware document engine for AI. Its ML-based layout analysis turns PDFs and Office files into high-quality Markdown for RAG and agents, running fast on CPU and grounding every result to its source page.

### Installation

In your project folder do the following:

- `dotnet msbuild -t:MuPDFNetSetupLayoutPython` (Python 3.11+ on `PATH`) 
- creates a per-user venv and pip-installs **PyMuPDF** `pymupdf==2.1` (the worker; layout is `pymupdf.layout` in that package) plus the empty stub `pymupdf-layout==2.1`
- This will include [PyMuPDF](https://pymupdf.io) in your solution, with [its own license conditions](https://pymupdf.io/licensing)
- Check with `dotnet msbuild -t:MuPDFNetVerifyLayoutPython`
- Full steps: [MuPDF.NET README — Layout analysis](https://github.com/ArtifexSoftware/MuPDF.NET#layout-analysis-optional).


### Improved Document Conversion

With Layout enabled discover superior results when converting documents to Markdown, JSON, plain text, or chunks for LLM and RAG pipelines (`Document.ToMarkdown`, `Document.ToJson`, `Document.ToText`, `Document.ToChunks`). 



## Office Files 

See the separate commercial package [MuPDF.NET.Office](https://mupdf.com/mupdf-dotnet/office) **2.1.0** ([NuGet](https://www.nuget.org/packages/MuPDF.NET.Office)) for Office (DOC/DOCX, XLS/XLSX, PPT/PPTX) and HWP/HWPX file support. 



## License and Copyright

**MuPDF.NET** is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. If you determine you cannot meet the requirements of the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md), please [contact Artifex](https://artifex.com/contact/mupdf-net) for more information regarding a commercial license.


---




[MuPDF.NET](https://mupdf.com/mupdf-dotnet), [PyMuPDF](https://pymupdf.io) and [MuPDF](https://mupdf.com) are maintained and developed by [Artifex Software, Inc.](https://artifex.com)
