using System;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Font class.
    /// Ported from tests/test_font.py.
    /// </summary>
    public class FontTests
    {
        // ─── Construction ───────────────────────────────────────────────

        [Fact]
        public void Font_DefaultHelvetica()
        {
            using var font = new Font("helv");
            Assert.Contains("Helvetica", font.Name, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Font_Courier()
        {
            using var font = new Font("cour");
            Assert.True(font.IsMonospaced);
        }

        [Fact]
        public void Font_TimesRoman()
        {
            using var font = new Font("tiro");
            Assert.True(font.IsSerif);
        }

        // ─── Properties ─────────────────────────────────────────────────

        [Fact]
        public void Font_Name()
        {
            using var font = new Font("helv");
            Assert.False(string.IsNullOrEmpty(font.Name));
        }

        [Fact]
        public void Font_GlyphCount()
        {
            using var font = new Font("helv");
            Assert.True(font.GlyphCount > 0);
        }

        [Fact]
        public void Font_BBox()
        {
            using var font = new Font("helv");
            var bbox = font.BBox;
            Assert.True(bbox.Width > 0);
            Assert.True(bbox.Height > 0);
        }

        [Fact]
        public void Font_Ascender()
        {
            using var font = new Font("helv");
            Assert.True(font.Ascender > 0);
        }

        [Fact]
        public void Font_Descender()
        {
            using var font = new Font("helv");
            Assert.True(font.Descender < 0);
        }

        [Fact]
        public void Font_IsBold()
        {
            using var font = new Font("helv");
            Assert.False(font.IsBold);
        }

        [Fact]
        public void Font_IsItalic()
        {
            using var font = new Font("helv");
            Assert.False(font.IsItalic);
        }

        [Fact]
        public void Font_FontBuffer()
        {
            using var font = new Font("helv");
            var buffer = font.FontBuffer;
            Assert.NotNull(buffer);
            Assert.True(buffer.Length > 0);
        }

        // ─── Glyph operations ───────────────────────────────────────────

        [Fact]
        public void Font_HasGlyph()
        {
            using var font = new Font("helv");
            Assert.True(font.HasGlyph('A'));
        }

        [Fact]
        public void Font_GlyphAdvance()
        {
            using var font = new Font("helv");
            float advance = font.GlyphAdvance('A');
            Assert.True(advance > 0);
        }

        [Fact]
        public void Font_GlyphBbox()
        {
            using var font = new Font("helv");
            var bbox = font.GlyphBbox('A');
            Assert.True(bbox.Width > 0);
        }

        [Fact]
        public void Font_GlyphName()
        {
            using var font = new Font("helv");
            string name = font.GlyphName('A');
            Assert.False(string.IsNullOrEmpty(name));
        }

        [Fact]
        public void Font_TextLength()
        {
            using var font = new Font("helv");
            float length = font.TextLength("Hello World", fontsize: 11);
            Assert.True(length > 0);
        }

        [Fact]
        public void Font_CharToGid()
        {
            using var font = new Font("helv");
            int gid = font.CharToGid('A');
            Assert.True(gid >= 0);
        }

        [Fact]
        public void Font_CharLengths()
        {
            using var font = new Font("helv");
            var lengths = font.CharLengths("ABC", fontsize: 11);
            Assert.Equal(3, lengths.Length);
            Assert.True(lengths.All(l => l > 0));
        }

        [Fact]
        public void Font_ValidCodepoints()
        {
            using var font = new Font("helv");
            var cps = font.ValidCodepoints();
            Assert.NotNull(cps);
            Assert.True(cps.Count > 0);
        }

        // ─── Dispose ────────────────────────────────────────────────────

        [Fact]
        public void Font_Dispose()
        {
            var font = new Font("helv");
            font.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = font.Name);
        }

        [Fact]
        public void Font_ToString()
        {
            using var font = new Font("helv");
            Assert.Contains("Font", font.ToString());
        }
    }
}
