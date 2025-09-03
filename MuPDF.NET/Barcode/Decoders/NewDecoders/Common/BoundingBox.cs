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
            foreach (SKPointI p in f.Polygon)
            {
                if (p.X < this.minX) this.minX = p.X;
                if (p.X > this.maxX) this.maxX = p.X;
                if (p.Y < this.minY) this.minY = p.Y;
                if (p.Y > this.maxY) this.maxY = p.Y;
            }
        }

        public bool contains(SKPointI p)
        {
            return p.X >= this.minX && p.X <= this.maxX && p.Y >= this.minY && p.Y <= this.maxY;
        }

        public bool contains(BoundingBox b)
        {
            int n = 0;
            foreach (SKPointI p in b.f.Polygon)
                if (this.contains(p)) n++;
            return (n > 0);
        }

        public FoundBarcode FoundBarcode { get { return f; } }
    }
}
