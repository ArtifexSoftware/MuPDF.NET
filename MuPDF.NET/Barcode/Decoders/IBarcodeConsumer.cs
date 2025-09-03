namespace BarcodeReader.Core
{
#if CORE_DEV
    public
#else
    internal
#endif
    interface IBarcodeConsumer
    {
        bool consumeBarcode(object foundBarcode);
    }
}
