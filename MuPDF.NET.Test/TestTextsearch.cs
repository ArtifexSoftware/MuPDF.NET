// Search for some text on a PDF page, and compare content of returned hit
// rectangle with the searched text.
// Text search with 'clip' parameter - clip rectangle contains two occurrences
// of searched text. Confirm search locations are inside clip.
using System;
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestTextsearch/</c>; outputs: <c>TestDocuments/_Output/TestTextsearch/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestTextsearch
    {
        private const string TestClassName = nameof(TestTextsearch);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary>Default <see cref="Page.SearchFor"/> flags when none are specified.</summary>
        private static int TextFlagsSearchDefault =>
            0
            | mupdf.mupdf.FZ_STEXT_DEHYPHENATE
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP;

        /// <summary>Default text-search flag set.</summary>
        private static int TextFlagsSearch =>
            0
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
            | mupdf.mupdf.FZ_STEXT_DEHYPHENATE
            | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

        [Fact]
        public void test_search1()
        {
            using var doc = new Document(Doc("2.pdf"));
            var page = doc[0];
            const string needle = "mupdf";
            List<Rect> rlist = page.SearchForRects(needle, flags: TextFlagsSearchDefault);
            Assert.NotEmpty(rlist);
            foreach (Rect rect in rlist)
                Assert.Contains(needle, page.GetTextbox(rect).ToLowerInvariant());
        }

        [Fact]
        public void test_search2()
        {
            using var doc = new Document(Doc("github_sample.pdf"));
            var page = doc[0];
            const string needle = "the";
            var clip = new Rect(40.5f, 228.31436157226562f, 346.5226135253906f, 239.5338592529297f);
            List<Rect> rl = page.SearchForRects(needle, clip: new Quad(clip), flags: TextFlagsSearchDefault);
            Assert.Equal(2, rl.Count);
            foreach (Rect r in rl)
                Assert.True(clip.Contains(r));
        }

        [Fact]
        public void test_search3()
        {
            // Ensure we find text whether or not it contains ligatures.
            using var doc = new Document(Doc("text-find-ligatures.pdf"));
            var page = doc[0];
            const string needle = "flag";

            List<Rect> hits = page.SearchForRects(needle, flags: TextFlagsSearch);
            Assert.Equal(2, hits.Count);

            int flagsWithLigatures = TextFlagsSearch | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES;
            hits = page.SearchForRects(needle, flags: flagsWithLigatures);
            Assert.Single(hits);
        }
    }
}