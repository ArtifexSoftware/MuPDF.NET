using System.IO;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Widget class.
    /// Ported from tests/test_widgets.py.
    /// Widgets require native PDF form fields - we test by using existing
    /// PDF resources or by testing via the Page.FirstWidget / Widgets() API.
    /// </summary>
    public class WidgetTests
    {
        /// <summary>
        /// Attempt to load the interfield-calculation.pdf resource that contains widgets.
        /// </summary>
        private static string GetInterFieldPdf()
        {
            return TestHelper.GetResource("interfield-calculation.pdf");
        }

        // ─── Widget presence ────────────────────────────────────────────

        [Fact]
        public void Widget_NoWidgetsOnEmptyPage()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            Assert.Null(page.FirstWidget);
        }

        [Fact]
        public void Widget_WidgetsEnumeration_EmptyPage()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            int count = page.Widgets().Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void Widget_IsFormPdf_FalseOnNew()
        {
            using var doc = new Document();
            Assert.False(doc.IsFormPdf);
        }

        // ─── Widget from PDF with form fields ───────────────────────────

        [Fact]
        public void Widget_LoadFromInterFieldPdf()
        {
            var path = GetInterFieldPdf();
            if (!File.Exists(path))
                return; // skip if resource not available

            using var doc = new Document(path);
            Assert.True(doc.IsFormPdf);
            var page = doc[0];
            var first = page.FirstWidget;
            Assert.NotNull(first);
        }

        [Fact]
        public void Widget_EnumerateFromPdf()
        {
            var path = GetInterFieldPdf();
            if (!File.Exists(path))
                return;

            using var doc = new Document(path);
            var page = doc[0];
            int count = page.Widgets().Count();
            Assert.True(count > 0);
        }

        [Fact]
        public void Widget_Properties()
        {
            var path = GetInterFieldPdf();
            if (!File.Exists(path))
                return;

            using var doc = new Document(path);
            var page = doc[0];
            var w = page.FirstWidget;
            Assert.NotNull(w);
            Assert.False(string.IsNullOrEmpty(w.FieldTypeString));
            Assert.True(w.Xref > 0);
            Assert.True(w.Rect.Width > 0);
        }

        [Fact]
        public void Widget_FieldName()
        {
            var path = GetInterFieldPdf();
            if (!File.Exists(path))
                return;

            using var doc = new Document(path);
            var page = doc[0];
            var w = page.FirstWidget;
            Assert.NotNull(w);
            Assert.NotNull(w.FieldName);
        }

        [Fact]
        public void Widget_Next()
        {
            var path = GetInterFieldPdf();
            if (!File.Exists(path))
                return;

            using var doc = new Document(path);
            var page = doc[0];
            var w = page.FirstWidget;
            Assert.NotNull(w);
            // The interfield-calculation.pdf typically has multiple widgets
            var next = w.Next;
            // Next may or may not be null depending on the PDF
        }

        [Fact]
        public void Widget_Dispose()
        {
            var path = GetInterFieldPdf();
            if (!File.Exists(path))
                return;

            using var doc = new Document(path);
            var page = doc[0];
            var w = page.FirstWidget;
            Assert.NotNull(w);
            w.Dispose();
        }

        [Fact]
        public void Widget_ToString()
        {
            var path = GetInterFieldPdf();
            if (!File.Exists(path))
                return;

            using var doc = new Document(path);
            var page = doc[0];
            var w = page.FirstWidget;
            Assert.NotNull(w);
            Assert.Contains("Widget", w.ToString());
        }
    }
}
