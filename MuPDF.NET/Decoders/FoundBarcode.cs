using BarcodeReader.Core.Common;
using SkiaSharp;
using System.Drawing;
using System.Linq;

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
            BarcodeType = source.BarcodeType;
            Color = source.Color;
            Confidence = source.Confidence;
            ParentRegion = source.ParentRegion;
        }

        public FoundBarcode(SymbologyType barcodeType, BarCodeRegion region)
        {
            BarcodeType = barcodeType;
            Polygon = new SKPoint[] { region.A, region.B, region.C, region.D, region.A };
            Color = Color.Blue;
            SKPoint[] skPoints = Polygon.Select(p => new SKPoint(p.X, p.Y)).ToArray();

            var path = new SKPath();

            if (skPoints.Length > 0)
            {
                path.MoveTo(skPoints[0]);
                for (int i = 1; i < skPoints.Length; i++)
                {
                    path.LineTo(skPoints[i]);
                }
                path.Close(); // if the polygon is closed
            }

            SKRect skRect = path.Bounds;
            Rect = Rectangle.Round(new RectangleF(skRect.Left, skRect.Top, skRect.Width, skRect.Height));

            Value = (region.Data != null ? region.Data[0].ToString() : "?");
            Confidence = region.Confidence;
            ParentRegion = region;
        }

        public string Value { get; set; }

        public int[] RawData { get; set; }

        public Rectangle Rect { get; set; }

        public SKPoint[] Polygon { get; set; }

        public SymbologyType BarcodeType { get; set; }

        public Color Color { get; set; }

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
