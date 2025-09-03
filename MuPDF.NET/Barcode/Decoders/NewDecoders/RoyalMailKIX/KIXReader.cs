using BarcodeReader.Core.Common;
using BarcodeReader.Core.RoyalMail;

namespace BarcodeReader.Core.RoyalMailKIX
{
    //The only difference between KIX and RM reader is the length of the barcode. KIX is a variable length barcode
    //thus nBars must be a multiple of 4.
    //A different decoder is also set.
#if CORE_DEV
    public
#else
    internal
#endif
    class KIXReader : RMReader
    {
		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.RoyalMailKIX;
		}

        protected override bool CheckNBars(int nBars)
        {
            return nBars>18 && (nBars)%4==0; //4*N
        }

        protected override IDecoderAscDescBars GetDecoder()
        {
            return new KIXDecoder();
        }
    }
}
