using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestTables/</c>; outputs: <c>TestDocuments/_Output/TestTables/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestTables
    {
        private const string TestClassName = nameof(TestTables);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static readonly string filename = Doc("chinese-tables.pdf");
        private static readonly string pickle_file = Doc("chinese-tables.pickle");

        private static bool HasFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required test document not found: {path}");
            return true;
        }

        private static bool CellsEqual(
            IReadOnlyList<(float x0, float y0, float x1, float y1)> a,
            IReadOnlyList<(float x0, float y0, float x1, float y1)> b,
            float tol = 1e-3f)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (Math.Abs(a[i].x0 - b[i].x0) > tol ||
                    Math.Abs(a[i].y0 - b[i].y0) > tol ||
                    Math.Abs(a[i].x1 - b[i].x1) > tol ||
                    Math.Abs(a[i].y1 - b[i].y1) > tol)
                    return false;
            }
            return true;
        }

        private static bool NullableCellsEqual(
            IReadOnlyList<(float x0, float y0, float x1, float y1)?> a,
            IReadOnlyList<(float x0, float y0, float x1, float y1)?> b,
            float tol = 1e-3f)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] is null != b[i] is null) return false;
                if (a[i] is null) continue;
                var ca = a[i]!.Value;
                var cb = b[i]!.Value;
                if (Math.Abs(ca.x0 - cb.x0) > tol ||
                    Math.Abs(ca.y0 - cb.y0) > tol ||
                    Math.Abs(ca.x1 - cb.x1) > tol ||
                    Math.Abs(ca.y1 - cb.y1) > tol)
                    return false;
            }
            return true;
        }

        private static string PprintExtract(List<List<string?>> extract)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < extract.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('[');
                var row = extract[i];
                for (int j = 0; j < row.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    var item = row[j];
                    if (item is null)
                        sb.Append("None");
                    else
                        sb.Append('\'').Append(item.Replace("'", "\\'")).Append('\'');
                }
                sb.Append(']');
            }
            sb.Append(']');
            sb.AppendLine();
            return sb.ToString();
        }

        private static string Dedent(string text)
        {
            var lines = text.Replace("\r\n", "\n").Trim('\n').Split('\n');
            if (lines.Length == 0)
                return "";
            int minIndent = int.MaxValue;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                int indent = line.Length - line.TrimStart(' ').Length;
                if (indent < minIndent)
                    minIndent = indent;
            }
            if (minIndent == int.MaxValue)
                minIndent = 0;
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    sb.Append('\n');
                var line = lines[i];
                if (line.Length >= minIndent)
                    sb.Append(line.Substring(minIndent));
                else
                    sb.Append(line);
            }
            return sb.ToString();
        }

        private static void AssertExtractRowsEqual(
            List<List<string?>> expected,
            List<List<string?>> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (int r = 0; r < expected.Count; r++)
                Assert.Equal(expected[r], actual[r]);
        }

        [Fact]
        public void test_table1()
        {
            if (!HasFile(filename) || !HasFile(pickle_file)) return;
            // pickle.load is not available in C#; skip when reference data is pickle-only.
            return;

            // pickle_in = open(pickle_file, "rb")
            // page = doc[0]
            // tabs = page.find_tables()
            // cells = tabs[0].cells + tabs[1].cells  # all table cell tuples on page
            // extracts = [tabs[0].extract(), tabs[1].extract()]  # all table cell content
            // old_data = pickle.load(pickle_in)  # previously saved data
            // old_cells = old_data["cells"][0] + old_data["cells"][1]
        }

        [Fact]
        public void test_table2()
        {
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // tab1, tab2 = page.find_tables().tables
            var tab1 = page.find_tables().Tables[0];
            var tab2 = page.find_tables().Tables[1];
            // both tables contain their header data
            Assert.False(tab1.Header.External);
            Assert.True(NullableCellsEqual(tab1.Header.Cells, tab1.Rows[0].Cells));
            Assert.False(tab2.Header.External);
            Assert.True(NullableCellsEqual(tab2.Header.Cells, tab2.Rows[0].Cells));
        }

        [Fact]
        public void test_2812()
        {
            // Make 4 pages with rotations 0, 90, 180 and 270 degrees respectively.
            // Each page shows the same 8x5 table.
            // We will check that each table is detected and delivers the same content.
            using var doc = new Document();
            // Page 0: rotation 0
            // page = doc.NewPage(width=842, height=595)
            var page = doc.NewPage(width: 842, height: 595);
            // rect = page.Rect + (72, 72, -72, -72)
            Rect rect = page.Rect + new Rect(72, 72, -72, -72);
            // cols = 5
            int cols = 5;
            // rows = 8
            int rows = 8;
            // define the cells, draw the grid and insert unique text in each cell.
            var cells = Utils.MakeTable(rect, rows: rows, cols: cols);
            //         page.DrawRect(cells[i][j])
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    page.DrawRect(cells[i][j]);
            }
            //         page.InsertTextbox(
            //             cells[i][j],
            //             f"cell[{i}][{j}]",
            //         )
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    page.InsertTextbox(
                        cells[i][j],
                        $"cell[{i}][{j}]",
                        align: Constants.TextAlignCenter);
                }
            }
            // page.CleanContents()
            page.CleanContents();
            // Page 1: rotation 90 degrees
            // page = doc.NewPage()
            page = doc.NewPage();
            // rect = page.Rect + (72, 72, -72, -72)
            rect = page.Rect + new Rect(72, 72, -72, -72);
            // cols = 8
            cols = 8;
            // rows = 5
            rows = 5;
            cells = Utils.MakeTable(rect, rows: rows, cols: cols);
            //         page.DrawRect(cells[i][j])
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    page.DrawRect(cells[i][j]);
            }
            //         page.InsertTextbox(
            //             cells[i][j],
            //             f"cell[{j}][{rows-i-1}]",
            //             rotate=90,
            //         )
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    page.InsertTextbox(
                        cells[i][j],
                        $"cell[{j}][{rows - i - 1}]",
                        rotate: 90,
                        align: Constants.TextAlignCenter);
                }
            }
            // page.SetRotation(90)
            page.SetRotation(90);
            // page.CleanContents()
            page.CleanContents();

            // Page 2: rotation 180 degrees
            // page = doc.NewPage(width=842, height=595)
            page = doc.NewPage(width: 842, height: 595);
            // rect = page.Rect + (72, 72, -72, -72)
            rect = page.Rect + new Rect(72, 72, -72, -72);
            // cols = 5
            cols = 5;
            // rows = 8
            rows = 8;
            cells = Utils.MakeTable(rect, rows: rows, cols: cols);
            //         page.DrawRect(cells[i][j])
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    page.DrawRect(cells[i][j]);
            }
            //         page.InsertTextbox(
            //             cells[i][j],
            //             f"cell[{rows-i-1}][{cols-j-1}]",
            //             rotate=180,
            //         )
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    page.InsertTextbox(
                        cells[i][j],
                        $"cell[{rows - i - 1}][{cols - j - 1}]",
                        rotate: 180,
                        align: Constants.TextAlignCenter);
                }
            }
            // page.SetRotation(180)
            page.SetRotation(180);
            // page.CleanContents()
            page.CleanContents();

            // Page 3: rotation 270 degrees
            // page = doc.NewPage()
            page = doc.NewPage();
            // rect = page.Rect + (72, 72, -72, -72)
            rect = page.Rect + new Rect(72, 72, -72, -72);
            // cols = 8
            cols = 8;
            // rows = 5
            rows = 5;
            cells = Utils.MakeTable(rect, rows: rows, cols: cols);
            //         page.DrawRect(cells[i][j])
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    page.DrawRect(cells[i][j]);
            }
            //         page.InsertTextbox(
            //             cells[i][j],
            //             f"cell[{cols-j-1}][{i}]",
            //             rotate=270,
            //         )
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    page.InsertTextbox(
                        cells[i][j],
                        $"cell[{cols - j - 1}][{i}]",
                        rotate: 270,
                        align: Constants.TextAlignCenter);
                }
            }
            // page.SetRotation(270)
            page.SetRotation(270);
            // page.CleanContents()
            page.CleanContents();

            // pdfdata = doc.ToBytes()
            byte[] pdfdata = doc.ToBytes();
            doc.Close();

            // -------------------------------------------------------------------------
            // Test PDF prepared. Extract table on each page and
            // ensure identical extracted table data.
            // -------------------------------------------------------------------------
            using var doc2 = new Document(pdfdata, "pdf");
            // extracts = []
            var extracts = new List<string>();
            foreach (var page2 in doc2)
            {
                // tabs = page.find_tables()
                var tabs = page2.find_tables();
                Assert.Single(tabs.Tables);
                // tab = tabs[0]
                var tab = tabs[0];
                // fp = io.StringIO()
                // pprint(tab.extract(), stream=fp)
                // extracts.Append(fp.getvalue())
                extracts.Add(PprintExtract(tab.Extract()));
                // fp = None
                Assert.Equal(8, tab.RowCount);
                Assert.Equal(5, tab.ColCount);
            }
            // e0 = extracts[0]
            string e0 = extracts[0];
            foreach (var e in extracts.Skip(1))
            {
                Assert.Equal(e0, e);
            }
            doc2.Save(Out("test_2812.pdf"));
        }

        [Fact]
        public void test_2979()
        {
            // 2979: identical cell count for each row
            // 3001: no change of global glyph heights
            // filename = os.path.join(scriptdir, "resources", "test_2979.pdf")
            string filename = Doc("test_2979.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // tab = page.find_tables()[0]  # extract the table
            var tab = page.find_tables()[0];
            // lengths = set()  # stores all row cell counts
            var lengths = new HashSet<int>();
            foreach (var e in tab.Extract())
            {
                // lengths.add(len(e))  # store number of cells for row
                lengths.Add(e.Count);
            }

            // test 2979
            Assert.Single(lengths);

            // test 3001
            Assert.False(Tools.SetSmallGlyphHeights());

            string wt = Tools.MupdfWarnings();
            if (_Version.mupdf_version_tuple_at_least(1, 28, 0))
            {
                Assert.Equal("", wt);
            }
            else
            {
                //     wt
                //     == "bogus font ascent/descent values (3117 / -2463)\n... repeated 2 times..."
                // )
                Assert.Equal(
                    "bogus font ascent/descent values (3117 / -2463)\n... repeated 2 times...",
                    wt);
            }
        }

        [Fact]
        public void test_3062()
        {
            // After table extraction, a rotated page should behave and look
            // like as before."""
            //     return

            // filename = os.path.join(scriptdir, "resources", "test_3062.pdf")
            string filename = Doc("test_3062.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // tab0 = page.find_tables()[0]
            var tab0 = page.find_tables()[0];
            // cells0 = tab0.cells
            var cells0 = tab0.Cells;

            // page = None
            page = null;
            // page = doc[0]
            page = doc[0];
            // tab1 = page.find_tables()[0]
            var tab1 = page.find_tables()[0];
            // cells1 = tab1.cells
            var cells1 = tab1.Cells;
            Assert.True(CellsEqual(cells1, cells0));
        }

        [Fact]
        public void test_strict_lines()
        {
            // filename = os.path.join(scriptdir, "resources", "strict-yes-no.pdf")
            string filename = Doc("strict-yes-no.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];

            // tab1 = page.find_tables()[0]
            var tab1 = page.find_tables()[0];
            // tab2 = page.find_tables(strategy="lines_strict")[0]
            var tab2 = page.find_tables(new TableSettings
            {
                VerticalStrategy = "lines_strict",
                HorizontalStrategy = "lines_strict",
            })[0];
            Assert.True(tab2.RowCount < tab1.RowCount);
            Assert.True(tab2.ColCount < tab1.ColCount);
        }

        [Fact]
        public void test_add_lines()
        {
            //     return

            // filename = os.path.join(scriptdir, "resources", "small-table.pdf")
            string filename = Doc("small-table.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            Assert.Empty(page.find_tables().Tables);

            // more_lines = [
            //     ((238.9949951171875, 200.0), (238.9949951171875, 300.0)),
            //     ((334.5559997558594, 200.0), (334.5559997558594, 300.0)),
            //     ((433.1809997558594, 200.0), (433.1809997558594, 300.0)),
            // ]
            // these 3 additional vertical lines should additional 3 columns
            // tab2 = page.find_tables(add_lines=more_lines)[0]
            var tab2 = page.find_tables(new TableSettings
            {
                ExplicitVerticalLines = new List<float>
                {
                    238.9949951171875f,
                    334.5559997558594f,
                    433.1809997558594f,
                },
            })[0];
            Assert.Equal(4, tab2.ColCount);
            Assert.Equal(5, tab2.RowCount);
        }

        [Fact]
        public void test_3148()
        {
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            Rect rect = new Rect(100, 100, 300, 300);
            // text = (
            // )
            string[] text =
            {
                "rotation 0 degrees",
                "rotation 90 degrees",
                "rotation 180 degrees",
                "rotation 270 degrees",
            };
            // degrees = (0, 90, 180, 270)
            int[] degrees = { 0, 90, 180, 270 };
            // delta = (2, 2, -2, -2)
            Rect delta = new Rect(2, 2, -2, -2);
            var cells = Utils.MakeTable(rect, cols: 3, rows: 4);
            //         page.DrawRect(cells[j][i])
            //         k = (i + j) % 4
            //         page.InsertTextbox(cells[j][i] + delta, text[k], rotate=degrees[k])
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    page.DrawRect(cells[j][i]);
                    int k = (i + j) % 4;
                    page.InsertTextbox(cells[j][i] + delta, text[k], rotate: degrees[k]);
                }
            }
            doc.Save(Out("test_3148.pdf"));
            // tabs = page.find_tables()
            var tabs = page.find_tables();
            // tab = tabs[0]
            var tab = tabs[0];
            foreach (var extract in tab.Extract())
            {
                foreach (var item in extract)
                {
                    // item = item.replace("\n", " ")
                    string s = (item ?? "").Replace("\n", " ");
                    Assert.Contains(s, text);
                }
            }
        }

        [Fact]
        public void test_3179()
        {
            // filename = os.path.join(scriptdir, "resources", "test_3179.pdf")
            string filename = Doc("test_3179.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // tabs = page.find_tables()
            var tabs = page.find_tables();
            Assert.Equal(3, tabs.Tables.Count);
        }

        [Fact]
        public void test_battery_file()
        {
            // Earlier versions erroneously tried to identify table headers
            // where there existed no table at all.
            // filename = os.path.join(scriptdir, "resources", "battery-file-22.pdf")
            string filename = Doc("battery-file-22.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // tabs = page.find_tables()
            var tabs = page.find_tables();
            Assert.Empty(tabs.Tables);
        }

        [Fact]
        public void test_markdown()
        {
            // filename = os.path.join(scriptdir, "resources", "strict-yes-no.pdf")
            string filename = Doc("strict-yes-no.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // tab = page.find_tables(strategy="lines_strict")[0]
            var tab = page.find_tables(new TableSettings
            {
                VerticalStrategy = "lines_strict",
                HorizontalStrategy = "lines_strict",
            })[0];
            string md_expected;
            if (!_Version.mupdf_version_tuple_at_least(1, 26, 3))
            {
                // md_expected = textwrap.dedent('''
                //         |Header1|Header2|Header3|
                //         |---|---|---|
                //         |Col11<br>Col12|~~Col21~~<br>~~Col22~~|Col31<br>Col32<br>Col33|
                //         |Col13|~~Col23~~|Col34<br>Col35|
                //         |Col14|~~Col24~~|Col36|
                //         |Col15|~~Col25~~<br>~~Col26~~||
                md_expected = Dedent("""
                    |Header1|Header2|Header3|
                    |---|---|---|
                    |Col11<br>Col12|~~Col21~~<br>~~Col22~~|Col31<br>Col32<br>Col33|
                    |Col13|~~Col23~~|Col34<br>Col35|
                    |Col14|~~Col24~~|Col36|
                    |Col15|~~Col25~~<br>~~Col26~~||
                    
                    """);
            }
            else
            {
                // md_expected = (
                // )
                md_expected =
                    "|Header1|Header2|Header3|\n" +
                    "|---|---|---|\n" +
                    "|Col11<br>Col12|Col21<br>Col22|Col31<br>Col32<br>Col33|\n" +
                    "|Col13|Col23|Col34<br>Col35|\n" +
                    "|Col14|Col24|Col36|\n" +
                    "|Col15|Col25<br>Col26||\n\n";
            }
            string md = tab.ToMarkdown();
            Assert.Equal(md_expected, md.Replace("\r\n", "\n"));
        }

        [Fact]
        public void test_paths_param()
        {
            // filename = os.path.join(scriptdir, "resources", "strict-yes-no.pdf")
            string filename = Doc("strict-yes-no.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // tabs = page.find_tables(paths=[])  # will cause all tables are missed
            var tabs = page.find_tables(paths: Array.Empty<Dictionary<string, object>>());
            Assert.Empty(tabs.Tables);
        }

        [Fact]
        public void test_boxes_param()
        {
            // filename = os.path.join(scriptdir, "resources", "small-table.pdf")
            string filename = Doc("small-table.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // paths = page.GetDrawings()
            var paths = page.GetDrawingsDict();
            // box0 = page.cluster_drawings(drawings=paths)[0]
            var box0 = page.cluster_drawings(drawings: paths)[0];
            // boxes = [box0]
            var boxes = new List<Rect> { box0 };
            // words = page.GetText("words")
            var words = (List<WordBlock>)page.GetText("words");
            // x_vals = [w[0] - 5 for w in words if w[4] in ("min", "max", "avg")]
            foreach (var w in words.Where(w => w.word is "min" or "max" or "avg"))
            {
                // r = +box0
                var r = +box0;
                // r.x1 = x
                r.X1 = w.x0 - 5;
                // boxes.Append(r)
                boxes.Add(r);
            }

            // y_vals = sorted(set([round(w[3]) for w in words]))
            var yVals = words.Select(w => (int)Math.Round(w.y1)).Distinct().OrderBy(y => y).ToList();
            foreach (var y in yVals.Take(yVals.Count - 1))
            {
                // r = +box0
                var r = +box0;
                // r.y1 = y
                r.Y1 = y;
                // boxes.Append(r)
                boxes.Add(r);
            }

            // tabs = page.find_tables(paths=[], add_boxes=boxes)
            var tabs = page.find_tables(
                paths: Array.Empty<Dictionary<string, object>>(),
                addBoxes: boxes);
            // tab = tabs.tables[0]
            var tab = tabs.Tables[0];
            AssertExtractRowsEqual(
                new List<List<string?>>
                {
                    new List<string?> { "Boiling Points °C", "min", "max", "avg" },
                    new List<string?> { "Noble gases", "-269", "-62", "-170.5" },
                    new List<string?> { "Nonmetals", "-253", "4827", "414.1" },
                    new List<string?> { "Metalloids", "335", "3900", "741.5" },
                    new List<string?> { "Metals", "357", ">5000", "2755.9" },
                },
                tab.Extract());
        }

        [Fact]
        public void test_dotted_grid()
        {
            // filename = os.path.join(scriptdir, "resources", "dotted-gridlines.pdf")
            string filename = Doc("dotted-gridlines.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // tabs = page.find_tables()
            var tabs = page.find_tables();
            Assert.Equal(3, tabs.Tables.Count);
            // t0, t1, t2 = tabs  # extract them
            var t0 = tabs[0];
            var t1 = tabs[1];
            var t2 = tabs[2];
            Assert.Equal(11, t0.RowCount);
            Assert.Equal(12, t0.ColCount);
            Assert.Equal(25, t1.RowCount);
            Assert.Equal(11, t1.ColCount);
            Assert.Equal(1, t2.RowCount);
            Assert.Equal(10, t2.ColCount);
        }

        [Fact]
        public void test_4017()
        {
            string path = Doc("test_4017.pdf");
            if (!HasFile(path)) return;
            using (var document = new Document(path))
            {
                // page = document[0]
                var page = document[0];

                // tables = page.find_tables(add_lines=None)
                var tables = page.find_tables();
                Console.WriteLine($"len(tables.tables)={tables.Tables.Count}.");
                // tables_text = list()
                //     t = table.extract()
                for (int i = 0; i < tables.Tables.Count; i++)
                {
                    Console.WriteLine($"## i={i}.");
                    var t = tables[i].Extract();
                    foreach (var tt in t)
                    {
                        Console.WriteLine($"    {tt}");
                    }

                }

                // 2024-11-29: expect current incorrect output for last two tables.

                // expected_a = [
                //     ["Class A/B Overcollateralization", "131.44%", ">=", "122.60%", "", "PASS"],
                //     [None, None, None, None, None, "PASS"],
                //     ["Class D Overcollateralization", "112.24%", ">=", "106.40%", "", "PASS"],
                //     [None, None, None, None, None, "PASS"],
                //     ["Event of Default", "156.08%", ">=", "102.50%", "", "PASS"],
                //     [None, None, None, None, None, "PASS"],
                //     ["Class A/B Interest Coverage", "N/A", ">=", "120.00%", "", "N/A"],
                //     [None, None, None, None, None, "N/A"],
                //     ["Class D Interest Coverage", "N/A", ">=", "105.00%", "", "N/A"],
                // ]
                var expected_a = new List<List<string?>>
                {
                    new() { "Class A/B Overcollateralization", "131.44%", ">=", "122.60%", "", "PASS" },
                    new() { null, null, null, null, null, "PASS" },
                    new() { "Class D Overcollateralization", "112.24%", ">=", "106.40%", "", "PASS" },
                    new() { null, null, null, null, null, "PASS" },
                    new() { "Event of Default", "156.08%", ">=", "102.50%", "", "PASS" },
                    new() { null, null, null, null, null, "PASS" },
                    new() { "Class A/B Interest Coverage", "N/A", ">=", "120.00%", "", "N/A" },
                    new() { null, null, null, null, null, "N/A" },
                    new() { "Class D Interest Coverage", "N/A", ">=", "105.00%", "", "N/A" },
                };
                AssertExtractRowsEqual(expected_a, tables[tables.Tables.Count - 2].Extract());

                // expected_b = [
                //     [
                //     ],
                //     [None, None, None, None, None, "PASS", None],
                //     [
                //     ],
                //     [None, None, None, None, None, "PASS", None],
                //     [
                //     ],
                //     [None, None, None, None, None, "PASS", None],
                //     ["Weighted Average Life", "4.83", "<=", "9.00", "", "PASS", "4.92"],
                // ]
                var expected_b = new List<List<string?>>
                {
                    new()
                    {
                        "Moody's Maximum Rating Factor Test", "2,577", "<=", "3,250", "", "PASS", "2,581",
                    },
                    new() { null, null, null, null, null, "PASS", null },
                    new()
                    {
                        "Minimum Floating Spread", "3.5006%", ">=", "2.0000%", "", "PASS", "3.4871%",
                    },
                    new() { null, null, null, null, null, "PASS", null },
                    new()
                    {
                        "Minimum Weighted Average S&P Recovery\nRate Test",
                        "40.50%", ">=", "40.00%", "", "PASS", "40.40%",
                    },
                    new() { null, null, null, null, null, "PASS", null },
                    new() { "Weighted Average Life", "4.83", "<=", "9.00", "", "PASS", "4.92" },
                };
                AssertExtractRowsEqual(expected_b, tables[tables.Tables.Count - 1].Extract());
            }
        }

        [Fact]
        public void test_md_styles()
        {
            // filename = os.path.join(scriptdir, "resources", "test-styled-table.pdf")
            string filename = Doc("test-styled-table.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // tabs = page.find_tables()[0]
            var tabs = page.find_tables()[0];
            // text = """|Column 1|Column 2|Column 3|\n|---|---|---|\n|Zelle (0,0)|**Bold (0,1)**|Zelle (0,2)|\n|~~Strikeout (1,0), Zeile 1~~<br>~~Hier kommt Zeile 2.~~|Zelle (1,1)|~~Strikeout (1,2)~~|\n|**`Bold-monospaced`**<br>**`(2,0)`**|_Italic (2,1)_|**_Bold-italic_**<br>**_(2,2)_**|\n|Zelle (3,0)|~~**Bold-strikeout**~~<br>~~**(3,1)**~~|Zelle (3,2)|\n\n"""
            string text =
                "|Column 1|Column 2|Column 3|\n" +
                "|---|---|---|\n" +
                "|Zelle (0,0)|**Bold (0,1)**|Zelle (0,2)|\n" +
                "|~~Strikeout (1,0), Zeile 1~~<br>~~Hier kommt Zeile 2.~~|Zelle (1,1)|~~Strikeout (1,2)~~|\n" +
                "|**`Bold-monospaced`**<br>**`(2,0)`**|_Italic (2,1)_|**_Bold-italic_**<br>**_(2,2)_**|\n" +
                "|Zelle (3,0)|~~**Bold-strikeout**~~<br>~~**(3,1)**~~|Zelle (3,2)|\n\n";
            Assert.Equal(text, tabs.ToMarkdown().Replace("\r\n", "\n"));
        }

        static byte[] _make_find_tables_state_doc()
        {
            using var doc = new Document();
            var page = doc.NewPage(width: 360, height: 220);
            var rect = new Rect(40, 40, 320, 180);
            var cells = Utils.MakeTable(rect, rows: 3, cols: 3);
            for (int rowIndex = 0; rowIndex < cells.Count; rowIndex++)
            {
                for (int colIndex = 0; colIndex < cells[rowIndex].Count; colIndex++)
                {
                    page.DrawRect(cells[rowIndex][colIndex]);
                    page.InsertTextbox(
                        cells[rowIndex][colIndex],
                        $"r{rowIndex}c{colIndex}",
                        align: Constants.TextAlignCenter);
                }
            }
            return doc.Write();
        }

        static (int rowCount, int colCount, List<List<string?>> extract) _find_tables_use_layout_false_signature(byte[] pdfBytes)
        {
            using var doc = new Document(pdfBytes, "pdf");
            var table = TableHelpers.FindTables(doc[0], strategy: "lines_strict", useLayout: false)[0];
            return (table.RowCount, table.ColCount, table.Extract());
        }

        [Fact]
        public void test_find_tables_use_layout_false_does_not_call_get_layout()
        {
            // use_layout=False must keep find_tables on the pure line-based path.
            byte[] pdfBytes = _make_find_tables_state_doc();
            using var doc = new Document(pdfBytes, "pdf");
            var page = doc[0];
            var original = Page.GetLayoutProvider;
            Page.GetLayoutProvider = _ => throw new InvalidOperationException("get_layout() should not be called");
            try
            {
                var table = TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: false)[0];
                Assert.Equal(3, table.RowCount);
                Assert.Equal(3, table.ColCount);
                Assert.Equal("r1c1", table.Extract()[1][1]);
            }
            finally
            {
                Page.GetLayoutProvider = original;
            }
        }

        [Fact]
        public void test_find_tables_state_is_call_local_for_threads()
        {
            // Concurrent find_tables calls must not mix text/vector extraction state.
            byte[] pdfBytes = _make_find_tables_state_doc();
            var expected = _find_tables_use_layout_false_signature(pdfBytes);

            // find_tables() uses thread-local TOOLS-flag overrides; snapshot globals
            // so the suite's global-state checks stay clean.
            bool smallBefore = Tools.SetSmallGlyphHeights();
            bool quadBefore = Helpers.SkipQuadCorrections;
            try
            {
                var results = new (int, int, List<List<string?>>)[32];
                Parallel.For(0, 32, i =>
                {
                    results[i] = _find_tables_use_layout_false_signature(pdfBytes);
                });
                foreach (var r in results)
                {
                    Assert.Equal(expected.rowCount, r.Item1);
                    Assert.Equal(expected.colCount, r.Item2);
                    Assert.Equal(expected.extract, r.Item3);
                }
            }
            finally
            {
                Tools.SetSmallGlyphHeights(smallBefore);
                Helpers.SkipQuadCorrections = quadBefore;
            }
        }

        static Document _make_marker_table_doc(string marker)
        {
            // Build a 1-page doc with a small drawn table whose cells embed `marker`.
            var doc = new Document();
            var page = doc.NewPage(width: 360, height: 220);
            var rect = new Rect(40, 40, 320, 180);
            var cells = Utils.MakeTable(rect, rows: 2, cols: 2);
            for (int rowIndex = 0; rowIndex < cells.Count; rowIndex++)
            {
                for (int colIndex = 0; colIndex < cells[rowIndex].Count; colIndex++)
                {
                    page.DrawRect(cells[rowIndex][colIndex]);
                    page.InsertTextbox(
                        cells[rowIndex][colIndex],
                        $"{marker}-{rowIndex}{colIndex}",
                        align: Constants.TextAlignCenter);
                }
            }
            page.CleanContents();
            return doc;
        }

        [Fact]
        public void test_table_extract_stable_after_second_find_tables()
        {
            // Regression test for the stale-CHARS bug.
            //
            // find_tables() snapshots table._chars right after each call so that an
            // already-returned Table's extract() cannot silently pick up a later,
            // unrelated find_tables() call's live (ContextVar-backed) CHARS content.
            using var doc1 = _make_marker_table_doc("PAGE1");
            using var doc2 = _make_marker_table_doc("PAGE2");
            var table1 = TableHelpers.FindTables(doc1[0], strategy: "lines_strict")[0];
            var first = table1.Extract();

            // An unrelated find_tables() call on a different page/doc resets and
            // repopulates the shared CHARS state used during text extraction.
            _ = TableHelpers.FindTables(doc2[0], strategy: "lines_strict");

            Assert.Equal(first, table1.Extract());
            string flatText = string.Join(" ", first.SelectMany(row => row).Where(c => c != null));
            Assert.Contains("PAGE1", flatText);  // guard against both-empty passes
            Assert.DoesNotContain("PAGE2", flatText);
        }

        [Fact]
        public void test_find_tables_use_layout_true_without_layout_is_line_based()
        {
            // use_layout=True (the default) must gracefully degrade to the pure
            // line-based detection path when the optional layout provider is not
            // available: get_layout() becomes a no-op, page.layout_information stays
            // None, and results must match use_layout=False exactly.
            byte[] pdfBytes = _make_find_tables_state_doc();
            using var doc = new Document(pdfBytes, "pdf");
            var page = doc[0];
            var original = Page.GetLayoutProvider;
            Page.GetLayoutProvider = null;  // simulate: layout wheel not installed
            try
            {
                var tablesTrue = TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: true);
                Assert.Null(page.LayoutInformation);

                var tablesFalse = TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: false);

                Assert.Single(tablesTrue.Tables);
                Assert.Equal(
                    tablesFalse.Tables.Select(t => t.Extract()).ToList(),
                    tablesTrue.Tables.Select(t => t.Extract()).ToList());
                var table = tablesTrue[0];
                Assert.Equal(3, table.RowCount);
                Assert.Equal(3, table.ColCount);
                Assert.Equal("r1c1", table.Extract()[1][1]);
            }
            finally
            {
                Page.GetLayoutProvider = original;
            }
        }

        static (Document doc, Page page) _make_overmerged_page()
        {
            // A page whose line grid detects one tall body row that actually holds
            // three record lines -- an under-segmented (over-merged) grid the refinement
            // is meant to repair. Needs no layout, so it exercises the standalone benefit.
            var doc = new Document();
            var page = doc.NewPage(width: 400, height: 300);
            // 2-column grid: header row 100-120, a single tall body row 120-200.
            foreach (float y in new float[] { 100, 120, 200 })
                page.DrawLine(new Point(100, y), new Point(300, y));
            foreach (float x in new float[] { 100, 200, 300 })
                page.DrawLine(new Point(x, 100), new Point(x, 200));
            page.InsertText(new Point(130, 114), "A");
            page.InsertText(new Point(230, 114), "B");
            // Three record lines crammed into the one body row.
            int i = 1;
            foreach (float y in new float[] { 140, 160, 180 })
            {
                page.InsertText(new Point(130, y), i.ToString());
                page.InsertText(new Point(230, y), (i * 10).ToString());
                i++;
            }
            return (doc, page);
        }

        [Fact]
        public void test_refine_grid_splits_overmerged_body()
        {
            // refine_grid() splits an over-merged body row into one row per record.
            // *** PyMuPDF extension (opt-in grid refinement). ***
            var (doc, page) = _make_overmerged_page();
            try
            {
                var grid = new List<List<(float x0, float y0, float x1, float y1)?>>
                {
                    new() { (100, 100, 200, 120), (200, 100, 300, 120) },  // header row
                    new() { (100, 120, 200, 200), (200, 120, 300, 200) },  // one over-merged body row
                };
                var refined = TableRefine.RefineGrid(page, grid, headerRowCount: 1);
                // header kept, body row split into the three record rows
                Assert.Equal(2, grid.Count);  // input untouched
                Assert.Equal(4, refined.Count);
                Assert.Equal(grid[0], refined[0]);  // header preserved verbatim
                Assert.All(refined, r => Assert.Equal(2, r.Count));
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_find_tables_refine_splits_rows_default_unchanged()
        {
            // find_tables(refine=True) repairs the over-merged grid; the default result
            // is unchanged -- refinement is strictly opt-in.
            // *** PyMuPDF extension. ***
            var (doc, page) = _make_overmerged_page();
            try
            {
                var defaultTf = page.FindTables(useLayout: false);
                var refined = page.FindTables(useLayout: false, refine: true);
                Assert.Single(defaultTf.Tables);
                Assert.Single(refined.Tables);

                // Default detects the merged 2-row grid (unchanged behaviour).
                Assert.Equal(2, defaultTf.Tables[0].RowCount);
                Assert.Equal(2, defaultTf.Tables[0].ColCount);

                // refine=True splits the body into three record rows.
                var t = refined.Tables[0];
                Assert.Equal(4, t.RowCount);
                Assert.Equal(2, t.ColCount);
                Assert.Equal(
                    new List<List<string?>>
                    {
                        new() { "A", "B" },
                        new() { "1", "10" },
                        new() { "2", "20" },
                        new() { "3", "30" },
                    },
                    t.Extract());
            }
            finally
            {
                doc.Dispose();
            }
        }

        static (Document doc, Page page) _make_merged_header_page()
        {
            // A page whose line grid detects a header cell that spans both body columns.
            //
            // The middle vertical divider is drawn only in the body (below the header
            // separator), so the top row is one wide cell over two columns while each body
            // row has two cells -- a merged header cell find_tables detects on its own.
            // Needs no layout, so it exercises the standalone benefit.
            var doc = new Document();
            var page = doc.NewPage(width: 400, height: 300);
            foreach (float y in new float[] { 100, 120, 140, 160 })
                page.DrawLine(new Point(100, y), new Point(300, y));
            page.DrawLine(new Point(100, 100), new Point(100, 160));  // left border
            page.DrawLine(new Point(300, 100), new Point(300, 160));  // right border
            page.DrawLine(new Point(200, 120), new Point(200, 160));  // middle divider: body only
            page.InsertText(new Point(150, 114), "Merged Header");
            page.InsertText(new Point(120, 134), "a");
            page.InsertText(new Point(220, 134), "b");
            page.InsertText(new Point(120, 154), "c");
            page.InsertText(new Point(220, 154), "d");
            return (doc, page);
        }

        [Fact]
        public void test_resolve_spans_merged_header()
        {
            // resolve_spans() surfaces a merged header cell as a colspan-2 SpanCell.
            // *** PyMuPDF extension (opt-in span resolution). ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var grid = new List<List<(float x0, float y0, float x1, float y1)?>>
                {
                    new() { (100, 100, 300, 120) },  // header spanning both columns
                    new() { (100, 120, 200, 140), (200, 120, 300, 140) },
                    new() { (100, 140, 200, 160), (200, 140, 300, 160) },
                };
                var placements = TableSpans.ResolveSpans(page, grid);
                Assert.Equal(3, placements.Count);
                // header is one placement spanning both columns
                Assert.Single(placements[0]);
                var head = placements[0][0];
                Assert.Equal((2, 1), (head.Colspan, head.Rowspan));
                Assert.Equal((100.0f, 100.0f, 300.0f, 120.0f), head.Bbox);
                Assert.Contains("Merged Header", head.Text);
                // resolve_spans leaves the HTML tag at its default; tagging td/th is the
                // caller's job (find_tables(refine=True) / the engine model builder).
                Assert.Equal("td", head.Tag);
                // body cells stay 1x1
                Assert.Equal(new[] { (1, 1), (1, 1) }, placements[1].Select(c => (c.Colspan, c.Rowspan)).ToArray());
                Assert.Equal(new[] { "c", "d" }, placements[2].Select(c => c.Text).ToArray());
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_find_tables_refine_exposes_placements_default_none()
        {
            // find_tables(refine=True) attaches Table.placements with the colspan/rowspan
            // structure; the default result exposes no placements and is otherwise unchanged.
            // *** PyMuPDF extension. ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var defaultTf = page.FindTables(useLayout: false);
                var refined = page.FindTables(useLayout: false, refine: true);
                Assert.Single(defaultTf.Tables);
                Assert.Single(refined.Tables);

                // Default detects the merged-header grid but resolves no spans.
                var dt = defaultTf.Tables[0];
                Assert.Equal((3, 2), (dt.RowCount, dt.ColCount));
                Assert.Null(dt.Placements);

                // refine=True exposes the header cell's colspan via .placements.
                var t = refined.Tables[0];
                Assert.NotNull(t.Placements);
                Assert.Equal(2, t.Placements[0][0].Colspan);
                Assert.Equal(1, t.Placements[0][0].Rowspan);
                Assert.Equal(new[] { 1, 1 }, t.Placements[1].Select(c => c.Colspan).ToArray());
                // placements are tagged: the top header row is th, body rows are td.
                Assert.Equal("th", t.Placements[0][0].Tag);
                Assert.Equal(new[] { "td", "td" }, t.Placements[1].Select(c => c.Tag).ToArray());
                Assert.Equal(new[] { "td", "td" }, t.Placements[2].Select(c => c.Tag).ToArray());
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_find_tables_refine_to_html_merged_header()
        {
            // find_tables(refine=True) + Table.to_html() serialize a merged header as a
            // <th colspan=2>, with body rows as <td>.
            // *** PyMuPDF extension (opt-in header tagging + HTML serialization). ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var t = page.FindTables(useLayout: false, refine: true).Tables[0];
                string html = t.ToHtml();
                Assert.Equal(
                    "<table>" +
                    "<tr><th colspan=\"2\">Merged Header</th></tr>" +
                    "<tr><td>a</td><td>b</td></tr>" +
                    "<tr><td>c</td><td>d</td></tr>" +
                    "</table>",
                    html);
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_find_tables_refine_header_meta()
        {
            // find_tables(refine=True) exposes the header meta (header_rows/section_rows)
            // on the Table; the default path leaves the conservative defaults.
            // *** PyMuPDF extension. ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var defaultT = page.FindTables(useLayout: false).Tables[0];
                Assert.Equal(0, defaultT.HeaderRows);
                Assert.Empty(defaultT.SectionRows);

                var t = page.FindTables(useLayout: false, refine: true).Tables[0];
                Assert.Equal(1, t.HeaderRows);
                Assert.Empty(t.SectionRows);
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_table_to_html_fallback_flat()
        {
            // Table.to_html() on a default (non-refined) table returns a well-formed,
            // td-only flat <table> built from extract() -- no placements needed.
            // *** PyMuPDF extension. ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var t = page.FindTables(useLayout: false).Tables[0];
                Assert.Null(t.Placements);
                string html = t.ToHtml();
                Assert.StartsWith("<table>", html);
                Assert.EndsWith("</table>", html);
                Assert.DoesNotContain("<th", html);  // flat fallback is td-only
                Assert.Equal(t.RowCount, System.Text.RegularExpressions.Regex.Matches(html, "<tr>").Count);
                // every cell is a plain <td>; the merged header text lands in one cell
                Assert.Contains("<td>Merged Header</td>", html);
                Assert.Contains("<td>a</td>", html);
                Assert.Contains("<td>d</td>", html);
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_render_table_html_section_row_collapse()
        {
            // The core serializer collapses a section-label row (a lone centered label)
            // to a single <th colspan=N>, and honours per-cell td/th tags + colspan.
            // *** PyMuPDF extension (HTML serialization). ***
            SpanCell Cell(string text, int colspan = 1, int rowspan = 1, string tag = "td") =>
                new SpanCell(null, text, colspan, rowspan, tag);

            var rows = new List<List<SpanCell>>
            {
                new() { Cell("Group", colspan: 3, tag: "th") },
                new() { Cell(""), Cell("Section", tag: "th"), Cell("") },  // centered section label
                new() { Cell("x"), Cell("1"), Cell("2") },
            };
            string html = TableHeaders.RenderTableHtml(rows, sectionHeaderRows: new[] { 1 });
            Assert.Equal(
                "<table>" +
                "<tr><th colspan=\"3\">Group</th></tr>" +
                "<tr><th colspan=\"3\">Section</th></tr>" +
                "<tr><td>x</td><td>1</td><td>2</td></tr>" +
                "</table>",
                html);
            // escaping (& < >) and <br/> line joins, quotes left literal
            Assert.Equal(
                "<table><tr><td>a &amp; b &lt; c &gt; \"d\"<br/>second</td></tr></table>",
                TableHeaders.RenderTableHtml(new List<List<SpanCell>>
                {
                    new() { Cell("a & b < c > \"d\"\nsecond") },
                }));
        }

        static void _make_bordered_table(Page page, float x0, float y0, string[][] texts)
        {
            // Draw a bordered 2x2 table (cells 100 wide, 20 tall) at (x0, y0), with the
            // 2x2 texts grid inserted into its cells; returns nothing (mutates page).
            float x1 = x0 + 100, x2 = x0 + 200;
            float y1 = y0 + 20, y2 = y0 + 40;
            foreach (float y in new float[] { y0, y1, y2 })
                page.DrawLine(new Point(x0, y), new Point(x2, y));
            foreach (float x in new float[] { x0, x1, x2 })
                page.DrawLine(new Point(x, y0), new Point(x, y2));
            float[] rys = { y0, y1 };
            float[] cxs = { x0, x1 };
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 2; c++)
                    page.InsertText(new Point(cxs[c] + 5, rys[r] + 14), texts[r][c]);
        }

        [Fact]
        public void test_find_tables_union_fuses_layout_grid_with_line_candidate()
        {
            // find_tables(union=True) fuses the layout analyzer's GNN table grids with
            // the line-based finder's candidates: a layout table with no matching line
            // candidate is kept from its GNN grid, and a disjoint line-detected table is
            // appended -- layout order first, then appended candidates.
            // *** PyMuPDF extension (opt-in layout/candidate union). ***
            using var doc = new Document();
            var page = doc.NewPage(width: 500, height: 500);
            // Table B: a real bordered 2x2 table the line finder detects (disjoint from A).
            _make_bordered_table(page, 80, 300, new[] { new[] { "b00", "b01" }, new[] { "b10", "b11" } });
            // Table A: only a layout (GNN) grid, no drawn lines. Inject the raw layout
            // form union reads (return_raw=True shape): a "table" group whose
            // table_grid carries interior h_lines/v_lines offsets.
            page.LayoutInformation = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["class_name"] = "table",
                    ["group_bbox"] = new List<float> { 80.0f, 80.0f, 280.0f, 120.0f },
                    ["table_grid"] = new Dictionary<string, object>
                    {
                        ["h_lines"] = new List<float> { 20.0f },
                        ["v_lines"] = new List<float> { 100.0f },
                    },
                },
            };
            var tf = page.FindTables(useLayout: true, union: true);
            var tables = tf.Tables;
            Assert.Equal(2, tables.Count);

            // A first (layout order): a 2x2 grid built from group_bbox + interior lines.
            var a = tables[0];
            Assert.Equal((2, 2), (a.RowCount, a.ColCount));
            Assert.Equal((80.0f, 80.0f, 280.0f, 120.0f), a.Bbox);
            var aCells = a.Rows.Select(row => row.Cells.ToList()).ToList();
            Assert.Equal((80.0f, 80.0f, 180.0f, 100.0f), aCells[0][0]);
            Assert.Equal((180.0f, 100.0f, 280.0f, 120.0f), aCells[1][1]);

            // B appended after the layout table: the line-detected grid, extractable.
            var b = tables[1];
            Assert.Equal((2, 2), (b.RowCount, b.ColCount));
            Assert.Equal("b00", b.Extract()[0][0]);
        }

        [Fact]
        public void test_find_tables_union_no_layout_degrades_to_line_candidates()
        {
            // union=True degrades to the pure line-based candidates when the layout
            // analyzer is unavailable: get_layout() is a no-op, layout_information stays
            // None, there are no primary grids, so every line-detected table is appended --
            // matching the plain line-based find_tables result.
            // *** PyMuPDF extension. ***
            using var doc = new Document();
            var page = doc.NewPage(width: 400, height: 400);
            _make_bordered_table(page, 80, 80, new[] { new[] { "a", "b" }, new[] { "c", "d" } });
            var original = Page.GetLayoutProvider;
            Page.GetLayoutProvider = null;  // simulate: layout wheel not installed
            try
            {
                var union = page.FindTables(useLayout: true, union: true);
                Assert.Null(page.LayoutInformation);

                var line = TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: false);
                Assert.Single(union.Tables);
                // Same table as the pure line-based path, just routed through the union.
                Assert.Equal(
                    line.Tables.Select(t => t.Extract()).ToList(),
                    union.Tables.Select(t => t.Extract()).ToList());
                var t = union.Tables[0];
                Assert.Equal((2, 2), (t.RowCount, t.ColCount));
                Assert.Equal("a", t.Extract()[0][0]);
            }
            finally
            {
                Page.GetLayoutProvider = original;
            }
        }

    }
}
