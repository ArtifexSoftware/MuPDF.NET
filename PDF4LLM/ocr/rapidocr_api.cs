using System.Collections.Generic;
using MuPDF.NET;

namespace PDF4LLM.Ocr
{
    /// <summary>
    /// RapidOCR-only pipeline (reference OCR module contract).
    /// </summary>
    public static class RapidOcrApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        /// <summary>
        /// Keyword arguments passed to RapidOCR (<c>KWARGS</c>).
        /// </summary>
        public static readonly Dictionary<string, object> Kwargs = new Dictionary<string, object>();

        /// <param name="span">Text span to inspect.</param>
        public static bool OcrText(Span span) => TesseractApi.OcrText(span);

        /// <summary>
        /// PDF4LLM.ocr.rapidocr_api.exec_ocr — not ported.
        /// </summary>
        /// <param name="page">Page to OCR.</param>
        /// <param name="dpi">Rendering DPI for the culled pixmap when none is supplied.</param>
        /// <param name="pixmap">Optional pre-rendered pixmap; created when <c>null</c>.</param>
        /// <param name="language">OCR language code passed to the backend.</param>
        /// <param name="keepOcrText">When <c>true</c>, skip OCR if the page already contains OCR spans.</param>
        public static void ExecOcr(Page page, int dpi = 300, Pixmap pixmap = null, string language = "eng", bool keepOcrText = false)
        {
            throw new System.NotImplementedException(
                "RapidOcrApi.ExecOcr is not implemented for MuPDF.NET; see PDF4LLM.ocr.rapidocr_api.");
        }
    }
}
