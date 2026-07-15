using System.Linq;
using MuPDF.NET;

namespace PDF4LLM.Ocr
{
    /// <summary>RapidOCR plus Tesseract combined pipeline.</summary>
    public static class RapidTessApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        /// <param name="span">Text span to inspect.</param>
        public static bool OcrText(Span span) => TesseractApi.OcrText(span);

        /// <summary>
        /// Use Tesseract to extract text from a bounding box of the pixmap.
        /// </summary>
        /// <param name="pixmap">Rendered page or region pixmap.</param>
        /// <param name="clipRect">Integer rectangle clipping the OCR region.</param>
        /// <param name="language">Tesseract language code (e.g. <c>eng</c>).</param>
        public static string GetText(Pixmap pixmap, IRect clipRect, string language = "eng")
        {
            if (string.IsNullOrEmpty(TesseractApi.Tessdata))
                return "";
            return OcrPageHelpers.GetTesseractLineText(pixmap, clipRect, language);
        }

        /// <summary>
        /// Detect text regions with RapidOCR and recognize each line with Tesseract.
        /// </summary>
        /// <param name="page">Page to OCR.</param>
        /// <param name="dpi">Rendering DPI for the culled pixmap when none is supplied.</param>
        /// <param name="pixmap">Optional pre-rendered pixmap; created when <c>null</c>.</param>
        /// <param name="language">Tesseract language code passed to the backend.</param>
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
                "RapidTessApi.ExecOcr requires .NET 8 or later and the RapidOcrNet package.");
#else
            if (!RapidOcrEngine.IsAvailable || string.IsNullOrEmpty(TesseractApi.Tessdata))
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

                Matrix matrix = OcrPageHelpers.PixmapToPageMatrix(pixmap, page);
                var tessResults = new System.Collections.Generic.List<(Rect rect, string text)>();
                foreach (var block in result.TextBlocks)
                {
                    if (block?.BoxPoints == null || block.BoxPoints.Length < 4)
                        continue;

                    float minX = block.BoxPoints.Min(p => p.X);
                    float minY = block.BoxPoints.Min(p => p.Y);
                    float maxX = block.BoxPoints.Max(p => p.X);
                    float maxY = block.BoxPoints.Max(p => p.Y);
                    var irect = new IRect(
                        (int)minX,
                        (int)minY,
                        (int)maxX,
                        (int)maxY);
                    string text = GetText(pixmap, irect, language);
                    if (string.IsNullOrWhiteSpace(text))
                        continue;
                    Rect rect = OcrPageHelpers.PixmapBoxToPageRect(minX, minY, maxX, maxY, matrix);
                    tessResults.Add((rect, text));
                }

                if (tessResults.Count == 0)
                    return;

                OcrPageHelpers.EnsureOcrFont(page);
                foreach ((Rect rect, string text) in tessResults)
                    OcrPageHelpers.InsertOcrLine(page, rect, text);
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
