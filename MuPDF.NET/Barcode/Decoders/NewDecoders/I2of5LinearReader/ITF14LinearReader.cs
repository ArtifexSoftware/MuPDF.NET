namespace BarcodeReader.Core.I2of5LinearReader
{
    /// <summary>
    /// ITF14 reader.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class ITF14LinearReader : I2of5LinearReader
    {
        public ITF14LinearReader()
        {
            SubMode = I2OF5SubMode.ITF14;
        }
    }
}