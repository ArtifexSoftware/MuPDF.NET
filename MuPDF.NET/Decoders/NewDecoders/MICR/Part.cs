using SkiaSharp;
using System;
using System.Drawing;

namespace BarcodeReader.Core.MICR
{
	internal class Part
    {
        bool scaned = false;
        string ch;
        int xIn, xEnd, yIn, yEnd;
        public Part(Part p)
        {
            this.xIn = p.xIn;
            this.xEnd = p.xEnd;
            this.yIn = p.yIn;
            this.yEnd = p.yEnd;
            this.ch = p.ch;
            this.scaned = p.scaned;
        }

        public Part(MyPoint p)
        {
            xIn = xEnd = p.X;
            yIn = yEnd = p.Y;
        }

        public void Add(int x, int y)
        {
            if (xIn > x) xIn = x;
            else if (xEnd < x) xEnd = x;

            if (yEnd < y) yEnd = y;
        }

        public void Join(Part p)
        {
            if (xIn > p.xIn) xIn = p.xIn;
            if (xEnd < p.xEnd) xEnd = p.xEnd;
            if (yIn > p.yIn) yIn = p.yIn;
            if (yEnd < p.yEnd) yEnd = p.yEnd;
        }

        public SKPoint[] GetBBox()
        {
            return new SKPoint[] { new SKPoint(xIn - 1, yIn - 1), new SKPoint(xEnd + 1, yIn - 1), new SKPoint(xEnd + 1, yEnd + 1), new SKPoint(xIn - 1, yEnd + 1), new SKPoint(xIn - 1, yIn - 1) };
        }

        public Rectangle GetRectangle()
        {
            return new Rectangle(xIn, yIn, xEnd - xIn + 1, yEnd - yIn + 1);
        }

        public void SetRectangle(Rectangle r)
        {
            this.xIn = r.X;
            this.yIn = r.Y;
            this.xEnd = r.X + r.Width - 1;
            this.YEnd = r.Y + r.Height - 1;
        }

        public int XIn { get { return xIn; } set { xIn = value; } }
        public int XEnd { get { return xEnd; } set { xEnd = value; } }
        public int YIn { get { return yIn; } set { yIn = value; } }
        public int YEnd { get { return yEnd; } set { yEnd = value; } }

        public int Width { get { return xEnd - xIn + 1; } }
        public int Height { get { return yEnd - yIn + 1; } }
        public string Char { get { return ch; } set { ch = value; scaned = true; } }
        public bool Scaned { get { return scaned; } }

        public SKPoint Center { get { return new SKPoint((xIn + xEnd) / 2, (yIn + yEnd) / 2); } }
        public SKPoint LU { get { return new SKPoint(xIn, yIn); } }
        public SKPoint LD { get { return new SKPoint(xIn, yEnd); } }
        public SKPoint RU { get { return new SKPoint(xEnd, yIn); } }
        public SKPoint RD { get { return new SKPoint(xEnd, yEnd); } }

        public float Dist(Part p)
        {
            SKPoint b = p.Center;
            SKPoint a = this.Center;
            return (float)(Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)));
        }       
    }

	internal class SortablePart : Part, IComparable
    {
        float x;
        public SortablePart(Part p, float x)
            : base(p)
        {
            this.x = x;
        }

        public int CompareTo(Object o)
        {
            SortablePart p = (SortablePart)o;
            return (int)((p.x - this.x) * 10);
        }

        public float X { get { return x; } }
    }
}
