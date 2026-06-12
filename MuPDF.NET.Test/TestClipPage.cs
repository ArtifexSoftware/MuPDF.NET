// Port of PyMuPDF-1.27.2.2/tests/test_clip_page.py
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Test <see cref="Page.ClipToRect"/>.
    /// Input: <c>TestDocuments/TestClipPage/v110-changes.pdf</c>;
    /// output: <c>TestDocuments/_Output/TestClipPage/test_clip.pdf</c>.
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class TestClipPage
    {
        private static readonly string testDocPath = _Path.ForTestClass("v110-changes.pdf", nameof(TestClipPage));
        private static readonly string outDocPath = _Path.ForOutput("test_clip.pdf", nameof(TestClipPage));

        /// <summary>
        /// Clip a <see cref="Page"/> to a rectangle and confirm that no text has survived
        /// that is completely outside the rectangle.
        /// <summary>Regression test: clip (PyMuPDF <c>tests/test_clip_page.py::test_clip</c>).</summary>
        /// </summary>
        [Fact]
        public void test_clip()
        {
            var rect = new Rect(200, 200, 400, 500);
            // filename = os.path.join(scriptdir, "resources", "v110-changes.pdf")
            using var doc = new Document(testDocPath);
            var page = doc[0];
            // clip the page to the rectangle
            page.ClipToRect(rect);

            // capture font warning message of MuPDF
            // if mupdf_version_tuple < (1, 27): assert TOOLS.mupdf_warnings() == "bogus font ascent/descent values (0 / 0)"

            // extract all text characters and assert that each one
            // has a non-empty intersection with the rectangle.
            using var tp = page.GetTextPage();
            var rawdict = tp.ExtractRawDict();
            var blocks = (List<Dictionary<string, object>>)rawdict["blocks"];

            foreach (var b in blocks)
            {
                if (!b.TryGetValue("lines", out var linesObj) || linesObj is not List<Dictionary<string, object>> lines)
                    continue;
                foreach (var line in lines)
                {
                    var spans = (List<Dictionary<string, object>>)line["spans"];
                    foreach (var span in spans)
                    {
                        if (!span.TryGetValue("chars", out var charsObj) || charsObj is not List<Dictionary<string, object>> chars)
                            continue;
                        foreach (var ch in chars)
                        {
                            var bboxArr = (float[])ch["bbox"];
                            var bbox = new Rect(bboxArr[0], bboxArr[1], bboxArr[2], bboxArr[3]);
                            if (bbox.IsEmpty)
                                continue;
                            string c = ch.TryGetValue("c", out var co) && co is string s ? s : "?";
                            Assert.True(
                                bbox.Intersects(rect),
                                $"Character '{c}' at {bbox} is outside of {rect}.");
                        }
                    }
                }
            }
            doc.Save(outDocPath);
        }

        /// <summary>clip page and read links without error (PyMuPDF <c>tests/test_4942.py::test_4942</c>).</summary>
        [Fact]
        public void test_4942()
        {
            using var document = new Document(_Path.ForTestClass("test_4942.pdf", nameof(TestClipPage)));
            var page = document[0];
            page.ClipToRect(page.Rect);
            var links = page.GetLinks();
            Assert.Equal(8, links.Count);
        }
    }
}
