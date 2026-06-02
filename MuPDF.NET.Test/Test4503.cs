// Port of PyMuPDF-1.27.2.2/tests/test_4503.py
//
// Test for issue #4503 in pymupdf:
// Correct recognition of strikeout and underline styles in text spans.
using System;
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test4503
    {
        private static readonly int STRIKEOUT = mupdf.mupdf.FZ_STEXT_STRIKEOUT;
        private static readonly int UNDERLINE = mupdf.mupdf.FZ_STEXT_UNDERLINE;

        private static readonly string testDocPath = _Path.ForTestClass("test-4503.pdf", nameof(Test4503));

        private static List<Dictionary<string, object>> AsDictList(object value) =>
            (List<Dictionary<string, object>>)value;

        [Fact]
        public void test_4503()
        {
            // Check that the text span with the specified text has the correct styling:
            // strikeout, but no underline.
            // Previously, the text was broken in multiple spans with span breaks at
            // every space. and some parts were not detected as strikeout at all.
            //
            // scriptdir = os.path.dirname(os.path.abspath(__file__))
            const string text = "the right to request the state to review and, if appropriate,";
            // filename = os.path.join(scriptdir, "resources", "test-4503.pdf")
            using var doc = new Document(testDocPath);
            Page page = doc[0];
            int flags = mupdf.mupdf.FZ_STEXT_ACCURATE_BBOXES | mupdf.mupdf.FZ_STEXT_COLLECT_STYLES;
            var spans = new List<Dictionary<string, object>>();
            var pageText = (Dictionary<string, object>)page.GetText("dict", flags: flags);
            foreach (var b in AsDictList(pageText["blocks"]))
            {
                foreach (var l in AsDictList(b["lines"]))
                {
                    foreach (var s in AsDictList(l["spans"]))
                    {
                        if ((string)s["text"] == text)
                            spans.Add(s);
                    }
                }
            }
            Assert.True(spans.Count > 0, "No spans found with the specified text");
            Dictionary<string, object> span = spans[0];

            int charFlags = Convert.ToInt32(span["char_flags"]);
            Assert.NotEqual(0, charFlags & STRIKEOUT);
            Assert.Equal(0, charFlags & UNDERLINE);
        }
    }
}
