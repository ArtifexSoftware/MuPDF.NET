using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test4520
    {
        private static readonly string outDocPath = _Path.ForOutput("test_4520.pdf", nameof(Test4520));

        [Fact]
        public void test_4520()
        {
            // Accept source pages without /Contents object in show_pdf_page.
            using var tar = new Document();
            using var src = new Document();
            src.NewPage();
            Page page = tar.NewPage();
            int xref = page.ShowPdfPage(page.Rect, src, 0);
            Assert.NotEqual(0, xref);
            tar.Save(outDocPath);
        }
    }
}