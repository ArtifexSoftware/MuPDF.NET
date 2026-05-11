using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Outline class.
    /// Ported from tests/test_toc.py.
    /// </summary>
    public class OutlineTests
    {
        [Fact]
        public void Outline_NullOnEmpty()
        {
            using var doc = new Document();
            doc.NewPage();
            var outline = doc.GetOutline();
            Assert.Null(outline);
        }

        [Fact]
        public void Outline_GetTocEmpty()
        {
            using var doc = new Document();
            doc.NewPage();
            var toc = doc.GetToc();
            Assert.Empty(toc);
        }
    }
}
