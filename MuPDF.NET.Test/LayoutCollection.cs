using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    /// <summary>
    /// Serializes tests that mutate <see cref="Utils.LayoutEnabled"/> (mirrors Python module globals).
    /// </summary>
    [CollectionDefinition("MuPDF.NET.layout", DisableParallelization = true)]
    public class LayoutCollection
    {
    }
}
