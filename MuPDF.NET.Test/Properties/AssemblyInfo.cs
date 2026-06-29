using Xunit;

// Native MuPDF interop is not reliably thread-safe under parallel xUnit collections (random AVs).
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// PyMuPDF tests/conftest.py: clear mupdf_warnings() before each test.
[assembly: MuPDF.NET.Test.ResetMupdfWarnings]
