using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Test <see cref="Page.ClipToRect"/>.
    /// Port of PyMuPDF 1.28.2 <c>tests/test_clip_page.py</c>.
    /// Input: <c>TestDocuments/TestClipPage/v110-changes.pdf</c>.
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class TestClipPage
    {
        private const string TestClassName = nameof(TestClipPage);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        /// <summary>
        /// Clip a Page to a rectangle and confirm that no text has survived
        /// that is completely outside the rectangle.
        /// </summary>
        [Fact]
        public void test_clip()
        {
            var rect = new Rect(200, 200, 400, 500);
            string filename = Doc("v110-changes.pdf");
            using var doc = new Document(filename);
            var page = doc[0];
            page.ClipToRect(rect);  // clip the page to the rectangle

            // capture font warning message of MuPDF
            // Python: if mupdf_version_tuple < (1, 27): assert TOOLS.mupdf_warnings() == "..."
            // MuPDF.NET ships MuPDF 1.28.2+, so that branch is skipped.

            // extract all text characters and assert that each one
            // has a non-empty intersection with the rectangle.
            var rawdict = page.GetText("rawdict") as PageInfo;
            Assert.NotNull(rawdict);

            var chars = (rawdict.Blocks ?? new List<Block>())
                .Where(b => b?.Type == 0 && b.Lines != null)
                .SelectMany(b => b.Lines)
                .Where(l => l?.Spans != null)
                .SelectMany(l => l.Spans)
                .Where(s => s?.Chars != null)
                .SelectMany(s => s.Chars)
                .Where(c => c != null)
                .ToList();

            foreach (var ch in chars)
            {
                var bbox = new Rect(ch.Bbox);
                if (bbox.IsEmpty)
                    continue;
                Assert.True(
                    bbox.Intersects(rect),
                    $"Character '{ch.C}' at {bbox} is outside of {rect}.");
            }
        }
    }
}
