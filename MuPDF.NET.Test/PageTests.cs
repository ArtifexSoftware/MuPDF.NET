using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Page class.
    /// Ported from tests/test_general.py page-level tests.
    /// </summary>
    public class PageTests
    {
        private Document CreateDocWithPage(float width = 595, float height = 842)
        {
            var doc = new Document();
            doc.NewPage(width: width, height: height);
            return doc;
        }

        // ─── Properties ─────────────────────────────────────────────────

        [Fact]
        public void Page_Number()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            Assert.Equal(0, page.Number);
        }

        [Fact]
        public void Page_Rect()
        {
            using var doc = CreateDocWithPage(595, 842);
            var page = doc[0];
            Assert.True(TestHelper.IsClose(595, page.Rect.Width));
            Assert.True(TestHelper.IsClose(842, page.Rect.Height));
        }

        [Fact]
        public void Page_WidthHeight()
        {
            using var doc = CreateDocWithPage(200, 300);
            var page = doc[0];
            Assert.True(TestHelper.IsClose(200, page.Width));
            Assert.True(TestHelper.IsClose(300, page.Height));
        }

        [Fact]
        public void Page_Rotation_DefaultIsZero()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            Assert.Equal(0, page.Rotation);
        }

        [Fact]
        public void Page_IsPdf()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            Assert.True(page.IsPdf);
        }

        [Fact]
        public void Page_Xref()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            Assert.True(page.Xref > 0);
        }

        // ─── Annotations ────────────────────────────────────────────────

        [Fact]
        public void Page_FirstAnnot_NullOnEmpty()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            Assert.Null(page.FirstAnnot);
        }

        [Fact]
        public void Page_FirstLink_NullOnEmpty()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            Assert.Null(page.FirstLink);
        }

        [Fact]
        public void Page_AddTextAnnot()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var annot = page.AddTextAnnot(new Point(100, 100), "Hello");
            Assert.NotNull(annot);
            Assert.NotNull(page.FirstAnnot);
        }

        [Fact]
        public void Page_AddRectAnnot()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var annot = page.AddRectAnnot(new Rect(50, 50, 200, 200));
            Assert.NotNull(annot);
        }

        [Fact]
        public void Page_AddCircleAnnot()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var annot = page.AddCircleAnnot(new Rect(50, 50, 200, 200));
            Assert.NotNull(annot);
        }

        [Fact]
        public void Page_AddLineAnnot()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var annot = page.AddLineAnnot(new Point(50, 50), new Point(200, 200));
            Assert.NotNull(annot);
        }

        [Fact]
        public void Page_AddCaretAnnot()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var annot = page.AddCaretAnnot(new Point(100, 100));
            Assert.NotNull(annot);
        }

        [Fact]
        public void Page_AddStampAnnot()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var annot = page.AddStampAnnot(new Rect(100, 100, 300, 200));
            Assert.NotNull(annot);
        }

        [Fact]
        public void Page_DeleteAnnot()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var annot = page.AddTextAnnot(new Point(100, 100), "To Delete");
            Assert.NotNull(page.FirstAnnot);
            page.DeleteAnnot(annot);
        }

        [Fact]
        public void Page_AnnotsEnumeration()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            page.AddTextAnnot(new Point(50, 50), "A");
            page.AddTextAnnot(new Point(100, 100), "B");
            int count = page.Annots().Count();
            Assert.Equal(2, count);
        }

        // ─── Page Modifications ─────────────────────────────────────────

        [Fact]
        public void Page_SetRotation()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            page.SetRotation(90);
            Assert.Equal(90, page.Rotation);
        }

        [Fact]
        public void Page_SetCropBox()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            page.SetCropBox(new Rect(10, 10, 200, 300));
        }

        // ─── Contents ───────────────────────────────────────────────────

        [Fact]
        public void Page_GetContents()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var contents = page.GetContents();
            Assert.NotNull(contents);
        }

        // ─── Drawing ────────────────────────────────────────────────────

        [Fact]
        public void Page_DrawRect()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            page.DrawRect(new Rect(10, 10, 100, 100));
        }

        [Fact]
        public void Page_DrawLine()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            page.DrawLine(new Point(10, 10), new Point(100, 100));
        }

        [Fact]
        public void Page_DrawCircle()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            page.DrawCircle(new Point(200, 200), 50);
        }

        [Fact]
        public void Page_NewShape()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var shape = page.NewShape();
            Assert.NotNull(shape);
        }

        // ─── Text Insertion ─────────────────────────────────────────────

        [Fact]
        public void Page_InsertText()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            int lines = page.InsertText(new Point(72, 72), "Hello World");
            Assert.True(lines > 0);
        }

        // ─── Search ─────────────────────────────────────────────────────

        [Fact]
        public void Page_SearchFor_NoResults()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            var results = page.SearchFor("nonexistent text");
            Assert.Empty(results);
        }

        // ─── Pixmap ─────────────────────────────────────────────────────

        [Fact]
        public void Page_GetPixmap()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            using var pix = page.GetPixmap();
            Assert.True(pix.Width > 0);
            Assert.True(pix.Height > 0);
        }

        // ─── Text Extraction ────────────────────────────────────────────

        [Fact]
        public void Page_GetTextPage()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            using var tp = page.GetTextPage();
            Assert.NotNull(tp);
        }

        [Fact]
        public void Page_GetText_Text()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            string text = page.GetText("text");
            Assert.NotNull(text);
        }

        // ─── Dispose ────────────────────────────────────────────────────

        [Fact]
        public void Page_Dispose()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            page.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = page.Rect);
        }

        [Fact]
        public void Page_ToString()
        {
            using var doc = CreateDocWithPage();
            var page = doc[0];
            Assert.Contains("page", page.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Port of <c>tests/test_geometry.py::test_pageboxes</c> (page boxes + PDF xref strings).</summary>
        [Fact]
        public void Page_PageBoxes_SetAndXrefMatchPython()
        {
            using var doc = CreateDocWithPage(595, 842);
            var page = doc[0];
            Assert.Equal(page.CropBox, page.ArtBox);
            Assert.Equal(page.CropBox, page.BleedBox);
            Assert.Equal(page.CropBox, page.TrimBox);

            var rect = new Rect(100, 200, 400, 700);
            page.SetCropBox(rect);
            page.SetArtBox(rect);
            page.SetBleedBox(rect);
            page.SetTrimBox(rect);

            Assert.Equal(rect, page.CropBox);

            int xref = page.Xref;
            Assert.Equal(doc.PageXref(0), xref);
            Assert.True(xref > 0);

            // Keys exist on the page object; xref string matches the rect written by pdf_dict_put_rect.
            const string expectedPdfArray = "[100 200 400 700]";
            foreach (var key in new[] { "CropBox", "ArtBox", "BleedBox", "TrimBox" })
            {
                var (type, value) = doc.XrefGetKey(xref, key);
                Assert.Equal("array", type);
                var normalized = Regex.Replace(value.Trim(), @"\s+", " ");
                Assert.Equal(expectedPdfArray, normalized);
            }

            Assert.Equal(page.CropBox, page.ArtBox);
            Assert.Equal(page.CropBox, page.BleedBox);
            Assert.Equal(page.CropBox, page.TrimBox);
        }
    }
}
