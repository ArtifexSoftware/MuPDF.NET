namespace PDF4LLM.Ocr
{
    /// <summary>
    /// Package exports aligned with PDF4LLM.ocr.__init__ (OCRMode).
    /// </summary>
    public enum OcrMode
    {
        Never = 0,
        SelectRemovingOld = 1,
        SelectPreservingOld = 2,
        AlwaysRemovingOld = 3,
        AlwaysPreservingOld = 4,
    }
}
