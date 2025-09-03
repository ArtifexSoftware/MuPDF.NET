namespace BarcodeReader.Core.Aztec
{
#if CORE_DEV
    public
#else
    internal
#endif
    partial class AztecReader : SymbologyReader2D
    {
        //Scan image rows main step
        protected int scanRowStep = 1;

        //Max difference between the center of the finder and the center of the mid segment of the finder
        protected float finderMaxCentersDifference = 0.1f;

        //Max distance in pixels between the center of the finder and the center of the mid segment of the finder
        protected int finderMaxCentersDistanceInPixels = 2;

        int width, height;

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Aztec;
		}

		protected override FoundBarcode[] DecodeBarcode()
        {
            this.width = BWImage.Width;
            this.height = BWImage.Height;
#if DEBUG 
            //this.bmp=bwImage.GetAsBitmap(); //needed to save images during execution.
#endif
            return Scan();
        }
    }
}
