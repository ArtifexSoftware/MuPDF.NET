using SkiaSharp;
using System;

namespace BarcodeReader.Core
{
#if CORE_DEV
    public
#else
    internal
#endif
    interface IPreparedImage : IDisposable
    {
        int Width { get; }
        int Height { get; }
        SKBitmap SKBitmap { get; }

        byte[] GetRow(int y);
        IPreparedImage Clone();
    }
}