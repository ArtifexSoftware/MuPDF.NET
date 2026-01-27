using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using mupdf;

namespace MuPDF.NET.LLM.Helpers
{
    /// <summary>
    /// OCR decision and repair utilities.
    /// Ported and adapted from the Python module helpers/check_ocr.py in pymupdf4llm.
    /// </summary>
    public static class CheckOcr
    {
        public static int FLAGS = (int)(
            mupdf.mupdf.FZ_STEXT_COLLECT_STYLES |
            mupdf.mupdf.FZ_STEXT_COLLECT_VECTORS |
            (int)TextFlags.TEXT_PRESERVE_IMAGES |
            (int)TextFlags.TEXT_ACCURATE_BBOXES
            // | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
        );

        /// <summary>
        /// Return OCR'd span text using Tesseract.
        /// </summary>
        /// <param name="page">MuPDF Page</param>
        /// <param name="bbox">MuPDF Rect or its sequence</param>
        /// <param name="dpi">Resolution for OCR image</param>
        /// <returns>The OCR-ed text of the bbox.</returns>
        public static string GetSpanOcr(Page page, Rect bbox, int dpi = 300)
        {
            // Step 1: Make a high-resolution image of the bbox.
            Pixmap pix = page.GetPixmap(dpi: dpi, clip: bbox);
            byte[] ocrPdfBytes = pix.PdfOCR2Bytes(true);
            
            Document ocrPdf = new Document("pdf", ocrPdfBytes);
            Page ocrPage = ocrPdf.LoadPage(0);
            string text = ocrPage.GetText();
            text = text.Replace("\n", " ").Trim(); // Get rid of line breaks
            
            ocrPage.Dispose();
            ocrPdf.Close();
            pix.Dispose();
            
            return text;
        }

        /// <summary>
        /// Repair text blocks with missing glyphs using OCR.
        /// 
        /// TODO: Support non-linear block structure.
        /// </summary>
        public static List<Block> RepairBlocks(List<Block> inputBlocks, Page page, int dpi = 300)
        {
            List<Block> repairedBlocks = new List<Block>();
            
            foreach (var block in inputBlocks)
            {
                if (block.Type != 0) // Accept non-text blocks as is
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

                                if (!spanText.Contains(Utils.REPLACEMENT_CHARACTER))
                                    continue;

                                int spanTextLen = spanText.Length;
                                string newText = GetSpanOcr(page, span.Bbox, dpi);
                                if (newText.Length > spanTextLen)
                                    newText = newText.Substring(0, spanTextLen);

                                if (span.Chars != null && span.Chars.Count > 0)
                                {
                                    // Rebuild chars array
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
                                            // Copy other properties as needed
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

        /// <summary>
        /// Determine whether the page contains text worthwhile to OCR.
        /// </summary>
        /// <param name="page">MuPDF.NET Page object</param>
        /// <param name="dpi">DPI used for rasterization *if* we decide to OCR</param>
        /// <param name="covered">Area to consider for text presence</param>
        /// <returns>
        /// The full-page transformation matrix, the full-page pixmap and a
        /// boolean indicating whether the page is photo-like (True) or
        /// text-like (False).
        /// </returns>
        public static (Matrix matrix, Pixmap pix, bool photo) GetPageImage(
            Page page, 
            int dpi = 150, 
            Rect covered = null)
        {
            if (covered == null)
                covered = page.Rect;

            IRect irect = new IRect((int)covered.X0, (int)covered.Y0, 
                                   (int)covered.X1, (int)covered.Y1);
            
            // Make a gray pixmap of the covered area
            Rect clipRect = new Rect(covered);
            Pixmap pixCovered = page.GetPixmap(colorSpace: "gray", clip: clipRect);
            
            // Convert to byte array for image quality analysis (convert to numpy array)
            int width = pixCovered.W;
            int height = pixCovered.H;
            byte[] samples = pixCovered.SAMPLES;
            
            // Create 2D array for image quality analysis
            byte[,] gray = new byte[height, width];
            int sampleIndex = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    gray[y, x] = samples[sampleIndex++];
                }
            }

            // Run photo checks
            var scores = ImageQuality.AnalyzeImage(gray);
            double score = scores.ContainsKey("score") ? scores["score"].value : 0;
            
            if (score >= 3)
            {
                pixCovered.Dispose();
                return (new Matrix(1, 0, 0, 1, 0, 0), null, true); // Identity matrix
            }
            else
            {
                Pixmap pix = page.GetPixmap(dpi: dpi);
                IRect pixRect = new IRect(0, 0, pix.W, pix.H);
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

        /// <summary>
        /// Decide whether a MuPDF.NET page should be OCR'd.
        /// </summary>
        /// <param name="page">MuPDF.NET page object</param>
        /// <param name="dpi">DPI used for rasterization</param>
        /// <param name="vectorThresh">Minimum number of vector paths to suggest glyph simulation</param>
        /// <param name="imageCoverageThresh">Fraction of page area covered by images to trigger OCR</param>
        /// <param name="textReadabilityThresh">Fraction of readable characters to skip OCR</param>
        /// <param name="blocks">Output of page.get_text("dict") if already available</param>
        /// <returns>Dictionary with decision and diagnostic flags</returns>
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
                ["transform"] = new Matrix(1, 0, 0, 1, 0, 0), // Identity matrix
                ["pixmap"] = null,
            };

            Rect pageRect = page.Rect;
            float pageArea = Math.Abs(pageRect.Width * pageRect.Height);

            // Analyze the page
            var analysis = Utils.AnalyzePage(page, blocks);

            // Return if page is completely blank
            Rect covered = analysis["covered"] as Rect;
            if (Utils.BboxIsEmpty(covered))
            {
                decision["should_ocr"] = false;
                return decision;
            }

            // Return if page has been OCR'd already
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

            // Preset OCR if very little text area exists
            // Less than 5% text area in covered area
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

            // We need OCR and do a final check for potential text presence
            if (!(bool)decision["has_text"])
            {
                // Rasterize and check for photo versus text-heaviness
                var (matrix, pix, photo) = GetPageImage(page, dpi, covered);

                if (photo)
                {
                    // This seems to be a non-text picture page
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
