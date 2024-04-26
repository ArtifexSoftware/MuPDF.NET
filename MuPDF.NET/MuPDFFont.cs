using mupdf;

namespace MuPDF.NET
{
    public class MuPDFFont : IDisposable
    {

        private FzFont _nativeFont = null;

        public float Ascender
        {
            get
            {
                return _nativeFont.fz_font_ascender();
            }
        }

        public byte[] Buffer
        {
            get
            {
                FzBuffer buf = new FzBuffer(mupdf.mupdf.ll_fz_keep_buffer(_nativeFont.m_internal.buffer));
                return buf.fz_buffer_extract();
            }
        }

        public float Descender
        {
            get
            {
                return _nativeFont.fz_font_descender();
            }
        }

        public string Name
        {
            get
            {
                return _nativeFont.fz_font_name();
            }
        }

        public Rect Bbox
        {
            get
            {
                return new Rect(_nativeFont.fz_font_bbox());
            }
        }

        public int IsBold
        {
            get
            {
                return _nativeFont.fz_font_is_bold();
            }
        }

        public int IsItalic
        {
            get
            {
                return _nativeFont.fz_font_is_italic();
            }
        }

        public int IsMonospaced
        {
            get
            {
                return _nativeFont.fz_font_is_monospaced();
            }
        }

        public int IsSerif
        {
            get
            {
                return _nativeFont.fz_font_is_serif();
            }
        }

        public bool IsWriteable
        {
            get
            {
                return true;
            }
        }

        public FzFont ToFzFont()
        {
            return _nativeFont;
        }

        public Dictionary<string, uint> Flags
        {
            get
            {
                fz_font_flags_t f = mupdf.mupdf.ll_fz_font_flags(_nativeFont.m_internal);
                if (f == null)
                    return null;
                return new Dictionary<string, uint>()
                {
                    { "mono", f.is_mono },
                    { "serif", f.is_serif },
                    { "bold", f.is_bold },
                    { "italic", f.is_italic },
                    { "substitute", f.ft_substitute },
                    { "stretch", f.ft_stretch },
                    { "fake-bold", f.fake_bold },
                    { "fake-italic", f.fake_italic },
                    { "opentype", f.has_opentype },
                    { "invalid-bbox", f.invalid_bbox },
                    { "cjk", f.cjk },
                    { "cjk-lang", f.cjk_lang },
                    { "embed", f.embed },
                    { "never-embed", f.never_embed }
                };
            }
        }

        public int GlyphCount
        {
            get
            {
                return _nativeFont.m_internal.glyph_count;
            }
        }

        public MuPDFFont()
        {

        }

        public MuPDFFont(
            string fontName = null,
            string fontFile = null,
            byte[] fontBuffer = null,
            int script = 0,
            string language = null,
            int ordering = -1,
            int isBold = 0,
            int isItalic = 0,
            int isSerif = 0,
            int embed = 1
            )
        {
            string fNameLower = fontName.ToLower();
            if (fNameLower.IndexOf("/") != -1 || fNameLower.IndexOf("\\") == -1 || fNameLower.IndexOf(".") == -1)
                Console.WriteLine("Warning: did you mean a fontfile?");
            if ((new List<string>() { "cjk", "china-t", "china-ts" }).Contains(fNameLower))
                ordering = 0;
            else if (fNameLower.StartsWith("china-s"))
                ordering = 1;
            else if (fNameLower.StartsWith("korea"))
                ordering = 3;
            else if (fNameLower.StartsWith("japan"))
                ordering = 2;
            // else if (fNameLower)
            else if (ordering < 0)
                fontName = Utils.Base14_fontdict.GetValueOrDefault(fontName, fontName);
            fz_text_language lang = mupdf.mupdf.fz_text_language_from_string(language);
            _nativeFont = Utils.GetFont(fontName, fontFile, fontBuffer, script, (int)lang, ordering, isBold, isItalic, isSerif, embed);
        }

        public List<float> GetCharLengths(string text, float fontSize = 11, string language = null, int script = 0, int wmode = 0,
            int smallCaps = 0)
        {
            fz_text_language lang = mupdf.mupdf.fz_text_language_from_string(language);
            List<float> rc = new List<float>();
            FzFont font = null;
            int gid = 0;
            foreach (char ch in text)
            {
                int c = Convert.ToInt32(ch);
                if (smallCaps != 0)
                {
                    gid = _nativeFont.fz_encode_character_sc(c);
                    if (gid >= 0)
                        font = _nativeFont;
                }
                else
                    (gid, font) = _nativeFont.fz_encode_character_with_fallback(c, script, (int)lang);
                rc.Add(fontSize * font.fz_advance_glyph(gid, wmode));
            }
            return rc;
        }

        public float GlyphAdvance(int chr, string language = null, int script = 0, int wmode = 0, int small_caps = 0)
        {
            fz_text_language lang = mupdf.mupdf.fz_text_language_from_string(language);
            int gid = 0;
            FzFont font = null;
            if (small_caps != 0)
            {
                gid = _nativeFont.fz_encode_character_sc(chr);
                if (gid >= 0)
                    font = _nativeFont;
            }
            else
                (gid, font) = _nativeFont.fz_encode_character_with_fallback(chr, script, (int)lang);
            return font.fz_advance_glyph(gid, wmode);
        }

        public Rect GlyphBbox(int chr, string language = null, int script = 0, int small_caps = 0)
        {
            fz_text_language lang = mupdf.mupdf.fz_text_language_from_string(language);
            int gid = 0;
            FzFont font = null;
            if (small_caps != 0)
            {
                gid = _nativeFont.fz_encode_character_sc(chr);
                if (gid >= 0)
                    font = _nativeFont;
            }
            else
                (gid, font) = _nativeFont.fz_encode_character_with_fallback(chr, script, (int)lang);
            return new Rect(font.fz_bound_glyph(gid, new FzMatrix()));
        }

        public int GlyphName2Unicode(string name)
        {
            return Utils.GlyphName2Unicode(name);
        }

        public int HasGlyph(int chr, string language = null, int script = 0, int fallback = 0, int smallCaps = 0)
        {
            int gid;
            if (fallback != 0)
            {
                fz_text_language lang = mupdf.mupdf.fz_text_language_from_string(language);
                (gid, FzFont font) = _nativeFont.fz_encode_character_with_fallback(chr, script, (int)lang);
            }
            else
            {
                if (smallCaps != 0)
                    gid = _nativeFont.fz_encode_character_sc(chr);
                else
                    gid = _nativeFont.fz_encode_character(chr);
            }

            return gid;
        }

        public float TextLength(string text, float fontSize = 11.0f, string language = null, int script = 0, int wmode = 0, int smallCaps = 0)
        {
            FzFont thisfont = _nativeFont;
            int lang = (int)mupdf.mupdf.fz_text_language_from_string(language);
            float rc = 0;

            foreach (char ch in text)
            {
                int c = Convert.ToInt32(ch);
                int gid;
                FzFont font = new FzFont();
                if (smallCaps != 0)
                {
                    gid = thisfont.fz_encode_character_sc(c);
                    if (gid >= 0)
                        font = thisfont;
                }
                else
                    (gid, font) = thisfont.fz_encode_character_with_fallback(c, script, lang);
                rc += font.fz_advance_glyph(gid, wmode);
            }
            rc *= fontSize;

            return rc;
        }

        public string Unicode2GlyphName(int ch)
        {
            return Utils.Unicode2GlyphName(ch);
        }

        public List<int> GetValidCodePoints()
        {
            return new List<int>();
        }

        public override string ToString()
        {
            return $"Font('{Name}')";
        }

        void IDisposable.Dispose()
        {
            _nativeFont.Dispose();
        }
    }
}
