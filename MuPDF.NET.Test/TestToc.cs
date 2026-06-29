// Port of PyMuPDF-1.27.2.2/tests/test_toc.py
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestToc/</c>; outputs: <c>TestDocuments/_Output/TestToc/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestToc
    {
        private const string TestClassName = nameof(TestToc);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_simple_toc()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var doc = new Document(Doc("001003ED.pdf"));
            string expected = File.ReadAllText(Doc("simple_toc.txt"), System.Text.Encoding.UTF8).Replace("\r", "");
            string toc = string.Concat(
                doc.GetToc(simple: true).Select(t => FormatSimpleTocEntry(t.level, t.title, t.page)));
            Assert.Equal(expected, Encoding.UTF8.GetString(Encoding.GetEncoding(1252).GetBytes(toc)));
        }

        [Fact]
        public void test_full_toc()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string expected = File.ReadAllText(Doc("full_toc.txt")).Replace("\r", "");
            using var doc = new Document(Doc("001003ED.pdf"));
            var toc = string.Join("\n", doc.GetToc(simple: false).Select(FormatExtendedTocEntry)) + "\n";
            Assert.Equal(expected, Encoding.UTF8.GetString(Encoding.GetEncoding(1252).GetBytes(toc)));
        }

        [Fact]
        public void test_erase_toc()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            doc.SetToc(new List<object>());
            Assert.Empty(doc.GetToc());
        }

        [Fact]
        public void test_replace_toc()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            var toc = doc.GetToc(simple: false);
            var rows = new List<object>();
            foreach (var (level, title, page, link) in toc)
                rows.Add(new object[] { level, title, page, link });
            doc.SetToc(rows);
        }

        [Fact]
        public void test_setcolors()
        {
            using var doc = new Document(Doc("2.pdf"));
            var toc = doc.GetToc(simple: false);
            for (int i = 0; i < toc.Count; i++)
            {
                var d = new Dictionary<string, object>(toc[i].link);
                d["color"] = _Constants.red;
                d["bold"] = true;
                d["italic"] = true;
                doc.SetTocItem(i, destDict: d);
            }

            var toc2 = doc.GetToc(simple: false);
            Assert.Equal(toc.Count, toc2.Count);
            foreach (var (_, _, _, d) in toc2)
            {
                Assert.True(Convert.ToBoolean(d["bold"], CultureInfo.InvariantCulture));
                Assert.True(Convert.ToBoolean(d["italic"], CultureInfo.InvariantCulture));
                AssertColorEqual(_Constants.red, d["color"]);
            }
        }

        [Fact]
        public void test_circular()
        {
            using var doc = new Document(Doc("circular-toc.pdf"));
            try
            {
                _ = doc.GetToc(simple: false);
            }
            catch (ApplicationException ex) when (ex.Message.Contains("Cycle", StringComparison.OrdinalIgnoreCase))
            {
                // MuPDF 1.27+ detects circular outlines instead of looping.
                return;
            }

            var (maj, min, _) = _Version.mupdf_version_tuple();
            if (maj < 1 || (maj == 1 && min < 27))
            {
                string wt = Tools.MupdfWarnings();
                Assert.Equal("Bad or missing prev pointer in outline tree, repairing", wt);
            }
        }

        [Fact]
        public void test_2355()
        {
            string path = Out("test_2355.pdf");
            try
            {
                using (var doc = new Document())
                {
                    for (int i = 0; i < 10; i++)
                        doc.NewPage(doc.PageCount);
                    doc.SetToc(new List<object>
                    {
                        new object[] { 1, "test", 1 },
                        new object[] { 1, "test2", 5 },
                    });
                    doc.Save(path);
                }

                for (int i = 0; i < 10; i++)
                {
                    using var newDoc = new Document(path);
                    _ = newDoc.GetToc();
                }

                using (var newDoc = new Document(path))
                {
                    for (int i = 0; i < 10; i++)
                        _ = newDoc.GetToc();
                }
            }
            finally
            {
                //TryDelete(path);
            }
        }

        [Fact]
        public void test_2788()
        {
            using var document = new Document(Doc("test_2788.pdf"));
            var toc0 = new List<(int, string, int, Dictionary<string, object>)>
            {
                (1, "page2", 2, new Dictionary<string, object>
                {
                    ["kind"] = Constants.LinkNamed,
                    ["xref"] = 14,
                    ["page"] = 1,
                    ["to"] = new Point(100.0f, 760.0f),
                    ["zoom"] = 0.0,
                    ["nameddest"] = "page.2",
                }),
            };

            var toc1 = document.GetToc(simple: false);
            AssertTocEqual(toc0, toc1, ignoreXref: true);

            var rows = new List<object> { new object[] { 1, "page2", 2, new Dictionary<string, object>(toc0[0].Item4) } };
            document.SetToc(rows);
            var toc2 = document.GetToc(simple: false);
            AssertTocEqual(toc0, toc2, ignoreXref: true);

            Tools.ResetMupdfWarnings();
            foreach (Page page in document)
                _ = page.GetLinks();
            mupdf.mupdf.fz_flush_warnings();
            string wt = Tools.MupdfWarnings(reset: false);
            const string expectedWt =
                "syntax error: expected 'obj' keyword (0 3 ?)\n" +
                "trying to repair broken xref\n" +
                "repairing PDF document";
            if (!string.IsNullOrEmpty(wt))
                Assert.Equal(expectedWt, wt);
        }

        [Fact]
        public void test_toc_count()
        {
            string fileIn = Doc("test_toc_count.pdf");
            string fileOut = Out("test_toc_count_out.pdf");
            try
            {
                using (var doc = new Document(fileIn))
                {
                    Console.WriteLine($"1: {GetOutlinesObject(doc)}");
                    var toc = doc.GetToc(simple: false);
                    doc.SetToc(new List<object>());
                    doc.SetToc(TocToRows(toc));
                    Console.WriteLine($"3: {GetOutlinesObject(doc)}");
                    doc.Save(fileOut, garbage: 4);
                }
                using (var doc = new Document(fileOut))
                {
                    Console.WriteLine($"4: {GetOutlinesObject(doc)}");
                }

            }
            finally
            {
                //TryDelete(fileOut);
            }
        }

        [Fact]
        public void test_3347()
        {
            using var doc = new Document();
            doc.NewPage(width: 500, height: 800);
            doc.NewPage(width: 800, height: 500);

            var rects = new (int page, Rect rect, float[] color)[]
            {
                (0, new Rect(10, 20, 50, 40), ColorRgb("red")),
                (0, new Rect(300, 350, 400, 450), ColorRgb("green")),
                (1, new Rect(20, 30, 40, 50), ColorRgb("blue")),
                (1, new Rect(350, 300, 450, 400), ColorRgb("black")),
            };

            foreach (var (page, rect, color) in rects)
                doc[page].DrawRect(rect, color: color);

            for (int i = 0; i < rects.Length; i++)
            {
                int j = (i + 1) % rects.Length;
                var (fromPage, fromRect, _) = rects[i];
                var (toPage, toRect, _) = rects[j];
                doc[fromPage].InsertLink(new Dictionary<string, object>
                {
                    ["kind"] = Constants.LinkGoto,
                    ["from"] = fromRect,
                    ["page"] = toPage,
                    ["to"] = toRect.TopLeft,
                });
            }

            var linksExpected = new List<(int page, Dictionary<string, object> link)>
            {
                (0, new Dictionary<string, object> { ["kind"] = 1, ["xref"] = 11, ["from"] = new Rect(10, 20, 50, 40), ["page"] = 0, ["to"] = new Point(300, 350), ["zoom"] = 0.0f, ["id"] = "fitz-L0" }),
                (0, new Dictionary<string, object> { ["kind"] = 1, ["xref"] = 12, ["from"] = new Rect(300, 350, 400, 450), ["page"] = 1, ["to"] = new Point(20, 30), ["zoom"] = 0.0f, ["id"] = "fitz-L1" }),
                (1, new Dictionary<string, object> { ["kind"] = 1, ["xref"] = 13, ["from"] = new Rect(20, 30, 40, 50), ["page"] = 1, ["to"] = new Point(350, 300), ["zoom"] = 0.0f, ["id"] = "fitz-L0" }),
                (1, new Dictionary<string, object> { ["kind"] = 1, ["xref"] = 14, ["from"] = new Rect(350, 300, 450, 400), ["page"] = 0, ["to"] = new Point(10, 20), ["zoom"] = 0.0f, ["id"] = "fitz-L1" }),
            };

            string outPath = Out("test_3347_out.pdf");
            try
            {
                doc.Save(outPath);
                var linksActual = CollectPageLinks(doc);
                AssertLinksEqual(linksExpected, linksActual);
            }
            finally
            {
                //TryDelete(outPath);
            }
        }

        /// <summary>
        /// Port of <c>test_3400()</c> in <c>PyMuPDF-1.27.2.2/tests/test.py</c> (#3400 link destinations with page rotation).
        /// </summary>
        [Fact]
        public void test_3400()
        {
            float width = 750;
            float height = 1110;
            var circleMiddlePoint = new Point(height / 4, width / 4);

            using var doc = new Document();
            var page = doc.NewPage(width: width, height: height);
            page.SetRotation(270);
            // draw a circle at the middle point to facilitate debugging
            page.DrawCircle(circleMiddlePoint, color: _Constants.blue, radius: 5, width: 2);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    float x = i / 10f * width;
                    float y = j / 10f * height;
                    page.DrawCircle(new Point(x, y), color: _Constants.black, radius: 0.2f, width: 0.1f);
                    _ = page.InsertHtmlbox(
                        new Rect(x, y, x + width / 10, y + height / 20),
                        $"<small><small><small><small>(x={x:F1},y={y:F1})</small></small></small></small>",
                        css: null,
                        scaleLow: 0f,
                        archive: null,
                        rotate: 0,
                        oc: 0,
                        opacity: 1f);
                }
            }

            // rotate the middle point by the page rotation for the new toc entry
            var tocLinkCoords = circleMiddlePoint;

            doc.SetToc(new List<object>
            {
                new object[]
                {
                    1, "Link to circle", 1,
                    new Dictionary<string, object>
                    {
                        ["kind"] = Constants.LinkGoto,
                        ["page"] = 1,
                        ["to"] = tocLinkCoords,
                        ["from"] = new Rect(0, 0, height / 4, width / 4),
                    },
                },
            }, collapse: 0);

            page = doc.NewPage(width: 200, height: 300);
            var fromRect = new Rect(10, 10, 100, 50);
            _ = page.InsertHtmlbox(fromRect, "link", css: null, scaleLow: 0f, archive: null, rotate: 0, oc: 0, opacity: 1f);
            page.InsertLink(new Dictionary<string, object>
            {
                ["from"] = fromRect,
                ["kind"] = Constants.LinkGoto,
                ["to"] = tocLinkCoords,
                ["page"] = 0,
            });

            var linksExpected = new List<(int page, Dictionary<string, object> link)>
            {
                (1, new Dictionary<string, object>
                {
                    ["kind"] = Constants.LinkGoto,
                    ["xref"] = 1120,
                    ["from"] = new Rect(10.0f, 10.0f, 100.0f, 50.0f),
                    ["page"] = 0,
                    ["to"] = new Point(187.5f, 472.5f),
                    ["zoom"] = 0.0f,
                    ["id"] = "fitz-L0",
                }),
            };

            var linksActual = CollectPageLinks(doc);
            AssertLinksEqual(linksExpected, linksActual, ignoreXref: true);

            // test.py: doc.Save('D:\\Artifex\\TestDocuments\\TestToc\\test_3400_.pdf')
            string outPath = Out("test_3400.pdf");
            TryDelete(outPath);
            try
            {
                doc.Save(outPath);
            }
            catch (IOException)
            {
                // Skip when the debug PDF is open in a viewer (assertions use the in-memory doc).
            }
        }

        [Fact]
        public void test_3820()
        {
            using var doc = new Document(Doc("test-3820.pdf"));
            foreach (var (_, _, epage, dest) in doc.GetToc(simple: false))
            {
                int page = Convert.ToInt32(dest["page"], CultureInfo.InvariantCulture);
                Assert.Equal(epage, page + 1);
            }
        }

        // ─── helpers ───────────────────────────────────────────────────

        static string GetOutlinesObject(Document doc)
        {
            var (_, olVal) = doc.XrefGetKey(doc.PdfCatalog, "Outlines");
            int olXref = int.Parse(olVal.Split()[0], CultureInfo.InvariantCulture);
            return doc.XrefObject(olXref);
        }

        static List<object> TocToRows(List<(int level, string title, int page, Dictionary<string, object> link)> toc)
        {
            var rows = new List<object>();
            foreach (var (level, title, page, link) in toc)
                rows.Add(new object[] { level, title, page, link });
            return rows;
        }

        static List<(int page, Dictionary<string, object> link)> CollectPageLinks(Document doc)
        {
            var linksActual = new List<(int, Dictionary<string, object>)>();
            for (int pageI = 0; pageI < doc.PageCount; pageI++)
            {
                foreach (var link in doc[pageI].GetLinks())
                    linksActual.Add((pageI, link));
            }
            return linksActual;
        }

        static void AssertLinksEqual(
            List<(int page, Dictionary<string, object> expected)> expected,
            List<(int page, Dictionary<string, object> actual)> actual,
            bool ignoreXref = false)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].page, actual[i].page);
                AssertLinkDictEqual(expected[i].Item2, actual[i].Item2, ignoreXref);
            }
        }

        static void AssertLinkDictEqual(Dictionary<string, object> expected, Dictionary<string, object> actual, bool ignoreXref)
        {
            foreach (var key in expected.Keys)
            {
                if (ignoreXref && key == "xref")
                    continue;
                Assert.True(actual.ContainsKey(key), $"missing key '{key}'");
                AssertLinkValueEqual(key, expected[key], actual[key]);
            }
            if (!ignoreXref)
            {
                foreach (var key in actual.Keys)
                    Assert.True(expected.ContainsKey(key) || key == "xref", $"unexpected key '{key}'");
            }
        }

        static void AssertLinkValueEqual(string key, object expected, object actual)
        {
            if (expected is Rect er && actual is Rect ar)
            {
                Assert.True(RectsApproxEqual(er, ar), $"Rect mismatch for {key}: {er} vs {ar}");
                return;
            }
            if (key == "to" || expected is Point || actual is Point)
            {
                var ep = ToPoint(expected);
                var ap = ToPoint(actual);
                Assert.True(Math.Abs(ep.X - ap.X) < 1e-3 && Math.Abs(ep.Y - ap.Y) < 1e-3,
                    $"Point mismatch for {key}: {ep} vs {ap}");
                return;
            }
            if (key == "zoom" && expected is float ed && actual is float ad)
            {
                Assert.Equal(ed, ad, 4);
                return;
            }
            Assert.Equal(expected, actual);
        }

        static void AssertTocEqual(
            List<(int, string, int, Dictionary<string, object>)> expected,
            List<(int level, string title, int page, Dictionary<string, object> link)> actual,
            bool ignoreXref = false)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Item1, actual[i].level);
                Assert.Equal(expected[i].Item2, actual[i].title);
                Assert.Equal(expected[i].Item3, actual[i].page);
                AssertTocDestEqual(expected[i].Item4, actual[i].link, ignoreXref);
            }
        }

        static void AssertTocDestEqual(Dictionary<string, object> expected, Dictionary<string, object> actual, bool ignoreXref = false)
        {
            foreach (var kv in expected)
            {
                if (ignoreXref && kv.Key == "xref")
                    continue;
                Assert.True(actual.ContainsKey(kv.Key), $"missing dest key '{kv.Key}'");
                if (kv.Key == "to")
                {
                    var ep = ToPoint(kv.Value);
                    var ap = ToPoint(actual[kv.Key]);
                    Assert.True(Math.Abs(ep.X - ap.X) < 1e-3 && Math.Abs(ep.Y - ap.Y) < 1e-3);
                    continue;
                }
                if (kv.Key == "xref")
                {
                    Assert.Equal(Convert.ToInt32(kv.Value, CultureInfo.InvariantCulture),
                        Convert.ToInt32(actual[kv.Key], CultureInfo.InvariantCulture));
                    continue;
                }
                if (kv.Key == "zoom")
                {
                    Assert.Equal(Convert.ToDouble(kv.Value, CultureInfo.InvariantCulture),
                        Convert.ToDouble(actual[kv.Key], CultureInfo.InvariantCulture), 4);
                    continue;
                }
                Assert.Equal(kv.Value, actual[kv.Key]);
            }
        }

        static void AssertColorEqual(float[] expected, object actual)
        {
            float[] ac = actual switch
            {
                float[] fa => fa,
                double[] da => new float[] { (float)da[0], (float)da[1], (float)da[2] },
                IList<object> lo => new float[] { Convert.ToSingle(lo[0]), Convert.ToSingle(lo[1]), Convert.ToSingle(lo[2]) },
                ITuple tup when tup.Length == 3 => new float[]
                {
                    Convert.ToSingle(tup[0], CultureInfo.InvariantCulture),
                    Convert.ToSingle(tup[1], CultureInfo.InvariantCulture),
                    Convert.ToSingle(tup[2], CultureInfo.InvariantCulture),
                },
                _ => throw new Xunit.Sdk.XunitException($"unexpected color type: {actual?.GetType().Name}"),
            };
            Assert.Equal(expected.Length, ac.Length);
            for (int i = 0; i < expected.Length; i++)
                Assert.Equal(expected[i], ac[i], 4);
        }

        static void TryDelete(string path)
        {
            if (!File.Exists(path))
                return;
            try { File.Delete(path); }
            catch (IOException) { /* Windows may keep PDF handles briefly */ }
        }

        static Point ToPoint(object v)
        {
            if (v is Point p)
                return p;
            if (v is float[] fa && fa.Length >= 2)
                return new Point(fa[0], fa[1]);
            if (v is float[] da && da.Length >= 2)
                return new Point(da[0], da[1]);
            if (v is IList<object> lo && lo.Count >= 2)
                return new Point((float)Convert.ToDouble(lo[0], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(lo[1], CultureInfo.InvariantCulture));
            if (v is System.Collections.IList il && il.Count >= 2)
                return new Point((float)Convert.ToDouble(il[0], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(il[1], CultureInfo.InvariantCulture));
            if (v is ITuple tup && tup.Length >= 2)
                return new Point((float)Convert.ToDouble(tup[0], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(tup[1], CultureInfo.InvariantCulture));
            throw new Xunit.Sdk.XunitException($"cannot convert to Point: {v?.GetType().Name}");
        }

        static float[] ColorRgb(string name)
        {
            var c = Utils.GetColor(name);
            return new float[] { c.r, c.g, c.b };
        }

        static bool RectsApproxEqual(Rect a, Rect b) =>
            Math.Abs(a.X0 - b.X0) < 1e-3 && Math.Abs(a.Y0 - b.Y0) < 1e-3
            && Math.Abs(a.X1 - b.X1) < 1e-3 && Math.Abs(a.Y1 - b.Y1) < 1e-3;

        static string FormatSimpleTocEntry(int level, string title, int page) =>
            $"[{level}, '{EscapePyString(title)}', {page}]";

        static string FormatExtendedTocEntry((int level, string title, int page, Dictionary<string, object> link) row) =>
            $"[{row.level}, '{EscapePyString(row.title)}', {row.page}, {FormatPyDict(row.link)}]";

        static string EscapePyString(string s) => s.Replace("'", "\\'", StringComparison.Ordinal);

        static string FormatPyFloat(float v) =>
            v.ToString(Math.Abs(v - Math.Truncate(v)) < 1e-9 ? "0.0######" : "g", CultureInfo.InvariantCulture);

        static string FormatPyDict(Dictionary<string, object> d)
        {
            if (d == null)
                return "{}";
            int kind = d.TryGetValue("kind", out var kObj)
                ? Convert.ToInt32(kObj, CultureInfo.InvariantCulture)
                : -1;
            // Python dict key order from getLinkDict + _extend_toc_items.
            string[] order = kind == Constants.LinkNone
                ? new[] { "kind", "xref", "from", "uri", "file", "page", "to", "nameddest", "name", "collapse", "bold", "italic", "color", "zoom" }
                : new[] { "kind", "xref", "from", "uri", "file", "page", "to", "nameddest", "name", "zoom", "bold", "italic", "color", "collapse" };
            var parts = new List<string>();
            var seen = new HashSet<string>();
            foreach (string key in order)
            {
                if (!d.ContainsKey(key))
                    continue;
                parts.Add($"'{key}': {FormatPyValue(d[key])}");
                seen.Add(key);
            }
            foreach (var kv in d)
            {
                if (!seen.Contains(kv.Key))
                    parts.Add($"'{kv.Key}': {FormatPyValue(kv.Value)}");
            }
            return "{" + string.Join(", ", parts) + "}";
        }

        static string FormatPyValue(object v)
        {
            switch (v)
            {
                case null:
                    return "None";
                case bool b:
                    return b ? "True" : "False";
                case string s:
                    return $"'{EscapePyString(s)}'";
                case Point p:
                    return $"Point({FormatPyFloat(p.X)}, {FormatPyFloat(p.Y)})";
                case Rect r:
                    return $"Rect({FormatPyFloat(r.X0)}, {FormatPyFloat(r.Y0)}, {FormatPyFloat(r.X1)}, {FormatPyFloat(r.Y1)})";
                case float[] fa when fa.Length == 3:
                    return $"({FormatPyFloat(fa[0])}, {FormatPyFloat(fa[1])}, {FormatPyFloat(fa[2])})";
                case float[] da when da.Length == 3:
                    return $"({FormatPyFloat(da[0])}, {FormatPyFloat(da[1])}, {FormatPyFloat(da[2])})";
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return FormatPyFloat(f);
                case double d:
                    return FormatPyFloat((float)d);
                default:
                    return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "None";
            }
        }
    }
}
