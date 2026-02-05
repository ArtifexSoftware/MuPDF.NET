/*
Copyright (C) 2023 Artifex Software, Inc.

This file is part of MuPDF.NET.

MuPDF.NET is free software: you can redistribute it and/or modify it under the
terms of the GNU Affero General Public License as published by the Free
Software Foundation, either version 3 of the License, or (at your option)
any later version.

MuPDF.NET is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
details.

You should have received a copy of the GNU Affero General Public License
along with MuPDF. If not, see <https://www.gnu.org/licenses/agpl-3.0.en.html>

Alternative licensing terms are available from the licensor.
For commercial licensing, see <https://www.artifex.com/> or contact
Artifex Software, Inc., 39 Mesa Street, Suite 108A, San Francisco,
CA 94129, USA, for further information.

---------------------------------------------------------------------
Portions of this code have been ported from pdfplumber, see
https://pypi.org/project/pdfplumber/.

The ported code is under the following MIT license:

---------------------------------------------------------------------
The MIT License (MIT)

Copyright (c) 2015, Jeremy Singer-Vine

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
---------------------------------------------------------------------
Also see here: https://github.com/jsvine/pdfplumber/blob/stable/LICENSE.txt
---------------------------------------------------------------------

The porting mainly pertains to files "table.py" and relevant parts of
"utils/text.py" within pdfplumber's repository on Github.
With respect to "text.py", we have removed functions or features that are not
used by table processing. Examples are:

* the text search function
* simple text extraction
* text extraction by lines

Original pdfplumber code does neither detect, nor identify table headers.
This MuPDF.NET port adds respective code to the 'Table' class as method '_get_header'.
This is implemented as new class TableHeader with the properties:
* bbox: A tuple for the header's bbox
* cells: A tuple for each bbox of a column header
* names: A list of strings with column header text
* external: A bool indicating whether the header is outside the table cells.

*/

using mupdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MuPDF.NET
{
    // Global state for table processing
    internal static class TableGlobals
    {
        internal static List<Edge> EDGES = new List<Edge>();  // vector graphics from MuPDF
        internal static List<CharDict> CHARS = new List<CharDict>();  // text characters from MuPDF
        internal static TextPage TEXTPAGE = null;  // textpage for cell text extraction
        
        // Constants
        internal static readonly HashSet<char> WHITE_SPACES = new HashSet<char> { ' ', '\t', '\n', '\r', '\f', '\v' };
        // TEXT_FONT_BOLD = 16, but for char flags use FZ_STEXT_BOLD
        internal static readonly int TEXT_BOLD = (int)mupdf.mupdf.FZ_STEXT_BOLD;
        // TEXT_STRIKEOUT = mupdf.FZ_STEXT_STRIKEOUT
        internal static readonly int TEXT_STRIKEOUT = (int)mupdf.mupdf.FZ_STEXT_STRIKEOUT;
        // TEXT_COLLECT_STYLES = mupdf.FZ_STEXT_COLLECT_STYLES
        internal static readonly int TEXT_COLLECT_STYLES = (int)mupdf.mupdf.FZ_STEXT_COLLECT_STYLES;
        // TEXT_COLLECT_VECTORS = mupdf.FZ_STEXT_COLLECT_VECTORS
        internal static readonly int TEXT_COLLECT_VECTORS = (int)mupdf.mupdf.FZ_STEXT_COLLECT_VECTORS;
        // TEXT_SEGMENT = mupdf.FZ_STEXT_SEGMENT
        internal static readonly int TEXT_SEGMENT = (int)mupdf.mupdf.FZ_STEXT_SEGMENT;
        // From table.py FLAGS: TEXTFLAGS_TEXT | TEXT_COLLECT_STYLES | TEXT_ACCURATE_BBOXES | TEXT_MEDIABOX_CLIP
        internal static readonly int FLAGS = 
            (int)TextFlagsExtension.TEXTFLAGS_TEXT |
            TEXT_COLLECT_STYLES |
            (int)TextFlags.TEXT_ACCURATE_BBOXES |
            (int)TextFlags.TEXT_MEDIABOX_CLIP;
        // From table.py TABLE_DETECTOR_FLAGS: TEXT_ACCURATE_BBOXES | TEXT_SEGMENT | TEXT_COLLECT_VECTORS | TEXT_MEDIABOX_CLIP
        internal static readonly int TABLE_DETECTOR_FLAGS =
            (int)TextFlags.TEXT_ACCURATE_BBOXES |
            TEXT_SEGMENT |
            TEXT_COLLECT_VECTORS |
            (int)TextFlags.TEXT_MEDIABOX_CLIP;
    }

    // Constants
    internal static class TableConstants
    {
        internal static readonly string[] NON_NEGATIVE_SETTINGS = {
            "snap_tolerance", "snap_x_tolerance", "snap_y_tolerance",
            "join_tolerance", "join_x_tolerance", "join_y_tolerance",
            "edge_min_length", "min_words_vertical", "min_words_horizontal",
            "intersection_tolerance", "intersection_x_tolerance", "intersection_y_tolerance"
        };

        internal static readonly Dictionary<string, string> LIGATURES = new Dictionary<string, string>
        {
            { "ﬀ", "ff" },
            { "ﬃ", "ffi" },
            { "ﬄ", "ffl" },
            { "ﬁ", "fi" },
            { "ﬂ", "fl" },
            { "ﬆ", "st" },
            { "ﬅ", "st" }
        };
    }

    // Character dictionary structure
    internal class CharDict
    {
        public float adv { get; set; }
        public float bottom { get; set; }
        public float doctop { get; set; }
        public string fontname { get; set; }
        public float height { get; set; }
        public Tuple<float, float, float, float, float, float> matrix { get; set; }
        public string ncs { get; set; }
        public Tuple<float, float, float> non_stroking_color { get; set; }
        public object non_stroking_pattern { get; set; }
        public string object_type { get; set; }
        public int page_number { get; set; }
        public float size { get; set; }
        public Tuple<float, float, float> stroking_color { get; set; }
        public object stroking_pattern { get; set; }
        public bool bold { get; set; }
        public string text { get; set; }
        public float top { get; set; }
        public bool upright { get; set; }
        public float width { get; set; }
        public float x0 { get; set; }
        public float x1 { get; set; }
        public float y0 { get; set; }
        public float y1 { get; set; }
    }

    // Edge structure for table detection
    public class Edge
    {
        public float x0 { get; set; }
        public float x1 { get; set; }
        public float top { get; set; }
        public float bottom { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public string orientation { get; set; }  // "h" or "v"
        public string object_type { get; set; }
        public float doctop { get; set; }
        public int page_number { get; set; }
        public float y0 { get; set; }
        public float y1 { get; set; }
    }

    // Helper functions
    internal static class TableHelpers
    {
        // rect_in_rect - Check whether rectangle 'inner' is fully inside rectangle 'outer'
        internal static bool RectInRect(Rect inner, Rect outer)
        {
            return inner.X0 >= outer.X0 && inner.Y0 >= outer.Y0 &&
                   inner.X1 <= outer.X1 && inner.Y1 <= outer.Y1;
        }

        // chars_in_rect - Check whether any of the chars are inside rectangle
        internal static bool CharsInRect(List<CharDict> chars, Rect rect)
        {
            return chars.Any(c =>
                rect.X0 <= c.x0 && c.x1 <= rect.X1 &&
                rect.Y0 <= c.y0 && c.y1 <= rect.Y1);
        }

        // _iou - Compute intersection over union of two rectangles
        internal static float Iou(Rect r1, Rect r2)
        {
            float ix = Math.Max(0, Math.Min(r1.X1, r2.X1) - Math.Max(r1.X0, r2.X0));
            float iy = Math.Max(0, Math.Min(r1.Y1, r2.Y1) - Math.Max(r1.Y0, r2.Y0));
            float intersection = ix * iy;
            if (intersection == 0)
                return 0;
            float area1 = (r1.X1 - r1.X0) * (r1.Y1 - r1.Y0);
            float area2 = (r2.X1 - r2.X0) * (r2.Y1 - r2.Y0);
            return intersection / (area1 + area2 - intersection);
        }

        // intersects_words_h - Check whether any words are cut through by horizontal line y
        internal static bool IntersectsWordsH(Rect bbox, float y, List<Rect> wordRects)
        {
            return wordRects.Any(r => RectInRect(r, bbox) && r.Y0 < y && y < r.Y1);
        }

        // get_table_dict_from_rect - Extract MuPDF table structure information
        // Note: This requires native MuPDF interop to call fz_find_table_within_bounds
        // This would need to be implemented via P/Invoke or native wrapper
        internal static Dictionary<string, object> GetTableDictFromRect(TextPage textpage, Rect rect)
        {
            var tableDict = new Dictionary<string, object>();
            // TODO: Implement native interop call to MuPDF's table detection function
            // This is used by make_table_from_bbox which is called when layout_information finds tables
            return tableDict;
        }

        // make_table_from_bbox - Detect table structure within a given rectangle
        internal static List<Rect> MakeTableFromBbox(TextPage textpage, List<Rect> wordRects, Rect rect)
        {
            var cells = new List<Rect>();
            var block = GetTableDictFromRect(textpage, rect);
            
            if (!block.ContainsKey("type") || Convert.ToInt32(block["type"]) != mupdf.mupdf.FZ_STEXT_BLOCK_GRID)
                return cells;

            var bboxList = block["bbox"] as List<object>;
            if (bboxList == null || bboxList.Count < 4)
                return cells;

            var bbox = new Rect(
                Convert.ToSingle(bboxList[0]),
                Convert.ToSingle(bboxList[1]),
                Convert.ToSingle(bboxList[2]),
                Convert.ToSingle(bboxList[3])
            );

            var xpos = (block["xpos"] as List<object>)?.Cast<List<object>>()
                .Select(x => Tuple.Create(Convert.ToSingle(x[0]), Convert.ToSingle(x[1])))
                .OrderBy(x => x.Item1).ToList() ?? new List<Tuple<float, float>>();
            
            var ypos = (block["ypos"] as List<object>)?.Cast<List<object>>()
                .Select(y => Tuple.Create(Convert.ToSingle(y[0]), Convert.ToSingle(y[1])))
                .OrderBy(y => y.Item1).ToList() ?? new List<Tuple<float, float>>();

            var maxUncertain = block["max_uncertain"] as List<object>;
            float xmaxu = maxUncertain != null && maxUncertain.Count > 0 ? Convert.ToSingle(maxUncertain[0]) : 0;
            float ymaxu = maxUncertain != null && maxUncertain.Count > 1 ? Convert.ToSingle(maxUncertain[1]) : 0;

            // Modify ypos to remove uncertain positions
            var nypos = new List<float>();
            foreach (var (y, yunc) in ypos)
            {
                if (yunc > 0) continue;
                if (IntersectsWordsH(bbox, y, wordRects)) continue;
                if (nypos.Count > 0 && (y - nypos[nypos.Count - 1] < 3))
                    nypos[nypos.Count - 1] = y;
                else
                    nypos.Add(y);
            }

            ymaxu = Math.Max(0, (float)Math.Round((nypos.Count - 2) * 0.35));

            var nxpos = xpos.Where(x => x.Item2 <= ymaxu).Select(x => x.Item1).ToList();
            if (bbox.X1 > nxpos[nxpos.Count - 1] + 3)
                nxpos.Add(bbox.X1);

            // Compose cells from remaining x and y positions
            for (int i = 0; i < nypos.Count - 1; i++)
            {
                var rowBox = new Rect(bbox.X0, nypos[i], bbox.X1, nypos[i + 1]);
                var rowWords = wordRects.Where(r => RectInRect(r, rowBox))
                    .OrderBy(r => r.X0).ToList();
                
                var thisXpos = nxpos.Where(x => !rowWords.Any(r => r.X0 < x && x < r.X1)).ToList();
                
                for (int j = 0; j < thisXpos.Count - 1; j++)
                {
                    var cell = new Rect(thisXpos[j], nypos[i], thisXpos[j + 1], nypos[i + 1]);
                    if (!cell.IsEmpty)
                        cells.Add(cell);
                }
            }

            return cells;
        }

        // extract_cells - Extract text from a cell as plain or MD styled text
        internal static string ExtractCells(TextPage textpage, Rect cell, bool markdown = false)
        {
            if (textpage == null)
                return "";

            var text = new StringBuilder();
            var pageInfo = textpage.ExtractRAWDict(cropbox: null, sort: false);

            if (pageInfo?.Blocks == null)
                return "";

            foreach (var block in pageInfo.Blocks)
            {
                if (block.Type != 0) continue;

                var blockBbox = block.Bbox;
                if (blockBbox == null) continue;

                if (blockBbox.X0 > cell.X1 || blockBbox.X1 < cell.X0 ||
                    blockBbox.Y0 > cell.Y1 || blockBbox.Y1 < cell.Y0)
                    continue;

                if (block.Lines == null) continue;

                foreach (var line in block.Lines)
                {
                    if (line.Bbox == null) continue;

                    var lbbox = line.Bbox;
                    if (lbbox.X0 > cell.X1 || lbbox.X1 < cell.X0 ||
                        lbbox.Y0 > cell.Y1 || lbbox.Y1 < cell.Y0)
                        continue;

                    if (text.Length > 0)
                        text.Append(markdown ? "<br>" : "\n");

                    var lineDir = line.Dir;
                    bool horizontal = lineDir != null &&
                        (lineDir.X == 0 && lineDir.Y == 1 || lineDir.X == 1 && lineDir.Y == 0);

                    if (line.Spans == null) continue;

                    foreach (var span in line.Spans)
                    {
                        if (span.Bbox == null) continue;

                        var sbbox = span.Bbox;
                        if (sbbox.X0 > cell.X1 || sbbox.X1 < cell.X0 ||
                            sbbox.Y0 > cell.Y1 || sbbox.Y1 < cell.Y0)
                            continue;

                        var spanText = new StringBuilder();
                        if (span.Chars != null)
                        {
                            foreach (var char_ in span.Chars)
                            {
                                if (char_.Bbox == null) continue;

                                var charRect = new Rect(char_.Bbox);
                                var cellRect = new Rect(cell.X0, cell.Y0, cell.X1, cell.Y1);
                                var intersection = charRect & cellRect;
                                
                                if (intersection != null && !intersection.IsEmpty && 
                                    (intersection.Width * intersection.Height) > 0.5 * (charRect.Width * charRect.Height))
                                {
                                    spanText.Append(char_.C);
                                }
                                else if (TableGlobals.WHITE_SPACES.Contains(char_.C))
                                {
                                    spanText.Append(" ");
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(span.Text))
                        {
                            spanText.Append(span.Text);
                        }

                        if (spanText.Length == 0) continue;

                        if (!markdown)
                        {
                            text.Append(spanText);
                            continue;
                        }

                        string prefix = "";
                        string suffix = "";
                        float flags = span.Flags;

                        if (horizontal && ((int)flags & TableGlobals.TEXT_STRIKEOUT) != 0)
                        {
                            prefix += "~~";
                            suffix = "~~" + suffix;
                        }
                        if (((int)flags & TableGlobals.TEXT_BOLD) != 0)
                        {
                            prefix += "**";
                            suffix = "**" + suffix;
                        }
                        if (((int)flags & (int)FontStyle.TEXT_FONT_ITALIC) != 0)
                        {
                            prefix += "_";
                            suffix = "_" + suffix;
                        }
                        if (((int)flags & (int)FontStyle.TEXT_FONT_MONOSPACED) != 0)
                        {
                            prefix += "`";
                            suffix = "`" + suffix;
                        }

                        string spanTextStr = spanText.ToString();
                        if (span.Chars != null && span.Chars.Count > 2)
                            spanTextStr = spanTextStr.TrimEnd();

                        if (suffix.Length > 0 && text.ToString().EndsWith(suffix))
                        {
                            text.Remove(text.Length - suffix.Length, suffix.Length);
                            text.Append(spanTextStr + suffix);
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(spanTextStr))
                                text.Append(" ");
                            else
                                text.Append(prefix + spanTextStr + suffix);
                        }
                    }
                }
            }

            return text.ToString().Trim();
        }

        // to_list - Convert collection to list
        internal static List<T> ToList<T>(object collection)
        {
            if (collection is List<T> list)
                return list;
            if (collection is IEnumerable<T> enumerable)
                return enumerable.ToList();
            return new List<T> { (T)collection };
        }

        // Helper function for clustering objects
        internal static List<List<T>> ClusterObjects<T>(IEnumerable<T> xs, Func<T, float> keyFn, float tolerance)
        {
            if (tolerance == 0)
                return xs.OrderBy(keyFn).Select(x => new List<T> { x }).ToList();

            var xsList = xs.ToList();
            if (xsList.Count < 2)
                return xsList.Select(x => new List<T> { x }).ToList();

            var values = xsList.Select(keyFn).Distinct().OrderBy(v => v).ToList();
            var clusters = ClusterList(values, tolerance);

            var clusterDict = new Dictionary<float, int>();
            for (int i = 0; i < clusters.Count; i++)
            {
                foreach (var val in clusters[i])
                    clusterDict[val] = i;
            }

            var grouped = xsList.GroupBy(x => clusterDict[keyFn(x)]).OrderBy(g => g.Key);
            return grouped.Select(g => g.ToList()).ToList();
        }

        internal static List<List<float>> ClusterList(List<float> xs, float tolerance = 0)
        {
            if (tolerance == 0)
                return xs.OrderBy(x => x).Select(x => new List<float> { x }).ToList();

            if (xs.Count < 2)
                return xs.Select(x => new List<float> { x }).ToList();

            var groups = new List<List<float>>();
            var sortedXs = xs.OrderBy(x => x).ToList();
            var currentGroup = new List<float> { sortedXs[0] };
            float last = sortedXs[0];

            for (int i = 1; i < sortedXs.Count; i++)
            {
                float x = sortedXs[i];
                if (x <= (last + tolerance))
                {
                    currentGroup.Add(x);
                }
                else
                {
                    groups.Add(currentGroup);
                    currentGroup = new List<float> { x };
                }
                last = x;
            }
            groups.Add(currentGroup);
            return groups;
        }

        internal static Rect ObjectsToBbox(IEnumerable<object> objects)
        {
            var rects = new List<Rect>();
            foreach (var obj in objects)
            {
                if (obj is CharDict charDict)
                {
                    rects.Add(new Rect(charDict.x0, charDict.top, charDict.x1, charDict.bottom));
                }
                else if (obj is Dictionary<string, object> dict)
                {
                    if (dict.ContainsKey("x0") && dict.ContainsKey("top") && dict.ContainsKey("x1") && dict.ContainsKey("bottom"))
                    {
                        rects.Add(new Rect(
                            Convert.ToSingle(dict["x0"]),
                            Convert.ToSingle(dict["top"]),
                            Convert.ToSingle(dict["x1"]),
                            Convert.ToSingle(dict["bottom"])
                        ));
                    }
                }
            }

            if (rects.Count == 0)
                return new Rect(0, 0, 0, 0);

            return new Rect(
                rects.Min(r => r.X0),
                rects.Min(r => r.Y0),
                rects.Max(r => r.X1),
                rects.Max(r => r.Y1)
            );
        }
    }

    // TextMap class - maps each unicode character to a char object
    internal class TextMap
    {
        public List<Tuple<string, CharDict>> tuples { get; set; }
        public string as_string { get; set; }

        public TextMap(List<Tuple<string, CharDict>> tuples = null)
        {
            this.tuples = tuples ?? new List<Tuple<string, CharDict>>();
            this.as_string = string.Join("", this.tuples.Select(t => t.Item1));
        }

        public Dictionary<string, object> MatchToDict(
            Match m,
            int mainGroup = 0,
            bool returnGroups = true,
            bool returnChars = true)
        {
            var subset = tuples.Skip(m.Groups[mainGroup].Index).Take(m.Groups[mainGroup].Length).ToList();
            var chars = subset.Where(t => t.Item2 != null).Select(t => t.Item2).ToList();
            var bbox = TableHelpers.ObjectsToBbox(chars);

            var result = new Dictionary<string, object>
            {
                { "text", m.Groups[mainGroup].Value },
                { "x0", bbox.X0 },
                { "top", bbox.Y0 },
                { "x1", bbox.X1 },
                { "bottom", bbox.Y1 }
            };

            if (returnGroups)
            {
                var groups = new List<string>();
                for (int i = 1; i < m.Groups.Count; i++)
                    groups.Add(m.Groups[i].Value);
                result["groups"] = groups;
            }

            if (returnChars)
                result["chars"] = chars;

            return result;
        }
    }

    // WordMap class - maps words to chars
    internal class WordMap
    {
        public List<Tuple<Dictionary<string, object>, List<CharDict>>> tuples { get; set; }

        public WordMap(List<Tuple<Dictionary<string, object>, List<CharDict>>> tuples)
        {
            this.tuples = tuples;
        }

        public TextMap ToTextmap(
            bool layout = false,
            float layoutWidth = 0,
            float layoutHeight = 0,
            int layoutWidthChars = 0,
            int layoutHeightChars = 0,
            float xDensity = TableFlags.TABLE_DEFAULT_X_DENSITY,
            float yDensity = TableFlags.TABLE_DEFAULT_Y_DENSITY,
            float xShift = 0,
            float yShift = 0,
            float yTolerance = TableFlags.TABLE_DEFAULT_Y_TOLERANCE,
            bool useTextFlow = false,
            bool presorted = false,
            bool expandLigatures = true)
        {
            var textmap = new List<Tuple<string, CharDict>>();

            if (tuples.Count == 0)
                return new TextMap(textmap);

            var expansions = expandLigatures ? TableConstants.LIGATURES : new Dictionary<string, string>();

            int layoutWidthCharsFinal = layoutWidthChars;
            int layoutHeightCharsFinal = layoutHeightChars;

            if (layout)
            {
                if (layoutWidthChars > 0)
                {
                    if (layoutWidth > 0)
                        throw new ArgumentException("`layout_width` and `layout_width_chars` cannot both be set.");
                }
                else
                {
                    layoutWidthCharsFinal = (int)Math.Round(layoutWidth / xDensity);
                }

                if (layoutHeightChars > 0)
                {
                    if (layoutHeight > 0)
                        throw new ArgumentException("`layout_height` and `layout_height_chars` cannot both be set.");
                }
                else
                {
                    layoutHeightCharsFinal = (int)Math.Round(layoutHeight / yDensity);
                }
            }

            var blankLine = layout ? Enumerable.Range(0, layoutWidthCharsFinal)
                .Select(_ => Tuple.Create(" ", (CharDict)null)).ToList() : new List<Tuple<string, CharDict>>();

            int numNewlines = 0;

            var wordsSortedDoctop = presorted || useTextFlow
                ? tuples
                : tuples.OrderBy(t => Convert.ToSingle(t.Item1["doctop"])).ToList();

            if (wordsSortedDoctop.Count == 0)
                return new TextMap(textmap);

            var firstWord = wordsSortedDoctop[0].Item1;
            float doctopStart = Convert.ToSingle(firstWord["doctop"]) - Convert.ToSingle(firstWord["top"]);

            var clusters = TableHelpers.ClusterObjects(wordsSortedDoctop, t => Convert.ToSingle(t.Item1["doctop"]), yTolerance);

            for (int i = 0; i < clusters.Count; i++)
            {
                var ws = clusters[i];
                float yDist = layout
                    ? (Convert.ToSingle(ws[0].Item1["doctop"]) - (doctopStart + yShift)) / yDensity
                    : 0;

                int numNewlinesPrepend = Math.Max(
                    i > 0 ? 1 : 0,
                    (int)Math.Round(yDist) - numNewlines
                );

                for (int j = 0; j < numNewlinesPrepend; j++)
                {
                    if (textmap.Count == 0 || textmap[textmap.Count - 1].Item1 == "\n")
                        textmap.AddRange(blankLine);
                    textmap.Add(Tuple.Create("\n", (CharDict)null));
                }

                numNewlines += numNewlinesPrepend;

                int lineLen = 0;
                var lineWordsSortedX0 = presorted || useTextFlow
                    ? ws
                    : ws.OrderBy(t => Convert.ToSingle(t.Item1["x0"])).ToList();

                foreach (Tuple<Dictionary<string, object>, List<CharDict>> tuple in lineWordsSortedX0)
                {
                    var word = tuple.Item1;
                    var chars = tuple.Item2;
                    float xDist = layout ? (Convert.ToSingle(word["x0"]) - xShift) / xDensity : 0;
                    int numSpacesPrepend = Math.Max(Math.Min(1, lineLen), (int)Math.Round(xDist) - lineLen);
                    
                    for (int k = 0; k < numSpacesPrepend; k++)
                        textmap.Add(Tuple.Create(" ", (CharDict)null));
                    lineLen += numSpacesPrepend;

                    foreach (var c in chars)
                    {
                        string letters = expansions.ContainsKey(c.text) ? expansions[c.text] : c.text;
                        foreach (char letter in letters)
                        {
                            textmap.Add(Tuple.Create(letter.ToString(), c));
                            lineLen++;
                        }
                    }
                }

                if (layout)
                {
                    for (int k = 0; k < layoutWidthCharsFinal - lineLen; k++)
                        textmap.Add(Tuple.Create(" ", (CharDict)null));
                }
            }

            if (layout)
            {
                int numNewlinesAppend = layoutHeightCharsFinal - (numNewlines + 1);
                for (int i = 0; i < numNewlinesAppend; i++)
                {
                    if (i > 0)
                        textmap.AddRange(blankLine);
                    textmap.Add(Tuple.Create("\n", (CharDict)null));
                }

                if (textmap.Count > 0 && textmap[textmap.Count - 1].Item1 == "\n")
                    textmap.RemoveAt(textmap.Count - 1);
            }

            return new TextMap(textmap);
        }
    }

    // WordExtractor class
    internal class WordExtractor
    {
        public float x_tolerance { get; set; }
        public float y_tolerance { get; set; }
        public bool keep_blank_chars { get; set; }
        public bool use_text_flow { get; set; }
        public bool horizontal_ltr { get; set; }
        public bool vertical_ttb { get; set; }
        public List<string> extra_attrs { get; set; }
        public string split_at_punctuation { get; set; }
        public Dictionary<string, string> expansions { get; set; }

        public WordExtractor(
            float x_tolerance = TableFlags.TABLE_DEFAULT_X_TOLERANCE,
            float y_tolerance = TableFlags.TABLE_DEFAULT_Y_TOLERANCE,
            bool keep_blank_chars = false,
            bool use_text_flow = false,
            bool horizontal_ltr = true,
            bool vertical_ttb = false,
            List<string> extra_attrs = null,
            bool split_at_punctuation = false,
            bool expand_ligatures = true)
        {
            this.x_tolerance = x_tolerance;
            this.y_tolerance = y_tolerance;
            this.keep_blank_chars = keep_blank_chars;
            this.use_text_flow = use_text_flow;
            this.horizontal_ltr = horizontal_ltr;
            this.vertical_ttb = vertical_ttb;
            this.extra_attrs = extra_attrs ?? new List<string>();
            this.split_at_punctuation = split_at_punctuation
                ? "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~"
                : "";
            this.expansions = expand_ligatures ? TableConstants.LIGATURES : new Dictionary<string, string>();
        }

        public Dictionary<string, object> MergeChars(List<CharDict> orderedChars)
        {
            var bbox = TableHelpers.ObjectsToBbox(orderedChars);
            float doctopAdj = orderedChars[0].doctop - orderedChars[0].top;
            bool upright = orderedChars[0].upright;
            int direction = (upright ? horizontal_ltr : vertical_ttb) ? 1 : -1;

            var matrix = orderedChars[0].matrix;
            int rotation = 0;

            if (!upright && matrix.Item2 < 0)
            {
                orderedChars = orderedChars.AsEnumerable().Reverse().ToList();
                rotation = 270;
            }

            if (matrix.Item1 < 0 && matrix.Item4 < 0)
                rotation = 180;
            else if (matrix.Item2 > 0)
                rotation = 90;

            var word = new Dictionary<string, object>
            {
                { "text", string.Join("", orderedChars.Select(c => expansions.ContainsKey(c.text) ? expansions[c.text] : c.text)) },
                { "x0", bbox.X0 },
                { "x1", bbox.X1 },
                { "top", bbox.Y0 },
                { "doctop", bbox.Y0 + doctopAdj },
                { "bottom", bbox.Y1 },
                { "upright", upright },
                { "direction", direction },
                { "rotation", rotation }
            };

            foreach (var key in extra_attrs)
            {
                if (orderedChars.Count > 0)
                {
                    var prop = typeof(CharDict).GetProperty(key);
                    if (prop != null)
                        word[key] = prop.GetValue(orderedChars[0]);
                }
            }

            return word;
        }

        public bool CharBeginsNewWord(CharDict prevChar, CharDict currChar)
        {
            if (currChar.upright)
            {
                float x = x_tolerance;
                float y = y_tolerance;
                float ay = prevChar.top;
                float cy = currChar.top;
                float ax, bx, cx;

                if (horizontal_ltr)
                {
                    ax = prevChar.x0;
                    bx = prevChar.x1;
                    cx = currChar.x0;
                }
                else
                {
                    ax = -prevChar.x1;
                    bx = -prevChar.x0;
                    cx = -currChar.x1;
                }

                return (cx < ax) || (cx > bx + x) || (cy > ay + y);
            }
            else
            {
                float x = y_tolerance;
                float y = x_tolerance;
                float ay = prevChar.x0;
                float cy = currChar.x0;
                float ax, bx, cx;

                if (vertical_ttb)
                {
                    ax = prevChar.top;
                    bx = prevChar.bottom;
                    cx = currChar.top;
                }
                else
                {
                    ax = -prevChar.bottom;
                    bx = -prevChar.top;
                    cx = -currChar.bottom;
                }

                return (cx < ax) || (cx > bx + x) || (cy > ay + y);
            }
        }

        public IEnumerable<List<CharDict>> IterCharsToWords(IEnumerable<CharDict> orderedChars)
        {
            var currentWord = new List<CharDict>();

            foreach (var char_ in orderedChars)
            {
                string text = char_.text;

                if (!keep_blank_chars && string.IsNullOrWhiteSpace(text))
                {
                    if (currentWord.Count > 0)
                    {
                        yield return currentWord;
                        currentWord = new List<CharDict>();
                    }
                }
                else if (split_at_punctuation.Contains(text))
                {
                    currentWord.Add(char_);
                    yield return currentWord;
                    currentWord = new List<CharDict>();
                }
                else if (currentWord.Count > 0 && CharBeginsNewWord(currentWord[currentWord.Count - 1], char_))
                {
                    yield return currentWord;
                    currentWord = new List<CharDict> { char_ };
                }
                else
                {
                    currentWord.Add(char_);
                }
            }

            if (currentWord.Count > 0)
                yield return currentWord;
        }

        public IEnumerable<CharDict> IterSortChars(IEnumerable<CharDict> chars)
        {
            var charsList = chars.ToList();
            var uprightClusters = TableHelpers.ClusterObjects(charsList, c => c.upright ? -1 : 0, 0);

            foreach (var uprightCluster in uprightClusters)
            {
                bool upright = uprightCluster[0].upright;
                string clusterKey = upright ? "doctop" : "x0";

                var subclusters = TableHelpers.ClusterObjects<CharDict>(uprightCluster, c => GetCharValue(c, clusterKey), y_tolerance);

                foreach (var sc in subclusters)
                {
                    string sortKey = upright ? "x0" : "doctop";
                    var toYield = sc.OrderBy(c => GetCharValue(c, sortKey)).ToList();

                    if (!(upright ? horizontal_ltr : vertical_ttb))
                        toYield.Reverse();

                    foreach (var c in toYield)
                        yield return c;
                }
            }
        }

        private float GetCharValue(CharDict c, string key)
        {
            switch (key)
            {
                case "x0":
                    return c.x0;
                case "doctop":
                    return c.doctop;
                case "top":
                    return c.top;
                default:
                    return 0;
            }
        }

        public IEnumerable<Tuple<Dictionary<string, object>, List<CharDict>>> IterExtractTuples(IEnumerable<CharDict> chars)
        {
            var orderedChars = use_text_flow ? chars : IterSortChars(chars);
            var groupedChars = orderedChars.GroupBy(c => new { c.upright });

            foreach (var group in groupedChars)
            {
                foreach (var wordChars in IterCharsToWords(group))
                {
                    yield return Tuple.Create(MergeChars(wordChars), wordChars);
                }
            }
        }

        public WordMap ExtractWordmap(IEnumerable<CharDict> chars)
        {
            return new WordMap(IterExtractTuples(chars).ToList());
        }

        public List<Dictionary<string, object>> ExtractWords(IEnumerable<CharDict> chars)
        {
            return IterExtractTuples(chars).Select(t => t.Item1).ToList();
        }
    }

    // Helper functions for text extraction
    internal static class TextExtractionHelpers
    {
        internal static List<Dictionary<string, object>> ExtractWords(List<CharDict> chars, Dictionary<string, object> kwargs = null)
        {
            if (kwargs == null) kwargs = new Dictionary<string, object>();
            var extractor = new WordExtractor(
                x_tolerance: kwargs.ContainsKey("x_tolerance") ? Convert.ToSingle(kwargs["x_tolerance"]) : TableFlags.TABLE_DEFAULT_X_TOLERANCE,
                y_tolerance: kwargs.ContainsKey("y_tolerance") ? Convert.ToSingle(kwargs["y_tolerance"]) : TableFlags.TABLE_DEFAULT_Y_TOLERANCE,
                keep_blank_chars: kwargs.ContainsKey("keep_blank_chars") && (bool)kwargs["keep_blank_chars"],
                use_text_flow: kwargs.ContainsKey("use_text_flow") && (bool)kwargs["use_text_flow"],
                horizontal_ltr: !kwargs.ContainsKey("horizontal_ltr") || (bool)kwargs["horizontal_ltr"],
                vertical_ttb: kwargs.ContainsKey("vertical_ttb") && (bool)kwargs["vertical_ttb"],
                split_at_punctuation: kwargs.ContainsKey("split_at_punctuation") && (bool)kwargs["split_at_punctuation"],
                expand_ligatures: !kwargs.ContainsKey("expand_ligatures") || (bool)kwargs["expand_ligatures"]
            );
            return extractor.ExtractWords(chars);
        }

        internal static TextMap CharsToTextmap(List<CharDict> chars, Dictionary<string, object> kwargs = null)
        {
            if (kwargs == null) kwargs = new Dictionary<string, object>();
            kwargs["presorted"] = true;

            var extractor = new WordExtractor(
                x_tolerance: kwargs.ContainsKey("x_tolerance") ? Convert.ToSingle(kwargs["x_tolerance"]) : TableFlags.TABLE_DEFAULT_X_TOLERANCE,
                y_tolerance: kwargs.ContainsKey("y_tolerance") ? Convert.ToSingle(kwargs["y_tolerance"]) : TableFlags.TABLE_DEFAULT_Y_TOLERANCE,
                keep_blank_chars: kwargs.ContainsKey("keep_blank_chars") && (bool)kwargs["keep_blank_chars"],
                use_text_flow: kwargs.ContainsKey("use_text_flow") && (bool)kwargs["use_text_flow"],
                expand_ligatures: !kwargs.ContainsKey("expand_ligatures") || (bool)kwargs["expand_ligatures"]
            );

            var wordmap = extractor.ExtractWordmap(chars);
            return wordmap.ToTextmap(
                layout: kwargs.ContainsKey("layout") && (bool)kwargs["layout"],
                layoutWidth: kwargs.ContainsKey("layout_width") ? Convert.ToSingle(kwargs["layout_width"]) : 0,
                layoutHeight: kwargs.ContainsKey("layout_height") ? Convert.ToSingle(kwargs["layout_height"]) : 0,
                layoutWidthChars: kwargs.ContainsKey("layout_width_chars") ? Convert.ToInt32(kwargs["layout_width_chars"]) : 0,
                layoutHeightChars: kwargs.ContainsKey("layout_height_chars") ? Convert.ToInt32(kwargs["layout_height_chars"]) : 0,
                xDensity: kwargs.ContainsKey("x_density") ? Convert.ToSingle(kwargs["x_density"]) : TableFlags.TABLE_DEFAULT_X_DENSITY,
                yDensity: kwargs.ContainsKey("y_density") ? Convert.ToSingle(kwargs["y_density"]) : TableFlags.TABLE_DEFAULT_Y_DENSITY,
                xShift: kwargs.ContainsKey("x_shift") ? Convert.ToSingle(kwargs["x_shift"]) : 0,
                yShift: kwargs.ContainsKey("y_shift") ? Convert.ToSingle(kwargs["y_shift"]) : 0,
                yTolerance: kwargs.ContainsKey("y_tolerance") ? Convert.ToSingle(kwargs["y_tolerance"]) : TableFlags.TABLE_DEFAULT_Y_TOLERANCE,
                useTextFlow: kwargs.ContainsKey("use_text_flow") && (bool)kwargs["use_text_flow"],
                presorted: kwargs.ContainsKey("presorted") && (bool)kwargs["presorted"],
                expandLigatures: !kwargs.ContainsKey("expand_ligatures") || (bool)kwargs["expand_ligatures"]
            );
        }

        internal static string ExtractText(List<CharDict> chars, Dictionary<string, object> kwargs = null)
        {
            if (kwargs == null) kwargs = new Dictionary<string, object>();
            var charsList = TableHelpers.ToList<CharDict>(chars);
            if (charsList.Count == 0)
                return "";

            if (kwargs.ContainsKey("layout") && (bool)kwargs["layout"])
                return CharsToTextmap(charsList, kwargs).as_string;

            float yTolerance = kwargs.ContainsKey("y_tolerance") ? Convert.ToSingle(kwargs["y_tolerance"]) : TableFlags.TABLE_DEFAULT_Y_TOLERANCE;
            var extractor = new WordExtractor(
                x_tolerance: kwargs.ContainsKey("x_tolerance") ? Convert.ToSingle(kwargs["x_tolerance"]) : TableFlags.TABLE_DEFAULT_X_TOLERANCE,
                y_tolerance: yTolerance,
                keep_blank_chars: kwargs.ContainsKey("keep_blank_chars") && (bool)kwargs["keep_blank_chars"],
                use_text_flow: kwargs.ContainsKey("use_text_flow") && (bool)kwargs["use_text_flow"],
                expand_ligatures: !kwargs.ContainsKey("expand_ligatures") || (bool)kwargs["expand_ligatures"]
            );

            var words = extractor.ExtractWords(charsList);
            if (words.Count == 0)
                return "";

            int rotation = words[0].ContainsKey("rotation") ? Convert.ToInt32(words[0]["rotation"]) : 0;

            if (rotation == 90)
            {
                words = words.OrderBy(w => Convert.ToSingle(w["x1"])).ThenByDescending(w => Convert.ToSingle(w["top"])).ToList();
                return string.Join(" ", words.Select(w => w["text"].ToString()));
            }
            else if (rotation == 270)
            {
                words = words.OrderByDescending(w => Convert.ToSingle(w["x1"])).ThenBy(w => Convert.ToSingle(w["top"])).ToList();
                return string.Join(" ", words.Select(w => w["text"].ToString()));
            }
            else
            {
                var lines = TableHelpers.ClusterObjects(words, w => Convert.ToSingle(w["doctop"]), yTolerance);
                var result = string.Join("\n", lines.Select(line => string.Join(" ", line.Select(w => w["text"].ToString()))));
                if (rotation == 180)
                {
                    var charArray = result.ToCharArray();
                    Array.Reverse(charArray);
                    return new string(charArray.Select(c => c == '\n' ? ' ' : c).ToArray());
                }
                return result;
            }
        }

        internal static string CollateLine(List<CharDict> lineChars, float tolerance = TableFlags.TABLE_DEFAULT_X_TOLERANCE)
        {
            var coll = new StringBuilder();
            float? lastX1 = null;
            foreach (var char_ in lineChars.OrderBy(c => c.x0))
            {
                if (lastX1.HasValue && char_.x0 > (lastX1.Value + tolerance))
                    coll.Append(" ");
                lastX1 = char_.x1;
                coll.Append(char_.text);
            }
            return coll.ToString();
        }

        internal static List<CharDict> DedupeChars(List<CharDict> chars, float tolerance = 1)
        {
            var key = new Func<CharDict, object>(c => new { c.fontname, c.size, c.upright, c.text });
            var posKey = new Func<CharDict, object>(c => new { c.doctop, c.x0 });

            var sortedChars = chars.OrderBy(key).ToList();
            var uniqueChars = new List<CharDict>();

            foreach (var group in sortedChars.GroupBy(key))
            {
                var yClusters = TableHelpers.ClusterObjects(group.ToList(), c => c.doctop, tolerance);
                foreach (var yCluster in yClusters)
                {
                    var xClusters = TableHelpers.ClusterObjects(yCluster, c => c.x0, tolerance);
                    foreach (var xCluster in xClusters)
                    {
                        uniqueChars.Add(xCluster.OrderBy(c => posKey(c)).First());
                    }
                }
            }

            return uniqueChars.OrderBy(c => chars.IndexOf(c)).ToList();
        }
    }


    // Edge processing functions
    internal static class EdgeProcessing
    {
        // line_to_edge - Convert line to edge
        internal static Edge LineToEdge(Dictionary<string, object> line)
        {
            var edge = new Edge
            {
                x0 = Convert.ToSingle(line["x0"]),
                x1 = Convert.ToSingle(line["x1"]),
                top = Convert.ToSingle(line["top"]),
                bottom = Convert.ToSingle(line["bottom"]),
                width = line.ContainsKey("width") ? Convert.ToSingle(line["width"]) : 0,
                height = line.ContainsKey("height") ? Convert.ToSingle(line["height"]) : 0,
                orientation = Convert.ToSingle(line["top"]) == Convert.ToSingle(line["bottom"]) ? "h" : "v",
                object_type = line.ContainsKey("object_type") ? line["object_type"].ToString() : "line",
                doctop = line.ContainsKey("doctop") ? Convert.ToSingle(line["doctop"]) : 0,
                page_number = line.ContainsKey("page_number") ? Convert.ToInt32(line["page_number"]) : 0,
                y0 = line.ContainsKey("y0") ? Convert.ToSingle(line["y0"]) : 0,
                y1 = line.ContainsKey("y1") ? Convert.ToSingle(line["y1"]) : 0
            };
            return edge;
        }

        // rect_to_edges - Convert rectangle to 4 edges
        internal static List<Edge> RectToEdges(Dictionary<string, object> rect)
        {
            var edges = new List<Edge>();
            float x0 = Convert.ToSingle(rect["x0"]);
            float top = Convert.ToSingle(rect["top"]);
            float x1 = Convert.ToSingle(rect["x1"]);
            float bottom = Convert.ToSingle(rect["bottom"]);
            float width = x1 - x0;
            float height = bottom - top;
            float doctop = rect.ContainsKey("doctop") ? Convert.ToSingle(rect["doctop"]) : top;

            // Top edge
            edges.Add(new Edge
            {
                x0 = x0,
                x1 = x1,
                top = bottom,
                bottom = top,
                width = width,
                height = 0,
                orientation = "h",
                object_type = "rect_edge",
                doctop = doctop,
                y0 = bottom,
                y1 = top
            });

            // Bottom edge
            edges.Add(new Edge
            {
                x0 = x0,
                x1 = x1,
                top = top + height,
                bottom = top + height,
                width = width,
                height = 0,
                orientation = "h",
                object_type = "rect_edge",
                doctop = doctop + height,
                y0 = top + height,
                y1 = top + height
            });

            // Left edge
            edges.Add(new Edge
            {
                x0 = x0,
                x1 = x0,
                top = top,
                bottom = bottom,
                width = 0,
                height = height,
                orientation = "v",
                object_type = "rect_edge",
                doctop = doctop,
                y0 = bottom,
                y1 = top
            });

            // Right edge
            edges.Add(new Edge
            {
                x0 = x1,
                x1 = x1,
                top = top,
                bottom = bottom,
                width = 0,
                height = height,
                orientation = "v",
                object_type = "rect_edge",
                doctop = doctop,
                y0 = bottom,
                y1 = top
            });

            return edges;
        }

        // curve_to_edges - Convert curve to edges
        internal static List<Edge> CurveToEdges(Dictionary<string, object> curve)
        {
            var edges = new List<Edge>();
            var pts = curve["pts"] as List<object>;
            if (pts == null) return edges;

            float doctop = curve.ContainsKey("doctop") ? Convert.ToSingle(curve["doctop"]) : 0;
            float top = curve.ContainsKey("top") ? Convert.ToSingle(curve["top"]) : 0;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p0Obj = pts[i] as List<object>;
                var p1Obj = pts[i + 1] as List<object>;
                if (p0Obj == null || p1Obj == null || p0Obj.Count < 2 || p1Obj.Count < 2)
                    continue;

                float p0x = Convert.ToSingle(p0Obj[0]);
                float p0y = Convert.ToSingle(p0Obj[1]);
                float p1x = Convert.ToSingle(p1Obj[0]);
                float p1y = Convert.ToSingle(p1Obj[1]);

                string orientation = null;
                if (p0x == p1x)
                    orientation = "v";
                else if (p0y == p1y)
                    orientation = "h";

                if (orientation == null) continue;

                edges.Add(new Edge
                {
                    x0 = Math.Min(p0x, p1x),
                    x1 = Math.Max(p0x, p1x),
                    top = Math.Min(p0y, p1y),
                    bottom = Math.Max(p0y, p1y),
                    width = Math.Abs(p0x - p1x),
                    height = Math.Abs(p0y - p1y),
                    orientation = orientation,
                    object_type = "curve_edge",
                    doctop = Math.Min(p0y, p1y) + (doctop - top),
                    y0 = Math.Max(p0y, p1y),
                    y1 = Math.Min(p0y, p1y)
                });
            }

            return edges;
        }

        // obj_to_edges - Convert object to edges
        internal static List<Edge> ObjToEdges(Dictionary<string, object> obj)
        {
            string objType = obj.ContainsKey("object_type") ? obj["object_type"].ToString() : "";
            
            if (objType.Contains("_edge"))
                return new List<Edge> { LineToEdge(obj) };
            else if (objType == "line")
                return new List<Edge> { LineToEdge(obj) };
            else if (objType == "rect")
                return RectToEdges(obj);
            else if (objType == "curve")
                return CurveToEdges(obj);
            
            return new List<Edge>();
        }

        // filter_edges - Filter edges by orientation, type, and min length
        internal static List<Edge> FilterEdges(
            List<Edge> edges,
            string orientation = null,
            string edgeType = null,
            float minLength = 1)
        {
            if (orientation != null && orientation != "v" && orientation != "h")
                throw new ArgumentException("Orientation must be 'v' or 'h'");

            return edges.Where(e =>
            {
                string dim = e.orientation == "v" ? "height" : "width";
                float dimValue = e.orientation == "v" ? e.height : e.width;
                bool etCorrect = edgeType == null || e.object_type == edgeType;
                bool orientCorrect = orientation == null || e.orientation == orientation;
                return etCorrect && orientCorrect && dimValue >= minLength;
            }).ToList();
        }

        // snap_objects - Snap objects to their average position
        internal static List<Dictionary<string, object>> SnapObjects(
            IEnumerable<Dictionary<string, object>> objs,
            string attr,
            float tolerance)
        {
            string axis = attr == "x0" || attr == "x1" ? "h" : "v";
            var objsList = objs.ToList();
            var clusters = TableHelpers.ClusterObjects(objsList, obj => Convert.ToSingle(obj[attr]), tolerance);
            var avgs = clusters.Select(cluster => cluster.Average(obj => Convert.ToSingle(obj[attr]))).ToList();
            
            var snappedClusters = new List<List<Dictionary<string, object>>>();
            for (int i = 0; i < clusters.Count; i++)
            {
                float avg = avgs[i];
                var snapped = clusters[i].Select(obj =>
                {
                    var newObj = new Dictionary<string, object>(obj);
                    float oldValue = Convert.ToSingle(obj[attr]);
                    float diff = avg - oldValue;
                    
                    if (axis == "h")
                    {
                        newObj["x0"] = Convert.ToSingle(obj["x0"]) + diff;
                        newObj["x1"] = Convert.ToSingle(obj["x1"]) + diff;
                    }
                    else
                    {
                        newObj["top"] = Convert.ToSingle(obj["top"]) + diff;
                        newObj["bottom"] = Convert.ToSingle(obj["bottom"]) + diff;
                        if (obj.ContainsKey("doctop"))
                            newObj["doctop"] = Convert.ToSingle(obj["doctop"]) + diff;
                        if (obj.ContainsKey("y0"))
                            newObj["y0"] = Convert.ToSingle(obj["y0"]) - diff;
                        if (obj.ContainsKey("y1"))
                            newObj["y1"] = Convert.ToSingle(obj["y1"]) - diff;
                    }
                    return newObj;
                }).ToList();
                snappedClusters.Add(snapped);
            }
            
            return snappedClusters.SelectMany(x => x).ToList();
        }

        // snap_edges - Snap edges within tolerance
        internal static List<Edge> SnapEdges(
            List<Edge> edges,
            float xTolerance = TableFlags.TABLE_DEFAULT_SNAP_TOLERANCE,
            float yTolerance = TableFlags.TABLE_DEFAULT_SNAP_TOLERANCE)
        {
            var byOrientation = new Dictionary<string, List<Edge>>
            {
                { "v", new List<Edge>() },
                { "h", new List<Edge>() }
            };

            foreach (var e in edges)
                byOrientation[e.orientation].Add(e);

            var snappedV = SnapEdgesByOrientation(byOrientation["v"], "x0", xTolerance);
            var snappedH = SnapEdgesByOrientation(byOrientation["h"], "top", yTolerance);
            
            return snappedV.Concat(snappedH).ToList();
        }

        private static List<Edge> SnapEdgesByOrientation(List<Edge> edges, string attr, float tolerance)
        {
            if (edges.Count == 0) return edges;

            var clusters = TableHelpers.ClusterObjects(edges, e => GetEdgeValue(e, attr), tolerance);
            var avgs = clusters.Select(cluster => cluster.Average(e => GetEdgeValue(e, attr))).ToList();

            var result = new List<Edge>();
            for (int i = 0; i < clusters.Count; i++)
            {
                float avg = avgs[i];
                foreach (var e in clusters[i])
                {
                    var snapped = new Edge
                    {
                        x0 = e.x0,
                        x1 = e.x1,
                        top = e.top,
                        bottom = e.bottom,
                        width = e.width,
                        height = e.height,
                        orientation = e.orientation,
                        object_type = e.object_type,
                        doctop = e.doctop,
                        page_number = e.page_number,
                        y0 = e.y0,
                        y1 = e.y1
                    };

                    float diff = avg - GetEdgeValue(e, attr);
                    if (attr == "x0")
                    {
                        snapped.x0 = avg;
                        snapped.x1 = e.x1 + diff;
                        snapped.width = snapped.x1 - snapped.x0;
                    }
                    else if (attr == "top")
                    {
                        snapped.top = avg;
                        snapped.bottom = e.bottom + diff;
                        snapped.height = snapped.bottom - snapped.top;
                        snapped.doctop = e.doctop + diff;
                    }

                    result.Add(snapped);
                }
            }

            return result;
        }

        private static float GetEdgeValue(Edge e, string attr)
        {
            switch (attr)
            {
                case "x0":
                    return e.x0;
                case "top":
                    return e.top;
                default:
                    return 0;
            }
        }

        // resize_object - Resize an object by changing a key value
        internal static Dictionary<string, object> ResizeObject(Dictionary<string, object> obj, string key, float value)
        {
            if (key != "x0" && key != "x1" && key != "top" && key != "bottom")
                throw new ArgumentException("Key must be 'x0', 'x1', 'top', or 'bottom'");

            var newObj = new Dictionary<string, object>(obj);
            float oldValue = Convert.ToSingle(obj[key]);
            float diff = value - oldValue;

            newObj[key] = value;

            if (key == "x0")
            {
                if (value > Convert.ToSingle(obj["x1"]))
                    throw new ArgumentException("x0 must be <= x1");
                newObj["width"] = Convert.ToSingle(obj["x1"]) - value;
            }
            else if (key == "x1")
            {
                if (value < Convert.ToSingle(obj["x0"]))
                    throw new ArgumentException("x1 must be >= x0");
                newObj["width"] = value - Convert.ToSingle(obj["x0"]);
            }
            else if (key == "top")
            {
                if (value > Convert.ToSingle(obj["bottom"]))
                    throw new ArgumentException("top must be <= bottom");
                newObj["doctop"] = Convert.ToSingle(obj["doctop"]) + diff;
                newObj["height"] = Convert.ToSingle(obj["height"]) - diff;
                if (obj.ContainsKey("y1"))
                    newObj["y1"] = Convert.ToSingle(obj["y1"]) - diff;
            }
            else if (key == "bottom")
            {
                if (value < Convert.ToSingle(obj["top"]))
                    throw new ArgumentException("bottom must be >= top");
                newObj["height"] = Convert.ToSingle(obj["height"]) + diff;
                if (obj.ContainsKey("y0"))
                    newObj["y0"] = Convert.ToSingle(obj["y0"]) - diff;
            }

            return newObj;
        }

        // join_edge_group - Join edges along the same line
        internal static List<Edge> JoinEdgeGroup_(
            List<Edge> edges,
            string orientation,
            float tolerance = TableFlags.TABLE_DEFAULT_JOIN_TOLERANCE)
        {
            string minProp, maxProp;
            if (orientation == "h")
            {
                minProp = "x0";
                maxProp = "x1";
            }
            else if (orientation == "v")
            {
                minProp = "top";
                maxProp = "bottom";
            }
            else
            {
                throw new ArgumentException("Orientation must be 'v' or 'h'");
            }

            var sortedEdges = edges.OrderBy(e => GetEdgeValue(e, minProp)).ToList();
            if (sortedEdges.Count == 0) return new List<Edge>();

            var joined = new List<Edge> { sortedEdges[0] };

            for (int i = 1; i < sortedEdges.Count; i++)
            {
                var e = sortedEdges[i];
                var last = joined[joined.Count - 1];

                float eMin = GetEdgeValue(e, minProp);
                float lastMax = GetEdgeValue(last, maxProp);

                if (eMin <= (lastMax + tolerance))
                {
                    float eMax = GetEdgeValue(e, maxProp);
                    if (eMax > lastMax)
                    {
                        // Extend current edge
                        var extended = new Edge
                        {
                            x0 = last.x0,
                            x1 = last.x1,
                            top = last.top,
                            bottom = last.bottom,
                            width = last.width,
                            height = last.height,
                            orientation = last.orientation,
                            object_type = last.object_type,
                            doctop = last.doctop,
                            page_number = last.page_number,
                            y0 = last.y0,
                            y1 = last.y1
                        };

                        if (orientation == "h")
                        {
                            extended.x1 = e.x1;
                            extended.width = extended.x1 - extended.x0;
                        }
                        else
                        {
                            extended.bottom = e.bottom;
                            extended.height = extended.bottom - extended.top;
                        }

                        joined[joined.Count - 1] = extended;
                    }
                }
                else
                {
                    joined.Add(e);
                }
            }

            return joined;
        }

        internal static List<Edge> JoinEdgeGroup(
            List<Edge> edges,
            string orientation,
            float tolerance)
        {
            Func<Edge, float> minProp;
            Func<Edge, float> maxProp;
            Action<Edge, float> setMaxProp;

            // Select properties based on orientation
            if (orientation == "h")
            {
                minProp = e => e.x0;
                maxProp = e => e.x1;
                setMaxProp = (e, v) => e.x1 = v;
            }
            else if (orientation == "v")
            {
                minProp = e => e.top;
                maxProp = e => e.bottom;
                setMaxProp = (e, v) => e.bottom = v;
            }
            else
            {
                throw new ArgumentException("Orientation must be 'h' or 'v'");
            }

            if (edges == null || edges.Count == 0)
                return new List<Edge>();

            // Sort edges by their minimum extent
            var sortedEdges = edges
                .OrderBy(minProp)
                .ToList();

            var joined = new List<Edge> { sortedEdges[0] };

            // Merge overlapping / nearby edges
            for (int i = 1; i < sortedEdges.Count; i++)
            {
                var current = sortedEdges[i];
                var last = joined[joined.Count - 1];

                if (minProp(current) <= maxProp(last) + tolerance)
                {
                    // Extend the last edge if needed
                    if (maxProp(current) > maxProp(last))
                    {
                        setMaxProp(last, maxProp(current));
                    }
                }
                else
                {
                    // Separate edge → start a new segment
                    joined.Add(current);
                }
            }

            return joined;
        }

        // merge_edges - Merge edges using snap and join
        internal static List<Edge> MergeEdges_(
            List<Edge> edges,
            float snapXTolerance,
            float snapYTolerance,
            float joinXTolerance,
            float joinYTolerance)
        {
            if (snapXTolerance > 0 || snapYTolerance > 0)
                edges = SnapEdges(edges, snapXTolerance, snapYTolerance);

            // Use Tuple for grouping key
            var sorted = edges.OrderBy(e => Tuple.Create(e.orientation, e.orientation == "h" ? e.top : e.x0)).ToList();
            var edgeGroups = sorted.GroupBy(e => Tuple.Create(e.orientation, e.orientation == "h" ? e.top : e.x0));

            var merged = new List<Edge>();
            foreach (var group in edgeGroups)
            {
                string orientation = group.Key.Item1; // First element of tuple is orientation
                float tolerance = orientation == "h" ? joinXTolerance : joinYTolerance;
                merged.AddRange(JoinEdgeGroup(group.ToList(), orientation, tolerance));
            }

            return merged;
        }

        public static List<Edge> MergeEdges(
            List<Edge> edges,
            float snapXTolerance,
            float snapYTolerance,
            float joinXTolerance,
            float joinYTolerance)
        {
            // Local grouping key
            (string, float) GetGroupKey(Edge edge)
            {
                return edge.orientation == "h"
                    ? ("h", edge.top)
                    : ("v", edge.x0);
            }

            // Optional snapping
            if (snapXTolerance > 0 || snapYTolerance > 0)
            {
                edges = SnapEdges(edges, snapXTolerance, snapYTolerance);
            }

            // Sort by group key
            var sortedEdges = edges
                .OrderBy(e => GetGroupKey(e).Item1)
                .ThenBy(e => GetGroupKey(e).Item2)
                .ToList();

            // Group edges
            var groupedEdges = sortedEdges
                .GroupBy(GetGroupKey);

            // Join edge groups
            var mergedEdges = new List<Edge>();

            foreach (var group in groupedEdges)
            {
                string orientation = group.Key.Item1;
                float joinTolerance =
                    orientation == "h" ? joinXTolerance : joinYTolerance;

                var joined = JoinEdgeGroup(
                    group.ToList(),
                    orientation,
                    joinTolerance
                );

                mergedEdges.AddRange(joined);
            }

            return mergedEdges;
        }

        // bbox_to_rect - Convert bbox tuple to rect dict
        internal static Dictionary<string, object> BboxToRect(Tuple<float, float, float, float> bbox)
        {
            return new Dictionary<string, object>
            {
                { "x0", bbox.Item1 },
                { "top", bbox.Item2 },
                { "x1", bbox.Item3 },
                { "bottom", bbox.Item4 }
            };
        }

        // objects_to_rect - Get smallest rect containing objects
        internal static Dictionary<string, object> ObjectsToRect(IEnumerable<object> objects)
        {
            var bbox = TableHelpers.ObjectsToBbox(objects);
            return BboxToRect(Tuple.Create(bbox.X0, bbox.Y0, bbox.X1, bbox.Y1));
        }

        // merge_bboxes - Merge multiple bboxes
        internal static Tuple<float, float, float, float> MergeBboxes(IEnumerable<Tuple<float, float, float, float>> bboxes)
        {
            var bboxList = bboxes.ToList();
            if (bboxList.Count == 0)
                return Tuple.Create(0f, 0f, 0f, 0f);

            return Tuple.Create(
                bboxList.Min(b => b.Item1),
                bboxList.Min(b => b.Item2),
                bboxList.Max(b => b.Item3),
                bboxList.Max(b => b.Item4)
            );
        }

        // words_to_edges_h - Find horizontal edges from words
        internal static List<Edge> WordsToEdgesH(
            List<Dictionary<string, object>> words,
            int wordThreshold = (int)TableFlags.TABLE_DEFAULT_MIN_WORDS_HORIZONTAL)
        {
            var byTop = TableHelpers.ClusterObjects(words, w => Convert.ToSingle(w["top"]), 1);
            var largeClusters = byTop.Where(x => x.Count >= wordThreshold).ToList();
            
            if (largeClusters.Count == 0)
                return new List<Edge>();

            var rects = largeClusters.Select(cluster => ObjectsToRect(cluster.Cast<object>())).ToList();
            float minX0 = rects.Min(r => Convert.ToSingle(r["x0"]));
            float maxX1 = rects.Max(r => Convert.ToSingle(r["x1"]));

            var edges = new List<Edge>();
            foreach (var r in rects)
            {
                float top = Convert.ToSingle(r["top"]);
                float bottom = Convert.ToSingle(r["bottom"]);

                // Top edge
                edges.Add(new Edge
                {
                    x0 = minX0,
                    x1 = maxX1,
                    top = top,
                    bottom = top,
                    width = maxX1 - minX0,
                    height = 0,
                    orientation = "h",
                    object_type = "text_edge"
                });

                // Bottom edge
                edges.Add(new Edge
                {
                    x0 = minX0,
                    x1 = maxX1,
                    top = bottom,
                    bottom = bottom,
                    width = maxX1 - minX0,
                    height = 0,
                    orientation = "h",
                    object_type = "text_edge"
                });
            }

            return edges;
        }

        // get_bbox_overlap - Get overlap between two bboxes
        internal static Tuple<float, float, float, float> GetBboxOverlap(
            Tuple<float, float, float, float> a,
            Tuple<float, float, float, float> b)
        {
            float oLeft = Math.Max(a.Item1, b.Item1);
            float oRight = Math.Min(a.Item3, b.Item3);
            float oBottom = Math.Min(a.Item4, b.Item4);
            float oTop = Math.Max(a.Item2, b.Item2);
            float oWidth = oRight - oLeft;
            float oHeight = oBottom - oTop;

            if (oHeight >= 0 && oWidth >= 0 && oHeight + oWidth > 0)
                return Tuple.Create(oLeft, oTop, oRight, oBottom);
            
            return null;
        }

        // words_to_edges_v - Find vertical edges from words
        internal static List<Edge> WordsToEdgesV(
            List<Dictionary<string, object>> words,
            int wordThreshold = (int)TableFlags.TABLE_DEFAULT_MIN_WORDS_VERTICAL)
        {
            var byX0 = TableHelpers.ClusterObjects(words, w => Convert.ToSingle(w["x0"]), 1);
            var byX1 = TableHelpers.ClusterObjects(words, w => Convert.ToSingle(w["x1"]), 1);
            
            Func<Dictionary<string, object>, float> getCenter = w => 
                (Convert.ToSingle(w["x0"]) + Convert.ToSingle(w["x1"])) / 2;
            var byCenter = TableHelpers.ClusterObjects(words, getCenter, 1);

            var clusters = byX0.Concat(byX1).Concat(byCenter).ToList();
            var sortedClusters = clusters.OrderByDescending(x => x.Count).ToList();
            var largeClusters = sortedClusters.Where(x => x.Count >= wordThreshold).ToList();

            if (largeClusters.Count == 0)
                return new List<Edge>();

            var bboxes = largeClusters.Select(cluster => 
            {
                var rect = ObjectsToRect(cluster.Cast<object>());
                return Tuple.Create(
                    Convert.ToSingle(rect["x0"]),
                    Convert.ToSingle(rect["top"]),
                    Convert.ToSingle(rect["x1"]),
                    Convert.ToSingle(rect["bottom"])
                );
            }).ToList();

            var condensedBboxes = new List<Tuple<float, float, float, float>>();
            foreach (var bbox in bboxes)
            {
                bool hasOverlap = condensedBboxes.Any(c => GetBboxOverlap(bbox, c) != null);
                if (!hasOverlap)
                    condensedBboxes.Add(bbox);
            }

            if (condensedBboxes.Count == 0)
                return new List<Edge>();

            var condensedRects = condensedBboxes.Select(bbox => BboxToRect(bbox))
                .OrderBy(r => Convert.ToSingle(r["x0"])).ToList();

            float maxX1 = condensedRects.Max(r => Convert.ToSingle(r["x1"]));
            float minTop = condensedRects.Min(r => Convert.ToSingle(r["top"]));
            float maxBottom = condensedRects.Max(r => Convert.ToSingle(r["bottom"]));

            var edges = new List<Edge>();
            foreach (var r in condensedRects)
            {
                edges.Add(new Edge
                {
                    x0 = Convert.ToSingle(r["x0"]),
                    x1 = Convert.ToSingle(r["x0"]),
                    top = minTop,
                    bottom = maxBottom,
                    width = 0,
                    height = maxBottom - minTop,
                    orientation = "v",
                    object_type = "text_edge"
                });
            }

            // Add rightmost edge
            edges.Add(new Edge
            {
                x0 = maxX1,
                x1 = maxX1,
                top = minTop,
                bottom = maxBottom,
                width = 0,
                height = maxBottom - minTop,
                orientation = "v",
                object_type = "text_edge"
            });

            return edges;
        }

        // edges_to_intersections - Find intersection points of edges
        internal static Dictionary<Tuple<float, float>, Dictionary<string, List<Edge>>> EdgesToIntersections(
            List<Edge> edges,
            float xTolerance = 1,
            float yTolerance = 1)
        {
            var intersections = new Dictionary<Tuple<float, float>, Dictionary<string, List<Edge>>>();
            var vEdges = edges.Where(e => e.orientation == "v")
                .OrderBy(e => e.x0).ThenBy(e => e.top).ToList();
            var hEdges = edges.Where(e => e.orientation == "h")
                .OrderBy(e => e.top).ThenBy(e => e.x0).ToList();

            foreach (var v in vEdges)
            {
                foreach (var h in hEdges)
                {
                    if ((v.top <= (h.top + yTolerance)) &&
                        (v.bottom >= (h.top - yTolerance)) &&
                        (v.x0 >= (h.x0 - xTolerance)) &&
                        (v.x0 <= (h.x1 + xTolerance)))
                    {
                        var vertex = Tuple.Create(v.x0, h.top);
                        if (!intersections.ContainsKey(vertex))
                        {
                            intersections[vertex] = new Dictionary<string, List<Edge>>
                            {
                                { "v", new List<Edge>() },
                                { "h", new List<Edge>() }
                            };
                        }
                        intersections[vertex]["v"].Add(v);
                        intersections[vertex]["h"].Add(h);
                    }
                }
            }

            return intersections;
        }

        // intersections_to_cells - Convert intersections to cells
        internal static List<Rect> IntersectionsToCells_(
            Dictionary<Tuple<float, float>, Dictionary<string, List<Edge>>> intersections)
        {
            var cells = new List<Rect>();
            var points = intersections.Keys.OrderBy(p => p.Item2).ThenBy(p => p.Item1).ToList();
            int nPoints = points.Count;

            Func<Tuple<float, float>, Tuple<float, float>, bool> edgeConnects = (p1, p2) =>
            {
                Func<List<Edge>, HashSet<Tuple<float, float, float, float>>> edgesToSet = edges =>
                {
                    return new HashSet<Tuple<float, float, float, float>>(edges.Select(e =>
                        Tuple.Create(e.x0, e.top, e.x1, e.bottom)));
                };

                if (p1.Item1 == p2.Item1) // Same x
                {
                    var common = new HashSet<Tuple<float, float, float, float>>(edgesToSet(intersections[p1]["v"]));
                    common.IntersectWith(edgesToSet(intersections[p2]["v"]));
                    if (common.Count > 0)
                        return true;
                }

                if (p1.Item2 == p2.Item2) // Same y
                {
                    var common = new HashSet<Tuple<float, float, float, float>>(edgesToSet(intersections[p1]["h"]));
                    common.IntersectWith(edgesToSet(intersections[p2]["h"]));
                    if (common.Count > 0)
                        return true;
                }

                return false;
            };

            for (int i = 0; i < nPoints - 1; i++)
            {
                var pt = points[i];
                var rest = points.Skip(i + 1).ToList();

                var below = rest.Where(x => x.Item1 == pt.Item1).ToList();
                var right = rest.Where(x => x.Item2 == pt.Item2).ToList();

                foreach (var belowPt in below)
                {
                    if (!edgeConnects(pt, belowPt))
                        continue;

                    foreach (var rightPt in right)
                    {
                        if (!edgeConnects(pt, rightPt))
                            continue;

                        var bottomRight = Tuple.Create(rightPt.Item1, belowPt.Item2);

                        if (intersections.ContainsKey(bottomRight) &&
                            edgeConnects(bottomRight, rightPt) &&
                            edgeConnects(bottomRight, belowPt))
                        {
                            cells.Add(new Rect(pt.Item1, pt.Item2, bottomRight.Item1, bottomRight.Item2));
                        }
                    }
                }
            }

            return cells;
        }

        internal static List<Rect> IntersectionsToCells(
            Dictionary<Tuple<float, float>, Dictionary<string, List<Edge>>> intersections)
        {
            // ---------- edge_connects ----------
            bool EdgeConnects(
                Tuple<float, float> p1,
                Tuple<float, float> p2)
            {
                HashSet<(float, float, float, float)> EdgesToSet(List<Edge> edges)
                {
                    var set = new HashSet<(float, float, float, float)>();
                    foreach (var e in edges)
                        set.Add(ObjToBBox(e));
                    return set;
                }

                // Same X → vertical edges
                if (p1.Item1 == p2.Item1)
                {
                    var common = EdgesToSet(intersections[p1]["v"])
                        .Intersect(EdgesToSet(intersections[p2]["v"]));

                    if (common.Any())
                        return true;
                }

                // Same Y → horizontal edges
                if (p1.Item2 == p2.Item2)
                {
                    var common = EdgesToSet(intersections[p1]["h"])
                        .Intersect(EdgesToSet(intersections[p2]["h"]));

                    if (common.Any())
                        return true;
                }

                return false;
            }

            var points = intersections.Keys
                .OrderBy(p => p.Item1)
                .ThenBy(p => p.Item2)
                .ToList();

            int nPoints = points.Count;

            // ---------- find_smallest_cell ----------
            Rect FindSmallestCell(int i)
            {
                if (i == nPoints - 1)
                    return null;

                var pt = points[i];
                var rest = points.Skip(i + 1);

                var below = rest.Where(p => p.Item1 == pt.Item1).ToList();
                var right = rest.Where(p => p.Item2 == pt.Item2).ToList();

                foreach (var belowPt in below)
                {
                    if (!EdgeConnects(pt, belowPt))
                        continue;

                    foreach (var rightPt in right)
                    {
                        if (!EdgeConnects(pt, rightPt))
                            continue;

                        var bottomRight = Tuple.Create(rightPt.Item1, belowPt.Item2);

                        if (intersections.ContainsKey(bottomRight) &&
                            EdgeConnects(bottomRight, rightPt) &&
                            EdgeConnects(bottomRight, belowPt))
                        {
                            float x0 = pt.Item1;
                            float y0 = pt.Item2;
                            float x1 = bottomRight.Item1;
                            float y1 = bottomRight.Item2;

                            return new Rect(
                                x0,
                                y0,
                                x1,
                                y1
                            );
                        }
                    }
                }

                return null;
            }

            // ---------- generate cells ----------
            var cells = new List<Rect>();

            for (int i = 0; i < points.Count; i++)
            {
                var cell = FindSmallestCell(i);
                if (cell != null)
                    cells.Add(cell);
            }

            return cells;
        }

        // ---------- obj_to_bbox ----------
        private static (float, float, float, float) ObjToBBox(Edge e)
        {
            return (e.x0, e.top, e.x1, e.bottom);
        }

        // cells_to_tables - Group cells into tables
        internal static List<List<Rect>> CellsToTables(Page page, List<Rect> cells)
        {
            Func<Rect, List<Tuple<float, float>>> bboxToCorners = bbox =>
            {
                return new List<Tuple<float, float>>
                {
                    Tuple.Create(bbox.X0, bbox.Y0),
                    Tuple.Create(bbox.X0, bbox.Y1),
                    Tuple.Create(bbox.X1, bbox.Y0),
                    Tuple.Create(bbox.X1, bbox.Y1)
                };
            };

            var remainingCells = new List<Rect>(cells);
            var currentCorners = new HashSet<Tuple<float, float>>();
            var currentCells = new List<Rect>();
            var tables = new List<List<Rect>>();

            while (remainingCells.Count > 0)
            {
                int initialCellCount = currentCells.Count;
                var cellsToRemove = new List<Rect>();

                foreach (var cell in remainingCells)
                {
                    var cellCorners = bboxToCorners(cell);

                    if (currentCells.Count == 0)
                    {
                        foreach (var corner in cellCorners)
                            currentCorners.Add(corner);
                        currentCells.Add(cell);
                        cellsToRemove.Add(cell);
                    }
                    else
                    {
                        int cornerCount = cellCorners.Count(c => currentCorners.Contains(c));
                        if (cornerCount > 0)
                        {
                            foreach (var corner in cellCorners)
                                currentCorners.Add(corner);
                            currentCells.Add(cell);
                            cellsToRemove.Add(cell);
                        }
                    }
                }

                foreach (var cell in cellsToRemove)
                    remainingCells.Remove(cell);

                if (currentCells.Count == initialCellCount)
                {
                    tables.Add(new List<Rect>(currentCells));
                    currentCorners.Clear();
                    currentCells.Clear();
                }
            }

            if (currentCells.Count > 0)
                tables.Add(currentCells);

            // MuPDF modification: Remove tables without text or having only 1 column
            for (int i = tables.Count - 1; i >= 0; i--)
            {
                var table = tables[i];
                var r = new Rect(0, 0, 0, 0);
                var x1Vals = new HashSet<float>();
                var x0Vals = new HashSet<float>();

                foreach (var c in table)
                {
                    r = r | c;
                    x1Vals.Add(c.X1);
                    x0Vals.Add(c.X0);
                }

                if (x1Vals.Count < 2 || x0Vals.Count < 2)
                {
                    tables.RemoveAt(i);
                    continue;
                }

                // Check if table has only whitespace
                try
                {
                    var textpage = TableGlobals.TEXTPAGE ?? page.GetTextPage();
                    string text = textpage.ExtractTextBox(r.ToFzRect());
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        tables.RemoveAt(i);
                        continue;
                    }
                }
                catch
                {
                    // If text extraction fails, keep the table
                }
            }

            // Sort tables top-to-bottom-left-to-right
            tables = tables.OrderBy(t => t.Min(c => Tuple.Create(c.Y0, c.X0))).ToList();

            return tables;
        }
    }

    // CellGroup base class
    public class CellGroup
    {
        public List<Rect> cells { get; set; }
        public Rect bbox { get; set; }

        public CellGroup(List<Rect> cells)
        {
            this.cells = cells;
            if (cells != null && cells.Count > 0)
            {
                var validCells = cells.Where(c => c != null).ToList();
                if (validCells.Count > 0)
                {
                    bbox = new Rect(
                        validCells.Min(c => c.X0),
                        validCells.Min(c => c.Y0),
                        validCells.Max(c => c.X1),
                        validCells.Max(c => c.Y1)
                    );
                }
            }
        }
    }

    // TableRow class
    public class TableRow : CellGroup
    {
        public TableRow(List<Rect> cells) : base(cells)
        {
        }
    }

    // TableHeader class
    public class TableHeader
    {
        public Rect bbox { get; set; }
        public List<Rect> cells { get; set; }
        public List<string> names { get; set; }
        public bool external { get; set; }

        public TableHeader(Rect bbox, List<Rect> cells, List<string> names, bool external)
        {
            this.bbox = bbox;
            this.cells = cells;
            this.names = names;
            this.external = external;
        }
    }

    // Table class
    public class Table
    {
        public Page page { get; set; }
        public TextPage textpage { get; set; }
        public List<Rect> cells { get; set; }
        public TableHeader header { get; set; }

        public Table(Page page, List<Rect> cells)
        {
            this.page = page;
            this.cells = cells;
            this.textpage = null;
            this.header = GetHeader();
        }

        public Rect bbox
        {
            get
            {
                if (cells == null || cells.Count == 0)
                    return null;
                return new Rect(
                    cells.Min(c => c.X0),
                    cells.Min(c => c.Y0),
                    cells.Max(c => c.X1),
                    cells.Max(c => c.Y1)
                );
            }
        }

        public List<TableRow> rows
        {
            get
            {
                var sorted = cells.OrderBy(c => c.Y0).ThenBy(c => c.X0).ToList();
                var xs = cells.Select(c => c.X0).Distinct().OrderBy(x => x).ToList();
                var rows = new List<TableRow>();

                foreach (var group in sorted.GroupBy(c => c.Y0))
                {
                    var rowCells = group.OrderBy(c => c.X0).ToList();
                    var xdict = rowCells.ToDictionary(c => c.X0, c => c);
                    var row = new TableRow(xs.Select(x => xdict.ContainsKey(x) ? xdict[x] : null).ToList());
                    rows.Add(row);
                }

                return rows;
            }
        }

        public int row_count
        {
            get { return rows.Count; }
        }

        public int col_count
        {
            get { return rows.Count > 0 ? rows.Max(r => r.cells.Count) : 0; }
        }

        public List<List<string>> Extract(Dictionary<string, object> kwargs = null)
        {
            if (kwargs == null)
                kwargs = new Dictionary<string, object>();

            var chars = TableGlobals.CHARS;
            var tableArr = new List<List<string>>();

            bool CharInBbox(CharDict char_, Rect bbox)
            {
                float v_mid = (char_.top + char_.bottom) / 2;
                float h_mid = (char_.x0 + char_.x1) / 2;
                return h_mid >= bbox.X0 && h_mid < bbox.X1 && v_mid >= bbox.Y0 && v_mid < bbox.Y1;
            }

            foreach (var row in rows)
            {
                var arr = new List<string>();
                var rowChars = chars.Where(c => CharInBbox(c, row.bbox)).ToList();

                foreach (var cell in row.cells)
                {
                    if (cell == null)
                    {
                        arr.Add(null);
                    }
                    else
                    {
                        var cellChars = rowChars.Where(c => CharInBbox(c, cell)).ToList();
                        if (cellChars.Count > 0)
                        {
                            var cellKwargs = new Dictionary<string, object>(kwargs);
                            cellKwargs["x_shift"] = cell.X0;
                            cellKwargs["y_shift"] = cell.Y0;
                            if (cellKwargs.ContainsKey("layout"))
                            {
                                cellKwargs["layout_width"] = cell.X1 - cell.X0;
                                cellKwargs["layout_height"] = cell.Y1 - cell.Y0;
                            }
                            var cellText = ExtractText(cellChars, cellKwargs);
                            arr.Add(cellText);
                        }
                        else
                        {
                            arr.Add("");
                        }
                    }
                }
                tableArr.Add(arr);
            }

            return tableArr;
        }

        private string ExtractText(List<CharDict> chars, Dictionary<string, object> kwargs)
        {
            return TextExtractionHelpers.ExtractText(chars, kwargs);
        }

        public string ToMarkdown(bool clean = false, bool fillEmpty = true)
        {
            var output = new StringBuilder();
            output.Append("|");
            int rows = row_count;
            int cols = col_count;

            // cell coordinates
            var cellBoxes = this.rows.Select(r => r.cells.ToList()).ToList();

            // cell text strings
            var cells = new List<List<string>>();
            for (int i = 0; i < rows; i++)
            {
                cells.Add(new List<string>());
                for (int colIdx = 0; colIdx < cols; colIdx++)
                {
                    cells[i].Add(null);
                }
            }

            for (int i = 0; i < cellBoxes.Count; i++)
            {
                for (int colIdx = 0; colIdx < cellBoxes[i].Count && colIdx < cols; colIdx++)
                {
                    if (cellBoxes[i][colIdx] != null)
                    {
                        cells[i][colIdx] = TableHelpers.ExtractCells(textpage, cellBoxes[i][colIdx], markdown: true);
                    }
                }
            }

            if (fillEmpty)
            {
                // for rows, copy content from left to right
                for (int rowIdx = 0; rowIdx < rows; rowIdx++)
                {
                    for (int i = 0; i < cols - 1; i++)
                    {
                        if (cells[rowIdx][i + 1] == null)
                        {
                            cells[rowIdx][i + 1] = cells[rowIdx][i];
                        }
                    }
                }

                // for columns, copy top to bottom
                for (int i = 0; i < cols; i++)
                {
                    for (int rowIdx = 0; rowIdx < rows - 1; rowIdx++)
                    {
                        if (cells[rowIdx + 1][i] == null)
                        {
                            cells[rowIdx + 1][i] = cells[rowIdx][i];
                        }
                    }
                }
            }

            // generate header string and MD separator
            for (int i = 0; i < header.names.Count; i++)
            {
                string name = header.names[i];
                if (string.IsNullOrEmpty(name))
                {
                    name = $"Col{i + 1}";
                }
                name = name.Replace("\n", "<br>");
                if (clean)
                {
                    name = System.Security.SecurityElement.Escape(name.Replace("-", "&#45;"));
                }
                output.Append(name + "|");
            }
            output.Append("\n");
            // insert GitHub header line separator
            output.Append("|" + string.Join("|", Enumerable.Range(0, col_count).Select(_ => "---")) + "|\n");

            // skip first row in details if header is part of the table
            int startRow = header.external ? 0 : 1;

            // iterate over detail rows
            for (int i = startRow; i < rows; i++)
            {
                output.Append("|");
                for (int k = 0; k < cols; k++)
                {
                    string cell = cells[i][k];
                    if (cell == null)
                        cell = "";
                    if (clean)
                    {
                        cell = System.Security.SecurityElement.Escape(cell.Replace("-", "&#45;"));
                    }
                    output.Append(cell + "|");
                }
                output.Append("\n");
            }
            return output.ToString() + "\n";
        }

        // to_pandas - Return a pandas DataFrame version of the table
        // Note: This would require the pandas.NET library or similar
        // For C#, users can convert the Extract() result to their preferred data structure
        public object ToPandas(Dictionary<string, object> kwargs = null)
        {
            // In C#: Could return DataTable, or users can use Extract() and convert manually
            throw new NotImplementedException("ToPandas is not implemented in C#. Use Extract() and convert to your preferred data structure (e.g., DataTable).");
        }

        private string ExtractCells(TextPage textpage, Rect cell, bool markdown = false)
        {
            return TableHelpers.ExtractCells(textpage, cell, markdown);
        }

        private TableHeader GetHeader(float yTolerance = 3.0f)
        {
            float yDelta = yTolerance;

            // Helper function: Check if top row has different background color
            bool TopRowBgColor()
            {
                try
                {
                    var bbox0 = rows[0].bbox;
                    var bboxt = new Rect(bbox0.X0, bbox0.Y0 - bbox0.Height, bbox0.X1, bbox0.Y0);
                    var (_, topColor0) = page.GetPixmap(clip: bbox0).ColorTopUsage();
                    var (_, topColort) = page.GetPixmap(clip: bboxt).ColorTopUsage();
                    return !topColor0.SequenceEqual(topColort);
                }
                catch
                {
                    return false;
                }
            }

            // Helper function: Check if row contains bold text
            bool RowHasBold(Rect rowBbox)
            {
                return TableGlobals.CHARS.Any(c =>
                    TableHelpers.RectInRect(new Rect(c.x0, c.y0, c.x1, c.y1), rowBbox) && c.bold);
            }

            if (rows == null || rows.Count == 0)
                return null;

            var row = rows[0];
            var cells = row.cells;
            var bbox = row.bbox;

            // Return this if we determine that the top row is the header
            var extractResult = Extract();
            var headerTopRow = new TableHeader(
                bbox,
                cells,
                extractResult.Count > 0 ? extractResult[0] : new List<string>(),
                false
            );

            // 1-line tables have no extra header
            if (rows.Count < 2)
                return headerTopRow;

            // 1-column tables have no extra header
            if (cells.Count < 2)
                return headerTopRow;

            // Assume top row is the header if second row is empty
            var row2 = rows[1];
            if (row2.cells.All(c => c == null))
                return headerTopRow;

            // Special check: is top row bold?
            bool topRowBold = RowHasBold(bbox);

            // Assume top row is header if it is bold and any cell of 2nd row is non-bold
            if (topRowBold && !RowHasBold(row2.bbox))
                return headerTopRow;

            if (TopRowBgColor())
                return headerTopRow;

            // Column coordinates (x1 values) in top row
            var colX = cells.Take(cells.Count - 1).Select(c => c != null ? c.X1 : (float?)null).ToList();

            // Clip = page area above the table
            var clip = new Rect(bbox.X0, 0, bbox.X1, bbox.Y0);

            // Get text blocks above table
            dynamic pageInfo = page.GetText("dict", clip: clip, flags: (int)TextFlagsExtension.TEXTFLAGS_TEXT);
            List<Block> blocks = pageInfo?.BLOCKS ?? new List<Block>();

            // Non-empty, non-superscript spans above table, sorted descending by y1
            var spans = new List<Dictionary<string, object>>();
            foreach (var block in blocks)
            {
                if (block.Lines == null) continue;
                foreach (var line in block.Lines)
                {
                    if (line.Spans == null) continue;
                    foreach (var span in line.Spans)
                    {
                        if (span.Bbox == null) continue;
                        string text = span.Text ?? "";
                        bool isWhitespace = text.All(c => TableGlobals.WHITE_SPACES.Contains(c));
                        bool isSuperscript = ((int)span.Flags & (int)FontStyle.TEXT_FONT_SUPERSCRIPT) != 0;
                        
                        if (!isWhitespace && !isSuperscript)
                        {
                            spans.Add(new Dictionary<string, object>
                            {
                                { "text", text },
                                { "bbox", new List<object> { span.Bbox.X0, span.Bbox.Y0, span.Bbox.X1, span.Bbox.Y1 } },
                                { "flags", span.Flags }
                            });
                        }
                    }
                }
            }

            spans = spans.OrderByDescending(s => ((List<object>)s["bbox"])[3]).ToList();

            var select = new List<float>();
            var lineHeights = new List<float>();
            var lineBolds = new List<bool>();

            // Walk through spans and fill the 3 lists
            for (int i = 0; i < spans.Count; i++)
            {
                var s = spans[i];
                var sbbox = s["bbox"] as List<object>;
                if (sbbox == null || sbbox.Count < 4) continue;

                float y1 = Convert.ToSingle(sbbox[3]);
                float h = y1 - Convert.ToSingle(sbbox[1]);
                bool bold = ((int)s["flags"] & (int)FontStyle.TEXT_FONT_BOLD) != 0;

                if (i == 0)
                {
                    select.Add(y1);
                    lineHeights.Add(h);
                    lineBolds.Add(bold);
                    continue;
                }

                float y0 = select[select.Count - 1];
                float h0 = lineHeights[lineHeights.Count - 1];
                bool bold0 = lineBolds[lineBolds.Count - 1];

                if (bold0 && !bold)
                    break;

                if (y0 - y1 <= yDelta || Math.Abs((y0 - h0) - Convert.ToSingle(sbbox[1])) <= yDelta)
                {
                    sbbox[1] = y0 - h0;
                    sbbox[3] = y0;
                    s["bbox"] = sbbox;
                    spans[i] = s;
                    if (bold)
                        lineBolds[lineBolds.Count - 1] = bold;
                    continue;
                }
                else if (y0 - y1 > 1.5 * h0)
                {
                    break;
                }

                select.Add(y1);
                lineHeights.Add(h);
                lineBolds.Add(bold);
            }

            if (select.Count == 0)
                return headerTopRow;

            select = select.Take(5).ToList();

            // Assume top row as header if text above is too far away
            if (bbox.Y0 - select[0] >= lineHeights[0])
                return headerTopRow;

            // Accept top row as header if bold, but line above is not
            if (topRowBold && !lineBolds[0])
                return headerTopRow;

            if (spans.Count == 0)
                return headerTopRow;

            // Re-compute clip above table
            var nclip = new Rect(0, 0, 0, 0);
            foreach (var s in spans.Where(s => Convert.ToSingle(((List<object>)s["bbox"])[3]) >= select[select.Count - 1]))
            {
                var sbbox = s["bbox"] as List<object>;
                if (sbbox != null && sbbox.Count >= 4)
                {
                    var srect = new Rect(
                        Convert.ToSingle(sbbox[0]),
                        Convert.ToSingle(sbbox[1]),
                        Convert.ToSingle(sbbox[2]),
                        Convert.ToSingle(sbbox[3])
                    );
                    nclip = nclip | srect;
                }
            }

            if (!nclip.IsEmpty)
                clip = nclip;

            clip.Y1 = bbox.Y0;

            // Confirm that no word in clip is intersecting a column separator
            // Get words from textpage or page
            var textpageForWords = page.GetTextPage(clip: clip);
            var words = textpageForWords.ExtractWords();
            var wordRects = words.Select(w => new Rect(w.X0, w.Y0, w.X1, w.Y1)).ToList();
            var wordTops = wordRects.Select(r => r.Y0).Distinct().OrderByDescending(y => y).ToList();

            select.Clear();

            // Exclude lines with words that intersect a column border
            foreach (var top in wordTops)
            {
                bool hasIntersecting = colX.Any(x =>
                    x.HasValue && wordRects.Any(r => r.Y0 == top && r.X0 < x.Value && r.X1 > x.Value));

                if (!hasIntersecting)
                {
                    select.Add(top);
                }
                else
                {
                    break;
                }
            }

            if (select.Count == 0)
                return headerTopRow;

            var hdrBbox = new Rect(clip.X0, select[select.Count - 1], clip.X1, clip.Y1);
            hdrBbox.X0 = this.bbox.X0;
            hdrBbox.X1 = this.bbox.X1;

            var hdrCells = cells.Select(c =>
                c != null ? new Rect(c.X0, hdrBbox.Y0, c.X1, hdrBbox.Y1) : (Rect)null
            ).ToList();

            // Column names: no line breaks, no excess spaces
            var hdrNames = hdrCells.Select(c =>
            {
                if (c == null) return "";
                try
                {
                    return page.GetTextbox(c).Replace("\n", " ").Replace("  ", " ").Trim();
                }
                catch
                {
                    return "";
                }
            }).ToList();

            return new TableHeader(hdrBbox, hdrCells, hdrNames, true);
        }
    }

    // TableSettings class
    public class TableSettings
    {
        public string vertical_strategy { get; set; } = "lines";
        public string horizontal_strategy { get; set; } = "lines";
        public List<object> explicit_vertical_lines { get; set; } = null;
        public List<object> explicit_horizontal_lines { get; set; } = null;
        public float snap_tolerance { get; set; } = TableFlags.TABLE_DEFAULT_SNAP_TOLERANCE;
        public float snap_x_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float snap_y_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float join_tolerance { get; set; } = TableFlags.TABLE_DEFAULT_JOIN_TOLERANCE;
        public float join_x_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float join_y_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float edge_min_length { get; set; } = 3.0f;
        public float min_words_vertical { get; set; } = TableFlags.TABLE_DEFAULT_MIN_WORDS_VERTICAL;
        public float min_words_horizontal { get; set; } = TableFlags.TABLE_DEFAULT_MIN_WORDS_HORIZONTAL;
        public float intersection_tolerance { get; set; } = 3.0f;
        public float intersection_x_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float intersection_y_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public Dictionary<string, object> text_settings { get; set; } = null;

        public TableSettings PostInit()
        {
            // Validate non-negative settings
            var nonNegativeSettings = new[]
            {
                "snap_tolerance", "snap_x_tolerance", "snap_y_tolerance",
                "join_tolerance", "join_x_tolerance", "join_y_tolerance",
                "edge_min_length", "min_words_vertical", "min_words_horizontal",
                "intersection_tolerance", "intersection_x_tolerance", "intersection_y_tolerance"
            };

            foreach (var setting in nonNegativeSettings)
            {
                var value = (float)GetType().GetProperty(setting).GetValue(this);
                if (value < 0)
                {
                    throw new ArgumentException($"Table setting '{setting}' cannot be negative");
                }
            }

            // Validate strategies
            if (!TableFlags.TABLE_STRATEGIES.Contains(vertical_strategy))
            {
                throw new ArgumentException($"vertical_strategy must be one of {{{string.Join(",", TableFlags.TABLE_STRATEGIES)}}}");
            }

            if (!TableFlags.TABLE_STRATEGIES.Contains(horizontal_strategy))
            {
                throw new ArgumentException($"horizontal_strategy must be one of {{{string.Join(",", TableFlags.TABLE_STRATEGIES)}}}");
            }

            if (text_settings == null)
            {
                text_settings = new Dictionary<string, object>();
            }

            // Set defaults for unset tolerances
            if (snap_x_tolerance == TableFlags.TABLE_UNSET)
                snap_x_tolerance = snap_tolerance;
            if (snap_y_tolerance == TableFlags.TABLE_UNSET)
                snap_y_tolerance = snap_tolerance;
            if (join_x_tolerance == TableFlags.TABLE_UNSET)
                join_x_tolerance = join_tolerance;
            if (join_y_tolerance == TableFlags.TABLE_UNSET)
                join_y_tolerance = join_tolerance;
            if (intersection_x_tolerance == TableFlags.TABLE_UNSET)
                intersection_x_tolerance = intersection_tolerance;
            if (intersection_y_tolerance == TableFlags.TABLE_UNSET)
                intersection_y_tolerance = intersection_tolerance;

            return this;
        }

        public static TableSettings Resolve(object settings = null)
        {
            if (settings == null)
            {
                return new TableSettings().PostInit();
            }

            if (settings is TableSettings ts)
            {
                return ts.PostInit();
            }

            if (settings is Dictionary<string, object> dict)
            {
                var coreSettings = new Dictionary<string, object>();
                var textSettings = new Dictionary<string, object>();

                foreach (var kvp in dict)
                {
                    if (kvp.Key.StartsWith("text_"))
                    {
                        textSettings[kvp.Key.Substring(5)] = kvp.Value;
                    }
                    else
                    {
                        coreSettings[kvp.Key] = kvp.Value;
                    }
                }

                coreSettings["text_settings"] = textSettings;

                var tableSettings = new TableSettings();
                foreach (var kvp in coreSettings)
                {
                    var prop = typeof(TableSettings).GetProperty(kvp.Key);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(tableSettings, kvp.Value);
                    }
                }

                return tableSettings.PostInit();
            }

            throw new ArgumentException($"Cannot resolve settings: {settings}");
        }
    }

    // FindTables function - C# port of find_tables from table.py
    public static class TableFinderHelper
    {
        /// <summary>
        /// Find tables on a page and return a TableFinder object.
        /// This is the C# port of the find_tables function from table.py.
        /// </summary>
        public static TableFinder FindTables(
            Page page,
            Rect clip = null,
            string vertical_strategy = "lines",
            string horizontal_strategy = "lines",
            List<object> vertical_lines = null,
            List<object> horizontal_lines = null,
            float snap_tolerance = TableFlags.TABLE_DEFAULT_SNAP_TOLERANCE,
            float? snap_x_tolerance = null,
            float? snap_y_tolerance = null,
            float join_tolerance = TableFlags.TABLE_DEFAULT_JOIN_TOLERANCE,
            float? join_x_tolerance = null,
            float? join_y_tolerance = null,
            float edge_min_length = 3.0f,
            float min_words_vertical = TableFlags.TABLE_DEFAULT_MIN_WORDS_VERTICAL,
            float min_words_horizontal = TableFlags.TABLE_DEFAULT_MIN_WORDS_HORIZONTAL,
            float intersection_tolerance = 3.0f,
            float? intersection_x_tolerance = null,
            float? intersection_y_tolerance = null,
            float text_tolerance = 3.0f,
            float text_x_tolerance = 3.0f,
            float text_y_tolerance = 3.0f,
            string strategy = null,
            List<Tuple<Point, Point>> add_lines = null,
            List<Rect> add_boxes = null,
            List<PathInfo> paths = null
        )
        {
            // Clear global state
            TableGlobals.CHARS.Clear();
            TableGlobals.EDGES.Clear();
            TableGlobals.TEXTPAGE = null;

            // Handle page rotation
            int oldRotation = page.Rotation;
            bool needsRotationReset = oldRotation != 0;
            Rect oldMediabox = null;

            if (needsRotationReset)
            {
                oldMediabox = page.MediaBox;
                page.SetRotation(0);
                // For now, we'll just reset rotation - full implementation may require more complex handling
            }

            // Handle UNSET values (use TABLE_UNSET)
            float snapX = snap_x_tolerance ?? TableFlags.TABLE_UNSET;
            float snapY = snap_y_tolerance ?? TableFlags.TABLE_UNSET;
            float joinX = join_x_tolerance ?? TableFlags.TABLE_UNSET;
            float joinY = join_y_tolerance ?? TableFlags.TABLE_UNSET;
            float interX = intersection_x_tolerance ?? TableFlags.TABLE_UNSET;
            float interY = intersection_y_tolerance ?? TableFlags.TABLE_UNSET;

            if (strategy != null)
            {
                vertical_strategy = strategy;
                horizontal_strategy = strategy;
            }

            Dictionary<string, object> settings = new Dictionary<string, object>
            {
                { "vertical_strategy", vertical_strategy },
                { "horizontal_strategy", horizontal_strategy },
                { "explicit_vertical_lines", vertical_lines },
                { "explicit_horizontal_lines", horizontal_lines },
                { "snap_tolerance", snap_tolerance },
                { "snap_x_tolerance", snapX },
                { "snap_y_tolerance", snapY },
                { "join_tolerance", join_tolerance },
                { "join_x_tolerance", joinX },
                { "join_y_tolerance", joinY },
                { "edge_min_length", edge_min_length },
                { "min_words_vertical", min_words_vertical },
                { "min_words_horizontal", min_words_horizontal },
                { "intersection_tolerance", intersection_tolerance },
                { "intersection_x_tolerance", interX },
                { "intersection_y_tolerance", interY },
                { "text_tolerance", text_tolerance },
                { "text_x_tolerance", text_x_tolerance },
                { "text_y_tolerance", text_y_tolerance }
            };

            TableFinder tbf = null;
            try
            {
                // Get layout information if available
                List<Rect> layoutBoxes = new List<Rect>();
                try
                {
                    // Try to get layout information - this may not be available in all MuPDF.NET versions
                    // page.get_layout() and page.layout_information
                    // For now, we'll skip this and proceed with table detection
                }
                catch
                {
                    // Layout information not available, continue without it
                }

                // Resolve settings
                TableSettings tset = TableSettings.Resolve(settings);

                // Create character list
                TextPage textpage = TablePageProcessing.MakeChars(page, clip: clip);
                TableGlobals.TEXTPAGE = textpage;

                // Create edges
                TablePageProcessing.MakeEdges(
                    page,
                    clip: clip,
                    tset: tset,
                    paths: paths,
                    addLines: add_lines,
                    addBoxes: add_boxes
                );

                // Create TableFinder
                tbf = new TableFinder(page, tset);
                tbf.textpage = textpage;

                // Filter tables based on layout boxes if available
                if (layoutBoxes.Count > 0)
                {
                    tbf.tables = tbf.tables.Where(tab =>
                        layoutBoxes.Any(box => IoU(tab.bbox, box) >= 0.6f)
                    ).ToList();

                    // Find layout boxes that don't match any found table
                    List<Rect> unmatchedBoxes = layoutBoxes.Where(box =>
                        tbf.tables.All(tab => IoU(box, tab.bbox) < 0.6f)
                    ).ToList();

                    // Create tables from unmatched layout boxes
                    if (unmatchedBoxes.Count > 0)
                    {
                        // Extract words for make_table_from_bbox
                        var words = textpage.ExtractWords();
                        List<Rect> wordRects = words.Select(w => new Rect(w.X0, w.Y0, w.X1, w.Y1)).ToList();

                        // Create a textpage with TABLE_DETECTOR_FLAGS for make_table_from_bbox
                        TextPage tp2 = page.GetTextPage(flags: TableGlobals.TABLE_DETECTOR_FLAGS);

                        foreach (Rect rect in unmatchedBoxes)
                        {
                            List<Rect> cells = TableHelpers.MakeTableFromBbox(tp2, wordRects, rect);
                            if (cells.Count > 0)
                            {
                                tbf.tables.Add(new Table(page, cells));
                            }
                        }
                    }
                }

                // Set textpage for all tables
                foreach (var table in tbf.tables)
                {
                    table.textpage = textpage;
                }
            }
            catch (Exception ex)
            {
                // Log exception
                System.Diagnostics.Debug.WriteLine($"find_tables: exception occurred: {ex.Message}");
                return null;
            }
            finally
            {
                if (needsRotationReset && oldRotation != 0)
                {
                    page.SetRotation(oldRotation);
                    // Note: Full page_rotation_reset would also restore mediabox and xref
                }
            }

            return tbf;
        }

        /// <summary>
        /// Compute intersection over union (IoU) of two rectangles.
        /// </summary>
        private static float IoU(Rect r1, Rect r2)
        {
            float ix = Math.Max(0, Math.Min(r1.X1, r2.X1) - Math.Max(r1.X0, r2.X0));
            float iy = Math.Max(0, Math.Min(r1.Y1, r2.Y1) - Math.Max(r1.Y0, r2.Y0));
            float intersection = ix * iy;

            if (intersection == 0)
                return 0;

            float area1 = (r1.X1 - r1.X0) * (r1.Y1 - r1.Y0);
            float area2 = (r2.X1 - r2.X0) * (r2.Y1 - r2.Y0);
            return intersection / (area1 + area2 - intersection);
        }
    }

    // TableFinder class
    public class TableFinder
    {
        public Page page { get; set; }
        public TextPage textpage { get; set; }
        public TableSettings settings { get; set; }
        public List<Edge> edges { get; set; }
        public Dictionary<Tuple<float, float>, Dictionary<string, List<Edge>>> intersections { get; set; }
        public List<Rect> cells { get; set; }
        public List<Table> tables { get; set; }

        public TableFinder(Page page, TableSettings settings = null)
        {
            this.page = page;
            this.settings = settings ?? TableSettings.Resolve();
            this.edges = GetEdges();
            this.intersections = EdgeProcessing.EdgesToIntersections(
                this.edges,
                this.settings.intersection_x_tolerance,
                this.settings.intersection_y_tolerance
            );
            this.cells = EdgeProcessing.IntersectionsToCells(this.intersections);
            var cellGroups = EdgeProcessing.CellsToTables(this.page, this.cells);
            this.tables = cellGroups.Select(cg => new Table(this.page, cg)).ToList();
        }

        private List<Edge> GetEdges()
        {
            var settings = this.settings;
            var edges = new List<Edge>();

            // Validate explicit strategies
            foreach (string orientation in new[] { "vertical", "horizontal" })
            {
                string strategy = orientation == "vertical" ? settings.vertical_strategy : settings.horizontal_strategy;
                if (strategy == "explicit")
                {
                    var lines = orientation == "vertical" ? settings.explicit_vertical_lines : settings.explicit_horizontal_lines;
                    if (lines == null || lines.Count < 2)
                    {
                        throw new ArgumentException(
                            $"If {orientation}_strategy == 'explicit', " +
                            $"explicit_{orientation}_lines must be specified as a list of two or more edges.");
                    }
                }
            }

            string vStrat = settings.vertical_strategy;
            string hStrat = settings.horizontal_strategy;

            List<Dictionary<string, object>> words = new List<Dictionary<string, object>>();
            if (vStrat == "text" || hStrat == "text")
            {
                words = TextExtractionHelpers.ExtractWords(TableGlobals.CHARS, settings.text_settings ?? new Dictionary<string, object>());
            }

            // Vertical edges
            var vExplicit = new List<Edge>();
            if (settings.explicit_vertical_lines != null)
            {
                foreach (var desc in settings.explicit_vertical_lines)
                {
                    if (desc is float x)
                    {
                        vExplicit.Add(new Edge
                        {
                            x0 = x,
                            x1 = x,
                            top = page.Rect.Y0,
                            bottom = page.Rect.Y1,
                            height = page.Rect.Height,
                            orientation = "v"
                        });
                    }
                    else if (desc is Dictionary<string, object> dict)
                    {
                        // Convert dictionary to Edge
                        var convertedEdges = EdgeProcessing.ObjToEdges(dict);
                        foreach (var e in convertedEdges)
                        {
                            if (e.orientation == "v")
                                vExplicit.Add(e);
                        }
                    }
                    else if (desc is Edge edge)
                    {
                        if (edge.orientation == "v")
                            vExplicit.Add(edge);
                    }
                }
            }

            List<Edge> vBase = new List<Edge>();
            if (vStrat == "lines")
            {
                vBase = TableGlobals.EDGES.Where(e => e.orientation == "v").ToList();
            }
            else if (vStrat == "lines_strict")
            {
                vBase = TableGlobals.EDGES.Where(e => e.orientation == "v" && e.object_type == "line").ToList();
            }
            else if (vStrat == "text")
            {
                vBase = EdgeProcessing.WordsToEdgesV(words, (int)settings.min_words_vertical);
            }

            var v = vBase.Concat(vExplicit).ToList();

            // Horizontal edges
            var hExplicit = new List<Edge>();
            if (settings.explicit_horizontal_lines != null)
            {
                foreach (var desc in settings.explicit_horizontal_lines)
                {
                    if (desc is float y)
                    {
                        hExplicit.Add(new Edge
                        {
                            x0 = page.Rect.X0,
                            x1 = page.Rect.X1,
                            top = y,
                            bottom = y,
                            width = page.Rect.Width,
                            orientation = "h"
                        });
                    }
                    else if (desc is Dictionary<string, object> dict)
                    {
                        // Convert dictionary to Edge
                        var convertedEdges = EdgeProcessing.ObjToEdges(dict);
                        foreach (var e in convertedEdges)
                        {
                            if (e.orientation == "h")
                                hExplicit.Add(e);
                        }
                    }
                    else if (desc is Edge edge)
                    {
                        if (edge.orientation == "h")
                            hExplicit.Add(edge);
                    }
                }
            }

            List<Edge> hBase = new List<Edge>();
            if (hStrat == "lines")
            {
                hBase = TableGlobals.EDGES.Where(e => e.orientation == "h").ToList();
            }
            else if (hStrat == "lines_strict")
            {
                hBase = TableGlobals.EDGES.Where(e => e.orientation == "h" && e.object_type == "line").ToList();
            }
            else if (hStrat == "text")
            {
                hBase = EdgeProcessing.WordsToEdgesH(words, (int)settings.min_words_horizontal);
            }

            var h = hBase.Concat(hExplicit).ToList();

            edges = v.Concat(h).ToList();
            edges = EdgeProcessing.MergeEdges(
                edges,
                settings.snap_x_tolerance,
                settings.snap_y_tolerance,
                settings.join_x_tolerance,
                settings.join_y_tolerance
            );

            return EdgeProcessing.FilterEdges(edges, minLength: settings.edge_min_length);
        }

        public static List<Table> FindTables(Page page, Rect clip, TableSettings settings)
        {
            var finder = new TableFinder(page, settings);
            return finder.tables;
        }

        public Table this[int i]
        {
            get
            {
                int tcount = tables.Count;
                if (i >= tcount)
                    throw new IndexOutOfRangeException("table not on page");
                while (i < 0)
                    i += tcount;
                return tables[i];
            }
        }
    }

    // Functions for making chars and edges from page
    internal static class TablePageProcessing
    {
        // make_chars - Extract text as "rawdict" to fill CHARS
        internal static TextPage MakeChars(Page page, Rect clip = null)
        {
            int pageNumber = page.Number + 1;
            float pageHeight = page.Rect.Height;
            var ctm = page.TransformationMatrix;

            var flags = TableGlobals.FLAGS;
            var textpage = page.GetTextPage(clip: clip, flags: flags);
            TableGlobals.TEXTPAGE = textpage;

            var pageInfo = textpage.ExtractRAWDict(cropbox: clip, sort: false);
            float doctopBase = pageHeight * page.Number;

            foreach (var block in pageInfo.Blocks)
            {
                if (block.Lines == null) continue;

                foreach (var line in block.Lines)
                {
                    var ldir = line.Dir;
                    var ldirRounded = Tuple.Create((float)Math.Round(ldir.X, 4), (float)Math.Round(ldir.Y, 4));
                    var matrix = new Matrix(ldirRounded.Item1, -ldirRounded.Item2, ldirRounded.Item2, ldirRounded.Item1, 0, 0);
                    bool upright = ldirRounded.Item2 == 0;

                    if (line.Spans == null) continue;
                    var sortedSpans = line.Spans.OrderBy(s => s.Bbox.X0).ToList();

                    foreach (var span in sortedSpans)
                    {
                        string fontname = span.Font;
                        float fontsize = span.Size;
                        bool spanBold = ((int)span.Flags & (int)FontStyle.TEXT_FONT_BOLD) != 0;
                        var colorInt = span.Color;
                        
                        // Extract RGB from int color (ARGB format: AARRGGBB)
                        // Normalize to 0-1 range for PDF color space
                        float r = ((colorInt >> 16) & 0xFF) / 255.0f;
                        float g = ((colorInt >> 8) & 0xFF) / 255.0f;
                        float b = (colorInt & 0xFF) / 255.0f;

                        if (span.Chars == null) continue;
                        var sortedChars = span.Chars.OrderBy(c => c.Bbox.x0).ToList();

                        foreach (var char_ in sortedChars)
                        {
                            var charBbox = char_.Bbox;
                            var bboxCtm = new Rect(charBbox) * ctm;
                            var origin = new Point(char_.Origin) * ctm;
                            matrix.E = origin.X;
                            matrix.F = origin.Y;
                            string text = char_.C.ToString();

                            var charDict = new CharDict
                            {
                                adv = upright ? (charBbox.x1 - charBbox.x0) : (charBbox.y1 - charBbox.y0),
                                bottom = charBbox.y1,
                                doctop = charBbox.y0 + doctopBase,
                                fontname = fontname,
                                height = charBbox.y1 - charBbox.y0,
                                matrix = Tuple.Create(matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, matrix.F),
                                ncs = "DeviceRGB",
                                non_stroking_color = Tuple.Create(r, g, b),
                                non_stroking_pattern = null,
                                object_type = "char",
                                page_number = pageNumber,
                                size = upright ? fontsize : (charBbox.y1 - charBbox.y0),
                                stroking_color = Tuple.Create(r, g, b),
                                stroking_pattern = null,
                                bold = spanBold,
                                text = text,
                                top = charBbox.y0,
                                upright = upright,
                                width = charBbox.x1 - charBbox.x0,
                                x0 = charBbox.x0,
                                x1 = charBbox.x1,
                                y0 = bboxCtm.Y0,
                                y1 = bboxCtm.Y1
                            };

                            TableGlobals.CHARS.Add(charDict);
                        }
                    }
                }
            }

            return textpage;
        }

        // make_edges - Extract all page vector graphics to fill the EDGES list
        internal static void MakeEdges(
            Page page,
            Rect clip = null,
            TableSettings tset = null,
            List<PathInfo> paths = null,
            List<Tuple<Point, Point>> addLines = null,
            List<Rect> addBoxes = null)
        {
            if (tset == null)
                tset = TableSettings.Resolve();

            float snapX = tset.snap_x_tolerance;
            float snapY = tset.snap_y_tolerance;
            float minLength = tset.edge_min_length;
            bool linesStrict = tset.vertical_strategy == "lines_strict" || tset.horizontal_strategy == "lines_strict";

            float pageHeight = page.Rect.Height;
            float doctopBasis = page.Number * pageHeight;
            int pageNumber = page.Number + 1;
            var prect = page.Rect;

            if (page.Rotation == 90 || page.Rotation == 270)
            {
                float w = prect.Width;
                float h = prect.Height;
                prect = new Rect(0, 0, h, w);
            }

            if (clip == null)
                clip = prect;
            else
                clip = new Rect(clip.X0, clip.Y0, clip.X1, clip.Y1);

            // Helper: Check if two rects are neighbors
            bool AreNeighbors(Rect r1, Rect r2)
            {
                if ((r2.X0 - snapX <= r1.X0 && r1.X0 <= r2.X1 + snapX ||
                     r2.X0 - snapX <= r1.X1 && r1.X1 <= r2.X1 + snapX) &&
                    (r2.Y0 - snapY <= r1.Y0 && r1.Y0 <= r2.Y1 + snapY ||
                     r2.Y0 - snapY <= r1.Y1 && r1.Y1 <= r2.Y1 + snapY))
                    return true;

                if ((r1.X0 - snapX <= r2.X0 && r2.X0 <= r1.X1 + snapX ||
                     r1.X0 - snapX <= r2.X1 && r2.X1 <= r1.X1 + snapX) &&
                    (r1.Y0 - snapY <= r2.Y0 && r2.Y0 <= r1.Y1 + snapY ||
                     r1.Y0 - snapY <= r2.Y1 && r2.Y1 <= r1.Y1 + snapY))
                    return true;

                return false;
            }

            // Helper: Clean graphics - detect and join rectangles
            Tuple<List<Rect>, List<PathInfo>> CleanGraphics(List<PathInfo> npaths = null)
            {
                List<PathInfo> allpaths = npaths ?? page.GetDrawings();
                var pathsList = new List<PathInfo>();

                foreach (var p in allpaths)
                {
                    if (linesStrict && p.Type == "f" && p.Rect.Width > snapX && p.Rect.Height > snapY)
                        continue;
                    pathsList.Add(p);
                }

                var prects = pathsList.Select(p => p.Rect).Distinct()
                    .OrderBy(r => r.Y1).ThenBy(r => r.X0).ToList();
                var newRects = new List<Rect>();

                while (prects.Count > 0)
                {
                    var prect0 = prects[0];
                    bool repeat = true;

                    while (repeat)
                    {
                        repeat = false;
                        for (int i = prects.Count - 1; i > 0; i--)
                        {
                            if (AreNeighbors(prect0, prects[i]))
                            {
                                prect0 = prect0 | prects[i];
                                prects.RemoveAt(i);
                                repeat = true;
                            }
                        }
                    }

                    if (TableHelpers.CharsInRect(TableGlobals.CHARS, prect0))
                        newRects.Add(prect0);

                    prects.RemoveAt(0);
                }

                return Tuple.Create(newRects, pathsList);
            }

            var (bboxes, cleanedPaths) = CleanGraphics(paths);

            // Helper: Check if line is roughly axis-parallel
            bool IsParallel(Point p1, Point p2)
            {
                return Math.Abs(p1.X - p2.X) <= snapX || Math.Abs(p1.Y - p2.Y) <= snapY;
            }

            // Helper: Make line dictionary
            Dictionary<string, object> MakeLine(PathInfo p, Point p1, Point p2, Rect clipRect)
            {
                if (!IsParallel(p1, p2))
                    return null;

                float x0 = Math.Min(p1.X, p2.X);
                float x1 = Math.Max(p1.X, p2.X);
                float y0 = Math.Min(p1.Y, p2.Y);
                float y1 = Math.Max(p1.Y, p2.Y);

                if (x0 > clipRect.X1 || x1 < clipRect.X0 || y0 > clipRect.Y1 || y1 < clipRect.Y0)
                    return null;

                if (x0 < clipRect.X0) x0 = clipRect.X0;
                if (x1 > clipRect.X1) x1 = clipRect.X1;
                if (y0 < clipRect.Y0) y0 = clipRect.Y0;
                if (y1 > clipRect.Y1) y1 = clipRect.Y1;

                float width = x1 - x0;
                float height = y1 - y0;
                if (width == 0 && height == 0)
                    return null;

                return new Dictionary<string, object>
                {
                    { "x0", x0 },
                    { "y0", pageHeight - y0 },
                    { "x1", x1 },
                    { "y1", pageHeight - y1 },
                    { "width", width },
                    { "height", height },
                    { "pts", new List<object> { new List<object> { x0, y0 }, new List<object> { x1, y1 } } },
                    { "linewidth", p.Width },
                    { "stroke", true },
                    { "fill", false },
                    { "evenodd", false },
                    { "stroking_color", p.Color ?? p.Fill },
                    { "non_stroking_color", null },
                    { "object_type", "line" },
                    { "page_number", pageNumber },
                    { "stroking_pattern", null },
                    { "non_stroking_pattern", null },
                    { "top", y0 },
                    { "bottom", y1 },
                    { "doctop", y0 + doctopBasis }
                };
            }

            // Process paths
            foreach (var p in cleanedPaths)
            {
                if (p.Items == null) continue;

                var items = new List<Item>(p.Items);

                // If closePath, add line from last to first point
                if (p.ClosePath && items.Count > 0 && items[0].Type == "l" && items[items.Count - 1].Type == "l")
                {
                    var lastItem = items[items.Count - 1];
                    var firstItem = items[0];
                    if (lastItem.P2 != null && firstItem.P1 != null)
                    {
                        items.Add(new Item
                        {
                            Type = "l",
                            P1 = lastItem.P2,
                            P2 = firstItem.P1
                        });
                    }
                }

                foreach (var item in items)
                {
                    if (item.Type == "l") // Line
                    {
                        if (item.P1 != null && item.LastPoint != null)
                        {
                            var lineDict = MakeLine(p, item.P1, item.LastPoint, clip);
                            if (lineDict != null)
                            {
                                var edge = EdgeProcessing.LineToEdge(lineDict);
                                TableGlobals.EDGES.Add(edge);
                            }
                        }
                    }
                    else if (item.Type == "re" && item.Rect != null) // Rectangle
                    {
                        var rect = item.Rect;
                        rect.Normalize();

                        // Check if simulates a vertical line
                        if (rect.Width <= minLength && rect.Width < rect.Height)
                        {
                            float x = Math.Abs(rect.X1 + rect.X0) / 2;
                            var p1 = new Point(x, rect.Y0);
                            var p2 = new Point(x, rect.Y1);
                            var lineDict = MakeLine(p, p1, p2, clip);
                            if (lineDict != null)
                            {
                                var edge = EdgeProcessing.LineToEdge(lineDict);
                                TableGlobals.EDGES.Add(edge);
                            }
                            continue;
                        }

                        // Check if simulates a horizontal line
                        if (rect.Height <= minLength && rect.Height < rect.Width)
                        {
                            float y = Math.Abs(rect.Y1 + rect.Y0) / 2;
                            var p1 = new Point(rect.X0, y);
                            var p2 = new Point(rect.X1, y);
                            var lineDict = MakeLine(p, p1, p2, clip);
                            if (lineDict != null)
                            {
                                var edge = EdgeProcessing.LineToEdge(lineDict);
                                TableGlobals.EDGES.Add(edge);
                            }
                            continue;
                        }

                        // Decompose rectangle into 4 lines
                        var tl = new Point(rect.X0, rect.Y0);
                        var tr = new Point(rect.X1, rect.Y0);
                        var bl = new Point(rect.X0, rect.Y1);
                        var br = new Point(rect.X1, rect.Y1);

                        var lineDict1 = MakeLine(p, tl, bl, clip);
                        if (lineDict1 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict1));

                        var lineDict2 = MakeLine(p, bl, br, clip);
                        if (lineDict2 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict2));

                        var lineDict3 = MakeLine(p, br, tr, clip);
                        if (lineDict3 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict3));

                        var lineDict4 = MakeLine(p, tr, tl, clip);
                        if (lineDict4 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict4));
                    }
                    else if (item.Type == "qu" && item.Quad != null) // Quad
                    {
                        var quad = item.Quad;
                        var ul = quad.UpperLeft;
                        var ur = quad.UpperRight;
                        var ll = quad.LowerLeft;
                        var lr = quad.LowerRight;

                        var lineDict1 = MakeLine(p, ul, ll, clip);
                        if (lineDict1 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict1));

                        var lineDict2 = MakeLine(p, ll, lr, clip);
                        if (lineDict2 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict2));

                        var lineDict3 = MakeLine(p, lr, ur, clip);
                        if (lineDict3 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict3));

                        var lineDict4 = MakeLine(p, ur, ul, clip);
                        if (lineDict4 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict4));
                    }
                }
            }

            // Add border lines for all enveloping bboxes
            var defaultPath = new PathInfo { Color = new float[] { 0, 0, 0 }, Fill = null, Width = 1 };
            foreach (var bbox in bboxes)
            {
                var tl = new Point(bbox.X0, bbox.Y0);
                var tr = new Point(bbox.X1, bbox.Y0);
                var bl = new Point(bbox.X0, bbox.Y1);
                var br = new Point(bbox.X1, bbox.Y1);

                var lineDict1 = MakeLine(defaultPath, tl, tr, clip);
                if (lineDict1 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict1));

                var lineDict2 = MakeLine(defaultPath, bl, br, clip);
                if (lineDict2 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict2));

                var lineDict3 = MakeLine(defaultPath, tl, bl, clip);
                if (lineDict3 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict3));

                var lineDict4 = MakeLine(defaultPath, tr, br, clip);
                if (lineDict4 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict4));
            }

            // Add user-specified lines
            if (addLines != null)
            {
                foreach (var (p1, p2) in addLines)
                {
                    var lineDict = MakeLine(defaultPath, p1, p2, clip);
                    if (lineDict != null)
                        TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict));
                }
            }

            // Add user-specified boxes
            if (addBoxes != null)
            {
                foreach (var box in addBoxes)
                {
                    var tl = new Point(box.X0, box.Y0);
                    var tr = new Point(box.X1, box.Y0);
                    var bl = new Point(box.X0, box.Y1);
                    var br = new Point(box.X1, box.Y1);

                    var lineDict1 = MakeLine(defaultPath, tl, bl, clip);
                    if (lineDict1 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict1));

                    var lineDict2 = MakeLine(defaultPath, bl, br, clip);
                    if (lineDict2 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict2));

                    var lineDict3 = MakeLine(defaultPath, br, tr, clip);
                    if (lineDict3 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict3));

                    var lineDict4 = MakeLine(defaultPath, tr, tl, clip);
                    if (lineDict4 != null) TableGlobals.EDGES.Add(EdgeProcessing.LineToEdge(lineDict4));
                }
            }
        }
    }
}
