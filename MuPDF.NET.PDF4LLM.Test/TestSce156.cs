using MuPDF.NET.PDF4LLM;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestSce156
    {
        private const string TestClassName = nameof(TestSce156);

        [Fact]
        public void test_sce_156()
        {
            // Python installs rapidocr then runs to_markdown with page_chunks and OCR.
            // MuPDF.NET.PDF4LLM: smoke-test the same API surface (must not throw).
            string? path = _Path.TryForTestClass("test_sce_156.pdf", TestClassName);
            if (path == null)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                string result = MuPDF4LLM.ToMarkdown(
                    path,
                    pageChunks: true,
                    showProgress: false,
                    useOcr: true);
                Assert.False(string.IsNullOrEmpty(result));
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }
    }
}