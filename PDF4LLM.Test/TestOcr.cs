using System.IO;
using MuPDF.NET;
using PDF4LLM;
using PDF4LLM.Ocr;
using Xunit;

namespace PDF4LLM.Test
{
    /// <summary>Port of <c>tests/test_ocr.py</c>.</summary>
    [Collection("PDF4LLM")]
    public class TestOcr
    {
        private const string TestClassName = nameof(TestOcr);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static bool _ocr_tesseract_available()
        {
            // def _ocr_tesseract_available():
            //     try:
            //         tesseract = pymupdf.get_tessdata()
            //     except Exception:
            //         tesseract = None
            //     return bool(tesseract)
            try
            {
                return !string.IsNullOrEmpty(MuPDF.NET.Utils.TESSDATA_PREFIX);
            }
            catch
            {
                return false;
            }
        }

        private static bool _ocr_rapidocr_onnxruntime_available()
        {
            // def _ocr_rapidocr_onnxruntime_available():
            //     try:
            //         import rapidocr_onnxruntime
            //     except Exception:
            //         rapidocr_onnxruntime = None
            //     return bool(rapidocr_onnxruntime)
            return false;
        }

        [Fact]
        public void test_ocr_1()
        {
            // def test_ocr_1():
            //     if pymupdf.mupdf_version_tuple < (1, 28):
            //         print(f'test_ocr_1(): not running because {pymupdf.mupdf_version=} < 1.28.')
            //         return
            var mupdfVersion = Constants.MupdfVersion;
            if (mupdfVersion.Major < 1 || (mupdfVersion.Major == 1 && mupdfVersion.Minor < 28))
                return;

            //     path = os.path.normpath(f'{g_root}//tests/test_ocr_loremipsum_FFFD.pdf')
            string path = Doc("test_ocr_loremipsum_FFFD.pdf");
            //     md = pymupdf4llm.to_markdown(path)
            string md = PdfExtractor.ToMarkdown(path);
            //     with open(f'{g_root}/tests/out_test_ocr_1.md', 'w', encoding='utf-8') as f:
            //         f.write(md)
            File.WriteAllText(Out("out_test_ocr_1.md"), md);
            //     if _ocr_tesseract_available() or _ocr_rapidocr_onnxruntime_available():
            //         assert REPLACEMENT_UNICODE not in md
            //     else:
            //         assert REPLACEMENT_UNICODE in md
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
