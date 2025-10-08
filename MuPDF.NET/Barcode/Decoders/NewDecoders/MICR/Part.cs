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

        public SKPointI[] GetBBox()
        {
            return new SKPointI[] { new SKPointI(xIn - 1, yIn - 1), new SKPointI(xEnd + 1, yIn - 1), new SKPointI(xEnd + 1, yEnd + 1), new SKPointI(xIn - 1, yEnd + 1), new SKPointI(xIn - 1, yIn - 1) };
        }

        public SKRect GetRectangle()
        {
            return new SKRect(xIn, yIn, xEnd + 1, yEnd + 1);
        }

        public void SetRectangle(SKRect r)
        {
            this.xIn = (int)r.Left;
            this.yIn = (int)r.Top;
            this.xEnd = (int)r.Right - 1;
            this.YEnd = (int)r.Bottom - 1;
        }

        public int XIn { get { return xIn; } set { xIn = value; } }
        public int XEnd { get { return xEnd; } set { xEnd = value; } }
        public int YIn { get { return yIn; } set { yIn = value; } }
        public int YEnd { get { return yEnd; } set { yEnd = value; } }

        public int Width { get { return xEnd - xIn + 1; } }
        public int Height { get { return yEnd - yIn + 1; } }
        public string Char { get { return ch; } set { ch = value; scaned = true; } }
        public bool Scaned { get { return scaned; } }

        public SKPointI Center { get { return new SKPointI((int)((xIn + xEnd) / 2), (int)((yIn + yEnd) / 2)); } }
        public SKPointI LU { get { return new SKPointI((int)xIn, (int)yIn); } }
        public SKPointI LD { get { return new SKPointI((int)xIn, (int)yEnd); } }
        public SKPointI RU { get { return new SKPointI((int)xEnd, (int)yIn); } }
        public SKPointI RD { get { return new SKPointI((int)xEnd, (int)yEnd); } }

        public float Dist(Part p)
        {
            SKPointI b = p.Center;
            SKPointI a = this.Center;
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
