## About

**MuPDF.NET** 4.1 adds **.NET** bindings and abstractions to [MuPDF](https://mupdf.com) **1.28.6**, matching [PyMuPDF](https://pypi.org/project/pymupdf/) 2.1.

LLM extract is in this package: `Document.ToMarkdown`, `ToJson`, `ToText`, `ToChunks`, plus `Utils.LoadAiAsync` / `Utils.MarkdownToPdf` on net8.

**Layout (optional):** from your project directory, `dotnet msbuild -t:MuPDFNetSetupLayoutPython` (Python 3.11+ on `PATH`). That creates a per-user venv (`%LOCALAPPDATA%\MuPDF.NET\.venv-layout` on Windows, `~/.local/share/mupdf.net/.venv-layout` on Linux/macOS) and installs pymupdf **2.1**. Check with `dotnet msbuild -t:MuPDFNetVerifyLayoutPython`. Or set `MUPDF_NET_PYTHON` to any Python that already has pymupdf 2.1. Without layout, extract falls back to classic text. Full steps: [MuPDF.NET README — Layout analysis](https://github.com/ArtifexSoftware/MuPDF.NET#layout-analysis-optional).

Office/HWP is the separate commercial package [MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office) **2.1.0**. [MuPDF.NET.PDF4LLM](https://www.nuget.org/packages/MuPDF.NET.PDF4LLM) is **deprecated** and will be removed; do not add it to new projects.

Programs written in any .NET language (C#, Visual Basic .NET, F#) use the same API shape as PyMuPDF.

Refer to the [MuPDF.NET documentation](https://mupdfnet.readthedocs.io/en/latest/) for usage.

MuPDF.NET, PyMuPDF and MuPDF are maintained and developed by [Artifex Software, Inc.](https://artifex.com)


## License and Copyright

**MuPDF.NET** is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. If you determine you cannot meet the requirements of the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md), please [contact Artifex](https://artifex.com/contact/mupdf-net-inquiry.php) for more information regarding a commercial license.