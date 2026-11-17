using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;

namespace MuPDF.NET.PDF4LLM.Helpers.Chunking
{
    internal static class LayoutChunker
    {
        public static readonly Dictionary<string, double> WeightsLayout = new Dictionary<string, double>
        {
            ["w_box"] = 0.35,
            ["w_class"] = 0.50,
            ["w_page"] = 0.15,
            ["w_gap"] = 0.30,
            ["w_hgap"] = 0.30,
            ["w_font"] = 0.25,
            ["w_head"] = 0.45,
            ["w_foot"] = 0.20,
            ["w_list"] = 0.40,
            ["w_table"] = 0.60,
            ["w_caption"] = 0.80,
        };

        static readonly Dictionary<string, string[]> EnumValues = new Dictionary<string, string[]>
        {
            ["header_footer_mode"] = new[] { "exclude", "auto", "include" },
            ["sentence_splitter"] = new[] { "default", "multilingual" },
            ["table_mode"] = new[] { "preserve", "isolate" },
            ["table_output"] = new[] { "markdown", "html" },
        };

        public static ChunkedDocument ToChunks(
            ParsedDocument parsedDoc,
            int maxTokens = 400,
            int minTokens = 120,
            double breakpointThreshold = 0.5,
            bool mergeSmallChunks = true,
            string tableMode = "preserve",
            bool respectSectionStarts = true,
            string headerFooterMode = "exclude",
            string sentenceSplitter = "default",
            Func<string, int> tokenizer = null,
            Dictionary<string, double> weights = null)
        {
            var parms = new Dictionary<string, object>
            {
                ["max_tokens"] = maxTokens,
                ["min_tokens"] = minTokens,
                ["breakpoint_threshold"] = breakpointThreshold,
                ["merge_small_chunks"] = mergeSmallChunks,
                ["table_mode"] = tableMode,
                ["respect_section_starts"] = respectSectionStarts,
                ["header_footer_mode"] = headerFooterMode,
                ["sentence_splitter"] = sentenceSplitter,
                ["tokenizer"] = tokenizer,
                ["weights"] = weights,
            };
            ValidateParams(parms);
            var builder = new SentenceBuilder(sentenceSplitter);
            var allUnits = builder.BuildFromDocument(parsedDoc);
            return ChunkUnits(parsedDoc, allUnits, parms);
        }

        public static ChunkedDocument ChunkUnits(
            ParsedDocument parsedDoc,
            List<SentenceUnit> allUnits,
            Dictionary<string, object> parms)
        {
            ValidateParams(parms);
            var elements = BuildElements(parsedDoc, allUnits);
            string hfMode = GetString(parms, "header_footer_mode", "exclude");
            List<SentenceUnit> units;
            if (hfMode == "auto")
            {
                var builder = new SentenceBuilder(GetString(parms, "sentence_splitter", "default"));
                var repeated = builder.DetectRepeatedHeadersFooters(parsedDoc);
                units = allUnits.Where(s =>
                    !repeated.Contains((s.PageNo, s.BoxIndex))
                    && !s.SourceBoxIndices.Any(bi => repeated.Contains((s.PageNo, bi)))).ToList();
            }
            else if (hfMode == "exclude")
                units = allUnits.Where(s => !s.IsHeaderFooter).ToList();
            else
                units = new List<SentenceUnit>(allUnits);

            int hfExcluded = allUnits.Count - units.Count;
            var counter = new TokenCounter(parms.TryGetValue("tokenizer", out object tok) ? tok as Func<string, int> : null);
            for (int i = 0; i < units.Count; i++)
            {
                units[i].SentId = i;
                units[i].TokenCount = counter.Count(units[i].Text);
            }

            var chunks = new List<Chunk>();
            var tables = new List<TableChunk>();
            var figures = new List<FigureChunk>();
            var sections = new List<SectionChunk>();
            if (units.Count > 0)
            {
                var w = parms.TryGetValue("weights", out object wo) ? wo as Dictionary<string, double> : null;
                var scorer = new BoundaryScorer(w ?? WeightsLayout);
                var scores = scorer.ScoreAll(units);
                var assembler = new ChunkAssembler(
                    maxTokens: GetInt(parms, "max_tokens", 400),
                    minTokens: GetInt(parms, "min_tokens", 120),
                    threshold: GetDouble(parms, "breakpoint_threshold", 0.5),
                    tableMode: GetString(parms, "table_mode", "preserve"),
                    mergeSmallChunks: GetBool(parms, "merge_small_chunks", true),
                    respectSectionStarts: GetBool(parms, "respect_section_starts", true));
                var proto = assembler.Assemble(units, scores);
                proto = assembler.Refine(proto);
                var serializer = new ChunkSerializer(parsedDoc);
                (chunks, tables, figures, sections) = serializer.Serialize(proto);
            }

            return new ChunkedDocument(
                chunks,
                elements: elements,
                tables: tables,
                figures: figures,
                sections: sections,
                parms: parms,
                doc: parsedDoc,
                allUnits: allUnits,
                headerFooterExcluded: hfExcluded);
        }

        static List<ChunkElement> BuildElements(ParsedDocument parsedDoc, List<SentenceUnit> allUnits)
        {
            var textsByBox = new Dictionary<(int, int), List<string>>();
            foreach (var u in allUnits)
            {
                var key = (u.PageNo, u.BoxIndex);
                if (!textsByBox.TryGetValue(key, out var list))
                {
                    list = new List<string>();
                    textsByBox[key] = list;
                }
                list.Add(u.Text);
            }
            var elements = new List<ChunkElement>();
            if (parsedDoc?.Pages == null)
                return elements;
            foreach (var page in parsedDoc.Pages)
            {
                var boxes = page.Boxes ?? new List<LayoutBox>();
                for (int boxIdx = 0; boxIdx < boxes.Count; boxIdx++)
                {
                    var box = boxes[boxIdx];
                    var key = (page.PageNumber, boxIdx);
                    textsByBox.TryGetValue(key, out var texts);
                    elements.Add(new ChunkElement
                    {
                        Id = ChunkIds.ElementId(key.PageNumber, boxIdx),
                        Page = key.PageNumber,
                        Box = boxIdx,
                        BoxClass = box.BoxClass,
                        Bbox = new[] { (double)box.X0, box.Y0, box.X1, box.Y1 },
                        Text = texts != null ? string.Join("\n", texts) : "",
                        IsHeaderFooter = box.BoxClass == "page-header" || box.BoxClass == "page-footer",
                    });
                }
            }
            return elements;
        }

        internal static void ValidateParams(Dictionary<string, object> parms)
        {
            foreach (var kv in EnumValues)
            {
                if (parms.TryGetValue(kv.Key, out object val) && val != null)
                {
                    string s = val.ToString();
                    if (Array.IndexOf(kv.Value, s) < 0)
                        throw new ArgumentException($"{kv.Key} must be one of [{string.Join(", ", kv.Value)}], got {s}");
                }
            }
            if (parms.TryGetValue("max_tokens", out object mt) && mt != null)
            {
                int n = Convert.ToInt32(mt);
                if (n <= 0)
                    throw new ArgumentException($"max_tokens must be a positive int, got {mt}");
            }
            if (parms.TryGetValue("min_tokens", out object nt) && nt != null)
            {
                int n = Convert.ToInt32(nt);
                if (n < 0)
                    throw new ArgumentException($"min_tokens must be a non-negative int, got {nt}");
            }
        }

        static string GetString(Dictionary<string, object> d, string key, string fallback) =>
            d.TryGetValue(key, out object v) && v != null ? v.ToString() : fallback;

        static int GetInt(Dictionary<string, object> d, string key, int fallback) =>
            d.TryGetValue(key, out object v) && v != null ? Convert.ToInt32(v) : fallback;

        static double GetDouble(Dictionary<string, object> d, string key, double fallback) =>
            d.TryGetValue(key, out object v) && v != null ? Convert.ToDouble(v) : fallback;

        static bool GetBool(Dictionary<string, object> d, string key, bool fallback) =>
            d.TryGetValue(key, out object v) && v != null ? Convert.ToBoolean(v) : fallback;
    }
}
