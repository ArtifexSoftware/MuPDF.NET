namespace BarcodeReader.Core.Code39
{
    internal class PZNReader : Code39Reader
    {
        public PZNReader()
        {
            CheckSumMode = Mode.CheckMod11ChecksumPZN8;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.PZN;
		}
    }
}