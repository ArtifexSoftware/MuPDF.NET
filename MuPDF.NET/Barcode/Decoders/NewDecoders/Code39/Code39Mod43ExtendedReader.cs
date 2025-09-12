namespace BarcodeReader.Core.Code39
{
	internal class Code39Mod43ExtendedReader : Code39Reader
	{
		public Code39Mod43ExtendedReader()
		{
			CheckSumMode = Mode.CheckMod43Checksum;
			DoDecodeExtended = true;
		}

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Code39Mod43Ext;
		}
	}
}
