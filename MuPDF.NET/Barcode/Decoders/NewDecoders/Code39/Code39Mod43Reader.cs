namespace BarcodeReader.Core.Code39
{
	internal class Code39Mod43Reader : Code39Reader
	{
		public Code39Mod43Reader()
		{
			CheckSumMode = Mode.CheckMod43Checksum;
		}

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Code39Mod43;
		}
	}
}
