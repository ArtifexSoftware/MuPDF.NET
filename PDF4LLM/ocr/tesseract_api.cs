using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using mupdf;
using Char = MuPDF.NET.Char;
using PDF4LLMHelpers = global::PDF4LLM.Helpers;

namespace PDF4LLM.Ocr
{
    /// <summary>
    /// Tesseract-oriented OCR API and page/span helpers, aligned with PDF4LLM.ocr.tesseract_api.
    /// </summary>
    public static class TesseractApi
    {
        public const char ReplacementUnicode = '\uFFFD';

        /// <summary>
        /// Mirrors PDF4LLM.ocr.tesseract_api.ocr_text(span).
        /// </summary>
        public static bool OcrText(Span span)
        {
            int flags = span?.Chars != null && span.Chars.Count > 0
                ? (int)(span.Flags)
                : (int)(span?.Flags ?? 0);
            return (flags & 32) == 0 && (flags & 16) == 0;
        }

        /// <summary>
        /// Full-page OCR callback from PDF4LLM (redaction + pdfocr_tobytes pipeline).
        /// Not ported; use <see cref="CheckOcr"/> for span repair and OCR decisions.
        /// </summary>
        public static void ExecOcr(Page page, int dpi = 300, Pixmap pixmap = null, string language = "eng", bool keepOcrText = false)
        {
            throw new NotImplementedException(
                "TesseractApi.ExecOcr (PDF4LLM.ocr.tesseract_api.exec_ocr) is not implemented for MuPDF.NET; use CheckOcr for span-level repair.");
        }
    }

    /// <summary>
    /// OCR decision and repair utilities used by the layout pipeline (MuPDF.NET / Tesseract).
    /// </summary>
    public static class CheckOcr
    {
        public static int FLAGS = (int)(
            mupdf.mupdf.FZ_STEXT_COLLECT_STYLES |
            mupdf.mupdf.FZ_STEXT_COLLECT_VECTORS |
            (int)TextFlags.TEXT_PRESERVE_IMAGES |
            (int)TextFlags.TEXT_ACCURATE_BBOXES
        );

        public static string GetSpanOcr(Page page, Rect bbox, int dpi = 300)
        {
            Pixmap pix = page.GetPixmap(dpi: dpi, clip: bbox);
            byte[] ocrPdfBytes = pix.PdfOCR2Bytes(true);

            Document ocrPdf = new Document("pdf", ocrPdfBytes);
            Page ocrPage = ocrPdf.LoadPage(0);
            string text = ocrPage.GetText();
            text = text.Replace("\n", " ").Trim();

            ocrPage.Dispose();
            ocrPdf.Close();
            pix.Dispose();

            return text;
        }

        public static List<Block> RepairBlocks(List<Block> inputBlocks, Page page, int dpi = 300)
        {
            List<Block> repairedBlocks = new List<Block>();

            foreach (var block in inputBlocks)
            {
                if (block.Type != 0)
                {
                    repairedBlocks.Add(block);
                    continue;
                }

                if (block.Lines != null)
                {
                    foreach (var line in block.Lines)
                    {
                        if (line.Spans != null)
                        {
                            foreach (var span in line.Spans)
                            {
                                string spanText = "";
                                if (span.Chars != null && span.Chars.Count > 0)
                                {
                                    spanText = string.Join("", span.Chars.Select(c => c.C));
                                }
                                else
                                {
                                    spanText = span.Text ?? "";
                                }

                                if (!spanText.Contains(PDF4LLMHelpers.Utils.REPLACEMENT_CHARACTER))
                                    continue;

                                int spanTextLen = spanText.Length;
                                string newText = GetSpanOcr(page, span.Bbox, dpi);
                                if (newText.Length > spanTextLen)
                                    newText = newText.Substring(0, spanTextLen);

                                if (span.Chars != null && span.Chars.Count > 0)
                                {
                                    List<Char> newChars = new List<Char>();
                                    int minLen = Math.Min(newText.Length, span.Chars.Count);
                                    for (int i = 0; i < minLen; i++)
                                    {
                                        Char oldChar = span.Chars[i];
                                        Char newChar = new Char
                                        {
                                            C = newText[i],
                                            Origin = oldChar.Origin,
                                            Bbox = oldChar.Bbox,
                                        };
                                        newChars.Add(newChar);
                                    }
                                    span.Chars = newChars;
                                }
                                else
                                {
                                    span.Text = newText;
                                }
                            }
                        }
                    }
                }
                repairedBlocks.Add(block);
            }

            return repairedBlocks;
        }

        public static (Matrix matrix, Pixmap pix, bool photo) GetPageImage(
            Page page,
            int dpi = 150,
            Rect covered = null)
        {
            if (covered == null)
                covered = page.Rect;

            Rect clipRect = new Rect(covered);
            Pixmap pixCovered = page.GetPixmap(colorSpace: "gray", clip: clipRect);

            int width = pixCovered.W;
            int height = pixCovered.H;
            byte[] samples = pixCovered.SAMPLES;

            byte[,] gray = new byte[height, width];
            int sampleIndex = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    gray[y, x] = samples[sampleIndex++];
                }
            }

            var scores = PDF4LLMHelpers.ImageQuality.AnalyzeImage(gray);
            double score = scores.ContainsKey("score") ? scores["score"].value : 0;

            if (score >= 3)
            {
                pixCovered.Dispose();
                return (new Matrix(1, 0, 0, 1, 0, 0), null, true);
            }
            else
            {
                Pixmap pix = page.GetPixmap(dpi: dpi);
                Matrix matrix = new Matrix(
                    page.Rect.Width / pix.W,
                    0,
                    0,
                    page.Rect.Height / pix.H,
                    0,
                    0
                );
                pixCovered.Dispose();
                return (matrix, pix, false);
            }
        }

        public static Dictionary<string, object> ShouldOcrPage(
            Page page,
            int dpi = 150,
            float vectorThresh = 0.9f,
            float imageCoverageThresh = 0.9f,
            float textReadabilityThresh = 0.9f,
            List<Block> blocks = null)
        {
            var decision = new Dictionary<string, object>
            {
                ["should_ocr"] = false,
                ["has_ocr_text"] = false,
                ["has_text"] = false,
                ["readable_text"] = false,
                ["image_covers_page"] = false,
                ["has_vector_chars"] = false,
                ["transform"] = new Matrix(1, 0, 0, 1, 0, 0),
                ["pixmap"] = null,
            };

            var analysis = PDF4LLMHelpers.Utils.AnalyzePage(page, blocks);

            Rect covered = analysis["covered"] as Rect;
            if (PDF4LLMHelpers.Utils.BboxIsEmpty(covered))
            {
                decision["should_ocr"] = false;
                return decision;
            }

            int ocrSpans = (int)analysis["ocr_spans"];
            if (ocrSpans > 0)
            {
                decision["has_ocr_text"] = true;
                decision["should_ocr"] = false;
                return decision;
            }

            float txtArea = (float)analysis["txt_area"];
            int charsTotal = (int)analysis["chars_total"];
            float txtJoins = (float)analysis["txt_joins"];
            float vecArea = (float)analysis["vec_area"];
            float imgArea = (float)analysis["img_area"];
            int charsBad = (int)analysis["chars_bad"];

            if (txtArea < 0.05f && charsTotal < 200 && txtJoins < 0.3f)
            {
                if (vecArea >= vectorThresh)
                {
                    decision["should_ocr"] = true;
                    decision["has_vector_chars"] = true;
                }
                if (imgArea >= imageCoverageThresh)
                {
                    decision["should_ocr"] = true;
                    decision["image_covers_page"] = true;
                }
            }
            else if (charsTotal >= 200)
            {
                decision["has_text"] = true;
                float readability = 1.0f - (float)charsBad / charsTotal;
                if (readability >= textReadabilityThresh)
                {
                    decision["readable_text"] = true;
                    decision["should_ocr"] = false;
                }
                else
                {
                    decision["readable_text"] = false;
                    decision["should_ocr"] = true;
                }
            }

            if (!(bool)decision["should_ocr"])
                return decision;

            if (!(bool)decision["readable_text"] && (bool)decision["has_text"])
                return decision;

            if (!(bool)decision["has_text"])
            {
                var (matrix, pix, photo) = GetPageImage(page, dpi, covered);

                if (photo)
                {
                    decision["should_ocr"] = false;
                    decision["pixmap"] = null;
                }
                else
                {
                    decision["should_ocr"] = true;
                    decision["transform"] = matrix;
                    decision["pixmap"] = pix;
                }
            }

            return decision;
        }
    }
}
