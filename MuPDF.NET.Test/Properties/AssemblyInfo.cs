using Xunit;

// Native MuPDF interop is not reliably thread-safe under parallel xUnit collections (random AVs).
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// Clear stored MuPDF warnings before each test.
[assembly: MuPDF.NET.Test.ResetMupdfWarnings]