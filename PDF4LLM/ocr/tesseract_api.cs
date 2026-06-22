using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using mupdf;

namespace PDF4LLM.Ocr
{
    /// <summary>Tesseract OCR integration.</summary>
    public static class TesseractApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        public static string Tessdata => Utils.TESSDATA_PREFIX;

        static TesseractApi()
        {
            if (Tessdata == null)
            {
                Console.WriteLine(
                    "Warning: Tesseract OCR is not available. No OCR text will be extracted.");
            }
        }

        /// <summary>Whether span text came from OCR.</summary>
        /// <param name="span">Text span to inspect.</param>
        public static bool OcrText(Span span)
        {
            uint flags = span?.CharFlags ?? 0;
            if ((flags & (uint)mupdf.mupdf.FZ_STEXT_STROKED) != 0)
                return false;
            if ((flags & (uint)mupdf.mupdf.FZ_STEXT_FILLED) != 0)
                return false;
            return true;
        }

        /// <summary>
        /// Perform OCR on the given page, excluding legible extractable text.
        /// </summary>
        /// <param name="page">Page to OCR.</param>
        /// <param name="dpi">Rendering DPI for the culled pixmap when none is supplied.</param>
        /// <param name="pixmap">Optional pre-rendered pixmap; created when <c>null</c>.</param>
        /// <param name="language">Tesseract language code (e.g. <c>eng</c>).</param>
        /// <param name="keepOcrText">When <c>true</c>, skip OCR if the page already contains OCR spans.</param>
        public static void ExecOcr(
            Page page,
            int dpi = 300,
            Pixmap pixmap = null,
            string language = "eng",
            bool keepOcrText = false)
        {
            if (string.IsNullOrEmpty(Tessdata))
                return;

            using (DisplayList displaylist = page.GetDisplayList())
            using (TextPage stextPage = displaylist.GetTextPage((int)TextFlags.TEXT_ACCURATE_BBOXES))
            {
                PageInfo pageInfo = stextPage.ExtractDict(null, false);
                List<Block> textBlocks = pageInfo.Blocks ?? new List<Block>();

                var spans = new List<Rect>();
                var fffdSpans = new List<Rect>();
                var ocrSpans = new List<Rect>();

                foreach (Block b in textBlocks)
                {
                    if (b?.Lines == null)
                        continue;
                    foreach (Line l in b.Lines)
                    {
                        if (l?.Spans == null)
                            continue;
                        foreach (Span s in l.Spans)
                        {
                            if (OcrText(s))
                            {
                                ocrSpans.Add(s.Bbox);
                            }
                            else
                            {
                                string text = s.Text ?? "";
                                if (s.Chars != null && s.Chars.Count > 0)
                                    text = string.Join("", s.Chars.Select(c => c.C));
                                if (text.Contains(ReplacementUnicode))
                                    fffdSpans.Add(s.Bbox);
                                else
                                    spans.Add(s.Bbox);
                            }
                        }
                    }
                }

                if (ocrSpans.Count > 0 && keepOcrText)
                    return;

                bool ownPixmap = pixmap == null;
                if (ownPixmap)
                    pixmap = GetCulledPixmap.GetPixmap(displaylist, dpi, spans, page);

                try
                {
                    byte[] ocrPdfBytes = pixmap.PdfOCR2Bytes(
                        compress: true,
                        language: language,
                        tessdata: Tessdata);

                    using (Document tempPdf = new Document(ocrPdfBytes, fileType: "pdf"))
                    {
                        Page tempPage = tempPdf.LoadPage(0);
                        tempPage.AddRedactAnnot(tempPage.Rect);
                        tempPage.ApplyRedactions(
                            images: mupdf.mupdf.PDF_REDACT_IMAGE_REMOVE,
                            graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_REMOVE_IF_TOUCHED,
                            text: mupdf.mupdf.PDF_REDACT_TEXT_NONE);

                        page.AddRedactAnnot(page.Rect);
                        page.ApplyRedactions(
                            images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE,
                            graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_NONE,
                            text: mupdf.mupdf.PDF_REDACT_TEXT_REMOVE);

                        page.ShowPdfPage(page.Rect, tempPdf, 0);
                    }
                }
                finally
                {
                    if (ownPixmap)
                        pixmap?.Dispose();
                }
            }
        }
    }
}
