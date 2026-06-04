using System;
using System.IO;
using MuPDF.NET;
using PDF4LLM;

namespace PDF4LLM.Test
{
    /// <summary>Port of pymupdf4llm/tests/test_137.py</summary>
    [TestFixture]
    public class Test137 : LLMTestBase
    {
        private const string TestClassName = nameof(Test137);
        private static string Doc(string fileName) => ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => ForOutput(fileName, TestClassName);

        [Test]
        public void test_137()
        {
            string path = Doc("test_137.pdf");
            if (!File.Exists(path))
            {
                Assert.Ignore($"Missing fixture: {path}");
                return;
            }

            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(false);
                using (var document = new Document(path))
                {
                    string md = PdfExtractor.ToMarkdown(document, embedImages: true);
                    Assert.That(md, Is.Not.Null);
                    File.WriteAllText(Out("test_137.out_nolayout.md"), md);
                }

                PdfExtractor.SetUseLayout(true);
                using (var document = new Document(path))
                {
                    string md = PdfExtractor.ToMarkdown(document, embedImages: true);
                    Assert.That(md, Is.Not.Null);
                    File.WriteAllText(Out("test_137.out_layout.md"), md);
                }
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }
    }
}
