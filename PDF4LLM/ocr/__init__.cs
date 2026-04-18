namespace PDF4LLM.Ocr
{
    /// <summary>
    /// Package exports for OCR integration (<see cref="OCRMode"/>).
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
