using MuPDF.NET;

namespace PDF4LLM.Ocr
{
    /// <summary>
    /// Same module contract as PDF4LLM.ocr.paddletess_api (duplicate of rapidtess_api in upstream).
    /// </summary>
    public static class PaddleTessApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        public static bool OcrText(Span span) => TesseractApi.OcrText(span);

        public static string GetText(Pixmap pixmap, IRect irect, string language = "eng")
        {
            throw new System.NotImplementedException(
                "PaddleTessApi.GetText requires RapidOCR + Tesseract integration; see PDF4LLM.ocr.paddletess_api.");
        }

        public static void ExecOcr(Page page, int dpi = 300, Pixmap pixmap = null, string language = "eng", bool keepOcrText = false)
        {
            throw new System.NotImplementedException(
                "PaddleTessApi.ExecOcr is not implemented for MuPDF.NET; see PDF4LLM.ocr.paddletess_api.");
        }
    }
}
