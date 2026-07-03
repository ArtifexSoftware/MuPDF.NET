using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Font wrapper for metrics, encoding, and embedding.
    /// </summary>
    public partial class Font : IDisposable
    {
        private static readonly object s_cacheLock = new object();
        private static readonly Dictionary<string, Font> s_writableCache =
            new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);

        private mupdf.FzFont _this;
        private bool _disposed;

        internal mupdf.FzFont NativeFont => NativeFontCore;

        private mupdf.FzFont NativeFontCore
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Font));
                return _this;
            }
        }

        /// <summary>
        /// Create a font .
        /// </summary>
        /// <remarks>
        /// Parameterless <c>new Font()</c> loads the default fallback font (typically
        /// <c>Noto Serif Regular</c> when Noto fonts are available), matching MuPDF.
        /// </remarks>
        /// <param name="fontName">Base-14 name, CJK name, etc.</param>
        /// <param name="fontFile">Path to a font file.</param>
        /// <param name="fontBuffer">Font binary (bytes).</param>
        /// <param name="script">UCDN script id.</param>
        /// <param name="language">Language tag for shaping.</param>
        /// <param name="ordering">CJK ordering; derived from <paramref name="fontName"/> when negative.</param>
        /// <param name="isBold">Non-zero for bold.</param>
        /// <param name="isItalic">Non-zero for italic.</param>
        /// <param name="isSerif">Non-zero for serif.</param>
        /// <param name="embed">Non-zero to embed when writing PDF.</param>
        public Font(
            string fontName = null,
            string fontFile = null,
            byte[] fontBuffer = null,
            int script = 0,
            string language = null,
            int ordering = -1,
            int isBold = 0,
            int isItalic = 0,
            int isSerif = 0,
            int embed = 1)
        {
            if (fontBuffer != null && fontBuffer.Length == 0)
                fontBuffer = null;

            if (fontName != null)
            {
                string fname_lower = fontName.ToLowerInvariant();
                if (fname_lower.IndexOf('/') >= 0 || fname_lower.IndexOf('\\') >= 0 || fname_lower.IndexOf('.') >= 0)
                    Helpers.message("Warning: did you mean a fontfile?");

                if (fname_lower == "cjk" || fname_lower == "china-t" || fname_lower == "china-ts")
                    ordering = 0;
                else if (fname_lower.StartsWith("china-s"))
                    ordering = 1;
                else if (fname_lower.StartsWith("korea"))
                    ordering = 3;
                else if (fname_lower.StartsWith("japan"))
                    ordering = 2;
                //     fontname = None
                else if (ordering < 0)
                {
                    if (Constants.Base14FontDict.TryGetValue(fname_lower, out string? resolved))
                        fontName = resolved;
                }
            }

            int lang = (int)mupdf.mupdf.fz_text_language_from_string(language);
            _this = Helpers.JM_get_font(
                fontName, fontFile, fontBuffer, script, lang, ordering,
                isBold, isItalic, isSerif, embed);
        }

        /// <summary>
        /// Legacy MuPDF.NET constructor: <c>new Font("cour", isBold: 1)</c>.
        /// </summary>
        public Font(string fontName, int isBold)
            : this(fontName, null, null, 0, null, -1, isBold, 0, 0, 1)
        {
        }

        internal Font(mupdf.FzFont font)
        {
            _this = font ?? throw new ArgumentNullException(nameof(font));
        }

        /// <inheritdoc/>
        public override string ToString() => $"Font('{Name}')";

        /// <summary>Glyph ascender.</summary>
        public float Ascender => mupdf.mupdf.fz_font_ascender(NativeFontCore);

        /// <summary>Font bounding box.</summary>
        public Rect BBox
        {
            get
            {
                var r = NativeFontCore.fz_font_bbox();
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
        }

        /// <summary>Font binary buffer.</summary>
        public byte[] Buffer
        {
            get
            {
                var internalBuf = NativeFontCore.m_internal.buffer;
                if (internalBuf == null)
                    return null;
                using var buffer_ = new mupdf.FzBuffer(mupdf.mupdf.ll_fz_keep_buffer(internalBuf));
                return Helpers.BinFromBuffer(buffer_);
            }
        }

        /// <summary>Glyph descender.</summary>
        public float Descender => mupdf.mupdf.fz_font_descender(NativeFontCore);

        /// <summary>
        /// Font flag dictionary .
        /// </summary>
        public IReadOnlyDictionary<string, uint> FlagDictionary
        {
            get
            {
                var f = mupdf.mupdf.ll_fz_font_flags(NativeFontCore.m_internal);
                if (f == null)
                    return null;
                // cppyy bitfield path ; not used in MuPDF.NET.
                return new Dictionary<string, uint>
                {
                    ["mono"] = f.is_mono,
                    ["serif"] = f.is_serif,
                    ["bold"] = f.is_bold,
                    ["italic"] = f.is_italic,
                    ["substitute"] = f.ft_substitute,
                    ["stretch"] = f.ft_stretch,
                    ["fake-bold"] = f.fake_bold,
                    ["fake-italic"] = f.fake_italic,
                    ["opentype"] = f.has_opentype,
                    ["invalid-bbox"] = f.invalid_bbox,
                    ["cjk"] = f.cjk,
                    ["cjk-lang"] = f.cjk_lang,
                    ["embed"] = f.embed,
                    ["never-embed"] = f.never_embed,
                };
            }
        }

        /// <summary>
        /// Aggregated <see cref="TextFontFlags"/> bitmask from <see cref="FlagDictionary"/>.
        /// </summary>
        public int FlagMask
        {
            get
            {
                var f = FlagDictionary;
                if (f == null) return 0;
                int v = 0;
                if (f["bold"] != 0) v |= (int)TextFontFlags.Bold;
                if (f["italic"] != 0) v |= (int)TextFontFlags.Italic;
                if (f["mono"] != 0) v |= (int)TextFontFlags.Monospaced;
                if (f["serif"] != 0) v |= (int)TextFontFlags.Serifed;
                return v;
            }
        }

        /// <summary>Number of glyphs.</summary>
        public int GlyphCount => NativeFontCore.m_internal.glyph_count;

        /// <summary>Font name.</summary>
        public string Name => mupdf.mupdf.fz_font_name(NativeFontCore);

        /// <summary>Whether font is bold.</summary>
        public bool IsBold => mupdf.mupdf.fz_font_is_bold(NativeFontCore) != 0;

        /// <summary>Whether font is italic.</summary>
        public bool IsItalic => mupdf.mupdf.fz_font_is_italic(NativeFontCore) != 0;

        /// <summary>Whether font is monospaced.</summary>
        public bool IsMonospaced => mupdf.mupdf.fz_font_is_monospaced(NativeFontCore) != 0;

        /// <summary>Whether font is serif.</summary>
        public bool IsSerif => mupdf.mupdf.fz_font_is_serif(NativeFontCore) != 0;

        /// <summary>
        /// Whether font can be written to PDF .
        /// </summary>
        public bool IsWritable
        {
            get
            {
                mupdf.FzFont font = NativeFontCore;
                var flagStruct = mupdf.mupdf.ll_fz_font_flags(font.m_internal);
                uint ft_substitute = flagStruct.ft_substitute;

                if (mupdf.mupdf.ll_fz_font_t3_procs(font.m_internal) != null
                        || ft_substitute != 0
                        || mupdf.mupdf.pdf_font_writing_supported(font) == 0)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Per-character widths for <paramref name="text"/> .
        /// </summary>
        public float[] CharLengths(string text, float fontSize = 11, string language = null, int script = 0, int wmode = 0, int smallCaps = 0)
        {
            var rc = CharLengthsList(text, fontSize, language, script, wmode, smallCaps);
            var arr = new float[rc.Count];
            rc.CopyTo(arr, 0);
            return arr;
        }

        /// <summary>
        /// Glyph advance at font size 1 .
        /// </summary>
        public float GlyphAdvance(int chr_, string language = null, int script = 0, int wmode = 0, int smallCaps = 0)
        {
            int lang = (int)mupdf.mupdf.fz_text_language_from_string(language);
            return AdvanceGlyphForCharacter(NativeFontCore, chr_, script, lang, wmode, smallCaps);
        }

        /// <summary>
        /// Glyph bbox at font size 1 .
        /// </summary>
        public Rect GlyphBbox(int chr_, string language = null, int script = 0, int smallCaps = 0)
        {
            int lang = (int)mupdf.mupdf.fz_text_language_from_string(language);
            mupdf.FzFont thisfont = NativeFontCore;
            if (smallCaps != 0)
            {
                int gid = mupdf.mupdf.fz_encode_character_sc(thisfont, chr_);
                mupdf.FzFont font = thisfont;
                if (gid >= 0)
                    font = thisfont;
                return new Rect(mupdf.mupdf.fz_bound_glyph(font, gid, new mupdf.FzMatrix()));
            }
            using var outFont = new mupdf.FzFont();
            int gid2 = thisfont.fz_encode_character_with_fallback(chr_, script, lang, outFont);
            return new Rect(outFont.fz_bound_glyph(gid2, new mupdf.FzMatrix()));
        }

        /// <summary>
        /// Unicode for a glyph name .
        /// </summary>
        public int GlyphNameToUnicode(string name) => Utils.GlyphNameToUnicode(name);

        /// <summary>
        /// Whether font has a glyph for a unicode .
        /// </summary>
        public int HasGlyph(int chr, string language = null, int script = 0, int fallback = 0, int smallCaps = 0)
        {
            if (fallback != 0)
            {
                int lang = (int)mupdf.mupdf.fz_text_language_from_string(language);
                using var outFont = new mupdf.FzFont();
                return NativeFontCore.fz_encode_character_with_fallback(chr, script, lang, outFont);
            }
            if (smallCaps != 0)
                return mupdf.mupdf.fz_encode_character_sc(NativeFontCore, chr);
            return mupdf.mupdf.fz_encode_character(NativeFontCore, chr);
        }

        /// <summary>
        /// Total text width .
        /// </summary>
        public float TextLength(string text, float fontSize = 11, string language = null, int script = 0, int wmode = 0, int smallCaps = 0)
        {
            mupdf.FzFont thisfont = NativeFontCore;
            int lang = (int)mupdf.mupdf.fz_text_language_from_string(language);
            float rc = 0;
            if (text == null)
                throw new ValueErrorException(Constants.MSG_BAD_TEXT);
            foreach (char ch in text)
                rc += AdvanceGlyphForCharacter(thisfont, ch, script, lang, wmode, smallCaps);
            rc *= fontSize;
            return rc;
        }

        /// <summary>
        /// Glyph name for a unicode .
        /// </summary>
        public string UnicodeToGlyphName(int ch) => Utils.UnicodeToGlyphName(ch);

        /// <summary>
        /// Sorted valid Unicode codepoints .
        /// </summary>
        public List<int> ValidCodepoints()
        {
            var ucs_gids = NativeFontCore.fz_enumerate_font_cmap2();
            var ucss = new List<int>();
            foreach (mupdf.fz_font_ucs_gid i in ucs_gids)
                ucss.Add((int)i.ucs);
            var ucss_unique = new HashSet<int>(ucss);
            var ucss_unique_sorted = new List<int>(ucss_unique);
            ucss_unique_sorted.Sort();
            return ucss_unique_sorted;
        }

        /// <summary>Maps user/base names to Base-14 names (<c>Base14_fontdict</c>).</summary>
        internal static string NormalizeBase14FontName(string name)
        {
            if (name == null) return "Helvetica";
            if (Constants.Base14FontDict.TryGetValue(name.ToLowerInvariant(), out string? resolved))
                return resolved;
            return name;
        }

        /// <summary>Reusable writable font (Base-14 / standard names). Do not dispose cached instances.</summary>
        public static Font GetWritableCached(string fontName = "helv", string fontFile = null)
        {
            string key = string.IsNullOrEmpty(fontName) ? "helv" : fontName.ToLowerInvariant();
            lock (s_cacheLock)
            {
                if (s_writableCache.TryGetValue(key, out Font cached))
                    return cached;
                var font = new Font(fontName, fontFile);
                s_writableCache[key] = font;
                return font;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (s_cacheLock)
                {
                    foreach (var kv in s_writableCache)
                    {
                        if (ReferenceEquals(kv.Value, this))
                            return;
                    }
                }
                // MuPDF Font.__del__ is a no-op; do not fz_drop_font while TextWriter/PDF code may still use it.
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        // ─── MuPDF API names (internal, same assembly) ─────────────────

        internal float ascender => Ascender;
        internal Rect bbox => BBox;
        internal byte[] buffer => Buffer;
        internal float descender => Descender;
        internal Dictionary<string, uint> flags => FlagDictionary as Dictionary<string, uint>;
        internal int glyph_count => GlyphCount;
        internal bool is_bold => IsBold;
        internal bool is_italic => IsItalic;
        internal bool is_monospaced => IsMonospaced;
        internal bool is_serif => IsSerif;
        internal bool is_writable => IsWritable;
        internal string name => Name;

        internal IList<float> char_lengths(string text, float fontSize = 11, string language = null, int script = 0, int wmode = 0, int small_caps = 0)
            => CharLengthsList(text, fontSize, language, script, wmode, small_caps);

        internal float glyph_advance(int chr_, string language = null, int script = 0, int wmode = 0, int small_caps = 0)
            => GlyphAdvance(chr_, language, script, wmode, small_caps);

        internal Rect glyph_bbox(int chr_, string language = null, int script = 0, int small_caps = 0)
            => GlyphBbox(chr_, language, script, small_caps);

        internal int glyph_name_to_unicode(string name) => GlyphNameToUnicode(name);

        internal int has_glyph(int chr, string language = null, int script = 0, int fallback = 0, int small_caps = 0)
            => HasGlyph(chr, language, script, fallback, small_caps);

        internal float text_length(string text, float fontSize = 11, string language = null, int script = 0, int wmode = 0, int small_caps = 0)
            => TextLength(text, fontSize, language, script, wmode, small_caps);

        internal string unicode_to_glyph_name(int ch) => UnicodeToGlyphName(ch);

        internal List<int> valid_codepoints() => ValidCodepoints();

        private List<float> CharLengthsList(string text, float fontSize, string language, int script, int wmode, int smallCaps)
        {
            int lang = (int)mupdf.mupdf.fz_text_language_from_string(language);
            var rc = new List<float>();
            mupdf.FzFont thisfont = NativeFontCore;
            foreach (char ch in text)
            {
                float adv = AdvanceGlyphForCharacter(thisfont, ch, script, lang, wmode, smallCaps);
                rc.Add(fontSize * adv);
            }
            return rc;
        }

        /// <summary>
        /// Legacy MuPDF.NET / MuPDF pattern: keep an out-param <see cref="mupdf.FzFont"/> alive while advancing.
        /// The tuple SWIG helper frees fallback state too early and causes native AVs.
        /// </summary>
        private static float AdvanceGlyphForCharacter(
            mupdf.FzFont thisfont, int c, int script, int lang, int wmode, int smallCaps)
        {
            if (smallCaps != 0)
            {
                int gid = mupdf.mupdf.fz_encode_character_sc(thisfont, c);
                mupdf.FzFont font = thisfont;
                if (gid >= 0)
                    font = thisfont;
                return mupdf.mupdf.fz_advance_glyph(font, gid, wmode);
            }
            using var outFont = new mupdf.FzFont();
            int gid2 = thisfont.fz_encode_character_with_fallback(c, script, lang, outFont);
            return outFont.fz_advance_glyph(gid2, wmode);
        }
    }
}