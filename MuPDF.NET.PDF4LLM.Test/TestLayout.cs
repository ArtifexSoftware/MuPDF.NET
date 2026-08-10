using MuPDF.NET.PDF4LLM;
using MuPDF.NET.PDF4LLM.Layout;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestLayout
    {
        [Fact]
        public void test_layout_switch()
        {
            // Check that we can activate/deactivate use of layout.

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                Assert.True(MuPDF4LLM.UseLayout);
                if (PyMuPdfLayout.IsAvailable)
                    Assert.True(MuPDF4LLM.LayoutAvailable);

                MuPDF4LLM.SetUseLayout(false);
                Assert.False(MuPDF4LLM.UseLayout);
                Assert.False(MuPDF4LLM.LayoutAvailable);

                MuPDF4LLM.SetUseLayout(true);
                Assert.True(MuPDF4LLM.UseLayout);
                if (PyMuPdfLayout.IsAvailable)
                    Assert.True(MuPDF4LLM.LayoutAvailable);
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_layout_default()
        {
            // Fresh interpreter import enables layout analysis when the layout bridge is installed.
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                Assert.True(MuPDF4LLM.UseLayout);
                Assert.True(MuPDF4LLM.LayoutAvailable);
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_layout_provider_returns_boxes()
        {
            if (!PyMuPdfLayout.IsAvailable)
                return;

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                Assert.True(MuPDF4LLM.LayoutAvailable);

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
                MuPDF4LLM.SetUseLayout(prior);
            }
        }
    }
}