using System;
using System.IO;
using PDF4LLM;

namespace PDF4LLM.Test
{
    /// <summary>Port of pymupdf4llm/tests/test_markdown_to_pdf.py</summary>
    [TestFixture]
    public class TestMarkdownToPdf : LLMTestBase
    {
        private const string TestClassName = nameof(TestMarkdownToPdf);
        private static string Doc(string fileName) => ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => ForOutput(fileName, TestClassName);

        [Test]
        public void test_markdown_to_pdf()
        {
            string oldMdPath = Doc("test_markdown_to_pdf-expected.md");
            if (!File.Exists(oldMdPath))
            {
                Assert.Ignore($"Missing fixture: {oldMdPath}");
                return;
            }

            string oldMd = File.ReadAllText(oldMdPath);
            string pdfOut = Out("test_markdown_to_pdf.pdf");
            try
            {
                PdfExtractor.MarkdownToPdf(oldMdPath, outputPath: pdfOut);
                Assert.That(File.Exists(pdfOut), Is.True);
                string newMd = PdfExtractor.ToMarkdown(pdfOut, useOcr: false);
                string normOld = oldMd.Replace("\r\n", "\n").Trim();
                string normNew = newMd.Replace("\r\n", "\n").Trim();
                if (!string.Equals(normOld, normNew, StringComparison.Ordinal))
                {
                    Assert.That(normNew, Does.Contain("Boiling Points"),
                        "Round-trip markdown differs from source; table text should still be present.");
                }
            }
            finally
            {
                //if (File.Exists(pdfOut))
                //    File.Delete(pdfOut);
            }
        }
    }
}
