namespace MuPDF.NET
{
    public enum TextFlags
    {
        TEXT_PRESERVE_LIGATURES = 1,

        TEXT_PRESERVE_WHITESPACE = 2,

        TEXT_PRESERVE_IMAGES = 4,

        TEXT_INHIBIT_SPACES = 8,

        TEXT_DEHYPHENATE = 16,

        TEXT_PRESERVE_SPANS = 32,

        TEXT_MEDIABOX_CLIP = 64,

        TEXT_CID_FOR_UNKNOWN_UNICODE = 128,

        TEXT_COLLECT_STRUCTURE = 256,
        
        TEXT_ACCURATE_BBOXES = 512
    }

    public enum TextFlagsExtension
    {
        TEXTFLAGS_WORDS = (0
        | TextFlags.TEXT_PRESERVE_LIGATURES
        | TextFlags.TEXT_PRESERVE_WHITESPACE
        | TextFlags.TEXT_MEDIABOX_CLIP
        | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
        ),

        TEXTFLAGS_BLOCKS = (0
        | TextFlags.TEXT_PRESERVE_LIGATURES
        | TextFlags.TEXT_PRESERVE_WHITESPACE
        | TextFlags.TEXT_MEDIABOX_CLIP
        | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
        ),

        TEXTFLAGS_DICT = (0
        | TextFlags.TEXT_PRESERVE_LIGATURES
        | TextFlags.TEXT_PRESERVE_WHITESPACE
        | TextFlags.TEXT_MEDIABOX_CLIP
        | TextFlags.TEXT_PRESERVE_IMAGES
        | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
        ),

        TEXTFLAGS_RAWDICT = TextFlagsExtension.TEXTFLAGS_DICT,

        TEXTFLAGS_SEARCH = (0
        | TextFlags.TEXT_PRESERVE_LIGATURES
        | TextFlags.TEXT_PRESERVE_WHITESPACE
        | TextFlags.TEXT_MEDIABOX_CLIP
        | TextFlags.TEXT_DEHYPHENATE
        | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
        ),

        TEXTFLAGS_HTML = (0
        | TextFlags.TEXT_PRESERVE_LIGATURES
        | TextFlags.TEXT_PRESERVE_WHITESPACE
        | TextFlags.TEXT_MEDIABOX_CLIP
        | TextFlags.TEXT_PRESERVE_IMAGES
        | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
        ),

        TEXTFLAGS_XHTML = (0
        | TextFlags.TEXT_PRESERVE_LIGATURES
        | TextFlags.TEXT_PRESERVE_WHITESPACE
        | TextFlags.TEXT_MEDIABOX_CLIP
        | TextFlags.TEXT_PRESERVE_IMAGES
        | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
        ),

        TEXTFLAGS_XML = (0
        | TextFlags.TEXT_PRESERVE_LIGATURES
        | TextFlags.TEXT_PRESERVE_WHITESPACE
        | TextFlags.TEXT_MEDIABOX_CLIP
        | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
        ),

        TEXTFLAGS_TEXT = (0
        | TextFlags.TEXT_PRESERVE_LIGATURES
        | TextFlags.TEXT_PRESERVE_WHITESPACE
        | TextFlags.TEXT_MEDIABOX_CLIP
        | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
        )
    }
}
