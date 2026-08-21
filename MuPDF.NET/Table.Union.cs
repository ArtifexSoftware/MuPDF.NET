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
// Port of PyMuPDF 1.28.2 src/_table_union.py
//
// MuPDF.NET table union stage, behind FindTables(union: true): fuse the layout
// analyzer's table grids with the line-based finder's candidates. Table,
// TableFinder and Iou come from Table.cs; FindTables is called with useLayout:false.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MuPDF.NET
{
    // ---------------------------------------------------------------------------
    // FindTables(union: true) fuses two table sources on one page: PRIMARY grids
    // from the layout analyzer (each "table" group's GridPrediction, read from
    // page.GetLayout() / LayoutInformation in its raw form by LayoutTableGrids)
    // and CANDIDATE grids from a nested line-based FindTables. A candidate matching a
    // primary 1:1 (high IoU) may REPLACE its grid (grid-ref), several candidates
    // each contained in one primary may SPLIT it, and candidates owned by no primary
    // are APPENDED. The output order -- primaries (kept / grid-ref'd / split) then
    // appended candidates -- is contractual: downstream consumers key tables by it.
    //
    // LayoutTableGrids also replaces FindTables' MakeTableFromBbox path,
    // which silently yields an empty Table because the stext grid block it needs is
    // not emitted; MakeTableFromBbox is left in place pending a separate cleanup.
    // ---------------------------------------------------------------------------
    internal static class TableUnion
    {
        const string UnionStrategy = "lines_strict";          // candidate detection strategy
        const float UnionGridRefIou = 0.9f;                   // min IoU for a 1:1 grid-ref replacement
        const bool UnionGridRefSpanMultGate = true;           // reject under-segmented candidate grids
        const float UnionGridRefSpanMultThreshold = 3.0f;     // max horizontally-separated span groups per cell
        const float UnionOwnerContainment = 0.85f;            // min containment for a split candidate's owner
        const float UnionOwnerAmbiguousOverlap = 0.25f;       // overlap above which an unowned candidate is suppressed

        static readonly ConditionalWeakTable<Page, List<Rect>> TextSpansCache =
            new ConditionalWeakTable<Page, List<Rect>>();

        /// <summary>
        /// Primary table grids from the raw layout analyzer result.
        ///
        /// Reads page.LayoutInformation in its raw form and yields a
        /// (bbox, grid) pair per "table" group -- grid is the full row-major cell
        /// grid built from the group box plus its interior GridPrediction lines, in
        /// layout (reading) order. Boxes without a usable grid are skipped.
        /// </summary>
        internal static List<(Rect bbox, List<List<(float x0, float y0, float x1, float y1)?>> grid)> LayoutTableGrids(Page page)
        {
            var grids = new List<(Rect, List<List<(float x0, float y0, float x1, float y1)?>>)>();
            var info = page.LayoutInformation;
            if (info is not System.Collections.IEnumerable rows)
                return grids;

            foreach (object group in rows)
            {
                if (group == null)
                    continue;
                // The union path needs the raw (return_raw=True) layout form; the
                // normalized [x0, y0, x1, y1, class] tuples carry no table_grid.
                bool isDictLike = group is System.Collections.IDictionary
                    || group.GetType().GetProperty("Item", new[] { typeof(string) }) != null;
                if (!isDictLike)
                    continue;

                string className = GetLayoutValue(group, "class_name")?.ToString();
                if (!string.Equals(className, "table", StringComparison.Ordinal))
                    continue;

                object groupBbox = GetLayoutValue(group, "group_bbox");
                object gridPred = GetLayoutValue(group, "table_grid");
                if (groupBbox == null || gridPred == null)
                    continue;
                if (!TryParseBbox(groupBbox, out float x0, out float y0, out float x1, out float y1))
                    continue;

                var hRel = ToFloatList(GetLayoutValue(gridPred, "h_lines"));
                var vRel = ToFloatList(GetLayoutValue(gridPred, "v_lines"));
                var hLines = new List<float> { y0 };
                hLines.AddRange(hRel.Select(h => h + y0));
                hLines.Add(y1);
                var vLines = new List<float> { x0 };
                vLines.AddRange(vRel.Select(v => v + x0));
                vLines.Add(x1);

                var grid = new List<List<(float x0, float y0, float x1, float y1)?>>();
                for (int i = 0; i < hLines.Count - 1; i++)
                {
                    var row = new List<(float x0, float y0, float x1, float y1)?>();
                    for (int j = 0; j < vLines.Count - 1; j++)
                        row.Add((vLines[j], hLines[i], vLines[j + 1], hLines[i + 1]));
                    grid.Add(row);
                }
                grids.Add((new Rect(x0, y0, x1, y1), grid));
            }
            return grids;
        }

        /// <summary>
        /// Line-based table candidates for the union stage as (bbox, grid) pairs.
        ///
        /// Runs a nested FindTables (strategy=UnionStrategy, useLayout=false) and
        /// keeps each detected table's bbox and row-major cell grid (Table.Rows, null
        /// for a gap), deduped by rounded bbox. Returns (candidates, finder); the
        /// finder is reused as the returned TableFinder shell.
        /// </summary>
        internal static (List<(Rect bbox, List<List<(float x0, float y0, float x1, float y1)?>> grid)> candidates, TableFinder finder)
            LineCandidates(Page page)
        {
            // Called here (not via a module-level import cycle) to break the cycle:
            // this module is itself used lazily by TableHelpers.FindTables (union path).
            var finder = TableHelpers.FindTables(page, strategy: UnionStrategy, useLayout: false);
            var candidates = new List<(Rect, List<List<(float x0, float y0, float x1, float y1)?>>)>();
            var seen = new HashSet<(int, int, int, int)>();
            foreach (var tab in finder?.Tables ?? new List<Table>())
            {
                try
                {
                    var b = tab.bbox;
                    if (b == null || b.IsEmpty)
                        continue;
                    var grid = tab.Rows
                        .Select(row => row.Cells
                            .Select(c => c.HasValue
                                ? ((float x0, float y0, float x1, float y1)?)(c.Value.x0, c.Value.y0, c.Value.x1, c.Value.y1)
                                : null)
                            .ToList())
                        .ToList();
                    if (grid.Count == 0)
                        continue;
                    var key = ((int)Math.Round(b.X0), (int)Math.Round(b.Y0), (int)Math.Round(b.X1), (int)Math.Round(b.Y1));
                    if (!seen.Add(key))
                        continue;
                    candidates.Add((b, grid));
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return (candidates, finder);
        }

        static float RectArea(Rect rect)
            => Math.Max(0f, rect.X1 - rect.X0) * Math.Max(0f, rect.Y1 - rect.Y0);

        static float IntersectionArea(Rect left, Rect right)
        {
            float x0 = Math.Max(left.X0, right.X0);
            float y0 = Math.Max(left.Y0, right.Y0);
            float x1 = Math.Min(left.X1, right.X1);
            float y1 = Math.Min(left.Y1, right.Y1);
            if (x1 <= x0 || y1 <= y0)
                return 0f;
            return (x1 - x0) * (y1 - y0);
        }

        static float XOverlap(Rect left, Rect right)
            => Math.Max(0f, Math.Min(left.X1, right.X1) - Math.Max(left.X0, right.X0));

        /// <summary>
        /// The primary a split candidate belongs inside, plus an ambiguity flag.
        ///
        /// Returns (ownerIndex, ambiguous): owner is the best-contained primary
        /// (candidate&gt;=UnionOwnerContainment inside it), else null; ambiguous is True
        /// when the candidate overlaps some primary enough to be unsafe to append.
        /// </summary>
        static (int? ownerIndex, bool ambiguous) FindOwner(Rect candidateBbox, IList<Rect> existingBboxes)
        {
            float candidateArea = RectArea(candidateBbox);
            if (candidateArea <= 0)
                return (null, true);
            int? bestOwner = null;
            float bestContainment = 0f;
            bool ambiguous = false;
            for (int index = 0; index < existingBboxes.Count; index++)
            {
                var existingBbox = existingBboxes[index];
                float existingArea = RectArea(existingBbox);
                float interArea = IntersectionArea(candidateBbox, existingBbox);
                if (interArea <= 0 || existingArea <= 0)
                    continue;
                float candidateContainment = interArea / candidateArea;
                float existingCoverage = interArea / existingArea;
                if (candidateContainment >= UnionOwnerContainment && candidateContainment > bestContainment)
                {
                    bestOwner = index;
                    bestContainment = candidateContainment;
                }
                else if (candidateContainment >= UnionOwnerAmbiguousOverlap
                         || existingCoverage >= UnionOwnerAmbiguousOverlap)
                {
                    ambiguous = true;
                }
            }
            return (bestOwner, ambiguous);
        }

        /// <summary>
        /// Non-empty page text-span rects, cached on the page.
        ///
        /// Drives the grid-ref span-multiplicity gate: every non-blank span as a bare
        /// rect.
        /// </summary>
        static List<Rect> TextSpanRects(Page page)
        {
            if (TextSpansCache.TryGetValue(page, out var cached))
                return cached;
            var spans = new List<Rect>();
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
                                if (string.IsNullOrWhiteSpace(span?.Text))
                                    continue;
                                var rect = span.Bbox;
                                if (rect != null && !rect.IsEmpty)
                                    spans.Add(rect);
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
            try
            {
                TextSpansCache.Add(page, spans);
            }
            catch
            {
                // ignore
            }
            return spans;
        }

        /// <summary>Max horizontally-separated text-span groups on any single text line in a cell.</summary>
        static int CellSpanGroupCount(Rect cell, List<Rect> textSpans)
        {
            var lineBands = new List<(float bandY, List<(float x0, float x1)> intervals)>();
            foreach (var span in textSpans)
            {
                float centerY = (span.Y0 + span.Y1) / 2f;
                if (centerY < cell.Y0 - 1f || centerY > cell.Y1 + 1f)
                    continue;
                if (XOverlap(span, cell) <= 1f)
                    continue;
                float x0 = Math.Max(cell.X0, span.X0);
                float x1 = Math.Min(cell.X1, span.X1);
                if (x1 <= x0)
                    continue;
                bool placed = false;
                for (int index = 0; index < lineBands.Count; index++)
                {
                    var (bandY, intervals) = lineBands[index];
                    if (Math.Abs(centerY - bandY) <= 4f)
                    {
                        intervals.Add((x0, x1));
                        lineBands[index] = ((bandY + centerY) / 2f, intervals);
                        placed = true;
                        break;
                    }
                }
                if (!placed)
                    lineBands.Add((centerY, new List<(float, float)> { (x0, x1) }));
            }
            int best = 0;
            foreach (var (_, intervals) in lineBands)
            {
                int groups = 0;
                float? lastX1 = null;
                foreach (var (x0, x1) in intervals.OrderBy(i => i.x0))
                {
                    if (lastX1 == null || x0 - lastX1.Value > 4f)
                    {
                        groups++;
                        lastX1 = x1;
                    }
                    else
                        lastX1 = Math.Max(lastX1.Value, x1);
                }
                best = Math.Max(best, groups);
            }
            return best;
        }

        /// <summary>Max cell span-group count over a grid (high =&gt; under-segmented grid).</summary>
        static float? SpanMultiplicity(Page page, List<List<(float x0, float y0, float x1, float y1)?>> grid)
        {
            if (page == null)
                return null;
            var textSpans = TextSpanRects(page);
            float? best = null;
            foreach (var row in grid)
            {
                foreach (var cell in row)
                {
                    if (cell == null)
                        continue;
                    var rect = new Rect(cell.Value.x0, cell.Value.y0, cell.Value.x1, cell.Value.y1);
                    if (rect.IsEmpty)
                        continue;
                    int count = CellSpanGroupCount(rect, textSpans);
                    if (count > 0)
                        best = best == null ? count : Math.Max(best.Value, count);
                }
            }
            return best;
        }

        /// <summary>
        /// Primaries a candidate can grid-ref (replace grid with), 1:1 by IoU.
        ///
        /// existing/candidates are (bbox, grid) lists. Returns (refs, consumed):
        /// refs maps a primary index to the candidate supplying its grid (mutual 1:1
        /// IoU&gt;=threshold matches passing the span-multiplicity gate), consumed the
        /// candidate indexes used.
        /// </summary>
        static (Dictionary<int, (Rect bbox, List<List<(float x0, float y0, float x1, float y1)?>> grid)> refs, HashSet<int> consumed)
            OneToOneGridRefs(
                List<(Rect bbox, List<List<(float x0, float y0, float x1, float y1)?>> grid)> existing,
                List<(Rect bbox, List<List<(float x0, float y0, float x1, float y1)?>> grid)> candidates,
                float iouThreshold,
                Page page,
                bool spanMultGate,
                float spanMultThreshold)
        {
            var existingMatches = new Dictionary<int, List<(int candidateIndex, float iou)>>();
            var candidateMatches = new Dictionary<int, List<(int existingIndex, float iou)>>();
            for (int existingIndex = 0; existingIndex < existing.Count; existingIndex++)
            {
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    float iou = TableHelpers.Iou(
                        (existing[existingIndex].bbox.X0, existing[existingIndex].bbox.Y0, existing[existingIndex].bbox.X1, existing[existingIndex].bbox.Y1),
                        (candidates[candidateIndex].bbox.X0, candidates[candidateIndex].bbox.Y0, candidates[candidateIndex].bbox.X1, candidates[candidateIndex].bbox.Y1));
                    if (iou >= iouThreshold)
                    {
                        if (!existingMatches.ContainsKey(existingIndex))
                            existingMatches[existingIndex] = new List<(int, float)>();
                        existingMatches[existingIndex].Add((candidateIndex, iou));
                        if (!candidateMatches.ContainsKey(candidateIndex))
                            candidateMatches[candidateIndex] = new List<(int, float)>();
                        candidateMatches[candidateIndex].Add((existingIndex, iou));
                    }
                }
            }
            var refs = new Dictionary<int, (Rect, List<List<(float x0, float y0, float x1, float y1)?>>)>();
            var consumed = new HashSet<int>();
            foreach (var kv in existingMatches)
            {
                if (kv.Value.Count != 1)
                    continue;
                int candidateIndex = kv.Value[0].candidateIndex;
                if (!candidateMatches.TryGetValue(candidateIndex, out var cm) || cm.Count != 1)
                    continue;
                if (spanMultGate)
                {
                    float? spanMult = SpanMultiplicity(page, candidates[candidateIndex].grid);
                    if (spanMult != null && spanMult >= spanMultThreshold)
                        continue;
                }
                refs[kv.Key] = candidates[candidateIndex];
                consumed.Add(candidateIndex);
            }
            return (refs, consumed);
        }

        /// <summary>
        /// Fuse primary and candidate (bbox, grid) entries.
        ///
        /// Applies grid-ref replacement, split replacement (&gt;=2 candidates owned by one
        /// primary replace it, ordered by y0/x0) and append of unowned candidates,
        /// returning the fused entry list in the contractual order.
        /// </summary>
        internal static List<(Rect bbox, List<List<(float x0, float y0, float x1, float y1)?>> grid)> ReplaceAppend(
            List<(Rect bbox, List<List<(float x0, float y0, float x1, float y1)?>> grid)> existing,
            List<(Rect bbox, List<List<(float x0, float y0, float x1, float y1)?>> grid)> candidates,
            Page page,
            bool gridRef = true,
            float gridRefIou = UnionGridRefIou,
            bool spanMultGate = UnionGridRefSpanMultGate,
            float spanMultThreshold = UnionGridRefSpanMultThreshold)
        {
            var existingBboxes = existing.Select(e => e.bbox).ToList();
            Dictionary<int, (Rect bbox, List<List<(float x0, float y0, float x1, float y1)?>> grid)> gridRefs;
            HashSet<int> consumed;
            if (gridRef)
            {
                (gridRefs, consumed) = OneToOneGridRefs(
                    existing,
                    candidates,
                    iouThreshold: gridRefIou,
                    page: page,
                    spanMultGate: spanMultGate,
                    spanMultThreshold: spanMultThreshold);
            }
            else
            {
                gridRefs = new Dictionary<int, (Rect, List<List<(float x0, float y0, float x1, float y1)?>>)>();
                consumed = new HashSet<int>();
            }

            var replacements = new Dictionary<int, List<(Rect, List<List<(float x0, float y0, float x1, float y1)?>>)>>();
            var appendCandidates = new List<(Rect, List<List<(float x0, float y0, float x1, float y1)?>>)>();
            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                if (consumed.Contains(candidateIndex))
                    continue;
                var candidate = candidates[candidateIndex];
                var (ownerIndex, ambiguous) = FindOwner(candidate.bbox, existingBboxes);
                if (ownerIndex != null)
                {
                    if (!replacements.ContainsKey(ownerIndex.Value))
                        replacements[ownerIndex.Value] = new List<(Rect, List<List<(float x0, float y0, float x1, float y1)?>>)>();
                    replacements[ownerIndex.Value].Add(candidate);
                }
                else if (ambiguous)
                    continue; // overlaps a primary but is not contained -> suppress
                else
                    appendCandidates.Add(candidate);
            }

            var finalReplacements = replacements
                .Where(kv => kv.Value.Count >= 2)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.OrderBy(e => e.Item1.Y0).ThenBy(e => e.Item1.X0).ToList());

            var entries = new List<(Rect, List<List<(float x0, float y0, float x1, float y1)?>>)>();
            for (int index = 0; index < existing.Count; index++)
            {
                if (finalReplacements.TryGetValue(index, out var reps))
                    entries.AddRange(reps);
                else if (gridRefs.TryGetValue(index, out var grepped))
                {
                    // Grid-ref: keep the primary's (layout) bbox, take the candidate grid.
                    entries.Add((existing[index].bbox, grepped.grid));
                }
                else
                    entries.Add(existing[index]);
            }
            entries.AddRange(appendCandidates);
            return entries;
        }

        /// <summary>
        /// Detect a page's tables by fusing layout grids with line-based candidates.
        ///
        /// Ensures the raw layout (computed only when page.LayoutInformation is null,
        /// like the official use_layout path), reads primary grids, detects candidates,
        /// applies grid-ref / split / append, and returns a TableFinder whose .Tables
        /// carry the fused grids in contractual order (grid-ref tables keep their
        /// explicit layout bbox).
        /// </summary>
        internal static TableFinder FindTablesUnion(Page page)
        {
            if (page.LayoutInformation == null)
                page.GetLayout();
            var primaries = LayoutTableGrids(page);
            var (candidates, finder) = LineCandidates(page);
            var entries = ReplaceAppend(
                primaries,
                candidates,
                page,
                gridRef: true,
                gridRefIou: UnionGridRefIou,
                spanMultGate: UnionGridRefSpanMultGate,
                spanMultThreshold: UnionGridRefSpanMultThreshold);

            if (finder == null)
            {
                // Candidate detection failed mid-way; the nested FindTables may have
                // left partially-filled EDGES/CHARS state behind, and TableFinder's
                // constructor runs a full detection from that state -- clear it first
                // so the fallback really is an empty shell.
                TableModule.Edges.Value.Clear();
                TableModule.Chars.Value.Clear();
                finder = new TableFinder(page);
            }

            var tables = new List<Table>();
            foreach (var (bbox, grid) in entries)
            {
                var flat = grid
                    .SelectMany(row => row)
                    .Where(c => c != null)
                    .Select(c => c.Value)
                    .ToList();
                if (flat.Count == 0)
                    continue;
                tables.Add(new Table(page, flat, bbox: bbox));
            }
            finder.Tables = tables;
            return finder;
        }

        // C#-only helpers for reading layout dict-like objects from PDF4LLM / GetLayout.
        static object GetLayoutValue(object row, string key)
        {
            if (row == null || string.IsNullOrEmpty(key))
                return null;
            if (row is System.Collections.IDictionary dict && dict.Contains(key))
                return dict[key];
            var indexer = row.GetType().GetProperty("Item", new[] { typeof(string) });
            if (indexer != null)
                return indexer.GetValue(row, new object[] { key });
            return row.GetType().GetProperty(key)?.GetValue(row);
        }

        static bool TryParseBbox(object bbox, out float x0, out float y0, out float x1, out float y1)
        {
            x0 = y0 = x1 = y1 = 0;
            if (bbox is System.Collections.IList list && list.Count >= 4)
            {
                x0 = Convert.ToSingle(list[0]);
                y0 = Convert.ToSingle(list[1]);
                x1 = Convert.ToSingle(list[2]);
                y1 = Convert.ToSingle(list[3]);
                return true;
            }
            return false;
        }

        static List<float> ToFloatList(object value)
        {
            var result = new List<float>();
            if (value is System.Collections.IEnumerable seq && value is not string)
            {
                foreach (var item in seq)
                    result.Add(Convert.ToSingle(item));
            }
            return result;
        }
    }
}
