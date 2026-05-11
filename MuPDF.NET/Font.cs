using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a font.
    /// </summary>
    public class Font : IDisposable
    {
        private mupdf.FzFont _nativeFont;
        private bool _disposed;

        internal mupdf.FzFont NativeFont
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Font));
                return _nativeFont;
            }
        }

        // ─── Constructors ───────────────────────────────────────────────

        /// <summary>
        /// Create a Font object. Googling "PDF Base 14 Fonts" will yield information on the
        /// built-in font names. If none of fontfile, fontbuffer, fontname are given,
        /// the font "Helvetica" (helv) is used.
        /// </summary>
        public Font(string fontname = null, string fontfile = null, byte[] fontbuffer = null,
            int script = 0, string language = null, int ordering = -1, bool isBold = false,
            bool isItalic = false, bool isSerif = true)
        {
            if (fontfile != null)
            {
                _nativeFont = mupdf.mupdf.fz_new_font_from_file(null, fontfile, 0, 0);
            }
            else if (fontbuffer != null && fontbuffer.Length > 0)
            {
                var buf = Helpers.BufferFromBytes(fontbuffer);
                _nativeFont = mupdf.mupdf.fz_new_font_from_buffer(null, buf, 0, 0);
            }
            else
            {
                string fname = ResolveBuiltinFontName(fontname ?? "helv");
                _nativeFont = mupdf.mupdf.fz_new_base14_font(fname);
            }
        }

        internal Font(mupdf.FzFont font)
        {
            _nativeFont = font;
        }

        // ─── Properties ─────────────────────────────────────────────────

        /// <summary>
        /// Font name.
        /// </summary>
        public string Name => mupdf.mupdf.fz_font_name(NativeFont);

        /// <summary>
        /// Font is bold.
        /// </summary>
        public bool IsBold => mupdf.mupdf.fz_font_is_bold(NativeFont) != 0;
        /// <summary>
        /// Font is italic.
        /// </summary>
        public bool IsItalic => mupdf.mupdf.fz_font_is_italic(NativeFont) != 0;
        /// <summary>
        /// Font is monospaced.
        /// </summary>
        public bool IsMonospaced => mupdf.mupdf.fz_font_is_monospaced(NativeFont) != 0;
        /// <summary>
        /// Font is a serif font.
        /// </summary>
        public bool IsSerif => mupdf.mupdf.fz_font_is_serif(NativeFont) != 0;

        /// <summary>
        /// Number of glyphs in the font.
        /// </summary>
        public int GlyphCount => NativeFont.m_internal.glyph_count;

        /// <summary>
        /// Font bounding box.
        /// </summary>
        public Rect BBox
        {
            get
            {
                var r = mupdf.mupdf.fz_font_bbox(NativeFont);
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
        }

        /// <summary>
        /// Font flags.
        /// </summary>
        public int Flags
        {
            get
            {
                int f = 0;
                if (IsBold) f |= (int)TextFontFlags.Bold;
                if (IsItalic) f |= (int)TextFontFlags.Italic;
                if (IsMonospaced) f |= (int)TextFontFlags.Monospaced;
                if (IsSerif) f |= (int)TextFontFlags.Serifed;
                return f;
            }
        }

        /// <summary>
        /// Return the glyph ascender value.
        /// </summary>
        public float Ascender => mupdf.mupdf.fz_font_ascender(NativeFont);
        /// <summary>
        /// Return the glyph descender value.
        /// </summary>
        public float Descender => mupdf.mupdf.fz_font_descender(NativeFont);

        /// <summary>
        /// Check whether the font has a glyph for this unicode.
        /// </summary>
        public bool HasGlyph(int chr, string language = null, int script = 0, int fallback = 0)
        {
            var gid = mupdf.mupdf.fz_encode_character(NativeFont, chr);
            return gid > 0;
        }

        // ─── Measurement ────────────────────────────────────────────────

        /// <summary>
        /// Return the glyph width of a glyph id (font size 1).
        /// </summary>
        public float GlyphAdvance(int glyph, bool wmode = false)
        {
            return mupdf.mupdf.fz_advance_glyph(NativeFont, glyph, wmode ? 1 : 0);
        }

        /// <summary>
        /// Return the glyph bounding box of a glyph id (font size 1).
        /// </summary>
        public Rect GlyphBbox(int glyph)
        {
            var r = mupdf.mupdf.fz_bound_glyph(NativeFont, glyph, new mupdf.FzMatrix());
            return new Rect(r.x0, r.y0, r.x1, r.y1);
        }

        /// <summary>
        /// Return the glyph name for a unicode value.
        /// </summary>
        /// <summary>Glyph name for a Unicode code point (uses this font's encoding).</summary>
        public string GlyphName(int unicode)
        {
            int gid = mupdf.mupdf.fz_encode_character(NativeFont, unicode);
            if (gid <= 0) return "";
            return NativeFont.fz_get_glyph_name2(gid) ?? "";
        }

        /// <summary>
        /// Return the unicode for a glyph name.
        /// </summary>
        public int GlyphNameToUnicode(string name)
        {
            return mupdf.mupdf.fz_unicode_from_glyph_name(name);
        }

        /// <summary>
        /// Return the length of a unicode text string under a given fontsize.
        /// </summary>
        public float TextLength(string text, float fontsize = 11, string language = null, int script = 0, int wmode = 0, float smallCaps = 0)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            float w = 0;
            foreach (char c in text)
            {
                int gid = mupdf.mupdf.fz_encode_character(NativeFont, c);
                w += mupdf.mupdf.fz_advance_glyph(NativeFont, gid, wmode);
            }
            return w * fontsize;
        }

        /// <summary>
        /// Get glyph id for a character.
        /// </summary>
        public int CharToGid(int chr) => mupdf.mupdf.fz_encode_character(NativeFont, chr);

        /// <summary>
        /// Return list of character lengths of a string under a given fontsize.
        /// </summary>
        public float[] CharLengths(string text, float fontsize = 11, string language = null, int script = 0, int wmode = 0)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<float>();
            var result = new float[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                int gid = mupdf.mupdf.fz_encode_character(NativeFont, text[i]);
                result[i] = mupdf.mupdf.fz_advance_glyph(NativeFont, gid, wmode) * fontsize;
            }
            return result;
        }

        /// <summary>
        /// Return sorted list of valid unicode codepoints of this font.
        /// </summary>
        public List<int> ValidCodepoints()
        {
            var result = new List<int>();
            for (int cp = 0; cp < 0xFFFF; cp++)
            {
                if (mupdf.mupdf.fz_encode_character(NativeFont, cp) > 0)
                    result.Add(cp);
            }
            return result;
        }

        /// <summary>
        /// Binary font file content.
        /// </summary>
        public byte[] FontBuffer
        {
            get
            {
                var buf = NativeFont.m_internal.buffer;
                if (buf == null) return null;
                using var copy = new mupdf.FzBuffer(buf).fz_clone_buffer();
                return copy.fz_buffer_extract();
            }
        }

        // ─── Static Methods ─────────────────────────────────────────────

        /// <summary>Maps user/base names to the string expected by <c>fz_new_base14_font</c> (used by <see cref="Page.InsertFont"/>).</summary>
        internal static string NormalizeBase14FontName(string name) => ResolveBuiltinFontName(name ?? "helv");

        private static string ResolveBuiltinFontName(string name)
        {
            if (name == null) return "Helvetica";
            string lower = name.ToLower();
            if (Constants.Base14FontDict.TryGetValue(lower, out string resolved))
                return resolved;
            if (lower.Contains("helv")) return "Helvetica";
            if (lower.Contains("cour")) return "Courier";
            if (lower.Contains("times") || lower.Contains("tiro")) return "Times-Roman";
            if (lower.Contains("symb")) return "Symbol";
            if (lower.Contains("zadb")) return "ZapfDingbats";
            return "Helvetica";
        }

        // ─── IDisposable ────────────────────────────────────────────────

        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeFont?.Dispose();
                _nativeFont = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Font() { Dispose(); }

        public override string ToString() => $"Font('{Name}')";
    }
}
