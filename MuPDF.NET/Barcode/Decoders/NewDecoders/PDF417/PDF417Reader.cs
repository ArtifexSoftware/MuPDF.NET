namespace BarcodeReader.Core.PDF417
{
#if CORE_DEV
    public
#else
    internal
#endif
    partial class PDF417Reader : SymbologyReader2D
    {
        int width, height;
        
		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.PDF417;
		}

        protected override FoundBarcode[] DecodeBarcode()
        {
            this.width = BWImage.Width;
            this.height = BWImage.Height;

            return Scan();
        }
    }
}
