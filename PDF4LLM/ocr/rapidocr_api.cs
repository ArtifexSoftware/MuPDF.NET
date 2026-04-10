using System.Collections.Generic;
using MuPDF.NET;

namespace PDF4LLM.Ocr
{
    /// <summary>
    /// RapidOCR-only pipeline, aligned with pdf4llm ocr.rapidocr_api.
    /// </summary>
    public static class RapidOcrApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        /// <summary>
        /// Keyword arguments passed to RapidOCR in Python (<c>KWARGS</c>).
        /// </summary>
        public static readonly Dictionary<string, object> Kwargs = new Dictionary<string, object>();

        public static bool OcrText(Span span) => TesseractApi.OcrText(span);

        /// <summary>
        /// PDF4LLM.ocr.rapidocr_api.exec_ocr — not ported.
        /// </summary>
        public static void ExecOcr(Page page, int dpi = 300, Pixmap pixmap = null, string language = "eng", bool keepOcrText = false)
        {
            throw new System.NotImplementedException(
                "RapidOcrApi.ExecOcr is not implemented for MuPDF.NET; see PDF4LLM.ocr.rapidocr_api.");
        }
    }
}
