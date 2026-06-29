using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using mupdf;

namespace PDF4LLM.Ocr
{
    /// <summary>Pixmap from a page with text culled from given rectangles.</summary>
    public static class GetCulledPixmap
    {
        /// <summary>
        /// Make a pixmap from the display list ignoring text in <paramref name="rects"/>.
        /// </summary>
        /// <param name="displaylist">Page display list.</param>
        /// <param name="dpi">Target DPI when no pixmap is supplied.</param>
        /// <param name="rects">Legible text regions to omit from rendering.</param>
        /// <param name="page">
        /// Source page; required when <paramref name="rects"/> is non-empty because
        /// MuPDF.NET does not expose <c>fz_new_pixmap_from_display_list_culling_text2</c>.
        /// </param>
        public static Pixmap GetPixmap(
            DisplayList displaylist,
            int dpi = 300,
            IEnumerable<Rect> rects = null,
            Page page = null)
        {
            List<Rect> rectList = rects?
                .Where(r => r != null && !r.IsEmpty)
                .ToList() ?? new List<Rect>();

            float zoom = dpi / 72f;
            var matrix = new Matrix(zoom, zoom);

            if (rectList.Count == 0)
                return displaylist.GetPixmap(matrix, alpha: false);

            if (page == null)
                return displaylist.GetPixmap(matrix, alpha: false);

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
                return tempPage.GetPixmap(dpi: dpi, alpha: false);
            }
        }
    }
}
