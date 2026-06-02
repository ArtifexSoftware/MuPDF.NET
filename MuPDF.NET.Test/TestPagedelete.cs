// """
// ----------------------------------------------------
// This tests correct functioning of multi-page delete
// ----------------------------------------------------
// Create a PDF in memory with 100 pages with a unique text each.
// Also create a TOC with a bookmark per page.
// On every page after the first to-be-deleted page, also insert a link, which
// points to this page.
// The bookmark text equals the text on the page for easy verification.
//
// Then delete some pages and verify:
// - the new TOC has empty items exactly for every deleted page
// - the remaining TOC items still point to the correct page
// - the document has no more links at all
// """
//
// import os
//
// import pymupdf
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using mupdf;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_pagedelete.py</c>.
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
            // doc = pymupdf.open()
            using var doc = new Document();
            // toc = []
            var toc = new List<object>();
            // for i in range(page_count):
            for (int i = 0; i < page_count; i++)
            {
                // page = doc.NewPage()  # make a page
                var page = doc.NewPage();  // make a page
                // page.InsertText((100, 100), "%i" % i)  # insert unique text
                page.InsertText(new Point(100, 100), string.Format("{0}", i));  // insert unique text
                // if i > r[0]:  # insert a link
                if (i > r[0])  // insert a link
                {
                    // page.insert_link(link)
                    page.insert_link(link);
                }
                // toc.Append([1, "%i" % i, i + 1])  # TOC bookmark to this page
                toc.Add(new object[] { 1, string.Format("{0}", i), i + 1 });  // TOC bookmark to this page
            }

            // doc.SetToc(toc)  # insert the TOC
            doc.SetToc(toc);  // insert the TOC
            // assert doc.has_links()  # check we did insert links
            Assert.True(doc.HasLinks());  // check we did insert links

            // Test page deletion.
            // Delete pages in range and verify result
            // del doc[r]
            doc.__delitem__(r);
            // assert not doc.has_links()  # verify all links have gone
            Assert.False(doc.HasLinks());  // verify all links have gone
            // assert doc.page_count == page_count - len(r)  # correct number deleted?
            Assert.Equal(page_count - r.Length, doc.PageCount);  // correct number deleted?
            // toc_new = doc.GetToc()  # this is the modified TOC
            var toc_new = doc.GetToc();  // this is the modified TOC
            // verify number of emptied items (have page number -1)
            // assert len([item for item in toc_new if item[-1] == -1]) == len(r)
            Assert.Equal(r.Length, toc_new.Count(item => item.page == -1));
            // Deleted page numbers must correspond to TOC items with page number -1.
            // for i in r:
            foreach (int i in r)
            {
                // assert toc_new[i][-1] == -1
                Assert.Equal(-1, toc_new[i].page);
            }
            // Remaining pages must be correctly pointed to by the non-empty TOC items
            // for item in toc_new:
            foreach (var item in toc_new)
            {
                // pno = item[-1]
                int pno = item.page;
                // if pno == -1:  # one of the emptied items
                if (pno == -1)  // one of the emptied items
                    continue;
                // pno -= 1  # PDF page number
                pno -= 1;  // PDF page number
                // text = doc[pno].GetText().replace("\n", "")
                string text = doc[pno].GetText("text").ToString()!.Replace("\n", "");
                // toc text must equal text on page
                // assert text == item[1]
                Assert.Equal(item.title, text);
            }

            // doc.DeletePage(0)  # just for the coverage stats
            doc.DeletePage(0);  // just for the coverage stats
            // del doc[5:10]
            doc.delete_pages_by_slice(5, 10);
            // doc.Select(range(doc.page_count))
            doc.Select(Enumerable.Range(0, doc.PageCount).ToArray());
            // doc.CopyPage(0)
            doc.CopyPage(0);
            // doc.MovePage(0)
            doc.MovePage(0);
            // doc.FullcopyPage(0)
            doc.FullCopyPage(0);
            doc.Save(Out("test_deletion.pdf"));
        }

        [Fact]
        public void test_3094()
        {
            // path = os.path.abspath(f"{__file__}/../../tests/resources/test_2871.pdf")
            string path = Doc("test_2871.pdf");
            // document = pymupdf.open(path)
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
            // """Assert correct functioning for problem file.
            //
            // Implicitly also check use of new MuPDF function
            // pdf_rearrange_pages() since version 1.23.9.
            // """
            // filename = os.path.join(scriptdir, "resources", "test-3150.pdf")
            string filename = Doc("test-3150.pdf");
            // pages = [3, 3, 3, 2, 3, 1, 0, 0]
            int[] pages = { 3, 3, 3, 2, 3, 1, 0, 0 };
            // doc = pymupdf.open(filename)
            using var doc = new Document(filename);
            // doc.Select(pages)
            doc.Select(pages);
            // assert doc.page_count == len(pages)
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
            // with pymupdf.open() as document:
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
            // with pymupdf.open(path0) as document:
            using (var document = new Document(path0))
            {
                // assert len(document) == 4
                Assert.Equal(4, document.__len__());
                // document.DeletePage(-1)
                document.DeletePage(-1);
                // document.Save(path1)
                document.Save(path1);
            }
            // with pymupdf.open(path1) as document:
            using (var document = new Document(path1))
            {
                // assert len(document) == 3
                Assert.Equal(3, document.__len__());
                // document.delete_pages(-1)
                document.delete_pages(-1);
                // document.Save(path2)
                document.Save(path2);
            }
            // with pymupdf.open(path2) as document:
            using (var document = new Document(path2))
            {
                // assert len(document) == 2
                Assert.Equal(2, document.__len__());
                document.Save(Out("test_4462.pdf"));
            }
        }

        [Fact]
        public void test_4790()
        {
            // path = os.path.normpath(f'{__file__}/../../tests/resources/test_4790.pdf')
            string path = Doc("test_4790.pdf");
            // path2 = os.path.normpath(f'{__file__}/../../tests/test_4790_out.pdf')
            string path2 = Out("test_4790.pdf");
            // print()
            Console.WriteLine();
            // page_to_delete = 1
            int page_to_delete = 1;

            // Reproduce the problem.
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                // assert not wt, f'{wt=}'
                Assert.True(string.IsNullOrEmpty(wt), $"wt={wt}");
                // assert len(document) == 2, f'{len(document)=}'
                Assert.Equal(2, document.__len__());
                // document.delete_pages(page_to_delete)
                document.delete_pages(page_to_delete);
                // assert len(document) == 1, f'{len(document)=}'
                Assert.Equal(1, document.__len__());
                // document.Save(path2)
                document.Save(path2);
                // wt = pymupdf.TOOLS.mupdf_warnings()
                wt = Tools.MupdfWarnings();
                // assert wt == 'repairing PDF document', f'{wt=}'
                Assert.Equal("repairing PDF document", wt);
            }
            // with pymupdf.open(path2) as document:
            using (var document = new Document(path2))
            {
                // Expect incorrect result.
                // assert len(document) == 2, f'{len(document)=}'
                Assert.Equal(2, document.__len__());
            }

            // Call mupdf.pdf_repair_xref() before delete_pages(); this works around the
            // problem.
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // document_pdf = pymupdf._as_pdf_document(document)
                var document_pdf = Helpers.AsPdfDocument(document);
                // pymupdf.mupdf.pdf_repair_xref(document_pdf)
                mupdf.mupdf.pdf_repair_xref(document_pdf);
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                // assert wt == 'repairing PDF document', f'{wt=}'
                Assert.Equal("repairing PDF document", wt);
                // document.delete_pages(page_to_delete)
                document.delete_pages(page_to_delete);
                // document.Save(path2)
                document.Save(path2);
            }
            // with pymupdf.open(path2) as document:
            using (var document = new Document(path2))
            {
                // Expect correct result.
                // assert len(document) == 1
                Assert.Equal(1, document.__len__());
            }

            // Call mupdf.pdf_check_document() before delete_pages(); this works around
            // the problem.
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // document_pdf = pymupdf._as_pdf_document(document)
                var document_pdf = Helpers.AsPdfDocument(document);
                // pymupdf.mupdf.pdf_check_document(document_pdf)
                mupdf.mupdf.pdf_check_document(document_pdf);
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                // assert wt == 'repairing PDF document', f'{wt=}'
                Assert.Equal("repairing PDF document", wt);
                // document.delete_pages(page_to_delete)
                document.delete_pages(page_to_delete);
                // document.Save(path2)
                document.Save(path2);
            }
            // with pymupdf.open(path2) as document:
            using (var document = new Document(path2))
            {
                // Expect correct result.
                // assert len(document) == 1
                Assert.Equal(1, document.__len__());
            }

            // Check that document is marked as repaired after save.
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // assert not document.is_repaired, f'{document.is_repaired=}'
                Assert.False(document.is_repaired(), $"is_repaired={document.is_repaired()}");
                // document.Save(path2)
                document.Save(path2);
                // assert document.is_repaired, f'{document.is_repaired=}'
                Assert.True(document.is_repaired(), $"is_repaired={document.is_repaired()}");
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                // assert wt == 'repairing PDF document', f'{wt=}'
                Assert.Equal("repairing PDF document", wt);
            }

            // Check that raise_on_repair=True works.
            // with pymupdf.open(path) as document:
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
                    // print(f'Received expected exception: {e}', flush=1)
                    Console.WriteLine($"Received expected exception: {e}");
                    Console.Out.Flush();
                }
                // else:
                //     assert 0, 'Did not get expected exception.'
                Assert.True(gotException, "Did not get expected exception.");
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                // assert wt == 'repairing PDF document'
                Assert.Equal("repairing PDF document", wt);
            }

            // Check that Document.repair() works.
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // document.repair()
                document.repair();
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                // assert wt == 'repairing PDF document'
                Assert.Equal("repairing PDF document", wt);
                // document.delete_pages(page_to_delete)
                document.delete_pages(page_to_delete);
                // document.Save(path2, raise_on_repair=True)
                document.Save(path2, raise_on_repair: true);
            }
            // with pymupdf.open(path2) as document:
            using (var document = new Document(path2))
            {
                // Expect correct result.
                // assert len(document) == 1, f'{len(document)=}'
                Assert.Equal(1, document.__len__());
            }
        }
    }
}
