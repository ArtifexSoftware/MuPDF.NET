using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using mupdf;

namespace MuPDF.NET.PDF4LLM.Ocr
{
    /// <summary>Pixmap from a page with text culled from given rectangles.</summary>
    public static class GetCulledPixmap
    {
        const int MaxPixels = 10; // maximum pixel count for OCR, in millions

        /// <summary>
        /// Compute the maximum integer DPI such that a page pixmap has fewer than
        /// <paramref name="maxPixels"/> pixels.
        /// </summary>
        public static int MaxDpiForPage(Rect mediabox, long maxPixels = 0)
        {
            float wPt = mediabox.X1 - mediabox.X0;
            float hPt = mediabox.Y1 - mediabox.Y0;
            if (wPt <= 0 || hPt <= 0)
                return 0;

            double a = wPt / 72.0;
            double b = hPt / 72.0;
            double dpiEst = Math.Sqrt(maxPixels / (a * b));
            return (int)dpiEst;
        }

        /// <summary>True when the pixmap is empty or near-white only.</summary>
        public static bool PixmapIsEmpty(Pixmap pix, int threshold = 250)
        {
            if (pix == null)
                return true;

            object colorsObj = pix.ColorCount(colors: true);
            if (!(colorsObj is Dictionary<byte[], int> colors) || colors.Count == 0)
                return true;
            if (colors.Count > 1)
                return false;

            KeyValuePair<byte[], int> only = colors.First();
            byte[] rgb = only.Key;
            if (rgb == null || rgb.Length == 0)
                return true;
            return rgb.Min(c => (int)c) >= threshold;
        }

        /// <summary>
        /// Make a pixmap from the display list ignoring text in <paramref name="rects"/>.
        /// </summary>
        /// <returns>Pixmap and whether it is empty / near-white after culling.</returns>
        public static (Pixmap pix, bool empty) GetPixmap(
            DisplayList displaylist,
            int dpi = 150,
            IEnumerable<Rect> rects = null,
            Page page = null,
            int emptyThreshold = 250)
        {
            Rect mediabox = displaylist?.Rect ?? page?.Rect ?? new Rect(0, 0, 1, 1);
            List<Rect> rectList = rects?
                .Where(r => r != null && !r.IsEmpty)
                .ToList() ?? new List<Rect>();
            if (rectList.Count == 0)
                rectList = new List<Rect> { new Rect(mediabox) };

            string envMax = Environment.GetEnvironmentVariable("PYMUPDF_MAX_OCRSIZE")
                ?? Environment.GetEnvironmentVariable("MuPDF4LLM_MAX_OCRSIZE");
            int maxMillions = MaxPixels;
            if (!string.IsNullOrEmpty(envMax) && int.TryParse(envMax, out int parsed) && parsed > 0)
                maxMillions = parsed;
            long maxPixels = (long)maxMillions * 1_000_000L;
            int maxDpi = MaxDpiForPage(mediabox, maxPixels: maxPixels);
            if (maxDpi > 0 && dpi > maxDpi)
            {
                Console.WriteLine(
                    $"Page too large for dpi={dpi}, reducing to dpi={maxDpi}. Results may be impaired.");
                dpi = maxDpi;
            }

            float zoom = dpi / 72f;
            var matrix = new Matrix(zoom, zoom);
            Pixmap pix;

            if (page == null)
            {
                pix = displaylist.GetPixmap(matrix, alpha: false);
            }
            else
            {
                // Fallback: redact good text on a temporary page copy, then render.
                using (var tempPdf = new Document())
                {
                    tempPdf.InsertPdf(page.Parent, fromPage: page.Number, toPage: page.Number);
                    Page tempPage = tempPdf.LoadPage(0);
                    tempPage.RemoveRotation();
                    foreach (Rect sbbox in rectList)
                        tempPage.AddRedactAnnot(sbbox);
                    tempPage.ApplyRedactions(
                        images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE,
                        graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_NONE,
                        text: mupdf.mupdf.PDF_REDACT_TEXT_REMOVE);
                    pix = tempPage.GetPixmap(dpi: dpi, alpha: false);
                }
            }

            pix?.SetDpi(dpi, dpi);
            bool empty = PixmapIsEmpty(pix, threshold: emptyThreshold);
            return (pix, empty);
        }
    }
}
