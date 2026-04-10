using System.Collections.Generic;
using MuPDF.NET;

namespace MuPDF.NET4LLM.Ocr
{
    /// <summary>
    /// Same module contract as mupdf4llm.ocr.paddleocr_api (duplicate of rapidocr_api in upstream).
    /// </summary>
    public static class PaddleOcrApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        public static readonly Dictionary<string, object> Kwargs = new Dictionary<string, object>();

        public static bool OcrText(Span span) => TesseractApi.OcrText(span);

        public static void ExecOcr(Page page, int dpi = 300, Pixmap pixmap = null, string language = "eng", bool keepOcrText = false)
        {
            throw new System.NotImplementedException(
                "PaddleOcrApi.ExecOcr is not implemented for MuPDF.NET; see mupdf4llm.ocr.paddleocr_api.");
        }
    }
}
