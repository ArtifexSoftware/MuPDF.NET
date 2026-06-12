/*
* Join multiple PDFs into a new one.
* Compare with stored earlier result:
*     - must have identical object definitions
*     - must have different trailers
* Try inserting files in a loop.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;
using mupdf;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_insertpdf.py</c>.
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestInsertpdf/</c>; outputs: <c>TestDocuments/_Output/TestInsertpdf/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestInsertpdf
    {
        private const string TestClassName = nameof(TestInsertpdf);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        static List<(string text, float? number)> approx_parse(string text)
        {
            /*
            Splits <text> into sequence of (text, number) pairs. Where sequence of
            [0-9.] is not convertible to a number (e.g. '4.5.6'), <number> will be
            None.
            */
            var ret = new List<(string, float?)>();
            foreach (Match m in Regex.Matches(text, @"([^0-9]+)([0-9.]*)"))
            {
                string t = m.Groups[1].Value;
                try
                {
                    float number = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    ret.Add((t, number));
                }
                catch (Exception)
                {
                    t += m.Groups[2].Value;
                    ret.Add((t, null));
                }
            }
            return ret;
        }

        static int approx_compare(string a, string b, float max_delta)
        {
            /*
            Compares <a> and <b>, allowing numbers to differ by up to <delta>.
            */
            var aa = approx_parse(a);
            var bb = approx_parse(b);
            if (aa.Count != bb.Count)
                return 1;
            int ret = 1;
            for (int i = 0; i < aa.Count; i++)
            {
                var (at, an) = aa[i];
                var (bt, bn) = bb[i];
                if (at != bt)
                    break;
                if (an != null && bn != null)
                {
                    if (Math.Abs(an.Value - bn.Value) >= max_delta)
                    {
                        Console.WriteLine($"diff={an - bn}: an={an} bn={bn}");
                        break;
                    }
                }
                else if ((an == null) != (bn == null))
                {
                    break;
                }
                if (i == aa.Count - 1)
                    ret = 0;
            }
            if (ret != 0)
            {
                Console.WriteLine($"Differ:\n    a={a}\n    b={b}");
            }

            return ret;
        }

        /// <summary>Regression test: insert (PyMuPDF <c>tests/test_insertpdf.py::test_insert</c>).</summary>
        [Fact]
        public void test_insert()
        {
            var all_text_original = new List<string>();  // text on input pages
            var all_text_combined = new List<string>();  // text on resulting output pages
            // prepare input PDFs
            var doc1 = new Document();
            for (int i = 0; i < 5; i++)  // just arbitrary number of pages
            {
                string text = $"doc 1, page {i}";  // the 'globally' unique text
                var page = doc1.NewPage();
                page.InsertText(new Point(100, 72), text);
                all_text_original.Add(text);
            }

            var doc2 = new Document();
            for (int i = 0; i < 4; i++)
            {
                string text = $"doc 2, page {i}";
                var page = doc2.NewPage();
                page.InsertText(new Point(100, 72), text);
                all_text_original.Add(text);
            }

            var doc3 = new Document();
            for (int i = 0; i < 3; i++)
            {
                string text = $"doc 3, page {i}";
                var page = doc3.NewPage();
                page.InsertText(new Point(100, 72), text);
                all_text_original.Add(text);
            }

            var doc4 = new Document();
            for (int i = 0; i < 6; i++)
            {
                string text = $"doc 4, page {i}";
                var page = doc4.NewPage();
                page.InsertText(new Point(100, 72), text);
                all_text_original.Add(text);
            }

            var new_doc = new Document();  // make combined PDF of input files
            new_doc.InsertPdf(doc1);
            new_doc.InsertPdf(doc2);
            new_doc.InsertPdf(doc3);
            new_doc.InsertPdf(doc4);
            // read text from all pages and store in list
            foreach (var page in new_doc)
                all_text_combined.Add(Convert.ToString(page.GetText()).Replace("\n", ""));
            // the lists must be equal
            Assert.Equal(all_text_original, all_text_combined);
            new_doc.Save(Out("test_insert.pdf"));
        }

        /// <summary>Regression test: issue1417 insertpdf in loop (PyMuPDF <c>tests/test_insertpdf.py::test_issue1417_insertpdf_in_loop</c>).</summary>
        [Fact]
        public void test_issue1417_insertpdf_in_loop()
        {
            string f = Doc("1.pdf");
            var big_doc = new Document();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                for (int n = 0; n < 1025; n++)
                {
                    using (var pdf = new Document(f))
                        big_doc.InsertPdf(pdf);
                }
                big_doc.Save(Out("test_issue1417_insertpdf_in_loop.pdf"));
                big_doc.Close();
                return;
            }

            int fd1 = Syscall.open(f, Syscall.O_RDONLY);
            Syscall.close(fd1);
            for (int n = 0; n < 1025; n++)
            {
                using (var pdf = new Document(f))
                    big_doc.InsertPdf(pdf);
                // a file descriptor, fd will be seen to increment.
                int fd2 = Syscall.open(f, Syscall.O_RDONLY);
                Assert.Equal(fd1, fd2);
                Syscall.close(fd2);
            }
            big_doc.Save(Out("test_issue1417_insertpdf_in_loop.pdf"));
            big_doc.Close();
        }

        static byte[] _2861_2871_merge_pdf(byte[] content, byte[] coverpage)
        {
            using (var coverpage_pdf = new Document(coverpage, "pdf"))
            {
                using (var content_pdf = new Document(content, "pdf"))
                {
                    coverpage_pdf.InsertPdf(content_pdf);
                    return coverpage_pdf.Write();
                }
            }
        }

        /// <summary>Regression test: 2861 (PyMuPDF <c>tests/test_insertpdf.py::test_2861</c>).</summary>
        [Fact]
        public void test_2861()
        {
            using (var content_pdf = File.OpenRead(Doc("test_2861.pdf")))
            using (var coverpage_pdf = File.OpenRead(Doc("test_2861.pdf")))
            {
                byte[] content = ReadAllBytes(content_pdf);
                byte[] coverpage = ReadAllBytes(coverpage_pdf);
                byte[] bytes = _2861_2871_merge_pdf(content, coverpage);
                using var doc = new Document(bytes, "pdf");
                doc.Save(Out("test_2861.pdf"));
            }
        }

        /// <summary>Regression test: 2871 (PyMuPDF <c>tests/test_insertpdf.py::test_2871</c>).</summary>
        [Fact]
        public void test_2871()
        {
            using (var content_pdf = File.OpenRead(Doc("test_2871.pdf")))
            using (var coverpage_pdf = File.OpenRead(Doc("test_2871.pdf")))
            {
                byte[] content = ReadAllBytes(content_pdf);
                byte[] coverpage = ReadAllBytes(coverpage_pdf);
                byte[] bytes = _2861_2871_merge_pdf(content, coverpage);
                using var doc = new Document(bytes, "pdf");
                doc.Save(Out("test_2871.pdf"));
            }
        }

        /// <summary>Regression test: 3789 (PyMuPDF <c>tests/test_insertpdf.py::test_3789</c>).</summary>
        [Fact]
        public void test_3789()
        {
            string result_path = Path.Combine(
                Path.GetDirectoryName(Out("test_3789_out_0.pdf"))!, "test_3789_out");
            int pages_per_split = 5;

            // Clean pdf
            var doc = new Document(Doc("test_3789.pdf"));
            using var tmp = new MemoryStream();
            using (var ms = new MemoryStream())
            {
                doc.Save(ms, garbage: 4, deflate: 1);
                tmp.Write(ms.ToArray());
            }

            var source_doc = new Document(tmp.ToArray(), "pdf");

            // Calculate the number of pages per split file and the number of split files
            int page_range = pages_per_split - 1;
            var split_range = new List<int>();
            for (int start = 0; start < source_doc.PageCount; start += pages_per_split)
                split_range.Add(start);
            int num_splits = split_range.Count;

            // Loop through each split range and create a new PDF file
            for (int i = 0; i < split_range.Count; i++)
            {
                int start = split_range[i];
                using var output_doc = new Document();

                // Determine the ending page for this split file
                int to_page = start + page_range;
                if (i < num_splits - 1)
                    output_doc.InsertPdf(source_doc, fromPage: start, toPage: to_page);
                else
                    output_doc.InsertPdf(source_doc, fromPage: start, toPage: -1);

                // Save the output document to a file and add the path to the list of split files
                string path = $"{result_path}_{i}.pdf";
                output_doc.Save(path, garbage: 2);
                Console.WriteLine($"Have saved to path={path}.");

                // If this is the last split file, exit the loop
                if (i == num_splits - 1)
                    break;
            }
        }

        static int PdfArrayLen(mupdf.PdfObj arr) => arr.m_internal != null ? arr.pdf_array_len() : 0;

        static List<Dictionary<string, object>> names_and_kids(Document doc)
        {
            var rc = new List<Dictionary<string, object>>();
            var pdf = Helpers.AsPdfDocument(doc);
            var fields = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("AcroForm"),
                mupdf.mupdf.pdf_new_name("Fields"));
            if (fields.pdf_is_array() == 0)
                return rc;
            int root_count = fields.pdf_array_len();
            if (root_count == 0)
                return rc;
            for (int i = 0; i < root_count; i++)
            {
                var field = fields.pdf_array_get(i);
                var kids = field.pdf_dict_get(mupdf.mupdf.pdf_new_name("Kids"));
                int kid_count = kids.m_internal != null ? kids.pdf_array_len() : 0;
                string T = field.pdf_dict_get_text_string(mupdf.mupdf.pdf_new_name("T"));
                rc.Add(new Dictionary<string, object> { ["name"] = T, ["kids"] = kid_count });
            }
            return rc;
        }

        /// <summary>Regression test: widget insert (PyMuPDF <c>tests/test_insertpdf.py::test_widget_insert</c>).</summary>
        [Fact]
        public void test_widget_insert()
        {
            var tar = new Document(Doc("merge-form1.pdf"));
            int pc0 = tar.PageCount;  // for later assertion
            var src = new Document(Doc("interfield-calculation.pdf"));
            int pc1 = src.PageCount;  // for later assertion

            var tarpdf = Helpers.AsPdfDocument(tar);
            int tar_field_count = PdfArrayLen(
                PdfDictGetp(tarpdf, "Root/AcroForm/Fields"));
            int tar_co_count = PdfArrayLen(
                PdfDictGetp(tarpdf, "Root/AcroForm/CO"));
            var srcpdf = Helpers.AsPdfDocument(src);
            int src_field_count = PdfArrayLen(
                PdfDictGetp(srcpdf, "Root/AcroForm/Fields"));
            int src_co_count = PdfArrayLen(
                PdfDictGetp(srcpdf, "Root/AcroForm/CO"));

            tar.InsertPdf(src);
            int new_field_count = PdfArrayLen(
                PdfDictGetp(tarpdf, "Root/AcroForm/Fields"));
            int new_co_count = PdfArrayLen(
                PdfDictGetp(tarpdf, "Root/AcroForm/CO"));
            Assert.Equal(pc0 + pc1, tar.PageCount);
            Assert.Equal(tar_field_count + src_field_count, new_field_count);
            Assert.Equal(tar_co_count + src_co_count, new_co_count);
            tar.Save(Out("test_widget_insert.pdf"));
        }

        /// <summary>Regression test: merge checks1 (PyMuPDF <c>tests/test_insertpdf.py::test_merge_checks1</c>).</summary>
        [Fact]
        public void test_merge_checks1()
        {
            var tar = new Document(Doc("merge-form1.pdf"));
            var rc0 = names_and_kids(tar);
            var src = new Document(Doc("merge-form2.pdf"));
            var rc1 = names_and_kids(src);
            tar.InsertPdf(src, joinDuplicates: false);
            var rc2 = names_and_kids(tar);
            Assert.Equal(rc0.Count + rc1.Count, rc2.Count);
            tar.Save(Out("test_merge_checks1.pdf"));
        }

        /// <summary>Regression test: merge checks2 (PyMuPDF <c>tests/test_insertpdf.py::test_merge_checks2</c>).</summary>
        [Fact]
        public void test_merge_checks2()
        {
            // Join / merge Form PDFs joining any duplicate names in the src PDF.
            var tar = new Document(Doc("merge-form1.pdf"));
            var rc0 = names_and_kids(tar);  // list of root names and kid counts
            var names0 = new List<string>();
            foreach (var itm in rc0)
                names0.Add((string)itm["name"]);
            int kids0 = 0;
            foreach (var itm in rc0)
                kids0 += Convert.ToInt32(itm["kids"]);  // number of kids in target

            var src = new Document(Doc("merge-form2.pdf"));
            var rc1 = names_and_kids(src);  // list of root namesand kids in source PDF
            int dup_kids = 0;  // counts the expected kids after merge

            foreach (var itm in rc1)  // walk root fields of source pdf
            {
                if (!names0.Contains((string)itm["name"]))  // not a duplicate name
                    continue;
                // if target field has kids, add their count, else add 1
                int dup_kids0 = 0;
                foreach (var i in rc0)
                {
                    if ((string)i["name"] == (string)itm["name"])
                        dup_kids0 += Convert.ToInt32(i["kids"]);
                }
                dup_kids += dup_kids0 != 0 ? dup_kids0 : 1;
                // if source field has kids add their count, else add 1
                dup_kids += Convert.ToInt32(itm["kids"]) != 0 ? Convert.ToInt32(itm["kids"]) : 1;
            }

            var names1 = new List<string>();
            foreach (var itm in rc1)
                names1.Add((string)itm["name"]);  // names in source

            tar.InsertPdf(src, joinDuplicates: true);  // join merging any duplicate names

            var rc2 = names_and_kids(tar);  // get names and kid counts in resulting PDF
            var names2 = new List<string>();
            foreach (var itm in rc2)
                names2.Add((string)itm["name"]);  // resulting names in target
            int kids2 = 0;
            foreach (var itm in rc2)
                kids2 += Convert.ToInt32(itm["kids"]);  // total resulting kid count

            var union = new HashSet<string>(names0);
            foreach (var n in names1)
                union.Add(n);
            Assert.Equal(union.Count, names2.Count);
            Assert.Equal(dup_kids, kids2);
            tar.Save(Out("test_merge_checks2.pdf"));
        }

        /// <summary>Regression test: 4412 (PyMuPDF <c>tests/test_insertpdf.py::test_4412</c>).</summary>
        [Fact]
        public void test_4412()
        {
            // This tests whether a page from a PDF containing widgets found in the wild
            // can be inserted into a new document with default options (widget=True)
            // and widget=False.
            Console.WriteLine();
            foreach (bool widget in new[] { true, false })
            {
                Console.WriteLine($"widget={widget}");
                using (var doc = new Document(Doc("test_4412.pdf")))
                using (var new_doc = new Document())
                {
                    using var buf = new MemoryStream();
                    new_doc.InsertPdf(doc, fromPage: 1, toPage: 1);
                    new_doc.Save(buf);
                    Assert.Equal(1, new_doc.PageCount);
                    new_doc.Save(Out("test_4412.pdf"));
                }
            }
        }

        /// <summary>Regression test: 4571 (PyMuPDF <c>tests/test_insertpdf.py::test_4571</c>).</summary>
        [Fact]
        public void test_4571()
        {
            string path_out = Out("test_4571_out.pdf");
            using (var newdocument = new Document())
            {
                using (var document = new Document(Doc("test_4571.pdf")))
                    newdocument.InsertPdf(document);
                newdocument.Save(path_out, garbage: 4, clean: 0);
                Console.WriteLine($"Have saved to: path_out={path_out}");
            }
            byte[] content = File.ReadAllBytes(path_out);
            if (_Version.mupdf_version_tuple().CompareTo((1, 26, 6)) >= 0)
            {
                // Correct.
                Assert.Contains(
                    "<</Type/Pages/Count 6/Kids[4 0 R 6 0 R 12 0 R 13 0 R 14 0 R 15 0 R]>>",
                    System.Text.Encoding.ASCII.GetString(content));
            }
            else
            {
                // Incorrect.
                Assert.Contains(
                    "<</Type/Pages/Count 6/Kids[4 0 R 6 0 R 12 0 R 4 0 R 6 0 R 12 0 R]>>",
                    System.Text.Encoding.ASCII.GetString(content));
            }
        }

        static byte[] ReadAllBytes(Stream s)
        {
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }

        static mupdf.PdfObj PdfDictGetp(mupdf.PdfDocument pdf, string path)
            => mupdf.mupdf.pdf_trailer(pdf).pdf_dict_getp(path);

        /// <summary>link rects preserved after insert_pdf with rotation (PyMuPDF <c>tests/test_insertpdf.py::test_4958</c>).</summary>
        [Fact]
        public void test_4958()
        {
            using var documentOrig = new Document();
            using var documentCopy = new Document();
            documentOrig.NewPage();
            documentOrig[0].SetRotation(90);
            documentOrig[0].InsertLink(new Dictionary<string, object>
            {
                { "kind", Constants.LinkUri },
                { "from", new Rect(10, 20, 40, 60) },
                { "uri", "https://example.org" },
            });
            documentCopy.InsertPdf(documentOrig, links: true);

            var fromRectsOrig = documentOrig[0].GetLinks().Select(l => l.From).ToList();
            var fromRectsCopy = documentCopy[0].GetLinks().Select(l => l.From).ToList();
            Assert.Equal(fromRectsOrig, fromRectsCopy);

            documentCopy.Save(Out("test_4958.pdf"));
        }
    }

    /// <summary>Minimal POSIX open/close for <c>test_issue1417_insertpdf_in_loop</c> on Unix.</summary>
    internal static class Syscall
    {
        public const int O_RDONLY = 0;

        [DllImport("libc", SetLastError = true, EntryPoint = "open")]
        private static extern int libc_open(string pathname, int flags);

        [DllImport("libc", SetLastError = true, EntryPoint = "close")]
        private static extern int libc_close(int fd);

        public static int open(string path, int flags) => libc_open(path, flags);
        public static void close(int fd) => libc_close(fd);
    }
}
