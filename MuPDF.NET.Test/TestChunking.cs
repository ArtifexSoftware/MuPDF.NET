using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
    public class TestChunking
    {
        static string Pdf370 => _Path.ForTestClass("test_370.pdf", nameof(Test370));

        static string Norm(string s) => Regex.Replace(s ?? "", @"\s+", " ").Trim();

        [Fact]
        public void token_counter_empty_is_zero()
        {
            var c = new TokenCounter();
            Assert.Equal(0, c.Count(""));
            Assert.Equal(0, c.Count(null));
        }

        [Fact]
        public void token_counter_truncates_char_div_four()
        {
            var c = new TokenCounter();
            Assert.Equal(1, c.Count("abcd"));
            Assert.Equal(1, c.Count("abcde"));
            Assert.Equal(2, c.Count("abcdefgh"));
        }

        [Fact]
        public void token_counter_custom_fn()
        {
            var c = new TokenCounter(t => t.Length);
            Assert.Equal(3, c.Count("abc"));
        }

        [Fact]
        public void horizontal_overlap_zero_width_is_one()
        {
            double ratio = Utils.HorizontalOverlapRatio(
                new[] { 10.0, 0, 10.0, 10 },
                new[] { 0.0, 0, 20.0, 10 });
            Assert.Equal(1.0, ratio);
        }

        [Fact]
        public void test_invalid_values_rejected_and_named()
        {
            var bad = new (string name, System.Collections.Generic.Dictionary<string, object> p)[]
            {
                ("max_tokens", new System.Collections.Generic.Dictionary<string, object> { ["max_tokens"] = 0 }),
                ("max_tokens", new System.Collections.Generic.Dictionary<string, object> { ["max_tokens"] = -5 }),
                ("min_tokens", new System.Collections.Generic.Dictionary<string, object> { ["min_tokens"] = -1 }),
                ("header_footer_mode", new System.Collections.Generic.Dictionary<string, object> { ["header_footer_mode"] = "exlcude" }),
                ("table_mode", new System.Collections.Generic.Dictionary<string, object> { ["table_mode"] = "isolated" }),
                ("sentence_splitter", new System.Collections.Generic.Dictionary<string, object> { ["sentence_splitter"] = "multi" }),
                ("table_output", new System.Collections.Generic.Dictionary<string, object> { ["table_output"] = "csv" }),
            };
            foreach (var (name, p) in bad)
            {
                var ex = Assert.Throws<ArgumentException>(() => Utils.ValidateChunkParams(p));
                Assert.Contains(name, ex.Message);
            }
        }

        [Fact]
        public void test_to_chunks_on_layout_document()
        {
            if (!Utils.LayoutPythonAvailable)
                return;

            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(true);
                using var doc = new Document(Pdf370);
                ChunkedDocument chunks = Utils.ToChunks(doc, pages: new System.Collections.Generic.List<int> { 0 });
                Assert.NotNull(chunks);
                Assert.True(chunks.Count >= 0);
                Assert.NotNull(chunks.Elements);
                Assert.NotNull(chunks.Diagnostics);
                Assert.Contains("chunk_count", chunks.Diagnostics.Keys);

                if (chunks.Count > 0)
                {
                    Chunk c0 = chunks[0];
                    Assert.StartsWith("c", c0.Id);
                    Assert.False(string.IsNullOrEmpty(c0.ContentHash));
                    Assert.NotNull(chunks.Get(c0.Id));
                    var dicts = chunks.ToDicts();
                    Assert.Equal(chunks.Count, dicts.Count);
                    string json = chunks.ToJson();
                    Assert.Contains("\"id\"", json);

                    ChunkedDocument again = chunks.ReassembleChunks(maxTokens: 200);
                    Assert.NotNull(again);
                    Assert.Throws<ArgumentException>(() =>
                    {
                        var merged = new System.Collections.Generic.Dictionary<string, object>(again.Params);
                        merged["table_output"] = "csv";
                        Utils.ValidateChunkParams(merged);
                    });
                }
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        [Fact]
        public void test_chunk_text_contained_in_markdown()
        {
            if (!Utils.LayoutPythonAvailable)
                return;

            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(true);
                ChunkedDocument chunks = Utils.ToChunks(Pdf370);
                string md = Norm(Utils.ToMarkdown(Pdf370));
                var missing = chunks
                    .SelectMany(c => (c.Text ?? "").Split('\n'))
                    .Select(Norm)
                    .Where(p => p.Length > 0 && !p.StartsWith("[Figure") && !md.Contains(p))
                    .Take(5)
                    .ToList();
                Assert.True(missing.Count == 0, string.Join(" | ", missing));
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }
    }
}
