using MuPDF.NET;

namespace PDF4LLM.Ocr
{
    /// <summary>
    /// <summary>RapidOCR plus Tesseract combined pipeline.</summary>
    /// Requires rapidocr_onnxruntime in the reference stack; not implemented for .NET.
    /// </summary>
    public static class RapidTessApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        /// <param name="span">Text span to inspect.</param>
        public static bool OcrText(Span span) => TesseractApi.OcrText(span);

        /// <summary>
        /// PDF4LLM.ocr.rapidtess_api.get_text — not ported (Tesseract region OCR + options).
        /// </summary>
        /// <param name="pixmap">Rendered page or region pixmap.</param>
        /// <param name="irect">Integer rectangle clipping the OCR region.</param>
        /// <param name="language">Tesseract language code (e.g. <c>eng</c>).</param>
        public static string GetText(Pixmap pixmap, IRect irect, string language = "eng")
        {
            throw new System.NotImplementedException(
                "RapidTessApi.GetText requires RapidOCR + Tesseract integration; see PDF4LLM.ocr.rapidtess_api.");
        }

        /// <summary>
        /// PDF4LLM.ocr.rapidtess_api.exec_ocr — not ported.
        /// </summary>
        /// <param name="page">Page to OCR.</param>
        /// <param name="dpi">Rendering DPI for the culled pixmap when none is supplied.</param>
        /// <param name="pixmap">Optional pre-rendered pixmap; created when <c>null</c>.</param>
        /// <param name="language">OCR language code passed to the backend.</param>
        /// <param name="keepOcrText">When <c>true</c>, skip OCR if the page already contains OCR spans.</param>
        public static void ExecOcr(Page page, int dpi = 300, Pixmap pixmap = null, string language = "eng", bool keepOcrText = false)
        {
            throw new System.NotImplementedException(
                "RapidTessApi.ExecOcr is not implemented for MuPDF.NET; see PDF4LLM.ocr.rapidtess_api.");
        }
    }
}
