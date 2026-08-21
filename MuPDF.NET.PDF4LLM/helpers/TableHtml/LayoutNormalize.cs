using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;

namespace MuPDF.NET.PDF4LLM.Helpers.TableHtml
{
    /// <summary>
    /// Assign HTML tables to layout boxes and split text boxes around tables
    /// (pymupdf4llm <c>document_layout.normalize_layout_boxes</c>).
    /// </summary>
    internal static class LayoutNormalize
    {
        internal sealed class HtmlTableMeta
        {
            public List<float> Bbox { get; set; }
            public string Html { get; set; }
            public int? Rows { get; set; }
            public int? Cols { get; set; }
            public List<List<object>> Cells { get; set; }
            public List<List<object>> Extract { get; set; }

            /// <summary>
            /// Snake-case dict matching pymupdf4llm <c>_html_table_meta</c> for JSON export.
            /// </summary>
            public Dictionary<string, object> ToDict() =>
                new Dictionary<string, object>
                {
                    ["bbox"] = Bbox,
                    ["html"] = Html,
                    ["rows"] = Rows,
                    ["cols"] = Cols,
                    ["cells"] = Cells,
                    ["extract"] = Extract,
                };
        }

        static float RectArea(Rect rect) =>
            Math.Max(0f, rect.X1 - rect.X0) * Math.Max(0f, rect.Y1 - rect.Y0);

        static HtmlTableMeta HtmlTableMetaFrom(
            (Rect bbox, string html, int rows, int cols, List<List<object>> cells, List<List<object>> extract) item)
        {
            Rect rect = item.bbox;
            return new HtmlTableMeta
            {
                Bbox = new List<float> { rect.X0, rect.Y0, rect.X1, rect.Y1 },
                Html = item.html,
                Rows = item.rows,
                Cols = item.cols,
                Cells = item.cells,
                Extract = item.extract,
            };
        }

        static string BoxKey(Rect bbox)
        {
            var ir = new IRect(bbox);
            return $"{ir.X0},{ir.Y0},{ir.X1},{ir.Y1}";
        }

        internal static (
            List<LayoutInfoEntry> layout,
            Dictionary<string, List<HtmlTableMeta>> htmlTablesByBox,
            Dictionary<string, List<TextLineInfo>> textlinesByBox)
            NormalizeLayoutBoxes(
                List<LayoutInfoEntry> layoutBoxes,
                List<(Rect bbox, string html, int rows, int cols, List<List<object>> cells, List<List<object>> extract)> htmlTables,
                List<Block> fulltext,
                float threshold = 0.5f)
        {
            (List<LayoutInfoEntry> normalized, Dictionary<string, List<HtmlTableMeta>> byBox) =
                AssignHtmlTablesToBoxes(layoutBoxes, htmlTables, threshold);
            if (byBox == null || byBox.Count == 0)
            {
                return (
                    normalized,
                    byBox ?? new Dictionary<string, List<HtmlTableMeta>>(),
                    new Dictionary<string, List<TextLineInfo>>());
            }

            var tableRects = byBox.Values
                .SelectMany(items => items)
                .Select(item => new Rect(item.Bbox[0], item.Bbox[1], item.Bbox[2], item.Bbox[3]))
                .ToList();

            var outputBoxes = new List<LayoutInfoEntry>();
            var textlinesByBox = new Dictionary<string, List<TextLineInfo>>();
            foreach (LayoutInfoEntry box in normalized)
            {
                string cls = box?.Class ?? "";
                if (box?.Bbox == null || cls == "table" || cls == "picture" || cls == "formula")
                {
                    outputBoxes.Add(box);
                    continue;
                }

                (List<LayoutInfoEntry> splitBoxes, Dictionary<string, List<TextLineInfo>> splitTextlines) =
                    SplitTextBoxAroundTables(box, fulltext, tableRects);
                outputBoxes.AddRange(splitBoxes);
                foreach (var kv in splitTextlines)
                    textlinesByBox[kv.Key] = kv.Value;
            }

            return (outputBoxes, byBox, textlinesByBox);
        }

        static (List<LayoutInfoEntry> layout, Dictionary<string, List<HtmlTableMeta>> byBox)
            AssignHtmlTablesToBoxes(
                List<LayoutInfoEntry> layoutBoxes,
                List<(Rect bbox, string html, int rows, int cols, List<List<object>> cells, List<List<object>> extract)> htmlTables,
                float threshold)
        {
            var byBox = new Dictionary<string, List<HtmlTableMeta>>();
            if (htmlTables == null || htmlTables.Count == 0)
                return (layoutBoxes ?? new List<LayoutInfoEntry>(), byBox);

            var tableBoxes = (layoutBoxes ?? new List<LayoutInfoEntry>())
                .Where(b => b?.Bbox != null && b.Class == "table")
                .Select(b => (key: BoxKey(b.Bbox), rect: new Rect(b.Bbox)))
                .ToList();
            var augmented = new List<LayoutInfoEntry>(layoutBoxes ?? new List<LayoutInfoEntry>());

            foreach (var tableItem in htmlTables)
            {
                HtmlTableMeta meta = HtmlTableMetaFrom(tableItem);
                Rect tableRect = new Rect(meta.Bbox[0], meta.Bbox[1], meta.Bbox[2], meta.Bbox[3]);
                float tableArea = RectArea(tableRect);
                string bestKey = null;
                float bestScore = 0f;
                if (tableArea > 0)
                {
                    foreach (var (key, boxRect) in tableBoxes)
                    {
                        Rect inter = tableRect & boxRect;
                        if (inter.IsEmpty)
                            continue;
                        float score = RectArea(inter) / tableArea;
                        if (score > bestScore)
                        {
                            bestKey = key;
                            bestScore = score;
                        }
                    }
                }

                if (bestKey == null || bestScore < threshold)
                {
                    augmented.Add(new LayoutInfoEntry
                    {
                        Bbox = new Rect(tableRect),
                        Class = "table",
                    });
                    bestKey = BoxKey(tableRect);
                    tableBoxes.Add((bestKey, tableRect));
                }

                if (!byBox.TryGetValue(bestKey, out List<HtmlTableMeta> list))
                {
                    list = new List<HtmlTableMeta>();
                    byBox[bestKey] = list;
                }
                list.Add(meta);
            }

            foreach (List<HtmlTableMeta> items in byBox.Values)
            {
                items.Sort((a, b) =>
                {
                    int c = a.Bbox[1].CompareTo(b.Bbox[1]);
                    return c != 0 ? c : a.Bbox[0].CompareTo(b.Bbox[0]);
                });
            }

            return (augmented, byBox);
        }

        static bool LineClaimedByTable(Rect lineBbox, List<Rect> tableRects)
        {
            Rect lineRect = new Rect(lineBbox);
            float lineArea = RectArea(lineRect);
            if (lineArea <= 0)
                return false;
            var center = new Point(
                (lineRect.X0 + lineRect.X1) * 0.5f,
                (lineRect.Y0 + lineRect.Y1) * 0.5f);
            foreach (Rect tableRect in tableRects)
            {
                if (tableRect.Contains(center))
                    return true;
                Rect inter = lineRect & tableRect;
                if (!inter.IsEmpty && RectArea(inter) / lineArea >= 0.5f)
                    return true;
            }
            return false;
        }

        static Rect UnionLineRects(List<TextLineInfo> lines)
        {
            Rect rect = new Rect(lines[0].Bbox);
            for (int i = 1; i < lines.Count; i++)
                rect |= new Rect(lines[i].Bbox);
            return rect;
        }

        static (List<LayoutInfoEntry> boxes, Dictionary<string, List<TextLineInfo>> textlines)
            SplitTextBoxAroundTables(LayoutInfoEntry box, List<Block> fulltext, List<Rect> tableRects)
        {
            Rect boxRect = new Rect(box.Bbox);
            if (!tableRects.Any(tr => boxRect.Intersects(tr)))
                return (new List<LayoutInfoEntry> { box }, new Dictionary<string, List<TextLineInfo>>());

            List<TextLine> raw;
            try
            {
                raw = GetTextLines.GetRawLines(
                    textPage: null,
                    blocks: fulltext,
                    clip: boxRect,
                    ignoreInvisible: false,
                    onlyHorizontal: false);
            }
            catch
            {
                return (new List<LayoutInfoEntry> { box }, new Dictionary<string, List<TextLineInfo>>());
            }

            var lines = raw
                .Select(l => new TextLineInfo { Bbox = l.Rect, Spans = l.Spans })
                .ToList();
            if (lines.Count == 0)
                return (new List<LayoutInfoEntry> { box }, new Dictionary<string, List<TextLineInfo>>());

            var splitBoxes = new List<LayoutInfoEntry>();
            var textlinesByBox = new Dictionary<string, List<TextLineInfo>>();
            var claimed = lines.Select(l => LineClaimedByTable(l.Bbox, tableRects)).ToList();
            claimed.Add(true);
            int? start = null;
            for (int index = 0; index < claimed.Count; index++)
            {
                bool isClaimed = claimed[index];
                if (!isClaimed && start == null)
                    start = index;
                else if (isClaimed && start != null)
                {
                    int end = index - 1;
                    float y0 = start.Value == 0
                        ? boxRect.Y0
                        : new Rect(lines[start.Value - 1].Bbox).Y1;
                    float y1 = end == lines.Count - 1
                        ? boxRect.Y1
                        : new Rect(lines[end + 1].Bbox).Y0;
                    if (y1 <= y0)
                    {
                        Rect union = UnionLineRects(lines.GetRange(start.Value, end - start.Value + 1));
                        y0 = union.Y0;
                        y1 = union.Y1;
                    }

                    var splitBox = new LayoutInfoEntry
                    {
                        Bbox = new Rect(boxRect.X0, y0, boxRect.X1, y1),
                        Class = box.Class,
                        RawDict = box.RawDict,
                    };
                    splitBoxes.Add(splitBox);
                    textlinesByBox[BoxKey(splitBox.Bbox)] =
                        lines.GetRange(start.Value, end - start.Value + 1);
                    start = null;
                }
            }

            return (splitBoxes, textlinesByBox);
        }
    }
}
