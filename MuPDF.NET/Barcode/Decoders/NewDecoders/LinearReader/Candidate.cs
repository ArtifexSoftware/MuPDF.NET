namespace BarcodeReader.Core.Common
{
#if CORE_DEV
    public
#else
    internal
#endif
    class Candidate
    {
        public float ModuleEstimate;
        public int BarsCount;
        public int SumWhiteLength;
        public int SumBlackLength;
        public int LineLength;
        public int MinBarLength;
        public int MaxBarLength;

        public PatternCluster From;
        public PatternCluster To;

        public BarCodeRegion Region;

        public Candidate(PatternCluster from , PatternCluster to)
        {
            this.From = from;
            this.To = to;
        }
    }


}