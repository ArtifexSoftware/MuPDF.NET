using PDF4LLM;
using Xunit;

namespace PDF4LLM.Test
{
    [Collection("PDF4LLM")]
    public class TestSce156
    {
        private const string TestClassName = nameof(TestSce156);

        [Fact]
        public void test_sce_156()
        {
            // Python installs rapidocr then runs to_markdown with page_chunks and OCR.
            // PDF4LLM: smoke-test the same API surface (must not throw).
            string? path = _Path.TryForTestClass("test_sce_156.pdf", TestClassName);
            if (path == null)
                return;

            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(true);
                string result = PdfExtractor.ToMarkdown(
                    path,
                    pageChunks: true,
                    showProgress: false,
                    useOcr: true);
                Assert.False(string.IsNullOrEmpty(result));
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }
    }
}