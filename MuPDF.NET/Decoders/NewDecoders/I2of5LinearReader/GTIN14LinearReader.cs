namespace BarcodeReader.Core.I2of5LinearReader
{
    /// <summary>
    /// GTIN14 reader.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class GTIN14LinearReader : I2of5LinearReader
    {
        public GTIN14LinearReader()
        {
            SubMode = I2OF5SubMode.GTIN14;
        }
    }
}