using System.IO;
using PDF4LLM;
using Xunit;

namespace PDF4LLM.Test
{
    [Collection("PDF4LLM")]
    public class TestSce150
    {
        private const string TestClassName = nameof(TestSce150);

        private static string? Doc(string fileName) => _Path.TryForTestClass(fileName, TestClassName);

        private static string Expected(string fileName) =>
            _Path.ForTestClassOrUpstream(fileName, TestClassName);

        private static string NormalizeExpected(string md) => md.Replace("\r", "");

        private static void RunGoldenCompare(string pdfFileName, string expectedFileName)
        {
            string? pdfPath = Doc(pdfFileName);
            if (pdfPath == null)
                return;

            string expected = NormalizeExpected(File.ReadAllText(Expected(expectedFileName)));

            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(true);
                string md = PdfExtractor.ToMarkdown(
                    pdfPath,
                    writeImages: false,
                    embedImages: false,
                    header: false,
                    footer: false);

                Assert.Equal(expected, NormalizeExpected(md));
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_sce_150_1()
        {
            // """Correct sequence of MD stylings."""
            RunGoldenCompare("test_sce_150_1.pdf", "test_sce_150_1.expected.md");
        }

        [Fact]
        public void test_sce_150_2()
        {
            // """Table recognition on OCR'd page."""
            RunGoldenCompare("test_sce_150_2.pdf", "test_sce_150_2.expected.md");
        }

        [Fact]
        public void test_sce_150_3()
        {
            // """No new OCR if old text layer should be kept."""
            RunGoldenCompare("test_sce_150_3.pdf", "test_sce_150_3.expected.md");
        }
    }
}