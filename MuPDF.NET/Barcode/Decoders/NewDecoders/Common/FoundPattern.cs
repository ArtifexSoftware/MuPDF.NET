namespace BarcodeReader.Core.Common
{
#if CORE_DEV
    public
#else
    internal
#endif
    class FoundPattern
    {
        public int nPattern;
        public float blackCompensation, whiteCompensation;
        public float moduleLength;

        /// <summary>
        /// Length of last module
        /// </summary>
        public int lastModule;

        public FoundPattern(int nPattern, float blackCompensation, float whiteCompensation, float moduleLength)
        {
            this.nPattern = nPattern;
            this.blackCompensation = blackCompensation;
            this.whiteCompensation = whiteCompensation;
            this.moduleLength = moduleLength;
        }

        public FoundPattern(int nPattern, float blackCompensation, float whiteCompensation, float moduleLength, int lastModule) : this (nPattern, blackCompensation, whiteCompensation, moduleLength)
        {
            this.lastModule = lastModule;
        }
    }
}
