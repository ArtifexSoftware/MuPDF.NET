using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MuPDF.NET.PDF4LLM.Helpers;
using MuPDF.NET.PDF4LLM.Helpers.Chunking;
using Newtonsoft.Json;

namespace MuPDF.NET
{
    /// <summary>One layout box, addressable as <c>p{page}.b{box}</c>.</summary>
    public sealed class ChunkElement
    {
        public string Id { get; set; }
        public int Page { get; set; }
        public int Box { get; set; }
        public string BoxClass { get; set; }
        public double[] Bbox { get; set; }
        public string Text { get; set; }
        public bool IsHeaderFooter { get; set; }
    }

    /// <summary>A table exposed in the <see cref="ChunkedDocument.Tables"/> view.</summary>
    public sealed class TableChunk
    {
        public string Id { get; set; }
        public string ChunkId { get; set; }
        public string ElementId { get; set; }
        public int Page { get; set; }
        public double[] Bbox { get; set; }
        public string Markdown { get; set; }
        public string Html { get; set; }
        public List<string> Headers { get; set; } = new List<string>();
        public string Caption { get; set; }
        public string CaptionElementId { get; set; }
        public string SectionId { get; set; }
        public int TokenCount { get; set; }

        public string Text => Html ?? Markdown ?? "";
    }

    /// <summary>A figure/formula exposed in the <see cref="ChunkedDocument.Figures"/> view.</summary>
    public sealed class FigureChunk
    {
        public string Id { get; set; }
        public string ChunkId { get; set; }
        public string ElementId { get; set; }
        public int Page { get; set; }
        public double[] Bbox { get; set; }
        public string BoxClass { get; set; } = "picture";
        public string OcrText { get; set; }
        public string Placeholder { get; set; } = "";
        public string Caption { get; set; }
        public string CaptionElementId { get; set; }
        public string SectionId { get; set; }
        public byte[] Image { get; set; }

        public bool HasText => !string.IsNullOrWhiteSpace(OcrText);
    }

    /// <summary>A heading span exposed in the <see cref="ChunkedDocument.Sections"/> view.</summary>
    public sealed class SectionChunk
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int Level { get; set; }
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public string HeadingElementId { get; set; }
        public List<string> Path { get; set; } = new List<string>();
        public int[] ElementSpan { get; set; } = new[] { 0, 0 };
        public List<string> ChildChunkIds { get; set; } = new List<string>();
        public int TokenCount { get; set; }

        internal ChunkElement[] BoundElements { get; set; } = Array.Empty<ChunkElement>();

        public string Text
        {
            get
            {
                var parts = new List<string>();
                foreach (var e in BoundElements)
                {
                    if (e != null && !string.IsNullOrEmpty(e.Text) && !e.IsHeaderFooter)
                        parts.Add(e.Text);
                }
                return string.Join("\n\n", parts);
            }
        }
    }

    /// <summary>A node in <see cref="ChunkedDocument.Hierarchy"/> (root has level 0).</summary>
    public sealed class SectionNode
    {
        public string Title { get; set; } = "";
        public int Level { get; set; }
        public string SectionId { get; set; }
        public List<SectionNode> Children { get; set; } = new List<SectionNode>();
    }

    /// <summary>Metadata attached to a retrieval chunk.</summary>
    public sealed class ChunkMetadata
    {
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public List<double[]> Bboxes { get; set; } = new List<double[]>();
        public List<string> Types { get; set; } = new List<string>();
        public string SectionId { get; set; }
        public List<string> SectionPath { get; set; } = new List<string>();
        public int TokenCount { get; set; }
        public List<string> ElementIds { get; set; } = new List<string>();
        public List<string> TableIds { get; set; } = new List<string>();
        public List<string> FigureIds { get; set; } = new List<string>();
        public List<Dictionary<string, object>> Lists { get; set; } = new List<Dictionary<string, object>>();
        public bool Ocr { get; set; }
        public string FilePath { get; set; }
        public int? PageCount { get; set; }
    }

    /// <summary>A finalized retrieval-ready chunk (<c>c{n}</c>).</summary>
    public sealed class Chunk
    {
        static readonly Regex WsRe = new Regex(@"\s+", RegexOptions.Compiled);

        public string Id { get; set; }
        public string Text { get; set; }
        public string TaggedContent { get; set; } = "";
        public ChunkMetadata Metadata { get; set; } = new ChunkMetadata();

        string _contentHash;

        public string ContentHash
        {
            get
            {
                if (_contentHash == null)
                {
                    string norm = WsRe.Replace(Text ?? "", " ").Trim();
                    using (var sha = SHA256.Create())
                    {
                        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(norm));
                        var sb = new StringBuilder(hash.Length * 2);
                        foreach (byte b in hash)
                            sb.Append(b.ToString("x2"));
                        _contentHash = sb.ToString();
                    }
                }
                return _contentHash;
            }
        }
    }

    /// <summary>
    /// Return type of <see cref="Document.ToChunks"/> / layout-main <c>to_chunks</c>.
    /// Sequence of chunks plus element registry and table/figure/section views.
    /// </summary>

    public sealed class ChunkedDocument : IReadOnlyList<Chunk>
    {
        public static readonly HashSet<string> ReassemblyParams = new HashSet<string>
        {
            "max_tokens", "min_tokens", "breakpoint_threshold",
            "table_mode", "merge_small_chunks", "respect_section_starts",
        };

        readonly Chunk[] _chunks;
        readonly ParsedDocument _doc;
        readonly List<SentenceUnit> _allUnits;
        readonly int _headerFooterExcluded;
        readonly Dictionary<string, object> _params;
        string _text;
        Dictionary<string, object> _byId;
        SectionNode _hierarchy;

        public ChunkedDocument(
            IEnumerable<Chunk> chunks,
            IEnumerable<ChunkElement> elements = null,
            IEnumerable<TableChunk> tables = null,
            IEnumerable<FigureChunk> figures = null,
            IEnumerable<SectionChunk> sections = null,
            Dictionary<string, object> parms = null,
            ParsedDocument doc = null)
            : this(chunks, elements, tables, figures, sections, parms, doc, null, 0)
        {
        }

        internal ChunkedDocument(
            IEnumerable<Chunk> chunks,
            IEnumerable<ChunkElement> elements,
            IEnumerable<TableChunk> tables,
            IEnumerable<FigureChunk> figures,
            IEnumerable<SectionChunk> sections,
            Dictionary<string, object> parms,
            ParsedDocument doc,
            IEnumerable<SentenceUnit> allUnits,
            int headerFooterExcluded)
        {
            _chunks = chunks?.ToArray() ?? Array.Empty<Chunk>();
            Elements = elements?.ToList() ?? new List<ChunkElement>();
            Tables = tables?.ToList() ?? new List<TableChunk>();
            Figures = figures?.ToList() ?? new List<FigureChunk>();
            Sections = sections?.ToList() ?? new List<SectionChunk>();
            _params = parms != null ? new Dictionary<string, object>(parms) : new Dictionary<string, object>();
            _doc = doc;
            _allUnits = allUnits?.ToList() ?? new List<SentenceUnit>();
            _headerFooterExcluded = headerFooterExcluded;

            foreach (var s in Sections)
            {
                int lo = s.ElementSpan != null && s.ElementSpan.Length > 0 ? s.ElementSpan[0] : 0;
                int hi = s.ElementSpan != null && s.ElementSpan.Length > 1 ? s.ElementSpan[1] : lo;
                if (lo < 0) lo = 0;
                if (hi < lo) hi = lo;
                if (hi > Elements.Count) hi = Elements.Count;
                if (lo > Elements.Count) lo = Elements.Count;
                s.BoundElements = Elements.Skip(lo).Take(Math.Max(0, hi - lo)).ToArray();
            }
        }

        public IReadOnlyList<Chunk> Chunks => _chunks;
        public List<ChunkElement> Elements { get; }
        public List<TableChunk> Tables { get; }
        public List<FigureChunk> Figures { get; }
        public List<SectionChunk> Sections { get; }
        public IReadOnlyDictionary<string, object> Params => _params;

        public int Count => _chunks.Length;
        public Chunk this[int index] => _chunks[index];
        IEnumerator<Chunk> IEnumerable<Chunk>.GetEnumerator() => ((IEnumerable<Chunk>)_chunks).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _chunks.GetEnumerator();

        public override string ToString() =>
            $"ChunkedDocument(chunks={_chunks.Length}, tables={Tables.Count}, figures={Figures.Count}, sections={Sections.Count})";

        public string Text
        {
            get
            {
                if (_text == null)
                    _text = string.Join("\n\n", _chunks.Select(c => c.Text ?? ""));
                return _text;
            }
        }

        public SectionNode Hierarchy
        {
            get
            {
                if (_hierarchy == null)
                {
                    var root = new SectionNode();
                    var stack = new List<SectionNode> { root };
                    foreach (var s in Sections)
                    {
                        var node = new SectionNode { Title = s.Title, Level = s.Level, SectionId = s.Id };
                        while (stack.Count > 1 && stack[stack.Count - 1].Level >= s.Level)
                            stack.RemoveAt(stack.Count - 1);
                        stack[stack.Count - 1].Children.Add(node);
                        stack.Add(node);
                    }
                    _hierarchy = root;
                }
                return _hierarchy;
            }
        }

        public object Get(string id, object defaultValue = null, bool throwIfMissing = true)
        {
            if (_byId == null)
            {
                var index = new Dictionary<string, object>();
                foreach (var c in _chunks)
                    index[c.Id] = c;
                foreach (var group in new IEnumerable<object>[] { Tables, Figures, Sections })
                {
                    foreach (var item in group)
                    {
                        string itemId = item is TableChunk t ? t.Id
                            : item is FigureChunk f ? f.Id
                            : item is SectionChunk s ? s.Id
                            : null;
                        if (!string.IsNullOrEmpty(itemId))
                            index[itemId] = item;
                    }
                }
                foreach (var el in Elements)
                    index[el.Id] = el;
                _byId = index;
            }

            if (!string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out object found))
                return found;
            if (throwIfMissing && defaultValue == null)
                throw new KeyNotFoundException(id);
            return defaultValue;
        }

        public List<Dictionary<string, object>> ToDicts(bool includeTagged = true)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var c in _chunks)
                list.Add(ChunkToDict(c, includeTagged));
            return list;
        }

        public string ToJson(bool includeTagged = true)
        {
            return JsonConvert.SerializeObject(ToDicts(includeTagged), new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
            });
        }

        public ChunkedDocument ReassembleChunks(
            int? maxTokens = null,
            int? minTokens = null,
            double? breakpointThreshold = null,
            string tableMode = null,
            bool? mergeSmallChunks = null,
            bool? respectSectionStarts = null)
        {
            var merged = new Dictionary<string, object>(_params);
            if (maxTokens.HasValue) merged["max_tokens"] = maxTokens.Value;
            if (minTokens.HasValue) merged["min_tokens"] = minTokens.Value;
            if (breakpointThreshold.HasValue) merged["breakpoint_threshold"] = breakpointThreshold.Value;
            if (tableMode != null) merged["table_mode"] = tableMode;
            if (mergeSmallChunks.HasValue) merged["merge_small_chunks"] = mergeSmallChunks.Value;
            if (respectSectionStarts.HasValue) merged["respect_section_starts"] = respectSectionStarts.Value;
            return LayoutChunker.ChunkUnits(_doc, _allUnits, merged);
        }

        public Dictionary<string, object> Diagnostics
        {
            get
            {
                var pages = _doc?.Pages?.Select(p => p.PageNumber).ToList() ?? new List<int>();
                var pagesWithChunks = new HashSet<int>();
                foreach (var c in _chunks)
                    pagesWithChunks.UnionWith(ChunkPages(c));

                var zero = new List<string>();
                if (_chunks.Length == 0)
                {
                    if (_allUnits.Count == 0)
                        zero.Add("no_text_extracted");
                    else if (_allUnits.All(u => u.IsHeaderFooter))
                        zero.Add("all_units_header_footer");
                    else
                        zero.Add("all_units_filtered");
                }

                return new Dictionary<string, object>
                {
                    ["chunk_count"] = _chunks.Length,
                    ["element_count"] = Elements.Count,
                    ["table_count"] = Tables.Count,
                    ["figure_count"] = Figures.Count,
                    ["section_count"] = Sections.Count,
                    ["page_count"] = pages.Count,
                    ["pages_without_chunks"] = pages.Where(p => !pagesWithChunks.Contains(p)).OrderBy(p => p).ToList(),
                    ["zero_chunk_causes"] = zero,
                    ["figures_without_text"] = Figures.Where(f => !f.HasText).Select(f => f.Id).ToList(),
                    ["degenerate_tables"] = Tables.Where(t => string.IsNullOrWhiteSpace(t.Text)).Select(t => t.Id).ToList(),
                    ["header_footer_excluded"] = _headerFooterExcluded,
                };
            }
        }

        static HashSet<int> ChunkPages(Chunk chunk)
        {
            var pages = new HashSet<int>();
            if (chunk?.Metadata?.Bboxes != null)
            {
                foreach (var b in chunk.Metadata.Bboxes)
                {
                    if (b != null && b.Length > 0)
                        pages.Add((int)b[0]);
                }
            }
            if (chunk?.Metadata?.ElementIds != null)
            {
                foreach (var element in chunk.Metadata.ElementIds)
                {
                    if (string.IsNullOrEmpty(element))
                        continue;
                    int dot = element.IndexOf(".b", StringComparison.Ordinal);
                    string pagePart = dot >= 0 ? element.Substring(0, dot) : element;
                    if (pagePart.Length > 1 && int.TryParse(pagePart.Substring(1), out int pno))
                        pages.Add(pno);
                }
            }
            if (pages.Count == 0 && chunk?.Metadata != null)
            {
                for (int p = chunk.Metadata.PageStart; p <= chunk.Metadata.PageEnd; p++)
                    pages.Add(p);
            }
            return pages;
        }

        static Dictionary<string, object> ChunkToDict(Chunk chunk, bool includeTagged)
        {
            var meta = chunk.Metadata ?? new ChunkMetadata();
            var d = new Dictionary<string, object>
            {
                ["id"] = chunk.Id,
                ["text"] = chunk.Text,
                ["content_hash"] = chunk.ContentHash,
                ["metadata"] = new Dictionary<string, object>
                {
                    ["page_start"] = meta.PageStart,
                    ["page_end"] = meta.PageEnd,
                    ["bboxes"] = meta.Bboxes,
                    ["types"] = meta.Types,
                    ["section_id"] = meta.SectionId,
                    ["section_path"] = meta.SectionPath,
                    ["token_count"] = meta.TokenCount,
                    ["element_ids"] = meta.ElementIds,
                    ["table_ids"] = meta.TableIds,
                    ["figure_ids"] = meta.FigureIds,
                    ["lists"] = meta.Lists,
                    ["ocr"] = meta.Ocr,
                    ["file_path"] = meta.FilePath,
                    ["page_count"] = meta.PageCount,
                },
            };
            if (includeTagged)
                d["tagged_content"] = chunk.TaggedContent;
            return d;
        }
    }
}
