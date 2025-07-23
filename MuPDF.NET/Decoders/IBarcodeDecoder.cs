using System;

namespace BarcodeReader.Core
{
    internal interface IBarcodeDecoder : IDisposable
	{
		SymbologyType GetBarCodeType();
	}
}