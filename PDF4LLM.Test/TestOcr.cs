using System.IO;
using MuPDF.NET;
using PDF4LLM;
using PDF4LLM.Ocr;

namespace PDF4LLM.Test
{
    /// <summary>Port of pymupdf4llm/tests/test_ocr.py</summary>
    [TestFixture]
    public class TestOcr : LLMTestBase
    {
        private const string TestClassName = nameof(TestOcr);
        private static string Doc(string fileName) => ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => ForOutput(fileName, TestClassName);

        private static bool _ocr_tesseract_available() =>
            !string.IsNullOrEmpty(Utils.TESSDATA_PREFIX);

        private static bool _ocr_rapidocr_onnxruntime_available() => false;

        [Test]
        public void test_ocr_1()
        {
            string path = Doc("test_ocr_loremipsum_FFFD.pdf");
            if (!File.Exists(path))
            {
                Assert.Ignore($"Missing fixture: {path}");
                return;
            }
            PdfExtractor.SetUseLayout(true);
            string md = PdfExtractor.ToMarkdown(path);
            File.WriteAllText(Out("out_test_ocr_1.md"), md);
            if (_ocr_tesseract_available() || _ocr_rapidocr_onnxruntime_available())
                Assert.That(md, Does.Not.Contain(TesseractApi.ReplacementUnicode.ToString()));
            else
                Assert.That(md, Does.Contain(TesseractApi.ReplacementUnicode.ToString()));
        }

        [Test]
        public void test_ocr_2()
        {
            string path = Doc("test_ocr_loremipsum_FFFD.pdf");
            if (!File.Exists(path))
            {
                Assert.Ignore($"Missing fixture: {path}");
                return;
            }

            string md = PdfExtractor.ToMarkdown(path, useOcr: false);
            File.WriteAllText(Out("out_test_ocr_2.md"), md);
            Assert.That(md, Does.Contain(TesseractApi.ReplacementUnicode.ToString()));
        }

        [Test]
        public void test_ocr_3()
        {
            string path = Doc("test_ocr_loremipsum_svg.pdf");
            if (!File.Exists(path))
            {
                Assert.Ignore($"Missing fixture: {path}");
                return;
            }

            PdfExtractor.SetUseLayout(true);
            string md = PdfExtractor.ToMarkdown(path);
            string mdNoOcr = PdfExtractor.ToMarkdown(path, useOcr: false);
            if (_ocr_tesseract_available())
            {
                Assert.That(mdNoOcr.Length, Is.LessThan(md.Length));
                File.WriteAllText(Out("out_test_ocr_3.md"), md);
                File.WriteAllText(Out("out_test_ocr_3_no_ocr.md"), mdNoOcr);
            }
        }
    }
}
