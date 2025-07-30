using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.FormOMR
{
#if CORE_DEV
    public
#else
    internal
#endif
    class RawSlicer : FormOMR
    {
        public RawSlicer()
        {
            this.minRatio = 0f; //all ratios
            this.whiteZone = -1; //no white zone check
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Segment;
		}

        protected override bool CheckOutline(Segment p, float[] outline)
        {                        
            return true; //all segments are added to results
        }

        protected override bool IsFilled(Segment p)
        {
            return false; //don't check if it is filled or not
        }
    }
}
