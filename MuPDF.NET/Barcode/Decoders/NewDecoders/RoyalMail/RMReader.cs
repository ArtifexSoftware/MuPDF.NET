using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.RoyalMail
{
    //The only difference between IM and RM reader is the length and ratio of the barcode.
    //A different decoder is also set.
#if CORE_DEV
    public
#else
    internal
#endif
    class RMReader : TwoFourStateBarcodesReader
    {
		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.RoyalMail;
		}

        protected override bool IsFourState()
        {
            return true;
        }

        protected override bool CheckNBars(int nBars)
        {
            return nBars>18 && (nBars-2)%4==0; //start + 4*N+ stop
        }

        protected override bool CheckRatio(float ratio)
        {
            return ratio > 2f && ratio < 25f;
        }

        protected override IDecoderAscDescBars GetDecoder()
        {
            return new RMDecoder();
        }
        
    }
}
