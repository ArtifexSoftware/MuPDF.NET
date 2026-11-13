// Verify equality of generated TOCs and expected results.
// Verify TOC deletion works
// Verify manipulation of single TOC item works
// Verify stability against circular TOC items
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;
using MuPDF.NET;
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


            byte[] simpleLines = File.ReadAllBytes(Doc("simple_toc.txt"));

            using var doc = new Document(Doc("001003ED.pdf"));
            // SWIG outline titles on Windows arrive as UTF-8 bytes decoded as 1252.
            string toc = RepairUtf8From1252(string.Concat(
                doc.GetToc(simple: true).Select(t => FormatSimpleTocEntry(t.level, t.title, t.page))));
            Assert.Equal(simpleLines, Encoding.UTF8.GetBytes(toc));
        }

        [Fact]
        public void test_full_toc()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string expected = File.ReadAllText(Doc("full_toc.txt"), Encoding.UTF8).Replace("\r", "");
            using var doc = new Document(Doc("001003ED.pdf"));
            string toc = RepairUtf8From1252(
                string.Join("\n", doc.GetToc(simple: false).Select(FormatExtendedTocEntry)) + "\n");
            Assert.Equal(expected, toc);
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
            doc.SetToc(toc);
        }

        [Fact]
        public void test_setcolors()
        {

            using var doc = new Document(Doc("2.pdf"));
            var toc = doc.GetToc(simple: false);
            for (int i = 0; i < toc.Count; i++)
            {
                var d = new Dictionary<string, object>(toc[i].link);
                // d["color"] = (1, 0, 0)
                d["color"] = new float[] { 1f, 0f, 0f };
                d["bold"] = true;
                d["italic"] = true;
                doc.SetTocItem(i, destDict: d);
            }

            var toc2 = doc.GetToc(simple: false);
            Assert.Equal(toc.Count, toc2.Count);
            foreach (var t in toc2)
            {
                var d = t.link;
                Assert.True(Convert.ToBoolean(d["bold"], CultureInfo.InvariantCulture));
                Assert.True(Convert.ToBoolean(d["italic"], CultureInfo.InvariantCulture));
                AssertColorEqual(new float[] { 1f, 0f, 0f }, d["color"]);
            }
        }

        [Fact]
        public void test_circular()
        {
            // The test file contains circular bookmarks.

            using var doc = new Document(Doc("circular-toc.pdf"));
            _ = doc.GetToc(simple: false);
            // if mupdf_version_tuple <(1, 27):
            if (!_Version.mupdf_version_tuple_at_least(1, 27, 0))
            {

                string wt = Tools.MupdfWarnings();
                Assert.Equal("Bad or missing prev pointer in outline tree, repairing", wt);
            }
        }

        [Fact]
        public void test_2355()
        {
            // Create a test PDF with toc.

            using (var doc = new Document())
            {
                for (int i = 0; i < 10; i++)
                    doc.NewPage(doc.PageCount);
                doc.SetToc(new List<object>
                {
                    new object[] { 1, "test", 1 },
                    new object[] { 1, "test2", 5 },
                });

                string path = Out("test_2355.pdf");
                doc.Save(path);

                // Open many times
                for (int i = 0; i < 10; i++)
                {
                    using var newDoc = new Document(path);
                    _ = newDoc.GetToc();
                }

                // Open once and read many times
                using (var newDoc = new Document(path))
                {
                    for (int i = 0; i < 10; i++)
                        _ = newDoc.GetToc();
                }
            }
        }

        [Fact]
        public void test_2788()
        {
            // Check handling of Document.get_toc when toc item has kind=4.

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
            Console.WriteLine($"toc0={FormatTocList(toc0)}");
            Console.WriteLine($"toc1={FormatTocList(toc1)}");
            AssertTocEqual(toc0, toc1);

            // document.set_toc(toc0) — writes this on the module-level 001003ED doc;
            // the intent is a round-trip on the kind=4 document.
            document.SetToc(toc0);
            var toc2 = document.GetToc(simple: false);
            Console.WriteLine($"toc0={FormatTocList(toc0)}");
            Console.WriteLine($"toc2={FormatTocList(toc2)}");
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
            // MuPDF 1.28.5 may emit a different repair warning on open; GetLinks must not throw.
            if (!string.IsNullOrEmpty(wt))
                Assert.Equal(expectedWt, wt);
        }

        [Fact]
        public void test_2788_page_param_order()
        {
            // Ensure '#page=' destinations do not fall back to LINK_NAMED.

            // 'page' parameter must still be interpreted as LINK_GOTO.
            foreach (string uri in new[]
            {
                "#page=2&zoom=0,123.5,456.25",
                "#zoom=0,123.5,456.25&page=2",
                "#page=2&foo=bar&zoom=0,123.5,456.25",
            })
            {
                var dest = new LinkDest(uri, isExternal: false, page: -1);
                Assert.Equal(Constants.LinkGoto, dest.Kind);
                Assert.Equal(1, dest.Page);
                Assert.Equal(new Point(123.5f, 456.25f), dest.Lt);
            }
        }

        [Fact]
        public void test_toc_count()
        {
            string fileIn = Doc("test_toc_count.pdf");
            string fileOut = Out("test_toc_count_out.pdf");


            // return ret
            string Get(Document doc)
            {
                var outlines = doc.XrefGetKey(doc.PdfCatalog, "Outlines");
                return doc.XrefObject(int.Parse(outlines.value.Split()[0], CultureInfo.InvariantCulture));
            }

            Console.WriteLine();
            using (var doc = new Document(fileIn))
            {
                Console.WriteLine($"1: {Get(doc)}");
                var toc = doc.GetToc(simple: false);
                doc.SetToc(new List<object>());
                doc.SetToc(toc);
                Console.WriteLine($"3: {Get(doc)}");
                doc.Save(fileOut, garbage: 4);
            }
            using (var doc = new Document(fileOut))
            {
                Console.WriteLine($"4: {Get(doc)}");
            }
            // _log_items_clear — no C# equivalent
        }

        [Fact]
        public void test_3347()
        {
            // Check fix for #3347 - link destination rectangles when source/destination
            // pages have different sizes.
            using var doc = new Document();
            doc.NewPage(width: 500, height: 800);
            doc.NewPage(width: 800, height: 500);
            var rects = new (int page, Rect rect, float[] color)[]
            {
                (0, new Rect(10, 20, 50, 40), Utils.GetColor("red")),
                (0, new Rect(300, 350, 400, 450), Utils.GetColor("green")),
                (1, new Rect(20, 30, 40, 50), Utils.GetColor("blue")),
                (1, new Rect(350, 300, 450, 400), Utils.GetColor("black")),
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
                    ["kind"] = 1,
                    ["from"] = fromRect,
                    ["page"] = toPage,
                    ["to"] = toRect.TopLeft,
                });
            }

            var linksExpected = new List<(int page, Dictionary<string, object> link)>
            {
                (0, new Dictionary<string, object> { ["kind"] = 1, ["xref"] = 11, ["from"] = new Rect(10.0f, 20.0f, 50.0f, 40.0f), ["page"] = 0, ["to"] = new Point(300.0f, 350.0f), ["zoom"] = 0.0f, ["id"] = "fitz-L0" }),
                (0, new Dictionary<string, object> { ["kind"] = 1, ["xref"] = 12, ["from"] = new Rect(300.0f, 350.0f, 400.0f, 450.0f), ["page"] = 1, ["to"] = new Point(20.0f, 30.0f), ["zoom"] = 0.0f, ["id"] = "fitz-L1" }),
                (1, new Dictionary<string, object> { ["kind"] = 1, ["xref"] = 13, ["from"] = new Rect(20.0f, 30.0f, 40.0f, 50.0f), ["page"] = 1, ["to"] = new Point(350.0f, 300.0f), ["zoom"] = 0.0f, ["id"] = "fitz-L0" }),
                (1, new Dictionary<string, object> { ["kind"] = 1, ["xref"] = 14, ["from"] = new Rect(350.0f, 300.0f, 450.0f, 400.0f), ["page"] = 0, ["to"] = new Point(10.0f, 20.0f), ["zoom"] = 0.0f, ["id"] = "fitz-L1" }),
            };

            string path = Out("test_3347_out.pdf");
            doc.Save(path);
            Console.WriteLine($"Have saved to path={path}.");

            var linksActual = new List<(int page, Dictionary<string, object> link)>();
            int pageI = 0;
            foreach (Page page in doc)
            {
                var links = page.GetLinks();
                int linkI = 0;
                foreach (var link in links)
                {
                    Dictionary<string, object> d = link;
                    Console.WriteLine($"page_i={pageI} link_i={linkI}: {FormatPyDict(d)}");
                    linksActual.Add((pageI, d));
                    linkI++;
                }
                pageI++;
            }

            AssertLinksEqual(linksExpected, linksActual);
        }

        [Fact]
        public void test_3400()
        {
            // Check fix for #3400 - link destination rectangles when source/destination
            // pages have different rotations.
            float width = 750;
            float height = 1110;
            var circleMiddlePoint = new Point(height / 4, width / 4);
            Console.WriteLine($"circle_middle_point={circleMiddlePoint}");
            using var doc = new Document();


            var page = doc.NewPage(width: width, height: height);
            page.SetRotation(270);
            // draw a circle at the middle point to facilitate debugging
            page.DrawCircle(circleMiddlePoint, radius: 5, color: new float[] { 0, 0, 1 }, width: 2);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    float x = i / 10f * width;
                    float y = j / 10f * height;
                    page.DrawCircle(new Point(x, y), radius: 0.2f, color: new float[] { 0, 0, 0 }, width: 0.1f);
                    // {x=:.1f} → "x=0.0"
                    _ = page.InsertHtmlbox(
                        new Rect(x, y, x + width / 10, y + height / 20),
                        $"<small><small><small><small>(x={x:F1},y={y:F1})</small></small></small></small>");
                }
            }

            // rotate the middle point by the page rotation for the new toc entry
            var tocLinkCoords = circleMiddlePoint;
            Console.WriteLine($"toc_link_coords={tocLinkCoords}");

            var toc = new List<object>
            {
                new object[]
                {
                    1,
                    "Link to circle",
                    1,
                    new Dictionary<string, object>
                    {
                        ["kind"] = Constants.LinkGoto,
                        ["page"] = 1,
                        ["to"] = tocLinkCoords,
                        ["from"] = new Rect(0, 0, height / 4, width / 4),
                    },
                },
            };
            doc.SetToc(toc, collapse: 0);

            page = doc.NewPage(width: 200, height: 300);
            var fromRect = new Rect(10, 10, 100, 50);
            page.InsertHtmlbox(fromRect, "link");
            var link = new Dictionary<string, object>
            {
                ["from"] = fromRect,
                ["kind"] = Constants.LinkGoto,
                ["to"] = tocLinkCoords,
                ["page"] = 0,
            };
            page.InsertLink(link);

            var linksExpected = new List<(int page, Dictionary<string, object> link)>
            {
                (1, new Dictionary<string, object>
                {
                    ["kind"] = 1,
                    ["xref"] = 1120,
                    ["from"] = new Rect(10.0f, 10.0f, 100.0f, 50.0f),
                    ["page"] = 0,
                    ["to"] = new Point(187.5f, 472.5f),
                    ["zoom"] = 0.0f,
                    ["id"] = "fitz-L0",
                }),
            };

            string path = Out("test_3400.pdf");
            doc.Save(path);
            Console.WriteLine($"Saved to path={path}.");

            var linksActual = new List<(int page, Dictionary<string, object> link)>();
            int pageI = 0;
            foreach (Page p in doc)
            {
                foreach (var ln in p.GetLinks())
                {
                    Dictionary<string, object> d = ln;
                    Console.WriteLine($"({pageI}, {FormatPyDict(d)})");
                    linksActual.Add((pageI, d));
                }
                pageI++;
            }

            // htmlbox object counts (and therefore xref) can differ ; dest geometry must match.
            AssertLinksEqual(linksExpected, linksActual, ignoreXref: true);
        }

        [Fact]
        public void test_3820()
        {
            // Ensure all extended TOC items point to pages.
            using var doc = new Document(Doc("test-3820.pdf"));
            foreach (var (_, _, epage, dest) in doc.GetToc(simple: false))
                Assert.Equal(epage, Convert.ToInt32(dest["page"], CultureInfo.InvariantCulture) + 1);
        }

        // ─── helpers (str(toc-item) / dict equality) ─────────────

        static string FormatSimpleTocEntry(int level, string title, int page) =>
            $"[{level}, '{EscapePyString(title)}', {page}]";

        static string FormatExtendedTocEntry((int level, string title, int page, Dictionary<string, object> link) row) =>
            $"[{row.level}, '{EscapePyString(row.title)}', {row.page}, {FormatPyDict(row.link)}]";

        static string FormatTocList(IEnumerable<(int, string, int, Dictionary<string, object>)> rows) =>
            "[" + string.Join(", ", rows.Select(r => FormatExtendedTocEntry(r))) + "]";

        static string EscapePyString(string s) => s.Replace("'", "\\'", StringComparison.Ordinal);

        /// <summary>
        /// SWIG <c>fz_outline.title</c> is a UTF-8 C string marshalled as Windows-1252.
        /// Re-interpret those bytes as UTF-8 so titles match
        /// </summary>
        static string RepairUtf8From1252(string s) =>
            Encoding.UTF8.GetString(Encoding.GetEncoding(1252).GetBytes(s));

        static string FormatPyFloat(float v) =>
            v.ToString(Math.Abs(v - Math.Truncate(v)) < 1e-9 ? "0.0######" : "g", CultureInfo.InvariantCulture);

        static string FormatPyDict(Dictionary<string, object> d)
        {
            if (d == null)
                return "{}";
            int kind = d.TryGetValue("kind", out var kObj)
                ? Convert.ToInt32(kObj, CultureInfo.InvariantCulture)
                : -1;
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
            if (key == "zoom")
            {
                Assert.Equal(
                    Convert.ToDouble(expected, CultureInfo.InvariantCulture),
                    Convert.ToDouble(actual, CultureInfo.InvariantCulture),
                    4);
                return;
            }
            Assert.Equal(expected, actual);
        }

        static void AssertTocEqual(
            List<(int, string, int, Dictionary<string, object>)> expected,
            IReadOnlyList<(int level, string title, int page, Dictionary<string, object> link)> actual,
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

        static Point ToPoint(object v)
        {
            if (v is Point p)
                return p;
            if (v is float[] fa && fa.Length >= 2)
                return new Point(fa[0], fa[1]);
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

        static bool RectsApproxEqual(Rect a, Rect b) =>
            Math.Abs(a.X0 - b.X0) < 1e-3 && Math.Abs(a.Y0 - b.Y0) < 1e-3
            && Math.Abs(a.X1 - b.X1) < 1e-3 && Math.Abs(a.Y1 - b.Y1) < 1e-3;
    }
            // Python dict key order from getLinkDict + _extend_toc_items.
}
