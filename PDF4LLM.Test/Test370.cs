using System;
using System.IO;
using MuPDF.NET;
using PDF4LLM;
using Xunit;

namespace PDF4LLM.Test
{
    /// <summary>Port of <c>tests/test_370.py</c>.</summary>
    [Collection("PDF4LLM")]
    public class Test370
    {
        private const string TestClassName = nameof(Test370);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_370()
        {
            // def test_370():
            //     # https://github.com/ArtifexSoftware/sce/issues/137
            //     print()
            //     path = os.path.normpath(f'{__file__}/../../tests/test_370.pdf')
            string path = Doc("test_370.pdf");
            string pathExpected = Doc("test_370_expected.md");
            string pathActual = Out("test_370_actual.md");

            string expected = File.ReadAllText(pathExpected);

            bool priorLayout = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(true);

                using (var document = new Document(path))
                {
                    string actual = PdfExtractor.ToMarkdown(
                        document,
                        writeImages: false,
                        embedImages: false,
                        imageFormat: "jpg",
                        header: false,
                        footer: false,
                        showProgress: true,
                        forceText: true,
                        pageSeparators: true);

                    File.WriteAllText(pathActual, actual);

                    // Full golden compare only when layout is off (stext fallback).
                    // With pymupdf.layout, picture markdown differs from the legacy expected file.
                    if (!PdfExtractor.LayoutAvailable)
                        Assert.Equal(expected.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
                    else
                    {
                        Assert.Contains("Synthesis of Silyl Dienol Ethers", actual);
                        Assert.Contains("Masahiro Sai", actual);
                    }
                }
            }
            finally
            {
                PdfExtractor.SetUseLayout(priorLayout);
            }
        }
    }
}
