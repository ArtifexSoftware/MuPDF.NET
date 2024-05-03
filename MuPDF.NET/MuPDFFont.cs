using mupdf;

namespace MuPDF.NET
{
    public class MuPDFFont
    {
        static MuPDFFont()
        {
            if (!File.Exists("mupdfcsharp.dll"))
                Utils.LoadEmbeddedDll();
        }

        private FzFont _nativeFont = null;

        public float Ascender
        {
            get { return _nativeFont.fz_font_ascender(); }
        }

        public byte[] Buffer
        {
            get
            {
                FzBuffer buf = new FzBuffer(
                    mupdf.mupdf.ll_fz_keep_buffer(_nativeFont.m_internal.buffer)
                );
                return buf.fz_buffer_extract();
            }
        }

        public float Descender
        {
            get { return _nativeFont.fz_font_descender(); }
        }

        public string Name
        {
            get { return _nativeFont.fz_font_name(); }
        }

        public Rect Bbox
        {
            get { return new Rect(_nativeFont.fz_font_bbox()); }
        }

        public int IsBold
        {
            get { return _nativeFont.fz_font_is_bold(); }
        }

        public int IsItalic
        {
            get { return _nativeFont.fz_font_is_italic(); }
        }

        public int IsMonospaced
        {
            get { return _nativeFont.fz_font_is_monospaced(); }
        }

        public int IsSerif
        {
            get { return _nativeFont.fz_font_is_serif(); }
        }

        public bool IsWriteable
        {
            get { return true; }
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
            get { return _nativeFont.m_internal.glyph_count; }
        }

        public MuPDFFont()
        {
            _nativeFont = new FzFont();
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
            if (
                fNameLower.IndexOf("/") != -1
                || fNameLower.IndexOf("\\") == -1
                || fNameLower.IndexOf(".") == -1
            )
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
            _nativeFont = Utils.GetFont(
                fontName,
                fontFile,
                fontBuffer,
                script,
                (int)lang,
                ordering,
                isBold,
                isItalic,
                isSerif,
                embed
            );
        }

        /// <summary>
        /// Sequence of character lengths in points of a unicode string.
        /// </summary>
        /// <param name="text">a text string, UTF-8 encoded.</param>
        /// <param name="fontSize">the fontsize.</param>
        /// <param name="language"></param>
        /// <param name="script"></param>
        /// <param name="wmode"></param>
        /// <param name="smallCaps"></param>
        /// <returns>the lengths in points of the characters of a string when stored in the PDF. It works like Font.text_length().</returns>
        public List<float> GetCharLengths(
            string text,
            float fontSize = 11,
            string language = null,
            int script = 0,
            int wmode = 0,
            int smallCaps = 0
        )
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
                    (gid, font) = _nativeFont.fz_encode_character_with_fallback(
                        c,
                        script,
                        (int)lang
                    );
                rc.Add(fontSize * font.fz_advance_glyph(gid, wmode));
            }
            return rc;
        }

        /// <summary>
        /// Calculate the “width” of the character’s glyph (visual representation).
        /// </summary>
        /// <param name="chr">the unicode number of the character. Use ord(), not the character itself. Again, this should normally work even if a character is not supported by that font, because fallback fonts will be checked where necessary.</param>
        /// <param name="language"></param>
        /// <param name="script"></param>
        /// <param name="wmode">write mode, 0 = horizontal, 1 = vertical.</param>
        /// <param name="small_caps"></param>
        /// <returns></returns>
        public float GlyphAdvance(
            int chr,
            string language = null,
            int script = 0,
            int wmode = 0,
            int small_caps = 0
        )
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

        /// <summary>
        /// The glyph rectangle relative to fontsize
        /// </summary>
        /// <param name="chr">ord() of the character.</param>
        /// <param name="language"></param>
        /// <param name="script"></param>
        /// <param name="small_caps"></param>
        /// <returns>returns rect</returns>
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

        /// <summary>
        /// Return the unicode value for a given glyph name. Use it in conjunction with chr() if you want to output e.g. a certain symbol.
        /// </summary>
        /// <param name="name">The name of the glyph.</param>
        /// <returns>The unicode integer, or 65533 = 0xFFFD if the name is unknown.</returns>
        public int GlyphName2Unicode(string name)
        {
            return Utils.GlyphName2Unicode(name);
        }

        /// <summary>
        /// Check whether the unicode chr exists in the font or (option) some fallback font. May be used to check whether any “TOFU” symbols will appear on output.
        /// </summary>
        /// <param name="chr">the unicode of the character (i.e. ord()).</param>
        /// <param name="language">the language – currently unused.</param>
        /// <param name="script">the UCDN script number.</param>
        /// <param name="fallback">perform an extended search in fallback fonts or restrict to current font (default).</param>
        /// <param name="smallCaps"></param>
        /// <returns>the glyph number. Zero indicates no glyph found.</returns>
        public int HasGlyph(
            int chr,
            string language = null,
            int script = 0,
            int fallback = 0,
            int smallCaps = 0
        )
        {
            int gid;
            if (fallback != 0)
            {
                fz_text_language lang = mupdf.mupdf.fz_text_language_from_string(language);
                (gid, FzFont font) = _nativeFont.fz_encode_character_with_fallback(
                    chr,
                    script,
                    (int)lang
                );
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

        /// <summary>
        /// Calculate the length in points of a unicode string.
        /// </summary>
        /// <param name="text">a text string, UTF-8 encoded.</param>
        /// <param name="fontSize">the fontsize.</param>
        /// <param name="language"></param>
        /// <param name="script"></param>
        /// <param name="wmode"></param>
        /// <param name="smallCaps"></param>
        /// <returns>the length of the string in points when stored in the PDF. If a character is not contained in the font, it will automatically be looked up in a fallback font.</returns>
        public float TextLength(
            string text,
            float fontSize = 11.0f,
            string language = null,
            int script = 0,
            int wmode = 0,
            int smallCaps = 0
        )
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

        /// <summary>
        /// Show the name of the character’s glyph.
        /// </summary>
        /// <param name="ch">the unicode number of the character. Use ord(), not the character itself.</param>
        /// <returns></returns>
        public string Unicode2GlyphName(int ch)
        {
            return Utils.Unicode2GlyphName(ch);
        }

        /// <summary>
        /// Return an array of unicodes supported by this font.
        /// </summary>
        /// <returns>an array.array of length at most Font.glyph_count.</returns>
        public List<int> GetValidCodePoints()
        {
            return new List<int>();
        }

        public override string ToString()
        {
            return $"Font('{Name}')";
        }
    }
}
