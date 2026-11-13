using System.IO;
using MuPDF.NET;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
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

        private static bool _ocr_rapidocr_onnxruntime_available() => Utils.RapidOcrAvailable;

        [Fact]
        public void test_ocr_1()
        {
            //         return
            var mupdfVersion = Constants.MupdfVersion;
            if (mupdfVersion.Major < 1 || (mupdfVersion.Major == 1 && mupdfVersion.Minor < 28))
                return;

            string path = Doc("test_ocr_loremipsum_FFFD.pdf");
            string md = Utils.ToMarkdown(path);
            //         f.write(md)
            File.WriteAllText(Out("out_test_ocr_1.md"), md);
            string replacement = Utils.OcrReplacementChar.ToString();
            if (_ocr_tesseract_available() || _ocr_rapidocr_onnxruntime_available())
                Assert.DoesNotContain(replacement, md);
            else
                Assert.Contains(replacement, md);
        }

        [Fact]
        public void test_ocr_2()
        {
            string path = Doc("test_ocr_loremipsum_FFFD.pdf");
            string md = Utils.ToMarkdown(path, useOcr: false);

            File.WriteAllText(Out("out_test_ocr_2.md"), md);
            Assert.Contains(Utils.OcrReplacementChar.ToString(), md);
        }

        [Fact]
        public void test_ocr_3()
        {
            var mupdfVersion = Constants.MupdfVersion;
            if (mupdfVersion.Major < 1 || (mupdfVersion.Major == 1 && mupdfVersion.Minor < 28))
                return;

            string path = Doc("test_ocr_loremipsum_svg.pdf");
            // Python: to_markdown(path) then to_markdown(path, use_ocr=False).
            // With vector IsRect populated, AnalyzePage sets needs_ocr and OCR runs.
            string md = Utils.ToMarkdown(path);
            string mdNoOcr = Utils.ToMarkdown(path, useOcr: false);
            File.WriteAllText(Out("out_test_ocr_3.md"), md);
            File.WriteAllText(Out("out_test_ocr_3_no_ocr.md"), mdNoOcr);
            if (_ocr_tesseract_available())
                Assert.True(mdNoOcr.Length < md.Length);
        }
    }
}
