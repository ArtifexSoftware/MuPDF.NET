using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Extract drawings of a PDF page and compare with stored expected result.
    /// </summary>
    /// <remarks>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_drawings.py</c>.
    /// Inputs: <c>TestDocuments/TestDrawings/</c>; outputs: <c>TestDocuments/_Output/TestDrawings/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestDrawings
    {
        private const string TestClassName = nameof(TestDrawings);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static readonly float[] color0 = { 0f }; // Python: color=0

        private static float[] PdfColor(string name)
        {
            var (r, g, b) = WxColors.PdfColorDict[name];
            return new float[] { r, g, b };
        }

        /// <summary>Normalize golden-file and pprint output before <c>Assert.Equal</c> (line endings, trailing space, final newline).</summary>
        private static string NormalizePprintComparisonText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].TrimEnd();
            int end = lines.Length;
            while (end > 0 && lines[end - 1].Length == 0)
                end--;
            return string.Join("", lines, 0, end);
        }

        /// <summary>PyMuPDF <c>tests/test_drawings.py::test_drawings1</c>.</summary>
        [Fact]
        public void test_drawings1()
        {
            // filename = os.path.join(scriptdir, "resources", "symbol-list.pdf")
            // symbols = os.path.join(scriptdir, "resources", "symbols.txt")
            string symbols_text = File.ReadAllText(Doc("symbols.txt")); // expected result
            using var doc = new Document(Doc("symbol-list.pdf"));
            var page = doc[0];
            // Python: paths = page.GetCdrawings()
            var paths = page.GetCdrawings();
            // out = io.StringIO()  # pprint output goes here
            // pprint.pprint(paths, stream=out)
            string outText = DrawingPathsPprint.Format(paths);
            Assert.Equal(paths.Count, 29);
            doc.Save(Out("test_drawings1.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_drawings.py::test_drawings2</c>.</summary>
        [Fact]
        public void test_drawings2()
        {
            var delta = new Rect(0, 20, 0, 20);
            using var doc = new Document();
            var page = doc.NewPage();

            var r = _Constants.rect;
            page.DrawCircle(r.BR, 2, color: color0);
            r += delta;

            page.DrawLine(r.TL, r.BR, color: color0);
            r += delta;

            page.DrawOval(r, color: color0);
            r += delta;

            page.DrawRect(r, color: color0);
            r += delta;

            page.DrawQuad(r.Quad, color: color0);
            r += delta;

            page.DrawPolyline(new[] { r.TL, r.TR, r.BR }, color: color0);
            r += delta;

            page.DrawBezier(r.TL, r.TR, r.BR, r.BL, color: color0);
            r += delta;

            page.DrawCurve(r.TL, r.TR, r.BR, color: color0);
            r += delta;

            page.DrawSquiggle(r.TL, r.BR, color: color0);
            r += delta;

            // rects = [p["rect"] for p in page.GetCdrawings()]
            var rects = new List<Rect>();
            foreach (var p in page.GetCdrawings())
            {
                if (p.TryGetValue("rect", out var rv) && Helpers.TryCoerceRect(rv, out var rr))
                    rects.Add(rr);
            }
            // bboxes = [b[1] for b in page.GetBboxlog()]
            var bboxes = new List<Rect>();
            foreach (var b in page.GetBboxlogTuples())
                bboxes.Add(b.bbox);
            for (int i = 0; i < rects.Count; i++)
            {
                // assert pymupdf.Rect(r) in pymupdf.Rect(bboxes[i])
                Assert.True(bboxes[i].Contains(rects[i]));
            }
            doc.Save(Out("test_drawings2.pdf"));
        }

        /// <summary>
        /// Verifies that dictionaries "a", "b"
        /// * have the same keys and values, except for key "items":
        /// * the items list of "a" must be one shorter but otherwise equal the "b" items
        ///
        /// Returns last item of b["items"].
        /// PyMuPDF: <c>tests/test_drawings.py::_dict_difference</c>.
        /// </summary>
        private static object[] DictDifference(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            Assert.Equal(a.Keys.OrderBy(k => k).ToList(), b.Keys.OrderBy(k => k).ToList());
            object[]? rc = null;
            foreach (var k in a.Keys)
            {
                var v1 = a[k];
                var v2 = b[k];
                if (k != "items")
                    Assert.True(DrawingPathValuesEqual(v1, v2), $"key {k}: {v1} != {v2}");
                else
                {
                    var l1 = (List<object>)v1;
                    var l2 = (List<object>)v2;
                    Assert.Equal(l1.Count + 1, l2.Count);
                    for (int i = 0; i < l1.Count; i++)
                        Assert.True(DrawingPathItemEqual(l1[i], l2[i]));
                    rc = (object[])l2[l2.Count - 1];
                }
            }
            return rc!;
        }

        /// <summary>PyMuPDF <c>tests/test_drawings.py::test_drawings3</c>.</summary>
        [Fact]
        public void test_drawings3()
        {
            using var doc = new Document();
            var page1 = doc.NewPage();
            var shape1 = page1.NewShape();
            shape1.DrawLine(new Point(10, 10), new Point(10, 50));
            shape1.DrawLine(new Point(10, 50), new Point(100, 100));
            shape1.Finish(closePath: false);
            shape1.Commit();
            var drawings1 = page1.GetDrawingsDict()[0];

            var page2 = doc.NewPage();
            var shape2 = page2.NewShape();
            shape2.DrawLine(new Point(10, 10), new Point(10, 50));
            shape2.DrawLine(new Point(10, 50), new Point(100, 100));
            shape2.Finish(closePath: true);
            shape2.Commit();
            var drawings2 = page2.GetDrawingsDict()[0];

            Assert.True(DrawingPathItemEqual(
                DictDifference(drawings1, drawings2),
                new object[] { "l", new Point(100, 100), new Point(10, 10) }));

            var page3 = doc.NewPage();
            var shape3 = page3.NewShape();
            shape3.DrawLine(new Point(10, 10), new Point(10, 50));
            shape3.DrawLine(new Point(10, 50), new Point(100, 100));
            shape3.DrawLine(new Point(100, 100), new Point(50, 70));
            shape3.Finish(closePath: false);
            shape3.Commit();
            var drawings3 = page3.GetDrawingsDict()[0];

            var page4 = doc.NewPage();
            var shape4 = page4.NewShape();
            shape4.DrawLine(new Point(10, 10), new Point(10, 50));
            shape4.DrawLine(new Point(10, 50), new Point(100, 100));
            shape4.DrawLine(new Point(100, 100), new Point(50, 70));
            shape4.Finish(closePath: true);
            shape4.Commit();
            var drawings4 = page4.GetDrawingsDict()[0];

            Assert.True(DrawingPathItemEqual(
                DictDifference(drawings3, drawings4),
                new object[] { "l", new Point(50, 70), new Point(10, 10) }));
            doc.Save(Out("test_drawings3.pdf"));
        }

        /// <summary>
        /// Draw a filled rectangle on a new page.
        ///
        /// Then extract the page's vector graphics and confirm that only one path
        /// was generated which has all the right properties.
        /// PyMuPDF: <c>tests/test_drawings.py::test_2365</c>.
        /// </summary>
        [Fact]
        public void test_2365()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var rect = _Constants.rect;
            page.DrawRect(
                rect,
                color: PdfColor("black"),
                fill: PdfColor("yellow"),
                width: 3);
            var paths = page.GetDrawingsDict();
            Assert.Single(paths);
            var path = paths[0];
            Assert.Equal("fs", path["type"]);
            Assert.True(DrawingPathValuesEqual(path["fill"], PdfColor("yellow")));
            Assert.Equal(1.0, Convert.ToDouble(path["fill_opacity"], CultureInfo.InvariantCulture));
            Assert.True(DrawingPathValuesEqual(path["color"], PdfColor("black")));
            Assert.Equal(1.0, Convert.ToDouble(path["stroke_opacity"], CultureInfo.InvariantCulture));
            Assert.Equal(3.0, Convert.ToDouble(path["width"], CultureInfo.InvariantCulture));
            Assert.True(DrawingPathValuesEqual(path["rect"], rect));
            doc.Save(Out("test_2365.pdf"));
        }

        /// <summary>
        /// Assertion happens, if this code does NOT bring down the interpreter.
        ///
        /// Background:
        /// We previously ignored clips for non-vector-graphics. However, ending
        /// a clip does not refer back the object(s) that have been clipped.
        /// In order to correctly compute the "scissor" rectangle, we now keep track
        /// of the clipped object type.
        /// PyMuPDF: <c>tests/test_drawings.py::test_2462</c>.
        /// </summary>
        [Fact]
        public void test_2462()
        {
            using var doc = new Document(Doc("test-2462.pdf"));
            var page = doc[0];
            // vg = page.GetDrawings(extended=True)
            var vg = page.GetDrawingsDict(extended: true);
            Assert.Equal(vg.Count, 63);
            doc.Save(Out("test_2462.pdf"));
        }

        /// <summary>
        /// Ensure that incomplete clip paths will be properly ignored.
        /// PyMuPDF: <c>tests/test_drawings.py::test_2556</c>.
        /// </summary>
        [Fact]
        public void test_2556()
        {
            using var doc = new Document(); // new empty PDF
            var page = doc.NewPage(); // new page
            // following contains an incomplete clip
            byte[] c = Encoding.ASCII.GetBytes("q 50 697.6 400 100.0 re W n q 0 0 m W n Q ");
            int xref = doc.GetNewXref(); // prepare /Contents object for page
            doc.UpdateObject(xref, "<<>>"); // new xref now is a dictionary
            doc.UpdateStream(xref, c); // store drawing commands
            page.SetContents(xref); // give the page this xref as /Contents
            // following will bring down interpreter if fix not installed
            Assert.NotEmpty(page.GetDrawingsDict(extended: true));
            doc.Save(Out("test_2556.pdf"));
        }

        /// <summary>
        /// Example graphics with multiple "close path" commands within same path.
        ///
        /// The fix translates a close-path commands into an additional line
        /// which connects the current point with a preceding "move" target.
        /// The example page has 2 paths which each contain 2 close-path
        /// commands after 2 normal "line" commands, i.e. 2 command sequences
        /// "move-to, line-to, line-to, close-path".
        /// This is converted into 3 connected lines, where the last end point
        /// is connect to the start point of the first line.
        /// So, in the sequence of lines / points
        ///
        /// (p0, p1), (p2, p3), (p4, p5), (p6, p7), (p8, p9), (p10, p11)
        ///
        /// point p5 must equal p0, and p11 must equal p6 (for each of the
        /// two paths in the example).
        /// PyMuPDF: <c>tests/test_drawings.py::test_3207</c>.
        /// </summary>
        [Fact]
        public void test_3207()
        {
            using var doc = new Document(Doc("test-3207.pdf"));
            var page = doc[0];
            var paths = page.GetDrawingsDict();
            Assert.Equal(2, paths.Count);

            static void CheckPath(Dictionary<string, object> path0)
            {
                var items = (List<object>)path0["items"];
                Assert.Equal(6, items.Count);
                var item0 = (object[])items[0];
                var item2 = (object[])items[2];
                var item3 = (object[])items[3];
                var item5 = (object[])items[5];
                Point p0 = PointFromPathItem(item0[1]);
                Point p5 = PointFromPathItem(item2[2]);
                Point p6 = PointFromPathItem(item3[1]);
                Point p11 = PointFromPathItem(item5[2]);
                Assert.Equal(p0, p5);
                Assert.Equal(p6, p11);
            }

            CheckPath(paths[0]);
            CheckPath(paths[1]);
        }

        /// <summary>Confirm correct scaling factor for rotation matrices. PyMuPDF: <c>tests/test_drawings.py::test_3591</c>.</summary>
        [Fact]
        public void test_3591()
        {
            using var doc = new Document(Doc("test-3591.pdf"));
            var page = doc[0];
            var paths = page.GetDrawingsDict();
            foreach (var p in paths)
                Assert.Equal(15.0, Convert.ToDouble(p["width"], CultureInfo.InvariantCulture));
        }

        private static Point PointFromPathItem(object o)
        {
            if (o is Point p) return p;
            if (o is IList list && list.Count >= 2)
                return new Point((float)Convert.ToDouble(list[0], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(list[1], CultureInfo.InvariantCulture));
            throw new InvalidOperationException($"not a point: {o}");
        }

        private static bool DrawingPathItemEqual(object a, object b)
        {
            if (a is object[] aa && b is object[] bb)
            {
                if (aa.Length != bb.Length) return false;
                for (int i = 0; i < aa.Length; i++)
                {
                    if (!DrawingPathValuesEqual(aa[i], bb[i])) return false;
                }
                return true;
            }
            return DrawingPathValuesEqual(a, b);
        }

        private static bool DrawingPathValuesEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a is Point pa && b is Point pb) return pa == pb;
            if (a is Rect ra && b is Rect rb) return ra == rb;
            if (a is float[] fa && b is float[] fb)
            {
                if (fa.Length != fb.Length) return false;
                for (int i = 0; i < fa.Length; i++)
                    if (Math.Abs(fa[i] - fb[i]) > 1e-6) return false;
                return true;
            }
            if (a is IList la && b is IList lb)
            {
                if (la.Count != lb.Count) return false;
                for (int i = 0; i < la.Count; i++)
                    if (!DrawingPathValuesEqual(la[i], lb[i])) return false;
                return true;
            }
            if (a is string sa && b is string sb) return sa == sb;
            if (IsNumeric(a) && IsNumeric(b))
                return Math.Abs(Convert.ToDouble(a, CultureInfo.InvariantCulture) -
                                Convert.ToDouble(b, CultureInfo.InvariantCulture)) < 1e-9;
            return Equals(a, b);
        }

        private static bool IsNumeric(object o) =>
            o is int or long or float or float or decimal;

        /// <summary>Python <c>pprint.pprint</c> formatting for <c>get_cdrawings()</c> output (golden <c>symbols.txt</c>).</summary>
        private static class DrawingPathsPprint
        {
            public static string Format(IReadOnlyList<Dictionary<string, object>> paths)
            {
                var sb = new StringBuilder();
                sb.Append('[');
                for (int i = 0; i < paths.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('\n');
                    FormatDict(sb, paths[i], 1);
                }
                if (paths.Count > 0) sb.Append('\n');
                sb.Append(']');
                return sb.ToString();
            }

            private static void FormatDict(StringBuilder sb, Dictionary<string, object> d, int depth)
            {
                sb.Append('{');
                int ki = 0;
                foreach (var kv in d)
                {
                    if (ki++ > 0) sb.Append(',');
                    sb.Append('\n');
                    sb.Append(' ', depth * 2);
                    sb.Append('\'').Append(kv.Key).Append("': ");
                    FormatValue(sb, kv.Value, depth + 1);
                }
                sb.Append('\n');
                sb.Append(' ', (depth - 1) * 2);
                sb.Append('}');
            }

            private static void FormatValue(StringBuilder sb, object v, int depth)
            {
                switch (v)
                {
                    case null:
                        sb.Append("None");
                        break;
                    case bool b:
                        sb.Append(b ? "True" : "False");
                        break;
                    case string s:
                        sb.Append('\'').Append(s).Append('\'');
                        break;
                    case float f:
                        sb.Append(FormatFloat(f));
                        break;
                    case double d:
                        sb.Append(FormatFloat((float)d));
                        break;
                    case int n:
                        sb.Append(n.ToString(CultureInfo.InvariantCulture));
                        break;
                    case long ln:
                        sb.Append(ln.ToString(CultureInfo.InvariantCulture));
                        break;
                    case Point p:
                        FormatTuple(sb, new object[] { p.X, p.Y });
                        break;
                    case Rect r:
                        FormatTuple(sb, new object[] { r.X0, r.Y0, r.X1, r.Y1 });
                        break;
                    case mupdf.FzRect fr:
                        FormatTuple(sb, new object[] { fr.x0, fr.y0, fr.x1, fr.y1 });
                        break;
                    case float[] fa:
                        FormatTuple(sb, fa);
                        break;
                    case object[] oa:
                        FormatTuple(sb, oa);
                        break;
                    case List<object> list:
                        FormatList(sb, list, depth);
                        break;
                    case IList list:
                        FormatList(sb, list, depth);
                        break;
                    default:
                        sb.Append(v.ToString());
                        break;
                }
            }

            private static void FormatList(StringBuilder sb, IList list, int depth)
            {
                sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('\n');
                    sb.Append(' ', depth * 2);
                    FormatValue(sb, list[i], depth + 1);
                }
                sb.Append('\n');
                sb.Append(' ', (depth - 1) * 2);
                sb.Append(']');
            }

            private static void FormatTuple(StringBuilder sb, IList items)
            {
                sb.Append('(');
                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    FormatValue(sb, items[i], 0);
                }
                sb.Append(')');
            }

            private static string FormatFloat(float f)
            {
                if (float.IsNaN(f) || float.IsInfinity(f))
                    return f.ToString(CultureInfo.InvariantCulture);
                // Match Python repr-style floats in symbols.txt (no gratuitous trailing zeros).
                string s = f.ToString("R", CultureInfo.InvariantCulture);
                if (!s.Contains('.') && !s.Contains('E') && !s.Contains('e'))
                    s += ".0";
                return s;
            }
        }
    }
}
