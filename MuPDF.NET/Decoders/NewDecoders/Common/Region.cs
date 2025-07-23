using System;
using System.Drawing;

namespace BarcodeReader.Core.Common
{
    //Base class to hold a found barcode defined by 4 corners.
#if CORE_DEV
    public
#else
    internal
#endif
    class BarCodeRegion
    {
        public int startPattern;
        public MyPointF A, B, C, D;
        protected float minX, minY, maxX, maxY;
        public ABarCodeData[] Data;
        public float Confidence, angle;
        public bool Reversed;

        public BarCodeRegion() { }

        public BarCodeRegion(BarCodeRegion r)
        {
            this.startPattern = r.startPattern;
            this.A = r.A;
            this.B = r.B;
            this.C = r.C;
            this.D = r.D;
            this.minX = r.minX;
            this.minY = r.minY;
            this.maxX = r.maxX;
            this.maxY = r.maxY;
            this.Data = null;
            this.angle = r.angle;
            this.Reversed = r.Reversed;
        }

        public BarCodeRegion(MyPointF A, MyPointF B, MyPointF C, MyPointF D)
        {
            SetCorners(A,B,C,D);
        }

        static float Min(float a, float b, float c, float d)
        {
            return a < b ? (a < c ? (a < d ? a : d) : (c < d ? c : d)) : (b < c ? (b < d ? b : d) : (c < d ? c : d));
        }

        static float Max(float a, float b, float c, float d)
        {
            return a > b ? (a > c ? (a > d ? a : d) : (c > d ? c : d)) : (b > c ? (b > d ? b : d) : (c > d ? c : d));
        }

        public void SetCorners(MyPointF A, MyPointF B, MyPointF C, MyPointF D)
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.D = D;
            minX = Min(A.X,B.X,C.X,D.X);
            minY = Min(A.Y, B.Y, C.Y, D.Y);
            maxX = Max(A.X, B.X, C.X, D.X);
            maxY = Max(A.Y, B.Y, C.Y, D.Y);
            angle=(A - C).Angle;
        }

        public bool In(MyPoint pp)
        {
            MyPointF p = (MyPointF)pp;
            return p.Y >= minY && p.Y <= maxY && p.X >= minX && p.X <= maxX;
        }

        public override string ToString()
        {
            return "[" + A + "-" + B + "-" + C + "-" + D+"]";
        }

        public bool SimilarTo(BarCodeRegion r)
        {
            if (this.startPattern==r.startPattern) return FindNumberOfSimilarPoints(r) == 4;
            return false;
            /*
            return Calc.Around(r.A.X, A.X, 2f) && Calc.Around(r.A.Y, A.Y, 2f) &&
                Calc.Around(r.B.X, B.X, 2f) && Calc.Around(r.B.Y, B.Y, 2f) &&
                Calc.Around(r.C.X, C.X, 2f) && Calc.Around(r.C.Y, C.Y, 2f) &&
                Calc.Around(r.D.X, D.X, 2f) && Calc.Around(r.D.Y, D.Y, 2f);
             */
        }

        public bool IntersectsWith(BarCodeRegion r, float epsilon)
        {
            // based on RectangleF.IntersectsWith

            return (r.A.X-epsilon < epsilon + this.A.X + (this.B.X - this.A.X)) &&
                   (this.A.X - epsilon < epsilon + r.A.X + (r.B.X - r.A.X)) &&
                   (r.A.Y - epsilon < epsilon+this.A.Y + (this.D.Y - this.A.Y)) &&
                   (this.A.Y - epsilon < epsilon + r.A.Y + (r.D.Y - r.A.Y));
        }


        public int FindNumberOfSimilarPoints(BarCodeRegion r)
        {
           int i = 0;

            if (Calc.Around(r.A.X, A.X, 2f) && Calc.Around(r.A.Y, A.Y, 2f))
                i++;
            
            if (Calc.Around(r.B.X, B.X, 2f) && Calc.Around(r.B.Y, B.Y, 2f))
                i++;

            if (Calc.Around(r.C.X, C.X, 2f) && Calc.Around(r.C.Y, C.Y, 2f))
                i++;

            if (Calc.Around(r.D.X, D.X, 2f) && Calc.Around(r.D.Y, D.Y, 2f))
                i++;

            return i;
        }
       
        public bool SimilarToData(BarCodeRegion r)
        {
            if (this.Data.Length != r.Data.Length)
                return false;

            for (int i=0; i<this.Data.Length; i++)
            {
                if (!this.Data[i].IsSimilar(r.Data[i]))
                    return false;
            }

            return true;
        }

        internal void InflateToLargest(BarCodeRegion r)
        {
            // left top
            A.X = Math.Min(r.A.X, A.X);
            A.Y = Math.Min(r.A.Y, A.Y);
            // top right 
            B.X = Math.Max(r.B.X, B.X);
            B.Y = Math.Min(r.B.Y, A.Y);
            // bottom right
            C.X = Math.Max(r.C.X, C.X);
            C.Y = Math.Max(r.C.Y, C.Y);
            // bottom left
            D.X = Math.Min(r.D.X, D.X);
            D.Y = Math.Max(r.D.Y, D.Y);
        }

        public Rectangle GetBounds()
        {
            var x1 = Math.Min(A.X, D.X);
            var y1 = Math.Min(A.Y, B.Y);
            var x2 = Math.Max(B.X, C.X);
            var y2 = Math.Min(C.Y, D.Y);

            return Rectangle.FromLTRB((int)x1, (int)y1, (int)x2, (int)y2);
        }
    }
}
