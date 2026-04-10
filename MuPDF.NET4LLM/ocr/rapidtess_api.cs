using MuPDF.NET;

namespace MuPDF.NET4LLM.Ocr
{
    /// <summary>
    /// RapidOCR + Tesseract pipeline, aligned with mupdf4llm.ocr.rapidtess_api.
    /// Requires rapidocr_onnxruntime in Python; not implemented for .NET.
    /// </summary>
    public static class RapidTessApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        public static bool OcrText(Span span) => TesseractApi.OcrText(span);

        /// <summary>
        /// mupdf4llm.ocr.rapidtess_api.get_text — not ported (Tesseract region OCR + options).
        /// </summary>
        public static string GetText(Pixmap pixmap, IRect irect, string language = "eng")
        {
            throw new System.NotImplementedException(
                "RapidTessApi.GetText requires RapidOCR + Tesseract integration; see mupdf4llm.ocr.rapidtess_api.");
        }

        /// <summary>
        /// mupdf4llm.ocr.rapidtess_api.exec_ocr — not ported.
        /// </summary>
        public static void ExecOcr(Page page, int dpi = 300, Pixmap pixmap = null, string language = "eng", bool keepOcrText = false)
        {
            throw new System.NotImplementedException(
                "RapidTessApi.ExecOcr is not implemented for MuPDF.NET; see mupdf4llm.ocr.rapidtess_api.");
        }
    }
}
