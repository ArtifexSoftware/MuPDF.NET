using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
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
        public static float IntersectionOverUnion(Rect a, Rect b) => IoU(a, b);

        /// <summary>Page filter: null (all pages), a single <c>int</c> (with negative wrap), or a sequence (sorted unique, strict bounds).</summary>
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

        /// <summary>Default OCR backend selection: full-page OCR is supplied via <see cref="OcrPageFunction"/> when configured.</summary>
        public static OcrPageFunction SelectOcrFunction()
        {
            return null;
        }

        /// <summary>Build layout from stext blocks, then merge <see cref="TableFinder"/> regions as <c>table</c> (approximates MuPdf.layout + table hints).</summary>
        public static List<LayoutInfoEntry> BuildLayoutInformation(Page page, List<Block> blocks)
        {
            var layout = new List<LayoutInfoEntry>();
            if (blocks != null)
            {
                foreach (Block b in blocks)
                {
                    if (b == null || Utils.BboxIsEmpty(b.Bbox))
                        continue;
                    if (b.Type == 0)
                        layout.Add(new LayoutInfoEntry { Bbox = new Rect(b.Bbox), Class = "text" });
                    else if (b.Type == 1)
                        layout.Add(new LayoutInfoEntry { Bbox = new Rect(b.Bbox), Class = "picture" });
                }
            }

            TableFinder tbf = null;
            try
            {
                tbf = TableFinderHelper.FindTables(page, strategy: "lines_strict");
            }
            catch
            {
                return layout;
            }

            if (tbf?.tables == null || tbf.tables.Count == 0)
                return layout;

            foreach (Table tab in tbf.tables
                         .Where(t => t?.bbox != null)
                         .OrderBy(t => t.bbox.Y0)
                         .ThenBy(t => t.bbox.X0))
            {
                Rect tb = tab.bbox;
                layout.RemoveAll(e =>
                    e.Class == "text"
                    && e.Bbox != null
                    && Utils.AlmostInBbox(e.Bbox, tb, 0.85f));
                if (layout.Any(e => e.Class == "table" && IoU(e.Bbox, tb) > 0.85f))
                    continue;
                layout.Add(new LayoutInfoEntry { Bbox = new Rect(tb), Class = "table" });
            }

            return layout;
        }

        public static bool TablesExist(IList<LayoutInfoEntry> layout) =>
            layout != null && layout.Any(b => b.Class == "table");

        /// <summary>Port of <c>utils.clean_pictures</c> (subset: block types 0,1; vector type 3 skipped if absent).</summary>
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

        /// <summary>Simplified <c>utils.clean_tables</c> (line-density reclassification; no vector <c>table_cleaner</c> split).</summary>
        public static void CleanTables(Page page, List<Block> blocks, List<LayoutInfoEntry> layout)
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

        /// <summary>Port of <c>utils.find_reading_order</c>: filter contained boxes, split header/footer, <c>compute_reading_order</c> on body.</summary>
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

        /// <summary>Port of <c>utils.compute_reading_order</c>: <c>cluster_stripes</c> then <c>cluster_columns_in_stripe</c> per stripe.</summary>
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

        /// <summary>Port of <c>utils.cluster_stripes</c>.</summary>
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

        /// <summary>Port of <c>utils.are_disjoint</c> (non-strict: shared edges count as disjoint).</summary>
        private static bool AreDisjoint(Rect bbox, Rect cell, bool strict = false)
        {
            if (bbox == null || cell == null)
                return true;
            if (!strict)
                return bbox.X0 >= cell.X1 || bbox.X1 <= cell.X0 || bbox.Y0 >= cell.Y1 || bbox.Y1 <= cell.Y0;
            return bbox.X0 > cell.X1 || bbox.X1 < cell.X0 || bbox.Y0 > cell.Y1 || bbox.Y1 < cell.Y0;
        }

        /// <summary>Port of <c>utils.cluster_substripe</c> inside <c>cluster_columns_in_stripe</c>.</summary>
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

        /// <summary>Port of <c>utils.cluster_columns_in_stripe</c>.</summary>
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
        public static (List<Tuple<Point, Point>> lines, List<Rect> boxes) CompleteTableStructure(Page page, List<LayoutInfoEntry> layout)
        {
            return (new List<Tuple<Point, Point>>(), new List<Rect>());
        }

    }
}
