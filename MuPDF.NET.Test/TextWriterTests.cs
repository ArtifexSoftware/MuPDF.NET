using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the TextWriter class.
    /// </summary>
    public class TextWriterTests
    {
        // ─── Construction ───────────────────────────────────────────────

        [Fact]
        public void TextWriter_Create()
        {
            var tw = new TextWriter(new Rect(0, 0, 595, 842));
            Assert.NotNull(tw);
            Assert.Equal(0, tw.SpanCount);
        }

        // ─── Append ─────────────────────────────────────────────────────

        [Fact]
        public void TextWriter_Append()
        {
            var tw = new TextWriter(new Rect(0, 0, 595, 842));
            tw.Append(new Point(72, 72), "Hello World");
            Assert.True(tw.SpanCount > 0);
        }

        [Fact]
        public void TextWriter_AppendMultiple()
        {
            var tw = new TextWriter(new Rect(0, 0, 595, 842));
            tw.Append(new Point(72, 72), "Line 1");
            tw.Append(new Point(72, 90), "Line 2");
            Assert.True(tw.SpanCount >= 2);
        }

        // ─── FillTextbox ────────────────────────────────────────────────

        [Fact]
        public void TextWriter_FillTextbox()
        {
            var tw = new TextWriter(new Rect(0, 0, 595, 842));
            tw.FillTextbox(
                new Rect(50, 50, 300, 200),
                "This is a longer text that should fill the textbox area."
            );
            Assert.True(tw.SpanCount > 0);
        }

        // ─── WriteText to page ──────────────────────────────────────────

        [Fact]
        public void TextWriter_WriteTextToPage()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var tw = new TextWriter(page.Rect);
            tw.Append(new Point(72, 72), "Written by TextWriter");
            tw.WriteText(page);

            string text = page.GetText("text");
            Assert.Contains("Written", text);
        }

        // ─── Properties ─────────────────────────────────────────────────

        [Fact]
        public void TextWriter_Rect()
        {
            var tw = new TextWriter(new Rect(0, 0, 595, 842));
            Assert.Equal(595, tw.Rect.Width);
            Assert.Equal(842, tw.Rect.Height);
        }

        [Fact]
        public void TextWriter_TextRect()
        {
            var tw = new TextWriter(new Rect(0, 0, 595, 842));
            tw.Append(new Point(72, 72), "Test");
            var tr = tw.TextRect;
            Assert.True(tr.Width > 0);
        }

        [Fact]
        public void TextWriter_LastPoint()
        {
            var tw = new TextWriter(new Rect(0, 0, 595, 842));
            tw.Append(new Point(72, 72), "Hello");
            var lp = tw.LastPoint;
            Assert.True(lp.X > 72);
        }

        [Fact]
        public void TextWriter_OpacityAndColor()
        {
            var tw = new TextWriter(new Rect(0, 0, 595, 842), opacity: 0.5f, color: new float[] { 1, 0, 0 });
            Assert.True(TestHelper.IsClose(0.5, tw.Opacity, 0.01));
        }

        // ─── Reset ──────────────────────────────────────────────────────

        [Fact]
        public void TextWriter_Reset()
        {
            var tw = new TextWriter(new Rect(0, 0, 595, 842));
            tw.Append(new Point(72, 72), "Some text");
            Assert.True(tw.SpanCount > 0);
            tw.Reset();
            Assert.Equal(0, tw.SpanCount);
        }
    }
}
