using SkiaSharp;
using System.Drawing;

namespace BarcodeReader.Core.Common
{
    //Calculates the x,y coordinates of pixels of an edge a-b
    internal class Bresenham
    {
        MyPoint a, b;
        MyVectorF vd;
        int steps;
        MyPoint p, k;
        bool swapXY;
        float error, incError;

        public Bresenham(Bresenham l)
        {
            this.a = l.a;
            this.b = l.b;
            this.vd = l.vd;
            this.steps = l.steps;
            this.p = l.p;
            this.k = l.k;
            this.swapXY = l.swapXY;
            this.error = l.error;
            this.incError = l.incError;
        }

        //Horizontal line, left to right
        public Bresenham(int xIn, int xEnd, int y)
        {
            a = new MyPoint(xIn, y);
            b = new MyPoint(xEnd, y);
            vd = ((MyVectorF)(b - a)).Normalized;
            p = a;
            k = new MyPoint(1, 0);
            incError = error = 0F;
            swapXY = false;
            steps = xEnd - xIn;
        }

        public Bresenham(MyPoint a, MyPoint b)
        {
            this.a = a; this.b = b;
            this.vd = ((MyVectorF)(b - a)).Normalized;
            int incX = b.X - a.X;
            int incY = b.Y - a.Y;
            if (incX >= 0)
                if (incY > 0)
                    if (incX >= incY) { k = new SKPoint(1, 1); swapXY = false; steps = incX; }
                    else { k = new SKPoint(1, 1); swapXY = true; steps = incY; }
                else
                    if (incX >= -incY) { k = new SKPoint(1, -1); swapXY = false; steps = incX; }
                    else { k = new SKPoint(1, -1); swapXY = true; steps = -incY; }
            else
                if (incY > 0)
                    if (-incX >= incY) { k = new SKPoint(-1, 1); swapXY = false; steps = -incX; }
                    else { k = new SKPoint(-1, 1); swapXY = true; steps = incY; }
                else
                    if (-incX >= -incY) { k = new SKPoint(-1, -1); swapXY = false; steps = -incX; }
                    else { k = new SKPoint(-1, -1); swapXY = true; steps = -incY; }
            p = a;
            error = 0.0F;
            incError = (swapXY ? (float)incX * k.X / incY * k.Y : (float)incY * k.Y / incX * k.X);
        }

        public Bresenham(MyPointF p0, MyVectorF vd)
        {
            this.vd = vd;
            if (vd.X >= 0F)
                if (vd.Y > 0F)
                    if (vd.X >= vd.Y) { k = new SKPoint(1, 1); swapXY = false; }
                    else { k = new SKPoint(1, 1); swapXY = true; }
                else
                    if (vd.X >= -vd.Y) { k = new SKPoint(1, -1); swapXY = false; }
                    else { k = new SKPoint(1, -1); swapXY = true;}
            else
                if (vd.Y > 0F)
                    if (-vd.X >= vd.Y) { k = new SKPoint(-1, 1); swapXY = false; }
                    else { k = new SKPoint(-1, 1); swapXY = true; }
                else
                    if (-vd.X >= -vd.Y) { k = new SKPoint(-1, -1); swapXY = false; }
                    else { k = new SKPoint(-1, -1); swapXY = true; }
            p = p0.Truncate();
            if (!swapXY) {
                float alfa=((float)p.X - p0.X + 0.5F) / vd.X;
                error=(p0.Y + vd.Y*alfa -((float)p.Y+0.5F) )*k.Y;
                incError=vd.Y*k.Y/vd.X*k.X; 
            } else {
                float alfa=((float)p.Y - p0.Y + 0.5F) / vd.Y;
                error=(p0.X + vd.X*alfa - ((float)p.X+0.5F))*k.X;
                incError=vd.X*k.X/vd.Y*k.Y;
            }
        }

        public MyPointF CurrentF
        {
            get
            {
                if (!swapXY) return (MyPointF)p + new MyVectorF(0.5F, 0.5F + error * k.Y);
                else return (MyPointF)p + new MyVectorF(0.5F + error * k.X, 0.5F);
            }
        }

        public void MoveTo(MyPoint q)
        {
            p = q;
            int incX = b.X - p.X; if (incX<0) incX=-incX;
            int incY = b.Y - p.Y; if (incY<0) incY=-incY;
            steps=incX>incY?incX:incY;
            error = 0.0F;
        }

        public bool End() { return steps < 0; }
        public void Next()
        {
            if (swapXY) p.Y += k.Y;
            else p.X += k.X;
            error += incError;
            if (error > 0.5F)
            {
                error -= 1.0F;
                if (swapXY) p.X += k.X;
                else p.Y += k.Y;
            }
            steps--;
        }

        public void Previous()
        {
            if (swapXY) p.Y -= k.Y;
            else p.X -= k.X;
            error -= incError;
            if (error < -0.5F)
            {
                error += 1.0F;
                if (swapXY) p.X -= k.X;
                else p.Y -= k.Y;
            }
            steps++;
        }

        public MyPoint Current { get { return p; } }
        public int Steps { get { return steps; } }
        public MyVectorF Vd { get { return vd; } }
        public float Length { get { return (a - b).Length; } }
        public float CurrentLength { get { return (p - a).Length; } }
    }
}
