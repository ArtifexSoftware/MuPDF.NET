using System.Collections.Generic;
using PDF4LLM;
using Xunit;

namespace PDF4LLM.Test
{
    /// <summary>Port of <c>tests/test_tablulate.py</c>.</summary>
    [Collection("PDF4LLM")]
    public class TestTablulate
    {
        private const string TestClassName = nameof(TestTablulate);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        [Fact]
        public void test_tablulate_bug()
        {
            // def test_tablulate_bug():
            bool prior = PdfExtractor.UseLayout;
            try
            {
                // pymupdf4llm.use_layout(True)  # default in Python when pymupdf.layout is available
                PdfExtractor.SetUseLayout(true);
                //     # tabulate 0.10.0 made 4llm raise exception with test_tablulate_bug.pdf.
                //     path = os.path.normpath(f'{__file__}/../../tests/test_tablulate_bug.pdf')
                string path = Doc("test_tablulate_bug.pdf");

                string pageList = PdfExtractor.ToText(
                    path,
                    pageChunks: true,
                    useOcr: false,
                    header: false,
                    footer: false,
                    pages: new List<int> { 0 },
                    showProgress: true);

                Assert.NotNull(pageList);
                Assert.NotEmpty(pageList);
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }
    }
}
