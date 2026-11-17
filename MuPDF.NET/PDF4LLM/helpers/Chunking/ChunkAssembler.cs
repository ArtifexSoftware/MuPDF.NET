using System;
using System.Collections.Generic;
using System.Linq;

namespace MuPDF.NET.PDF4LLM.Helpers.Chunking
{
    internal sealed class ChunkAssembler
    {
        static readonly HashSet<string> ElementTypes = new HashSet<string> { "table", "figure", "list" };

        readonly int _maxTokens;
        readonly int _minTokens;
        readonly double _threshold;
        readonly string _tableMode;
        readonly bool _mergeSmall;
        readonly bool _respectSectionStarts;

        public ChunkAssembler(
            int maxTokens = 400,
            int minTokens = 120,
            double threshold = 0.5,
            string tableMode = "preserve",
            bool mergeSmallChunks = true,
            bool respectSectionStarts = true)
        {
            _maxTokens = maxTokens;
            _minTokens = minTokens;
            _threshold = threshold;
            _tableMode = tableMode;
            _mergeSmall = mergeSmallChunks;
            _respectSectionStarts = respectSectionStarts;
        }

        public List<ProtoChunk> Assemble(List<SentenceUnit> sents, List<double> scores)
        {
            if (sents == null || sents.Count == 0)
                return new List<ProtoChunk>();
            var chunks = new List<ProtoChunk>();
            var current = new List<SentenceUnit> { sents[0] };
            int currentTokens = sents[0].TokenCount;
            for (int i = 0; i < scores.Count; i++)
            {
                var next = sents[i + 1];
                bool doBreak = scores[i] > _threshold;
                if (!doBreak)
                    doBreak = IsStructuralBreak(current[current.Count - 1], next);
                if (!doBreak)
                    doBreak = currentTokens + next.TokenCount > _maxTokens;
                if (doBreak && next.BoxIndex == current[current.Count - 1].BoxIndex
                    && next.PageNo == current[current.Count - 1].PageNo)
                    doBreak = false;
                if (doBreak)
                {
                    chunks.Add(MakeProtoChunk(chunks.Count, current));
                    current = new List<SentenceUnit> { next };
                    currentTokens = next.TokenCount;
                }
                else
                {
                    current.Add(next);
                    currentTokens += next.TokenCount;
                }
            }
            if (current.Count > 0)
                chunks.Add(MakeProtoChunk(chunks.Count, current));
            return chunks;
        }

        public List<ProtoChunk> Refine(List<ProtoChunk> chunks)
        {
            var split = new List<ProtoChunk>();
            foreach (var chunk in chunks)
            {
                if (chunk.TokenCount > _maxTokens)
                    split.AddRange(SplitChunk(chunk));
                else
                    split.Add(chunk);
            }
            if (!_mergeSmall)
                return Renumber(split);
            var semantic = MergeSemantic(split);
            return Renumber(MergeBudget(semantic));
        }

        bool IsStructuralBreak(SentenceUnit prev, SentenceUnit next)
        {
            if (IsCaptionElementPair(prev, next))
                return false;
            if (IsHeadingElementPair(prev, next))
                return false;
            if (next.IsHeaderFooter != prev.IsHeaderFooter)
                return true;
            if (next.IsTableContent != prev.IsTableContent)
                return true;
            if (next.IsFigureRelated != prev.IsFigureRelated)
                return true;
            if (next.IsHeadingHint)
                return true;
            return false;
        }

        static bool IsHeadingElementPair(SentenceUnit prev, SentenceUnit next)
        {
            if (!prev.IsHeadingHint || prev.PageNo != next.PageNo)
                return false;
            if (!(next.IsTableContent || next.IsFigureRelated))
                return false;
            return ChunkGeometry.HorizontalOverlapRatio(prev.Bbox, next.Bbox) >= 0.3;
        }

        static bool IsCaptionElementPair(SentenceUnit a, SentenceUnit b)
        {
            if (a.PageNo != b.PageNo)
                return false;
            if (ChunkGeometry.HorizontalOverlapRatio(a.Bbox, b.Bbox) < 0.3)
                return false;
            return ChunkGeometry.CaptionMatchesElement(a, b) || ChunkGeometry.CaptionMatchesElement(b, a);
        }

        List<ProtoChunk> SplitChunk(ProtoChunk chunk)
        {
            var sents = chunk.Sentences;
            if (sents == null || sents.Count == 0)
                return new List<ProtoChunk> { chunk };
            if (chunk.PrimaryType == "table" && _tableMode == "preserve")
                return new List<ProtoChunk> { chunk };
            var result = new List<ProtoChunk>();
            var current = new List<SentenceUnit>();
            int currentTokens = 0;
            foreach (var sent in sents)
            {
                if (sent.TokenCount > _maxTokens)
                {
                    if (current.Count > 0)
                    {
                        result.Add(MakeProtoChunk(0, current));
                        current = new List<SentenceUnit>();
                        currentTokens = 0;
                    }
                    result.Add(MakeProtoChunk(0, new List<SentenceUnit> { sent }));
                    continue;
                }
                if (currentTokens + sent.TokenCount > _maxTokens && current.Count > 0)
                {
                    int splitPoint = current.Count;
                    if (sent.BoxIndex == current[current.Count - 1].BoxIndex
                        && sent.PageNo == current[current.Count - 1].PageNo)
                    {
                        int bi = sent.BoxIndex, pg = sent.PageNo, j = current.Count - 1;
                        while (j > 0 && current[j].BoxIndex == bi && current[j].PageNo == pg)
                            j--;
                        if (j > 0)
                        {
                            int carryTokens = current.Skip(j + 1).Sum(s => s.TokenCount);
                            if (carryTokens + sent.TokenCount <= _maxTokens)
                                splitPoint = j + 1;
                        }
                    }
                    var keep = current.Take(splitPoint).ToList();
                    var carry = current.Skip(splitPoint).ToList();
                    if (keep.Count > 0)
                        result.Add(MakeProtoChunk(0, keep));
                    current = carry;
                    current.Add(sent);
                    currentTokens = current.Sum(s => s.TokenCount);
                }
                else
                {
                    current.Add(sent);
                    currentTokens += sent.TokenCount;
                }
            }
            if (current.Count > 0)
                result.Add(MakeProtoChunk(0, current));
            return result;
        }

        List<ProtoChunk> MergeSemantic(List<ProtoChunk> chunks)
        {
            if (chunks.Count <= 1)
                return chunks;
            var result = new List<ProtoChunk>();
            int i = 0;
            while (i < chunks.Count)
            {
                var chunk = chunks[i];
                if (i + 1 < chunks.Count && IsSemanticPair(chunk, chunks[i + 1]))
                {
                    var combined = new List<SentenceUnit>(chunk.Sentences);
                    int j = i + 1;
                    while (j < chunks.Count && IsSemanticPair(MakeProtoChunk(0, combined), chunks[j]))
                    {
                        combined.AddRange(chunks[j].Sentences);
                        j++;
                    }
                    var pc = MakeProtoChunk(0, combined);
                    if (pc.Bboxes.Count > 1)
                        pc.Bboxes = UnionPerPage(pc.Bboxes);
                    result.Add(pc);
                    i = j;
                }
                else
                {
                    result.Add(chunk);
                    i++;
                }
            }
            return result;
        }

        bool IsSemanticPair(ProtoChunk a, ProtoChunk b)
        {
            if (a.PrimaryType == "header_footer" || b.PrimaryType == "header_footer")
                return false;
            if (_respectSectionStarts && b.Sentences.Count > 0 && b.Sentences[0].IsHeadingHint)
                return false;
            if (a.TokenCount + b.TokenCount > _maxTokens)
                return false;
            if (Math.Abs(a.PageEnd - b.PageStart) > 1)
                return false;
            if (!AnyBboxOverlap(a.Bboxes, b.Bboxes))
                return false;
            if (a.PrimaryType == "heading" && b.PrimaryType != "heading")
                return true;
            if (a.PrimaryType == "list" && b.PrimaryType == "list")
                return true;
            bool aIsCaption = a.Sentences.Count > 0 && a.Sentences.All(s => s.IsCaption);
            if (aIsCaption && ElementTypes.Contains(b.PrimaryType ?? ""))
                return CaptionTargetMatches(a, b.PrimaryType);
            bool bIsCaption = b.Sentences.Count > 0 && b.Sentences.All(s => s.IsCaption);
            if (ElementTypes.Contains(a.PrimaryType ?? "") && bIsCaption)
                return CaptionTargetMatches(b, a.PrimaryType);
            return false;
        }

        static bool CaptionTargetMatches(ProtoChunk captionChunk, string elementType)
        {
            var targets = new HashSet<string>();
            foreach (var s in captionChunk.Sentences)
            {
                if (!string.IsNullOrEmpty(s.CaptionTargetType))
                    targets.Add(s.CaptionTargetType);
            }
            if (targets.Count == 0)
                return true;
            return targets.Contains(elementType);
        }

        List<ProtoChunk> MergeBudget(List<ProtoChunk> chunks)
        {
            if (chunks.Count <= 1)
                return chunks;
            var merged = new List<ProtoChunk> { chunks[0] };
            for (int i = 1; i < chunks.Count; i++)
            {
                var prev = merged[merged.Count - 1];
                var curr = chunks[i];
                if (CanBudgetMerge(prev, curr))
                {
                    var combined = prev.Sentences.Concat(curr.Sentences).ToList();
                    var pc = MakeProtoChunk(0, combined);
                    pc.Bboxes = RegroupPageBboxes(prev.Bboxes.Concat(curr.Bboxes));
                    merged[merged.Count - 1] = pc;
                }
                else
                    merged.Add(curr);
            }
            return merged;
        }

        bool CanBudgetMerge(ProtoChunk a, ProtoChunk b)
        {
            if (a.PrimaryType == "header_footer" || b.PrimaryType == "header_footer")
                return false;
            if (b.PrimaryType == "heading")
                return false;
            if (_respectSectionStarts && b.Sentences.Count > 0 && b.Sentences[0].IsHeadingHint)
                return false;
            if (_tableMode == "isolate" && (a.PrimaryType == "table" || b.PrimaryType == "table"))
                return false;
            if (a.TokenCount + b.TokenCount > _maxTokens)
                return false;
            if (_minTokens > 0 && a.TokenCount >= _minTokens && b.TokenCount >= _minTokens)
                return false;
            return true;
        }

        static bool AnyBboxOverlap(List<double[]> a, List<double[]> b, double minRatio = 0.3)
        {
            foreach (var ba in a)
            {
                double[] ba4 = ba.Length == 5 ? new[] { ba[1], ba[2], ba[3], ba[4] } : ba;
                foreach (var bb in b)
                {
                    double[] bb4 = bb.Length == 5 ? new[] { bb[1], bb[2], bb[3], bb[4] } : bb;
                    if (ChunkGeometry.HorizontalOverlapRatio(ba4, bb4) >= minRatio)
                        return true;
                }
            }
            return false;
        }

        ProtoChunk MakeProtoChunk(int chunkId, List<SentenceUnit> sents)
        {
            var (primary, allTypes) = InferChunkTypes(sents);
            var boxIndices = new List<(int, int)>();
            var seen = new HashSet<(int, int)>();
            foreach (var s in sents)
            {
                var key = (s.PageNo, s.BoxIndex);
                if (seen.Add(key))
                    boxIndices.Add(key);
            }
            return new ProtoChunk
            {
                ChunkId = chunkId,
                SentIds = sents.Select(s => s.SentId).ToList(),
                TokenCount = sents.Sum(s => s.TokenCount),
                UnitRange = new[] { sents[0].SentId, sents[sents.Count - 1].SentId + 1 },
                PageStart = sents[0].PageNo,
                PageEnd = sents[sents.Count - 1].PageNo,
                BoxIndices = boxIndices,
                Bboxes = GroupBboxesByPage(sents),
                PrimaryType = primary,
                Types = allTypes,
                Sentences = new List<SentenceUnit>(sents),
            };
        }

        static (string primary, List<string> all) InferChunkTypes(List<SentenceUnit> sents)
        {
            var seen = new HashSet<string>();
            var all = new List<string>();
            foreach (var s in sents)
            {
                string t = s.IsHeaderFooter ? "header_footer"
                    : s.IsTableContent ? "table"
                    : s.IsFigureRelated ? "figure"
                    : s.IsListItem ? "list"
                    : s.IsHeadingHint ? "heading"
                    : s.IsCaption ? "caption"
                    : s.IsFootnote ? "footnote"
                    : "paragraph";
                if (seen.Add(t))
                    all.Add(t);
            }
            foreach (var et in new[] { "table", "figure", "list" })
            {
                if (seen.Contains(et))
                    return (et, all);
            }
            if (seen.Contains("header_footer"))
                return ("header_footer", all);
            if (seen.Contains("heading") && sents.Count == 1)
                return ("heading", all);
            if (seen.Contains("footnote"))
                return ("footnote", all);
            return ("paragraph", all);
        }

        static List<ProtoChunk> Renumber(List<ProtoChunk> chunks)
        {
            for (int i = 0; i < chunks.Count; i++)
                chunks[i].ChunkId = i;
            return chunks;
        }

        static List<double[]> GroupBboxesByPage(List<SentenceUnit> sents)
        {
            var merged = new List<(int page, double[] bbox)>();
            var listRun = new List<SentenceUnit>();
            foreach (var s in sents)
            {
                if (s.IsListItem)
                    listRun.Add(s);
                else
                {
                    if (listRun.Count > 0)
                    {
                        merged.AddRange(FlushListRun(listRun));
                        listRun.Clear();
                    }
                    merged.Add((s.PageNo, s.Bbox));
                }
            }
            if (listRun.Count > 0)
                merged.AddRange(FlushListRun(listRun));

            var pageBboxes = new SortedDictionary<int, List<double[]>>();
            foreach (var (page, bbox) in merged)
            {
                if (!pageBboxes.TryGetValue(page, out var list))
                {
                    list = new List<double[]>();
                    pageBboxes[page] = list;
                }
                list.Add(bbox);
            }
            var result = new List<double[]>();
            foreach (var kv in pageBboxes)
            {
                foreach (var bbox in ChunkGeometry.GroupBboxes(kv.Value))
                    result.Add(new[] { kv.Key, bbox[0], bbox[1], bbox[2], bbox[3] });
            }
            return result;
        }

        static List<(int page, double[] bbox)> FlushListRun(List<SentenceUnit> items)
        {
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
            var result = new List<(int, double[])>();
            foreach (var kv in byPage)
                result.Add((kv.Key, ChunkGeometry.UnionBbox(kv.Value)));
            return result;
        }

        static List<double[]> UnionPerPage(List<double[]> pageBboxes)
        {
            var byPage = new SortedDictionary<int, List<double[]>>();
            foreach (var pb in pageBboxes)
            {
                int page = (int)pb[0];
                if (!byPage.TryGetValue(page, out var list))
                {
                    list = new List<double[]>();
                    byPage[page] = list;
                }
                list.Add(new[] { pb[1], pb[2], pb[3], pb[4] });
            }
            var result = new List<double[]>();
            foreach (var kv in byPage)
            {
                var u = ChunkGeometry.UnionBbox(kv.Value);
                result.Add(new[] { kv.Key, u[0], u[1], u[2], u[3] });
            }
            return result;
        }

        static List<double[]> RegroupPageBboxes(IEnumerable<double[]> pageBboxes)
        {
            var byPage = new SortedDictionary<int, List<double[]>>();
            foreach (var pb in pageBboxes)
            {
                int page = (int)pb[0];
                if (!byPage.TryGetValue(page, out var list))
                {
                    list = new List<double[]>();
                    byPage[page] = list;
                }
                list.Add(new[] { pb[1], pb[2], pb[3], pb[4] });
            }
            var result = new List<double[]>();
            foreach (var kv in byPage)
            {
                foreach (var bbox in ChunkGeometry.GroupBboxes(kv.Value))
                    result.Add(new[] { kv.Key, bbox[0], bbox[1], bbox[2], bbox[3] });
            }
            return result;
        }
    }
}
