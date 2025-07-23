namespace BarcodeReader.Core.Common
{
#if CORE_DEV
    public
#else
    internal
#endif
    interface IDecoderAscDescBars
    {
        string Decode(bool[][] samples, out float confidence);
    }
}
