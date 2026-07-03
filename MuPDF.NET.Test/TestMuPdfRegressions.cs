using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestMuPdfRegressions/</c>;
    /// outputs: <c>TestDocuments/_Output/TestMuPdfRegressions/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestMuPdfRegressions
    {
        private const string TestClassName = nameof(TestMuPdfRegressions);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> GetWords(Page page, bool sort = false)
            => page.GetTextWords(sort: sort);

        [Fact]
        public void test_707448()
        {
            // filename = os.path.join(scriptdir, "resources", "test-707448.pdf")
            string filename = Doc("test-707448.pdf");
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // words0 = page.GetText("words")
            var words0 = GetWords(page);
            // page.CleanContents(sanitize=True)
            page.CleanContents(sanitize: 1);
            // words1 = page.GetText("words")
            var words1 = GetWords(page);
            Assert.True(_Compare.GentleCompareWords(words0, words1));
            doc.Save(Out("test_707448.pdf"));
        }

        [Fact]
        public void test_707673()
        {
            // Fails starting with MuPDF v1.23.9.
            // Fixed in:
            // commit 779b8234529cb82aa1e92826854c7bb98b19e44b (golden/master)
            // filename = os.path.join(scriptdir, "resources", "test-707673.pdf")
            string filename = Doc("test-707673.pdf");
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // words0 = page.GetText("words")
            var words0 = GetWords(page);
            // page.CleanContents(sanitize=True)
            page.CleanContents(sanitize: 1);
            // words1 = page.GetText("words")
            var words1 = GetWords(page);
            // ok = gentle_compare.gentle_compare(words0, words1)
            bool ok = _Compare.GentleCompareWords(words0, words1);
            Assert.True(ok);
            doc.Save(Out("test_707673.pdf"));
        }

        [Fact]
        public void test_707727()
        {
            // MuPDF issue: https://bugs.ghostscript.com/show_bug.cgi?id=707727
            // filename = os.path.join(scriptdir, "resources", "test_3362.pdf")
            string filename = Doc("test_3362.pdf");
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // pix0 = page.GetPixmap()
            var pix0 = page.GetPixmap();
            // page.CleanContents(sanitize=True)
            page.CleanContents(sanitize: 1);
            // page = doc.ReloadPage(page)  # required to prevent re-use
            page = doc.ReloadPage(page);
            // pix1 = page.GetPixmap()
            var pix1 = page.GetPixmap();
            // rms = gentle_compare.pixmaps_rms(pix0, pix1)
            float rms = _Compare.PixmapsRms(pix0, pix1);
            Console.WriteLine($"rms={rms}");
            // pix0.Save(os.path.normpath(f'{__file__}/../../tests/test_707727_pix0.png'))
            pix0.Save(Out("test_707727_pix0.png"));
            // pix1.Save(os.path.normpath(f'{__file__}/../../tests/test_707727_pix1.png'))
            pix1.Save(Out("test_707727_pix1.png"));
            // New sanitising gives small fp rounding errors.
            Assert.True(rms < 0.05);
        }

        [Fact]
        public void test_707721()
        {
            // MuPDF issue https://github.com/ArtifexSoftware/mupdf/issues/3357
            // MuPDF issue: https://bugs.ghostscript.com/show_bug.cgi?id=707721
            // filename = os.path.join(scriptdir, "resources", "test_3357.pdf")
            string filename = Doc("test_3357.pdf");
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // ok = page.GetText()
            string ok = (string)page.GetText();
            Assert.False(string.IsNullOrEmpty(ok));
        }

        [Fact]
        public void test_3376()
        {
            // https://bugs.ghostscript.com/show_bug.cgi?id=707733
            // MuPDF issue https://github.com/ArtifexSoftware/mupdf/issues/3376
            // Test file contains a redaction for the first 3 words: "Table of Contents".
            // Test strategy:
            // - extract all words (sorted)
            // - apply redactions
            // - extract words again
            // - confirm: we now have 3 words less and remaining words are equal.
            // filename = os.path.join(scriptdir, "resources", "test_3376.pdf")
            string filename = Doc("test_3376.pdf");
            using var doc = new Document(filename);
            // page = doc[0]
            var page = doc[0];
            // words0 = page.GetText("words", sort=True)
            var words0 = GetWords(page, sort: true);
            // words0_s = words0[:3]  # first 3 words
            var words0_s = words0.GetRange(0, 3);
            // words0_e = words0[3:]  # remaining words
            var words0_e = words0.GetRange(3, words0.Count - 3);
            Assert.Equal("Table of Contents", string.Join(" ", words0_s.Select(w => w.word)));

            // page.ApplyRedactions()
            page.ApplyRedactions();

            // words1 = page.GetText("words", sort=True)
            var words1 = GetWords(page, sort: true);

            // ok = gentle_compare.gentle_compare(words0_e, words1)
            bool ok = _Compare.GentleCompareWords(words0_e, words1);
            Assert.True(ok);
            doc.Save(Out("test_3376.pdf"));
        }
    }
}