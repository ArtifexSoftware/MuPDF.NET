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
        SelectRemovingOld = 1,

        /// <summary>Run OCR when needed, preserve previous OCR text.</summary>
        [Description("Run OCR when needed, preserve previous OCR text")]
        SelectPreservingOld = 2,

        /// <summary>Run OCR for all pages, remove previous OCR text.</summary>
        [Description("Run OCR for all pages, remove previous OCR text")]
        AlwaysRemovingOld = 3,

        /// <summary>Run OCR for all pages, preserve previous OCR text.</summary>
        [Description("Run OCR for all pages, preserve previous OCR text")]
        AlwaysPreservingOld = 4,
    }
}
