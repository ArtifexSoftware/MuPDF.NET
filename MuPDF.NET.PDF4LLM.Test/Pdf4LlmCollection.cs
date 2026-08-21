using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    /// <summary>
    /// Serializes tests that mutate <see cref="MuPDF4LLM.UseLayout"/> (mirrors Python module globals).
    /// </summary>
    [CollectionDefinition("MuPDF.NET.PDF4LLM", DisableParallelization = true)]
    public class Pdf4LlmCollection
    {
    }
}
