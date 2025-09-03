namespace BarcodeReader.Core
{
    /// <summary>
    /// Supports multithreading
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
        interface IParallelSupporting
    {
        bool IsParallelSupported { get; }
    }
}