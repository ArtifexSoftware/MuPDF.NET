namespace MuPDF.NET
{
    /// <summary>
    /// Legacy readthedocs constant names (<c>LINK_GOTO</c>, <c>TEXTFLAGS_TEXT</c>, etc.).
    /// </summary>
    /// <remarks>
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/glossary/vars.html"/>.
    /// Modern PascalCase members are defined in <c>Constants.cs</c>.
    /// </remarks>
    public static partial class Constants
    {
        // ─── Colorspaces ─────────────────────────────────────────────────

        /// <summary>Predefined RGB colorspace (<c>new ColorSpace(Utils.CS_RGB)</c>).</summary>
        public static readonly ColorSpace csRGB = ColorSpace.csRGB;

        /// <summary>Predefined GRAY colorspace.</summary>
        public static readonly ColorSpace csGRAY = ColorSpace.csGRAY;

        /// <summary>Predefined CMYK colorspace.</summary>
        public static readonly ColorSpace csCMYK = ColorSpace.csCMYK;

        /// <summary>RGB colorspace type id (1).</summary>
        public const int CS_RGB = CsRgb;

        /// <summary>GRAY colorspace type id (2).</summary>
        public const int CS_GRAY = CsGray;

        /// <summary>CMYK colorspace type id (3).</summary>
        public const int CS_CMYK = CsCmyk;

        /// <summary>MuPDF version tuple (legacy spelling of <see cref="MupdfVersion"/>).</summary>
        public static (int, int, int) MUPDF_VERSION => (MupdfVersion.Major, MupdfVersion.Minor, MupdfVersion.Patch);

        /// <summary>Combined MuPDF.NET / MuPDF version tuple (legacy <c>VERSION</c>).</summary>
        public static (string, string, string) VERSION =>
            (Version.MuPdfNetVersion, Version.MuPdfVersion, Version.BuildTimestamp);

        // ─── Document permissions ────────────────────────────────────────

        /// <summary>Permission to print the document.</summary>
        public const int PDF_PERM_PRINT = (int)PermissionCodes.PDF_PERM_PRINT;

        /// <summary>Permission to modify document contents.</summary>
        public const int PDF_PERM_MODIFY = (int)PermissionCodes.PDF_PERM_MODIFY;

        /// <summary>Permission to copy or extract content.</summary>
        public const int PDF_PERM_COPY = (int)PermissionCodes.PDF_PERM_COPY;

        /// <summary>Permission to add or change annotations and form fields.</summary>
        public const int PDF_PERM_ANNOTATE = (int)PermissionCodes.PDF_PERM_ANNOTATE;

        /// <summary>Permission to fill in forms and sign.</summary>
        public const int PDF_PERM_FORM = (int)PermissionCodes.PDF_PERM_FORM;

        /// <summary>Accessibility permission (obsolete in PDF 2.0; always granted).</summary>
        public const int PDF_PERM_ACCESSIBILITY = (int)PermissionCodes.PDF_PERM_ACCESSIBILITY;

        /// <summary>Permission to assemble the document (insert/rotate/delete pages).</summary>
        public const int PDF_PERM_ASSEMBLE = (int)PermissionCodes.PDF_PERM_ASSEMBLE;

        /// <summary>Permission for high-quality printing.</summary>
        public const int PDF_PERM_PRINT_HQ = (int)PermissionCodes.PDF_PERM_PRINT_HQ;

        // ─── PDF encryption ────────────────────────────────────────────

        /// <summary>Do not change encryption on save.</summary>
        public static readonly int PDF_ENCRYPT_KEEP = mupdf.mupdf.PDF_ENCRYPT_KEEP;

        /// <summary>Remove encryption on save.</summary>
        public static readonly int PDF_ENCRYPT_NONE = mupdf.mupdf.PDF_ENCRYPT_NONE;

        /// <summary>RC4 40-bit encryption.</summary>
        public static readonly int PDF_ENCRYPT_RC4_40 = mupdf.mupdf.PDF_ENCRYPT_RC4_40;

        /// <summary>RC4 128-bit encryption.</summary>
        public static readonly int PDF_ENCRYPT_RC4_128 = mupdf.mupdf.PDF_ENCRYPT_RC4_128;

        /// <summary>AES 128-bit encryption.</summary>
        public static readonly int PDF_ENCRYPT_AES_128 = mupdf.mupdf.PDF_ENCRYPT_AES_128;

        /// <summary>AES 256-bit encryption.</summary>
        public static readonly int PDF_ENCRYPT_AES_256 = mupdf.mupdf.PDF_ENCRYPT_AES_256;

        /// <summary>Unknown encryption method.</summary>
        public static readonly int PDF_ENCRYPT_UNKNOWN = mupdf.mupdf.PDF_ENCRYPT_UNKNOWN;

        // ─── Text extraction flags (bit values) ──────────────────────────

        /// <summary>Preserve ligatures in extracted text (bit 0).</summary>
        public const int TEXT_PRESERVE_LIGATURES = (int)TextFlags.TEXT_PRESERVE_LIGATURES;

        /// <summary>Preserve whitespace (bit 1).</summary>
        public const int TEXT_PRESERVE_WHITESPACE = (int)TextFlags.TEXT_PRESERVE_WHITESPACE;

        /// <summary>Include images in <see cref="TextPage"/> (bit 2).</summary>
        public const int TEXT_PRESERVE_IMAGES = (int)TextFlags.TEXT_PRESERVE_IMAGES;

        /// <summary>Do not synthesize missing space characters (bit 3).</summary>
        public const int TEXT_INHIBIT_SPACES = (int)TextFlags.TEXT_INHIBIT_SPACES;

        /// <summary>Join hyphenated lines (bit 4).</summary>
        public const int TEXT_DEHYPHENATE = (int)TextFlags.TEXT_DEHYPHENATE;

        /// <summary>One span per line in dict/json output (bit 5).</summary>
        public const int TEXT_PRESERVE_SPANS = (int)TextFlags.TEXT_PRESERVE_SPANS;

        /// <summary>Ignore text outside the mediabox (bit 6).</summary>
        public const int TEXT_MEDIABOX_CLIP = (int)TextFlags.TEXT_MEDIABOX_CLIP;

        /// <summary>Use CID codes instead of U+FFFD for unknown Unicode (bit 7).</summary>
        public const int TEXT_CID_FOR_UNKNOWN_UNICODE = (int)TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE;

        /// <summary>Default flags for plain text extraction.</summary>
        public static readonly int TEXTFLAGS_TEXT = TextFlagsText;

        /// <summary>Default flags for word extraction.</summary>
        public static readonly int TEXTFLAGS_WORDS = TextFlagsWords;

        /// <summary>Default flags for block extraction.</summary>
        public static readonly int TEXTFLAGS_BLOCKS = TextFlagsBlocks;

        /// <summary>Default flags for dict/json extraction (includes images).</summary>
        public static readonly int TEXTFLAGS_DICT = TextFlagsDict;

        /// <summary>Default flags for rawdict/rawjson extraction.</summary>
        public static readonly int TEXTFLAGS_RAWDICT = TextFlagsRawDict;

        /// <summary>Default flags for HTML extraction.</summary>
        public static readonly int TEXTFLAGS_HTML = TextFlagsHtml;

        /// <summary>Default flags for XHTML extraction.</summary>
        public static readonly int TEXTFLAGS_XHTML = TextFlagsXhtml;

        /// <summary>Default flags for XML extraction.</summary>
        public static readonly int TEXTFLAGS_XML = TextFlagsXml;

        /// <summary>Default flags for text search (includes <see cref="TEXT_DEHYPHENATE"/>).</summary>
        public static readonly int TEXTFLAGS_SEARCH = TextFlagsSearch;

        // ─── Link kinds and flags ────────────────────────────────────────

        /// <inheritdoc cref="LinkNone"/>
        public const int LINK_NONE = LinkNone;

        /// <inheritdoc cref="LinkGoto"/>
        public const int LINK_GOTO = LinkGoto;

        /// <inheritdoc cref="LinkUri"/>
        public const int LINK_URI = LinkUri;

        /// <inheritdoc cref="LinkLaunch"/>
        public const int LINK_LAUNCH = LinkLaunch;

        /// <inheritdoc cref="LinkNamed"/>
        public const int LINK_NAMED = LinkNamed;

        /// <inheritdoc cref="LinkGotor"/>
        public const int LINK_GOTOR = LinkGotor;

        /// <inheritdoc cref="LinkFlagLValid"/>
        public const int LINK_FLAG_L_VALID = LinkFlagLValid;

        /// <inheritdoc cref="LinkFlagTValid"/>
        public const int LINK_FLAG_T_VALID = LinkFlagTValid;

        /// <inheritdoc cref="LinkFlagRValid"/>
        public const int LINK_FLAG_R_VALID = LinkFlagRValid;

        /// <inheritdoc cref="LinkFlagBValid"/>
        public const int LINK_FLAG_B_VALID = LinkFlagBValid;

        /// <inheritdoc cref="LinkFlagFitH"/>
        public const int LINK_FLAG_FIT_H = LinkFlagFitH;

        /// <inheritdoc cref="LinkFlagFitV"/>
        public const int LINK_FLAG_FIT_V = LinkFlagFitV;

        /// <inheritdoc cref="LinkFlagRIsZoom"/>
        public const int LINK_FLAG_R_IS_ZOOM = LinkFlagRIsZoom;

        // ─── Text alignment ──────────────────────────────────────────────

        /// <inheritdoc cref="TextAlignLeft"/>
        public const int TEXT_ALIGN_LEFT = TextAlignLeft;

        /// <inheritdoc cref="TextAlignCenter"/>
        public const int TEXT_ALIGN_CENTER = TextAlignCenter;

        /// <inheritdoc cref="TextAlignRight"/>
        public const int TEXT_ALIGN_RIGHT = TextAlignRight;

        /// <inheritdoc cref="TextAlignJustify"/>
        public const int TEXT_ALIGN_JUSTIFY = TextAlignJustify;

        // ─── Optional content ────────────────────────────────────────────

        /// <inheritdoc cref="PdfOcOn"/>
        public const int PDF_OC_ON = PdfOcOn;

        /// <inheritdoc cref="PdfOcToggle"/>
        public const int PDF_OC_TOGGLE = PdfOcToggle;

        /// <inheritdoc cref="PdfOcOff"/>
        public const int PDF_OC_OFF = PdfOcOff;

        // ─── PDF blend modes (string names) ──────────────────────────────

        /// <inheritdoc cref="PdfBlendModeColor"/>
        public const string PDF_BM_Color = PdfBlendModeColor;

        /// <inheritdoc cref="PdfBlendModeColorBurn"/>
        public const string PDF_BM_ColorBurn = PdfBlendModeColorBurn;

        /// <inheritdoc cref="PdfBlendModeColorDodge"/>
        public const string PDF_BM_ColorDodge = PdfBlendModeColorDodge;

        /// <inheritdoc cref="PdfBlendModeDarken"/>
        public const string PDF_BM_Darken = PdfBlendModeDarken;

        /// <inheritdoc cref="PdfBlendModeDifference"/>
        public const string PDF_BM_Difference = PdfBlendModeDifference;

        /// <inheritdoc cref="PdfBlendModeExclusion"/>
        public const string PDF_BM_Exclusion = PdfBlendModeExclusion;

        /// <inheritdoc cref="PdfBlendModeHardLight"/>
        public const string PDF_BM_HardLight = PdfBlendModeHardLight;

        /// <inheritdoc cref="PdfBlendModeHue"/>
        public const string PDF_BM_Hue = PdfBlendModeHue;

        /// <inheritdoc cref="PdfBlendModeLighten"/>
        public const string PDF_BM_Lighten = PdfBlendModeLighten;

        /// <inheritdoc cref="PdfBlendModeLuminosity"/>
        public const string PDF_BM_Luminosity = PdfBlendModeLuminosity;

        /// <inheritdoc cref="PdfBlendModeMultiply"/>
        public const string PDF_BM_Multiply = PdfBlendModeMultiply;

        /// <inheritdoc cref="PdfBlendModeNormal"/>
        public const string PDF_BM_Normal = PdfBlendModeNormal;

        /// <inheritdoc cref="PdfBlendModeOverlay"/>
        public const string PDF_BM_Overlay = PdfBlendModeOverlay;

        /// <inheritdoc cref="PdfBlendModeSaturation"/>
        public const string PDF_BM_Saturation = PdfBlendModeSaturation;

        /// <inheritdoc cref="PdfBlendModeScreen"/>
        public const string PDF_BM_Screen = PdfBlendModeScreen;

        /// <inheritdoc cref="PdfBlendModeSoftLight"/>
        public const string PDF_BM_SoftLight = PdfBlendModeSoftLight;

        // ─── Annotation types ────────────────────────────────────────────

        /// <summary>Text annotation.</summary>
        public const int PDF_ANNOT_TEXT = (int)AnnotationType.Text;

        /// <summary>Link annotation.</summary>
        public const int PDF_ANNOT_LINK = (int)AnnotationType.Link;

        /// <summary>Free text annotation.</summary>
        public const int PDF_ANNOT_FREE_TEXT = (int)AnnotationType.FreeText;

        /// <summary>Line annotation.</summary>
        public const int PDF_ANNOT_LINE = (int)AnnotationType.Line;

        /// <summary>Square annotation.</summary>
        public const int PDF_ANNOT_SQUARE = (int)AnnotationType.Square;

        /// <summary>Circle annotation.</summary>
        public const int PDF_ANNOT_CIRCLE = (int)AnnotationType.Circle;

        /// <summary>Polygon annotation.</summary>
        public const int PDF_ANNOT_POLYGON = (int)AnnotationType.Polygon;

        /// <summary>Polyline annotation.</summary>
        public const int PDF_ANNOT_POLY_LINE = (int)AnnotationType.PolyLine;

        /// <summary>Highlight annotation.</summary>
        public const int PDF_ANNOT_HIGHLIGHT = (int)AnnotationType.Highlight;

        /// <summary>Underline annotation.</summary>
        public const int PDF_ANNOT_UNDERLINE = (int)AnnotationType.Underline;

        /// <summary>Squiggly underline annotation.</summary>
        public const int PDF_ANNOT_SQUIGGLY = (int)AnnotationType.Squiggly;

        /// <summary>Strike-out annotation.</summary>
        public const int PDF_ANNOT_STRIKE_OUT = (int)AnnotationType.StrikeOut;

        /// <summary>Redaction annotation.</summary>
        public const int PDF_ANNOT_REDACT = (int)AnnotationType.Redact;

        /// <summary>Stamp annotation.</summary>
        public const int PDF_ANNOT_STAMP = (int)AnnotationType.Stamp;

        /// <summary>Caret annotation.</summary>
        public const int PDF_ANNOT_CARET = (int)AnnotationType.Caret;

        /// <summary>Ink annotation.</summary>
        public const int PDF_ANNOT_INK = (int)AnnotationType.Ink;

        /// <summary>Popup annotation.</summary>
        public const int PDF_ANNOT_POPUP = (int)AnnotationType.Popup;

        /// <summary>File attachment annotation.</summary>
        public const int PDF_ANNOT_FILE_ATTACHMENT = (int)AnnotationType.FileAttachment;

        /// <summary>Sound annotation.</summary>
        public const int PDF_ANNOT_SOUND = (int)AnnotationType.Sound;

        /// <summary>Movie annotation.</summary>
        public const int PDF_ANNOT_MOVIE = (int)AnnotationType.Movie;

        /// <summary>Rich media annotation.</summary>
        public const int PDF_ANNOT_RICH_MEDIA = (int)AnnotationType.RichMedia;

        /// <summary>Widget annotation (form field).</summary>
        public const int PDF_ANNOT_WIDGET = (int)AnnotationType.Widget;

        /// <summary>Screen annotation.</summary>
        public const int PDF_ANNOT_SCREEN = (int)AnnotationType.Screen;

        /// <summary>Printer’s mark annotation.</summary>
        public const int PDF_ANNOT_PRINTER_MARK = (int)AnnotationType.PrinterMark;

        /// <summary>Trap network annotation.</summary>
        public const int PDF_ANNOT_TRAP_NET = (int)AnnotationType.TrapNet;

        /// <summary>Watermark annotation.</summary>
        public const int PDF_ANNOT_WATERMARK = (int)AnnotationType.Watermark;

        /// <summary>3D annotation.</summary>
        public const int PDF_ANNOT_3D = (int)AnnotationType.ThreeD;

        /// <summary>Projection annotation.</summary>
        public const int PDF_ANNOT_PROJECTION = (int)AnnotationType.Projection;

        /// <summary>Unknown annotation type.</summary>
        public const int PDF_ANNOT_UNKNOWN = (int)AnnotationType.Unknown;

        // ─── Annotation flag bits ────────────────────────────────────────

        /// <summary>Annotation is invisible.</summary>
        public static readonly int PDF_ANNOT_IS_INVISIBLE = mupdf.mupdf.PDF_ANNOT_IS_INVISIBLE;

        /// <summary>Annotation is hidden.</summary>
        public static readonly int PDF_ANNOT_IS_HIDDEN = mupdf.mupdf.PDF_ANNOT_IS_HIDDEN;

        /// <summary>Annotation is printed.</summary>
        public static readonly int PDF_ANNOT_IS_PRINT = mupdf.mupdf.PDF_ANNOT_IS_PRINT;

        /// <summary>Do not zoom annotation with page.</summary>
        public static readonly int PDF_ANNOT_IS_NO_ZOOM = mupdf.mupdf.PDF_ANNOT_IS_NO_ZOOM;

        /// <summary>Do not rotate annotation with page.</summary>
        public static readonly int PDF_ANNOT_IS_NO_ROTATE = mupdf.mupdf.PDF_ANNOT_IS_NO_ROTATE;

        /// <summary>Annotation is not shown on screen.</summary>
        public static readonly int PDF_ANNOT_IS_NO_VIEW = mupdf.mupdf.PDF_ANNOT_IS_NO_VIEW;

        /// <summary>Annotation is read-only.</summary>
        public static readonly int PDF_ANNOT_IS_READ_ONLY = mupdf.mupdf.PDF_ANNOT_IS_READ_ONLY;

        /// <summary>Annotation is locked.</summary>
        public static readonly int PDF_ANNOT_IS_LOCKED = mupdf.mupdf.PDF_ANNOT_IS_LOCKED;

        /// <summary>Toggle no-view when activated.</summary>
        public static readonly int PDF_ANNOT_IS_TOGGLE_NO_VIEW = mupdf.mupdf.PDF_ANNOT_IS_TOGGLE_NO_VIEW;

        /// <summary>Contents of annotation are locked.</summary>
        public static readonly int PDF_ANNOT_IS_LOCKED_CONTENTS = mupdf.mupdf.PDF_ANNOT_IS_LOCKED_CONTENTS;

        // ─── Annotation line endings ─────────────────────────────────────

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_NONE"/>
        public const int PDF_ANNOT_LE_NONE = (int)PdfLineEnding.PDF_ANNOT_LE_NONE;

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_SQUARE"/>
        public const int PDF_ANNOT_LE_SQUARE = (int)PdfLineEnding.PDF_ANNOT_LE_SQUARE;

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_CIRCLE"/>
        public const int PDF_ANNOT_LE_CIRCLE = (int)PdfLineEnding.PDF_ANNOT_LE_CIRCLE;

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_DIAMOND"/>
        public const int PDF_ANNOT_LE_DIAMOND = (int)PdfLineEnding.PDF_ANNOT_LE_DIAMOND;

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_OPEN_ARROW"/>
        public const int PDF_ANNOT_LE_OPEN_ARROW = (int)PdfLineEnding.PDF_ANNOT_LE_OPEN_ARROW;

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_CLOSED_ARROW"/>
        public const int PDF_ANNOT_LE_CLOSED_ARROW = (int)PdfLineEnding.PDF_ANNOT_LE_CLOSED_ARROW;

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_BUTT"/>
        public const int PDF_ANNOT_LE_BUTT = (int)PdfLineEnding.PDF_ANNOT_LE_BUTT;

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_R_OPEN_ARROW"/>
        public const int PDF_ANNOT_LE_R_OPEN_ARROW = (int)PdfLineEnding.PDF_ANNOT_LE_R_OPEN_ARROW;

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_R_CLOSED_ARROW"/>
        public const int PDF_ANNOT_LE_R_CLOSED_ARROW = (int)PdfLineEnding.PDF_ANNOT_LE_R_CLOSED_ARROW;

        /// <inheritdoc cref="PdfLineEnding.PDF_ANNOT_LE_SLASH"/>
        public const int PDF_ANNOT_LE_SLASH = (int)PdfLineEnding.PDF_ANNOT_LE_SLASH;

        // ─── Widget types and text formats ───────────────────────────────

        /// <inheritdoc cref="PdfWidgetType.PDF_WIDGET_TYPE_UNKNOWN"/>
        public const int PDF_WIDGET_TYPE_UNKNOWN = (int)PdfWidgetType.PDF_WIDGET_TYPE_UNKNOWN;

        /// <inheritdoc cref="PdfWidgetType.PDF_WIDGET_TYPE_BUTTON"/>
        public const int PDF_WIDGET_TYPE_BUTTON = (int)PdfWidgetType.PDF_WIDGET_TYPE_BUTTON;

        /// <inheritdoc cref="PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX"/>
        public const int PDF_WIDGET_TYPE_CHECKBOX = (int)PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX;

        /// <inheritdoc cref="PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX"/>
        public const int PDF_WIDGET_TYPE_COMBOBOX = (int)PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX;

        /// <inheritdoc cref="PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX"/>
        public const int PDF_WIDGET_TYPE_LISTBOX = (int)PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX;

        /// <inheritdoc cref="PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON"/>
        public const int PDF_WIDGET_TYPE_RADIOBUTTON = (int)PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON;

        /// <inheritdoc cref="PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE"/>
        public const int PDF_WIDGET_TYPE_SIGNATURE = (int)PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE;

        /// <inheritdoc cref="PdfWidgetType.PDF_WIDGET_TYPE_TEXT"/>
        public const int PDF_WIDGET_TYPE_TEXT = (int)PdfWidgetType.PDF_WIDGET_TYPE_TEXT;

        /// <summary>Text field with no special format.</summary>
        public const int PDF_WIDGET_TX_FORMAT_NONE = (int)mupdf.pdf_widget_tx_format.PDF_WIDGET_TX_FORMAT_NONE;

        /// <summary>Numeric text field format.</summary>
        public const int PDF_WIDGET_TX_FORMAT_NUMBER = (int)mupdf.pdf_widget_tx_format.PDF_WIDGET_TX_FORMAT_NUMBER;

        /// <summary>Special text field format.</summary>
        public const int PDF_WIDGET_TX_FORMAT_SPECIAL = (int)mupdf.pdf_widget_tx_format.PDF_WIDGET_TX_FORMAT_SPECIAL;

        /// <summary>Date text field format.</summary>
        public const int PDF_WIDGET_TX_FORMAT_DATE = (int)mupdf.pdf_widget_tx_format.PDF_WIDGET_TX_FORMAT_DATE;

        /// <summary>Time text field format.</summary>
        public const int PDF_WIDGET_TX_FORMAT_TIME = (int)mupdf.pdf_widget_tx_format.PDF_WIDGET_TX_FORMAT_TIME;

        // ─── Field flags ─────────────────────────────────────────────────

        /// <inheritdoc cref="PdfFieldFlags.PDF_FIELD_IS_READ_ONLY"/>
        public const int PDF_FIELD_IS_READ_ONLY = (int)PdfFieldFlags.PDF_FIELD_IS_READ_ONLY;

        /// <inheritdoc cref="PdfFieldFlags.PDF_FIELD_IS_REQUIRED"/>
        public const int PDF_FIELD_IS_REQUIRED = (int)PdfFieldFlags.PDF_FIELD_IS_REQUIRED;

        /// <inheritdoc cref="PdfFieldFlags.PDF_FIELD_IS_NO_EXPORT"/>
        public const int PDF_FIELD_IS_NO_EXPORT = (int)PdfFieldFlags.PDF_FIELD_IS_NO_EXPORT;

        /// <inheritdoc cref="PdfFieldFlags.PDF_TX_FIELD_IS_MULTILINE"/>
        public const int PDF_TX_FIELD_IS_MULTILINE = (int)PdfFieldFlags.PDF_TX_FIELD_IS_MULTILINE;

        /// <inheritdoc cref="PdfFieldFlags.PDF_TX_FIELD_IS_PASSWORD"/>
        public const int PDF_TX_FIELD_IS_PASSWORD = (int)PdfFieldFlags.PDF_TX_FIELD_IS_PASSWORD;

        /// <inheritdoc cref="PdfFieldFlags.PDF_TX_FIELD_IS_FILE_SELECT"/>
        public const int PDF_TX_FIELD_IS_FILE_SELECT = (int)PdfFieldFlags.PDF_TX_FIELD_IS_FILE_SELECT;

        /// <inheritdoc cref="PdfFieldFlags.PDF_TX_FIELD_IS_DO_NOT_SPELL_CHECK"/>
        public const int PDF_TX_FIELD_IS_DO_NOT_SPELL_CHECK = (int)PdfFieldFlags.PDF_TX_FIELD_IS_DO_NOT_SPELL_CHECK;

        /// <inheritdoc cref="PdfFieldFlags.PDF_TX_FIELD_IS_DO_NOT_SCROLL"/>
        public const int PDF_TX_FIELD_IS_DO_NOT_SCROLL = (int)PdfFieldFlags.PDF_TX_FIELD_IS_DO_NOT_SCROLL;

        /// <inheritdoc cref="PdfFieldFlags.PDF_TX_FIELD_IS_COMB"/>
        public const int PDF_TX_FIELD_IS_COMB = (int)PdfFieldFlags.PDF_TX_FIELD_IS_COMB;

        /// <inheritdoc cref="PdfFieldFlags.PDF_TX_FIELD_IS_RICH_TEXT"/>
        public const int PDF_TX_FIELD_IS_RICH_TEXT = (int)PdfFieldFlags.PDF_TX_FIELD_IS_RICH_TEXT;

        /// <inheritdoc cref="PdfFieldFlags.PDF_BTN_FIELD_IS_NO_TOGGLE_TO_OFF"/>
        public const int PDF_BTN_FIELD_IS_NO_TOGGLE_TO_OFF = (int)PdfFieldFlags.PDF_BTN_FIELD_IS_NO_TOGGLE_TO_OFF;

        /// <inheritdoc cref="PdfFieldFlags.PDF_BTN_FIELD_IS_RADIO"/>
        public const int PDF_BTN_FIELD_IS_RADIO = (int)PdfFieldFlags.PDF_BTN_FIELD_IS_RADIO;

        /// <inheritdoc cref="PdfFieldFlags.PDF_BTN_FIELD_IS_PUSHBUTTON"/>
        public const int PDF_BTN_FIELD_IS_PUSHBUTTON = (int)PdfFieldFlags.PDF_BTN_FIELD_IS_PUSHBUTTON;

        /// <inheritdoc cref="PdfFieldFlags.PDF_BTN_FIELD_IS_RADIOS_IN_UNISON"/>
        public const int PDF_BTN_FIELD_IS_RADIOS_IN_UNISON = (int)PdfFieldFlags.PDF_BTN_FIELD_IS_RADIOS_IN_UNISON;

        /// <inheritdoc cref="PdfFieldFlags.PDF_CH_FIELD_IS_COMBO"/>
        public const int PDF_CH_FIELD_IS_COMBO = (int)PdfFieldFlags.PDF_CH_FIELD_IS_COMBO;

        /// <inheritdoc cref="PdfFieldFlags.PDF_CH_FIELD_IS_EDIT"/>
        public const int PDF_CH_FIELD_IS_EDIT = (int)PdfFieldFlags.PDF_CH_FIELD_IS_EDIT;

        /// <inheritdoc cref="PdfFieldFlags.PDF_CH_FIELD_IS_SORT"/>
        public const int PDF_CH_FIELD_IS_SORT = (int)PdfFieldFlags.PDF_CH_FIELD_IS_SORT;

        /// <inheritdoc cref="PdfFieldFlags.PDF_CH_FIELD_IS_MULTI_SELECT"/>
        public const int PDF_CH_FIELD_IS_MULTI_SELECT = (int)PdfFieldFlags.PDF_CH_FIELD_IS_MULTI_SELECT;

        /// <inheritdoc cref="PdfFieldFlags.PDF_CH_FIELD_IS_DO_NOT_SPELL_CHECK"/>
        public const int PDF_CH_FIELD_IS_DO_NOT_SPELL_CHECK = (int)PdfFieldFlags.PDF_CH_FIELD_IS_DO_NOT_SPELL_CHECK;

        /// <inheritdoc cref="PdfFieldFlags.PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE"/>
        public const int PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE = (int)PdfFieldFlags.PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE;

        // ─── Stamp icons ─────────────────────────────────────────────────

        /// <inheritdoc cref="StampApproved"/>
        public const int STAMP_Approved = StampApproved;

        /// <inheritdoc cref="StampAsIs"/>
        public const int STAMP_AsIs = StampAsIs;

        /// <inheritdoc cref="StampConfidential"/>
        public const int STAMP_Confidential = StampConfidential;

        /// <inheritdoc cref="StampDepartmental"/>
        public const int STAMP_Departmental = StampDepartmental;

        /// <inheritdoc cref="StampExperimental"/>
        public const int STAMP_Experimental = StampExperimental;

        /// <inheritdoc cref="StampExpired"/>
        public const int STAMP_Expired = StampExpired;

        /// <inheritdoc cref="StampFinal"/>
        public const int STAMP_Final = StampFinal;

        /// <inheritdoc cref="StampForComment"/>
        public const int STAMP_ForComment = StampForComment;

        /// <inheritdoc cref="StampForPublicRelease"/>
        public const int STAMP_ForPublicRelease = StampForPublicRelease;

        /// <inheritdoc cref="StampNotApproved"/>
        public const int STAMP_NotApproved = StampNotApproved;

        /// <inheritdoc cref="StampNotForPublicRelease"/>
        public const int STAMP_NotForPublicRelease = StampNotForPublicRelease;

        /// <inheritdoc cref="StampSold"/>
        public const int STAMP_Sold = StampSold;

        /// <inheritdoc cref="StampTopSecret"/>
        public const int STAMP_TopSecret = StampTopSecret;

        /// <inheritdoc cref="StampDraft"/>
        public const int STAMP_Draft = StampDraft;

        // ─── Text font span flags ────────────────────────────────────────

        /// <inheritdoc cref="TextFontSuperscript"/>
        public const int TEXT_FONT_SUPERSCRIPT = TextFontSuperscript;

        /// <inheritdoc cref="TextFontItalic"/>
        public const int TEXT_FONT_ITALIC = TextFontItalic;

        /// <inheritdoc cref="TextFontSerifed"/>
        public const int TEXT_FONT_SERIFED = TextFontSerifed;

        /// <inheritdoc cref="TextFontMonospaced"/>
        public const int TEXT_FONT_MONOSPACED = TextFontMonospaced;

        /// <inheritdoc cref="TextFontBold"/>
        public const int TEXT_FONT_BOLD = TextFontBold;
    }
}
