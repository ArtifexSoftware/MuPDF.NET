using System;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for text extraction (TextPage, page.GetText, search).
    /// Ported from tests/test_textextract.py, tests/test_textsearch.py.
    /// </summary>
    public class TextExtractionTests
    {
        // ─── GetText variants ───────────────────────────────────────────

        [Fact]
        public void GetText_PlainText()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "Hello World");
            string text = page.GetText("text");
            Assert.Contains("Hello", text);
        }

        [Fact]
        public void GetText_Html()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "Test HTML");
            string html = page.GetText("html");
            Assert.NotNull(html);
        }

        [Fact]
        public void GetText_Xhtml()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            string xhtml = page.GetText("xhtml");
            Assert.NotNull(xhtml);
        }

        [Fact]
        public void GetText_Xml()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            string xml = page.GetText("xml");
            Assert.NotNull(xml);
        }

        // ─── TextPage ───────────────────────────────────────────────────

        [Fact]
        public void TextPage_ExtractText()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "TextPage Test");
            using var tp = page.GetTextPage();
            string text = tp.ExtractText();
            Assert.Contains("TextPage", text);
        }

        [Fact]
        public void TextPage_ExtractBlocks()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "Block Test");
            using var tp = page.GetTextPage();
            var blocks = tp.ExtractBlocks();
            Assert.NotNull(blocks);
        }

        [Fact]
        public void TextPage_ExtractWords()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "Word One Two");
            using var tp = page.GetTextPage();
            var words = tp.ExtractWords();
            Assert.NotNull(words);
        }

        [Fact]
        public void TextPage_ExtractHtml()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            using var tp = page.GetTextPage();
            string html = tp.ExtractHtml();
            Assert.NotNull(html);
        }

        [Fact]
        public void TextPage_Rect()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            using var tp = page.GetTextPage();
            Assert.NotNull(tp.Rect);
        }

        [Fact]
        public void TextPage_Dispose()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var tp = page.GetTextPage();
            tp.Dispose();
            Assert.Throws<ObjectDisposedException>(() => tp.ExtractText());
        }

        // ─── Search ─────────────────────────────────────────────────────

        [Fact]
        public void Search_FindsText()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "Findable Text");
            var results = page.SearchFor("Findable");
            Assert.NotEmpty(results);
        }

        [Fact]
        public void Search_NoMatch()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "Some text here");
            var results = page.SearchFor("ZZZZZ");
            Assert.Empty(results);
        }

        [Fact]
        public void Search_MaxHits()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "AAA AAA AAA AAA");
            var results = page.SearchFor("AAA", maxHits: 2);
            Assert.True(results.Count <= 2);
        }

        // ─── Document-level convenience ─────────────────────────────────

        [Fact]
        public void Document_SearchPageFor()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "DocSearch");
            var results = doc.SearchPageFor(0, "DocSearch");
            Assert.NotEmpty(results);
        }

        [Fact]
        public void Page_SearchFor_WrongTextPageParent_Throws()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.NewPage();
            // Second NewPage resets page wrappers; reload before use.
            var p0 = doc.LoadPage(0);
            var p1 = doc.LoadPage(1);
            p0.InsertText(new Point(72, 72), "OnlyHere");
            using var tpOther = p1.GetTextPage();
            Assert.Throws<ValueErrorException>(() => p0.SearchFor("OnlyHere", textpage: tpOther));
        }

        [Fact]
        public void Document_SearchPageFor_WithClip_FindsTextInsideClip()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "NearOrigin");
            page.InsertText(new Point(400, 400), "FarCorner");
            var clip = new Quad(new Rect(0, 0, 200, 200));
            var near = doc.SearchPageFor(0, "NearOrigin", clip: clip);
            Assert.NotEmpty(near);
        }

        [Fact]
        public void TextPage_SearchRects_ReturnsRects()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "RectSearch");
            using var tp = page.GetTextPage();
            var rects = tp.SearchRects("RectSearch");
            Assert.NotEmpty(rects);
            Assert.All(rects, r => Assert.False(r.IsEmpty));
        }

        [Fact]
        public void Document_SearchPageForRects_ReturnsRects()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "DocRectSearch");
            var rects = doc.SearchPageForRects(0, "DocRectSearch");
            Assert.NotEmpty(rects);
            Assert.All(rects, r => Assert.False(r.IsEmpty));
        }

        [Fact]
        public void Page_search_for_rects_PythonCompat_ReturnsRects()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "SnakeRect");
            var rects = page.search_for_rects("SnakeRect");
            Assert.NotEmpty(rects);
        }

        [Fact]
        public void TextPage_search_rects_PythonCompat_ReturnsRects()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "TpRect");
            using var tp = page.GetTextPage();
            var rects = tp.search_rects("TpRect");
            Assert.NotEmpty(rects);
        }

        [Fact]
        public void Document_GetPageText()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "PageText");
            string text = doc.GetPageText(0);
            Assert.Contains("PageText", text);
        }
    }
}
