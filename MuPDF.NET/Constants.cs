using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Static constants mirroring PyMuPDF's global constant values.
    /// </summary>
    public static class Constants
    {
        /// <summary>Small value used for floating-point comparisons.</summary>
        public const double Epsilon = 1e-5;

        /// <summary>Small value used for floating-point comparisons.</summary>
        public const double FltEpsilon = 1e-5;

        /// <summary>MuPDF internal minimum infinite rectangle coordinate.</summary>
        public const int FzMinInfRect = unchecked((int)0x80000000);

        /// <summary>MuPDF internal maximum infinite rectangle coordinate.</summary>
        public const int FzMaxInfRect = 0x7fffff80;

        // Link kinds
        public const int LINK_NONE = 0;
        public const int LINK_GOTO = 1;
        public const int LINK_URI = 2;
        public const int LINK_LAUNCH = 3;
        public const int LINK_NAMED = 4;
        public const int LINK_GOTOR = 5;

        // Link flags
        public const int LINK_FLAG_L_VALID = 1;
        public const int LINK_FLAG_T_VALID = 2;
        public const int LINK_FLAG_R_VALID = 4;
        public const int LINK_FLAG_B_VALID = 8;
        public const int LINK_FLAG_FIT_H = 16;
        public const int LINK_FLAG_FIT_V = 32;
        public const int LINK_FLAG_R_IS_ZOOM = 64;

        // Signature flags
        public const int SigFlagSignaturesExist = 1;
        public const int SigFlagAppendOnly = 2;

        // Stamp types
        public const int STAMP_Approved = 0;
        public const int STAMP_AsIs = 1;
        public const int STAMP_Confidential = 2;
        public const int STAMP_Departmental = 3;
        public const int STAMP_Experimental = 4;
        public const int STAMP_Expired = 5;
        public const int STAMP_Final = 6;
        public const int STAMP_ForComment = 7;
        public const int STAMP_ForPublicRelease = 8;
        public const int STAMP_NotApproved = 9;
        public const int STAMP_NotForPublicRelease = 10;
        public const int STAMP_Sold = 11;
        public const int STAMP_TopSecret = 12;
        public const int STAMP_Draft = 13;

        // Text alignment
        public const int TEXT_ALIGN_LEFT = 0;
        public const int TEXT_ALIGN_CENTER = 1;
        public const int TEXT_ALIGN_RIGHT = 2;
        public const int TEXT_ALIGN_JUSTIFY = 3;

        // Text font flags
        public const int TEXT_FONT_SUPERSCRIPT = 1;
        public const int TEXT_FONT_ITALIC = 2;
        public const int TEXT_FONT_SERIFED = 4;
        public const int TEXT_FONT_MONOSPACED = 8;
        public const int TEXT_FONT_BOLD = 16;

        // Text output types
        public const int TEXT_OUTPUT_TEXT = 0;
        public const int TEXT_OUTPUT_HTML = 1;
        public const int TEXT_OUTPUT_JSON = 2;
        public const int TEXT_OUTPUT_XML = 3;
        public const int TEXT_OUTPUT_XHTML = 4;

        // Text encoding
        public const int TEXT_ENCODING_LATIN = 0;
        public const int TEXT_ENCODING_GREEK = 1;
        public const int TEXT_ENCODING_CYRILLIC = 2;

        // Colorspace identifiers
        public const int CS_RGB = 1;
        public const int CS_GRAY = 2;
        public const int CS_CMYK = 3;

        // PDF Optional Content
        public const int PDF_OC_ON = 0;
        public const int PDF_OC_TOGGLE = 1;
        public const int PDF_OC_OFF = 2;

        // PDF Blend Modes
        public const string PDF_BM_Color = "Color";
        public const string PDF_BM_ColorBurn = "ColorBurn";
        public const string PDF_BM_ColorDodge = "ColorDodge";
        public const string PDF_BM_Darken = "Darken";
        public const string PDF_BM_Difference = "Difference";
        public const string PDF_BM_Exclusion = "Exclusion";
        public const string PDF_BM_HardLight = "HardLight";
        public const string PDF_BM_Hue = "Hue";
        public const string PDF_BM_Lighten = "Lighten";
        public const string PDF_BM_Luminosity = "Luminosity";
        public const string PDF_BM_Multiply = "Multiply";
        public const string PDF_BM_Normal = "Normal";
        public const string PDF_BM_Overlay = "Overlay";
        public const string PDF_BM_Saturation = "Saturation";
        public const string PDF_BM_Screen = "Screen";
        public const string PDF_BM_SoftLight = "Softlight";

        // Error messages
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
        /// Maps lower-case font names (including short aliases like "helv")
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

        internal const string JM_ANNOT_ID_STEM = "fitz";
    }
}
