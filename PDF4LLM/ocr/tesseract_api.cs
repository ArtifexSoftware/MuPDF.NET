using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using mupdf;

namespace PDF4LLM.Ocr
{
    /// <summary>
    /// Tesseract-oriented OCR API and page/span helpers, aligned with pymupdf4llm.ocr.tesseract_api.
    /// </summary>
    public static class TesseractApi
    {
        public const char ReplacementUnicode = '\uFFFD';  // Unicode Replacement Character

        /// <summary>Resolved tessdata directory, or null (pymupdf.get_tessdata() / TESSDATA_PREFIX).</summary>
        public static string Tessdata => Utils.TESSDATA_PREFIX;

        static TesseractApi()
        {
            if (Tessdata == null)
            {
                Console.WriteLine(
                    "Warning: Tesseract OCR is not available. No OCR text will be extracted.");
            }
        }

        /// <summary>
        /// Mirrors pymupdf4llm.ocr.tesseract_api.ocr_text(span).
        /// </summary>
        public static bool OcrText(Span span)
        {
            int flags = (int)(span?.CharFlags ?? 0);
            if (flags == 0)
                flags = (int)(span?.Flags ?? 0);
            if ((flags & 32) == 0 && (flags & 16) == 0)
                return true;
            return false;
        }

        /// <summary>
        /// This callback function performs OCR on the given page.
        ///
        /// It uses RapidOCR for text region detection and Tesseract OCR for text
        /// recognition in each identified region (boundary box).
        ///
        /// If a Pixmap is provided, the DPI parameter is ignored. Otherwise, an RGB
        /// Pixmap is created from the page at the specified DPI.
        /// The DPI parameter is also used if extractable text is present.
        ///
        /// We ensure that legible extractable text is excluded from OCR. If present
        /// on page we make a temporary copy without such text and perform OCR
        /// on that copy.
        /// </summary>
        public static void ExecOcr(
            Page page,
            int dpi = 300,
            Pixmap pixmap = null,
            string language = "eng",
            bool keepOcrText = false)
        {
            string tessdata = Tessdata;
            if (string.IsNullOrEmpty(tessdata))
                return;

            TextPage textPage = page.GetTextPage(flags: (int)TextFlags.TEXT_ACCURATE_BBOXES);
            PageInfo pageInfo = textPage.ExtractDict(null, false);
            List<Block> textBlocks = pageInfo.Blocks ?? new List<Block>();

            // get bboxes with significant legible text on page
            var spans = new List<Rect>();
            var fffdSpans = new List<Rect>();
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
                            if (keepOcrText)
                                spans.Add(new Rect(s.Bbox));
                            else
                                fffdSpans.Add(new Rect(s.Bbox));
                            continue;
                        }

                        string text = s.Text ?? "";
                        if (s.Chars != null && s.Chars.Count > 0)
                            text = string.Join("", s.Chars.Select(c => c.C));
                        if (text.Contains(ReplacementUnicode))
                            fffdSpans.Add(new Rect(s.Bbox));
                        else
                            spans.Add(new Rect(s.Bbox));
                    }
                }
            }

            textPage.Dispose();

            if (spans.Count > 0)
            {
                Document tempPdf = new Document();  // create a temporary PDF in memory
                tempPdf.InsertPdf(
                    page.Parent,
                    fromPage: page.Number,
                    toPage: page.Number);
                Page tempPage = tempPdf.LoadPage(0);
                foreach (Rect sbbox in spans)
                    tempPage.AddRedactAnnot(sbbox);

                tempPage.ApplyRedactions(
                    images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE,
                    graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_NONE,
                    text: mupdf.mupdf.PDF_REDACT_TEXT_REMOVE);

                pixmap = tempPage.GetPixmap(dpi: dpi);
                tempPdf.Close();
            }

            if (pixmap == null)
                pixmap = page.GetPixmap(dpi: dpi);

            if (fffdSpans.Count > 0)
            {
                foreach (Rect sbbox in fffdSpans)
                    page.AddRedactAnnot(sbbox);
                page.ApplyRedactions(
                    images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE,
                    graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_NONE,
                    text: mupdf.mupdf.PDF_REDACT_TEXT_REMOVE);
            }

            byte[] ocrPdfBytes = pixmap.PdfOCR2Bytes(compress: true, language: language, tessdata: tessdata);
            pixmap.Dispose();

            using (Document tempPdf = new Document(ocrPdfBytes, filetype: "pdf"))
            {
                Page tempPage = tempPdf.LoadPage(0);
                tempPage.AddRedactAnnot(tempPage.Rect);
                tempPage.ApplyRedactions(
                    images: mupdf.mupdf.PDF_REDACT_IMAGE_REMOVE,
                    graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_REMOVE_IF_TOUCHED,
                    text: mupdf.mupdf.PDF_REDACT_TEXT_NONE);

                page.ShowPdfPage(page.Rect, tempPdf, 0);
            }
        }
    }
}
