using SkiaSharp;
using System.Drawing;

namespace BarcodeReader.Core.Common
{
#if CORE_DEV
    public
#else
    internal
#endif
    class BoundingBox
    {
        int minX, minY, maxX, maxY;
        FoundBarcode f;
        public BoundingBox(FoundBarcode f)
        {
            this.f = f;
            this.minX = this.minY = int.MaxValue;
            this.maxX = this.maxY = 0;
            foreach (SKPoint p in f.Polygon)
            {
                if (p.X < this.minX) this.minX = (int)p.X;
                if (p.X > this.maxX) this.maxX = (int)p.X;
                if (p.Y < this.minY) this.minY = (int)p.Y;
                if (p.Y > this.maxY) this.maxY = (int)p.Y;
            }
        }

        public bool contains(SKPoint p)
        {
            return p.X >= this.minX && p.X <= this.maxX && p.Y >= this.minY && p.Y <= this.maxY;
        }

        public bool contains(BoundingBox b)
        {
            int n = 0;
            foreach (SKPoint p in b.f.Polygon)
                if (this.contains(p)) n++;
            return (n > 0);
        }

        public FoundBarcode FoundBarcode { get { return f; } }
    }
}
