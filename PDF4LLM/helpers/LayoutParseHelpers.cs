using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MuPDF.NET;
using Newtonsoft.Json.Linq;
using mupdf;
namespace PDF4LLM.Helpers
{
    /// <summary>
    /// Layout tuples <c>(x0, y0, x1, y1, "class")</c>, mutable during <c>clean_*</c> passes.
    /// </summary>
    public sealed class LayoutInfoEntry
    {
        public Rect Bbox { get; set; }
        public string Class { get; set; }
    }

    /// <summary>
    /// Helpers for document layout parsing and layout cleanup passes.
    /// </summary>
    public static class LayoutParseHelpers
    {
        private static float RectArea(Rect r)
        {
            if (r == null || Utils.BboxIsEmpty(r))
                return 0;
            return Math.Max(0, r.X1 - r.X0) * Math.Max(0, r.Y1 - r.Y0);
        }

        private static float IoU(Rect r1, Rect r2)
        {
            if (r1 == null || r2 == null)
                return 0;
            float ix = Math.Max(0, Math.Min(r1.X1, r2.X1) - Math.Max(r1.X0, r2.X0));
            float iy = Math.Max(0, Math.Min(r1.Y1, r2.Y1) - Math.Max(r1.Y0, r2.Y0));
            float inter = ix * iy;
            if (inter <= 0)
                return 0;
            float a1 = RectArea(r1);
            float a2 = RectArea(r2);
            return inter / (a1 + a2 - inter);
        }

        /// <summary>Same role as MuPdf <c>table._iou</c> for matching layout clips to detected tables.</summary>
        /// <param name="a">First rectangle.</param>
        /// <param name="b">Second rectangle.</param>
        public static float IntersectionOverUnion(Rect a, Rect b) => IoU(a, b);

        /// <summary>Page filter: null (all pages), a single <c>int</c> (with negative wrap), or a sequence (sorted unique, strict bounds).</summary>
        /// <param name="pageCount">Total number of pages in the document.</param>
        /// <param name="pages"><c>null</c>, a single page index, or a sequence of 0-based page indices.</param>
        public static List<int> ResolvePageFilter(int pageCount, object pages)
        {
            if (pageCount < 0)
                throw new ArgumentOutOfRangeException(nameof(pageCount));
            if (pages == null)
                return Enumerable.Range(0, pageCount).ToList();

            if (pages is int single)
            {
                int q = single;
                while (q < 0)
                    q += pageCount;
                if (q < 0 || q >= pageCount)
                    throw new ArgumentOutOfRangeException(nameof(pages),
                        $"'pages' must be an index in [0, {pageCount}) after normalizing negatives; got {single}.");
                return new List<int> { q };
            }

            if (pages is IEnumerable<int> seq)
            {
                var set = new SortedSet<int>(seq);
                if (set.Count == 0)
                    return new List<int>();
                if (set.Min < 0 || set.Max >= pageCount)
                    throw new ArgumentOutOfRangeException(nameof(pages),
                        $"'pages' must contain indices in [0, {pageCount}) (negative indices are not wrapped inside lists).");
                return set.ToList();
            }

            throw new ArgumentException("'pages' must be null, int, or a sequence of ints.", nameof(pages));
        }

        /// <param name="doc">PDF document whose structure tree may be removed for performance.</param>
        public static void TryRemovePdfStructTreeRoot(Document doc)
        {
            if (doc == null)
                return;
            try
            {
                PdfDocument pdf = Document.AsPdfDocument(doc);
                if (pdf?.m_internal == null)
                    return;
                PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
                root?.pdf_dict_del(new PdfObj("StructTreeRoot"));
            }
            catch
            {
                /* best-effort: ignore failures */
            }
        }

        static bool _tesseractSetupHelpPrinted;

        /// <summary>Whether <see cref="PrintTesseractSetupHelp"/> has already run.</summary>
        internal static bool TesseractSetupHelpPrinted => _tesseractSetupHelpPrinted;

        /// <summary>
        /// Log pymupdf-layout status at the start of layout parsing, or setup help when it is missing.
        /// </summary>
        public static void LogLayoutStatus()
        {
            if (global::PDF4LLM.Layout.PyMuPdfLayout.IsActivated
                && Page.GetLayoutProvider != null)
            {
                string version = global::PDF4LLM.Layout.PyMuPdfLayout.Version;
                if (!string.IsNullOrEmpty(version))
                    Console.WriteLine($"Using pymupdf-layout ({version}) for document processing.");
                else
                    Console.WriteLine("Using pymupdf-layout for document processing.");
                return;
            }

            global::PDF4LLM.Layout.LayoutPythonPaths.PrintSetupHelp();
        }

        /// <summary>Print once how to install Tesseract OCR and tessdata.</summary>
        public static void PrintTesseractSetupHelp()
        {
            if (_tesseractSetupHelpPrinted)
                return;
            _tesseractSetupHelpPrinted = true;

            Console.Error.WriteLine(
                "PDF4LLM: Tesseract OCR is not available; OCR will be disabled.\n" +
                "\n" +
                "Install Tesseract OCR and language data (tessdata), then ensure one of:\n" +
                "  - Tesseract is on PATH (verify with: tesseract --list-langs)\n" +
                "  - Set TESSDATA_PREFIX to your tessdata folder\n" +
                "\n" +
                "Windows: https://github.com/UB-Mannheim/tesseract/wiki\n" +
                "Debian/Ubuntu: sudo apt install tesseract-ocr tesseract-ocr-eng");
        }

        /// <summary>Resolve tessdata if Tesseract is installed; otherwise <c>null</c>.</summary>
        internal static string TryGetTessdata()
        {
            string env = global::MuPDF.NET.Utils.TESSDATA_PREFIX;
            if (!string.IsNullOrWhiteSpace(env))
                return env;

            env = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            if (!string.IsNullOrWhiteSpace(env))
                return env;

            return TryFindTessdataViaTesseractListLangs();
        }

        static string TryFindTessdataViaTesseractListLangs()
        {
            try
            {
                using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "tesseract",
                    Arguments = "--list-langs",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }))
                {
                    if (process == null)
                        return null;
                    string stdout = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                        return null;
                    var m = System.Text.RegularExpressions.Regex.Match(
                        stdout,
                        @"List of available languages in ""(.+)""");
                    return m.Success ? m.Groups[1].Value : null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check availability of OCR tools and language data.
        /// <summary>Return the best OCR function available or null.</summary>
        /// </summary>
        public static OcrPageFunction SelectOcrFunction()
        {
            string tessdata = TryGetTessdata();
            bool rapidocrAvailable = false;
            bool paddleocrAvailable = false;

            // rapidocr_onnxruntime / PaddleOCR are not wired for .NET yet.
            if (string.IsNullOrEmpty(tessdata) && !rapidocrAvailable && !paddleocrAvailable)
            {
                PrintTesseractSetupHelp();
                return null;
            }

            if (!string.IsNullOrEmpty(tessdata))
            {
                if (rapidocrAvailable)
                {
                    Console.WriteLine("Using RapidOCR and Tesseract for OCR processing.");
                    return (page, ocrDpi, ocrLanguage, keepOcrText) =>
                        global::PDF4LLM.Ocr.RapidTessApi.ExecOcr(
                            page, dpi: ocrDpi, language: ocrLanguage, keepOcrText: keepOcrText);
                }

                if (paddleocrAvailable)
                {
                    Console.WriteLine("Using PaddleOCR and Tesseract for OCR processing.");
                    return (page, ocrDpi, ocrLanguage, keepOcrText) =>
                        global::PDF4LLM.Ocr.PaddleTessApi.ExecOcr(
                            page, dpi: ocrDpi, language: ocrLanguage, keepOcrText: keepOcrText);
                }

                Console.WriteLine("Using Tesseract for OCR processing.");
                return (page, ocrDpi, ocrLanguage, keepOcrText) =>
                    global::PDF4LLM.Ocr.TesseractApi.ExecOcr(
                        page, dpi: ocrDpi, language: ocrLanguage, keepOcrText: keepOcrText);
            }

            if (rapidocrAvailable)
            {
                Console.WriteLine("Using RapidOCR for OCR processing.");
                return (page, ocrDpi, ocrLanguage, keepOcrText) =>
                    global::PDF4LLM.Ocr.RapidOcrApi.ExecOcr(
                        page, dpi: ocrDpi, language: ocrLanguage, keepOcrText: keepOcrText);
            }

            if (paddleocrAvailable)
            {
                Console.WriteLine("Using PaddleOCR for OCR processing.");
                return (page, ocrDpi, ocrLanguage, keepOcrText) =>
                    global::PDF4LLM.Ocr.PaddleOcrApi.ExecOcr(
                        page, dpi: ocrDpi, language: ocrLanguage, keepOcrText: keepOcrText);
            }

            return null;
        }

        /// <summary>Parse <c>page.layout_information</c> rows <c>(x0, y0, x1, y1, class)</c>.</summary>
        private static List<LayoutInfoEntry> ParseLayoutInformation(object layoutObj)
        {
            if (layoutObj == null)
                return null;
            if (!(layoutObj is System.Collections.IEnumerable rows))
                return null;

            var layout = new List<LayoutInfoEntry>();
            int rawCount = 0;
            foreach (object row in rows)
            {
                rawCount++;
                if (!TryParseLayoutRow(row, out LayoutInfoEntry entry))
                    continue;
                layout.Add(entry);
            }

            // Treat parse failure like missing layout (fall back to stext-only boxes).
            if (rawCount > 0 && layout.Count == 0)
                return null;

            return layout;
        }

        private static bool TryParseLayoutRow(object row, out LayoutInfoEntry entry)
        {
            entry = null;
            JArray ja = row as JArray;
            System.Collections.IList list = row as object[];
            if (ja != null)
                list = ja;
            else if (list == null && row is System.Collections.IList il)
                list = il;

            if (list == null || list.Count < 5)
                return false;

            string cls = list[4]?.ToString() ?? "text";
            if (ja != null && list[4] is JValue jv && jv.Type == JTokenType.String)
                cls = jv.Value<string>() ?? cls;

            entry = new LayoutInfoEntry
            {
                Bbox = new Rect(
                    Convert.ToSingle(list[0], CultureInfo.InvariantCulture),
                    Convert.ToSingle(list[1], CultureInfo.InvariantCulture),
                    Convert.ToSingle(list[2], CultureInfo.InvariantCulture),
                    Convert.ToSingle(list[3], CultureInfo.InvariantCulture)),
                Class = cls,
            };
            return true;
        }

        /// <summary>
        /// Layout from <see cref="Page.GetLayout"/> / <c>page.layout_information</c> when a provider is registered.
        /// Returns <c>null</c> when no layout provider is active.
        /// </summary>
        private static List<LayoutInfoEntry> TryNativeLayoutInformation(Page page)
        {
            object layoutObj = page.LayoutInformation;
            if (layoutObj == null)
            {
                page.GetLayout();
                layoutObj = page.LayoutInformation;
            }

            if (layoutObj == null)
                return null;

            return ParseLayoutInformation(layoutObj);
        }

        /// <summary>
        /// Build layout boxes for <c>parse_document</c>.
        /// Prefer <see cref="Page.GetLayout"/> / <c>page.layout_information</c> when a provider is set.
        /// If unavailable, stext text blocks only — tables come from layout boxes or
        /// <c>tables_exist</c> handling in <c>parse_document</c> (see document_layout.py).
        /// </summary>
        /// <param name="page">Page whose layout boxes are built.</param>
        /// <param name="blocks">Extracted text blocks used when native layout is unavailable.</param>
        public static List<LayoutInfoEntry> BuildLayoutInformation(Page page, List<Block> blocks)
        {
            List<LayoutInfoEntry> native = TryNativeLayoutInformation(page);
            if (native != null && native.Count > 0)
                return native;

            // Fallback: do not map image blocks (type 1) to "picture" here.
            // Do not run TableFinder here — Python only sets tables_exist from layout boxes.
            var layout = new List<LayoutInfoEntry>();
            if (blocks != null)
            {
                foreach (Block b in blocks)
                {
                    if (b == null || Utils.BboxIsEmpty(b.Bbox))
                        continue;
                    if (b.Type == 0)
                        layout.Add(new LayoutInfoEntry { Bbox = new Rect(b.Bbox), Class = "text" });
                }
            }

            return layout;
        }

        /// <param name="layout">Layout boxes to inspect.</param>
        public static bool TablesExist(IList<LayoutInfoEntry> layout) =>
            layout != null && layout.Any(b => b.Class == "table");

        /// <summary><c>utils.clean_pictures</c> (subset: block types 0,1; vector type 3 skipped if absent).</summary>
        /// <param name="page">Source page (reserved for future vector-block support).</param>
        /// <param name="blocks">Page text/image blocks used to expand picture regions.</param>
        /// <param name="layout">Layout entries to adjust in place.</param>
        public static void CleanPictures(Page page, List<Block> blocks, List<LayoutInfoEntry> layout)
        {
            if (layout == null || blocks == null || layout.Count == 0)
                return;

            var allBboxes = layout.Select(l => l.Bbox).ToList();

            for (int i = 0; i < layout.Count; i++)
            {
                string cls = layout[i].Class;
                if (cls != "picture" && cls != "formula" && cls != "table")
                    continue;

                Rect bbox = new Rect(layout[i].Bbox);
                foreach (Block b in blocks)
                {
                    if (b?.Bbox == null)
                        continue;
                    if (b.Type != 0 && b.Type != 1 && b.Type != 3)
                        continue;

                    Rect blockBbox = new Rect(b.Bbox);
                    if (!bbox.Intersects(blockBbox))
                        continue;
                    bool otherHit = false;
                    for (int j = 0; j < allBboxes.Count; j++)
                    {
                        if (j == i)
                            continue;
                        if (allBboxes[j] != null && allBboxes[j].Intersects(blockBbox))
                        {
                            otherHit = true;
                            break;
                        }
                    }

                    if (!otherHit)
                        bbox = bbox | blockBbox;
                }

                layout[i].Bbox = bbox;
                allBboxes[i] = bbox;
            }
        }

        private static bool BboxInBboxStrict(Rect inner, Rect outer)
        {
            if (inner == null || outer == null)
                return false;
            return outer.X0 <= inner.X0 && outer.Y0 <= inner.Y0 && outer.X1 >= inner.X1 && outer.Y1 >= inner.Y1;
        }

        /// <summary>Subset of <c>utils.add_image_orphans</c> using <see cref="Page.GetDrawings"/> when vector blocks are unavailable.</summary>
        /// <param name="page">Source page for drawings and image blocks.</param>
        /// <param name="blocks">Extracted page blocks.</param>
        /// <param name="layout">Layout list to which orphan picture boxes are appended.</param>
        public static void AddImageOrphans(Page page, List<Block> blocks, List<LayoutInfoEntry> layout)
        {
            if (layout == null || page == null || blocks == null)
                return;

            var allBboxes = layout.Select(l => l.Bbox).Where(b => b != null).ToList();
            float pageArea = RectArea(page.Rect);
            float areaLimit = pageArea * 0.9f;

            var images = new List<Rect>();
            foreach (Block b in blocks)
            {
                if (b?.Type != 1 || b.Bbox == null)
                    continue;
                Rect r = Utils.IntersectRects(page.Rect, b.Bbox);
                if (r.Width <= 3 || r.Height <= 3)
                    continue;
                if (Utils.BboxIsEmpty(r) || RectArea(r) >= areaLimit)
                    continue;
                images.Add(r);
            }

            List<PathInfo> drawings;
            try
            {
                drawings = page.GetDrawings();
            }
            catch
            {
                drawings = new List<PathInfo>();
            }

            var vectors = drawings
                .Select(p => p?.Rect)
                .Where(r => r != null && r.Height > 3 && r.Width > 3)
                .Select(r => Utils.IntersectRects(page.Rect, r))
                .OrderByDescending(RectArea)
                .Take(500)
                .ToList();

            var paths = new List<PathInfo>();
            foreach (Rect r in vectors)
            {
                if (RectArea(r) >= areaLimit)
                    continue;
                float absR = RectArea(r);
                float rLow = 0.1f * absR;
                float rHi = 0.8f * absR;

                if (allBboxes.Any(bb =>
                    {
                        float inter = RectArea(Utils.IntersectRects(r, bb));
                        return inter > Math.Min(rLow, RectArea(bb) * 0.1f);
                    }))
                    continue;

                if (images.Any(img =>
                    {
                        float inter = RectArea(Utils.IntersectRects(r, img));
                        return inter > rHi;
                    }))
                    continue;

                paths.Add(new PathInfo { Rect = r });
            }

            List<Rect> clustered;
            try
            {
                clustered = page.ClusterDrawings(clip: null, drawings: paths, xTolerance: 20, yTolerance: 20);
            }
            catch
            {
                clustered = new List<Rect>();
            }

            clustered = clustered.Where(v => v.Width > 30 && v.Height > 30).ToList();

            var merged = new List<Rect>();
            foreach (Rect r in images.Concat(clustered).OrderByDescending(RectArea).Take(500))
            {
                if (merged.Any(fr => BboxInBboxStrict(r, fr)))
                    continue;
                merged.Add(r);
            }

            foreach (Rect r in merged)
            {
                float absR = RectArea(r);
                if (allBboxes.Any(bb =>
                    {
                        float inter = RectArea(Utils.IntersectRects(r, bb));
                        return inter > 0.1f * Math.Min(absR, RectArea(bb));
                    }))
                    continue;

                layout.Add(new LayoutInfoEntry { Bbox = r, Class = "picture" });
                allBboxes.Add(r);
            }
        }

        /// <summary><c>utils.clean_tables</c></summary>
        /// <param name="page">Source page (reserved for extended table cleaning).</param>
        /// <param name="blocks">Text blocks overlapping table layout boxes.</param>
        /// <param name="layout">Layout entries to validate or reclassify in place.</param>
        /// <param name="textPage">Optional text page for <see cref="Utils.TableCleaner"/> splitting.</param>
        public static void CleanTables(Page page, List<Block> blocks, List<LayoutInfoEntry> layout, TextPage textPage = null)
        {
            if (layout == null || blocks == null)
                return;

            for (int i = 0; i < layout.Count; i++)
            {
                if (layout[i].Class != "table")
                    continue;

                Rect bbox = layout[i].Bbox;
                if (bbox == null)
                    continue;

                var lines = new List<Rect>();
                foreach (Block b in blocks.Where(b => b?.Type == 0))
                {
                    if (b.Lines == null)
                        continue;
                    foreach (Line line in b.Lines)
                    {
                        if (line?.Bbox == null || Utils.BboxIsEmpty(line.Bbox))
                            continue;
                        if (!line.Bbox.Intersects(bbox))
                            continue;
                        lines.Add(line.Bbox);
                    }
                }

                var yVals0 = lines.Select(l => (float)Math.Round(l.Y1)).Distinct().OrderBy(y => y).ToList();
                if (yVals0.Count == 0)
                {
                    layout[i].Class = "table-fallback";
                    continue;
                }

                var yVals = new List<float> { yVals0[0] };
                for (int k = 1; k < yVals0.Count; k++)
                {
                    if (yVals0[k] - yVals[yVals.Count - 1] > 3)
                        yVals.Add(yVals0[k]);
                }

                if (yVals.Count < 2)
                {
                    layout[i].Class = "text";
                    continue;
                }

                int mxSame = 1;
                foreach (float y in yVals)
                {
                    int count = lines.Count(l => Math.Abs(y - l.Y1) <= 3);
                    if (count > mxSame)
                    {
                        mxSame = count;
                        break;
                    }
                }

                if (mxSame < 2)
                    layout[i].Class = "text";
                else if (textPage != null)
                {
                    List<Dictionary<string, object>> dictBlocks = Utils.StextDictBlocks(textPage);
                    (LayoutInfoEntry picture, LayoutInfoEntry table) = Utils.TableCleaner(dictBlocks, layout[i]);
                    if (picture != null)
                    {
                        if (table == null)
                            layout[i] = picture;
                        else
                        {
                            layout[i] = table;
                            layout.Insert(i, picture);
                            i++;
                        }
                    }
                }
            }
        }

        private static List<LayoutInfoEntry> FilterContained(IList<LayoutInfoEntry> boxes)
        {
            if (boxes == null || boxes.Count == 0)
                return new List<LayoutInfoEntry>();

            var sorted = boxes
                .OrderByDescending(b => RectArea(b.Bbox))
                .ToList();
            var result = new List<LayoutInfoEntry>();
            foreach (LayoutInfoEntry r in sorted)
            {
                if (!result.Any(other => IsContainedIn(r.Bbox, other.Bbox)))
                    result.Add(r);
            }

            return result;
        }

        private static bool IsContainedIn(Rect inner, Rect outer)
        {
            if (inner == null || outer == null)
                return false;
            if (inner.X0 == outer.X0 && inner.Y0 == outer.Y0 && inner.X1 == outer.X1 && inner.Y1 == outer.Y1)
                return false;
            return outer.X0 <= inner.X0 && outer.Y0 <= inner.Y0 && outer.X1 >= inner.X1 && outer.Y1 >= inner.Y1;
        }

        /// <summary><c>utils.find_reading_order</c>: filter contained boxes, split header/footer, <c>compute_reading_order</c> on body.</summary>
        /// <param name="pageRect">Full page rectangle for scaling vertical gaps.</param>
        /// <param name="blocks">Page blocks used as vector hints during ordering.</param>
        /// <param name="boxes">Layout boxes to order for reading sequence.</param>
        /// <param name="verticalGap">Base vertical gap in points before page-height scaling.</param>
        public static List<LayoutInfoEntry> FindReadingOrder(Rect pageRect, List<Block> blocks, List<LayoutInfoEntry> boxes, float verticalGap = 12f)
        {
            if (boxes == null || boxes.Count == 0)
                return new List<LayoutInfoEntry>();

            List<LayoutInfoEntry> filtered = FilterContained(boxes);
            var headers = new List<LayoutInfoEntry>();
            var footers = new List<LayoutInfoEntry>();
            var body = new List<LayoutInfoEntry>();

            foreach (LayoutInfoEntry box in filtered)
            {
                string c = box.Class ?? "";
                if (c == "page-header")
                    headers.Add(box);
                else if (c == "page-footer")
                    footers.Add(box);
                else
                    body.Add(box);
            }

            if (body.Count == 0)
            {
                headers.Sort((a, b) => a.Bbox.Y0.CompareTo(b.Bbox.Y0) != 0 ? a.Bbox.Y0.CompareTo(b.Bbox.Y0) : a.Bbox.X0.CompareTo(b.Bbox.X0));
                footers.Sort((a, b) => a.Bbox.Y0.CompareTo(b.Bbox.Y0) != 0 ? a.Bbox.Y0.CompareTo(b.Bbox.Y0) : a.Bbox.X0.CompareTo(b.Bbox.X0));
                var only = new List<LayoutInfoEntry>();
                only.AddRange(headers);
                only.AddRange(footers);
                return only;
            }

            Rect joined = Utils.JoinRects(body.Select(b => b.Bbox).ToList());
            float vGap = verticalGap * pageRect.Height / 800f;

            float minBodyH = body.Min(b => b.Bbox.Height);
            var vectorRects = new List<Rect>();
            if (blocks != null && !Utils.BboxIsEmpty(joined))
            {
                foreach (Block b in blocks)
                {
                    if (b?.Bbox == null || Utils.BboxIsEmpty(b.Bbox))
                        continue;
                    if (b.Bbox.Height < minBodyH)
                        continue;
                    if (!Utils.BboxInBbox(b.Bbox, joined))
                        continue;
                    vectorRects.Add(new Rect(b.Bbox));
                }
            }

            List<LayoutInfoEntry> ordered = ComputeReadingOrderSimple(body, joined, vectorRects, vGap);

            headers.Sort((a, b) => a.Bbox.Y0.CompareTo(b.Bbox.Y0) != 0 ? a.Bbox.Y0.CompareTo(b.Bbox.Y0) : a.Bbox.X0.CompareTo(b.Bbox.X0));
            footers.Sort((a, b) => a.Bbox.Y0.CompareTo(b.Bbox.Y0) != 0 ? a.Bbox.Y0.CompareTo(b.Bbox.Y0) : a.Bbox.X0.CompareTo(b.Bbox.X0));

            var final = new List<LayoutInfoEntry>();
            final.AddRange(headers);
            final.AddRange(ordered);
            final.AddRange(footers);
            return final;
        }

        /// <summary><c>utils.compute_reading_order</c>: <c>cluster_stripes</c> then <c>cluster_columns_in_stripe</c> per stripe.</summary>
        private static List<LayoutInfoEntry> ComputeReadingOrderSimple(
            List<LayoutInfoEntry> body,
            Rect joined,
            List<Rect> vectors,
            float verticalGap)
        {
            if (body == null || body.Count == 0)
                return body != null ? new List<LayoutInfoEntry>(body) : new List<LayoutInfoEntry>();

            List<List<LayoutInfoEntry>> stripes = ClusterStripes(body, joined, vectors, verticalGap);
            var ordered = new List<LayoutInfoEntry>();
            foreach (List<LayoutInfoEntry> stripe in stripes)
            {
                foreach (List<LayoutInfoEntry> col in ClusterColumnsInStripe(stripe))
                    ordered.AddRange(col);
            }

            return ordered;
        }

        /// <summary><c>utils.cluster_stripes</c>.</summary>
        private static List<List<LayoutInfoEntry>> ClusterStripes(
            List<LayoutInfoEntry> boxes,
            Rect joinedBoxes,
            List<Rect> vectors,
            float verticalGap)
        {
            if (boxes == null || boxes.Count == 0)
                return new List<List<LayoutInfoEntry>>();

            List<LayoutInfoEntry> sortedBoxes = boxes.OrderBy(b => b.Bbox.Y1).ToList();

            if (IsMultiColumnLayout(boxes))
                return new List<List<LayoutInfoEntry>> { new List<LayoutInfoEntry>(boxes) };

            var yValues = new SortedSet<float> { joinedBoxes.Y1 };
            foreach (LayoutInfoEntry box in sortedBoxes)
            {
                float y = box.Bbox.Y1;
                if (y >= joinedBoxes.Y1)
                    continue;

                var div = new Rect(joinedBoxes.X0, y, joinedBoxes.X1, y + verticalGap);
                if (boxes.Any(b => div.Intersects(b.Bbox)))
                    continue;

                float y0Below = sortedBoxes.Where(b => b.Bbox.Y0 >= div.Y1).Select(b => b.Bbox.Y0).DefaultIfEmpty(joinedBoxes.Y1).Min();
                div = new Rect(div.X0, div.Y0, div.X1, y0Below);

                int interCount = 0;
                if (vectors != null)
                {
                    foreach (Rect vr in vectors)
                    {
                        if (div.Intersects(vr) && vr.Y0 <= div.Y0 && div.Y1 <= vr.Y1)
                            interCount++;
                    }
                }

                if (interCount <= 1)
                    yValues.Add(div.Y1);
            }

            var work = new List<LayoutInfoEntry>(sortedBoxes);
            work.Sort((a, b) => a.Bbox.Y1.CompareTo(b.Bbox.Y1));
            var stripes = new List<List<LayoutInfoEntry>>();

            foreach (float y in yValues)
            {
                var currentStripe = new List<LayoutInfoEntry>();
                while (work.Count > 0 && work[0].Bbox.Y1 <= y)
                {
                    currentStripe.Add(work[0]);
                    work.RemoveAt(0);
                }

                if (currentStripe.Count > 0)
                    stripes.Add(currentStripe);
            }

            return stripes;
        }

        /// <summary><c>utils.are_disjoint</c> (non-strict: shared edges count as disjoint).</summary>
        private static bool AreDisjoint(Rect bbox, Rect cell, bool strict = false)
        {
            if (bbox == null || cell == null)
                return true;
            if (!strict)
                return bbox.X0 >= cell.X1 || bbox.X1 <= cell.X0 || bbox.Y0 >= cell.Y1 || bbox.Y1 <= cell.Y0;
            return bbox.X0 > cell.X1 || bbox.X1 < cell.X0 || bbox.Y0 > cell.Y1 || bbox.Y1 < cell.Y0;
        }

        /// <summary><c>utils.cluster_substripe</c> inside <c>cluster_columns_in_stripe</c>.</summary>
        private static List<List<LayoutInfoEntry>> ClusterSubstripe(List<LayoutInfoEntry> substripe)
        {
            const float HorizontalGap = 1f;
            if (substripe == null || substripe.Count == 0)
                return new List<List<LayoutInfoEntry>>();

            List<LayoutInfoEntry> sortedBoxes = substripe.OrderBy(b => b.Bbox.X0).ToList();
            var columns = new List<List<LayoutInfoEntry>>();
            var currentColumn = new List<LayoutInfoEntry> { sortedBoxes[0] };

            for (int i = 1; i < sortedBoxes.Count; i++)
            {
                LayoutInfoEntry box = sortedBoxes[i];
                float prevRight = currentColumn.Max(b => b.Bbox.X1);
                if (box.Bbox.X0 - prevRight > HorizontalGap)
                {
                    columns.Add(currentColumn.OrderBy(b => b.Bbox.Y0).ToList());
                    currentColumn = new List<LayoutInfoEntry> { box };
                }
                else
                    currentColumn.Add(box);
            }

            columns.Add(currentColumn.OrderBy(b => b.Bbox.Y0).ToList());
            return columns;
        }

        /// <summary><c>utils.cluster_columns_in_stripe</c>.</summary>
        private static List<List<LayoutInfoEntry>> ClusterColumnsInStripe(List<LayoutInfoEntry> stripe)
        {
            if (stripe == null || stripe.Count == 0)
                return new List<List<LayoutInfoEntry>>();

            float x0 = stripe.Min(b => b.Bbox.X0);
            float y0 = stripe.Min(b => b.Bbox.Y0);
            float x1 = stripe.Max(b => b.Bbox.X1);

            Rect ExpandedBand(LayoutInfoEntry b) => new Rect(x0, b.Bbox.Y0, x1, b.Bbox.Y1);

            List<LayoutInfoEntry> solitaries = stripe
                .Where(b => stripe.All(r => ReferenceEquals(r, b) || AreDisjoint(r.Bbox, ExpandedBand(b))))
                .ToList();

            if (solitaries.Count == 0)
                return ClusterSubstripe(stripe);

            solitaries.Sort((a, b) => a.Bbox.Y0.CompareTo(b.Bbox.Y0));
            var finalists = new List<List<LayoutInfoEntry>>();
            float s0 = y0;

            foreach (LayoutInfoEntry sol in solitaries)
            {
                List<LayoutInfoEntry> sstripe = stripe.Where(b => b.Bbox.Y1 <= sol.Bbox.Y0 && b.Bbox.Y0 >= s0).ToList();
                finalists.AddRange(ClusterSubstripe(sstripe));
                s0 = sol.Bbox.Y1;
                finalists.Add(new List<LayoutInfoEntry> { sol });
            }

            LayoutInfoEntry lastSol = solitaries[solitaries.Count - 1];
            List<LayoutInfoEntry> tail = stripe.Where(b => b.Bbox.Y0 >= lastSol.Bbox.Y1).ToList();
            if (tail.Count > 0)
                finalists.AddRange(ClusterSubstripe(tail));

            return finalists;
        }

        private static bool IsMultiColumnLayout(List<LayoutInfoEntry> boxes)
        {
            if (boxes == null || boxes.Count < 2)
                return false;
            var sorted = boxes.OrderBy(b => b.Bbox.X0).ToList();
            var columns = new List<List<LayoutInfoEntry>> { new List<LayoutInfoEntry> { sorted[0] } };
            for (int i = 1; i < sorted.Count; i++)
            {
                LayoutInfoEntry box = sorted[i];
                float prevRight = columns[columns.Count - 1].Max(b => b.Bbox.X1);
                if (box.Bbox.X0 - prevRight > 3)
                    columns.Add(new List<LayoutInfoEntry>());
                columns[columns.Count - 1].Add(box);
            }

            return columns.Count > 1;
        }

        /// <summary><c>complete_table_structure</c> extras: virtual lines not ported; returns empty lists.</summary>
        /// <summary><c>utils.simplify_vectors</c></summary>
        /// <param name="vectors">Vector drawing blocks from <c>extractDICT</c>.</param>
        public static List<Dictionary<string, object>> SimplifyVectors(List<Dictionary<string, object>> vectors)
        {
            const float yTolerance = 1f;
            var newVectors = new List<Dictionary<string, object>>();
            if (vectors == null || vectors.Count == 0)
                return newVectors;

            newVectors.Add(vectors[0]);
            for (int vi = 1; vi < vectors.Count; vi++)
            {
                Dictionary<string, object> v = vectors[vi];
                Dictionary<string, object> lastV = newVectors[newVectors.Count - 1];
                var vb = GetMutableBbox(v);
                var lb = GetMutableBbox(lastV);
                if (vb == null || lb == null)
                {
                    newVectors.Add(v);
                    continue;
                }

                if (Math.Abs(vb[1] - lb[1]) < yTolerance
                    && Math.Abs(vb[3] - lb[3]) < yTolerance
                    && vb[0] <= lb[2] + 1)
                {
                    lb[0] = Math.Min(vb[0], lb[0]);
                    lb[1] = Math.Min(vb[1], lb[1]);
                    lb[2] = Math.Max(vb[2], lb[2]);
                    lb[3] = Math.Max(vb[3], lb[3]);
                }
                else
                    newVectors.Add(v);
            }

            return newVectors;
        }

        /// <summary><c>utils.find_virtual_lines</c></summary>
        /// <param name="page">Source page.</param>
        /// <param name="tableBbox">Bounding box of the table region.</param>
        /// <param name="words">Word boxes inside the table as <c>[x0, y0, x1, y1]</c> arrays.</param>
        /// <param name="vectors">Simplified vector blocks overlapping the table.</param>
        /// <param name="linkRects">Link hot areas to exclude from vertical line inference.</param>
        public static (List<Tuple<Point, Point>> lines, List<Rect> boxes) FindVirtualLines(
            Page page,
            Rect tableBbox,
            List<float[]> words,
            List<Dictionary<string, object>> vectors,
            List<Rect> linkRects)
        {
            List<Tuple<Point, Point>> MakeVertical(Rect tbbox, Rect lineBbox, List<Rect> wboxes)
            {
                var top = new Point(lineBbox.X0, lineBbox.Y0 - 2);
                var bottom = new Point(top.X, tbbox.Y1);
                List<Rect> myWboxes = wboxes
                    .Where(wr => wr.Y0 >= top.Y && wr.Y1 <= bottom.Y && wr.X0 < top.X && wr.X1 > top.X)
                    .OrderBy(wr => wr.Y1)
                    .ToList();
                if (myWboxes.Count > 0)
                    bottom.Y = myWboxes[0].Y0;

                myWboxes = wboxes
                    .Where(wr => wr.Y0 >= tbbox.Y0 && wr.Y1 <= top.Y && wr.X0 < top.X && wr.X1 > top.X)
                    .OrderBy(wr => wr.Y1)
                    .ToList();
                if (myWboxes.Count > 0)
                    top.Y = myWboxes[myWboxes.Count - 1].Y1;
                else
                    top.Y = tbbox.Y0;

                return new List<Tuple<Point, Point>> { Tuple.Create(top, bottom) };
            }

            var wordBoxes = words
                .Where(w => w != null && w.Length >= 4 && (w[3] - w[1]) > 5 && tableBbox.Contains(new Rect(w[0], w[1], w[2], w[3])))
                .Select(w => new Rect(w[0], w[1], w[2], w[3]))
                .OrderBy(r => r.Y1)
                .ToList();

            var allLines = new List<Tuple<Point, Point>>();
            var allBoxes = new List<Rect>();
            foreach (Dictionary<string, object> v in vectors)
            {
                Rect vbbox = Utils.DictBboxToRect(v.TryGetValue("bbox", out object bb) ? bb : null).Normalize();
                vbbox = new Rect(vbbox.X0, vbbox.Y0 - 0.5f, vbbox.X1, vbbox.Y1 + 0.5f);
                vbbox &= tableBbox;
                if (Utils.BboxIsEmpty(vbbox))
                    continue;

                bool stroked = v.TryGetValue("stroked", out object so) && Convert.ToBoolean(so);
                if (!stroked && vbbox.Height >= 5 && vbbox.Width > 20)
                {
                    allLines.Add(Tuple.Create(vbbox.TopLeft, vbbox.TopRight));
                    allLines.Add(Tuple.Create(vbbox.BottomLeft, vbbox.BottomRight));
                    continue;
                }

                if (vbbox.Width > 20 && vbbox.Height <= 3
                    && !(linkRects?.Any(lr => vbbox.Intersects(lr)) ?? false))
                {
                    foreach (Tuple<Point, Point> line in MakeVertical(tableBbox, vbbox, wordBoxes))
                        allLines.Add(line);
                }
            }

            return (allLines, allBoxes);
        }

        private static float[] GetMutableBbox(Dictionary<string, object> block)
        {
            if (!block.TryGetValue("bbox", out object bboxObj))
                return null;
            if (bboxObj is float[] fa && fa.Length >= 4)
                return fa;
            Rect r = Utils.DictBboxToRect(bboxObj);
            var arr = new float[] { r.X0, r.Y0, r.X1, r.Y1 };
            block["bbox"] = arr;
            return arr;
        }

        /// <summary>Write layout entries back to <see cref="Page.LayoutInformation"/>.</summary>
        /// <param name="page">Page whose layout information is updated.</param>
        /// <param name="layout">Layout boxes to serialize; clears layout when <c>null</c>.</param>
        public static void WritePageLayout(Page page, List<LayoutInfoEntry> layout)
        {
            if (page == null)
                return;
            if (layout == null)
            {
                page.LayoutInformation = null;
                return;
            }

            page.LayoutInformation = layout
                .Select(e => (object)new object[]
                {
                    e.Bbox.X0, e.Bbox.Y0, e.Bbox.X1, e.Bbox.Y1, e.Class ?? "text"
                })
                .ToList();
        }

        /// <summary>Read layout from <see cref="Page.GetLayout"/> / <c>layout_information</c>.</summary>
        /// <param name="page">Page to read or refresh layout from.</param>
        /// <param name="blocks">Fallback text blocks when native layout is empty.</param>
        public static List<LayoutInfoEntry> ReadPageLayout(Page page, List<Block> blocks)
        {
            page.GetLayout();
            List<LayoutInfoEntry> layout = ParseLayoutInformation(page.LayoutInformation);
            if (layout != null && layout.Count > 0)
                return layout;
            return BuildLayoutInformation(page, blocks);
        }

        /// <summary><c>utils.complete_table_structure(page)</c></summary>
        /// <param name="page">Page whose table layout boxes are completed.</param>
        public static (List<Tuple<Point, Point>> lines, List<Rect> boxes) CompleteTableStructure(Page page)
        {
            List<LayoutInfoEntry> layout = ParseLayoutInformation(page?.LayoutInformation);
            if (page == null || layout == null)
                return (new List<Tuple<Point, Point>>(), new List<Rect>());

            int flags = (int)TextFlags.TEXT_ACCURATE_BBOXES
                | (int)mupdf.mupdf.FZ_STEXT_COLLECT_VECTORS
                | (int)mupdf.mupdf.FZ_STEXT_COLLECT_STYLES;
            using (TextPage textPage = page.GetTextPage(flags: flags))
            {
                return CompleteTableStructure(page, layout, textPage);
            }
        }

        /// <summary><c>utils.complete_table_structure</c></summary>
        /// <param name="page">Source page.</param>
        /// <param name="layout">Layout entries; table-class boxes are processed.</param>
        /// <param name="textPage">Optional pre-built text page; created when <c>null</c>.</param>
        public static (List<Tuple<Point, Point>> lines, List<Rect> boxes) CompleteTableStructure(
            Page page,
            List<LayoutInfoEntry> layout,
            TextPage textPage = null)
        {
            var allLines = new List<Tuple<Point, Point>>();
            var allBoxes = new List<Rect>();
            if (page == null || layout == null)
                return (allLines, allBoxes);

            bool dispose = false;
            if (textPage == null)
            {
                textPage = page.GetTextPage(flags: Utils.FLAGS);
                dispose = true;
            }

            try
            {
                List<Dictionary<string, object>> dictBlocks = Utils.StextDictBlocks(textPage);
                var words = page.GetTextWords(textpage: textPage)
                    .Select(w => new[] { w.x0, w.y0, w.x1, w.y1 })
                    .ToList();

                var vectors = dictBlocks
                    .Where(b => b.TryGetValue("type", out object t) && Convert.ToInt32(t) == 3
                        && b.TryGetValue("isrect", out object ir) && Convert.ToBoolean(ir))
                    .OrderBy(b => Utils.DictBboxToRect(b["bbox"]).Y1)
                    .ThenBy(b => Utils.DictBboxToRect(b["bbox"]).X0)
                    .ToList();
                vectors = SimplifyVectors(vectors);

                var linkRects = page.GetLinks()
                    .Where(l => l.From != null)
                    .Select(l => new Rect(l.From))
                    .ToList();

                foreach (LayoutInfoEntry entry in layout)
                {
                    if (entry?.Class != "table" || entry.Bbox == null)
                        continue;
                    Rect tableBbox = new Rect(entry.Bbox);
                    allBoxes.Add(tableBbox);
                    (List<Tuple<Point, Point>> lines, List<Rect> boxes) = FindVirtualLines(
                        page, tableBbox, words, vectors, linkRects);
                    allLines.AddRange(lines);
                    allBoxes.AddRange(boxes);
                }
            }
            finally
            {
                if (dispose)
                    textPage?.Dispose();
            }

            return (allLines, allBoxes);
        }

        /// <param name="page">Source page.</param>
        /// <param name="layout">Layout entries; table-class boxes are processed.</param>
        public static (List<Tuple<Point, Point>> lines, List<Rect> boxes) CompleteTableStructure(Page page, List<LayoutInfoEntry> layout)
            => CompleteTableStructure(page, layout, null);

    }
}
