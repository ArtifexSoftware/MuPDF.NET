using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MuPDF.NET;
using MuPDF.NET.PDF4LLM;
using MuPDF.NET.PDF4LLM.Helpers;
using MuPDF.NET.PDF4LLM.Layout;
using Newtonsoft.Json.Linq;
using Xunit;
using TableHtmlApi = MuPDF.NET.PDF4LLM.Helpers.TableHtml.TableHtml;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestTableHtml
    {
        private const string TestClassName = nameof(TestTableHtml);

        private static string TablePdf => _Path.ForTestClass("test_sce_150_1.pdf", TestClassName);

        private static readonly Regex TableTagRe = new Regex("<table.*?</table>", RegexOptions.Singleline);
        private static readonly Regex CellTagRe = new Regex(@"<(?:td|th)\b([^>]*)>");
        private static readonly Regex ColspanRe = new Regex(@"colspan=""(\d+)""");
        private static readonly Regex RowTagRe = new Regex("<tr\\b[^>]*>.*?</tr>", RegexOptions.Singleline);

        [Fact]
        public void test_to_html_is_live_only_public_api()
        {
            MethodInfo method = typeof(TableHtmlApi).GetMethod(
                nameof(TableHtmlApi.ToHtml),
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            string[] names = method!.GetParameters().Select(p => p.Name).ToArray();
            Assert.Equal(new[] { "pdf", "pageIndex" }, names);
        }

        [Fact]
        public void test_page_html_tables_uses_core_union_find_tables()
        {
            // Python monkeypatches Page.find_tables and asserts a single call with
            // use_layout=True, union=True, refine=True. C# cannot replace instance
            // methods, so TableHtml.FindTablesHook is the equivalent spy point.
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            var calls = new List<(bool useLayout, bool union, bool refine)>();
            var original = TableHtmlApi.FindTablesHook;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                TableHtmlApi.FindTablesHook = (page, useLayout, union, refine) =>
                {
                    calls.Add((useLayout, union, refine));
                    return original(page, useLayout, union, refine);
                };

                using (var doc = new Document(TablePdf))
                {
                    var tables = TableHtmlApi.PageHtmlTables(doc[0]);
                    Assert.Equal(2, tables.Count);
                }
            }
            finally
            {
                TableHtmlApi.FindTablesHook = original;
                MuPDF4LLM.SetUseLayout(prior);
            }

            // find_tables(union=True, refine=True) is called exactly once at the Page
            // level; the line-based candidate pass runs via the module-level find_tables
            // (not a Page.find_tables call), so it is not counted here.
            Assert.Single(calls);
            Assert.True(calls[0].useLayout);
            Assert.True(calls[0].union);
            Assert.True(calls[0].refine);
        }

        [Fact]
        public void test_to_markdown_table_output_html_uses_layout_path()
        {
            // Python monkeypatches document_layout.parse_document and asserts
            // render_html_tables=True. ParseDocumentObserver is the C# spy point.
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            var calls = new List<ParseDocumentCallInfo>();
            var original = DocumentLayout.ParseDocumentObserver;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                DocumentLayout.ParseDocumentObserver = info => calls.Add(info);
                string md = MuPDF4LLM.ToMarkdown(
                    TablePdf,
                    pages: new List<int> { 0 },
                    tableOutput: "html",
                    useOcr: false);
                Assert.Equal(2, Regex.Matches(md, "<table").Count);
                Assert.DoesNotContain("| --- |", md);
            }
            finally
            {
                DocumentLayout.ParseDocumentObserver = original;
                MuPDF4LLM.SetUseLayout(prior);
            }

            Assert.Single(calls);
            Assert.True(calls[0].RenderHtmlTables == true);
            Assert.Equal(14, calls[0].Kwargs.Count);
        }

        [Fact]
        public void test_to_json_table_output_html_uses_layout_path()
        {
            // Python monkeypatches document_layout.parse_document and asserts
            // render_html_tables=True. ParseDocumentObserver is the C# spy point.
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            var calls = new List<ParseDocumentCallInfo>();
            var original = DocumentLayout.ParseDocumentObserver;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                DocumentLayout.ParseDocumentObserver = info => calls.Add(info);
                string js = MuPDF4LLM.ToJson(
                    TablePdf,
                    pages: new List<int> { 0 },
                    tableOutput: "html",
                    useOcr: false);
                JObject data = JObject.Parse(js);
                var tableBoxes = data["pages"]!
                    .SelectMany(p => p["boxes"]!)
                    .Where(b => (string?)b["boxclass"] == "table")
                    .ToList();
                Assert.Contains(tableBoxes, box => !string.IsNullOrEmpty((string?)box["table"]?["html"]));
                // html_tables entries must use snake_case keys (Python _html_table_meta),
                // not CLR property names from HtmlTableMeta.
                JToken? firstHtmlTables = tableBoxes
                    .Select(b => b["table"]?["html_tables"])
                    .FirstOrDefault(t => t is JArray { Count: > 0 });
                Assert.NotNull(firstHtmlTables);
                var metaKeys = ((JObject)firstHtmlTables![0]!).Properties().Select(p => p.Name).OrderBy(n => n).ToArray();
                Assert.Equal(new[] { "bbox", "cells", "cols", "extract", "html", "rows" }, metaKeys);
            }
            finally
            {
                DocumentLayout.ParseDocumentObserver = original;
                MuPDF4LLM.SetUseLayout(prior);
            }

            Assert.Single(calls);
            Assert.True(calls[0].RenderHtmlTables == true);
            Assert.Equal(14, calls[0].Kwargs.Count);
        }

        [Fact]
        public void test_layout_html_env_does_not_enable_table_html()
        {
            string? priorEnv = Environment.GetEnvironmentVariable("PYMUPDF_LAYOUT_HTML_TABLES");
            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                Environment.SetEnvironmentVariable("PYMUPDF_LAYOUT_HTML_TABLES", "1");
                MuPDF4LLM.SetUseLayout(true);
                string md = MuPDF4LLM.ToMarkdown(TablePdf, pages: new List<int> { 0 }, useOcr: false);
                Assert.DoesNotContain("<table", md);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PYMUPDF_LAYOUT_HTML_TABLES", priorEnv);
                MuPDF4LLM.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_table_html_parallel_smoke()
        {
            // Python: ThreadPoolExecutor(max_workers=8), 16x to_html(path, 0),
            // assert results == [expected] * 16. to_html uses find_tables(
            // use_layout=True, union=True, refine=True); that needs a registered
            // layout provider in C# (SetUseLayout), unlike PyMuPDF where layout
            // is available whenever pymupdf-layout is installed.
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                string expected = TableHtmlApi.ToHtml(TablePdf, 0);
                Assert.False(string.IsNullOrEmpty(expected)); // guard against both-empty passes

                string[] results = new string[16];
                Parallel.For(
                    0,
                    16,
                    new ParallelOptions { MaxDegreeOfParallelism = 8 },
                    i => { results[i] = TableHtmlApi.ToHtml(TablePdf, 0); });

                Assert.Equal(Enumerable.Repeat(expected, 16).ToList(), results.ToList());
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_to_json_html_tables_match_to_markdown()
        {
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                string md = MuPDF4LLM.ToMarkdown(
                    TablePdf,
                    pages: new List<int> { 0 },
                    tableOutput: "html",
                    useOcr: false);
                string js = MuPDF4LLM.ToJson(
                    TablePdf,
                    pages: new List<int> { 0 },
                    tableOutput: "html",
                    useOcr: false);
                JObject data = JObject.Parse(js);
                string jsonHtml = string.Join(
                    "\n\n",
                    data["pages"]!
                        .SelectMany(p => p["boxes"]!)
                        .Where(b => (string?)b["boxclass"] == "table")
                        .Select(b => (string?)b["table"]?["html"])
                        .Where(h => !string.IsNullOrEmpty(h)));

                var mdTables = TableTagRe.Matches(md).Cast<Match>().Select(m => m.Value).ToList();
                var jsonTables = TableTagRe.Matches(jsonHtml).Cast<Match>().Select(m => m.Value).ToList();
                Assert.NotEmpty(mdTables);
                Assert.Equal(mdTables, jsonTables);
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }

        static int TableRowWidth(string rowHtml)
        {
            int width = 0;
            foreach (Match attrs in CellTagRe.Matches(rowHtml))
            {
                Match m = ColspanRe.Match(attrs.Groups[1].Value);
                width += m.Success ? int.Parse(m.Groups[1].Value) : 1;
            }
            return width;
        }

        [Fact]
        public void test_to_json_html_mode_grid_fields_consistent()
        {
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                string js = MuPDF4LLM.ToJson(
                    TablePdf,
                    pages: new List<int> { 0 },
                    tableOutput: "html",
                    useOcr: false);
                JObject data = JObject.Parse(js);
                var tableBoxes = data["pages"]!
                    .SelectMany(p => p["boxes"]!)
                    .Where(b => (string?)b["boxclass"] == "table")
                    .ToList();
                Assert.NotEmpty(tableBoxes);

                foreach (JToken box in tableBoxes)
                {
                    JToken tbl = box["table"]!;
                    string html = (string?)tbl["html"] ?? "";
                    var rows = RowTagRe.Matches(html).Cast<Match>().Select(m => m.Value).ToList();
                    int rowCount = tbl["row_count"]!.Value<int>();
                    int colCount = tbl["col_count"]!.Value<int>();
                    Assert.Equal(rowCount, rows.Count);
                    Assert.Equal(colCount, rows.Count == 0 ? 0 : rows.Max(TableRowWidth));

                    JArray cells = (JArray)tbl["cells"]!;
                    Assert.Equal(rowCount, cells.Count);
                    Assert.All(cells, row => Assert.Equal(colCount, ((JArray)row!).Count));

                    JArray extract = (JArray)tbl["extract"]!;
                    Assert.NotNull(extract);
                    Assert.Equal(rowCount, extract.Count);
                    Assert.All(extract, row => Assert.Equal(colCount, ((JArray)row!).Count));
                    Assert.True(tbl["markdown"] == null || tbl["markdown"].Type == JTokenType.Null);
                }
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_table_output_html_no_layout_falls_back_to_rag_path()
        {
            // When the layout *pipeline* is unavailable, table_output="html" must still
            // work via the legacy MuPdfRag path. Keep the layout provider registered so
            // FindTables(useLayout:true) inside PageHtmlTables can still run (matches
            // Python monkeypatching pymupdf4llm._use_layout without deactivating layout).
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                if (!PyMuPdfLayout.IsActivated)
                    PyMuPdfLayout.Activate();
                MuPDF4LLM.UseLayout = false;
                string md = MuPDF4LLM.ToMarkdown(TablePdf, tableOutput: "html");
                Assert.Contains("<table", md);
            }
            finally
            {
                MuPDF4LLM.UseLayout = prior;
                if (prior)
                    MuPDF4LLM.SetUseLayout(true);
                else
                    MuPDF4LLM.SetUseLayout(false);
            }
        }

        static Document BuildBodyTableBodyDoc()
        {
            var doc = new Document();
            Page page = doc.NewPage(width: 612, height: 792);
            page.InsertTextbox(
                new Rect(72, 72, 540, 140),
                "ALPHAMARK is a short introductory paragraph that appears before the table on this page.",
                fontSize: 11);
            var tableRect = new Rect(72, 200, 540, 340);
            List<List<Rect>> cells = Utils.MakeTable(tableRect, cols: 3, rows: 4);
            foreach (List<Rect> row in cells)
            {
                foreach (Rect cell in row)
                    page.DrawRect(cell);
            }
            for (int i = 0; i < cells.Count; i++)
            {
                for (int j = 0; j < cells[i].Count; j++)
                {
                    page.InsertTextbox(
                        cells[i][j],
                        $"R{i}C{j}",
                        align: (int)TextAlign.TEXT_ALIGN_CENTER,
                        fontSize: 10);
                }
            }
            page.InsertTextbox(
                new Rect(72, 400, 540, 470),
                "OMEGAMARK is a short closing paragraph that appears after the table on this page.",
                fontSize: 11);
            page.CleanContents();
            return doc;
        }

        [Fact]
        public void test_body_text_preserved_around_tables()
        {
            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                using (Document built = BuildBodyTableBodyDoc())
                {
                    byte[] pdfdata = built.Write(garbage: true);
                    using (var docPlain = new Document(pdfdata, "pdf"))
                    {
                        string mdPlain = MuPDF4LLM.ToMarkdown(docPlain, useOcr: false);
                        Assert.Equal(1, CountOccurrences(mdPlain, "ALPHAMARK"));
                        Assert.Equal(1, CountOccurrences(mdPlain, "OMEGAMARK"));
                    }

                    using (var docHtml = new Document(pdfdata, "pdf"))
                    {
                        string mdHtml = MuPDF4LLM.ToMarkdown(docHtml, tableOutput: "html", useOcr: false);
                        Assert.Equal(1, CountOccurrences(mdHtml, "ALPHAMARK"));
                        Assert.Equal(1, CountOccurrences(mdHtml, "OMEGAMARK"));
                    }
                }
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }

        static int CountOccurrences(string text, string marker)
        {
            int count = 0;
            int idx = 0;
            while ((idx = text.IndexOf(marker, idx, StringComparison.Ordinal)) >= 0)
            {
                count++;
                idx += marker.Length;
            }
            return count;
        }
    }
}
