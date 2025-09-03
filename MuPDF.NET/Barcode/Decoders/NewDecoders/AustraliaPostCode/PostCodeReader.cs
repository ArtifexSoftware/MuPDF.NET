using BarcodeReader.Core.Common;
using BarcodeReader.Core.IntelligentMail;

namespace BarcodeReader.Core.AustraliaPostCode
{
    //The only difference between IM and PostCode reader is the length and ratio of the barcode.
    //A different decoder is also set.
#if CORE_DEV
    public
#else
    internal
#endif
    class PostCodeReader : IMReader
    {
		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.AustralianPostCode;
		}

        protected override bool CheckNBars(int nBars)
        {
            return nBars == 37 || nBars == 52 || nBars == 67;
        }

        protected override bool CheckRatio(float ratio)
        {
            return ratio > 2f && ratio < 25f;
        }

        protected override IDecoderAscDescBars GetDecoder()
        {
            return new PostCodeDecoder();
        }
    }
}
