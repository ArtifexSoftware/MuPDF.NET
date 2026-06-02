// """
// Tests:
//     * Convert some image to a PDF
//     * Insert it rotated in some rectangle of a PDF page
//     * Assert PDF Form XObject has been created
//     * Assert that image contained in inserted PDF is inside given rectangle
// """
// import os
//
// import pymupdf
using System;
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>Port of <c>PyMuPDF-1.27.2.2/tests/test_showpdfpage.py</c>.</summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestShowpdfpage/</c>; outputs: <c>TestDocuments/_Output/TestShowpdfpage/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestShowpdfpage
    {
        private const string TestClassName = nameof(TestShowpdfpage);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static Rect BboxFromInfo(Dictionary<string, object> im)
        {
            var bboxObj = im["bbox"];
            if (bboxObj is Rect r)
                return r;
            if (bboxObj is float[] f && f.Length >= 4)
                return new Rect(f[0], f[1], f[2], f[3]);
            if (bboxObj is float[] d && d.Length >= 4)
                return new Rect(d[0], d[1], d[2], d[3]);
            throw new InvalidOperationException($"unexpected bbox type: {bboxObj?.GetType().Name}");
        }

        [Fact]
        public void test_insert()
        {
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            // rect = pymupdf.Rect(50, 50, 100, 100)  # insert in here
            var rect = new Rect(50, 50, 100, 100);
            // img = pymupdf.open(imgfile)  # open image
            using var img = new Document(Doc("nur-ruhig.jpg"));
            // tobytes = img.convert_to_pdf()  # get its PDF version (bytes object)
            byte[] tobytes = img.convert_to_pdf();
            // src = pymupdf.open("pdf", tobytes)  # open as PDF
            using var src = new Document(tobytes, "pdf");
            // xref = page.ShowPdfPage(rect, src, 0, rotate=-23)  # insert in rectangle
            int xref = page.ShowPdfPage(rect, src, 0, rotate: -23);
            // extract just inserted image info
            // img = page.GetImages(True)[0]
            var imgTuple = doc.get_page_images_py(page.Number, full: true)[0];
            // assert img[-1] == xref  # xref of Form XObject!
            Assert.Equal(xref, (int)imgTuple[^1]);
            // img = page.GetImageInfo()[0]  # read the page's images
            var imgInfo = page.GetImageInfoDict()[0];

            // Multiple computations may have lead to rounding deviations, so we need
            // some generosity here: enlarge rect by 1 point in each direction.
            // assert img["bbox"] in rect + (-1, -1, 1, 1)
            var expanded = rect + new Rect(-1, -1, 1, 1);
            Assert.True(expanded.Contains(BboxFromInfo(imgInfo)));
            doc.Save(Out("test_insert.pdf"));
        }

        [Fact]
        public void test_2742()
        {
            // dest = pymupdf.open()
            using var dest = new Document();
            // destpage = dest.NewPage(width=842, height=595)
            var destpage = dest.NewPage(width: 842, height: 595);

            // a5 = pymupdf.Rect(0, 0, destpage.rect.width / 3, destpage.rect.height)
            var a5 = new Rect(0, 0, destpage.Rect.Width / 3, destpage.Rect.Height);
            // shiftright = pymupdf.Rect(destpage.rect.width/3, 0, destpage.rect.width/3, 0)
            var shiftright = new Rect(destpage.Rect.Width / 3, 0, destpage.Rect.Width / 3, 0);

            // src = pymupdf.open(os.path.abspath(f'{__file__}/../../tests/resources/test_2742.pdf'))
            using var src = new Document(Doc("test_2742.pdf"));

            // destpage.ShowPdfPage(a5, src, 0)
            destpage.ShowPdfPage(a5, src, 0);
            // destpage.ShowPdfPage(a5 + shiftright, src, 0)
            destpage.ShowPdfPage(a5 + shiftright, src, 0);
            // destpage.ShowPdfPage(a5 + shiftright + shiftright, src, 0)
            destpage.ShowPdfPage(a5 + shiftright + shiftright, src, 0);

            // dest.Save(os.path.abspath(f'{__file__}/../../tests/test_2742-out.pdf'))
            dest.Save(Out("test_2742.pdf"));
            // print("The end!")

            // wt = pymupdf.TOOLS.mupdf_warnings()
            string wt = Tools.MupdfWarnings();
            // assert wt == (
            //         'Circular dependencies! Consider page cleaning.\n'
            //         '... repeated 3 times...'
            //         ), f'{wt=}'
            Assert.Equal(
                "Circular dependencies! Consider page cleaning.\n... repeated 3 times...",
                wt);
        }
    }
}
