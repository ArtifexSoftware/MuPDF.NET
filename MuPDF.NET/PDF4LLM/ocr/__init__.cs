using System;
using System.ComponentModel;

namespace MuPDF.NET.PDF4LLM.Ocr
{
    /// <summary>OCR execution modes for page analysis.</summary>
    public enum OcrMode
    {
        /// <summary>Never run OCR.</summary>
        [Description("Never run OCR")]
        Never = 0,

        /// <summary>OCR when needed dropping old OCR text.</summary>
        [Description("OCR when needed dropping old OCR text")]
        SelectDropOld = 1,

        /// <summary>OCR when needed and there is no old OCR text.</summary>
        [Description("OCR when needed and there is no old OCR text")]
        SelectKeepOld = 2,

        /// <summary>OCR for all pages dropping old OCR text.</summary>
        [Description("OCR for all pages dropping old OCR text")]
        ForceDropOld = 3,

        /// <summary>OCR for all pages which contain no old OCR text.</summary>
        [Description("OCR for all pages which contain no old OCR text")]
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