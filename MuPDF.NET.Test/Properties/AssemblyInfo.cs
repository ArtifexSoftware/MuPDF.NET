using Xunit;

// Native MuPDF interop is not reliably thread-safe under parallel xUnit collections (random AVs).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
