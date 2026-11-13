using SkiaSharp;
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
    /// Inputs: <c>TestDocuments/TestDrawings/</c>; outputs: <c>TestDocuments/_Output/TestDrawings/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestDrawings
    {
        private const string TestClassName = nameof(TestDrawings);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary><c>color=0</c> (DeviceGray black).</summary>
        private static readonly float[] color0 = { 0f };

        private static float[] PdfColor(string name)
        {
            var (r, g, b) = WxColors.PdfColorDict[name];
            return new float[] { r, g, b };
        }

        /// <summary>Regression test: drawings1.</summary>
        [Fact]
        public void test_drawings1()
        {
            string symbolsText = File.ReadAllText(Doc("symbols.txt"));
            using var doc = new Document(Doc("symbol-list.pdf"));
            var page = doc[0];
            var paths = page.GetCdrawings();
            string outText = DrawingPathsPprint.Format(paths);
            Assert.Equal(NormalizeNl(symbolsText), NormalizeNl(outText));
        }

        /// <summary>Regression test: drawings2.</summary>
        [Fact]
        public void test_drawings2()
        {
            using var doc = new Document();
            var delta = new Rect(0, 20, 0, 20);
            var page = doc.NewPage();

            var r = new Rect(100, 100, 200, 200);
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

            var rects = new List<Rect>();
            foreach (var p in page.GetCdrawings())
            {
                if (p.TryGetValue("rect", out var rv) && Helpers.TryCoerceRect(rv, out var rr))
                    rects.Add(rr);
            }
            var bboxes = new List<Rect>();
            foreach (var b in page.GetBboxlogTuples())
                bboxes.Add(b.bbox);
            for (int i = 0; i < rects.Count; i++)
                Assert.True(bboxes[i].Contains(rects[i]));
            doc.Save(Out("test_drawings2.pdf"));
        }

        /// <summary>
        /// Verifies that dictionaries "a", "b"
        /// * have the same keys and values, except for key "items":
        /// * the items list of "a" must be one shorter but otherwise equal the "b" items
        /// Returns last item of b["items"].
        /// </summary>
        private static object[] DictDifference(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            Assert.Equal(a.Keys.OrderBy(k => k).ToList(), b.Keys.OrderBy(k => k).ToList());
            object[] rc = null;
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
            return rc;
        }

        /// <summary>Regression test: drawings3.</summary>
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
        /// Then extract the page's vector graphics and confirm that only one path
        /// was generated which has all the right properties.
        /// </summary>
        [Fact]
        public void test_2365()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var rect = new Rect(100, 100, 200, 200);
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
        /// Background:
        /// We previously ignored clips for non-vector-graphics. However, ending
        /// a clip does not refer back the object(s) that have been clipped.
        /// In order to correctly compute the "scissor" rectangle, we now keep track
        /// of the clipped object type.
        /// </summary>
        [Fact]
        public void test_2462()
        {
            using var doc = new Document(Doc("test-2462.pdf"));
            var page = doc[0];
            var vg = page.GetDrawingsDict(extended: true);
            Assert.Equal(63, vg.Count);
        }

        /// <summary>Ensure that incomplete clip paths will be properly ignored.</summary>
        [Fact]
        public void test_2556()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            byte[] c = Encoding.ASCII.GetBytes("q 50 697.6 400 100.0 re W n q 0 0 m W n Q ");
            int xref = doc.GetNewXref();
            doc.UpdateObject(xref, "<<>>");
            doc.UpdateStream(xref, c);
            page.SetContents(xref);
            Assert.NotEmpty(page.GetDrawingsDict(extended: true));
            doc.Save(Out("test_2556.pdf"));
        }

        /// <summary>
        /// Example graphics with multiple "close path" commands within same path.
        /// The fix translates a close-path commands into an additional line
        /// which connects the current point with a preceding "move" target.
        /// The example page has 2 paths which each contain 2 close-path
        /// commands after 2 normal "line" commands, i.e. 2 command sequences
        /// "move-to, line-to, line-to, close-path".
        /// This is converted into 3 connected lines, where the last end point
        /// is connect to the start point of the first line.
        /// So, in the sequence of lines / points
        /// (p0, p1), (p2, p3), (p4, p5), (p6, p7), (p8, p9), (p10, p11)
        /// point p5 must equal p0, and p11 must equal p6 (for each of the
        /// two paths in the example).
        /// </summary>
        [Fact]
        public void test_3207()
        {
            using var doc = new Document(Doc("test-3207.pdf"));
            var page = doc[0];
            var paths = page.GetDrawingsDict();
            Assert.Equal(2, paths.Count);

            static void CheckPath(Dictionary<string, object> path)
            {
                var items = (List<object>)path["items"];
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

        /// <summary>Confirm correct scaling factor for rotation matrices.</summary>
        [Fact]
        public void test_3591()
        {
            using var doc = new Document(Doc("test-3591.pdf"));
            var page = doc[0];
            var paths = page.GetDrawingsDict();
            foreach (var p in paths)
                Assert.Equal(15.0, Convert.ToDouble(p["width"], CultureInfo.InvariantCulture));
        }

        /// <summary>lineJoin must not be scaled by pathfactor.</summary>
        [Fact]
        public void test_4954_1()
        {
            string pathOut = Out("test_4954_1.pdf");
            byte[] content = Encoding.ASCII.GetBytes(
                "q\n0.12 0 0 0.12 0 0 cm\n2 j\n6 w\n100 100 m\n800 100 l\nS\nQ\n");
            using (var doc = new Document())
            {
                var page = doc.NewPage(width: 200, height: 200);
                var shape = page.NewShape();
                shape.DrawLine(new Point(0, 0), new Point(1, 1));
                shape.Finish(color: new float[] { 0, 0, 0 }, width: 0.1f);
                shape.Commit();
                doc.UpdateStream(page.GetContents()[0], content, compress: false);
                doc.Save(pathOut);
            }
            using (var doc = new Document(pathOut))
            {
                var d = doc[0].GetDrawingsDict().Last();
                Assert.Equal(2.0, Convert.ToDouble(d["lineJoin"], CultureInfo.InvariantCulture));
            }
        }

        /// <summary>stroke width uses determinant pathfactor.</summary>
        [Fact]
        public void test_4954_2()
        {
            string pathOut = Out("test_4954_2.pdf");
            byte[] content = Encoding.ASCII.GetBytes(
                "q\n2 0 0 3 0 0 cm\n1 w\n10 10 m\n90 10 l\nS\nQ\n");
            using (var doc = new Document())
            {
                var page = doc.NewPage(width: 200, height: 200);
                var shape = page.NewShape();
                shape.DrawLine(new Point(0, 0), new Point(1, 1));
                shape.Finish(color: new float[] { 1, 0, 0 }, width: 0.1f);
                shape.Commit();
                doc.UpdateStream(page.GetContents()[0], content, compress: false);
                doc.Save(pathOut);
            }
            using (var doc = new Document(pathOut))
            {
                var d = doc[0].GetDrawingsDict().Last();
                double width = Convert.ToDouble(d["width"], CultureInfo.InvariantCulture);
                Assert.True(Math.Abs(width - 2.449) < 0.01, $"width={width}");
            }
        }

        /// <summary>
        /// An exception raised by a get_cdrawings callback must not crash the process.
        /// It used to: jm_append_merge reports the failed callback via messagef
        /// while the exception is still set, so messagev's import of
        /// returned NULL, which was then dereferenced.
        /// C# analog: InvokeCallback catches the throw, prints the same message, and GetCdrawings returns.
        /// </summary>
        [Fact]
        public void test_cdrawings_callback_exception()
        {
            using var document = new Document();
            var page = document.NewPage();
            page.DrawLine(new Point(10, 10), new Point(100, 100));
            void Callback(Dictionary<string, object> path)
            {
                throw new InvalidOperationException("callback failed");
            }

            var stdout = new StringWriter();
            var old = Console.Out;
            Console.SetOut(stdout);
            try
            {
                page.GetCdrawings(callback: (Action<Dictionary<string, object>>)Callback);
                Console.WriteLine("returned");
            }
            finally
            {
                Console.SetOut(old);
            }

            string text = stdout.ToString().Replace("\r\n", "\n");
            Assert.Contains("calling cdrawings callback function/method failed!\n", text);
            Assert.Contains("returned", text);
        }

        private static string NormalizeNl(string text) =>
            (text ?? "").Replace("\r\n", "\n");

        private static Point PointFromPathItem(object o)
        {
            if (o is Point p) return p;
            if (o is IList list && list.Count >= 2)
                return new Point(
                    (float)Convert.ToDouble(list[0], CultureInfo.InvariantCulture),
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
            o is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

        /// <summary>PyMuPDF <c>pprint.pprint</c> formatting for <c>get_cdrawings()</c> output (golden <c>symbols.txt</c>).</summary>
        private static class DrawingPathsPprint
        {
            private const int Width = 80;

            public static string Format(IReadOnlyList<Dictionary<string, object>> paths)
            {
                var w = new Writer();
                w.FormatValue(paths);
                w.Sb.Append('\n');
                return w.Sb.ToString();
            }

            private sealed class Writer
            {
                public readonly StringBuilder Sb = new StringBuilder();
                public int Col;

                public void Append(string s)
                {
                    foreach (char c in s)
                    {
                        Sb.Append(c);
                        Col = c == '\n' ? 0 : Col + 1;
                    }
                }

                public void Append(char c)
                {
                    Sb.Append(c);
                    Col = c == '\n' ? 0 : Col + 1;
                }

                public void FormatValue(object v)
                {
                    switch (v)
                    {
                        case null:
                            Append("None");
                            break;
                        case bool b:
                            Append(b ? "True" : "False");
                            break;
                        case string s:
                            Append("'");
                            Append(s);
                            Append("'");
                            break;
                        case float f:
                            Append(FormatFloat(f));
                            break;
                        case double d:
                            Append(FormatFloat((float)d));
                            break;
                        case int n:
                            Append(n.ToString(CultureInfo.InvariantCulture));
                            break;
                        case long ln:
                            Append(ln.ToString(CultureInfo.InvariantCulture));
                            break;
                        case Point p:
                            FormatSequence(new object[] { p.X, p.Y }, '(', ')');
                            break;
                        case Rect r:
                            FormatSequence(new object[] { r.X0, r.Y0, r.X1, r.Y1 }, '(', ')');
                            break;
                        case mupdf.FzRect fr:
                            FormatSequence(new object[] { fr.x0, fr.y0, fr.x1, fr.y1 }, '(', ')');
                            break;
                        case float[] fa:
                            FormatSequence(fa.Cast<object>().ToArray(), '(', ')');
                            break;
                        case object[] oa:
                            FormatSequence(oa, '(', ')');
                            break;
                        case Dictionary<string, object> dict:
                            FormatDict(dict);
                            break;
                        case IList list:
                            FormatSequence(list, '[', ']');
                            break;
                        default:
                            Append(v.ToString());
                            break;
                    }
                }

                public void FormatDict(Dictionary<string, object> d)
                {
                    var keys = d.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
                    if (keys.Count == 0)
                    {
                        Append("{}");
                        return;
                    }

                    string compact = CompactDict(d, keys);
                    if (Col + compact.Length <= Width)
                    {
                        Append(compact);
                        return;
                    }

                    Append('{');
                    int hang = Col;
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (i > 0)
                        {
                            Append(",\n");
                            Append(new string(' ', hang));
                        }
                        string key = keys[i];
                        Append("'");
                        Append(key);
                        Append("': ");
                        FormatValue(d[key]);
                    }
                    Append('}');
                }

                public void FormatSequence(IList items, char open, char close)
                {
                    string compact = CompactSequence(items, open, close);
                    if (Col + compact.Length <= Width)
                    {
                        Append(compact);
                        return;
                    }

                    Append(open);
                    int hang = Col;
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (i > 0)
                        {
                            Append(",\n");
                            Append(new string(' ', hang));
                        }
                        FormatValue(items[i]);
                    }
                    Append(close);
                }
            }

            private static string CompactDict(Dictionary<string, object> d, List<string> keys)
            {
                var sb = new StringBuilder();
                sb.Append('{');
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append('\'').Append(keys[i]).Append("': ");
                    CompactValue(sb, d[keys[i]]);
                }
                sb.Append('}');
                return sb.ToString();
            }

            private static string CompactSequence(IList items, char open, char close)
            {
                var sb = new StringBuilder();
                sb.Append(open);
                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    CompactValue(sb, items[i]);
                }
                sb.Append(close);
                return sb.ToString();
            }

            private static void CompactValue(StringBuilder sb, object v)
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
                // Match PyMuPDF repr-style floats in symbols.txt (no gratuitous trailing zeros).
                        sb.Append(CompactSequence(new object[] { p.X, p.Y }, '(', ')'));
                        break;
                    case Rect r:
                        sb.Append(CompactSequence(new object[] { r.X0, r.Y0, r.X1, r.Y1 }, '(', ')'));
                        break;
                    case mupdf.FzRect fr:
                        sb.Append(CompactSequence(new object[] { fr.x0, fr.y0, fr.x1, fr.y1 }, '(', ')'));
                        break;
                    case float[] fa:
                        sb.Append(CompactSequence(fa.Cast<object>().ToArray(), '(', ')'));
                        break;
                    case object[] oa:
                        sb.Append(CompactSequence(oa, '(', ')'));
                        break;
                    case Dictionary<string, object> dict:
                        sb.Append(CompactDict(dict, dict.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList()));
                        break;
                    case IList list:
                        sb.Append(CompactSequence(list, '[', ']'));
                        break;
                    default:
                        sb.Append(v);
                        break;
                }
            }

            private static string FormatFloat(float f)
            {
                if (float.IsNaN(f) || float.IsInfinity(f))
                    return f.ToString(CultureInfo.InvariantCulture);
                double d = f;
                string s = d.ToString("R", CultureInfo.InvariantCulture);
                if (!s.Contains('.') && !s.Contains('E') && !s.Contains('e'))
                    s += ".0";
                return s;
            }
        }
    }
}
