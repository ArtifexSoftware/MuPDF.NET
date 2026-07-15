using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using mupdf;

namespace PDF4LLM.Ocr
{
    /// <summary>Shared helpers for OCR page execution callbacks.</summary>
    internal static class OcrPageHelpers
    {
        internal const string OcrFontName = "myfont";

        private static readonly Lazy<Font> OcrFont = new Lazy<Font>(() => new Font("cjk"));

        internal readonly struct SpanBuckets
        {
            public SpanBuckets(List<Rect> good, List<Rect> fffd, List<Rect> ocr)
            {
                Good = good;
                Fffd = fffd;
                Ocr = ocr;
            }

            public List<Rect> Good { get; }
            public List<Rect> Fffd { get; }
            public List<Rect> Ocr { get; }
        }

        internal static SpanBuckets CollectSpanRects(Page page, Func<Span, bool> isOcrText)
        {
            var good = new List<Rect>();
            var fffd = new List<Rect>();
            var ocr = new List<Rect>();

            using (DisplayList displaylist = page.GetDisplayList())
            using (TextPage stextPage = displaylist.GetTextPage((int)TextFlags.TEXT_ACCURATE_BBOXES))
            {
                PageInfo pageInfo = stextPage.ExtractDict(null, false);
                foreach (Block b in pageInfo.Blocks ?? new List<Block>())
                {
                    if (b?.Lines == null)
                        continue;
                    foreach (Line l in b.Lines)
                    {
                        if (l?.Spans == null)
                            continue;
                        foreach (Span s in l.Spans)
                        {
                            if (s?.Bbox == null)
                                continue;
                            if (isOcrText(s))
                            {
                                ocr.Add(s.Bbox);
                            }
                            else
                            {
                                string text = s.Text ?? "";
                                if (s.Chars != null && s.Chars.Count > 0)
                                    text = string.Join("", s.Chars.Select(c => c.C));
                                if (text.Contains(TesseractApi.ReplacementUnicode))
                                    fffd.Add(s.Bbox);
                                else
                                    good.Add(s.Bbox);
                            }
                        }
                    }
                }
            }

            return new SpanBuckets(good, fffd, ocr);
        }

        internal static bool ShouldSkipForKeepOcr(SpanBuckets buckets, bool keepOcrText) =>
            keepOcrText && buckets.Ocr.Count > 0;

        internal static void RedactBadSpans(Page page, SpanBuckets buckets)
        {
            var redactionRects = new List<Rect>();
            redactionRects.AddRange(buckets.Fffd);
            redactionRects.AddRange(buckets.Ocr);
            if (redactionRects.Count == 0)
                return;

            foreach (Rect sbbox in redactionRects)
                page.AddRedactAnnot(sbbox);
            page.ApplyRedactions(
                images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE,
                graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_NONE,
                text: mupdf.mupdf.PDF_REDACT_TEXT_REMOVE);
        }

        internal static Pixmap RenderCulledPixmap(
            Page page,
            int dpi,
            IEnumerable<Rect> goodSpans,
            Pixmap pixmap)
        {
            if (pixmap != null)
                return pixmap;

            using (DisplayList displaylist = page.GetDisplayList())
                return GetCulledPixmap.GetPixmap(displaylist, dpi, goodSpans, page);
        }

        internal static Matrix PixmapToPageMatrix(Pixmap pix, Page page) =>
            new Rect(pix.IRect).ToRect(page.Rect);

        internal static Rect PixmapBoxToPageRect(float minX, float minY, float maxX, float maxY, Matrix matrix) =>
            new Rect(minX, minY, maxX, maxY) * matrix;

        internal static Rect PixmapBoxToPageRect(float[][] box, Matrix matrix)
        {
            float minX = box.Min(p => p[0]);
            float minY = box.Min(p => p[1]);
            float maxX = box.Max(p => p[0]);
            float maxY = box.Max(p => p[1]);
            return new Rect(minX, minY, maxX, maxY) * matrix;
        }

        static string _insertFontName = OcrFontName;

        internal static void EnsureOcrFont(Page page)
        {
            _insertFontName = OcrFontName;
            try
            {
                byte[] buffer = OcrFont.Value.Buffer;
                if (buffer != null && buffer.Length > 0)
                    page.InsertFont(OcrFontName, fontbuffer: buffer);
                else
                    throw new InvalidOperationException("CJK fallback font buffer is empty.");
            }
            catch (Exception)
            {
                // Some page types (e.g. SVG) cannot embed the CJK fallback buffer.
                _insertFontName = "helv";
                page.InsertFont("helv");
            }
        }

        internal static Matrix AdjustWidthMatrix(string text, float fontsize, Rect rect)
        {
            float tl = OcrFont.Value.TextLength(text, fontsize);
            return tl > 0
                ? new Matrix(rect.Width / tl, 1)
                : Matrix.Identity;
        }

        internal static void InsertOcrLine(Page page, Rect rect, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            float fontsize = rect.Height;
            Matrix mat = AdjustWidthMatrix(text, fontsize, rect);
            Point bl = new Point(rect.X0, rect.Y1);
            page.InsertText(
                bl + new Point(0, -0.2f * fontsize),
                text,
                fontSize: fontsize,
                fontName: _insertFontName,
                morph: new Morph(bl, mat));
        }

        internal static string GetTesseractLineText(Pixmap pixmap, IRect clipRect, string language)
        {
            if (clipRect.IsEmpty)
                return "";

            var expanded = new IRect(clipRect.X0 - 2, clipRect.Y0 - 2, clipRect.X1 + 2, clipRect.Y1 + 2);
            using (var clipPix = new Pixmap(Colorspace.Rgb, expanded))
            {
                clipPix.Copy(pixmap, expanded);
                byte[] data = clipPix.PdfOCR2Bytes(
                    compress: true,
                    language: language,
                    tessdata: TesseractApi.Tessdata);
                using (var doc = new Document(data, fileType: "pdf"))
                    return doc.LoadPage(0).GetText()?.Trim() ?? "";
            }
        }
    }
}
