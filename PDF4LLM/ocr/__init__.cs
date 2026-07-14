using System;
using System.ComponentModel;

namespace PDF4LLM.Ocr
{
    /// <summary>OCR execution modes for page analysis.</summary>
    public enum OcrMode
    {
        /// <summary>Never run OCR.</summary>
        [Description("Never run OCR")]
        Never = 0,

        /// <summary>Run OCR when needed, remove previous OCR text.</summary>
        [Description("Run OCR when needed, remove previous OCR text")]
        SelectDropOld = 1,

        /// <summary>Run OCR when needed, preserve previous OCR text.</summary>
        [Description("Run OCR when needed, preserve previous OCR text")]
        SelectKeepOld = 2,

        /// <summary>Run OCR for all pages, remove previous OCR text.</summary>
        [Description("Run OCR for all pages, remove previous OCR text")]
        ForceDropOld = 3,

        /// <summary>Run OCR for all pages, preserve previous OCR text.</summary>
        [Description("Run OCR for all pages, preserve previous OCR text")]
        ForceKeepOld = 4,

        /// <summary>Alias for <see cref="SelectDropOld"/> (pre-1.28 name).</summary>
        [Obsolete("Use SelectDropOld (OCRMode rename).")]
        SelectRemovingOld = SelectDropOld,

        /// <summary>Alias for <see cref="SelectKeepOld"/> (pre-1.28 name).</summary>
        [Obsolete("Use SelectKeepOld (OCRMode rename).")]
        SelectPreservingOld = SelectKeepOld,

        /// <summary>Alias for <see cref="ForceDropOld"/> (pre-1.28 name).</summary>
        [Obsolete("Use ForceDropOld (OCRMode rename).")]
        AlwaysRemovingOld = ForceDropOld,

        /// <summary>Alias for <see cref="ForceKeepOld"/> (pre-1.28 name).</summary>
        [Obsolete("Use ForceKeepOld (OCRMode rename).")]
        AlwaysPreservingOld = ForceKeepOld,
    }
}