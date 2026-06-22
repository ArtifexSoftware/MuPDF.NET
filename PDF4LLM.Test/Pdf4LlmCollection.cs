using Xunit;

namespace PDF4LLM.Test
{
    /// <summary>
    /// Serializes tests that mutate <see cref="PDF4LLM.PdfExtractor.UseLayout"/> (mirrors Python module globals).
    /// </summary>
    [CollectionDefinition("PDF4LLM", DisableParallelization = true)]
    public class Pdf4LlmCollection
    {
    }
}
