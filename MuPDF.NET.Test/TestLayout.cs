using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
    public class TestLayout
    {
        [Fact]
        public void test_layout_switch()
        {
            // Check that we can activate/deactivate use of layout.

            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(true);
                Assert.True(Utils.LayoutEnabled);
                if (Utils.LayoutPythonAvailable)
                    Assert.True(Utils.LayoutAvailable);

                Utils.UseLayout(false);
                Assert.False(Utils.LayoutEnabled);
                Assert.False(Utils.LayoutAvailable);

                Utils.UseLayout(true);
                Assert.True(Utils.LayoutEnabled);
                if (Utils.LayoutPythonAvailable)
                    Assert.True(Utils.LayoutAvailable);
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        [Fact]
        public void test_layout_default()
        {
            // Fresh interpreter import enables layout analysis when the layout bridge is installed.
            if (!Utils.LayoutPythonAvailable)
                return;

            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(true);
                Assert.True(Utils.LayoutEnabled);
                Assert.True(Utils.LayoutAvailable);
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        [Fact]
        public void test_get_layout_stays_off_after_use_layout_false()
        {
            // use_layout(False) must stick; GetLayout must not re-enable it.
            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(false);
                using var doc = new MuPDF.NET.Document();
                doc.NewPage();
                Assert.Null(doc[0].GetLayout());
                Assert.False(Utils.LayoutAvailable);
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        [Fact]
        public void test_layout_provider_returns_boxes()
        {
            if (!Utils.LayoutPythonAvailable)
                return;

            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(true);
                Assert.True(Utils.LayoutAvailable);

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
                Utils.UseLayout(prior);
            }
        }
    }
}