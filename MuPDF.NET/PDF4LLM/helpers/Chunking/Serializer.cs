using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MuPDF.NET;

namespace MuPDF.NET.PDF4LLM.Helpers.Chunking
{
    internal sealed class ChunkSerializer
    {
        static readonly Regex MdDecorRe = new Regex(@"^[#>*_`\s]+", RegexOptions.Compiled);
        static readonly Regex MdDecorTrailRe = new Regex(@"[*_`\s]+$", RegexOptions.Compiled);

        readonly ParsedDocument _doc;
        readonly HashSet<int> _ocrPages;

        public ChunkSerializer(ParsedDocument doc)
        {
            _doc = doc;
            _ocrPages = new HashSet<int>();
            if (doc?.Pages != null)
            {
                foreach (var p in doc.Pages)
                {
                    if (p.FullOcred)
                        _ocrPages.Add(p.PageNumber);
                }
            }
        }

        public (List<Chunk> chunks, List<TableChunk> tables, List<FigureChunk> figures, List<SectionChunk> sections)
            Serialize(List<ProtoChunk> protoChunks)
        {
            if (protoChunks == null || protoChunks.Count == 0)
                return (new List<Chunk>(), new List<TableChunk>(), new List<FigureChunk>(), new List<SectionChunk>());

            var chunks = protoChunks.Select(MakeChunk).ToList();
            var (tables, figures, sections) = BuildViews(protoChunks, chunks);
            for (int i = 0; i < protoChunks.Count; i++)
                chunks[i].TaggedContent = BuildTaggedContent(protoChunks[i], chunks[i].Metadata.SectionPath);
            return (chunks, tables, figures, sections);
        }

        Chunk MakeChunk(ProtoChunk pc)
        {
            pc.Text = string.Join("\n", pc.Sentences.Select(s => s.Text));
            return new Chunk
            {
                Id = $"c{pc.ChunkId}",
                Text = pc.Text,
                TaggedContent = "",
                Metadata = new ChunkMetadata
                {
                    PageStart = pc.PageStart,
                    PageEnd = pc.PageEnd,
                    ElementIds = pc.BoxIndices.Select(b => ChunkIds.ElementId(b.page, b.box)).ToList(),
                    Types = (pc.Types != null && pc.Types.Count > 0)
                        ? pc.Types
                        : new List<string> { pc.PrimaryType ?? "paragraph" },
                    TokenCount = pc.TokenCount,
                    Bboxes = pc.Bboxes,
                    Lists = GroupListItems(pc.Sentences),
                    Ocr = pc.Sentences.Any(s => _ocrPages.Contains(s.PageNo)),
                    FilePath = _doc?.Filename,
                    PageCount = _doc?.PageCount,
                },
            };
        }

        (List<TableChunk>, List<FigureChunk>, List<SectionChunk>) BuildViews(
            List<ProtoChunk> protoChunks, List<Chunk> chunks)
        {
            var tables = new List<TableChunk>();
            var figures = new List<FigureChunk>();
            var sections = new List<SectionChunk>();
            var seenFigures = new Dictionary<int, FigureChunk>();
            var openSections = new List<(int level, SectionChunk section)>();

            var boxOffset = new Dictionary<int, int>();
            int totalBoxes = 0;
            if (_doc?.Pages != null)
            {
                foreach (var page in _doc.Pages)
                {
                    boxOffset[page.PageNumber] = totalBoxes;
                    totalBoxes += page.Boxes?.Count ?? 0;
                }
            }

            void Link(SectionChunk section, Chunk chunk)
            {
                if (!section.ChildChunkIds.Contains(chunk.Id))
                {
                    section.ChildChunkIds.Add(chunk.Id);
                    section.TokenCount += chunk.Metadata.TokenCount;
                }
            }

            for (int pci = 0; pci < protoChunks.Count; pci++)
            {
                var pc = protoChunks[pci];
                var chunk = chunks[pci];
                for (int idx = 0; idx < pc.Sentences.Count; idx++)
                {
                    var s = pc.Sentences[idx];
                    if (s.IsTableContent)
                    {
                        var cap = NearestCaption(pc.Sentences, idx, "table");
                        var table = new TableChunk
                        {
                            Id = $"t{tables.Count}",
                            ChunkId = chunk.Id,
                            ElementId = ChunkIds.ElementId(s.PageNo, s.BoxIndex),
                            Page = s.PageNo,
                            Bbox = s.Bbox,
                            Markdown = s.TableMarkdown,
                            Html = s.TableHtml,
                            Headers = TextSource.ExtractTableHeaders(s.TableHtml),
                            Caption = cap?.Text,
                            CaptionElementId = cap != null ? ChunkIds.ElementId(cap.PageNo, cap.BoxIndex) : null,
                            TokenCount = s.TokenCount,
                        };
                        tables.Add(table);
                        chunk.Metadata.TableIds.Add(table.Id);
                    }
                    else if (s.IsFigureRelated && s.FigureNumber != null)
                    {
                        if (!seenFigures.TryGetValue(s.FigureNumber.Value, out var fig))
                        {
                            string placeholder = (s.Text ?? "").StartsWith("[Figure", StringComparison.Ordinal) ? s.Text : "";
                            var cap = NearestCaption(pc.Sentences, idx, "figure");
                            fig = new FigureChunk
                            {
                                Id = $"f{s.FigureNumber.Value}",
                                ChunkId = chunk.Id,
                                ElementId = ChunkIds.ElementId(s.PageNo, s.BoxIndex),
                                Page = s.PageNo,
                                Bbox = s.Bbox,
                                BoxClass = s.BoxClass,
                                OcrText = string.IsNullOrEmpty(placeholder) ? s.Text : null,
                                Placeholder = placeholder,
                                Caption = cap?.Text,
                                CaptionElementId = cap != null ? ChunkIds.ElementId(cap.PageNo, cap.BoxIndex) : null,
                                Image = s.ImageData,
                            };
                            seenFigures[s.FigureNumber.Value] = fig;
                            figures.Add(fig);
                            chunk.Metadata.FigureIds.Add(fig.Id);
                        }
                        else
                        {
                            if (!(s.Text ?? "").StartsWith("[Figure", StringComparison.Ordinal))
                                fig.OcrText = string.IsNullOrEmpty(fig.OcrText) ? s.Text : (fig.OcrText + "\n" + s.Text).Trim();
                            if (!chunk.Metadata.FigureIds.Contains(fig.Id))
                                chunk.Metadata.FigureIds.Add(fig.Id);
                        }
                    }
                    else if (s.IsHeadingHint)
                    {
                        int level = s.HeadingLevelHint ?? 1;
                        while (openSections.Count > 0 && openSections[openSections.Count - 1].level >= level)
                            openSections.RemoveAt(openSections.Count - 1);
                        string title = MdDecorTrailRe.Replace(MdDecorRe.Replace(s.Text ?? "", ""), "").Trim();
                        int offset = boxOffset.TryGetValue(s.PageNo, out int off) ? off : 0;
                        var section = new SectionChunk
                        {
                            Id = $"s{sections.Count}",
                            Title = title,
                            Level = level,
                            PageStart = s.PageNo,
                            PageEnd = s.PageNo,
                            HeadingElementId = ChunkIds.ElementId(s.PageNo, s.BoxIndex),
                            Path = openSections.Select(os => os.section.Title).Concat(new[] { title }).ToList(),
                            ElementSpan = new[] { offset + s.BoxIndex, totalBoxes },
                        };
                        sections.Add(section);
                        Link(section, chunk);
                        openSections.Add((level, section));
                    }
                }

                foreach (var os in openSections)
                {
                    Link(os.section, chunk);
                    os.section.PageEnd = Math.Max(os.section.PageEnd, pc.PageEnd);
                }
                if (openSections.Count > 0)
                {
                    var innermost = openSections[openSections.Count - 1].section;
                    chunk.Metadata.SectionId = innermost.Id;
                    chunk.Metadata.SectionPath = new List<string>(innermost.Path);
                }
            }

            for (int i = 0; i < sections.Count; i++)
            {
                int end = totalBoxes;
                for (int k = i + 1; k < sections.Count; k++)
                {
                    if (sections[k].Level <= sections[i].Level)
                    {
                        end = sections[k].ElementSpan[0];
                        break;
                    }
                }
                sections[i].ElementSpan = new[] { sections[i].ElementSpan[0], end };
            }

            var unchunked = UnchunkedTables(protoChunks);
            if (unchunked.Count > 0)
                tables = MergeUnchunkedTables(tables, unchunked, chunks);

            var chunkById = chunks.ToDictionary(c => c.Id);
            foreach (var item in tables)
            {
                if (item.ChunkId != null && chunkById.TryGetValue(item.ChunkId, out var owner))
                    item.SectionId = owner.Metadata.SectionId;
            }
            foreach (var item in figures)
            {
                if (item.ChunkId != null && chunkById.TryGetValue(item.ChunkId, out var owner))
                    item.SectionId = owner.Metadata.SectionId;
            }
            return (tables, figures, sections);
        }

        List<TableChunk> UnchunkedTables(List<ProtoChunk> protoChunks)
        {
            var chunked = new HashSet<(int, int)>();
            foreach (var pc in protoChunks)
            {
                foreach (var s in pc.Sentences)
                {
                    if (s.IsTableContent)
                        chunked.Add((s.PageNo, s.BoxIndex));
                }
            }
            var missing = new List<TableChunk>();
            if (_doc?.Pages == null)
                return missing;
            foreach (var page in _doc.Pages)
            {
                var boxes = page.Boxes ?? new List<LayoutBox>();
                for (int boxIdx = 0; boxIdx < boxes.Count; boxIdx++)
                {
                    var box = boxes[boxIdx];
                    if (box.BoxClass != "table" && box.BoxClass != "table-fallback")
                        continue;
                    if (chunked.Contains((page.PageNumber, boxIdx)))
                        continue;
                    var (markdown, html) = TextSource.TableContent(box);
                    if (markdown == null && html == null)
                        markdown = "";
                    missing.Add(new TableChunk
                    {
                        Id = "",
                        ChunkId = null,
                        ElementId = ChunkIds.ElementId(page.PageNumber, boxIdx),
                        Page = page.PageNumber,
                        Bbox = new[] { (double)box.X0, box.Y0, box.X1, box.Y1 },
                        Markdown = markdown,
                        Html = html,
                        Headers = TextSource.ExtractTableHeaders(html),
                    });
                }
            }
            return missing;
        }

        static List<TableChunk> MergeUnchunkedTables(List<TableChunk> tables, List<TableChunk> unchunked, List<Chunk> chunks)
        {
            var merged = tables.Concat(unchunked)
                .OrderBy(t => t.Page)
                .ThenBy(t =>
                {
                    string id = t.ElementId ?? "";
                    int dot = id.LastIndexOf(".b", StringComparison.Ordinal);
                    return dot >= 0 && int.TryParse(id.Substring(dot + 2), out int b) ? b : 0;
                })
                .ToList();
            for (int i = 0; i < merged.Count; i++)
                merged[i].Id = $"t{i}";
            var byId = chunks.ToDictionary(c => c.Id);
            foreach (var chunk in chunks)
                chunk.Metadata.TableIds.Clear();
            foreach (var table in merged)
            {
                if (!string.IsNullOrEmpty(table.ChunkId) && byId.TryGetValue(table.ChunkId, out var owner))
                    owner.Metadata.TableIds.Add(table.Id);
            }
            return merged;
        }

        static string BuildTaggedContent(ProtoChunk pc, List<string> hierarchy)
        {
            var parts = new List<string>();
            if (hierarchy != null && hierarchy.Count > 0)
                parts.Add("[Section] " + string.Join(" > ", hierarchy));
            if (pc.PageStart == pc.PageEnd)
                parts.Add($"[Page] {pc.PageStart}");
            else
                parts.Add($"[Pages] {pc.PageStart}-{pc.PageEnd}");
            var nonPara = (pc.Types ?? new List<string>()).Where(t => t != "paragraph").ToList();
            if (nonPara.Count > 0)
                parts.Add("[Type] " + string.Join(", ", nonPara));
            parts.Add($"[Markdown]\n{pc.Text}");
            return string.Join("\n", parts);
        }

        static SentenceUnit NearestCaption(List<SentenceUnit> sents, int idx, string targetType)
        {
            SentenceUnit best = null;
            int bestDist = int.MaxValue;
            for (int j = 0; j < sents.Count; j++)
            {
                var s = sents[j];
                if (!s.IsCaption)
                    continue;
                if (s.CaptionTargetType != null && s.CaptionTargetType != targetType)
                    continue;
                int dist = Math.Abs(j - idx);
                if (dist < bestDist)
                {
                    best = s;
                    bestDist = dist;
                }
            }
            return best;
        }

        static List<Dictionary<string, object>> GroupListItems(List<SentenceUnit> sents)
        {
            var groups = new List<Dictionary<string, object>>();
            var current = new List<SentenceUnit>();
            foreach (var s in sents)
            {
                if (s.IsListItem)
                    current.Add(s);
                else if (current.Count > 0)
                {
                    groups.Add(FinalizeListGroup(current));
                    current.Clear();
                }
            }
            if (current.Count > 0)
                groups.Add(FinalizeListGroup(current));
            return groups;
        }

        static Dictionary<string, object> FinalizeListGroup(List<SentenceUnit> items)
        {
            var entries = items.Select(s => new Dictionary<string, object>
            {
                ["text"] = s.Text,
                ["page"] = s.PageNo,
                ["bbox"] = s.Bbox,
            }).ToList();
            var byPage = new SortedDictionary<int, List<double[]>>();
            foreach (var s in items)
            {
                if (!byPage.TryGetValue(s.PageNo, out var list))
                {
                    list = new List<double[]>();
                    byPage[s.PageNo] = list;
                }
                list.Add(s.Bbox);
            }
            var bboxes = new List<double[]>();
            foreach (var kv in byPage)
            {
                var u = ChunkGeometry.UnionBbox(kv.Value);
                bboxes.Add(new[] { (double)kv.Key, u[0], u[1], u[2], u[3] });
            }
            return new Dictionary<string, object> { ["items"] = entries, ["bboxes"] = bboxes };
        }
    }
}
