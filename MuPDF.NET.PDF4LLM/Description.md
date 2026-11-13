## About

**Deprecated.** **MuPDF.NET.PDF4LLM** will be removed. 2.1 is an empty type-forward stub for [MuPDF.NET](https://www.nuget.org/packages/MuPDF.NET) **4.1**, matching PyPI [pymupdf4llm 2.1](https://pypi.org/project/pymupdf4llm/).

**Do not add this package to new projects.** PDF-to-Markdown, layout JSON, and plain-text extract live on `Document.ToMarkdown` / `ToJson` / `ToText` in `MuPDF.NET.dll`. Office/HWP: [MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office) **2.1.0**. Existing `PackageReference Include="MuPDF.NET.PDF4LLM"` lines still compile until this package is removed.

**Documentation:** [docs.pdf4llm.com](https://docs.pdf4llm.com/) · [MuPDF.NET README](https://github.com/ArtifexSoftware/MuPDF.NET)

```bash
dotnet add package MuPDF.NET
```

For Office/HWP documents (DOC/DOCX, XLS/XLSX, PPT/PPTX, HWP/HWPX), also install
[MuPDF.NET.Office](https://www.nuget.org/packages/MuPDF.NET.Office) **2.1.0**, call `MuPDFOffice.Unlock(...)`,
then `new Document(path).ToMarkdown()`.

## PyMuPDF Layout

Layout setup is on the **MuPDF.NET** package. See [MuPDF.NET README — Layout analysis](https://github.com/ArtifexSoftware/MuPDF.NET#layout-analysis-optional) (`dotnet msbuild -t:MuPDFNetSetupLayoutPython`).

## License and Copyright

**MuPDF.NET.PDF4LLM** is part of MuPDF.NET and is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) and commercial license agreements. If you determine you cannot meet the requirements of the Artifex Community License, please [contact Artifex](https://artifex.com/contact/mupdf-net-inquiry.php) for more information regarding a commercial license.
