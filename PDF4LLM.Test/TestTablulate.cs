using System.Collections.Generic;
using PDF4LLM;
using Xunit;

namespace PDF4LLM.Test
{
    [Collection("PDF4LLM")]
    public class TestTablulate
    {
        private const string TestClassName = nameof(TestTablulate);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        [Fact]
        public void test_tablulate_bug()
        {
            bool prior = PdfExtractor.UseLayout;
            try
            {
                // layout package.use_layout(True) # default in Python when layout bridge is available
                PdfExtractor.SetUseLayout(true);
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