using System;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_nonpdf.py</c>.
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestNonpdf/</c>; outputs: <c>TestDocuments/_Output/TestNonpdf/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestNonpdf
    {
        private const string TestClassName = nameof(TestNonpdf);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_isnopdf()
        {
            using var doc = new Document(Doc("Bezier.epub"));
            Assert.False(doc.IsPdf);
        }

        [Fact]
        public void test_pageids()
        {
            using var doc = new Document(Doc("Bezier.epub"));
            Assert.Equal(7, doc.chapter_count());
            Assert.Equal((6, 1), doc.last_location());
            Assert.Equal((5, 11), doc.prev_location((6, 0)));
            Assert.Equal((6, 0), doc.next_location((5, 11)));
            // Check page numbers have no gaps:
            int i = 0;
            for (int chapter = 0; chapter < doc.chapter_count(); chapter++)
            {
                for (int cpno = 0; cpno < doc.chapter_page_count(chapter); cpno++)
                {
                    Assert.Equal(i, doc.page_number_from_location((chapter, cpno)));
                    i++;
                }
            }
        }

        [Fact]
        public void test_layout()
        {
            using var doc = new Document(Doc("Bezier.epub"));
            // loc = doc.make_bookmark((5, 11))
            var loc = doc.make_bookmark((5, 11));
            doc.layout(Utils.PaperRect("a4"));
            Assert.Equal((5, 6), doc.find_bookmark(loc));
        }
    }
}
