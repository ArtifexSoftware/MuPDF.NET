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
// Port of PyMuPDF 1.28.2 src/_table_refine.py
// plus find_tables(refine=True) reconstruction glue from src/table.py.
//
// MuPDF.NET table grid refinement (opt-in extension).
//
// Provides the RefineGrid / RefineGridStructure / RefineGridRows API that
// refines a detected table's cell grid using page text and vector-graphics
// geometry. Re-exported via the table API; never runs on the default
// FindTables() path.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MuPDF.NET
{
    /// <summary>
    /// Table grid refinement (PyMuPDF <c>_table_refine</c> + refine helpers from <c>table.py</c>).
    /// Opt-in via <c>FindTables(refine: true)</c>; never runs on the default path.
    /// </summary>
    internal static partial class TableRefine
    {

        // Grid refinement runs three splitters, each taking (page, grid) and returning a
        // grid without mutating the page:
        //   * SplitShadedRows            split rows the line grid merged but a cell
        //                                background-shading rectangle separates,
        //   * SplitUndersegmentedColumns split a column that jams several values into
        //                                one cell,
        //   * SplitOvermergedRows        split body rows that collapsed several
        //                                records into a single grid row.
        // Word selection uses center-point cell membership with rotated/vertical span
        // substitution; it is independent of the CHARS/extract_words path in
        // Table.cs, so refinement needs no CHARS state.



        // grid without mutating the page:
        //   * SplitShadedRows            split rows the line grid merged but a cell
        //                                background-shading rectangle separates,
        //   * SplitUndersegmentedColumns split a column that jams several values into
        //                                one cell,
        //   * SplitOvermergedRows        split body rows that collapsed several
        //                                records into a single grid row.
        // Word selection uses center-point cell membership with rotated/vertical span
        // substitution; it is independent of the CHARS/extract_words path in
        // Table.cs, so refinement needs no CHARS state.

        const float RefineLineGap = 3.0f;  
// center-y gap (points) that groups body words into lines

        static readonly ConditionalWeakTable<Page, List<(float x0, float y0, float x1, float y1, string text)>> RefineWordsCache =
            new ConditionalWeakTable<Page, List<(float x0, float y0, float x1, float y1, string text)>>();
        // Grid refinement runs three splitters, each taking (page, grid) and returning a
        // --- word selection: center-point membership + rotated-span substitution -----

        sealed class RawSpan
        {
            public Rect Bbox;
            public string Text;
            public Point Dir;
            public int? WMode;
        }

        sealed class ClusterLine
        {
            public float X0, Y0, X1, Y1;
            public string Text;
        }

        sealed class OvermergeMeta
        {
            public float[] BodyBbox;
            public List<(float x0, float x1)> ColBounds;
            public List<(float y0, float y1)> RecordLineBounds;
            public int ExistingBodyRows;
            public int HeaderRowCount;
        }

        /// <summary>
        /// Flattened rawdict text spans carrying line-direction metadata.
        ///
        /// Only used to locate vertical/rotated text so those words can be replaced by
        /// span-level bboxes in PageWords / BuildPageWords. Minimal fields (bbox/text/dir/wmode)
        /// -- the only ones the consumers read.
        /// </summary>
        static List<RawSpan> RawdictSpans(Page page)
        {
            var spans = new List<RawSpan>();
            PageInfo raw = null;
            try
            {
                raw = page.GetText("rawdict") as PageInfo;
            }
            catch
            {
                return spans;
            }
            if (raw?.Blocks == null)
                return spans;

            foreach (var block in raw.Blocks)
            {
                if (block == null || block.Type != 0 || block.Lines == null)
                    continue;
                foreach (var line in block.Lines)
                {
                    if (line?.Spans == null)
                        continue;
                    Point direction = line.Dir;
                    int? wmode = line.WMode;
                    foreach (var span in line.Spans)
                    {
                        if (span == null)
                            continue;
                        string text = span.Text;
                        if (string.IsNullOrEmpty(text) && span.Chars != null)
                            text = string.Concat(span.Chars.Select(ch => ch?.C.ToString() ?? ""));
                        if (string.IsNullOrWhiteSpace(text))
                            continue;
                        spans.Add(new RawSpan
                        {
                            Bbox = span.Bbox != null ? new Rect(span.Bbox) : new Rect(),
                            Text = text,
                            Dir = direction,
                            WMode = wmode,
                        });
                    }
                }
            }
            return spans;
        }

        /// <summary>
        /// True when a rawdict span's direction indicates non-horizontal text flow.
        ///
        /// wmode != 0 is vertical; otherwise compare |dir_x| vs |dir_y| (a missing or
        /// unparsable direction counts as horizontal).
        /// </summary>
        internal static bool IsVerticalOrRotated(int? wmode, Point dir)
        {
            if (wmode is int wm && wm != 0)
                return true;
            if (dir == null)
                return false;
            try
            {
                float dx = Math.Abs(dir.X);
                float dy = Math.Abs(dir.Y);
                return dy > dx;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// True when a line/span direction indicates non-horizontal text flow.
        ///
        /// wmode != 0 is vertical; otherwise compare |dir_x| vs |dir_y| (a missing or
        /// unparsable direction counts as horizontal).
        /// </summary>
        internal static bool IsVerticalOrRotated(Line line)
        {
            if (line == null)
                return false;
            return IsVerticalOrRotated(line.WMode, line.Dir);
        }

        /// <summary>
        /// Dictionary/span-info overload for callers that pass
        /// <c>{"wmode", "dir", ...}</c>.
        /// </summary>
        internal static bool IsVerticalOrRotated(IDictionary<string, object> span)
        {
            if (span == null)
                return false;
            int? wmode = null;
            if (span.TryGetValue("wmode", out var wm) && wm != null)
            {
                try { wmode = Convert.ToInt32(wm, CultureInfo.InvariantCulture); }
                catch { wmode = null; }
            }
            Point dir = null;
            if (span.TryGetValue("dir", out var d) && d != null)
            {
                if (d is Point p)
                    dir = p;
                else if (d is IList list && list.Count >= 2)
                {
                    try
                    {
                        dir = new Point(
                            Convert.ToSingle(list[0], CultureInfo.InvariantCulture),
                            Convert.ToSingle(list[1], CultureInfo.InvariantCulture));
                    }
                    catch { dir = null; }
                }
            }
            return IsVerticalOrRotated(wmode, dir);
        }

        static List<(float x0, float y0, float x1, float y1, string text)> BuildPageWords(Page page)
        {
            var words = new List<(float x0, float y0, float x1, float y1, string text)>();
            try
            {
                var rawWords = page.GetText("words") as IEnumerable;
                if (rawWords != null)
                {
                    foreach (var item in rawWords)
                    {
                        if (item is WordBlock wb)
                        {
                            string t = wb.Text ?? "";
                            if (string.IsNullOrWhiteSpace(t))
                                continue;
                            words.Add((wb.X0, wb.Y0, wb.X1, wb.Y1, t));
                        }
                    }
                }
            }
            catch
            {
                // ignore extraction failures
            }

            var rotatedSpans = RawdictSpans(page)
                .Where(span => IsVerticalOrRotated(span.WMode, span.Dir)
                               && !string.IsNullOrWhiteSpace(span.Text))
                .ToList();
            if (rotatedSpans.Count == 0)
                return words;

            var rotatedRects = rotatedSpans
                .Select(span => span.Bbox ?? new Rect())
                .ToList();
            var kept = new List<(float x0, float y0, float x1, float y1, string text)>();
            foreach (var word in words)
            {
                var center = new Point((word.x0 + word.x1) * 0.5f, (word.y0 + word.y1) * 0.5f);
                bool inside = false;
                foreach (var rect in rotatedRects)
                {
                    if (rect != null && !rect.IsEmpty && rect.Contains(center))
                    {
                        inside = true;
                        break;
                    }
                }
                if (!inside)
                    kept.Add(word);
            }
            for (int i = 0; i < rotatedSpans.Count; i++)
            {
                var rect = rotatedRects[i];
                if (rect == null || rect.IsEmpty)
                    continue;
                kept.Add((rect.X0, rect.Y0, rect.X1, rect.Y1, rotatedSpans[i].Text));
            }
            kept.Sort((a, b) =>
            {
                int cy = a.y0.CompareTo(b.y0);
                return cy != 0 ? cy : a.x0.CompareTo(b.x0);
            });
            return kept;
        }

        /// <summary>
        /// Center-point word list for grid refinement, cached on the page via ConditionalWeakTable.
        ///
        /// Extract page words once (page.GetText("words")), then substitute vertical/
        /// rotated text: any horizontal word whose center falls inside a rotated span's
        /// bbox is dropped, and each rotated span is appended as a single word.
        /// </summary>
        internal static List<(float x0, float y0, float x1, float y1, string text)> PageWords(Page page)
        {
            if (page == null)
                return new List<(float x0, float y0, float x1, float y1, string text)>();
            return RefineWordsCache.GetValue(page, static p => BuildPageWords(p));
        }

        /// <summary>
        /// Word-to-cell membership by CENTER point (inclusive), not clip-overlap.
        ///
        /// A word straddling a column line is claimed by exactly one cell.
        /// </summary>
        static bool WordInRect(
            float wx0, float wy0, float wx1, float wy1,
            float x0, float y0, float x1, float y1)
        {
            float cx = (wx0 + wx1) * 0.5f;
            float cy = (wy0 + wy1) * 0.5f;
            return x0 <= cx && cx <= x1 && y0 <= cy && cy <= y1;
        }

        /// <summary>
        /// Page words whose center lies in rect, as (x0, y0, x1, y1, text) tuples.
        /// </summary>
        static List<(float x0, float y0, float x1, float y1, string text)> WordsInRect(Page page, Rect rect)
        {
            float x0 = rect.X0, y0 = rect.Y0, x1 = rect.X1, y1 = rect.Y1;
            var result = new List<(float x0, float y0, float x1, float y1, string text)>();
            foreach (var w in PageWords(page))
            {
                if (WordInRect(w.x0, w.y0, w.x1, w.y1, x0, y0, x1, y1))
                    result.Add(w);
            }
            return result;
        }

        /// <summary>Loose value-like predicate: the text contains any digit.</summary>
        static bool HasDigit(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            foreach (char c in text.Trim())
            {
                if (char.IsDigit(c))
                    return true;
            }
            return false;
        }

        /// <summary>Strict value-like predicate: contains a digit and no letters.</summary>
        static bool HasDigitNoAlpha(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            string stripped = text.Trim();
            bool digit = false;
            foreach (char c in stripped)
            {
                if (char.IsDigit(c))
                    digit = true;
                if (char.IsLetter(c))
                    return false;
            }
            return digit;
        }

        // --- flat-placement word helpers (same semantics as Span* text path) ---------

        static List<(int index, (float x0, float y0, float x1, float y1, string text) word)> SelectWordsInRect(
            List<(float x0, float y0, float x1, float y1, string text)> pageWords,
            Rect rect)
        {
            var selected = new List<(int, (float, float, float, float, string))>();
            if (pageWords == null || rect == null)
                return selected;
            for (int index = 0; index < pageWords.Count; index++)
            {
                var word = pageWords[index];
                if (string.IsNullOrWhiteSpace(word.text))
                    continue;
                float cx = (word.x0 + word.x1) * 0.5f;
                float cy = (word.y0 + word.y1) * 0.5f;
                if (rect.X0 <= cx && cx <= rect.X1 && rect.Y0 <= cy && cy <= rect.Y1)
                    selected.Add((index, word));
            }
            return selected;
        }

        static (float y0, float x0, float y1, string text) WordLineTuple(
            (float x0, float y0, float x1, float y1, string text) word)
            => (word.y0, word.x0, word.y1, word.text ?? "");

        static string WordsToLineText(List<(float y0, float x0, float y1, string text)> words)
        {
            if (words == null || words.Count == 0)
                return "";
            var heights = words.Select(w => Math.Max(0.1f, w.y1 - w.y0)).OrderBy(h => h).ToList();
            float medianHeight = heights[heights.Count / 2];
            float lineThreshold = Math.Max(2.0f, medianHeight * 0.55f);
            var lines = new List<(float centerY, List<(float x0, string text)> words)>();
            foreach (var word in words.OrderBy(w => w.y0).ThenBy(w => w.x0))
            {
                float cy = (word.y0 + word.y1) / 2.0f;
                int best = -1;
                float bestDistance = lineThreshold;
                for (int i = 0; i < lines.Count; i++)
                {
                    float distance = Math.Abs(cy - lines[i].centerY);
                    if (distance <= bestDistance)
                    {
                        best = i;
                        bestDistance = distance;
                    }
                }
                if (best < 0)
                {
                    lines.Add((cy, new List<(float, string)> { (word.x0, word.text) }));
                    continue;
                }
                var (centerY, lineWords) = lines[best];
                lineWords.Add((word.x0, word.text));
                int count = lineWords.Count;
                lines[best] = ((centerY * (count - 1) + cy) / count, lineWords);
            }
            var textLines = new List<string>();
            foreach (var line in lines.OrderBy(l => l.centerY))
            {
                textLines.Add(string.Join(" ",
                    line.words.OrderBy(w => w.x0).Select(w => w.text)));
            }
            return string.Join("\n", textLines);
        }

        // --- stage 1: split rows separated by cell background shading ----------------

        static bool IsWhite(object fill, float threshold = 0.95f)
        {
            if (fill == null)
                return true;
            try
            {
                float r, g, b;
                if (fill is float[] fa)
                {
                    if (fa.Length < 3)
                        return true;
                    r = fa[0];
                    g = fa[1];
                    b = fa[2];
                }
                else if (fill is double[] da)
                {
                    if (da.Length < 3)
                        return true;
                    r = (float)da[0];
                    g = (float)da[1];
                    b = (float)da[2];
                }
                else if (fill is IList list)
                {
                    if (list.Count < 3)
                        return true;
                    r = Convert.ToSingle(list[0], CultureInfo.InvariantCulture);
                    g = Convert.ToSingle(list[1], CultureInfo.InvariantCulture);
                    b = Convert.ToSingle(list[2], CultureInfo.InvariantCulture);
                }
                else
                    return true;
                return r > threshold && g > threshold && b > threshold;
            }
            catch
            {
                return true;
            }
        }

        static Rect TableRect(
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            Rect tableBbox)
        {
            if (tableBbox != null)
            {
                var rect = new Rect(tableBbox);
                if (!rect.IsEmpty)
                    return rect;
            }
            var rects = new List<Rect>();
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
                        var r = CellToRect(cell.Value);
                        if (!r.IsEmpty)
                            rects.Add(r);
                    }
                }
            }
            if (rects.Count == 0)
                return null;
            var union = new Rect(rects[0]);
            for (int i = 1; i < rects.Count; i++)
                union.IncludeRect(rects[i]);
            return union;
        }

        static List<Rect> RawShadedRects(Page page, Rect tableRect, float minDim)
        {
            var outRects = new List<Rect>();
            float pageWidth = (float)page.Rect.Width;
            foreach (var drawing in page.GetDrawingsDict() ?? new List<Dictionary<string, object>>())
            {
                if (drawing == null)
                    continue;
                drawing.TryGetValue("fill", out var fill);
                if (IsWhite(fill))
                    continue;
                if (!drawing.TryGetValue("items", out var itemsObj) || itemsObj is not IEnumerable items)
                    continue;
                foreach (var item in items)
                {
                    if (!TryGetDrawingItem(item, out string kind, out object[] parts))
                        continue;
                    if (kind != "re" || parts.Length < 2)
                        continue;
                    var rect = CoerceRect(parts[1]);
                    if (rect == null)
                        continue;
                    rect = rect.Normalize();
                    if (rect.Width >= 0.95f * pageWidth)
                        continue;
                    if (rect.Width < minDim || rect.Height < minDim)
                        continue;
                    if ((rect & tableRect).IsEmpty)
                        continue;
                    outRects.Add(rect);
                }
            }
            return outRects;
        }

        static List<Rect> CellBackgroundRects(List<Rect> rawRects, float containFrac = 0.9f)
        {
            var keep = new List<Rect>();
            for (int index = 0; index < rawRects.Count; index++)
            {
                var rect = rawRects[index];
                float area = rect.GetArea();
                if (area <= 0)
                    continue;
                bool nested = false;
                for (int otherIndex = 0; otherIndex < rawRects.Count; otherIndex++)
                {
                    if (otherIndex == index)
                        continue;
                    var other = rawRects[otherIndex];
                    if ((rect & other).GetArea() >= containFrac * area
                        && other.GetArea() > area * 1.05f)
                    {
                        nested = true;
                        break;
                    }
                }
                if (!nested)
                    keep.Add(rect);
            }
            return keep;
        }

        static List<float> Cluster(IEnumerable<float> values, float tolerance)
        {
            var clusters = new List<float>();
            var current = new List<float>();
            foreach (float value in values.OrderBy(v => v))
            {
                if (current.Count > 0 && value - current[current.Count - 1] > tolerance)
                {
                    clusters.Add(current.Average());
                    current.Clear();
                }
                current.Add(value);
            }
            if (current.Count > 0)
                clusters.Add(current.Average());
            return clusters;
        }

        static (List<float> xs, List<float> ys) BorderLines(Page page, Rect tableRect)
        {
            var xs = new HashSet<float>();
            var ys = new HashSet<float>();
            foreach (var drawing in page.GetDrawingsDict() ?? new List<Dictionary<string, object>>())
            {
                if (drawing == null)
                    continue;
                string typeStr = drawing.TryGetValue("type", out var tObj) ? tObj?.ToString() : null;
                bool stroked = typeStr == "s" || typeStr == "fs";
                if (!drawing.TryGetValue("items", out var itemsObj) || itemsObj is not IEnumerable items)
                    continue;
                foreach (var item in items)
                {
                    if (!TryGetDrawingItem(item, out string kind, out object[] parts))
                        continue;
                    if (kind == "l" && parts.Length >= 3)
                    {
                        if (!TryCoercePoint(parts[1], out var p1) || !TryCoercePoint(parts[2], out var p2))
                            continue;
                        if (Math.Abs(p1.Y - p2.Y) < 1.0f
                            && Math.Abs(p1.X - p2.X) > 10.0f
                            && tableRect.Y0 - 2.0f < (p1.Y + p2.Y) * 0.5f
                            && (p1.Y + p2.Y) * 0.5f < tableRect.Y1 + 2.0f)
                        {
                            ys.Add((float)Math.Round((p1.Y + p2.Y) * 0.5f, 1));
                        }
                        else if (Math.Abs(p1.X - p2.X) < 1.0f
                                 && Math.Abs(p1.Y - p2.Y) > 10.0f
                                 && tableRect.X0 - 2.0f < (p1.X + p2.X) * 0.5f
                                 && (p1.X + p2.X) * 0.5f < tableRect.X1 + 2.0f)
                        {
                            xs.Add((float)Math.Round((p1.X + p2.X) * 0.5f, 1));
                        }
                        continue;
                    }
                    if (kind != "re" || parts.Length < 2)
                        continue;
                    var rect = CoerceRect(parts[1]);
                    if (rect == null)
                        continue;
                    rect = rect.Normalize();
                    if ((rect & tableRect).IsEmpty)
                        continue;
                    if (rect.Height < 2.0f && rect.Width > 10.0f)
                        ys.Add((float)Math.Round((rect.Y0 + rect.Y1) * 0.5f, 1));
                    else if (rect.Width < 2.0f && rect.Height > 10.0f)
                        xs.Add((float)Math.Round((rect.X0 + rect.X1) * 0.5f, 1));
                    else if (stroked)
                    {
                        ys.Add((float)Math.Round(rect.Y0, 1));
                        ys.Add((float)Math.Round(rect.Y1, 1));
                        xs.Add((float)Math.Round(rect.X0, 1));
                        xs.Add((float)Math.Round(rect.X1, 1));
                    }
                }
            }
            return (xs.OrderBy(v => v).ToList(), ys.OrderBy(v => v).ToList());
        }

        static List<float> DropNear(List<float> edges, List<float> borders, float tolerance)
        {
            return edges
                .Where(edge => !borders.Any(border => Math.Abs(edge - border) <= tolerance))
                .ToList();
        }

        static bool HasText(
            List<(float x0, float y0, float x1, float y1, string text)> words,
            float x0, float y0, float x1, float y1,
            float margin)
        {
            foreach (var w in words)
            {
                if (string.IsNullOrWhiteSpace(w.text))
                    continue;
                float cx = (w.x0 + w.x1) * 0.5f;
                float cy = (w.y0 + w.y1) * 0.5f;
                if (x0 + margin < cx && cx < x1 - margin && y0 + margin < cy && cy < y1 - margin)
                    return true;
            }
            return false;
        }

        static List<float> ContentFilterRowEdges(
            List<(float x0, float y0, float x1, float y1, string text)> words,
            float y0, float y1,
            List<float> candidates,
            float x0, float x1,
            float margin)
        {
            var edges = candidates
                .Where(edge => y0 + margin < edge && edge < y1 - margin)
                .OrderBy(e => e)
                .ToList();
            while (edges.Count > 0)
            {
                var bounds = new List<float> { y0 };
                bounds.AddRange(edges);
                bounds.Add(y1);
                int? emptyIndex = null;
                for (int index = 0; index < bounds.Count - 1; index++)
                {
                    if (!HasText(words, x0, bounds[index], x1, bounds[index + 1], margin))
                    {
                        emptyIndex = index;
                        break;
                    }
                }
                if (emptyIndex == null)
                    break;
                int ei = emptyIndex.Value;
                if (ei < edges.Count)
                    edges.RemoveAt(ei);
                else if (ei - 1 >= 0)
                    edges.RemoveAt(ei - 1);
                else
                    break;
            }
            return edges;
        }

        /// <summary>
        /// Split rows the line grid merged but a cell background rectangle separates.
        ///
        /// Cell background-shading rectangles give row boundaries the border lines do
        /// not; candidate y-edges from those rectangles (minus edges that coincide with
        /// real borders, minus edges that would cut an empty band) subdivide each row.
        /// </summary>
        static List<List<(float x0, float y0, float x1, float y1)?>> SplitShadedRows(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            Rect tableBbox = null,
            float clusterTolerance = 3.0f,
            float margin = 2.0f,
            float minDim = 6.0f,
            float borderTolerance = 2.5f)
        {
            var tableRect = TableRect(cells, tableBbox);
            if (tableRect == null || cells == null || cells.Count == 0)
                return cells;

            var rawRects = RawShadedRects(page, tableRect, minDim);
            var backgroundRects = CellBackgroundRects(rawRects);
            if (backgroundRects.Count == 0)
                return cells;

            var yEdges = Cluster(
                backgroundRects.SelectMany(rect => new[] { rect.Y0, rect.Y1 }),
                clusterTolerance);
            var (_, borderYs) = BorderLines(page, tableRect);
            yEdges = DropNear(yEdges, borderYs, borderTolerance);
            if (yEdges.Count == 0)
                return cells;

            var words = PageWords(page);
            var newCells = new List<List<(float x0, float y0, float x1, float y1)?>>();
            int addedRows = 0;
            foreach (var row in cells)
            {
                var live = new List<Rect>();
                if (row != null)
                {
                    foreach (var cell in row)
                    {
                        if (cell != null)
                            live.Add(CellToRect(cell.Value));
                    }
                }
                if (live.Count == 0)
                {
                    newCells.Add(row);
                    continue;
                }
                float ry0 = live.Min(c => c.Y0);
                float ry1 = live.Max(c => c.Y1);
                float rx0 = live.Min(c => c.X0);
                float rx1 = live.Max(c => c.X1);
                var cuts = ContentFilterRowEdges(words, ry0, ry1, yEdges, rx0, rx1, margin);
                if (cuts.Count == 0)
                {
                    newCells.Add(row);
                    continue;
                }
                var bounds = new List<float> { ry0 };
                bounds.AddRange(cuts);
                bounds.Add(ry1);
                for (int bandIndex = 0; bandIndex < bounds.Count - 1; bandIndex++)
                {
                    var band = new List<(float x0, float y0, float x1, float y1)?>();
                    if (row != null)
                    {
                        foreach (var cell in row)
                        {
                            if (cell == null)
                                band.Add(null);
                            else
                            {
                                var rect = CellToRect(cell.Value);
                                band.Add((rect.X0, bounds[bandIndex], rect.X1, bounds[bandIndex + 1]));
                            }
                        }
                    }
                    newCells.Add(band);
                }
                addedRows += bounds.Count - 2;
            }

            if (addedRows <= 0)
                return cells;
            return newCells;
        }

        // --- stage 2: split under-segmented columns ----------------------------------

        static bool ValueLike(string text) => HasDigitNoAlpha(text);

        static bool SpanValueLike(string text) => HasDigit(text);

        /// <summary>
        /// True if a cell holds two value-like groups separated by a wide gap on one
        /// text line -- the signal that a single grid column packs several columns.
        /// </summary>
        static bool CellSpanSignal(Page page, (float x0, float y0, float x1, float y1) cell, float gap, float lineTolerance)
        {
            var rect = CellToRect(cell);
            var words = new List<(float x0, float x1, float cy, string text)>();
            foreach (var w in WordsInRect(page, rect))
            {
                string stripped = (w.text ?? "").Trim();
                if (stripped.Length == 0)
                    continue;
                float cx = (w.x0 + w.x1) / 2.0f;
                float cy = (w.y0 + w.y1) / 2.0f;
                if (rect.X0 <= cx && cx <= rect.X1 && rect.Y0 <= cy && cy <= rect.Y1)
                    words.Add((w.x0, w.x1, cy, stripped));
            }
            if (words.Count < 2)
                return false;

            words.Sort((a, b) => a.cy.CompareTo(b.cy));
            var lines = new List<List<(float x0, float x1, float cy, string text)>>();
            var current = new List<(float x0, float x1, float cy, string text)> { words[0] };
            for (int i = 1; i < words.Count; i++)
            {
                var word = words[i];
                if (word.cy - current[current.Count - 1].cy > lineTolerance)
                {
                    lines.Add(current);
                    current = new List<(float, float, float, string)> { word };
                }
                else
                    current.Add(word);
            }
            lines.Add(current);

            foreach (var line in lines)
            {
                line.Sort((a, b) => a.x0.CompareTo(b.x0));
                for (int index = 0; index < line.Count - 1; index++)
                {
                    if (line[index + 1].x0 - line[index].x1 <= gap)
                        continue;
                    string left = string.Join(" ", line.Take(index + 1).Select(w => w.text));
                    string right = string.Join(" ", line.Skip(index + 1).Select(w => w.text));
                    if (SpanValueLike(left) && SpanValueLike(right))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return the column indices that fire the under-segmentation gate: columns
        /// where enough rows carry the two-value-group signal (a support threshold).
        /// </summary>
        static List<int> DetectSpanColumns(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            float gap = 12.0f,
            float lineTolerance = 4.0f,
            float supportRatio = 0.3f,
            int minSupport = 2)
        {
            int rows = cells?.Count ?? 0;
            int supportThreshold = Math.Max(minSupport, (int)(supportRatio * rows));
            var colHits = new Dictionary<int, int>();
            if (cells == null)
                return new List<int>();
            foreach (var row in cells)
            {
                if (row == null)
                    continue;
                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    var cell = row[colIndex];
                    if (cell == null)
                        continue;
                    if (CellSpanSignal(page, cell.Value, gap, lineTolerance))
                    {
                        colHits.TryGetValue(colIndex, out int count);
                        colHits[colIndex] = count + 1;
                    }
                }
            }
            return colHits
                .Where(kv => kv.Value >= supportThreshold)
                .Select(kv => kv.Key)
                .OrderBy(c => c)
                .ToList();
        }

        static List<List<(float x0, float x1, string text)>> ColumnWords(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            int col)
        {
            var rows = new List<List<(float x0, float x1, string text)>>();
            if (cells == null)
                return rows;
            foreach (var row in cells)
            {
                var words = new List<(float x0, float x1, string text)>();
                if (row != null && col < row.Count && row[col] is { } cell)
                {
                    var rect = CellToRect(cell);
                    foreach (var w in WordsInRect(page, rect))
                    {
                        if (string.IsNullOrWhiteSpace(w.text))
                            continue;
                        float cx = (w.x0 + w.x1) / 2.0f;
                        float cy = (w.y0 + w.y1) / 2.0f;
                        if (rect.X0 <= cx && cx <= rect.X1 && rect.Y0 <= cy && cy <= rect.Y1)
                            words.Add((w.x0, w.x1, w.text));
                    }
                }
                words.Sort((a, b) => a.x0.CompareTo(b.x0));
                rows.Add(words);
            }
            return rows;
        }

        static List<float> CutXs(
            List<List<(float x0, float x1, string text)>> rowsWords,
            int bridgeTolerance = 0)
        {
            var intervals = new List<(float x0, float x1, int rowIndex)>();
            for (int rowIndex = 0; rowIndex < rowsWords.Count; rowIndex++)
            {
                foreach (var (x0, x1, _) in rowsWords[rowIndex])
                    intervals.Add((x0, x1, rowIndex));
            }
            if (intervals.Count < 2)
                return new List<float>();

            float lo = intervals.Min(i => i.x0);
            float hi = intervals.Max(i => i.x1);
            if (hi - lo < 1)
                return new List<float>();

            var channels = new List<(float left, float right)>();
            float[] current = null;
            for (float x = lo; x <= hi; x += 1.0f)
            {
                var crossingRows = new HashSet<int>();
                foreach (var (x0, x1, rowIndex) in intervals)
                {
                    if (x0 < x && x < x1)
                        crossingRows.Add(rowIndex);
                }
                if (crossingRows.Count > bridgeTolerance)
                {
                    if (current != null)
                    {
                        channels.Add((current[0], current[1]));
                        current = null;
                    }
                }
                else
                {
                    if (current == null)
                        current = new[] { x, x };
                    else
                        current[1] = x;
                }
            }
            if (current != null)
                channels.Add((current[0], current[1]));

            var cuts = new List<float>();
            foreach (var (leftEdge, rightEdge) in channels)
            {
                if (leftEdge <= lo + 1 || rightEdge >= hi - 1)
                    continue;
                float cutX = (leftEdge + rightEdge) / 2.0f;
                int crossingCount = rowsWords.Count(words =>
                    words.Any(w => w.x0 < cutX && cutX < w.x1));
                // Evaluate the symmetry/value guards on non-bridging rows only, so a
                // merged label spanning value subcolumns does not block column recovery.
                var bodyRows = rowsWords
                    .Where(words => !words.Any(w => w.x0 < cutX && cutX < w.x1))
                    .ToList();
                int leftRows = 0, rightRows = 0;
                int leftOk = 0, rightOk = 0;
                int leftN = 0, rightN = 0;
                var leftValues = new List<string>();
                var rightValues = new List<string>();
                foreach (var words in bodyRows)
                {
                    var left = words.Where(w => w.x1 <= cutX).Select(w => w.text).ToList();
                    var right = words.Where(w => w.x0 >= cutX).Select(w => w.text).ToList();
                    if (left.Count > 0)
                    {
                        leftRows++;
                        leftN++;
                        leftOk += ValueLike(string.Join(" ", left)) ? 1 : 0;
                        leftValues.AddRange(left);
                    }
                    if (right.Count > 0)
                    {
                        rightRows++;
                        rightN++;
                        rightOk += ValueLike(string.Join(" ", right)) ? 1 : 0;
                        rightValues.AddRange(right);
                    }
                }

                if (leftRows < 2 || rightRows < 2)
                    continue;
                if (crossingCount > 0)
                {
                    if (!ValueLike(string.Join(" ", leftValues)) || !ValueLike(string.Join(" ", rightValues)))
                        continue;
                }
                else if ((leftN > 0 && leftOk / (float)leftN < 0.5f)
                         || (rightN > 0 && rightOk / (float)rightN < 0.5f))
                {
                    continue;
                }
                cuts.Add(cutX);
            }
            cuts.Sort();
            return cuts;
        }

        static List<List<(float x0, float y0, float x1, float y1)?>> RefineSplitColumns(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            int bridgeTolerance = 0,
            HashSet<int> allowedCols = null)
        {
            int ncols = cells?.Count > 0 ? cells.Max(row => row?.Count ?? 0) : 0;
            var colCuts = new Dictionary<int, List<float>>();
            for (int col = 0; col < ncols; col++)
            {
                if (allowedCols != null && !allowedCols.Contains(col))
                    continue;
                var cuts = CutXs(ColumnWords(page, cells, col), bridgeTolerance: bridgeTolerance);
                if (cuts.Count > 0)
                    colCuts[col] = cuts;
            }

            if (colCuts.Count == 0)
                return cells;

            var newCells = new List<List<(float x0, float y0, float x1, float y1)?>>();
            foreach (var row in cells)
            {
                var newRow = new List<(float x0, float y0, float x1, float y1)?>();
                if (row == null)
                {
                    newCells.Add(newRow);
                    continue;
                }
                for (int col = 0; col < row.Count; col++)
                {
                    var cell = row[col];
                    if (!colCuts.TryGetValue(col, out var cuts))
                    {
                        newRow.Add(cell);
                    }
                    else if (cell == null)
                    {
                        for (int i = 0; i < cuts.Count + 1; i++)
                            newRow.Add(null);
                    }
                    else
                    {
                        var rect = CellToRect(cell.Value);
                        var xs = new List<float> { rect.X0 };
                        xs.AddRange(cuts.Where(cutX => rect.X0 < cutX && cutX < rect.X1));
                        xs.Add(rect.X1);
                        for (int index = 0; index < xs.Count - 1; index++)
                            newRow.Add((xs[index], rect.Y0, xs[index + 1], rect.Y1));
                    }
                }
                newCells.Add(newRow);
            }
            return newCells;
        }

        /// <summary>
        /// Split under-segmented columns, but only those that fire the value-group
        /// gate -- so ordinary single-value columns are never disturbed.
        /// </summary>
        static List<List<(float x0, float y0, float x1, float y1)?>> SplitUndersegmentedColumns(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            float gap = 12.0f,
            float lineTolerance = 4.0f,
            float supportRatio = 0.3f,
            int minSupport = 2,
            int bridgeTolerance = 0)
        {
            var firedCols = DetectSpanColumns(
                page, cells,
                gap: gap,
                lineTolerance: lineTolerance,
                supportRatio: supportRatio,
                minSupport: minSupport);
            if (firedCols.Count == 0)
                return cells;
            return RefineSplitColumns(
                page, cells,
                bridgeTolerance: bridgeTolerance,
                allowedCols: new HashSet<int>(firedCols));
        }

        // --- stage 3: split over-merged body rows ------------------------------------

        static List<Rect> RectsFromCells(List<List<(float x0, float y0, float x1, float y1)?>> cells)
        {
            var rects = new List<Rect>();
            if (cells == null)
                return rects;
            foreach (var row in cells)
            {
                if (row == null)
                    continue;
                foreach (var cell in row)
                {
                    if (cell != null)
                        rects.Add(CellToRect(cell.Value));
                }
            }
            return rects;
        }

        static Rect UnionRect(List<Rect> rects)
        {
            if (rects == null || rects.Count == 0)
                return null;
            var rect = new Rect(rects[0]);
            for (int i = 1; i < rects.Count; i++)
                rect.IncludeRect(rects[i]);
            return rect;
        }

        static List<(float x0, float x1)> BestColumnBounds(
            List<List<(float x0, float y0, float x1, float y1)?>> cells)
        {
            List<(float x0, float y0, float x1, float y1)?> bestRow = null;
            int bestLive = -1;
            int bestLen = -1;
            if (cells != null)
            {
                foreach (var row in cells)
                {
                    if (row == null)
                        continue;
                    int live = row.Count(c => c != null);
                    int len = row.Count;
                    if (live > bestLive || (live == bestLive && len > bestLen))
                    {
                        bestLive = live;
                        bestLen = len;
                        bestRow = row;
                    }
                }
            }
            if (bestRow == null)
                return new List<(float x0, float x1)>();
            return bestRow
                .Where(c => c != null)
                .Select(c => CellToRect(c.Value))
                .Where(r => r.X1 > r.X0)
                .Select(r => (r.X0, r.X1))
                .OrderBy(b => b.Item1)
                .ToList();
        }

        /// <summary>
        /// Group center-point words into body "lines" by a fixed center-y gap,
        /// returning each line's union bbox and joined text.
        /// </summary>
        static List<ClusterLine> ClusterWordsByY(
            List<(float x0, float y0, float x1, float y1, string text)> words)
        {
            if (words == null || words.Count == 0)
                return new List<ClusterLine>();
            var sorted = words
                .OrderBy(w => (w.y0 + w.y1) / 2.0f)
                .ThenBy(w => w.x0)
                .ToList();
            var clusters = new List<List<(float x0, float y0, float x1, float y1, string text)>>
            {
                new List<(float, float, float, float, string)> { sorted[0] }
            };
            float lastCenter = (sorted[0].y0 + sorted[0].y1) / 2.0f;
            for (int i = 1; i < sorted.Count; i++)
            {
                var word = sorted[i];
                float center = (word.y0 + word.y1) / 2.0f;
                if (center - lastCenter > RefineLineGap)
                    clusters.Add(new List<(float, float, float, float, string)> { word });
                else
                    clusters[clusters.Count - 1].Add(word);
                lastCenter = center;
            }

            var lines = new List<ClusterLine>();
            foreach (var cluster in clusters)
            {
                lines.Add(new ClusterLine
                {
                    X0 = cluster.Min(w => w.x0),
                    Y0 = cluster.Min(w => w.y0),
                    X1 = cluster.Max(w => w.x1),
                    Y1 = cluster.Max(w => w.y1),
                    Text = string.Join(" ",
                        cluster.OrderBy(w => w.x0).Select(w => w.text)),
                });
            }
            return lines;
        }

        static HashSet<int> LineColumns(ClusterLine line, List<(float x0, float x1)> colBounds)
        {
            var cols = new HashSet<int>();
            float lineX0 = line.X0;
            float lineX1 = line.X1;
            float center = (lineX0 + lineX1) / 2.0f;
            for (int index = 0; index < colBounds.Count; index++)
            {
                var (x0, x1) = colBounds[index];
                float overlap = Math.Min(lineX1, x1) - Math.Max(lineX0, x0);
                if (overlap > 0 || (x0 <= center && center <= x1))
                    cols.Add(index);
            }
            return cols;
        }

        static List<(float x0, float y0, float x1, float y1, string text)> PageWordsInRect(Page page, Rect rect)
        {
            var words = new List<(float x0, float y0, float x1, float y1, string text)>();
            float rx0 = rect.X0, ry0 = rect.Y0, rx1 = rect.X1, ry1 = rect.Y1;
            foreach (var w in PageWords(page))
            {
                float cx = (w.x0 + w.x1) * 0.5f;
                float cy = (w.y0 + w.y1) * 0.5f;
                if (rx0 <= cx && cx <= rx1 && ry0 <= cy && cy <= ry1)
                {
                    string text = (w.text ?? "").Trim();
                    if (text.Length > 0)
                        words.Add((w.x0, w.y0, w.x1, w.y1, text));
                }
            }
            return words;
        }

        static List<(float y0, float y1)> MergeOverlappingLines(
            List<(float y0, float y1)> lines,
            float frac)
        {
            if (lines == null || lines.Count == 0)
                return new List<(float y0, float y1)>();
            var items = lines.OrderBy(l => l.y0).ThenBy(l => l.y1).ToList();
            var merged = new List<float[]> { new[] { items[0].y0, items[0].y1 } };
            for (int i = 1; i < items.Count; i++)
            {
                float y0 = items[i].y0;
                float y1 = items[i].y1;
                var current = merged[merged.Count - 1];
                float overlap = current[1] - y0;
                float minHeight = Math.Min(current[1] - current[0], y1 - y0);
                if (minHeight > 0 && overlap / minHeight >= frac)
                {
                    current[0] = Math.Min(current[0], y0);
                    current[1] = Math.Max(current[1], y1);
                }
                else
                    merged.Add(new[] { y0, y1 });
            }
            return merged.Select(m => (m[0], m[1])).ToList();
        }

        /// <summary>
        /// Decide whether the body rows are over-merged. Returns the geometry needed
        /// to re-cut them (body bbox, column bounds, per-record y-bounds) or null.
        ///
        /// Fires only when the body text clusters into clean multi-column "record" lines
        /// (clean_ratio &gt;= cleanThreshold) and there are more records than existing body
        /// rows -- i.e. several records collapsed into one grid row.
        /// </summary>
        static OvermergeMeta DetectRowOvermerge(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            float cleanThreshold = 0.85f,
            int headerRowCount = 1)
        {
            var colBounds = BestColumnBounds(cells);
            int cellCount = cells?.Count ?? 0;
            headerRowCount = cellCount > 0
                ? Math.Max(1, Math.Min(headerRowCount, cellCount))
                : 0;
            int existingBodyRows = Math.Max(0, cellCount - headerRowCount);
            if (cellCount <= headerRowCount)
                return null;
            if (colBounds.Count < 2)
                return null;

            var bodyRect = UnionRect(RectsFromCells(cells.Skip(headerRowCount).ToList()));
            if (bodyRect == null || bodyRect.IsEmpty)
                return null;

            var words = PageWordsInRect(page, bodyRect);
            var lines = ClusterWordsByY(words);
            if (lines.Count == 0)
                return null;

            int minCols = Math.Max(2, (colBounds.Count + 1) / 2);  // == max(2, ceil(len/2))
            var recordLines = new List<ClusterLine>();
            foreach (var line in lines)
            {
                var cols = LineColumns(line, colBounds).OrderBy(c => c).ToList();
                if (cols.Count >= minCols)
                    recordLines.Add(line);
            }

            float cleanRatio = recordLines.Count / (float)lines.Count;
            if (cleanRatio < cleanThreshold)
                return null;
            if (recordLines.Count <= existingBodyRows)
                return null;

            return new OvermergeMeta
            {
                BodyBbox = new[] { bodyRect.X0, bodyRect.Y0, bodyRect.X1, bodyRect.Y1 },
                ColBounds = colBounds
                    .Select(b => ((float)Math.Round(b.x0, 2), (float)Math.Round(b.x1, 2)))
                    .ToList(),
                RecordLineBounds = recordLines
                    .Select(line => (line.Y0, line.Y1))
                    .ToList(),
                ExistingBodyRows = existingBodyRows,
                HeaderRowCount = headerRowCount,
            };
        }

        /// <summary>
        /// Re-cut over-merged body rows into one grid row per detected record.
        ///
        /// Keeps the header rows intact, then rebuilds the body as evenly-bounded rows
        /// (cut at the midpoints between consecutive record centers) across the detected
        /// column bounds. <c>headerRowCount</c> is the number of leading header rows to keep.
        /// </summary>
        static List<List<(float x0, float y0, float x1, float y1)?>> SplitOvermergedRows(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            float cleanThreshold = 0.85f,
            float mergeOverlapFrac = 0.35f,
            int headerRowCount = 1)
        {
            var meta = DetectRowOvermerge(
                page, cells,
                cleanThreshold: cleanThreshold,
                headerRowCount: headerRowCount);
            if (meta == null)
                return cells;

            var bodyBbox = new Rect(meta.BodyBbox[0], meta.BodyBbox[1], meta.BodyBbox[2], meta.BodyBbox[3]);
            var colBounds = meta.ColBounds;
            var lineBounds = MergeOverlappingLines(meta.RecordLineBounds, mergeOverlapFrac);
            if (lineBounds.Count <= meta.ExistingBodyRows)
                return cells;

            var centers = lineBounds.Select(b => (b.y0 + b.y1) / 2.0f).ToList();
            var edges = new List<float> { bodyBbox.Y0 };
            for (int i = 0; i < centers.Count - 1; i++)
                edges.Add((centers[i] + centers[i + 1]) / 2.0f);
            edges.Add(bodyBbox.Y1);

            var headerRows = cells.Take(meta.HeaderRowCount).ToList();
            var newBody = new List<List<(float x0, float y0, float x1, float y1)?>>();
            for (int rowIndex = 0; rowIndex < lineBounds.Count; rowIndex++)
            {
                float y0 = edges[rowIndex];
                float y1 = edges[rowIndex + 1];
                var newRow = new List<(float x0, float y0, float x1, float y1)?>();
                foreach (var (x0, x1) in colBounds)
                {
                    var rect = new Rect(x0, y0, x1, y1);
                    newRow.Add((rect.X0, rect.Y0, rect.X1, rect.Y1));
                }
                newBody.Add(newRow);
            }

            var result = new List<List<(float x0, float y0, float x1, float y1)?>>(headerRows);
            result.AddRange(newBody);
            return result;
        }

        /// <summary>
        /// Structural half of RefineGrid: split shaded rows, then under-segmented
        /// columns. Exposed separately so a caller that must know the header/body
        /// boundary of the post-structure grid can insert that computation between the
        /// two phases. <c>tableBbox</c>, when given, bounds the shaded-rectangle search;
        /// otherwise the cells' union is used.
        /// </summary>
        internal static List<List<(float x0, float y0, float x1, float y1)?>> RefineGridStructure(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            Rect tableBbox = null)
        {
            cells = SplitShadedRows(page, cells, tableBbox);
            cells = SplitUndersegmentedColumns(page, cells);
            return cells;
        }

        /// <summary>
        /// Row half of RefineGrid: split body rows that collapsed several records
        /// into one grid row. <c>headerRowCount</c> is the number of leading header rows to
        /// keep intact; callers that resolve a header region pass it in, and the default
        /// of 1 is a conservative single-header assumption.
        /// </summary>
        internal static List<List<(float x0, float y0, float x1, float y1)?>> RefineGridRows(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            int headerRowCount = 1,
            float cleanThreshold = 0.85f,
            float mergeOverlapFrac = 0.35f)
        {
            return SplitOvermergedRows(
                page,
                cells,
                cleanThreshold: cleanThreshold,
                mergeOverlapFrac: mergeOverlapFrac,
                headerRowCount: headerRowCount);
        }

        /// <summary>
        /// Refine a detected table's cell grid and return the refined grid.
        ///
        /// <c>cells</c> is a row-major grid: a list of rows, each a list of
        /// <c>(x0, y0, x1, y1)</c> cell rectangles (null for a gap). The grid is refined
        /// in three stages -- split rows that cell background shading separates, split
        /// columns that jam several values into one cell, and split body rows that merged
        /// several records -- using the page's words and vector graphics. The result is
        /// a new grid in the same format (rows may be added and rows may widen).
        ///
        /// <c>tableBbox</c> optionally bounds the table (else the cells' union is used).
        /// <c>headerRowCount</c> is the number of leading header rows to preserve when
        /// re-cutting body rows (default 1). This is the all-in-one convenience wrapper;
        /// a caller needing the header boundary of the intermediate grid can instead
        /// call <see cref="RefineGridStructure"/> then <see cref="RefineGridRows"/>.
        /// </summary>
        internal static List<List<(float x0, float y0, float x1, float y1)?>> RefineGrid(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            Rect tableBbox = null,
            int headerRowCount = 1)
        {
            cells = RefineGridStructure(page, cells, tableBbox: tableBbox);
            cells = RefineGridRows(page, cells, headerRowCount: headerRowCount);
            return cells;
        }

        // --- public seam -------------------------------------------------------------

        /// <summary>
        /// Convert a Table's flat cell list into the row-major grid RefineGrid wants.
        ///
        /// Mirrors Table.Rows: columns are the sorted distinct left edges, rows are the
        /// cells grouped by top edge, each padded with null where a column has no cell.
        /// </summary>
        internal static List<List<(float x0, float y0, float x1, float y1)?>> CellsToGrid(
            List<(float x0, float y0, float x1, float y1)> cells)
        {
            if (cells == null || cells.Count == 0)
                return new List<List<(float x0, float y0, float x1, float y1)?>>();

            var xs = cells.Select(c => c.x0).Distinct().OrderBy(x => x).ToList();
            var colIndex = new Dictionary<float, int>();
            for (int i = 0; i < xs.Count; i++)
                colIndex[xs[i]] = i;

            var ordered = cells.OrderBy(c => c.y0).ThenBy(c => c.x0).ToList();
            var grid = new List<List<(float x0, float y0, float x1, float y1)?>>();
            int idx = 0;
            while (idx < ordered.Count)
            {
                float y = ordered[idx].y0;
                var row = new List<(float x0, float y0, float x1, float y1)?>(
                    Enumerable.Repeat<(float x0, float y0, float x1, float y1)?>(null, xs.Count));
                while (idx < ordered.Count && ordered[idx].y0 == y)
                {
                    var cell = ordered[idx];
                    row[colIndex[cell.x0]] = (cell.x0, cell.y0, cell.x1, cell.y1);
                    idx++;
                }
                grid.Add(row);
            }
            return grid;
        }

        /// <summary>
        /// Flatten a refined grid back to a Table's flat (x0, y0, x1, y1) cell list.
        /// </summary>
        internal static List<(float x0, float y0, float x1, float y1)> GridToCells(
            List<List<(float x0, float y0, float x1, float y1)?>> grid)
        {
            var outCells = new List<(float x0, float y0, float x1, float y1)>();
            if (grid == null)
                return outCells;
            foreach (var row in grid)
            {
                if (row == null)
                    continue;
                foreach (var cell in row)
                {
                    if (cell is { } c)
                        outCells.Add((c.x0, c.y0, c.x1, c.y1));
                }
            }
            return outCells;
        }

        // ---------------------------------------------------------------------------
        // find_tables(refine=True) reconstruction glue.
        //
        // Resolve a merged-cell placement grid (falling back to a flat 1x1 grid when
        // span resolution changes the column count), determine the header/body split,
        // and tag each placement td/th. Built on ResolveSpans, FindHeaderRegion and
        // the Refine* stages; only reached when refine=true.
        // ---------------------------------------------------------------------------

        /// <summary>Column extent of a placement grid after resolving colspan/rowspan.</summary>
        static int PlacementGridWidth(List<List<SpanCell>> placements)
        {
            var occupied = new HashSet<(int row, int col)>();
            int maxCol = 0;
            if (placements == null)
                return 0;
            for (int rowIdx = 0; rowIdx < placements.Count; rowIdx++)
            {
                var row = placements[rowIdx];
                if (row == null)
                    continue;
                int colIdx = 0;
                foreach (var placement in row)
                {
                    if (placement == null)
                        continue;
                    while (occupied.Contains((rowIdx, colIdx)))
                        colIdx++;
                    int rs = Math.Max(1, placement.Rowspan);
                    int cs = Math.Max(1, placement.Colspan);
                    for (int dr = 0; dr < rs; dr++)
                    {
                        for (int dc = 0; dc < cs; dc++)
                            occupied.Add((rowIdx + dr, colIdx + dc));
                    }
                    colIdx += cs;
                    if (colIdx > maxCol)
                        maxCol = colIdx;
                }
            }
            return maxCol;
        }

        /// <summary>
        /// Flat fallback grid: one 1x1 SpanCell per slot, padded to <c>colCount</c>.
        ///
        /// Selects words per cell by center-point and synthesizes each cell's line text
        /// ("" for a gap), using the same word source and line builder as ResolveSpans.
        /// Used when span resolution changes the column count.
        /// </summary>
        static List<List<SpanCell>> FlatPlacementGrid(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            int colCount)
        {
            var pageWords = PageWords(page);
            var grid = new List<List<SpanCell>>();
            foreach (var row in cells ?? Enumerable.Empty<List<(float x0, float y0, float x1, float y1)?>>())
            {
                var outRow = new List<SpanCell>();
                if (row != null)
                {
                    foreach (var cell in row)
                    {
                        if (cell == null)
                        {
                            outRow.Add(new SpanCell(bbox: null, text: "", colspan: 1, rowspan: 1));
                        }
                        else
                        {
                            var rect = CellToRect(cell.Value);
                            var lineWords = SelectWordsInRect(pageWords, rect)
                                .Select(pair => WordLineTuple(pair.word))
                                .ToList();
                            outRow.Add(new SpanCell(
                                bbox: (rect.X0, rect.Y0, rect.X1, rect.Y1),
                                text: WordsToLineText(lineWords),
                                colspan: 1,
                                rowspan: 1));
                        }
                    }
                }
                while (outRow.Count < colCount)
                    outRow.Add(new SpanCell(bbox: null, text: "", colspan: 1, rowspan: 1));
                grid.Add(outRow);
            }
            return grid;
        }

        /// <summary>
        /// The reconstructed cell grid: resolved SpanCell placements, or the flat 1x1
        /// fallback when span resolution changes the column count (grid width != col
        /// count). <c>strictColspan</c>/<c>headerRowCount</c> pass straight to ResolveSpans.
        /// </summary>
        static List<List<SpanCell>> PlacementOrFlatGrid(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells,
            bool strictColspan = false,
            int? headerRowCount = null)
        {
            int colCount = cells?.Count > 0
                ? cells.Max(row => row?.Count ?? 0)
                : 0;
            var placements = TableSpans.ResolveSpans(
                page, cells, headerRowCount: headerRowCount, strictColspan: strictColspan);
            if (PlacementGridWidth(placements) == colCount)
                return placements;
            return FlatPlacementGrid(page, cells, colCount);
        }

        /// <summary>
        /// Row-major whitespace-collapsed cell text -- the <c>[[text]]</c> header rules read.
        /// </summary>
        static List<List<string>> PlacementsTextGrid(List<List<SpanCell>> grid)
        {
            var outGrid = new List<List<string>>();
            if (grid == null)
                return outGrid;
            foreach (var row in grid)
            {
                var texts = new List<string>();
                if (row != null)
                {
                    foreach (var cell in row)
                        texts.Add(TableHeaders.CollapseCellWs(cell?.Text));
                }
                outGrid.Add(texts);
            }
            return outGrid;
        }

        /// <summary>
        /// Set each placement's HTML tag in place: cells in the top header rows
        /// become <c>th</c>; every other cell becomes <c>td</c>.
        /// </summary>
        static List<List<SpanCell>> TagGrid(List<List<SpanCell>> grid, int topHeaderRows)
        {
            if (grid == null)
                return grid;
            for (int rowIdx = 0; rowIdx < grid.Count; rowIdx++)
            {
                var row = grid[rowIdx];
                if (row == null)
                    continue;
                string tag = rowIdx < topHeaderRows ? "th" : "td";
                foreach (var cell in row)
                {
                    if (cell != null)
                        cell.Tag = tag;
                }
            }
            return grid;
        }

        /// <summary>
        /// Header/body boundary: resolve the merge-preserved placement grid once and
        /// ask the header finder how many leading rows are header, clamped to [1, rows].
        /// Port of table.py <c>_refine_body_start_row</c>.
        /// </summary>
        internal static int BodyStartRow(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> cells)
        {
            try
            {
                var modelGrid = PlacementOrFlatGrid(page, cells);
                var region = TableHeaders.FindHeaderRegion(PlacementsTextGrid(modelGrid));
                int raw = region.TopHeaderRows;
                return cells != null && cells.Count > 0
                    ? Math.Max(1, Math.Min(raw, cells.Count))
                    : 0;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        /// Resolve the final placement grid (strict colspan, header boundary known),
        /// run header rules on its own text grid, tag cells -&gt; (tagged grid, region).
        /// Port of table.py <c>_refine_build_placements</c>.
        /// </summary>
        internal static (List<List<SpanCell>> placements, HeaderRegion region) BuildPlacements(
            Page page,
            List<List<(float x0, float y0, float x1, float y1)?>> working,
            int bodyStart)
        {
            var grid = PlacementOrFlatGrid(
                page, working, strictColspan: true, headerRowCount: bodyStart);
            var region = TableHeaders.FindHeaderRegion(PlacementsTextGrid(grid));
            var tagged = TagGrid(grid, region.TopHeaderRows);
            return (tagged, region);
        }

        // --- geometry / drawings helpers ---------------------------------------------

        static Rect CellToRect((float x0, float y0, float x1, float y1) cell)
            => new Rect(cell.x0, cell.y0, cell.x1, cell.y1);

        static bool TryGetDrawingItem(object item, out string kind, out object[] parts)
        {
            kind = null;
            parts = null;
            if (item is object[] oa && oa.Length > 0 && oa[0] is string s)
            {
                kind = s;
                parts = oa;
                return true;
            }
            if (item is IList list && list.Count > 0 && list[0] is string s2)
            {
                kind = s2;
                parts = new object[list.Count];
                for (int i = 0; i < list.Count; i++)
                    parts[i] = list[i];
                return true;
            }
            return false;
        }

        static Rect CoerceRect(object value)
        {
            if (value == null)
                return null;
            if (value is Rect r)
                return new Rect(r);
            if (value is mupdf.FzRect fr)
                return new Rect(fr);
            if (Helpers.TryCoerceRect(value, out var coerced))
                return coerced;
            if (value is IList list && list.Count >= 4)
            {
                try
                {
                    return new Rect(
                        Convert.ToSingle(list[0], CultureInfo.InvariantCulture),
                        Convert.ToSingle(list[1], CultureInfo.InvariantCulture),
                        Convert.ToSingle(list[2], CultureInfo.InvariantCulture),
                        Convert.ToSingle(list[3], CultureInfo.InvariantCulture));
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        static bool TryCoercePoint(object value, out Point point)
        {
            point = null;
            if (value == null)
                return false;
            if (value is Point p)
            {
                point = p;
                return true;
            }
            if (value is mupdf.FzPoint fp)
            {
                point = Helpers.PointFromFz(fp);
                return true;
            }
            if (Helpers.TryCoercePoint(value, out var coerced))
            {
                point = coerced;
                return true;
            }
            return false;
        }
}
}
