namespace PDF4LLM.Ocr
{
    /// <summary>Runtime probe for RapidOCR (RapidOcrNet on .NET 8+).</summary>
    public static class RapidOcrSupport
    {
        /// <summary>True when RapidOcrNet initialized successfully on this runtime.</summary>
        public static bool IsAvailable
        {
            get
            {
#if NET8_0_OR_GREATER
                return RapidOcrEngine.IsAvailable;
#else
                return false;
#endif
            }
        }
    }
}
