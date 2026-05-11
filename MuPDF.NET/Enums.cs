using System;

namespace MuPDF.NET
{
    /// <summary>
    /// PDF link action kinds.
    /// </summary>
    public enum LinkType
    {
        None = 0,
        Goto = 1,
        Uri = 2,
        Launch = 3,
        Named = 4,
        GoToR = 5
    }

    /// <summary>
    /// Bit flags describing PDF link destination rectangles and zoom.
    /// </summary>
    [Flags]
    public enum LinkFlags
    {
        LValid = 1,
        TValid = 2,
        RValid = 4,
        BValid = 8,
        FitH = 16,
        FitV = 32,
        RIsZoom = 64
    }

    /// <summary>
    /// Text alignment modes.
    /// </summary>
    public enum TextAlign
    {
        Left = 0,
        Center = 1,
        Right = 2,
        Justify = 3
    }

    /// <summary>
    /// Font style flags for extracted text spans.
    /// </summary>
    [Flags]
    public enum TextFontFlags
    {
        Superscript = 1,
        Italic = 2,
        Serifed = 4,
        Monospaced = 8,
        Bold = 16
    }

    /// <summary>
    /// Text extraction output formats.
    /// </summary>
    public enum TextOutput
    {
        Text = 0,
        Html = 1,
        Json = 2,
        Xml = 3,
        XHtml = 4
    }

    /// <summary>
    /// Text encoding for extraction.
    /// </summary>
    public enum TextEncoding
    {
        Latin = 0,
        Greek = 1,
        Cyrillic = 2
    }

    /// <summary>
    /// Built-in PDF device colorspace kinds.
    /// </summary>
    public enum ColorspaceType
    {
        RGB = 1,
        GRAY = 2,
        CMYK = 3
    }

    /// <summary>
    /// PDF stamp annotation appearance types.
    /// </summary>
    public enum StampType
    {
        Approved = 0,
        AsIs = 1,
        Confidential = 2,
        Departmental = 3,
        Experimental = 4,
        Expired = 5,
        Final = 6,
        ForComment = 7,
        ForPublicRelease = 8,
        NotApproved = 9,
        NotForPublicRelease = 10,
        Sold = 11,
        TopSecret = 12,
        Draft = 13
    }

    /// <summary>
    /// Optional content (layers) visibility modes.
    /// </summary>
    public enum PdfOcMode
    {
        On = 0,
        Toggle = 1,
        Off = 2
    }

    /// <summary>
    /// PDF annotation subtypes.
    /// </summary>
    public enum AnnotationType
    {
        Text = 0,
        Link = 1,
        FreeText = 2,
        Line = 3,
        Square = 4,
        Circle = 5,
        Polygon = 6,
        PolyLine = 7,
        Highlight = 8,
        Underline = 9,
        Squiggly = 10,
        StrikeOut = 11,
        Redact = 12,
        Stamp = 13,
        Caret = 14,
        Ink = 15,
        Popup = 16,
        FileAttachment = 17,
        Sound = 18,
        Movie = 19,
        RichMedia = 20,
        Widget = 21,
        Screen = 22,
        PrinterMark = 23,
        TrapNet = 24,
        Watermark = 25,
        ThreeD = 26,
        Projection = 27,
        Unknown = -1
    }

    /// <summary>
    /// PDF form field widget types.
    /// </summary>
    public enum WidgetType
    {
        Unknown = 0,
        Button = 1,
        CheckBox = 2,
        ComboBox = 3,
        ListBox = 4,
        RadioButton = 5,
        Signature = 6,
        Text = 7
    }
}
