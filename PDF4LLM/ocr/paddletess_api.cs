using MuPDF.NET;

namespace PDF4LLM.Ocr
{
    /// <summary>PaddleOCR plus Tesseract pipeline (alias of <see cref="RapidTessApi"/> in upstream).</summary>
    public static class PaddleTessApi
    {
        public const char ReplacementUnicode = RapidTessApi.ReplacementUnicode;

        public static bool OcrText(Span span) => RapidTessApi.OcrText(span);

        public static string GetText(Pixmap pixmap, IRect clipRect, string language = "eng") =>
            RapidTessApi.GetText(pixmap, clipRect, language);

        public static void ExecOcr(
            Page page,
            int dpi = 300,
            Pixmap pixmap = null,
            string language = "eng",
            bool keepOcrText = false) =>
            RapidTessApi.ExecOcr(page, dpi, pixmap, language, keepOcrText);
    }
}
