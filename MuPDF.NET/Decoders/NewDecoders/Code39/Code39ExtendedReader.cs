namespace BarcodeReader.Core.Code39
{
	internal class Code39ExtendedReader : Code39Reader
	{
		public Code39ExtendedReader()
		{
			DoDecodeExtended = true;
		}

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Code39Ext;
		}
	}
}