namespace BarcodeReader.Core.QR
{
#if CORE_DEV
    public
#else
    internal
#endif
    partial class QRReader : SymbologyReader2D
    {
        int width, height;
        bool mergePartialBarcodes = false;

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.QRCode;
		}

        protected override FoundBarcode[] DecodeBarcode()
        {
            this.width = BWImage.Width;
            this.height = BWImage.Height;

            return Scan();
        }

        public bool MergePartialBarcodes { get { return this.mergePartialBarcodes; } set { this.mergePartialBarcodes = value; } }
    }
}
