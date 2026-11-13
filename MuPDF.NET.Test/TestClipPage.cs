using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Test <see cref="Page.ClipToRect"/>.
    /// Inputs: <c>TestDocuments/MuPDF.NET.Test/TestClipPage/</c>. Port of PyMuPDF 1.28.2 <c>tests/test_clip_page.py</c>.
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class TestClipPage
    {
        private const string TestClassName = nameof(TestClipPage);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

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
            if (!_Version.mupdf_version_tuple_at_least(1, 27, 0))
                Assert.Equal("bogus font ascent/descent values (0 / 0)", Tools.MupdfWarnings());

            // Extract all text characters and assert that each one
            // has a non-empty intersection with the rectangle.
            var rawdict = page.GetText("rawdict") as PageInfo;
            Assert.NotNull(rawdict);

            var chars = (rawdict.Blocks ?? new List<Block>())
                .Where(b => b?.Lines != null)
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

        [Fact]
        public void test_5108()
        {
            Console.WriteLine();
            string path = Doc("test_5108.pdf");

            byte[] ClippedContent(int i, Rect clip)
            {
                using var document = new Document(path);
                var page = document[0];
                page.ClipToRect(clip);
                using var ms = new MemoryStream();
                document.Save(ms, garbage: 3, clean: 1);
                using var document2 = new Document(ms.ToArray(), "pdf");
                page = document2[0];
                var joined = new List<byte>();
                foreach (int x in page.GetContents())
                {
                    byte[] stream = document2.XrefStream(x);
                    if (stream != null && stream.Length > 0)
                        joined.AddRange(stream);
                }
                byte[] ret = joined.ToArray();
                page.DrawRect(clip, new float[] { 1, 0, 0 });
                document2.Save(Out($"test_5108.out.{i}.pdf"));
                return ret;
            }

            int numErrors = 0;
            var cases = new (bool shKeptExpected, string label, Rect clip)[]
            {
                (true,  "whole page       ", new Rect(0, 0, 400, 400)),
                (true,  "right half       ", new Rect(200, 0, 400, 400)),
                (true,  "page inset by 1pt", new Rect(1, 1, 399, 399)),
                (false, "1x1 at PDF origin", new Rect(0, 399, 1, 400)),
                (false, "                 ", new Rect(0, 0, 10, 10)),
                (false, "                 ", new Rect(0, 2, 10, 7)),
                (true,  "Partial          ", new Rect(100, 100, 300, 300)),
            };
            for (int i = 0; i < cases.Length; i++)
            {
                var (shKeptExpected, label, clip) = cases[i];
                byte[] content = ClippedContent(i, clip);
                bool shKept = ContainsBytes(content, (byte)'s', (byte)'h');
                if (shKept != shKeptExpected)
                    numErrors++;
                Console.WriteLine($"    {label} -> shKept={shKept} {Encoding.Latin1.GetString(content)}");
            }
            Console.WriteLine($"numErrors={numErrors}");
            if (_Version.mupdf_version_tuple_at_least(1, 28, 5))
                Assert.Equal(0, numErrors);
            else
                Assert.True(numErrors > 0);
        }

        static bool ContainsBytes(byte[] data, byte a, byte b)
        {
            if (data == null || data.Length < 2)
                return false;
            for (int i = 0; i < data.Length - 1; i++)
            {
                if (data[i] == a && data[i + 1] == b)
                    return true;
            }
            return false;
        }
    }
}
