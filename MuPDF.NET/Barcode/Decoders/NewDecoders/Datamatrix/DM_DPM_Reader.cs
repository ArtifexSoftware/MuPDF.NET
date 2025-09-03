namespace BarcodeReader.Core.Datamatrix
{
    /// <summary>
    /// Reads Datamatrix DPM ("dotted") barcodes
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class DM_DPM_Reader : DMReader
    {
        public DM_DPM_Reader()
        {
            this.ThresholdFilterMethodToUse = ThresholdFilterMethod.BlockSmoothed;
        }
    }
}