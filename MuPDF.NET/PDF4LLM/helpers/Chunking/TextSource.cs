using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MuPDF.NET;

namespace MuPDF.NET.PDF4LLM.Helpers.Chunking
{
    internal static class TextSource
    {
        static readonly Regex ThRe = new Regex(
            @"<th\b[^>]*>(.*?)</th>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex TagRe = new Regex(@"<[^>]+>", RegexOptions.Compiled);

        public static (string markdown, string html) TableContent(LayoutBox box)
        {
            var table = box?.Table ?? new Dictionary<string, object>();
            object htmlObj = null;
            table.TryGetValue("html", out htmlObj);
            string html = htmlObj as string;
            if (html == null)
            {
                if (table.TryGetValue("html_tables", out object per) && per is IEnumerable<object> items)
                {
                    var parts = new List<string>();
                    foreach (var t in items)
                    {
                        if (t is Dictionary<string, object> d && d.TryGetValue("html", out object h) && h != null)
                            parts.Add(h.ToString());
                    }
                    if (parts.Count > 0)
                        html = string.Join("\n\n", parts);
                }
            }
            if (html != null)
                return (null, html);
            table.TryGetValue("markdown", out object md);
            return (md as string, null);
        }

        public static List<string> ExtractTableHeaders(string html)
        {
            var headers = new List<string>();
            if (string.IsNullOrEmpty(html))
                return headers;
            foreach (Match m in ThRe.Matches(html))
            {
                string inner = TagRe.Replace(m.Groups[1].Value, "");
                headers.Add(inner.Trim());
            }
            return headers;
        }

        public static string FigurePlaceholder(int figNumber, LayoutBox box)
        {
            int w = (int)(box.X1 - box.X0);
            int h = (int)(box.Y1 - box.Y0);
            return $"[Figure f{figNumber}: {w}x{h}]";
        }

        static List<TextLineInfo> CopyTextLines(List<TextLineInfo> textLines)
        {
            if (textLines == null)
                return null;
            var copy = new List<TextLineInfo>(textLines.Count);
            foreach (var tl in textLines)
            {
                var spans = new List<ExtendedSpan>();
                if (tl?.Spans != null)
                {
                    foreach (var s in tl.Spans)
                    {
                        if (s == null)
                            continue;
                        spans.Add(new ExtendedSpan
                        {
                            Text = s.Text,
                            Bbox = s.Bbox,
                            Origin = s.Origin,
                            Size = s.Size,
                            Font = s.Font,
                            Flags = s.Flags,
                            CharFlags = s.CharFlags,
                            Alpha = s.Alpha,
                            Line = s.Line,
                            Block = s.Block,
                            Dir = s.Dir,
                            Chars = s.Chars,
                        });
                    }
                }
                copy.Add(new TextLineInfo { Bbox = tl?.Bbox, Spans = spans });
            }
            return copy;
        }

        public static string BoxToMarkdown(
            PageLayout page,
            LayoutBox box,
            int boxIdx,
            Dictionary<int, int> listItemLevels = null,
            bool ignoreCode = false)
        {
            if (box == null)
                return "";
            string btype = box.BoxClass;
            ignoreCode = ignoreCode || (page != null && page.FullOcred);

            if (btype == "table" || btype == "table-fallback")
            {
                var (markdown, html) = TableContent(box);
                if (html != null)
                    return html;
                string tableText = markdown ?? "";
                if (page != null && page.FullOcred)
                    tableText = tableText.Replace("`", "");
                if (!string.IsNullOrWhiteSpace(tableText))
                    return tableText;
                if (box.TextLines != null && box.TextLines.Count > 0)
                    return ParsedDocument.TextToMd(CopyTextLines(box.TextLines), ignoreCode);
                return "";
            }

            if (box.TextLines == null || box.TextLines.Count == 0)
                return "";
            var textLines = CopyTextLines(box.TextLines);

            if (btype == "picture")
                return ParsedDocument.PictureTextToMd(textLines, ignoreCode);
            if (btype == "formula")
                return ParsedDocument.TextToMd(textLines, ignoreCode);
            if (btype == "title")
                return ParsedDocument.TitleToMd(box.HeaderLevel, textLines);
            if (btype == "section-header")
                return ParsedDocument.SectionHdrToMd(box.HeaderLevel, textLines);
            if (btype == "list-item")
            {
                if (listItemLevels == null && page?.Boxes != null)
                    listItemLevels = ParsedDocument.CreateListItemLevels(page.Boxes);
                int level = 1;
                if (listItemLevels != null)
                    listItemLevels.TryGetValue(boxIdx, out level);
                if (level < 1)
                    level = 1;
                return ParsedDocument.ListItemToMd(textLines, level);
            }
            if (btype == "footnote")
                return ParsedDocument.FootnoteToMd(textLines);
            return ParsedDocument.TextToMd(textLines, ignoreCode);
        }
    }
}
