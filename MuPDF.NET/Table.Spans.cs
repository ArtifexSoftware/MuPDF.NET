// Copyright (C) 2023 Artifex Software, Inc.
//
// This file is part of MuPDF.NET.
//
// MuPDF.NET is free software: you can redistribute it and/or modify it under the
// terms of the GNU Affero General Public License as published by the Free
// Software Foundation, either version 3 of the License, or (at your option)
// any later version.
//
// MuPDF.NET is distributed in the hope that it will be useful, but WITHOUT ANY
// WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with MuPDF. If not, see <https://www.gnu.org/licenses/agpl-3.0.en.html>
//
// Alternative licensing terms are available from the licensor.
// For commercial licensing, see <https://www.artifex.com/> or contact
// Artifex Software, Inc., 39 Mesa Street, Suite 108A, San Francisco,
// CA 94129, USA, for further information.
//
// ---------------------------------------------------------------------
//
// Port of PyMuPDF 1.28.2 src/_table_spans.py
//
// MuPDF.NET table cell-span resolution (opt-in extension).
//
// Provides SpanCell and ResolveSpans (plus the Span* helpers), which
// reconstruct a detected table's merged-cell (colspan / rowspan) structure.
// Re-exported via the table API; never runs on the default FindTables() path.
// Reuses the word-selection helpers of Table.Refine.cs (TableRefine).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MuPDF.NET
{
    /// <summary>
    /// One reconstructed table cell after span resolution (PyMuPDF extension).
    ///
    /// <c>Bbox</c> is the placement's <c>(x0, y0, x1, y1)</c> union rect, <c>Text</c> the
    /// page text it claims (lines joined by <c>\n</c>), and <c>Colspan</c>/<c>Rowspan</c>
    /// how many grid columns/rows it covers. ResolveSpans always sets a real bbox;
    /// a caller padding its own grid may construct SpanCells with <c>Bbox=null</c>.
    ///
    /// <c>Tag</c> is the cell's HTML tag (<c>"td"</c>/<c>"th"</c>), defaulting to <c>"td"</c>.
    /// ResolveSpans leaves it at the default; FindTables(refine: true) overwrites
    /// it from the resolved header region so Table.ToHtml() can serialize the grid
    /// directly, and a caller building its own grid may set it too.
    /// </summary>
    public class SpanCell
    {
        /// <summary>Placement union rect <c>(x0, y0, x1, y1)</c>, or null when padding a grid.</summary>
        public (float x0, float y0, float x1, float y1)? Bbox { get; set; }

        public string Text { get; set; }

        public int Colspan { get; set; }

        public int Rowspan { get; set; }

        /// <summary>HTML tag (<c>"td"</c>/<c>"th"</c>); ResolveSpans leaves the default.</summary>
        public string Tag { get; set; }

        public SpanCell(
            (float x0, float y0, float x1, float y1)? bbox,
            string text,
            int colspan,
            int rowspan,
            string tag = "td")
        {
            Bbox = bbox;
            Text = text ?? "";
            Colspan = colspan;
            Rowspan = rowspan;
            Tag = tag ?? "td";
        }

        // snake_case aliases (PyMuPDF attribute names)
        public (float x0, float y0, float x1, float y1)? bbox
        {
            get => Bbox;
            set => Bbox = value;
        }

        public string text
        {
            get => Text;
            set => Text = value;
        }

        public int colspan
        {
            get => Colspan;
            set => Colspan = value;
        }

        public int rowspan
        {
            get => Rowspan;
            set => Rowspan = value;
        }

        public string tag
        {
            get => Tag;
            set => Tag = value;
        }
    }

    /// <summary>
    /// Table cell-span resolution (PyMuPDF <c>_table_spans</c> / <c>resolve_spans</c>).
    /// Uses <see cref="TableRefine.PageWords"/> / <see cref="TableRefine.IsVerticalOrRotated"/>.
    /// </summary>
    internal static class TableSpans
    {

        static readonly ConditionalWeakTable<Page, List<(Rect rect, string text)>> VerticalLinesCache =
            new ConditionalWeakTable<Page, List<(Rect rect, string text)>>();
        static readonly ConditionalWeakTable<Page, List<(Rect rect, string text)>> TextSpansCache =
            new ConditionalWeakTable<Page, List<(Rect rect, string text)>>();

        sealed class BaseCell
        {
            public int Row;
            public int Position;
            public Rect Rect;
            public (int start, int end) Cols;
            public int Colspan;
            public string Role;
        }

        sealed class LineBucket
        {
            public float CenterY;
            public List<(float x0, string text)> Words = new List<(float, string)>();
        }

        // --- slot geometry: cluster cell edges into column/row boundaries ------------

        /// <summary>
        /// Greedy 1-D clustering of edge coordinates into slot boundaries.
        ///
        /// Keeps a sorted value only when it is more than <c>tolerance</c> from the last
        /// kept boundary, so the first value of each run is the retained boundary.
        /// </summary>
        static List<float> SpanClusteredBoundaries(IEnumerable<float> values, float tolerance = 3.0f)
        {
            var boundaries = new List<float>();
            foreach (float value in values.OrderBy(v => v))
            {
                if (boundaries.Count > 0 && value - boundaries[boundaries.Count - 1] <= tolerance)
                    continue;
                boundaries.Add(value);
            }
            return boundaries;
        }

        /// <summary>
        /// How many [boundaries[i], boundaries[i+1]] slots the span start..end covers.
        /// </summary>
        static int SpanCoveredSlotCount(float start, float end, List<float> boundaries, float tolerance = 1.0f)
        {
            int count = 0;
            for (int index = 0; index < boundaries.Count - 1; index++)
            {
                float midpoint = (boundaries[index] + boundaries[index + 1]) / 2.0f;
                if (start - tolerance <= midpoint && midpoint <= end + tolerance)
                    count += 1;
            }
            return Math.Max(1, count);
        }

        /// <summary>
        /// The half-open column-slot range (first, last+1) a rect's x-extent covers.
        /// </summary>
        static (int start, int end) SpanSlotRange(Rect rect, List<float> xBoundaries, float tolerance = 1.0f)
        {
            var hits = new List<int>();
            for (int index = 0; index < xBoundaries.Count - 1; index++)
            {
                float midpoint = (xBoundaries[index] + xBoundaries[index + 1]) / 2.0f;
                if (rect.X0 - tolerance <= midpoint && midpoint <= rect.X1 + tolerance)
                    hits.Add(index);
            }
            if (hits.Count == 0)
                return (0, 1);
            return (hits.Min(), hits.Max() + 1);
        }

        static bool SpanRangesIntersect((int start, int end) a, (int start, int end) b)
            => a.start < b.end && b.start < a.end;

        static bool SpanPointInRect(float x, float y, Rect rect)
            => (float)rect.X0 <= x && x <= (float)rect.X1
            && (float)rect.Y0 <= y && y <= (float)rect.Y1;

        static Rect SpanRectUnion(IList<Rect> rects)
        {
            return new Rect(
                rects.Min(r => r.X0),
                rects.Min(r => r.Y0),
                rects.Max(r => r.X1),
                rects.Max(r => r.Y1));
        }

        static float SpanXOverlap(Rect a, Rect b)
            => Math.Min(a.X1, b.X1) - Math.Max(a.X0, b.X0);

        /// <summary>
        /// True if a text span overlaps a cell by enough x to signal a merge.
        ///
        /// A few points of overlap is noise (a span drifting across a column line with
        /// trailing whitespace or a currency glyph); require &gt;2pt and &gt;=15% of the cell
        /// width before treating it as a merged-cell signal.
        /// </summary>
        static bool SpanSubstantialXOverlap(Rect spanRect, Rect cellRect)
        {
            float overlap = SpanXOverlap(spanRect, cellRect);
            if (overlap <= 2.0f)
                return false;
            float cellWidth = Math.Max(1.0f, cellRect.Width);
            return overlap / cellWidth >= 0.15f;
        }

        /// <summary>Collapse a sorted index list into (start, end) inclusive runs.</summary>
        static List<(int start, int end)> SpanContiguousRanges(IList<int> indices)
        {
            if (indices == null || indices.Count == 0)
                return new List<(int, int)>();
            var ranges = new List<(int, int)>();
            int start = indices[0];
            int end = indices[0];
            for (int i = 1; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index == end + 1)
                {
                    end = index;
                    continue;
                }
                ranges.Add((start, end));
                start = end = index;
            }
            ranges.Add((start, end));
            return ranges;
        }

        /// <summary>Merge overlapping (start, end, text) intervals, accumulating their texts.</summary>
        static List<(int start, int end, List<string> texts)> SpanMergeIntervals(
            IEnumerable<(int start, int end, string text)> intervals)
        {
            var merged = new List<(int start, int end, List<string> texts)>();
            foreach (var item in intervals.OrderBy(i => i.start).ThenBy(i => i.end))
            {
                int start = item.start;
                int end = item.end;
                string text = item.text;
                if (start == end)
                    continue;
                if (merged.Count == 0 || start > merged[merged.Count - 1].end)
                {
                    merged.Add((start, end, new List<string> { text }));
                    continue;
                }
                var prev = merged[merged.Count - 1];
                var texts = new List<string>(prev.texts) { text };
                merged[merged.Count - 1] = (prev.start, Math.Max(prev.end, end), texts);
            }
            return merged;
        }

        // --- cell text: page words + vertical-text lines claimed by a placement -------

        /// <summary>Space-joined non-empty span texts of a get_text("dict") line.</summary>
        static string SpanLineText(Line line)
        {
            if (line?.Spans == null)
                return "";
            var parts = line.Spans
                .Select(span => (span?.Text ?? "").Trim())
                .Where(t => t.Length > 0);
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Bounding rect of a dict line (its bbox, else the union of its span bboxes).
        /// </summary>
        static Rect SpanLineRect(Line line)
        {
            var bbox = line?.Bbox;
            if (bbox != null && !bbox.IsEmpty)
                return bbox;
            var rects = (line?.Spans ?? new List<Span>())
                .Where(span => span?.Bbox != null && !span.Bbox.IsEmpty)
                .Select(span => span.Bbox)
                .ToList();
            if (rects.Count == 0)
                return null;
            return SpanRectUnion(rects);
        }

        /// <summary>
        /// Vertical/rotated text lines as (rect, text), cached on the page.
        ///
        /// Selects non-horizontal lines whose reading order get_text("dict") already
        /// preserves.
        /// </summary>
        static List<(Rect rect, string text)> SpanVerticalTextLines(Page page)
        {
            if (VerticalLinesCache.TryGetValue(page, out var cached))
                return cached;

            var lines = new List<(Rect, string)>();
            try
            {
                var dict = page.GetText("dict") as PageInfo;
                if (dict?.Blocks != null)
                {
                    foreach (var block in dict.Blocks)
                    {
                        if (block == null || (block.Type != 0))
                            continue;
                        if (block.Lines == null)
                            continue;
                        foreach (var line in block.Lines)
                        {
                            if (!TableRefine.IsVerticalOrRotated(line))
                                continue;
                            string text = SpanLineText(line);
                            if (string.IsNullOrEmpty(text))
                                continue;
                            var rect = SpanLineRect(line);
                            if (rect == null)
                                continue;
                            lines.Add((rect, text));
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            try { VerticalLinesCache.Add(page, lines); }
            catch { /* already cached */ }
            return lines;
        }

        /// <summary>
        /// Text of vertical lines centered in rect, when they dominate the rect.
        ///
        /// Returns the stacked line text only if vertical lines are centered in the rect,
        /// cover &gt;=60% of the rect's selected words, and carry &gt;=2 tokens; else null so
        /// the caller falls back to horizontal line synthesis.
        /// </summary>
        static string SpanVerticalTextForRect(
            Page page,
            Rect rect,
            List<(int index, (float x0, float y0, float x1, float y1, string text) word)> selectedWords)
        {
            if (selectedWords == null || selectedWords.Count == 0)
                return null;
            var candidates = new List<(Rect rect, string text)>();
            foreach (var (lineRect, text) in SpanVerticalTextLines(page))
            {
                float cx = (lineRect.X0 + lineRect.X1) * 0.5f;
                float cy = (lineRect.Y0 + lineRect.Y1) * 0.5f;
                if (SpanPointInRect(cx, cy, rect))
                    candidates.Add((lineRect, text));
            }
            if (candidates.Count == 0)
                return null;

            int verticalHits = 0;
            foreach (var (_, word) in selectedWords)
            {
                float cx = (word.x0 + word.x1) * 0.5f;
                float cy = (word.y0 + word.y1) * 0.5f;
                if (candidates.Any(c => SpanPointInRect(cx, cy, c.rect)))
                    verticalHits += 1;
            }
            if (verticalHits / (float)Math.Max(1, selectedWords.Count) < 0.6f)
                return null;
            if (candidates.Sum(c => c.text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length) < 2)
                return null;

            return string.Join("\n",
                candidates
                    .OrderBy(item => item.rect.X0)
                    .ThenByDescending(item => (item.rect.Y0 + item.rect.Y1) * 0.5f)
                    .Select(item => item.text));
        }

        /// <summary>
        /// Join center-point-selected cell words into text, re-synthesizing lines.
        ///
        /// <c>words</c> are (y0, x0, y1, text) tuples; they are grouped into lines by an
        /// adaptive median-height nearest-line rule, each line's words ordered by x, and
        /// the lines joined by newlines.
        /// </summary>
        static string SpanWordsToLineText(IList<(float y0, float x0, float y1, string text)> words)
        {
            if (words == null || words.Count == 0)
                return "";
            var heights = words.Select(w => Math.Max(0.1f, w.y1 - w.y0)).ToList();
            float medianHeight = heights.OrderBy(h => h).ElementAt(heights.Count / 2);
            float lineThreshold = Math.Max(2.0f, medianHeight * 0.55f);
            var lines = new List<LineBucket>();
            foreach (var (y0, x0, y1, text) in words.OrderBy(w => w.y0).ThenBy(w => w.x0))
            {
                float cy = (y0 + y1) / 2.0f;
                LineBucket bestLine = null;
                float bestDistance = lineThreshold;
                foreach (var line in lines)
                {
                    float distance = Math.Abs(cy - line.CenterY);
                    if (distance <= bestDistance)
                    {
                        bestLine = line;
                        bestDistance = distance;
                    }
                }
                if (bestLine == null)
                {
                    lines.Add(new LineBucket
                    {
                        CenterY = cy,
                        Words = new List<(float, string)> { (x0, text) }
                    });
                    continue;
                }
                bestLine.Words.Add((x0, text));
                int count = bestLine.Words.Count;
                bestLine.CenterY = (bestLine.CenterY * (count - 1) + cy) / count;
            }
            var textLines = new List<string>();
            foreach (var line in lines.OrderBy(l => l.CenterY))
            {
                textLines.Add(string.Join(" ",
                    line.Words.OrderBy(w => w.x0).Select(w => w.text)));
            }
            return string.Join("\n", textLines);
        }

        /// <summary>
        /// Reorder a (x0, y0, x1, y1, text) word to the (y0, x0, y1, text) line tuple.
        /// </summary>
        static (float y0, float x0, float y1, string text) SpanWordLineTuple(
            (float x0, float y0, float x1, float y1, string text) word)
            => (word.y0, word.x0, word.y1, word.text ?? "");

        /// <summary>
        /// (index, word) pairs whose center lies in rect, index into <c>pageWords</c>.
        ///
        /// The index is what lets ResolveSpans claim each page word for exactly one
        /// placement (an earlier cell's word is not re-claimed by a later one).
        /// </summary>
        static List<(int index, (float x0, float y0, float x1, float y1, string text) word)> SpanSelectWordsInRect(
            List<(float x0, float y0, float x1, float y1, string text)> pageWords,
            Rect rect)
        {
            var selected = new List<(int, (float, float, float, float, string))>();
            if (pageWords == null)
                return selected;
            for (int index = 0; index < pageWords.Count; index++)
            {
                var word = pageWords[index];
                if (string.IsNullOrWhiteSpace(word.text))
                    continue;
                float cx = (word.x0 + word.x1) * 0.5f;
                float cy = (word.y0 + word.y1) * 0.5f;
                if (SpanPointInRect(cx, cy, rect))
                    selected.Add((index, word));
            }
            return selected;
        }

        // --- strict-colspan gate: reject a merge that fights the header/body split ----
        // When strictColspan is set, a header cell may only merge across columns if the
        // body rows below actually split there (and vice-versa for a body cell against a
        // leaf header). This keeps a stray text span from collapsing a real column.

        /// <summary>
        /// Text for a rect: vertical-line text if it dominates, else line synthesis.
        /// </summary>
        static string SpanWordsTextForRect(
            Page page,
            Rect rect,
            List<(int index, (float x0, float y0, float x1, float y1, string text) word)> selectedWords)
        {
            string verticalText = SpanVerticalTextForRect(page, rect, selectedWords);
            if (verticalText != null)
                return verticalText;
            return SpanWordsToLineText(
                selectedWords.Select(sw => SpanWordLineTuple(sw.word)).ToList());
        }

        /// <summary>
        /// Text of rect's words, skipping words already claimed and claiming the rest.
        /// </summary>
        static string SpanClaimTextInRect(
            Page page,
            Rect rect,
            List<(float x0, float y0, float x1, float y1, string text)> pageWords,
            HashSet<int> claimedWords)
        {
            var selected = SpanSelectWordsInRect(pageWords, rect)
                .Where(sw => !claimedWords.Contains(sw.index))
                .ToList();
            foreach (var (index, _) in selected)
                claimedWords.Add(index);
            return SpanWordsTextForRect(page, rect, selected);
        }

        /// <summary>
        /// Page text spans as (rect, text), length&gt;=2, cached on the page.
        ///
        /// These drive merged-cell detection: a single span whose x-extent crosses a
        /// grid column line signals cells the line grid split but text joins.
        /// </summary>
        static List<(Rect rect, string text)> SpanTextSpans(Page page)
        {
            if (TextSpansCache.TryGetValue(page, out var cached))
                return cached;

            var spans = new List<(Rect, string)>();
            try
            {
                var dict = page.GetText("dict") as PageInfo;
                if (dict?.Blocks != null)
                {
                    foreach (var block in dict.Blocks)
                    {
                        if (block?.Lines == null)
                            continue;
                        foreach (var line in block.Lines)
                        {
                            if (line?.Spans == null)
                                continue;
                            foreach (var span in line.Spans)
                            {
                                string text = (span?.Text ?? "").Trim();
                                if (text.Length < 2)
                                    continue;
                                var rect = span.Bbox;
                                if (rect == null || rect.IsEmpty)
                                    continue;
                                spans.Add((rect, text));
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            try { TextSpansCache.Add(page, spans); }
            catch { /* already cached */ }
            return spans;
        }

        /// <summary>
        /// Column ranges within one grid row that a single text span crosses.
        ///
        /// <c>entries</c> are the row's cell rects (in column order). A text span centered
        /// in the row band and substantially overlapping &gt;=2 adjacent cells marks those
        /// cells for merging; overlapping intervals are merged. Returns
        /// (start, end, texts) with end&gt;start.
        /// </summary>
        static List<(int start, int end, List<string> texts)> SpanCrossingIntervals(
            IList<Rect> entries,
            List<(Rect rect, string text)> textSpans)
        {
            if (entries == null || entries.Count < 2)
                return new List<(int, int, List<string>)>();
            float rowY0 = entries.Min(e => e.Y0);
            float rowY1 = entries.Max(e => e.Y1);
            var intervals = new List<(int start, int end, string text)>();
            foreach (var (spanRect, spanText) in textSpans)
            {
                float centerY = (spanRect.Y0 + spanRect.Y1) / 2.0f;
                if (centerY < rowY0 - 1.0f || centerY > rowY1 + 1.0f)
                    continue;
                var hitPositions = new List<int>();
                for (int position = 0; position < entries.Count; position++)
                {
                    if (SpanSubstantialXOverlap(spanRect, entries[position]))
                        hitPositions.Add(position);
                }
                if (hitPositions.Count < 2)
                    continue;
                foreach (var (start, end) in SpanContiguousRanges(hitPositions))
                {
                    if (end > start)
                        intervals.Add((start, end, spanText));
                }
            }
            return SpanMergeIntervals(intervals);
        }

        static bool SpanIsLeafHeaderRange(
            int rowIdx, (int start, int end) cols, List<BaseCell> baselist, int bodyStart)
        {
            if (rowIdx >= bodyStart)
                return false;
            foreach (var other in baselist)
            {
                if (other.Role != "header" && other.Role != "header_leaf" && other.Role != "header_group")
                    continue;
                if (other.Row <= rowIdx || other.Row >= bodyStart)
                    continue;
                if (SpanRangesIntersect(cols, other.Cols))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Describe every grid cell by row/slot-range/role for the strict-colspan gate.
        ///
        /// Header cells split into <c>header_leaf</c> (no header cell below within body) vs
        /// <c>header_group</c> (spans over a lower leaf), which the reject rules compare
        /// against the body split.
        /// </summary>
        static List<BaseCell> SpanBuildBaseCells(
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            List<float> xBoundaries,
            int bodyStart)
        {
            var baselist = new List<BaseCell>();
            for (int rowIdx = 0; rowIdx < cells.Count; rowIdx++)
            {
                var row = cells[rowIdx] ?? new List<(float x0, float y0, float x1, float y1)?>();
                var nonempty = row.Where(cell => cell != null).ToList();
                for (int position = 0; position < nonempty.Count; position++)
                {
                    var rect = CellToRect(nonempty[position].Value);
                    var cols = SpanSlotRange(rect, xBoundaries);
                    baselist.Add(new BaseCell
                    {
                        Row = rowIdx,
                        Position = position,
                        Rect = rect,
                        Cols = cols,
                        Colspan = cols.end - cols.start,
                        Role = rowIdx >= bodyStart ? "body" : "header",
                    });
                }
            }
            foreach (var item in baselist)
            {
                if (item.Role != "header")
                    continue;
                item.Role = SpanIsLeafHeaderRange(item.Row, item.Cols, baselist, bodyStart)
                    ? "header_leaf"
                    : "header_group";
            }
            return baselist;
        }

        /// <summary>
        /// True if some body row splits the column range <c>cols</c> into &gt;1 cell.
        /// </summary>
        static bool SpanBodyRowsSplitUnder(
            (int start, int end) cols, List<BaseCell> baselist, int bodyStart)
        {
            var bodyRows = baselist
                .Where(item => item.Role == "body" && item.Row >= bodyStart)
                .Select(item => item.Row)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
            foreach (int rowIdx in bodyRows)
            {
                var hits = baselist
                    .Where(item => item.Role == "body"
                        && item.Row == rowIdx
                        && SpanRangesIntersect(cols, item.Cols))
                    .ToList();
                if (hits.Count == 0)
                    continue;
                if (hits.Count == 1 && hits[0].Cols == cols)
                    continue;
                int coveredStart = hits.Min(item => Math.Max(cols.start, item.Cols.start));
                int coveredEnd = hits.Max(item => Math.Min(cols.end, item.Cols.end));
                if (coveredStart <= cols.start && coveredEnd >= cols.end)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True if leaf-header cells split the column range <c>cols</c> into &gt;1 cell.
        /// </summary>
        static bool SpanHeaderLeafsSplitOver((int start, int end) cols, List<BaseCell> baselist)
        {
            var hits = baselist
                .Where(item => item.Role == "header_leaf" && SpanRangesIntersect(cols, item.Cols))
                .ToList();
            if (hits.Count == 0)
                return false;
            if (hits.Count == 1 && hits[0].Cols == cols)
                return false;
            int coveredStart = hits.Min(item => Math.Max(cols.start, item.Cols.start));
            int coveredEnd = hits.Max(item => Math.Min(cols.end, item.Cols.end));
            return coveredStart <= cols.start && coveredEnd >= cols.end;
        }

        /// <summary>
        /// Whether to reject a candidate merge over <c>cols</c> as a (reason,) mismatch.
        /// </summary>
        static (bool reject, string reason) SpanRejectColspanMismatchMerge(
            int rowIdx, (int start, int end) cols, List<BaseCell> baselist, int bodyStart)
        {
            if (cols.end - cols.start <= 1)
                return (false, "");
            if (rowIdx < bodyStart)
            {
                if (!SpanIsLeafHeaderRange(rowIdx, cols, baselist, bodyStart))
                    return (false, "");
                if (SpanBodyRowsSplitUnder(cols, baselist, bodyStart))
                    return (true, "header_leaf_colspan_changed_against_body_split");
                return (false, "");
            }
            if (SpanHeaderLeafsSplitOver(cols, baselist))
                return (true, "body_colspan_changed_against_header_leaf_split");
            return (false, "");
        }

        static List<string> SpanCellTextsForEntries(
            Page page,
            IList<Rect> entries,
            int start,
            int end,
            List<(float x0, float y0, float x1, float y1, string text)> pageWords)
        {
            var texts = new List<string>();
            for (int i = start; i <= end; i++)
            {
                var words = SpanSelectWordsInRect(pageWords, entries[i]);
                texts.Add(SpanWordsTextForRect(page, entries[i], words));
            }
            return texts;
        }

        /// <summary>
        /// Allow an otherwise-rejected header merge when a covered part is empty.
        ///
        /// A header leaf that would be rejected against a body split is still merged if
        /// one of the merged parts has no text (an empty header slot the body fills).
        /// </summary>
        static bool SpanAllowHeaderColspanMergeWithEmptyPart(
            string reason,
            Page page,
            IList<Rect> entries,
            int start,
            int end,
            List<(float x0, float y0, float x1, float y1, string text)> pageWords)
        {
            if (reason != "header_leaf_colspan_changed_against_body_split")
                return false;
            var partTexts = SpanCellTextsForEntries(page, entries, start, end, pageWords)
                .Select(t => (t ?? "").Trim())
                .ToList();
            return !partTexts.All(t => t.Length > 0);
        }

        /// <summary>
        /// Resolve a detected table's merged-cell (colspan/rowspan) structure.
        ///
        /// <c>cells</c> is a row-major grid: a list of rows, each a list of
        /// <c>(x0, y0, x1, y1)</c> cell rectangles (null for a gap) -- the same grid
        /// shape <see cref="TableRefine.RefineGrid"/> accepts. Returns a row-major
        /// grid of <see cref="SpanCell"/> placements, ragged where cells span: each
        /// placement carries its union <c>Bbox</c>, the page <c>Text</c> it claims, and
        /// its <c>Colspan</c>/<c>Rowspan</c> (how many clustered column/row slots it
        /// covers). Every page word is claimed by at most one placement.
        ///
        /// <c>headerRowCount</c> is the number of leading header rows (default a
        /// conservative 1); it only matters together with <c>strictColspan</c>.
        /// <c>strictColspan</c> (default false), when set, refuses a merge whose colspan
        /// would contradict the header/body column split. This is a PyMuPDF extension;
        /// it reads page text/graphics but does not mutate the page.
        /// </summary>
        internal static List<List<SpanCell>> ResolveSpans(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            int? headerRowCount = null,
            bool strictColspan = false)
        {
            int rows = cells?.Count ?? 0;

            var xEdges = new List<float>();
            var yEdges = new List<float>();
            if (cells != null)
            {
                foreach (var row in cells)
                {
                    if (row == null)
                        continue;
                    foreach (var cell in row)
                    {
                        if (cell == null)
                            continue;
                        var rect = CellToRect(cell.Value);
                        xEdges.Add(rect.X0);
                        xEdges.Add(rect.X1);
                        yEdges.Add(rect.Y0);
                        yEdges.Add(rect.Y1);
                    }
                }
            }

            var xBoundaries = SpanClusteredBoundaries(xEdges);
            var yBoundaries = SpanClusteredBoundaries(yEdges);
            int bodyStart = rows > 0
                ? Math.Max(1, Math.Min(headerRowCount ?? 1, rows))
                : 0;
            var baseCells = strictColspan
                ? SpanBuildBaseCells(cells, xBoundaries, bodyStart)
                : new List<BaseCell>();

            var textSpans = SpanTextSpans(page);
            var pageWords = TableRefine.PageWords(page);
            var claimedWords = new HashSet<int>();
            var placements = new List<List<SpanCell>>();

            for (int rowIdx = 0; rowIdx < rows; rowIdx++)
            {
                var row = cells[rowIdx] ?? new List<(float x0, float y0, float x1, float y1)?>();
                var placementRow = new List<SpanCell>();
                var entries = row
                    .Where(cell => cell != null)
                    .Select(cell => CellToRect(cell.Value))
                    .ToList();
                var intervals = SpanCrossingIntervals(entries, textSpans);
                var intervalsByStart = new Dictionary<int, (int end, List<string> texts)>();
                foreach (var (start, end, texts) in intervals)
                    intervalsByStart[start] = (end, texts);

                int position = 0;
                while (position < entries.Count)
                {
                    Rect rect;
                    if (intervalsByStart.TryGetValue(position, out var interval))
                    {
                        int endPosition = interval.end;
                        rect = SpanRectUnion(entries.GetRange(position, endPosition - position + 1));
                        if (strictColspan)
                        {
                            var candidateCols = SpanSlotRange(rect, xBoundaries);
                            var (reject, reason) = SpanRejectColspanMismatchMerge(
                                rowIdx, candidateCols, baseCells, bodyStart);
                            if (reject && SpanAllowHeaderColspanMergeWithEmptyPart(
                                    reason, page, entries, position, endPosition, pageWords))
                                reject = false;
                            if (reject)
                            {
                                rect = entries[position];
                                position += 1;
                            }
                            else
                                position = endPosition + 1;
                        }
                        else
                            position = endPosition + 1;
                    }
                    else
                    {
                        rect = entries[position];
                        position += 1;
                    }

                    if (rect == null || rect.IsEmpty)
                        continue;
                    int colspan = SpanCoveredSlotCount(rect.X0, rect.X1, xBoundaries);
                    int rowspan = SpanCoveredSlotCount(rect.Y0, rect.Y1, yBoundaries);
                    placementRow.Add(new SpanCell(
                        bbox: (rect.X0, rect.Y0, rect.X1, rect.Y1),
                        text: SpanClaimTextInRect(page, rect, pageWords, claimedWords),
                        colspan: colspan,
                        rowspan: rowspan));
                }
                placements.Add(placementRow);
            }

            return placements;
        }

        static Rect CellToRect((float x0, float y0, float x1, float y1) cell)
            => new Rect(cell.x0, cell.y0, cell.x1, cell.y1);
}
}
