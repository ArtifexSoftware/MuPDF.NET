using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.PostNet
{
    //The only difference between IM and RM reader is the length and ratio of the barcode.
    //A different decoder is also set.
#if CORE_DEV
    public
#else
    internal
#endif
    class PostNetReader : TwoFourStateBarcodesReader
    {
		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.PostNet;
		}

        protected override bool IsFourState()
        {
            return false; //postnet is a two-state barcode
        }

        protected override bool CheckNBars(int nBars)
        {
            return nBars>=32 && nBars<=62 && (nBars-2)%5==0; //start + 5*N+ stop where N=6, 7, 10 or 12
        }

        protected override bool CheckRatio(float ratio)
        {
            return ratio > 2f && ratio < 25f;
        }

        protected override IDecoderAscDescBars GetDecoder()
        {
            return new PostNetDecoder();
        }
        
    }
}
