using MuPDF.NET;

namespace PDF4LLM.Ocr
{
    /// <summary>PaddleOCR ONNX pipeline (alias of <see cref="RapidOcrApi"/> in upstream).</summary>
    public static class PaddleOcrApi
    {
        public const char ReplacementUnicode = RapidOcrApi.ReplacementUnicode;

        public static bool OcrText(Span span) => RapidOcrApi.OcrText(span);

        public static void ExecOcr(
            Page page,
            int dpi = 300,
            Pixmap pixmap = null,
            string language = "eng",
            bool keepOcrText = false) =>
            RapidOcrApi.ExecOcr(page, dpi, pixmap, language, keepOcrText);
    }
}
