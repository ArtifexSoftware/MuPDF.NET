namespace MuPDF.NET4LLM.Ocr
{
    /// <summary>
    /// Package exports aligned with mupdf4llm.ocr.__init__ (OCRMode).
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
