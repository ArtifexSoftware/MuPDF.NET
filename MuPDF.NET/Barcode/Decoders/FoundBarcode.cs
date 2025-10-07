using SkiaSharp;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core
{
#if CORE_DEV
    public
#else
    internal
#endif
    class FoundBarcode
    {
        public FoundBarcode()
        {
        }

        public FoundBarcode(FoundBarcode source)
        {
            Value = source.Value;
            RawData = source.RawData;
            Rect = source.Rect;
            Polygon = source.Polygon;
            BarcodeFormat = source.BarcodeFormat;
            Color = source.Color;
            Confidence = source.Confidence;
            ParentRegion = source.ParentRegion;
        }

        public FoundBarcode(SymbologyType barcodeType, BarCodeRegion region)
        {
            BarcodeFormat = barcodeType;
            Polygon = new SKPointI[] { region.A, region.B, region.C, region.D, region.A };
            Color = Color.Blue;
            //byte[] pointTypes = new byte[5] { (byte)PathPointType.Start, (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line };
            //using (GraphicsPath path = new GraphicsPath(Polygon, pointTypes))
            //    Rect = Rectangle.Round(path.GetBounds());
            Rect = Utils.DrawPath(Polygon);
            Value = (region.Data != null ? region.Data[0].ToString() : "?");
            Confidence = region.Confidence;
            ParentRegion = region;
        }

        public string Value { get; set; }

        public int[] RawData { get; set; }

        public SKRect Rect { get; set; }

        public SKPointI[] Polygon { get; set; }

        public SymbologyType BarcodeFormat { get; set; }

        public SKColor Color { get; set; }

        public float Confidence { get; set; } = 1f;

        public object Tag { get; set; }

        public int StructureAppendIndex { get; set; } = -1;

        public int StructureAppendCount { get; set; } = -1;

        public BarCodeRegion ParentRegion { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}
