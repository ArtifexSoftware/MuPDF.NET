// Port of PyMuPDF-1.27.2.2/tests/test_2634.py
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test2634
    {
        private static readonly string testDoc = _Path.ForTestClass("test_2634.pdf", nameof(Test2634));

        [Fact]
        public void test_2634()
        {
            // if not hasattr(pymupdf, 'mupdf'):
            //     print('test_2634(): Not running on classic.')
            //     return;

            using var pdf = new Document(testDoc);
            using var newDoc = new Document();
            newDoc.InsertPdf(pdf);
            newDoc.SetToc(TocToRows(pdf.GetToc(simple: false)));
            var toc_pdf = pdf.GetToc(simple: false);
            var toc_new = newDoc.GetToc(simple: false);

            void clear_xref(List<(int level, string title, int page, Dictionary<string, object> link)> toc)
            {
                //
                // Clear toc items that naturally differ.
                //
                foreach (var item in toc)
                {
                    var d = item.link;
                    if (d.ContainsKey("collapse"))
                        d["collapse"] = "dummy";
                    if (d.ContainsKey("xref"))
                        d["xref"] = "dummy";
                }
            }

            clear_xref(toc_pdf);
            clear_xref(toc_new);

            Console.WriteLine("toc_pdf");
            foreach (var item in toc_pdf)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine();
            Console.WriteLine("toc_new");
            foreach (var item in toc_new)
            {
                Console.WriteLine(item);
            }


            string[] toc_text_pdf = Pformat(toc_pdf, indent: 4).Split('\n');
            string[] toc_text_new = Pformat(toc_new, indent: 4).Split('\n');

            IEnumerable<string> diff = UnifiedDiff(
                toc_text_pdf,
                toc_text_new,
                lineterm: "");
            Console.WriteLine(string.Join("\n", diff));

            // Check 'to' points are identical apart from rounding errors.
            //
            Assert.Equal(toc_pdf.Count, toc_new.Count);
            foreach (var pair in toc_pdf.Zip(toc_new))
            {
                var a = pair.First;
                var b = pair.Second;
                var a_dict = a.link;
                var b_dict = b.link;
                if (a_dict.ContainsKey("to"))
                {
                    Assert.True(b_dict.ContainsKey("to"));
                    object a_to = a_dict["to"];
                    object b_to = b_dict["to"];
                    Assert.IsType<Point>(a_to);
                    Assert.IsType<Point>(b_to);
                    var aPt = (Point)a_to;
                    var bPt = (Point)b_to;
                    if (aPt != bPt)
                        Console.WriteLine($"Points not identical: a_to={aPt} b_to={bPt}.");
                    Assert.True(Math.Abs(aPt.X - bPt.X) < 0.01);
                    Assert.True(Math.Abs(aPt.Y - bPt.Y) < 0.01);
                }
            }
        }

        private static List<object> TocToRows(
            List<(int level, string title, int page, Dictionary<string, object> link)> toc)
        {
            var rows = new List<object>();
            foreach (var (level, title, page, link) in toc)
                rows.Add(new object[] { level, title, page, link });
            return rows;
        }

        private static string Pformat(List<(int level, string title, int page, Dictionary<string, object> link)> toc, int indent = 4)
        {
            string pad = new string(' ', indent);
            var lines = new List<string> { "[" };
            foreach (var row in toc)
                lines.Add($"{pad}{FormatExtendedTocEntry(row)},");
            lines.Add("]");
            return string.Join("\n", lines);
        }

        private static IEnumerable<string> UnifiedDiff(
            IReadOnlyList<string> a,
            IReadOnlyList<string> b,
            string lineterm = "")
        {
            var matcher = new SequenceMatcher(a, b);
            foreach (var (tag, i1, i2, j1, j2) in matcher.GetGroupedOpcodes())
            {
                if (tag == "equal")
                    continue;
                if (tag == "replace" || tag == "delete")
                {
                    for (int i = i1; i < i2; i++)
                        yield return $"-{a[i]}{lineterm}";
                }
                if (tag == "replace" || tag == "insert")
                {
                    for (int j = j1; j < j2; j++)
                        yield return $"+{b[j]}{lineterm}";
                }
            }
        }

        private sealed class SequenceMatcher
        {
            private readonly IReadOnlyList<string> _a;
            private readonly IReadOnlyList<string> _b;
            private readonly int[,] _lcs;

            public SequenceMatcher(IReadOnlyList<string> a, IReadOnlyList<string> b)
            {
                _a = a;
                _b = b;
                int m = a.Count, n = b.Count;
                _lcs = new int[m + 1, n + 1];
                for (int i = m - 1; i >= 0; i--)
                {
                    for (int j = n - 1; j >= 0; j--)
                    {
                        if (a[i] == b[j])
                            _lcs[i, j] = 1 + _lcs[i + 1, j + 1];
                        else
                            _lcs[i, j] = Math.Max(_lcs[i + 1, j], _lcs[i, j + 1]);
                    }
                }
            }

            public IEnumerable<(string Tag, int I1, int I2, int J1, int J2)> GetGroupedOpcodes()
            {
                var opcodes = new List<(string Tag, int I1, int I2, int J1, int J2)>();
                Diff(0, 0, _a.Count, _b.Count, opcodes);
                string? groupTag = null;
                int gi1 = 0, gi2 = 0, gj1 = 0, gj2 = 0;
                foreach (var op in opcodes)
                {
                    if (groupTag == null)
                    {
                        groupTag = op.Tag;
                        gi1 = op.I1;
                        gi2 = op.I2;
                        gj1 = op.J1;
                        gj2 = op.J2;
                        continue;
                    }
                    if (op.Tag == groupTag && op.I1 == gi2 && op.J1 == gj2)
                    {
                        gi2 = op.I2;
                        gj2 = op.J2;
                        continue;
                    }
                    yield return (groupTag, gi1, gi2, gj1, gj2);
                    groupTag = op.Tag;
                    gi1 = op.I1;
                    gi2 = op.I2;
                    gj1 = op.J1;
                    gj2 = op.J2;
                }
                if (groupTag != null)
                    yield return (groupTag, gi1, gi2, gj1, gj2);
            }

            private void Diff(int i, int j, int iEnd, int jEnd, List<(string, int, int, int, int)> opcodes)
            {
                while (i < iEnd && j < jEnd && _a[i] == _b[j])
                {
                    i++;
                    j++;
                }
                if (i < iEnd && j < jEnd)
                {
                    if (_lcs[i + 1, j] >= _lcs[i, j + 1])
                    {
                        int ni = i + 1;
                        while (ni < iEnd && _lcs[ni, j] == _lcs[i, j])
                            ni++;
                        opcodes.Add(("delete", i, ni, j, j));
                        Diff(ni, j, iEnd, jEnd, opcodes);
                        return;
                    }
                    int nj = j + 1;
                    while (nj < jEnd && _lcs[i, nj] == _lcs[i, j])
                        nj++;
                    opcodes.Add(("insert", i, i, j, nj));
                    Diff(i, nj, iEnd, jEnd, opcodes);
                    return;
                }
                if (i < iEnd)
                    opcodes.Add(("delete", i, iEnd, j, j));
                else if (j < jEnd)
                    opcodes.Add(("insert", i, i, j, jEnd));
            }
        }

        private static string FormatExtendedTocEntry((int level, string title, int page, Dictionary<string, object> link) row) =>
            $"({row.level}, '{EscapePyString(row.title)}', {row.page}, {FormatPyDict(row.link)})";

        private static string EscapePyString(string s) => s.Replace("'", "\\'", StringComparison.Ordinal);

        private static string FormatPyFloat(float v) =>
            v.ToString(Math.Abs(v - Math.Truncate(v)) < 1e-9 ? "0.0######" : "g", CultureInfo.InvariantCulture);

        private static string FormatPyDict(Dictionary<string, object> d)
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

        private static string FormatPyValue(object v)
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
