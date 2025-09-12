namespace BarcodeReader.Core.Code39
{
    internal class UPUReader : Code39Reader
    {
		public UPUReader()
        {
            CheckSumMode = Mode.CheckMod11ChecksumUPU;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.UPU;
		}
    }
}