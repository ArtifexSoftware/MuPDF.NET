using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using MuPDF.NET;
using mupdf;
using Newtonsoft.Json;

namespace PDF4LLM.Ocr
{
    /// <summary>Analyze a page and decide whether OCR is needed.</summary>
    public static class AnalyzePage
    {
        private static readonly int FLAGS =
            0
            | (int)TextFlags.TEXT_ACCURATE_BBOXES
            | (int)TextFlags.TEXT_PRESERVE_IMAGES
            | mupdf.mupdf.FZ_STEXT_COLLECT_VECTORS;

        // MuPDF version of standard gray colorspace
        // private static readonly Colorspace GRAY = Colorspace.Gray;

        private const string Type3FontName = "Type3"; // MuPDF starts the fontname with this string
        public const string TesseractFontName = "GlyphLessFont";
        public const char ReplacementCharacter = '\uFFFD';

        private static readonly uint TextStroked = (uint)mupdf.mupdf.FZ_STEXT_STROKED;
        private static readonly uint TextFilled = (uint)mupdf.mupdf.FZ_STEXT_FILLED;
        private static readonly int BlockText = mupdf.mupdf.FZ_STEXT_BLOCK_TEXT;
        private static readonly int BlockImage = mupdf.mupdf.FZ_STEXT_BLOCK_IMAGE;
        private static readonly int BlockVector = mupdf.mupdf.FZ_STEXT_BLOCK_VECTOR;

        // Thresholds
        public const float BadCharThreshold = 0.05f; // >=5% bad chars suggests OCR

        // Return needs_ocr as True if the probability is at least this:
        public const float OcrModelThreshold = 0.93f;

        // The model file is in our folder!
        // _MODEL_PATH = Path(__file__).parent / "ocr_decision_model.onnx"
        private static InferenceSession _session;
        private static string _inputName = "";
        private static readonly object SessionLock = new object();

        private static (InferenceSession session, string inputName) GetSession()
        {
            if (_session != null)
                return (_session, _inputName);

            lock (SessionLock)
            {
                if (_session != null)
                    return (_session, _inputName);

                string modelPath = OcrDecisionModel.ResolveModelPath();
                if (modelPath == null || !File.Exists(modelPath))
                    return (null, "");

                SessionOptions opts = new SessionOptions();
                opts.InterOpNumThreads = 1;
                opts.IntraOpNumThreads = 1;
                _session = new InferenceSession(modelPath, opts);
                _inputName = _session.InputMetadata.Keys.First();
                return (_session, _inputName);
            }
        }

        /// <summary>Return the probability that these features require OCR.</summary>
        public static float PredictOcrProbability(Dictionary<string, float> features)
        {
            InferenceSession session;
            string inputName;
            (session, inputName) = GetSession();
            if (session == null || string.IsNullOrEmpty(inputName) || features == null)
                return 0f;

            try
            {
                // x = np.array([[features[f] for f in FEATURE_NAMES]], dtype=np.float32)
                string[] featureNames = ComputeOcrFeatures.FeatureNames;
                var x = new float[featureNames.Length];
                for (int i = 0; i < featureNames.Length; i++)
                    x[i] = features.TryGetValue(featureNames[i], out float value) ? value : 0f;

                var inputTensor = new DenseTensor<float>(x, new[] { 1, featureNames.Length });
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(inputName, inputTensor),
                };

                // probas = session.run(None, {input_name: x})[1]  # output[1] = probability
                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs))
                {
                    Tensor<float> probas = results[1].AsTensor<float>();
                    return probas[0, 1];
                }
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>Stage 2: Separate OCR check for large images.</summary>
        /// <remarks>Currently not in use.</remarks>
        /// <param name="imageBlocks">Image blocks in page.get_text("dict") format.</param>
        /// <param name="prob">OCR probability for rendered full page.</param>
        /// <param name="threshold">Probability threshold. If larger recommend OCR.</param>
        /// <returns>do OCR, probability, check count</returns>
        public static (bool doOcr, float probability, int checkCount) CheckImages(
            List<Block> imageBlocks,
            float prob,
            float threshold = OcrModelThreshold)
        {
            float bestProb = prob; // probability for rendered page
            int i = -1;

            for (i = 0; i < imageBlocks.Count; i++)
            {
                Block block = imageBlocks[i];
                // Iterate image blocks.
                // Make a 128x128 GRAY pixmap of the image.
                using (Pixmap small = MakeGray128Pixmap(block))
                {
                    if (small == null)
                        continue;

                    // compute the features and predict OCR probability
                    Dictionary<string, float> features = ComputeOcrFeatures.ComputeFeatures(
                        new List<Block> { block },
                        block.Bbox,
                        small);
                    float imgProb = PredictOcrProbability(features);
                    bestProb = Math.Max(bestProb, imgProb);

                    if (bestProb >= threshold)
                        return (true, bestProb, i); // Early Exit if OCR is probable
                }
            }

            return (false, bestProb, i);
        }

        private static Pixmap MakeGray128Pixmap(Block block)
        {
            if (block?.Image == null)
                return null;

            Pixmap pix = new Pixmap(block.Image);
            try
            {
                if (pix.Alpha != 0)
                {
                    Pixmap noAlpha = new Pixmap(pix, 0);
                    pix.Dispose();
                    pix = noAlpha;
                }

                if (block.Mask != null) // Add mask if present
                {
                    Pixmap mask = new Pixmap(block.Mask);
                    try
                    {
                        Pixmap masked = new Pixmap(pix, mask);
                        Pixmap maskedNoAlpha = new Pixmap(masked, 0);
                        masked.Dispose();
                        pix.Dispose();
                        pix = maskedNoAlpha;
                    }
                    catch
                    {
                        // mask = None
                    }
                    finally
                    {
                        mask.Dispose();
                    }
                }

                Pixmap gray = new Pixmap(ColorSpace.csGRAY, pix);
                pix.Dispose();
                pix = gray;
                Pixmap small = new Pixmap(pix, 128, 128); // downscale Pixmap
                pix.Dispose();
                return small;
            }
            catch
            {
                pix?.Dispose();
                return null;
            }
        }

        //     """Make a pixmap from the page removing "good" text."""
        //
        //     ctm = mupdf.fz_make_matrix(dpi / 72, 0, 0, dpi / 72, 0, 0)
        //
        //     pm = mupdf.fz_new_pixmap_from_display_list_culling_text2(
        //         displaylist, ctm, GRAY, 0, rects
        //     )
        //

        /// <summary>If this is an OCR text span.</summary>
        public static bool IsOcrSpan(Span span)
        {
            if (span == null)
                return false;
            return span.Font == TesseractFontName
                || (
                    true
                    && (span.CharFlags & TextStroked) == 0
                    && (span.CharFlags & TextFilled) == 0
                );
        }

        /// <summary>Speedy version using indices.</summary>
        public static Rect IntersectRects(Rect r1, Rect r2, bool bboxOnly = false)
        {
            Rect bbox = new Rect(
                Math.Max(r1.X0, r2.X0),
                Math.Max(r1.Y0, r2.Y0),
                Math.Min(r1.X1, r2.X1),
                Math.Min(r1.Y1, r2.Y1));
            return bboxOnly ? bbox : bbox;
        }

        /// <summary>Speedy version using indices.</summary>
        public static Rect JoinRects(Rect r1, Rect r2, bool bboxOnly = false)
        {
            Rect bbox = new Rect(
                Math.Min(r1.X0, r2.X0),
                Math.Min(r1.Y0, r2.Y0),
                Math.Max(r1.X1, r2.X1),
                Math.Max(r1.Y1, r2.Y1));
            return bboxOnly ? bbox : bbox;
        }

        /// <summary>Speedy version using indices.</summary>
        public static bool BboxIsEmpty(Rect bbox) =>
            bbox.X0 >= bbox.X1 || bbox.Y0 >= bbox.Y1;

        /// <summary>Analyze the page for the OCR decision.</summary>
        /// <param name="page">Page to analyze.</param>
        /// <param name="blocks">Output of page.get_text("dict") if already available.</param>
        /// <param name="replaceOcr">If true, we should make a new OCR text layer.</param>
        /// <param name="ocrDpi">DPI reserved for follow-up OCR rendering.</param>
        /// <param name="stats">If given fill in execution information (debugging).</param>
        /// <returns>
        /// A dict with analysis results. The area-related float values are
        /// computed as fractions of the total covered area.
        /// </returns>
        public static Dictionary<string, object> Analyze(
            Page page,
            List<Block> blocks = null,
            bool replaceOcr = false,
            int ocrDpi = 200,
            Dictionary<string, object> stats = null)
        {
            // --------------------------------------------------------------------
            // Main analysis
            // --------------------------------------------------------------------
            if (blocks == null) // make "dict" text extraction if not provided
            {
                // stextpage = displaylist.get_textpage(flags=FLAGS)
                // blocks = textpage.extractDICT()["blocks"]
                var pageDict = page.GetText(
                    "dict",
                    clip: Utils.INFINITE_RECT(),
                    flags: FLAGS) as PageInfo;
                blocks = pageDict?.Blocks ?? new List<Block>();
            }

            Rect pageRect = page.Rect;
            Rect imgRect = Helpers.Utils.EmptyRect(); // joined image bboxes
            Rect txtRect = imgRect; // joined text span bboxes
            Rect vecRect = imgRect; // joined suspicious vector bboxes
            int charsTotal = 0; // total character count
            int charsBad = 0; // bad character count
            float badAreas = 0.0f; // sum of areas of text spans having bad characters
            float imgArea = 0.0f; // sum of image block areas
            float txtArea = 0.0f; // sum of all text span bbox areas
            float vecArea = 0.0f; // sum of suspicious vector block areas
            int ocrSpans = 0; // count text spans with OCR flags
            var ocrSpanBoxes = new List<Rect>();
            var badCharBoxes = new List<Rect>();
            var goodCharBoxes = new List<Rect>();
            var imageBlocks = new List<Block>(); // currently not used

            foreach (Block b in blocks)
            {
                Rect bbox = IntersectRects(pageRect, b.Bbox);
                float area = bbox.Width * bbox.Height;
                if (area == 0f)
                    continue;

                // Text block: we analyze text spans for bad characters and OCR flags.
                if (b.Type == BlockText)
                {
                    if (b.Lines != null)
                    {
                        foreach (Line l in b.Lines)
                        {
                            if (l?.Spans == null)
                                continue;
                            foreach (Span s in l.Spans)
                            {
                                Rect sr = IntersectRects(bbox, s.Bbox);
                                float srArea = sr.Width * sr.Height;
                                if (srArea == 0f)
                                    continue;
                                string text = (s.Text ?? "").Trim();
                                if (string.IsNullOrEmpty(text) || text.All(char.IsWhiteSpace))
                                    continue; // ignore spans having no relevant text
                                charsTotal += text.Length; // total character count
                                // OCR layer / invisible text
                                if (IsOcrSpan(s))
                                {
                                    ocrSpans++;
                                    ocrSpanBoxes.Add(s.Bbox);
                                    continue;
                                }

                                // bad character count
                                int badChars = text.Count(c => c == ReplacementCharacter);
                                charsBad += badChars;

                                txtRect = JoinRects(txtRect, sr);
                                txtArea += srArea;
                                if (badChars > 0)
                                {
                                    // add area of span area if it contains bad characters
                                    badAreas += srArea;
                                    badCharBoxes.Add(s.Bbox);
                                }
                                else
                                {
                                    goodCharBoxes.Add(s.Bbox);
                                }
                            }
                        }
                    }
                    continue;
                }

                // Image block: We only look at its area now.
                // OCR decisions based on image content disabled for now.
                if (b.Type == BlockImage)
                {
                    // Image block
                    imgRect = JoinRects(imgRect, bbox);
                    imgArea += area;
                    //     image_blocks.append(b)
                    continue;
                }

                if (b.Type == BlockVector)
                {
                    // Vector block
                    vecRect = JoinRects(vecRect, bbox);
                    vecArea += area;
                    continue;
                }
            }

            // the rectangle on page covered by content
            Rect covered = imgRect | txtRect | vecRect;
            if (BboxIsEmpty(covered))
            {
                // no content at all ? return early with empty covered area
                return new Dictionary<string, object>
                {
                    ["covered"] = covered,
                    ["img_joins"] = 0.0f,
                    ["img_area"] = 0.0f,
                    ["txt_joins"] = 0.0f,
                    ["txt_area"] = 0.0f,
                    ["vec_joins"] = 0.0f,
                    ["vec_area"] = 0.0f,
                    ["chars_total"] = 0,
                    ["chars_bad"] = 0,
                    ["bad_areas"] = 0.0f,
                    ["ocr_spans"] = 0,
                    ["pixmap"] = null,
                    ["needs_ocr"] = false,
                    ["reason"] = null,
                    ["probability"] = null,
                };
            }

            float coverArea = (covered.X1 - covered.X0) * (covered.Y1 - covered.Y0);

            var analysis = new Dictionary<string, object>
            {
                ["covered"] = covered,
                ["img_joins"] = coverArea > 0 ? imgRect.Abs() / coverArea : 0.0f,
                ["img_area"] = coverArea > 0 ? imgArea / coverArea : 0.0f,
                ["txt_joins"] = coverArea > 0 ? txtRect.Abs() / coverArea : 0.0f,
                ["txt_area"] = coverArea > 0 ? txtArea / coverArea : 0.0f,
                ["vec_joins"] = coverArea > 0 ? vecRect.Abs() / coverArea : 0.0f,
                ["vec_area"] = coverArea > 0 ? vecArea / coverArea : 0.0f,
                ["chars_total"] = charsTotal,
                ["chars_bad"] = charsBad,
                ["bad_areas"] = coverArea > 0 ? badAreas / coverArea : 0.0f,
                ["ocr_spans"] = ocrSpans,
                ["pixmap"] = null,
            };

            // --- final OCR decision ---

            if (ocrSpans > 0)
            {
                // This page has previously been OCRed.
                // If replace_ocr is False, we keep the existing OCR layer
                // and accept the page as is.
                if (stats != null)
                    stats["old_ocr"] = stats.TryGetValue("old_ocr", out object oldOcr)
                        ? Convert.ToInt32(oldOcr) + 1
                        : 1;
                if (!replaceOcr)
                {
                    // Accept the page with its current OCR layer
                    return MergeAnalysis(analysis, false, null, null);
                }

                // Else remove old OCR text and request OCR
                foreach (Rect r in ocrSpanBoxes)
                    page.AddRedactAnnot(r);
                page.ApplyRedactions(
                    images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE, // do not touch images
                    graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_NONE, // do not touch vectors
                    text: mupdf.mupdf.PDF_REDACT_TEXT_REMOVE); // remove old OCR layer
                return MergeAnalysis(analysis, true, "ocr_spans", null);
            }

            // 2. Bad character check
            // Too many bad characters result in early exit with OCR recommended.
            if (true
                && charsTotal > 0
                && txtArea > 0
                && (
                    false
                    || (float)charsBad / charsTotal > BadCharThreshold
                    || badAreas / txtArea > BadCharThreshold
                ))
            {
                return MergeAnalysis(analysis, true, "chars_bad", null);
            }

            if (stats != null)
                stats["model_check"] = stats.TryGetValue("model_check", out object modelCheck)
                    ? Convert.ToInt32(modelCheck) + 1
                    : 1;

            Dictionary<string, float> features = ComputeOcrFeatures.ComputeFeatures(blocks, pageRect, page);

            if (stats != null
                && stats.TryGetValue("show_features", out object showFeatures)
                && showFeatures is bool show
                && show) // for debugging
            {
                Debug.WriteLine(JsonConvert.SerializeObject(features, Formatting.Indented));
            }

            float prob = PredictOcrProbability(features);
            bool needsOcr = prob >= OcrModelThreshold; // True if beyond threshold

            // If text-like page detected ? OCR needed
            if (needsOcr)
            {
                return MergeAnalysis(analysis, needsOcr, "img_text", prob);
            }

            // Otherwise, check large images to see if they qualify for OCR
            // This is currently disabled: the list 'image_blocks' is empty.
            if (imageBlocks.Count > 0)
            {
                int checkCount;
                (needsOcr, prob, checkCount) = CheckImages(
                    imageBlocks,
                    prob,
                    OcrModelThreshold);
                if (stats != null)
                    stats["img_checks"] = stats.TryGetValue("img_checks", out object imgChecks)
                        ? Convert.ToInt32(imgChecks) + checkCount
                        : checkCount;
            }

            return MergeAnalysis(
                analysis,
                needsOcr,
                needsOcr ? "img_text" : null,
                prob);
        }

        private static Dictionary<string, object> MergeAnalysis(
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
    }
}
