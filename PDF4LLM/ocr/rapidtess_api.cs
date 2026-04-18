using MuPDF.NET;

namespace PDF4LLM.Ocr
{
    /// <summary>
    /// RapidOCR + Tesseract pipeline, aligned with PDF4LLM.ocr.rapidtess_api.
    /// Requires rapidocr_onnxruntime in the reference stack; not implemented for .NET.
    /// </summary>
    public static class RapidTessApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        public static bool OcrText(Span span) => TesseractApi.OcrText(span);

        /// <summary>
        /// PDF4LLM.ocr.rapidtess_api.get_text — not ported (Tesseract region OCR + options).
        /// </summary>
        public static string GetText(Pixmap pixmap, IRect irect, string language = "eng")
        {
            throw new System.NotImplementedException(
                "RapidTessApi.GetText requires RapidOCR + Tesseract integration; see PDF4LLM.ocr.rapidtess_api.");
        }

        /// <summary>
        /// PDF4LLM.ocr.rapidtess_api.exec_ocr — not ported.
        /// </summary>
        public static void ExecOcr(Page page, int dpi = 300, Pixmap pixmap = null, string language = "eng", bool keepOcrText = false)
        {
            throw new System.NotImplementedException(
                "RapidTessApi.ExecOcr is not implemented for MuPDF.NET; see PDF4LLM.ocr.rapidtess_api.");
        }
    }
}
