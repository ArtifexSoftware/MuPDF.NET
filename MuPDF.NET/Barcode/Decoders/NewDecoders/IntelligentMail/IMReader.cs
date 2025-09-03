using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.IntelligentMail
{
    //Class to read Intelligent Mail Barcode - 
#if CORE_DEV
    public
#else
    internal
#endif
    class IMReader : TwoFourStateBarcodesReader
    {
        //Min aspect ratio Y/X for IM barcodes
        protected float imMinAspectRatio = 6f;

        //Max aspect ratio Y/X for IM barcodes
        protected float imMaxAspectRatio = 25;

        //Number of bars of this barcode
        protected int NBars = 65;

	    public override SymbologyType GetBarCodeType()
	    {
		    return SymbologyType.IntelligentMail;
	    }

	    protected override bool IsFourState()
        {
            return true;
        }

        protected override bool CheckNBars(int nBars)
        {
            return nBars == NBars || nBars==NBars+1; //IM always has 65 bars, but read if one more (due to noise)
        }

        protected override bool CheckRatio(float ratio)
        {
            return ratio > imMinAspectRatio && ratio < imMaxAspectRatio; //IM has width>>height
        }
        
        //returns the decoder to decode the barcode region. Overwritten in RM, KIX and postCode
        protected override IDecoderAscDescBars GetDecoder()
        {
            return new IMDecoder();
        }
    }
}
