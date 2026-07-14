using System.Collections.Generic;
using System.Linq;
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
        /// Perform OCR on the given page using RapidOCR detect + recognize.
        /// </summary>
        /// <param name="page">Page to OCR.</param>
        /// <param name="dpi">Rendering DPI for the culled pixmap when none is supplied.</param>
        /// <param name="pixmap">Optional pre-rendered pixmap; created when <c>null</c>.</param>
        /// <param name="language">Reserved for API parity; RapidOCR uses bundled ONNX models.</param>
        /// <param name="keepOcrText">When <c>true</c>, skip OCR if the page already contains OCR spans.</param>
        public static void ExecOcr(
            Page page,
            int dpi = 300,
            Pixmap pixmap = null,
            string language = "eng",
            bool keepOcrText = false)
        {
#if !NET8_0_OR_GREATER
            _ = page; _ = dpi; _ = pixmap; _ = language; _ = keepOcrText;
            throw new System.NotImplementedException(
                "RapidOcrApi.ExecOcr requires .NET 8 or later and the RapidOcrNet package.");
#else
            if (!RapidOcrEngine.IsAvailable)
                return;

            OcrPageHelpers.SpanBuckets buckets = OcrPageHelpers.CollectSpanRects(page, OcrText);
            if (OcrPageHelpers.ShouldSkipForKeepOcr(buckets, keepOcrText))
                return;

            bool ownPixmap = pixmap == null;
            pixmap = OcrPageHelpers.RenderCulledPixmap(page, dpi, buckets.Good, pixmap);
            try
            {
                RapidOcrNet.OcrResult result = RapidOcrEngine.Detect(pixmap);
                if (result?.TextBlocks == null || !result.TextBlocks.Any())
                    return;

                OcrPageHelpers.RedactBadSpans(page, buckets);
                OcrPageHelpers.EnsureOcrFont(page);

                Matrix matrix = OcrPageHelpers.PixmapToPageMatrix(pixmap, page);
                foreach (var block in result.TextBlocks)
                {
                    if (block?.BoxPoints == null || block.BoxPoints.Length < 4)
                        continue;
                    string text = block.Text ?? "";
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    float minX = block.BoxPoints.Min(p => p.X);
                    float minY = block.BoxPoints.Min(p => p.Y);
                    float maxX = block.BoxPoints.Max(p => p.X);
                    float maxY = block.BoxPoints.Max(p => p.Y);
                    Rect rect = OcrPageHelpers.PixmapBoxToPageRect(minX, minY, maxX, maxY, matrix);
                    OcrPageHelpers.InsertOcrLine(page, rect, text);
                }
            }
            finally
            {
                if (ownPixmap)
                    pixmap?.Dispose();
            }
#endif
        }
    }
}
