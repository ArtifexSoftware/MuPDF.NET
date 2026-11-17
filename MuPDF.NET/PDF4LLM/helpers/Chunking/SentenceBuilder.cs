using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MuPDF.NET;

namespace MuPDF.NET.PDF4LLM.Helpers.Chunking
{
    internal sealed class SentenceBuilder
    {
        static readonly Regex SentEndEn = new Regex(
            @"[.!?]\s*[""'\)\]]*(\s+)(?=[A-Z""'\(\[])",
            RegexOptions.Compiled);
        static readonly Regex SentEndMulti = new Regex(
            @"[.!?。！？]\s*[""'\)\]」』]*(\s*)(?=[A-Z가-힣ㄱ-ㅎㅏ-ㅣ一-鿿""'\(\[「『]|$)",
            RegexOptions.Compiled);
        static readonly Regex MultiSpace = new Regex(@"[ \t]+", RegexOptions.Compiled);
        static readonly Regex MdDecoration = new Regex(@"^[#>*_`\s]+", RegexOptions.Compiled);
        static readonly Regex[] CaptionPatterns =
        {
            new Regex(@"^(?:figure|fig\.?\s*)\s*\d+(?:[.\-]\d+)*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^(?:table|tbl\.?\s*)\s*\d+(?:[.\-]\d+)*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^\(\s*[a-z0-9]\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^(?:source|notes?)\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };

        readonly Regex _splitRe;
        int _figureCount;

        public SentenceBuilder(string splitter = "default")
        {
            _splitRe = splitter == "multilingual" ? SentEndMulti : SentEndEn;
        }

        public List<SentenceUnit> BuildFromDocument(ParsedDocument doc)
        {
            var units = new List<SentenceUnit>();
            int sentId = 0;
            SentenceUnit prevUnit = null;
            _figureCount = 0;
            if (doc?.Pages == null)
                return units;

            foreach (var page in doc.Pages)
            {
                var listItemLevels = ParsedDocument.CreateListItemLevels(page.Boxes);
                var boxes = page.Boxes ?? new List<LayoutBox>();
                for (int boxIdx = 0; boxIdx < boxes.Count; boxIdx++)
                {
                    var newUnits = ProcessBox(page, boxIdx, boxes[boxIdx], sentId, doc.Toc, prevUnit, listItemLevels);
                    foreach (var u in newUnits)
                    {
                        units.Add(u);
                        sentId++;
                        prevUnit = u;
                    }
                }
            }

            units = MergeSamePageHfUnits(units);
            RenumberSentIds(units);
            for (int i = 0; i < units.Count - 1; i++)
            {
                if (units[i + 1].LineGapBefore != null && units[i].PageNo == units[i + 1].PageNo)
                    units[i].LineGapAfter = units[i + 1].LineGapBefore;
            }
            return units;
        }

        List<SentenceUnit> ProcessBox(
            PageLayout page, int boxIdx, LayoutBox box, int sentIdStart,
            List<object> toc, SentenceUnit prevUnit, Dictionary<int, int> listItemLevels)
        {
            if (box == null)
                return new List<SentenceUnit>();
            string boxclass = box.BoxClass;
            if (boxclass == "table" || boxclass == "table-fallback")
                return TableAsUnit(page, boxIdx, box, sentIdStart, toc);
            if (boxclass == "picture" || boxclass == "formula")
            {
                _figureCount++;
                if (box.TextLines == null || box.TextLines.Count == 0)
                    return FigureAsUnit(page, boxIdx, box, sentIdStart, toc);
                var units = SplitSentences(page, boxIdx, box, sentIdStart, toc, prevUnit, listItemLevels);
                foreach (var u in units)
                    u.FigureNumber = _figureCount;
                return units;
            }
            if (box.TextLines != null && box.TextLines.Count > 0)
                return SplitSentences(page, boxIdx, box, sentIdStart, toc, prevUnit, listItemLevels);
            return new List<SentenceUnit>();
        }

        List<SentenceUnit> TableAsUnit(PageLayout page, int boxIdx, LayoutBox box, int sentIdStart, List<object> toc)
        {
            var (tableMd, tableHtml) = TextSource.TableContent(box);
            string text = TextSource.BoxToMarkdown(page, box, boxIdx).Trim();
            if (string.IsNullOrEmpty(text))
                return new List<SentenceUnit>();
            var unit = MakeBaseUnit(sentIdStart, text, page.PageNumber, boxIdx, box, toc);
            unit.IsTableContent = true;
            unit.TableMarkdown = tableMd;
            unit.TableHtml = tableHtml;
            ApplyHints(unit, box.BoxClass, toc, page.PageNumber, text, box.HeaderLevel);
            return new List<SentenceUnit> { unit };
        }

        List<SentenceUnit> FigureAsUnit(PageLayout page, int boxIdx, LayoutBox box, int sentIdStart, List<object> toc)
        {
            string text = TextSource.FigurePlaceholder(_figureCount, box);
            var unit = MakeBaseUnit(sentIdStart, text, page.PageNumber, boxIdx, box, toc);
            unit.IsFigureRelated = true;
            unit.FigureNumber = _figureCount;
            unit.ImageData = box.Image;
            ApplyHints(unit, box.BoxClass, toc, page.PageNumber, text, box.HeaderLevel);
            return new List<SentenceUnit> { unit };
        }

        List<SentenceUnit> SplitSentences(
            PageLayout page, int boxIdx, LayoutBox box, int sentIdStart,
            List<object> toc, SentenceUnit prevUnit, Dictionary<int, int> listItemLevels)
        {
            var textLines = box.TextLines;
            if (textLines == null || textLines.Count == 0)
                return new List<SentenceUnit>();
            string joined = TextSource.BoxToMarkdown(page, box, boxIdx, listItemLevels).Trim();
            if (string.IsNullOrEmpty(joined))
                return new List<SentenceUnit>();

            var (fontSize, fontFlags) = GetDominantFont(textLines);
            double? firstGap = null;
            if (prevUnit != null && prevUnit.PageNo == page.PageNumber && textLines[0].Bbox != null && prevUnit.Bbox != null)
                firstGap = textLines[0].Bbox.Y0 - prevUnit.Bbox[3];

            string plain = StripMdDecoration(joined);
            var hints = new SentenceUnit();
            ApplyHints(hints, box.BoxClass, toc, page.PageNumber, plain, box.HeaderLevel);

            if (!hints.IsCaption && box.BoxClass != "title" && box.BoxClass != "section-header"
                && box.BoxClass != "table" && box.BoxClass != "table-fallback"
                && box.BoxClass != "picture" && box.BoxClass != "formula"
                && box.BoxClass != "footnote" && box.BoxClass != "page-header" && box.BoxClass != "page-footer")
            {
                var (isCap, capTarget) = DetectCaption(plain);
                if (isCap)
                {
                    hints.IsCaption = true;
                    hints.CaptionTargetType = capTarget;
                }
            }

            bool keepSingle = hints.IsHeadingHint || hints.IsFootnote || hints.IsHeaderFooter
                || hints.IsCaption || hints.IsListItem || joined.Contains("\n");
            List<string> sentences = null;
            if (!keepSingle)
            {
                sentences = SplitSentenceText(joined, _splitRe);
                if (sentences.Count == 0)
                    return new List<SentenceUnit>();
                keepSingle = sentences.Count == 1;
            }

            double[] bbox = ComputeBboxUnion(textLines);
            if (keepSingle)
            {
                var unit = MakeBaseUnit(sentIdStart, joined, page.PageNumber, boxIdx, box, toc);
                CopyHints(hints, unit);
                unit.Bbox = bbox;
                unit.FontSizeDominant = fontSize;
                unit.FontFlagsDominant = fontFlags;
                unit.LineGapBefore = firstGap;
                return new List<SentenceUnit> { unit };
            }

            var units = new List<SentenceUnit>();
            for (int i = 0; i < sentences.Count; i++)
            {
                var unit = MakeBaseUnit(sentIdStart + i, sentences[i], page.PageNumber, boxIdx, box, toc);
                CopyHints(hints, unit);
                unit.Bbox = bbox;
                unit.FontSizeDominant = fontSize;
                unit.FontFlagsDominant = fontFlags;
                unit.LineGapBefore = i == 0 ? firstGap : null;
                units.Add(unit);
            }
            return units;
        }

        public HashSet<(int page, int box)> DetectRepeatedHeadersFooters(ParsedDocument doc)
        {
            var repeated = new HashSet<(int, int)>();
            if (doc?.Pages == null || doc.Pages.Count < 3)
                return repeated;
            var hfTexts = new Dictionary<(string boxclass, int yBucket, string text), List<(int page, int box)>>();
            foreach (var page in doc.Pages)
            {
                var boxes = page.Boxes ?? new List<LayoutBox>();
                for (int boxIdx = 0; boxIdx < boxes.Count; boxIdx++)
                {
                    var box = boxes[boxIdx];
                    if ((box.BoxClass == "page-header" || box.BoxClass == "page-footer") && box.TextLines != null)
                    {
                        string text = NormalizeText(TextSource.BoxToMarkdown(page, box, boxIdx));
                        if (!string.IsNullOrEmpty(text))
                        {
                            int yBucket = (int)(Math.Round(box.Y0 / 10.0) * 10);
                            var key = (box.BoxClass, yBucket, text);
                            if (!hfTexts.TryGetValue(key, out var locs))
                            {
                                locs = new List<(int, int)>();
                                hfTexts[key] = locs;
                            }
                            locs.Add((page.PageNumber, boxIdx));
                        }
                    }
                }
            }
            double threshold = Math.Max(2, doc.Pages.Count * 0.5);
            foreach (var locs in hfTexts.Values)
            {
                if (locs.Count >= threshold)
                {
                    foreach (var loc in locs)
                        repeated.Add(loc);
                }
            }
            return repeated;
        }

        List<SentenceUnit> MergeSamePageHfUnits(List<SentenceUnit> units, double yTolerance = 20.0)
        {
            if (units == null || units.Count == 0)
                return units ?? new List<SentenceUnit>();
            var result = new List<SentenceUnit>();
            int i = 0;
            while (i < units.Count)
            {
                var u = units[i];
                if (!u.IsHeaderFooter)
                {
                    result.Add(u);
                    i++;
                    continue;
                }
                var group = new List<SentenceUnit> { u };
                int j = i + 1;
                while (j < units.Count)
                {
                    var nxt = units[j];
                    if (nxt.IsHeaderFooter && nxt.PageNo == u.PageNo && nxt.BoxClass == u.BoxClass)
                    {
                        group.Add(nxt);
                        j++;
                    }
                    else
                        break;
                }
                if (group.Count == 1)
                    result.Add(u);
                else
                {
                    foreach (var band in GroupByYBand(group, yTolerance))
                        result.Add(MergeHfGroup(band));
                }
                i = j;
            }
            return result;
        }

        static List<List<SentenceUnit>> GroupByYBand(List<SentenceUnit> units, double yTolerance)
        {
            var sorted = units.OrderBy(u => (u.Bbox[1] + u.Bbox[3]) / 2).ToList();
            var bands = new List<List<SentenceUnit>>();
            foreach (var u in sorted)
            {
                double yc = (u.Bbox[1] + u.Bbox[3]) / 2;
                if (bands.Count > 0)
                {
                    var last = bands[bands.Count - 1][bands[bands.Count - 1].Count - 1];
                    double lastYc = (last.Bbox[1] + last.Bbox[3]) / 2;
                    if (Math.Abs(yc - lastYc) <= yTolerance)
                    {
                        bands[bands.Count - 1].Add(u);
                        continue;
                    }
                }
                bands.Add(new List<SentenceUnit> { u });
            }
            return bands;
        }

        static SentenceUnit MergeHfGroup(List<SentenceUnit> group)
        {
            if (group.Count == 1)
                return group[0];
            var sorted = group.OrderBy(u => u.Bbox[0]).ToList();
            string text = string.Join(" | ", sorted.Select(u => u.Text));
            var longest = sorted.OrderByDescending(u => (u.Text ?? "").Length).First();
            var source = new List<int>();
            foreach (var u in sorted)
            {
                source.Add(u.BoxIndex);
                if (u.SourceBoxIndices != null)
                    source.AddRange(u.SourceBoxIndices);
            }
            return new SentenceUnit
            {
                SentId = sorted[0].SentId,
                Text = text,
                NormText = NormalizeText(text),
                PageNo = sorted[0].PageNo,
                BoxIndex = sorted[0].BoxIndex,
                BoxClass = sorted[0].BoxClass,
                Bbox = ChunkGeometry.UnionBbox(sorted.Select(u => u.Bbox)),
                FontSizeDominant = longest.FontSizeDominant,
                FontFlagsDominant = longest.FontFlagsDominant,
                LineGapBefore = sorted[0].LineGapBefore,
                IsHeaderFooter = true,
                SourceBoxIndices = source,
            };
        }

        static void RenumberSentIds(List<SentenceUnit> units)
        {
            for (int i = 0; i < units.Count; i++)
                units[i].SentId = i;
        }

        static SentenceUnit MakeBaseUnit(int sentId, string text, int pageNo, int boxIdx, LayoutBox box, List<object> toc)
        {
            return new SentenceUnit
            {
                SentId = sentId,
                Text = text,
                NormText = NormalizeText(text),
                PageNo = pageNo,
                BoxIndex = boxIdx,
                BoxClass = box.BoxClass,
                Bbox = new[] { (double)box.X0, box.Y0, box.X1, box.Y1 },
            };
        }

        static void ApplyHints(SentenceUnit unit, string boxclass, List<object> toc, int pageNo, string text, int headerLevel)
        {
            if (boxclass == "title")
            {
                unit.IsHeadingHint = true;
                unit.HeadingLevelHint = headerLevel != 0 ? headerLevel : 1;
            }
            else if (boxclass == "section-header")
            {
                unit.IsHeadingHint = true;
                int? level = headerLevel != 0 ? headerLevel : MatchTocLevel(toc, pageNo, text);
                unit.HeadingLevelHint = level ?? 2;
            }
            else if (boxclass == "list-item")
                unit.IsListItem = true;
            else if (boxclass == "table" || boxclass == "table-fallback")
                unit.IsTableContent = true;
            else if (boxclass == "picture" || boxclass == "formula")
                unit.IsFigureRelated = true;
            else if (boxclass == "footnote")
                unit.IsFootnote = true;
            else if (boxclass == "page-header" || boxclass == "page-footer")
                unit.IsHeaderFooter = true;
            else if (boxclass == "caption")
            {
                unit.IsCaption = true;
                if (!string.IsNullOrEmpty(text))
                {
                    var (_, target) = DetectCaption(text);
                    if (target != null)
                        unit.CaptionTargetType = target;
                }
            }
        }

        static void CopyHints(SentenceUnit from, SentenceUnit to)
        {
            to.IsHeadingHint = from.IsHeadingHint;
            to.HeadingLevelHint = from.HeadingLevelHint;
            to.IsListItem = from.IsListItem;
            to.IsTableContent = from.IsTableContent;
            to.IsFigureRelated = from.IsFigureRelated;
            to.IsFootnote = from.IsFootnote;
            to.IsHeaderFooter = from.IsHeaderFooter;
            to.IsCaption = from.IsCaption;
            to.CaptionTargetType = from.CaptionTargetType;
        }

        static int? MatchTocLevel(List<object> toc, int pageNo, string text)
        {
            if (toc == null || string.IsNullOrEmpty(text))
                return null;
            string norm = NormalizeText(text);
            if (string.IsNullOrEmpty(norm))
                return null;
            foreach (var entry in toc)
            {
                if (!TryTocEntry(entry, out int level, out string title, out int tocPage))
                    continue;
                if (tocPage != pageNo)
                    continue;
                string tocNorm = NormalizeText(title);
                if (string.IsNullOrEmpty(tocNorm))
                    continue;
                if (norm.StartsWith(tocNorm, StringComparison.Ordinal) || tocNorm.StartsWith(norm, StringComparison.Ordinal))
                    return level;
            }
            return null;
        }

        static bool TryTocEntry(object entry, out int level, out string title, out int page)
        {
            level = 0;
            title = null;
            page = 0;
            if (entry is Toc toc)
            {
                level = toc.Level;
                title = toc.Title;
                page = toc.Page;
                return true;
            }
            if (entry is ValueTuple<int, string, int, Dictionary<string, object>> vt)
            {
                level = vt.Item1;
                title = vt.Item2;
                page = vt.Item3;
                return true;
            }
            if (entry is IList<object> list && list.Count >= 3)
            {
                try
                {
                    level = Convert.ToInt32(list[0]);
                    title = list[1]?.ToString();
                    page = Convert.ToInt32(list[2]);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        static (bool isCaption, string target) DetectCaption(string text)
        {
            string stripped = (text ?? "").Trim();
            if (string.IsNullOrEmpty(stripped))
                return (false, null);
            foreach (var pat in CaptionPatterns)
            {
                if (pat.IsMatch(stripped))
                {
                    string low = stripped.ToLowerInvariant();
                    if (low.StartsWith("figure") || low.StartsWith("fig"))
                        return (true, "figure");
                    if (low.StartsWith("table") || low.StartsWith("tbl"))
                        return (true, "table");
                    return (true, null);
                }
            }
            return (false, null);
        }

        static string NormalizeText(string text)
        {
            string t = (text ?? "").Trim().ToLowerInvariant();
            return MultiSpace.Replace(t, " ");
        }

        static string StripMdDecoration(string text) => MdDecoration.Replace(text ?? "", "");

        static (double size, int flags) GetDominantFont(List<TextLineInfo> textLines)
        {
            var counts = new Dictionary<(double size, int flags), int>();
            foreach (var tl in textLines)
            {
                if (tl?.Spans == null)
                    continue;
                foreach (var span in tl.Spans)
                {
                    int charCount = (span.Text ?? "").Trim().Length;
                    if (charCount > 0)
                    {
                        var key = (Math.Round(span.Size, 1), span.Flags);
                        counts.TryGetValue(key, out int n);
                        counts[key] = n + charCount;
                    }
                }
            }
            if (counts.Count == 0)
                return (0, 0);
            var best = counts.OrderByDescending(kv => kv.Value).First().Key;
            return (best.size, best.flags);
        }

        static double[] ComputeBboxUnion(List<TextLineInfo> textLines)
        {
            var boxes = new List<double[]>();
            foreach (var tl in textLines)
            {
                if (tl?.Bbox == null)
                    continue;
                boxes.Add(new[] { (double)tl.Bbox.X0, tl.Bbox.Y0, tl.Bbox.X1, tl.Bbox.Y1 });
            }
            return ChunkGeometry.UnionBbox(boxes);
        }

        static List<string> SplitSentenceText(string text, Regex pattern)
        {
            var pieces = new List<string>();
            int pos = 0;
            foreach (Match m in pattern.Matches(text))
            {
                if (!m.Groups[1].Success)
                    continue;
                string piece = text.Substring(pos, m.Groups[1].Index - pos).Trim();
                if (!string.IsNullOrEmpty(piece))
                    pieces.Add(piece);
                pos = m.Groups[1].Index + m.Groups[1].Length;
            }
            string tail = text.Substring(pos).Trim();
            if (!string.IsNullOrEmpty(tail))
                pieces.Add(tail);
            return pieces;
        }
    }
}
