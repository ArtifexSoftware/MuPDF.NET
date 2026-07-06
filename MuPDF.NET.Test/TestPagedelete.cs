// ----------------------------------------------------
// This tests correct functioning of multi-page delete
// ----------------------------------------------------
// Create a PDF in memory with 100 pages with a unique text each.
// Also create a TOC with a bookmark per page.
// On every page after the first to-be-deleted page, also insert a link, which
// points to this page.
// The bookmark text equals the text on the page for easy verification.
// Then delete some pages and verify:
// - the new TOC has empty items exactly for every deleted page
// - the remaining TOC items still point to the correct page
// - the document has no more links at all
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using mupdf;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestPagedelete/</c>; outputs: <c>TestDocuments/_Output/TestPagedelete/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestPagedelete
    {
        private const string TestClassName = nameof(TestPagedelete);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        // page_count = 100  # initial document length
        private const int page_count = 100;  // initial document length

        // r = range(5, 35, 5)  # contains page numbers we will delete
        private static readonly int[] r = new[] { 5, 10, 15, 20, 25, 30 };

        // insert this link on pages after first deleted one
        private static readonly Dictionary<string, object> link = new Dictionary<string, object>
        {
            ["from"] = new Rect(100, 100, 120, 120),
            ["kind"] = Constants.LinkGoto,
            ["page"] = r[0],
            ["to"] = new Point(100, 100),
        };

        [Fact]
        public void test_deletion()
        {
            // First prepare the document.
            using var doc = new Document();
            // toc = []
            var toc = new List<object>();
            for (int i = 0; i < page_count; i++)
            {
                // page = doc.NewPage()  # make a page
                var page = doc.NewPage();  // make a page
                // page.InsertText((100, 100), "%i" % i)  # insert unique text
                page.InsertText(new Point(100, 100), string.Format("{0}", i));  // insert unique text
                if (i > r[0])  // insert a link
                {
                    // page.insert_link(link)
                    page.insert_link(link);
                }
                // toc.Append([1, "%i" % i, i + 1])  # TOC bookmark to this page
                toc.Add(new object[] { 1, string.Format("{0}", i), i + 1 });  // TOC bookmark to this page
            }
            doc.SetToc(toc);  // insert the TOC
            Assert.True(doc.HasLinks());  // check we did insert links

            // Test page deletion.
            // Delete pages in range and verify result
            doc.__delitem__(r);
            Assert.False(doc.HasLinks());  // verify all links have gone
            Assert.Equal(page_count - r.Length, doc.PageCount);  // correct number deleted?
            // toc_new = doc.GetToc()  # this is the modified TOC
            var toc_new = doc.GetToc();  // this is the modified TOC
            // verify number of emptied items (have page number -1)
            Assert.Equal(r.Length, toc_new.Count(item => item.page == -1));
            // Deleted page numbers must correspond to TOC items with page number -1.
            foreach (int i in r)
            {
                Assert.Equal(-1, toc_new[i].page);
            }
            // Remaining pages must be correctly pointed to by the non-empty TOC items
            foreach (var item in toc_new)
            {
                // pno = item[-1]
                int pno = item.page;
                if (pno == -1)  // one of the emptied items
                    continue;
                // pno -= 1  # PDF page number
                pno -= 1;  // PDF page number
                // text = doc[pno].GetText().replace("\n", "")
                string text = doc[pno].GetText("text").ToString()!.Replace("\n", "");
                // toc text must equal text on page
                Assert.Equal(item.title, text);
            }
            doc.DeletePage(0);  // just for the coverage stats
            doc.delete_pages_by_slice(5, 10);
            doc.Select(Enumerable.Range(0, doc.PageCount).ToArray());
            doc.CopyPage(0);
            doc.MovePage(0);
            doc.FullCopyPage(0);
            doc.Save(Out("test_deletion.pdf"));
        }

        [Fact]
        public void test_3094()
        {
            string path = Doc("test_2871.pdf");
            using var document = new Document(path);
            // pnos = [i for i in range(0, document.page_count, 2)]
            var pnos = Enumerable.Range(0, document.PageCount).Where(i => i % 2 == 0).ToArray();
            // document.delete_pages(pnos)
            document.delete_pages(pnos);
            document.Save(Out("test_3094.pdf"));
        }

        [Fact]
        public void test_3150()
        {
            // Implicitly also check use of new MuPDF function
            // pdf_rearrange_pages() since version 1.23.9.
            // filename = os.path.join(scriptdir, "resources", "test-3150.pdf")
            string filename = Doc("test-3150.pdf");
            // pages = [3, 3, 3, 2, 3, 1, 0, 0]
            int[] pages = { 3, 3, 3, 2, 3, 1, 0, 0 };
            using var doc = new Document(filename);
            doc.Select(pages);
            Assert.Equal(pages.Length, doc.PageCount);
        }

        [Fact]
        public void test_4462()
        {
            // path0 = os.path.normpath(f'{__file__}/../../tests/resources/test_4462_0.pdf')
            string path0 = Out("test_4462_0.pdf");
            // path1 = os.path.normpath(f'{__file__}/../../tests/resources/test_4462_1.pdf')
            string path1 = Out("test_4462_1.pdf");
            // path2 = os.path.normpath(f'{__file__}/../../tests/resources/test_4462_2.pdf')
            string path2 = Out("test_4462_2.pdf");
            using (var document = new Document())
            {
                // document.NewPage()
                document.NewPage();
                // document.NewPage()
                document.NewPage();
                // document.NewPage()
                document.NewPage();
                // document.NewPage()
                document.NewPage();
                // document.Save(path0)
                document.Save(path0);
            }
            using (var document = new Document(path0))
            {
                Assert.Equal(4, document.__len__());
                // document.DeletePage(-1)
                document.DeletePage(-1);
                // document.Save(path1)
                document.Save(path1);
            }
            using (var document = new Document(path1))
            {
                Assert.Equal(3, document.__len__());
                // document.delete_pages(-1)
                document.delete_pages(-1);
                // document.Save(path2)
                document.Save(path2);
            }
            using (var document = new Document(path2))
            {
                Assert.Equal(2, document.__len__());
                document.Save(Out("test_4462.pdf"));
            }
        }

        [Fact]
        public void test_4790()
        {
            string path = Doc("test_4790.pdf");
            // path2 = os.path.normpath(f'{__file__}/../../tests/test_4790_out.pdf')
            string path2 = Out("test_4790.pdf");
            Console.WriteLine();
            // page_to_delete = 1
            int page_to_delete = 1;

            // Reproduce the problem.
            using (var document = new Document(path))
            {
                string wt = Tools.MupdfWarnings();
                Assert.True(string.IsNullOrEmpty(wt), $"wt={wt}");
                Assert.Equal(2, document.__len__());
                // document.delete_pages(page_to_delete)
                document.delete_pages(page_to_delete);
                Assert.Equal(1, document.__len__());
                // document.Save(path2)
                document.Save(path2);
                wt = Tools.MupdfWarnings();
                Assert.Equal("repairing PDF document", wt);
            }
            using (var document = new Document(path2))
            {
                // Expect incorrect result.
                Assert.Equal(2, document.__len__());
            }

            // Call mupdf.pdf_repair_xref() before delete_pages(); this works around the
            // problem.
            using (var document = new Document(path))
            {
                var document_pdf = Helpers.AsPdfDocument(document);
                mupdf.mupdf.pdf_repair_xref(document_pdf);
                string wt = Tools.MupdfWarnings();
                Assert.Equal("repairing PDF document", wt);
                // document.delete_pages(page_to_delete)
                document.delete_pages(page_to_delete);
                // document.Save(path2)
                document.Save(path2);
            }
            using (var document = new Document(path2))
            {
                // Expect correct result.
                Assert.Equal(1, document.__len__());
            }

            // Call mupdf.pdf_check_document() before delete_pages(); this works around
            // the problem.
            using (var document = new Document(path))
            {
                var document_pdf = Helpers.AsPdfDocument(document);
                mupdf.mupdf.pdf_check_document(document_pdf);
                string wt = Tools.MupdfWarnings();
                Assert.Equal("repairing PDF document", wt);
                // document.delete_pages(page_to_delete)
                document.delete_pages(page_to_delete);
                // document.Save(path2)
                document.Save(path2);
            }
            using (var document = new Document(path2))
            {
                // Expect correct result.
                Assert.Equal(1, document.__len__());
            }

            // Check that document is marked as repaired after save.
            using (var document = new Document(path))
            {
                Assert.False(document.is_repaired(), $"is_repaired={document.is_repaired()}");
                // document.Save(path2)
                document.Save(path2);
                Assert.True(document.is_repaired(), $"is_repaired={document.is_repaired()}");
                string wt = Tools.MupdfWarnings();
                Assert.Equal("repairing PDF document", wt);
            }

            // Check that raise_on_repair=True works.
            using (var document = new Document(path))
            {
                bool gotException = false;
                try
                {
                    // document.Save(path2, raise_on_repair=True)
                    document.Save(path2, raise_on_repair: true);
                }
                catch (Exception e)
                {
                    gotException = true;
                    Console.WriteLine($"Received expected exception: {e}");
                    Console.Out.Flush();
                }
                Assert.True(gotException, "Did not get expected exception.");
                string wt = Tools.MupdfWarnings();
                Assert.Equal("repairing PDF document", wt);
            }

            // Check that Document.repair() works.
            using (var document = new Document(path))
            {
                // document.repair()
                document.repair();
                string wt = Tools.MupdfWarnings();
                Assert.Equal("repairing PDF document", wt);
                // document.delete_pages(page_to_delete)
                document.delete_pages(page_to_delete);
                // document.Save(path2, raise_on_repair=True)
                document.Save(path2, raise_on_repair: true);
            }
            using (var document = new Document(path2))
            {
                // Expect correct result.
                Assert.Equal(1, document.__len__());
            }
        }
    }
}