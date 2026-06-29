using PDF4LLM;
using PDF4LLM.Layout;
using Xunit;

namespace PDF4LLM.Test
{
    /// <summary>Port of <c>tests/llama_index/test_layout.py</c>.</summary>
    [Collection("PDF4LLM")]
    public class TestLayout
    {
        [Fact]
        public void test_layout_switch()
        {
            // Check that we can activate/deactivate use of layout.

            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(true);
                Assert.True(PdfExtractor.UseLayout);
                if (PyMuPdfLayout.IsAvailable)
                    Assert.True(PdfExtractor.LayoutAvailable);

                PdfExtractor.SetUseLayout(false);
                Assert.False(PdfExtractor.UseLayout);
                Assert.False(PdfExtractor.LayoutAvailable);

                PdfExtractor.SetUseLayout(true);
                Assert.True(PdfExtractor.UseLayout);
                if (PyMuPdfLayout.IsAvailable)
                    Assert.True(PdfExtractor.LayoutAvailable);
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_layout_default()
        {
            // Check that deactivating layout clears the provider.
            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(false);
                Assert.False(PdfExtractor.UseLayout);
                Assert.False(PdfExtractor.LayoutAvailable);
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_layout_provider_returns_boxes()
        {
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(true);
                Assert.True(PdfExtractor.LayoutAvailable);

                string path = _Path.ForTestClass("test_370.pdf", nameof(Test370));
                using (var doc = new MuPDF.NET.Document(path))
                {
                    var page = doc[0];
                    object layout = page.GetLayout();
                    Assert.NotNull(layout);
                    var rows = layout as System.Collections.IList;
                    Assert.NotNull(rows);
                    Assert.True(rows.Count > 0);
                }
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }
    }
}
