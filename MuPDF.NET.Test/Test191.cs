using System.Threading.Tasks;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Parallel <see cref="Page.GetPixmap"/> must not crash: independent documents may
    /// render concurrently; a shared document is serialized on its native lock (#191).
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class Test191
    {
        [Fact]
        public void test_191_independent_getpixmap_parallel()
        {
            byte[] data = BuildOnePagePdf();
            int width = 0;
            Parallel.For(0, 16, new ParallelOptions { MaxDegreeOfParallelism = 8 }, _ =>
            {
                using var doc = new Document(stream: data);
                using Page page = doc.LoadPage(0);
                using var pix = page.GetPixmap(new Matrix(2, 2));
                Assert.True(pix.Width > 0);
                Interlocked.Exchange(ref width, pix.Width);
            });
            Assert.True(width > 0);
        }

        [Fact]
        public void test_191_shared_getpixmap_parallel()
        {
            byte[] data = BuildOnePagePdf();
            using var doc = new Document(stream: data);
            int width = 0;
            Parallel.For(0, 16, new ParallelOptions { MaxDegreeOfParallelism = 8 }, _ =>
            {
                using Page page = doc.LoadPage(0);
                using var pix = page.GetPixmap(new Matrix(2, 2));
                Assert.True(pix.Width > 0);
                Interlocked.Exchange(ref width, pix.Width);
            });
            Assert.True(width > 0);
        }

        private static byte[] BuildOnePagePdf()
        {
            using var doc = new Document();
            using Page page = doc.NewPage();
            page.InsertText(new Point(72, 72), "issue-191", fontSize: 12);
            return doc.Write();
        }
    }
}
