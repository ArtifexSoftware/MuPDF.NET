// Port of PyMuPDF-1.27.2.2/tests/test_badfonts.py
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tolerate non-Latin font names when opening a PDF and enumerating fonts.
    /// Input: <c>TestDocuments/TestBadFonts/has-bad-fonts.pdf</c>.
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class TestBadFonts
    {
        private static readonly string testDocPath = _Path.ForTestClass("has-bad-fonts.pdf", nameof(TestBadFonts));

        /// <summary>PyMuPDF <c>tests/test_badfonts.py::test_survive_names</c>.</summary>
        [Fact]
        public void test_survive_names()
        {
            // filename = os.path.join(scriptdir, "resources", "has-bad-fonts.pdf")
            using var doc = new Document(testDocPath);
            var fonts = doc.GetPageFonts(0);
            Assert.NotEmpty(fonts);
        }
    }
}
