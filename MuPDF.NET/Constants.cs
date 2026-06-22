using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Module-level constants for MuPDF.NET (PyMuPDF <c>src/__init__.py</c> and legacy glossary).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Modern names use PascalCase (<see cref="LinkGoto"/>, <see cref="TextFlagsText"/>).
    /// Legacy readthedocs names (<c>LINK_GOTO</c>, <c>TEXTFLAGS_TEXT</c>, <c>csRGB</c>) are on
    /// the same partial class in <c>Constants.Legacy.cs</c>.
    /// </para>
    /// <para>
    /// See <see href="https://mupdfnet.readthedocs.io/en/latest/glossary/vars.html"/>.
    /// </para>
    /// </remarks>
    public static partial class Constants
    {
        // ─── Version information ─────────────────────────────────────────

        /// <summary>
        /// MuPDF library version as <c>(major, minor, patch)</c> (legacy <c>MUPDF_VERSION</c>).
        /// </summary>
        public static readonly (int Major, int Minor, int Patch) MupdfVersion =
            ParseVersionTuple(Artifex.Versions.MuPdf);

        /// <summary>
        /// Combined version info matching PyMuPDF <c>version</c>:
        /// (PyMuPDF, native MuPDF, build timestamp).
        /// </summary>
        public static readonly (string PyMuPdfVersion, string MuPdfVersion, string BuildTimestamp) Version =
            (Artifex.Versions.PyMuPDF, Artifex.Versions.MuPdf, null);

        /// <summary>Small value used for floating-point comparisons.</summary>
        public const float Epsilon = 1e-5f;

        /// <summary>Alias of <see cref="Epsilon"/> (PyMuPDF <c>FLT_EPSILON</c> usage).</summary>
        public const float FltEpsilon = 1e-5f;

        /// <summary>MuPDF minimum infinite rectangle coordinate.</summary>
        public const int FzMinInfRect = unchecked((int)0x80000000);

        /// <summary>MuPDF maximum infinite rectangle coordinate.</summary>
        public const int FzMaxInfRect = 0x7fffff80;

        // ─── Link kinds (PyMuPDF LINK_*) ───────────────────────────────

        /// <summary>No link action (<c>LINK_NONE</c>).</summary>
        public const int LinkNone = (int)LinkType.None;

        /// <summary>Go to destination in document (<c>LINK_GOTO</c>).</summary>
        public const int LinkGoto = (int)LinkType.Goto;

        /// <summary>URI link (<c>LINK_URI</c>).</summary>
        public const int LinkUri = (int)LinkType.Uri;

        /// <summary>Launch file (<c>LINK_LAUNCH</c>).</summary>
        public const int LinkLaunch = (int)LinkType.Launch;

        /// <summary>Named action (<c>LINK_NAMED</c>).</summary>
        public const int LinkNamed = (int)LinkType.Named;

        /// <summary>Go to destination in another file (<c>LINK_GOTOR</c>).</summary>
        public const int LinkGotor = (int)LinkType.Gotor;

        // ─── Link flags (PyMuPDF LINK_FLAG_*) ───────────────────────────

        /// <summary>Left coordinate valid (<c>LINK_FLAG_L_VALID</c>).</summary>
        public const int LinkFlagLValid = (int)LinkFlags.LValid;

        /// <summary>Top coordinate valid (<c>LINK_FLAG_T_VALID</c>).</summary>
        public const int LinkFlagTValid = (int)LinkFlags.TValid;

        /// <summary>Right coordinate valid (<c>LINK_FLAG_R_VALID</c>).</summary>
        public const int LinkFlagRValid = (int)LinkFlags.RValid;

        /// <summary>Bottom coordinate valid (<c>LINK_FLAG_B_VALID</c>).</summary>
        public const int LinkFlagBValid = (int)LinkFlags.BValid;

        /// <summary>Fit horizontally (<c>LINK_FLAG_FIT_H</c>).</summary>
        public const int LinkFlagFitH = (int)LinkFlags.FitH;

        /// <summary>Fit vertically (<c>LINK_FLAG_FIT_V</c>).</summary>
        public const int LinkFlagFitV = (int)LinkFlags.FitV;

        /// <summary>Right value is zoom factor (<c>LINK_FLAG_R_IS_ZOOM</c>).</summary>
        public const int LinkFlagRIsZoom = (int)LinkFlags.RIsZoom;

        // ─── Signature flags (PyMuPDF SigFlag_*) ────────────────────────

        /// <summary>Document contains signatures (<c>SigFlag_SignaturesExist</c>).</summary>
        public const int SigFlagSignaturesExist = 1;

        /// <summary>Append-only after signing (<c>SigFlag_AppendOnly</c>).</summary>
        public const int SigFlagAppendOnly = 2;

        // ─── Stamp types (PyMuPDF STAMP_*) ──────────────────────────────

        /// <summary>Stamp appearance: Approved.</summary>
        public const int StampApproved = (int)StampType.Approved;

        /// <summary>Stamp appearance: AsIs.</summary>
        public const int StampAsIs = (int)StampType.AsIs;

        /// <summary>Stamp appearance: Confidential.</summary>
        public const int StampConfidential = (int)StampType.Confidential;

        /// <summary>Stamp appearance: Departmental.</summary>
        public const int StampDepartmental = (int)StampType.Departmental;

        /// <summary>Stamp appearance: Experimental.</summary>
        public const int StampExperimental = (int)StampType.Experimental;

        /// <summary>Stamp appearance: Expired.</summary>
        public const int StampExpired = (int)StampType.Expired;

        /// <summary>Stamp appearance: Final.</summary>
        public const int StampFinal = (int)StampType.Final;

        /// <summary>Stamp appearance: ForComment.</summary>
        public const int StampForComment = (int)StampType.ForComment;

        /// <summary>Stamp appearance: ForPublicRelease.</summary>
        public const int StampForPublicRelease = (int)StampType.ForPublicRelease;

        /// <summary>Stamp appearance: NotApproved.</summary>
        public const int StampNotApproved = (int)StampType.NotApproved;

        /// <summary>Stamp appearance: NotForPublicRelease.</summary>
        public const int StampNotForPublicRelease = (int)StampType.NotForPublicRelease;

        /// <summary>Stamp appearance: Sold.</summary>
        public const int StampSold = (int)StampType.Sold;

        /// <summary>Stamp appearance: TopSecret.</summary>
        public const int StampTopSecret = (int)StampType.TopSecret;

        /// <summary>Stamp appearance: Draft.</summary>
        public const int StampDraft = (int)StampType.Draft;

        // ─── Text alignment (PyMuPDF TEXT_ALIGN_*) ──────────────────────

        /// <summary>Left-aligned text (<c>TEXT_ALIGN_LEFT</c>).</summary>
        public const int TextAlignLeft = (int)TextAlign.Left;

        /// <summary>Centered text (<c>TEXT_ALIGN_CENTER</c>).</summary>
        public const int TextAlignCenter = (int)TextAlign.Center;

        /// <summary>Right-aligned text (<c>TEXT_ALIGN_RIGHT</c>).</summary>
        public const int TextAlignRight = (int)TextAlign.Right;

        /// <summary>Justified text (<c>TEXT_ALIGN_JUSTIFY</c>).</summary>
        public const int TextAlignJustify = (int)TextAlign.Justify;

        // ─── Text font flags (PyMuPDF TEXT_FONT_*) ───────────────────────

        /// <summary>Superscript span flag (<c>TEXT_FONT_SUPERSCRIPT</c>).</summary>
        public const int TextFontSuperscript = (int)TextFontFlags.Superscript;

        /// <summary>Italic span flag (<c>TEXT_FONT_ITALIC</c>).</summary>
        public const int TextFontItalic = (int)TextFontFlags.Italic;

        /// <summary>Serifed span flag (<c>TEXT_FONT_SERIFED</c>).</summary>
        public const int TextFontSerifed = (int)TextFontFlags.Serifed;

        /// <summary>Monospaced span flag (<c>TEXT_FONT_MONOSPACED</c>).</summary>
        public const int TextFontMonospaced = (int)TextFontFlags.Monospaced;

        /// <summary>Bold span flag (<c>TEXT_FONT_BOLD</c>).</summary>
        public const int TextFontBold = (int)TextFontFlags.Bold;

        // ─── Text output kinds (PyMuPDF TEXT_OUTPUT_*) ──────────────────

        /// <summary>Plain text output (<c>TEXT_OUTPUT_TEXT</c>).</summary>
        public const int TextOutputText = (int)TextOutput.Text;

        /// <summary>HTML output (<c>TEXT_OUTPUT_HTML</c>).</summary>
        public const int TextOutputHtml = (int)TextOutput.Html;

        /// <summary>JSON output (<c>TEXT_OUTPUT_JSON</c>).</summary>
        public const int TextOutputJson = (int)TextOutput.Json;

        /// <summary>XML output (<c>TEXT_OUTPUT_XML</c>).</summary>
        public const int TextOutputXml = (int)TextOutput.Xml;

        /// <summary>XHTML output (<c>TEXT_OUTPUT_XHTML</c>).</summary>
        public const int TextOutputXhtml = (int)TextOutput.XHtml;

        // ─── Text encoding (PyMuPDF TEXT_ENCODING_*) ────────────────────

        /// <summary>Latin text encoding (<c>TEXT_ENCODING_LATIN</c>).</summary>
        public const int TextEncodingLatin = (int)TextEncoding.Latin;

        /// <summary>Greek text encoding (<c>TEXT_ENCODING_GREEK</c>).</summary>
        public const int TextEncodingGreek = (int)TextEncoding.Greek;

        /// <summary>Cyrillic text encoding (<c>TEXT_ENCODING_CYRILLIC</c>).</summary>
        public const int TextEncodingCyrillic = (int)TextEncoding.Cyrillic;

        // ─── Colorspace identifiers (PyMuPDF CS_*) ────────────────────────

        /// <summary>RGB colorspace id (<c>CS_RGB</c>).</summary>
        public const int CsRgb = (int)ColorspaceType.Rgb;

        /// <summary>GRAY colorspace id (<c>CS_GRAY</c>).</summary>
        public const int CsGray = (int)ColorspaceType.Gray;

        /// <summary>CMYK colorspace id (<c>CS_CMYK</c>).</summary>
        public const int CsCmyk = (int)ColorspaceType.Cmyk;

        // ─── PDF optional content (PyMuPDF PDF_OC_*) ─────────────────────

        /// <summary>Layer on (<c>PDF_OC_ON</c>).</summary>
        public const int PdfOcOn = (int)PdfOcMode.On;

        /// <summary>Layer toggle (<c>PDF_OC_TOGGLE</c>).</summary>
        public const int PdfOcToggle = (int)PdfOcMode.Toggle;

        /// <summary>Layer off (<c>PDF_OC_OFF</c>).</summary>
        public const int PdfOcOff = (int)PdfOcMode.Off;

        // ─── PDF blend modes (PyMuPDF PDF_BM_*) ─────────────────────────

        /// <summary>Blend mode Color.</summary>
        public const string PdfBlendModeColor = "Color";

        /// <summary>Blend mode ColorBurn.</summary>
        public const string PdfBlendModeColorBurn = "ColorBurn";

        /// <summary>Blend mode ColorDodge.</summary>
        public const string PdfBlendModeColorDodge = "ColorDodge";

        /// <summary>Blend mode Darken.</summary>
        public const string PdfBlendModeDarken = "Darken";

        /// <summary>Blend mode Difference.</summary>
        public const string PdfBlendModeDifference = "Difference";

        /// <summary>Blend mode Exclusion.</summary>
        public const string PdfBlendModeExclusion = "Exclusion";

        /// <summary>Blend mode HardLight.</summary>
        public const string PdfBlendModeHardLight = "HardLight";

        /// <summary>Blend mode Hue.</summary>
        public const string PdfBlendModeHue = "Hue";

        /// <summary>Blend mode Lighten.</summary>
        public const string PdfBlendModeLighten = "Lighten";

        /// <summary>Blend mode Luminosity.</summary>
        public const string PdfBlendModeLuminosity = "Luminosity";

        /// <summary>Blend mode Multiply.</summary>
        public const string PdfBlendModeMultiply = "Multiply";

        /// <summary>Blend mode Normal.</summary>
        public const string PdfBlendModeNormal = "Normal";

        /// <summary>Blend mode Overlay.</summary>
        public const string PdfBlendModeOverlay = "Overlay";

        /// <summary>Blend mode Saturation.</summary>
        public const string PdfBlendModeSaturation = "Saturation";

        /// <summary>Blend mode Screen.</summary>
        public const string PdfBlendModeScreen = "Screen";

        /// <summary>Blend mode SoftLight (PDF name <c>Softlight</c>).</summary>
        public const string PdfBlendModeSoftLight = "Softlight";

        // ─── Base-14 fonts ───────────────────────────────────────────────

        /// <summary>The 14 standard PDF font names available in every PDF viewer.</summary>
        public static readonly string[] Base14FontNames = new[]
        {
            "Courier",
            "Courier-Oblique",
            "Courier-Bold",
            "Courier-BoldOblique",
            "Helvetica",
            "Helvetica-Oblique",
            "Helvetica-Bold",
            "Helvetica-BoldOblique",
            "Times-Roman",
            "Times-Italic",
            "Times-Bold",
            "Times-BoldItalic",
            "Symbol",
            "ZapfDingbats"
        };

        /// <summary>
        /// Maps lower-case font names (including short aliases like <c>helv</c>)
        /// to their canonical Base-14 font name.
        /// </summary>
        public static readonly Dictionary<string, string> Base14FontDict = new Dictionary<string, string>
        {
            ["courier"] = "Courier",
            ["courier-oblique"] = "Courier-Oblique",
            ["courier-bold"] = "Courier-Bold",
            ["courier-boldoblique"] = "Courier-BoldOblique",
            ["helvetica"] = "Helvetica",
            ["helvetica-oblique"] = "Helvetica-Oblique",
            ["helvetica-bold"] = "Helvetica-Bold",
            ["helvetica-boldoblique"] = "Helvetica-BoldOblique",
            ["times-roman"] = "Times-Roman",
            ["times-italic"] = "Times-Italic",
            ["times-bold"] = "Times-Bold",
            ["times-bolditalic"] = "Times-BoldItalic",
            ["symbol"] = "Symbol",
            ["zapfdingbats"] = "ZapfDingbats",
            ["helv"] = "Helvetica",
            ["heit"] = "Helvetica-Oblique",
            ["hebo"] = "Helvetica-Bold",
            ["hebi"] = "Helvetica-BoldOblique",
            ["cour"] = "Courier",
            ["coit"] = "Courier-Oblique",
            ["cobo"] = "Courier-Bold",
            ["cobi"] = "Courier-BoldOblique",
            ["tiro"] = "Times-Roman",
            ["tibo"] = "Times-Bold",
            ["tiit"] = "Times-Italic",
            ["tibi"] = "Times-BoldItalic",
            ["symb"] = "Symbol",
            ["zadb"] = "ZapfDingbats"
        };

        /// <summary><c>src/__init__.py</c>.</summary>
        internal static readonly HashSet<char> InvalidNameChars = BuildInvalidNameChars();

        private static HashSet<char> BuildInvalidNameChars()
        {
            var chars = new HashSet<char>("()<>[]{}/%\0");
            foreach (char c in " \t\n\r\f\v")
                chars.Add(c);
            return chars;
        }

        /// <summary>CJK font names (non-serif) accepted by <see cref="Page.InsertFont"/>.</summary>
        internal static readonly string[] CjkListN = { "china-t", "china-s", "japan", "korea" };

        /// <summary>CJK font names (serif) accepted by <see cref="Page.InsertFont"/>.</summary>
        internal static readonly string[] CjkListS = { "china-ts", "china-ss", "japan-s", "korea-s" };

        // ─── Text extraction flag bundles (PyMuPDF TEXTFLAGS_*) ──────────

        /// <summary>Default flags for plain text extraction (<c>TEXTFLAGS_TEXT</c>).</summary>
        public static readonly int TextFlagsText =
            0
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
            | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

        /// <summary>Flags for block-level text extraction (<c>TEXTFLAGS_BLOCKS</c>).</summary>
        public static readonly int TextFlagsBlocks =
            0
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
            | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

        /// <summary>Flags for word-level text extraction (<c>TEXTFLAGS_WORDS</c>).</summary>
        public static readonly int TextFlagsWords =
            0
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
            | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

        /// <summary>Flags for dict-level text extraction (<c>TEXTFLAGS_DICT</c>).</summary>
        public static readonly int TextFlagsDict =
            0
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
            | mupdf.mupdf.FZ_STEXT_PRESERVE_IMAGES
            | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

        /// <summary>Same as <see cref="TextFlagsDict"/> (<c>TEXTFLAGS_RAWDICT</c>).</summary>
        public static readonly int TextFlagsRawDict = TextFlagsDict;

        /// <summary>Flags for HTML text extraction (<c>TEXTFLAGS_HTML</c>).</summary>
        public static readonly int TextFlagsHtml =
            0
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
            | mupdf.mupdf.FZ_STEXT_PRESERVE_IMAGES
            | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

        /// <summary>Flags for XHTML text extraction (<c>TEXTFLAGS_XHTML</c>).</summary>
        public static readonly int TextFlagsXhtml =
            0
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
            | mupdf.mupdf.FZ_STEXT_PRESERVE_IMAGES
            | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

        /// <summary>Flags for XML text extraction (<c>TEXTFLAGS_XML</c>).</summary>
        public static readonly int TextFlagsXml =
            0
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
            | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

        /// <summary>Flags for text search (<c>TEXTFLAGS_SEARCH</c>, includes dehyphenation).</summary>
        public static readonly int TextFlagsSearch =
            0
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
            | mupdf.mupdf.FZ_STEXT_DEHYPHENATE
            | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

        // ─── Font file extensions (extracted font buffers) ───────────────

        /// <summary>TrueType / OpenType extension for extracted fonts (<c>ttf</c>).</summary>
        public const string FontExtTtf = "ttf";

        /// <summary>PostScript ASCII font extension (<c>pfa</c>).</summary>
        public const string FontExtPfa = "pfa";

        /// <summary>Type1C / CFF compressed Type1 extension (<c>cff</c>).</summary>
        public const string FontExtCff = "cff";

        /// <summary>CID-keyed PostScript font extension (<c>cid</c>).</summary>
        public const string FontExtCid = "cid";

        /// <summary>OpenType font extension (<c>otf</c>).</summary>
        public const string FontExtOtf = "otf";

        /// <summary>Placeholder when a font cannot be extracted (Base-14, Type3, etc.).</summary>
        public const string FontExtNotAvailable = "n/a";

        // ─── Internal error messages (PyMuPDF MSG_*) ─────────────────────

        internal const string MSG_BAD_ANNOT_TYPE = "bad annot type";
        internal const string MSG_BAD_APN = "bad or missing annot AP/N";
        internal const string MSG_BAD_ARG_INK_ANNOT = "arg must be seq of seq of float pairs";
        internal const string MSG_BAD_ARG_POINTS = "bad seq of points";
        internal const string MSG_BAD_BUFFER = "bad type: 'buffer'";
        internal const string MSG_BAD_COLOR_SEQ = "bad color sequence";
        internal const string MSG_BAD_DOCUMENT = "cannot open broken document";
        internal const string MSG_BAD_FILETYPE = "bad filetype";
        internal const string MSG_BAD_LOCATION = "bad location";
        internal const string MSG_BAD_OC_CONFIG = "bad config number";
        internal const string MSG_BAD_OC_LAYER = "bad layer number";
        internal const string MSG_BAD_OC_REF = "bad 'oc' reference";
        internal const string MSG_BAD_PAGEID = "bad page id";
        internal const string MSG_BAD_PAGENO = "bad page number(s)";
        internal const string MSG_BAD_PDFROOT = "PDF has no root";
        internal const string MSG_BAD_RECT = "rect is infinite or empty";
        internal const string MSG_BAD_TEXT = "bad type: 'text'";
        internal const string MSG_BAD_XREF = "bad xref";
        internal const string MSG_COLOR_COUNT_FAILED = "color count failed";
        internal const string MSG_FILE_OR_BUFFER = "need font file or buffer";
        internal const string MSG_FONT_FAILED = "cannot create font";
        internal const string MSG_IS_NO_ANNOT = "is no annotation";
        internal const string MSG_IS_NO_IMAGE = "is no image";
        internal const string MSG_IS_NO_PDF = "is no PDF";
        internal const string MSG_IS_NO_DICT = "object is no PDF dict";
        internal const string MSG_PIX_NOALPHA = "source pixmap has no alpha";
        internal const string MSG_PIXEL_OUTSIDE = "pixel(s) outside image";

        /// <summary>Default annot /NM prefix; runtime value is <see cref="Utils.ANNOT_ID_STEM"/>.</summary>
        internal const string JM_ANNOT_ID_STEM = "fitz";

        private static (int Major, int Minor, int Patch) ParseVersionTuple(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return (0, 0, 0);
            var parts = version.Split('.', '-');
            int major = parts.Length > 0 && int.TryParse(parts[0], out int m) ? m : 0;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out int n) ? n : 0;
            int patch = parts.Length > 2 && int.TryParse(parts[2], out int p) ? p : 0;
            return (major, minor, patch);
        }
    }
}
