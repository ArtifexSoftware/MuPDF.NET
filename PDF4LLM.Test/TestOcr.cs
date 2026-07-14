using System.IO;
using MuPDF.NET;
using PDF4LLM;
using PDF4LLM.Ocr;
using Xunit;

namespace PDF4LLM.Test
{
    [Collection("PDF4LLM")]
    public class TestOcr
    {
        private const string TestClassName = nameof(TestOcr);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static bool _ocr_tesseract_available()
        {
            //     try:
            //     except Exception:
            //         tesseract = None
            try
            {
                return !string.IsNullOrEmpty(MuPDF.NET.Utils.TESSDATA_PREFIX);
            }
            catch
            {
                return false;
            }
        }

        private static bool _ocr_rapidocr_onnxruntime_available() => RapidOcrSupport.IsAvailable;

        [Fact]
        public void test_ocr_1()
        {
            //         return
            var mupdfVersion = Constants.MupdfVersion;
            if (mupdfVersion.Major < 1 || (mupdfVersion.Major == 1 && mupdfVersion.Minor < 28))
                return;

            string path = Doc("test_ocr_loremipsum_FFFD.pdf");
            string md = PdfExtractor.ToMarkdown(path);
            //         f.write(md)
            File.WriteAllText(Out("out_test_ocr_1.md"), md);
            string replacement = TesseractApi.ReplacementUnicode.ToString();
            if (_ocr_tesseract_available() || _ocr_rapidocr_onnxruntime_available())
                Assert.DoesNotContain(replacement, md);
            else
                Assert.Contains(replacement, md);
        }

        [Fact]
        public void test_ocr_2()
        {
            string path = Doc("test_ocr_loremipsum_FFFD.pdf");
            string md = PdfExtractor.ToMarkdown(path, useOcr: false);

            File.WriteAllText(Out("out_test_ocr_2.md"), md);
            Assert.Contains(TesseractApi.ReplacementUnicode.ToString(), md);
        }

        [Fact]
        public void test_ocr_3()
        {
            var mupdfVersion = Constants.MupdfVersion;
            if (mupdfVersion.Major < 1 || (mupdfVersion.Major == 1 && mupdfVersion.Minor < 28))
                return;

            string path = Doc("test_ocr_loremipsum_svg.pdf");
            string md = PdfExtractor.ToMarkdown(path, useOcr: true, forceOcr: true);
            string mdNoOcr = PdfExtractor.ToMarkdown(path, useOcr: false);
            File.WriteAllText(Out("out_test_ocr_3.md"), md);
            File.WriteAllText(Out("out_test_ocr_3_no_ocr.md"), mdNoOcr);
            if (_ocr_tesseract_available())
                Assert.True(mdNoOcr.Length < md.Length);
        }
    }
}
