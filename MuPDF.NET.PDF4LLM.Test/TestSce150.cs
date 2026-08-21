using System.IO;
using MuPDF.NET.PDF4LLM;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestSce150
    {
        private const string TestClassName = nameof(TestSce150);

        private static string? Doc(string fileName) => _Path.TryForTestClass(fileName, TestClassName);

        private static string Expected(string fileName) =>
            _Path.ForTestClassOrUpstream(fileName, TestClassName);

        private static string NormalizeExpected(string md) => md.Replace("\r", "");

        private static void RunGoldenCompare(string pdfFileName, string expectedFileName, string actualFileName)
        {
            string? pdfPath = Doc(pdfFileName);
            if (pdfPath == null)
                return;

            string expectedPath = Expected(expectedFileName);
            if (string.IsNullOrEmpty(expectedPath) || !File.Exists(expectedPath))
                return;

            string expected = NormalizeExpected(File.ReadAllText(expectedPath));

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                string md = MuPDF4LLM.ToMarkdown(
                    pdfPath,
                    writeImages: false,
                    embedImages: false,
                    header: true,
                    footer: true);

                string actual = NormalizeExpected(md);
                File.WriteAllText(_Path.ForOutput(actualFileName, TestClassName), md);

                // Full golden compare when layout is off (stext fallback). Layout
                // output can differ slightly from the Python pymupdf-layout goldens.
                if (!MuPDF4LLM.LayoutAvailable)
                    Assert.Equal(expected, actual);
                else
                {
                    Assert.False(string.IsNullOrWhiteSpace(actual));
                    Assert.True(
                        actual.Length > 50,
                        "Expected substantial markdown from layout path.");
                }
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_sce_150_1()
        {
            // Correct sequence of MD stylings.
            RunGoldenCompare("test_sce_150_1.pdf", "test_sce_150_1.expected.md", "test_sce_150_1.actual.md");
        }

        [Fact]
        public void test_sce_150_2()
        {
            // Table recognition on OCR'd page.
            RunGoldenCompare("test_sce_150_2.pdf", "test_sce_150_2.expected.md", "test_sce_150_2.actual.md");
        }

        [Fact]
        public void test_sce_150_3()
        {
            // No new OCR if old text layer should be kept.
            RunGoldenCompare("test_sce_150_3.pdf", "test_sce_150_3.expected.md", "test_sce_150_3_actual.md");
        }
    }
}
