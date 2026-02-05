using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;

namespace MuPDF.NET4LLM.Helpers
{
    /// <summary>
    /// Multi-column page detection utilities.
    /// Ported and adapted from the Python module helpers/multi_column.py in pymupdf4llm.
    /// </summary>
    public static class MultiColumn
    {
        /// <summary>
        /// Determine bboxes which wrap a column on the page
        /// </summary>
        public static List<Rect> ColumnBoxes(
            Page page,
            float footerMargin = 50,
            float headerMargin = 50,
            bool noImageText = true,
            TextPage textpage = null,
            List<PathInfo> paths = null,
            List<Rect> avoid = null,
            bool ignoreImages = false)
        { 
            // Compute relevant page area
            Rect clip = new Rect(page.Rect);
            clip.Y1 -= footerMargin; // Remove footer area
            clip.Y0 += headerMargin; // Remove header area

            if (paths == null)
            {
                paths = page.GetDrawings()
                    .Where(p => p.Rect.Width < clip.Width && p.Rect.Height < clip.Height)
                    .ToList();
            }

            if (textpage == null)
            {
                textpage = page.GetTextPage(clip: clip, flags: (int)TextFlags.TEXT_ACCURATE_BBOXES);
            }

            List<Rect> bboxes = new List<Rect>();
            List<Rect> imgBboxes = new List<Rect>();
            if (avoid != null)
                imgBboxes.AddRange(avoid);

            List<Rect> vertBboxes = new List<Rect>();
            List<Rect> pathRects = new List<Rect>();

            // Path rectangles
            foreach (var p in paths)
            {
                // Give empty path rectangles some small width or height
                Rect prect = new Rect(p.Rect);
                float lwidth = p.Width > 0 ? p.Width * 0.5f : 0.5f;

                if (prect.Width == 0)
                {
                    prect.X0 -= lwidth;
                    prect.X1 += lwidth;
                }
                if (prect.Height == 0)
                {
                    prect.Y0 -= lwidth;
                    prect.Y1 += lwidth;
                }
                pathRects.Add(prect);
            }

            // Sort path bboxes by ascending top, then left coordinates
            pathRects = pathRects.OrderBy(b => (b.Y0, b.X0)).ToList();

            // Bboxes of images on page, no need to sort them
            if (!ignoreImages)
            {
                var images = page.GetImages();
                foreach (var item in images)
                {
                    var boxes = page.GetImageRects(item.Xref);
                    var rects = boxes.Select(b => b.Rect).ToList();
                    imgBboxes.AddRange(rects);
                }
            }

            // Blocks of text on page
            PageInfo pageInfo = textpage.ExtractDict(null, false);
            List<Block> blocks = pageInfo.Blocks;

            // Make block rectangles, ignoring non-horizontal text
            foreach (var b in blocks)
            {
                Rect bbox = new Rect(b.Bbox); // Bbox of the block

                // Ignore text written upon images (bbox contained in any image bbox)
                if (noImageText && Utils.BboxInAnyBbox(bbox, imgBboxes))
                    continue;

                // Confirm first line to be horizontal
                if (b.Lines == null || b.Lines.Count == 0)
                    continue;

                Line line0 = b.Lines[0]; // Get first line
                if (line0.Dir == null || Math.Abs(1 - line0.Dir.X) > 1e-3) // Only (almost) horizontal text
                {
                    vertBboxes.Add(bbox); // A block with non-horizontal text
                    continue;
                }

                Rect srect = Utils.EmptyRect();
                foreach (var line in b.Lines)
                {
                    Rect lbbox = new Rect(line.Bbox);
                    string text = string.Join("", line.Spans?.Select(s => s.Text) ?? new string[0]);
                    if (!Utils.IsWhite(text))
                    {
                        srect = Utils.JoinRects(new List<Rect> { srect, lbbox });
                    }
                }
                bbox = srect;

                if (!Utils.BboxIsEmpty(bbox))
                    bboxes.Add(bbox);
            }

            // Sort text bboxes by ascending background, top, then left coordinates
            bboxes = bboxes.OrderBy(k => (InBbox(k, pathRects), k.Y0, k.X0)).ToList();

            // Immediately return if no text found
            if (bboxes.Count == 0)
                return new List<Rect>();

            // --------------------------------------------------------------------
            // Join bboxes to establish some column structure
            // --------------------------------------------------------------------
            // The final block bboxes on page
            List<Rect> nblocks = new List<Rect> { bboxes[0] }; // Pre-fill with first bbox
            bboxes = bboxes.Skip(1).ToList(); // Remaining old bboxes
            Dictionary<string, int> cache = new Dictionary<string, int>();

            for (int i = 0; i < bboxes.Count; i++) // Iterate old bboxes
            {
                Rect bb = bboxes[i];
                // Skip if already processed (Python sets bboxes[i] = None)
                if (bb == null)
                    continue;

                bool check = false; // Indicates unwanted joins
                int j = -1;
                Rect temp = null;

                // Check if bb can extend one of the new blocks
                for (int jj = 0; jj < nblocks.Count; jj++)
                {
                    Rect nbb = nblocks[jj]; // A new block

                    // Never join across columns
                    if (nbb.X1 < bb.X0 || bb.X1 < nbb.X0)
                        continue;

                    // Never join across different background colors
                    if (InBboxUsingCache(nbb, pathRects, cache) != InBboxUsingCache(bb, pathRects, cache))
                        continue;

                    temp = Utils.JoinRects(new List<Rect> { bb, nbb }); // Temporary extension of new block
                    check = CanExtend(temp, nbb, nblocks, vertBboxes);
                    if (check)
                    {
                        j = jj;
                        break;
                    }
                }

                if (!check) // Bb cannot be used to extend any of the new bboxes
                {
                    nblocks.Add(bb); // So add it to the list
                    j = nblocks.Count - 1; // Index of it
                    temp = nblocks[j]; // New bbox added
                }

                // Check if some remaining bbox is contained in temp (Python always runs this)
                check = CanExtend(temp, bb, bboxes, vertBboxes);
                if (!check)
                    nblocks.Add(bb);
                else
                    nblocks[j] = temp;
                bboxes[i] = null;
            }

            // Do some elementary cleaning
            nblocks = CleanNblocks(nblocks);
            if (nblocks.Count == 0)
                return nblocks;

            // Several phases of rectangle joining
            // TODO: disabled for now as too aggressive:
            // nblocks = JoinRectsPhase1(nblocks);
            nblocks = JoinRectsPhase2(nblocks);
            nblocks = JoinRectsPhase3(nblocks, pathRects, cache);

            // Return identified text bboxes

            //if (textpage != null && textpage != page.GetTextPage())
            //    textpage.Dispose();

            return nblocks;
        }

        private static int InBbox(Rect bb, List<Rect> bboxes)
        {
            for (int i = 0; i < bboxes.Count; i++)
            {
                if (Utils.BboxInBbox(bb, bboxes[i]))
                    return i + 1;
            }
            return 0;
        }

        private static int InBboxUsingCache(Rect bb, List<Rect> bboxes, Dictionary<string, int> cache)
        {
            string cacheKey = $"{bb.GetHashCode()}_{bboxes.GetHashCode()}";
            if (cache.TryGetValue(cacheKey, out int cached))
                return cached;

            int index = InBbox(bb, bboxes);
            cache[cacheKey] = index;
            return index;
        }

        private static bool IntersectsBboxes(Rect bb, List<Rect> bboxes)
        {
            return bboxes.Any(bbox => !Utils.OutsideBbox(bb, bbox, strict: true));
        }

        private static bool CanExtend(Rect temp, Rect bb, List<Rect> bboxlist, List<Rect> vertBboxes)
        {
            foreach (var b in bboxlist)
            {
                if (!IntersectsBboxes(temp, vertBboxes) &&
                    (b == null || b == bb || Utils.BboxIsEmpty(Utils.IntersectRects(temp, b))))
                    continue;
                return false;
            }
            return true;
        }

        private static List<Rect> CleanNblocks(List<Rect> nblocks)
        {
            // 1. Remove any duplicate blocks.
            if (nblocks.Count < 2)
                return nblocks;

            for (int i = nblocks.Count - 1; i > 0; i--)
            {
                if (nblocks[i].EqualTo(nblocks[i - 1]))
                    nblocks.RemoveAt(i);
            }

            if (nblocks.Count == 0)
                return nblocks;

            // 2. Repair sequence in special cases:
            // Consecutive bboxes with almost same bottom value are sorted ascending
            // by x-coordinate.
            float y1 = nblocks[0].Y1; // First bottom coordinate
            int i0 = 0; // Its index
            int i1 = 0; // Index of last bbox with same bottom

            // Iterate over bboxes, identifying segments with approx. same bottom value.
            // Replace every segment by its sorted version.

            for (int i = 1; i < nblocks.Count; i++)
            {
                Rect b1 = nblocks[i];
                if (Math.Abs(b1.Y1 - y1) > 3) // Different bottom
                {
                    if (i1 > i0) // Segment length > 1? Sort it!
                    {
                        var segment = nblocks.Skip(i0).Take(i1 - i0 + 1).OrderBy(b => b.X0).ToList();
                        for (int j = 0; j < segment.Count; j++)
                            nblocks[i0 + j] = segment[j];
                    }
                    y1 = b1.Y1; // Store new bottom value
                    i0 = i; // Store its start index
                }
                i1 = i; // Store current index
            }
            if (i1 > i0) // Segment waiting to be sorted
            {
                var segment = nblocks.Skip(i0).Take(i1 - i0 + 1).OrderBy(b => b.X0).ToList();
                for (int j = 0; j < segment.Count; j++)
                    nblocks[i0 + j] = segment[j];
            }

            return nblocks;
        }

        private static List<Rect> JoinRectsPhase2(List<Rect> bboxes)
        {
            // Postprocess identified text blocks, phase 2.
            // Increase the width of each text block so that small left or right
            // border differences are removed. Then try to join even more text
            // rectangles.
            List<Rect> prects = bboxes.Select(b => new Rect(b)).ToList(); // Copy of argument list

            for (int i = 0; i < prects.Count; i++)
            {
                Rect b = prects[i];
                // Go left and right somewhat
                float x0 = prects.Where(bb => Math.Abs(bb.X0 - b.X0) <= 3).Min(bb => bb.X0);
                float x1 = prects.Where(bb => Math.Abs(bb.X1 - b.X1) <= 3).Max(bb => bb.X1);
                b.X0 = x0; // Store new left / right border
                b.X1 = x1;
                prects[i] = b;
            }

            // Sort by left, top
            prects = prects.OrderBy(b => (b.X0, b.Y0)).ToList();
            List<Rect> newRects = new List<Rect> { prects[0] }; // Initialize with first item

            // Walk through the rest, top to bottom, then left to right
            for (int i = 1; i < prects.Count; i++)
            {
                Rect r = prects[i];
                Rect r0 = newRects[newRects.Count - 1]; // Previous bbox

                // Join if we have similar borders and are not too far down
                if (Math.Abs(r.X0 - r0.X0) <= 3 &&
                    Math.Abs(r.X1 - r0.X1) <= 3 &&
                    Math.Abs(r0.Y1 - r.Y0) <= 10)
                {
                    r0 = Utils.JoinRects(new List<Rect> { r0, r });
                    newRects[newRects.Count - 1] = r0;
                    continue;
                }
                // Else append this as new text block
                newRects.Add(r);
            }
            return newRects;
        }

        private static List<Rect> JoinRectsPhase3(List<Rect> bboxes, List<Rect> pathRects, Dictionary<string, int> cache)
        {
            List<Rect> prects = bboxes.Select(b => new Rect(b)).ToList();
            List<Rect> newRects = new List<Rect>();

            while (prects.Count > 0)
            {
                Rect prect0 = prects[0];
                bool repeat = true;
                while (repeat)
                {
                    repeat = false;
                    for (int i = prects.Count - 1; i >= 0; i--)
                    {
                        Rect prect1 = prects[i];
                        // Do not join across columns
                        if (prect1.X0 > prect0.X1 || prect1.X1 < prect0.X0)
                            continue;

                        // Do not join different backgrounds
                        if (InBboxUsingCache(prect0, pathRects, cache) != InBboxUsingCache(prect1, pathRects, cache))
                            continue;

                        Rect temp = Utils.JoinRects(new List<Rect> { prect0, prect1 });
                        // Python: test = set(tuple(b) for b in prects+new_rects if b.intersects(temp))
                        //        if test == set((tuple(prect0), tuple(prect1))): join
                        var intersecting = prects.Concat(newRects).Where(b => b != null && b.Intersects(temp)).ToList();
                        var intersectingCoords = new HashSet<(float, float, float, float)>(
                            intersecting.Select(b => (b.X0, b.Y0, b.X1, b.Y1)));
                        var needCoords = new HashSet<(float, float, float, float)>
                        {
                            (prect0.X0, prect0.Y0, prect0.X1, prect0.Y1),
                            (prect1.X0, prect1.Y0, prect1.X1, prect1.Y1)
                        };
                        if (intersectingCoords.Count == 2 && intersectingCoords.SetEquals(needCoords))
                        {
                            prect0 = temp;
                            prects[0] = prect0;
                            prects.RemoveAt(i);
                            repeat = true;
                        }
                    }
                }
                newRects.Add(prect0);
                prects.RemoveAt(0);
            }

            // Hopefully the most reasonable sorting sequence:
            // At this point we have finished identifying blocks that wrap text.
            // We now need to determine the SEQUENCE by which text extraction from
            // these blocks should take place. This is hardly possible with 100%
            // certainty. Our sorting approach is guided by the following thought:
            // 1. Extraction should start with the block whose top-left corner is the
            //    left-most and top-most.
            // 2. Any blocks further to the right should be extracted later - even if
            //    their top-left corner is higher up on the page.
            // 3. Sorting the identified rectangles must therefore happen using a
            //    tuple (y, x) as key, where y is not smaller (= higher up) than that
            //    of the left-most block with a non-empty vertical overlap.
            // 4. To continue "left block" with "next is ...", its sort key must be
            //    tuple (P.y, Q.x).
            var sortRects = newRects.Select(box =>
            {
                // Search for the left-most rect that overlaps like "P" above
                // Candidates must have the same background
                int background = InBbox(box, pathRects); // This background
                var leftRects = newRects
                    .Where(r => r.X1 < box.X0 &&
                                (box.Y0 <= r.Y0 && r.Y0 <= box.Y1 || box.Y0 <= r.Y1 && r.Y1 <= box.Y1))
                    .OrderBy(r => r.X1)
                    .ToList();

                (float y, float x) key;
                if (leftRects.Count > 0) // If a "P" rectangle was found ...
                {
                    key = (leftRects[leftRects.Count - 1].Y0, box.X0); // Use this key
                }
                else
                {
                    key = (box.Y0, box.X0); // Else use the original (Q.y, Q.x).
                }
                return (box, key);
            })
            .OrderBy(sr => sr.key) // By computed key
            .Select(sr => sr.box) // Extract sorted rectangles
            .ToList();

            return sortRects;
        }
    }
}
