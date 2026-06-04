using System.Collections.Generic;
using System.IO;
using PDF4LLM;

namespace PDF4LLM.Test
{
    /// <summary>Port of pymupdf4llm/tests/test_tablulate.py</summary>
    [TestFixture]
    public class TestTablulate : LLMTestBase
    {
        private const string TestClassName = nameof(TestTablulate);
        private static string Doc(string fileName) => ForTestClass(fileName, TestClassName);

        [Test]
        public void test_tablulate_bug()
        {
            string path = Doc("test_tablulate_bug.pdf");
            if (!File.Exists(path))
            {
                Assert.Ignore($"Missing fixture: {path}");
                return;
            }

            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(true);
                string json = PdfExtractor.ToText(
                    path,
                    pageChunks: true,
                    useOcr: false,
                    header: false,
                    footer: false,
                    pages: new List<int> { 0 },
                    showProgress: false);

                Assert.That(json, Is.Not.Null.And.Not.Empty);
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }
    }
}
