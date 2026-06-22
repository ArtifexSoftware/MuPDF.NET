using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using mupdf;

namespace PDF4LLM.Ocr
{
    /// <summary>Analyze a page and decide whether OCR is needed.</summary>
    public static class AnalyzePage
    {
        public const char ReplacementCharacter = '\uFFFD';
        public const string TesseractFontName = "GlyphLessFont";
        public const float BadCharThreshold = 0.05f;
        public const float OcrModelThreshold = 0.93f;

        private static readonly int BlockText = mupdf.mupdf.FZ_STEXT_BLOCK_TEXT;
        private static readonly int BlockImage = mupdf.mupdf.FZ_STEXT_BLOCK_IMAGE;
        private static readonly int BlockVector = mupdf.mupdf.FZ_STEXT_BLOCK_VECTOR;

        /// <summary>Return the probability that these features require OCR.</summary>
        /// <param name="features">Feature dictionary from <see cref="ComputeOcrFeatures.ComputeFeatures"/>.</param>
        public static float PredictOcrProbability(Dictionary<string, float> features) =>
            OcrDecisionModel.Predict(features);

        /// <summary>If this is an OCR text span.</summary>
        /// <param name="span">Text span to classify as OCR-generated.</param>
        public static bool IsOcrSpan(Span span)
        {
            if (span == null)
                return false;
            if (span.Font == TesseractFontName)
                return true;
            uint flags = span.CharFlags;
            if ((flags & (uint)mupdf.mupdf.FZ_STEXT_STROKED) != 0)
                return false;
            if ((flags & (uint)mupdf.mupdf.FZ_STEXT_FILLED) != 0)
                return false;
            return true;
        }

        /// <summary>Analyze the page for the OCR decision.</summary>
        /// <param name="page">Page to analyze.</param>
        /// <param name="blocks">Optional pre-extracted blocks; extracted when <c>null</c>.</param>
        /// <param name="replaceOcr">When <c>true</c>, redact existing OCR spans before re-OCR.</param>
        /// <param name="ocrDpi">DPI reserved for follow-up OCR rendering.</param>
        /// <param name="stats">Optional counter dictionary updated with analysis statistics.</param>
        public static Dictionary<string, object> Analyze(
            Page page,
            List<Block> blocks = null,
            bool replaceOcr = false,
            int ocrDpi = 200,
            Dictionary<string, object> stats = null)
        {
            if (blocks == null)
            {
                TextPage textPage = page.GetTextPage(
                    clip: new Rect(float.NegativeInfinity, float.NegativeInfinity,
                        float.PositiveInfinity, float.PositiveInfinity),
                    flags: Helpers.Utils.FLAGS);
                PageInfo pageInfo = textPage.ExtractDict(null, false);
                blocks = pageInfo.Blocks ?? new List<Block>();
                textPage.Dispose();
            }

            Rect pageRect = page.Rect;
            Rect imgRect = Helpers.Utils.EmptyRect();
            Rect txtRect = imgRect;
            Rect vecRect = imgRect;
            int charsTotal = 0;
            int charsBad = 0;
            float badAreas = 0f;
            float imgArea = 0f;
            float txtArea = 0f;
            float vecArea = 0f;
            int ocrSpans = 0;
            var ocrSpanBoxes = new List<Rect>();

            foreach (Block b in blocks)
            {
                Rect bbox = IntersectRects(pageRect, b.Bbox);
                float area = bbox.Width * bbox.Height;
                if (area == 0f)
                    continue;

                if (b.Type == BlockText)
                {
                    if (b.Lines == null)
                        continue;
                    foreach (Line line in b.Lines)
                    {
                        if (line?.Spans == null)
                            continue;
                        foreach (Span span in line.Spans)
                        {
                            Rect sr = IntersectRects(bbox, span.Bbox);
                            float srArea = sr.Width * sr.Height;
                            if (srArea == 0f)
                                continue;

                            string text = (span.Text ?? "").Trim();
                            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                                continue;

                            charsTotal += text.Length;
                            if (IsOcrSpan(span))
                            {
                                ocrSpans++;
                                ocrSpanBoxes.Add(span.Bbox);
                                continue;
                            }

                            int badChars = text.Count(c => c == ReplacementCharacter);
                            charsBad += badChars;
                            txtRect = JoinRects(txtRect, sr);
                            txtArea += srArea;
                            if (badChars > 0)
                                badAreas += srArea;
                        }
                    }
                    continue;
                }

                if (b.Type == BlockImage)
                {
                    imgRect = JoinRects(imgRect, bbox);
                    imgArea += area;
                    continue;
                }

                if (b.Type == BlockVector)
                {
                    vecRect = JoinRects(vecRect, bbox);
                    vecArea += area;
                }
            }

            Rect covered = JoinRects(JoinRects(imgRect, txtRect), vecRect);
            if (BboxIsEmpty(covered))
            {
                return EmptyResult(covered);
            }

            float coverArea = (covered.X1 - covered.X0) * (covered.Y1 - covered.Y0);
            var analysis = new Dictionary<string, object>
            {
                ["covered"] = covered,
                ["img_joins"] = coverArea > 0 ? Math.Abs(imgRect.Width * imgRect.Height) / coverArea : 0f,
                ["img_area"] = coverArea > 0 ? imgArea / coverArea : 0f,
                ["txt_joins"] = coverArea > 0 ? Math.Abs(txtRect.Width * txtRect.Height) / coverArea : 0f,
                ["txt_area"] = coverArea > 0 ? txtArea / coverArea : 0f,
                ["vec_joins"] = coverArea > 0 ? Math.Abs(vecRect.Width * vecRect.Height) / coverArea : 0f,
                ["vec_area"] = coverArea > 0 ? vecArea / coverArea : 0f,
                ["chars_total"] = charsTotal,
                ["chars_bad"] = charsBad,
                ["bad_areas"] = coverArea > 0 ? badAreas / coverArea : 0f,
                ["ocr_spans"] = ocrSpans,
                ["pixmap"] = null,
            };

            if (ocrSpans > 0)
            {
                if (stats != null)
                    stats["old_ocr"] = stats.TryGetValue("old_ocr", out object o) ? Convert.ToInt32(o) + 1 : 1;
                if (!replaceOcr)
                {
                    return Finish(analysis, false, null, null);
                }

                foreach (Rect r in ocrSpanBoxes)
                    page.AddRedactAnnot(r);
                page.ApplyRedactions(
                    images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE,
                    graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_NONE,
                    text: mupdf.mupdf.PDF_REDACT_TEXT_REMOVE);
                return Finish(analysis, true, "ocr_spans", null);
            }

            if (charsTotal > 0
                && txtArea > 0
                && ((float)charsBad / charsTotal > BadCharThreshold
                    || badAreas / txtArea > BadCharThreshold))
            {
                return Finish(analysis, true, "chars_bad", null);
            }

            if (stats != null)
                stats["model_check"] = stats.TryGetValue("model_check", out object mc) ? Convert.ToInt32(mc) + 1 : 1;

            Dictionary<string, float> features = ComputeOcrFeatures.ComputeFeatures(blocks, pageRect, page);
            float prob = PredictOcrProbability(features);
            bool needsOcr = prob >= OcrModelThreshold;
            if (needsOcr)
                return Finish(analysis, true, "img_text", prob);

            return Finish(analysis, false, null, prob);
        }

        private static Dictionary<string, object> EmptyResult(Rect covered)
        {
            return new Dictionary<string, object>
            {
                ["covered"] = covered,
                ["img_joins"] = 0f,
                ["img_area"] = 0f,
                ["txt_joins"] = 0f,
                ["txt_area"] = 0f,
                ["vec_joins"] = 0f,
                ["vec_area"] = 0f,
                ["chars_total"] = 0,
                ["chars_bad"] = 0,
                ["bad_areas"] = 0f,
                ["ocr_spans"] = 0,
                ["pixmap"] = null,
                ["needs_ocr"] = false,
                ["reason"] = null,
                ["probability"] = null,
            };
        }

        private static Dictionary<string, object> Finish(
            Dictionary<string, object> analysis,
            bool needsOcr,
            string reason,
            float? probability)
        {
            var result = new Dictionary<string, object>(analysis)
            {
                ["needs_ocr"] = needsOcr,
                ["reason"] = reason,
                ["probability"] = probability,
            };
            return result;
        }

        private static Rect IntersectRects(Rect r1, Rect r2) =>
            new Rect(
                Math.Max(r1.X0, r2.X0),
                Math.Max(r1.Y0, r2.Y0),
                Math.Min(r1.X1, r2.X1),
                Math.Min(r1.Y1, r2.Y1));

        private static Rect JoinRects(Rect r1, Rect r2)
        {
            if (BboxIsEmpty(r1))
                return r2;
            if (BboxIsEmpty(r2))
                return r1;
            return new Rect(
                Math.Min(r1.X0, r2.X0),
                Math.Min(r1.Y0, r2.Y0),
                Math.Max(r1.X1, r2.X1),
                Math.Max(r1.Y1, r2.Y1));
        }

        private static bool BboxIsEmpty(Rect bbox) =>
            bbox.X0 >= bbox.X1 || bbox.Y0 >= bbox.Y1;
    }
}
