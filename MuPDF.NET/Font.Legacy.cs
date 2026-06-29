using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Legacy MuPDF.NET API surface for <see cref="Font"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Restores names and signatures from the original MuPDF.NET wrapper documented at
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Font.html"/>.
    /// Required for <see cref="TextWriter"/> and <see cref="Page.WriteText"/>.
    /// </para>
    /// <para>
    /// Modern PyMuPDF-aligned members live on the main <c>Font.cs</c> partial
    /// (<see cref="BBox"/>, <see cref="GlyphAdvance"/>, <see cref="CharLengths"/>, etc.).
    /// </para>
    /// </remarks>
    public partial class Font
    {
        /// <summary>
        /// Whether the native font handle is missing or this wrapper has been disposed.
        /// </summary>
        /// <value><c>true</c> if there is no valid underlying <c>fz_font</c>.</value>
        public bool IsNull => _disposed || _this?.m_internal == null;

        /// <summary>
        /// Font bounding box.
        /// </summary>
        /// <remarks>Legacy property name <c>Bbox</c> (modern spelling <see cref="BBox"/>).</remarks>
        /// <value>Rectangle containing all glyphs at unit size.</value>
        public Rect Bbox => BBox;

        /// <summary>
        /// Collection of font properties as a dictionary of flag names to 0/1 values.
        /// </summary>
        /// <remarks>
        /// Keys include <c>mono</c>, <c>serif</c>, <c>bold</c>, <c>italic</c>,
        /// <c>substitute</c>, <c>embed</c>, etc. Same data as <see cref="FlagDictionary"/>.
        /// For a combined bitmask use <see cref="FlagMask"/>.
        /// </remarks>
        /// <returns>A mutable copy of the flag dictionary, or <c>null</c> if unavailable.</returns>
        public Dictionary<string, uint> Flags
        {
            get
            {
                var flags = FlagDictionary;
                if (flags == null)
                    return null;
                if (flags is Dictionary<string, uint> dict)
                    return dict;
                var copy = new Dictionary<string, uint>();
                foreach (var kv in flags)
                    copy[kv.Key] = kv.Value;
                return copy;
            }
        }

        /// <summary>
        /// Whether this font can be embedded when writing PDF text.
        /// </summary>
        /// <remarks>
        /// Legacy spelling <c>IsWriteable</c> (modern <see cref="IsWritable"/>).
        /// Returns <c>false</c> for Type3 fonts, substitute fonts, or when PDF writing
        /// is not supported for this face.
        /// </remarks>
        public bool IsWriteable => IsWritable;

        /// <summary>
        /// Access the underlying MuPDF <see cref="mupdf.FzFont"/> handle.
        /// </summary>
        /// <returns>The native font object used by MuPDF.</returns>
        /// <remarks>Legacy MuPDF.NET method; same handle as <see cref="NativeFont"/>.</remarks>
        public mupdf.FzFont ToFzFont() => NativeFont;

        /// <summary>
        /// Per-character advance widths for a string at a given font size.
        /// </summary>
        /// <param name="text">Text string (UTF-16).</param>
        /// <param name="fontSize">Font size in points (default 11).</param>
        /// <param name="language">Optional language tag for shaping (e.g. <c>"en"</c>).</param>
        /// <param name="script">UCDN script identifier (default 0).</param>
        /// <param name="wmode">Writing mode: <c>0</c> horizontal, <c>1</c> vertical.</param>
        /// <param name="smallCaps">Non-zero to use small-caps glyph mapping.</param>
        /// <returns>
        /// List of character widths in points (same order as <paramref name="text"/>).
        /// </returns>
        /// <remarks>Legacy name <c>GetCharLengths</c>; modern <see cref="CharLengths"/>.</remarks>
        public List<float> GetCharLengths(
            string text,
            float fontSize = 11,
            string language = null,
            int script = 0,
            int wmode = 0,
            int smallCaps = 0)
        {
            return new List<float>(CharLengths(text, fontSize, language, script, wmode, smallCaps));
        }

        /// <summary>
        /// Map a PostScript glyph name to a Unicode code point.
        /// </summary>
        /// <param name="name">Glyph name (e.g. <c>"afii10057"</c>).</param>
        /// <returns>Unicode scalar value, or <c>0xFFFD</c> if the name is unknown.</returns>
        /// <remarks>Legacy name <c>GlyphName2Unicode</c>; modern <see cref="GlyphNameToUnicode"/>.</remarks>
        public int GlyphName2Unicode(string name) => GlyphNameToUnicode(name);

        /// <summary>
        /// Map a Unicode code point to a PostScript glyph name.
        /// </summary>
        /// <param name="ch">Unicode code point (in Python use <c>ord(ch)</c>; pass the integer here).</param>
        /// <returns>Glyph name string for the font’s encoding.</returns>
        /// <remarks>Legacy name <c>Unicode2GlyphName</c>; modern <see cref="UnicodeToGlyphName"/>.</remarks>
        public string Unicode2GlyphName(int ch) => UnicodeToGlyphName(ch);

        /// <summary>
        /// Return all Unicode code points supported by this font’s cmap.
        /// </summary>
        /// <returns>Sorted list of distinct code points (length at most <see cref="GlyphCount"/>).</returns>
        /// <remarks>Legacy name <c>GetValidCodePoints</c>; modern <see cref="ValidCodepoints"/>.</remarks>
        public List<int> GetValidCodePoints() => ValidCodepoints();
    }
}
