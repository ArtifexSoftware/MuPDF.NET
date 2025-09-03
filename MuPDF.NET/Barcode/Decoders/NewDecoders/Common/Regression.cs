using System;
using System.Collections.Generic;

namespace BarcodeReader.Core.Common
{
    //Class to calculate a regression line or a "dual" regression line (if samples 
    //belong to a 2 different but parallel lines
    class Regression
    {
        MyPointF l0,r0;
        float a, b, lC, rC, la, lb, lc, ra, rb, rc;  // ax + by + c = 0
        float modul; // sqrt(a^2 + b^2)
        float dX, dY; // slope=dY/dX       
        float lsumX, lsumY, lsumXY, lsumX2, lsumY2, lerror;
        float rsumX, rsumY, rsumXY, rsumX2, rsumY2, rerror;
        float minX, minY, maxX, maxY; // to get an approximation of variance
        int nL, nR;
        bool solved;
        LinkedList<MyPointF> pointsL = new LinkedList<MyPointF>();
        LinkedList<MyPointF> pointsR = new LinkedList<MyPointF>();
        bool debug = false;

        public Regression(MyPointF l0)
        {
            this.l0 = l0;
            this.r0 = l0; //right side should not be used!
            Initialize();
        }

        public Regression(MyPointF l0, MyPointF r0)
        {
            this.l0 = l0;
            this.r0 = r0;
            Initialize();
        }
        public Regression(MyPointF p0, MyVectorF dist)
        {
            this.l0 = p0;
            this.r0 = l0 + dist;
            Initialize();
        }

        void Initialize()
        {
            lsumX = lsumY = lsumXY = lsumX2 = lsumY2 = 0F;
            rsumX = rsumY = rsumXY = rsumX2 = rsumY2 = 0F;
            minX = minY = float.MaxValue;
            maxX = maxY = float.MinValue;
            nL = nR = 0;
            solved = false;
        }


        public void AddPointL(MyPointF p)
        {
            float x = p.X - l0.X;
            float y = p.Y - l0.Y;
            pointsL.AddLast(new MyPointF(x, y));

            nL++;
            lsumX += x;
            lsumY += y;
            lsumXY += x * y;
            lsumX2 += x * x;
            lsumY2 += y * y;
            UpdateMaxMin(p);
            solved = false;
        }

        public void AddPointR(MyPointF p)
        {
            float x = p.X - r0.X;
            float y = p.Y - r0.Y;
            pointsR.AddLast(new MyPointF(x, y));

            nR++;
            rsumX += x;
            rsumY += y;
            rsumXY += x * y;
            rsumX2 += x * x;
            rsumY2 += y * y;
            solved = false;
        }

        void UpdateMaxMin(MyPointF p)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        public void Solve()
        {
            if (nL > 0) _solve(lsumX, lsumY, lsumXY, lsumX2, lsumY2, nL, pointsL, out la, out lb, out lc, out lerror);
            if (nR > 0) _solve(rsumX, rsumY, rsumXY, rsumX2, rsumY2, nR, pointsR, out ra, out rb, out rc, out rerror);

            //mean of both lines
            if (nL == 0) { a = ra; b = rb; }
            else if (nR == 0) { a = la; b = lb; }
            else if (rerror / nR < lerror / nL) { a = ra; b = rb; }
            else if (rerror / nR > lerror / nL) { a = la; b = lb; }
            else
            {
                a = (la + ra) / 2f;
                b = (lb + rb) / 2f;
            }

            modul = (float)Math.Sqrt(a * a + b * b);
            a /= modul; b /= modul;

            lC = -a * (l0.X + lsumX / nL) - b * (l0.Y + lsumY / nL);
            rC = -a * (r0.X + rsumX / nR) - b * (r0.Y + rsumY / nR);

            solved = true;
        }
        
        
        void _solve(float sumX, float sumY, float sumXY, float sumX2, float sumY2, int n, LinkedList<MyPointF> points, out float a, out float b, out float c, out float error)
        {            
            if (maxX - minX > maxY - minY)
            { //horizontal line
                dX = (sumX2 * n - sumX * sumX);
                if (Calc.Around(dX, 0.0F, 0.00001F)) //vertical line
                {
                    dY = a = 1F;
                    dX = b = 0F;
                    c = -sumX / n; if (n == 0) throw new Exception("div by 0");
                }
                else
                {
                    dY = (sumXY * n - sumX * sumY);
                    a = dY;
                    b = -dX;
                    c = (dX * sumY - dY * sumX) / n; if (n == 0) throw new Exception("div by 0");
                }
            }
            else //Vertical line --> invert axis
            {
                dY = (sumY2 * n - sumY * sumY);
                if (Calc.Around(dY, 0.0F, 0.00001F)) //horizontal line
                {
                    dY = a = 0F;
                    dX = b = 1F;
                    if (n == 0) throw new Exception("div by 0"); c = -sumY / n;
                }
                else
                {
                    dX = (sumXY * n - sumX * sumY);
                    a = -dY;
                    b = dX;
                    if (n == 0) throw new Exception("div by 0"); c = (dY * sumX - dX * sumY) / n;
                }
            }

            float modul = (float)Math.Sqrt(a * a + b * b);
            a /= modul; b /= modul; c /= modul; //normalize result

            error = 0f;
            foreach (MyPointF p in points)
            {
                float d = a * p.X + b * p.Y + c;
                error += d * d;
            }
        }

        public float DistL(MyPointF p)
        {
            if (p.IsEmpty) return float.MaxValue;
            if (!solved) Solve();
            if (modul == 0f) throw new Exception("Div by 0");
            //float x = p.X - l0.X;
            //float y = p.Y - l0.Y;
            return (a * p.X + b * p.Y + lC) / modul;
        }

        public float DistR(MyPointF p)
        {
            if (p.IsEmpty) return float.MaxValue;
            if (!solved) Solve();
            if (modul == 0f) throw new Exception("Div by 0");
            //float x = p.X - r0.X;
            //float y = p.Y - r0.Y;
            return (a * p.X + b * p.Y + rC) / modul;
        }

        //project to Left line
        public MyPointF Project(MyPoint p) { return Project((MyPointF)p); }
        public MyPointF Project(MyPointF p)
        {
            float a2 = la * la;
            float b2 = lb * lb;
            float d = b2 + a2;
            float x = 0F, y = 0F;
            float xx = p.X - l0.X;
            float yy = p.Y - l0.Y;
            if (a2 < b2)
            {
                if (d == 0f) throw new Exception("Div by 0");
                x = (lb * (lb * xx - la * yy) - la * lc) / d;
                if (lb == 0f) throw new Exception("Div by 0");
                y = (-la * x - lc) / lb;
            }
            else
            {
                if (d == 0f) throw new Exception("Div by 0");
                y = (la * (la * yy - lb * xx) - lb * lc) / d;
                if (la == 0f) throw new Exception("Div by 0");
                x = (-lb * y - lc) / la;
            }
            return new MyPointF(x + l0.X, y + l0.Y);
        }

        //intersection with y=p.Y or x=p.X based on vd direction
        public MyPoint project(MyPoint p, MyVector vd)
        {
            if (!solved) Solve();
            if (vd.Y == 0) //vd is horizontal
                p.X = Convert.ToInt32(l0.X - (lb * (p.Y - l0.Y) + lc) / la);
            else
                p.Y = Convert.ToInt32(l0.Y - (la * (p.X - l0.X) + lc) / lb);
            return p;
        }

        public float A { get { if (!solved) Solve(); return a; } }
        public float B { get { if (!solved) Solve(); return b; } }
        //public float C { get { if (!solved) Solve(); return c; } }
        public float Dx { get { if (!solved) Solve(); return dX; } }
        public float Dy { get { if (!solved) Solve(); return dY; } }
        public MyVectorF VdX { get { if (!solved) Solve(); return new MyVectorF(dX, dY); } }
        public MyVectorF VdY { get { if (!solved) Solve(); return new MyVectorF(-dY, dX); } }
        public int NR { get { return pointsR.Count; } }
        public int NL { get { return pointsL.Count; } }
        public RegressionLine LineL { get { if (!solved) Solve(); return new RegressionLine(a, b, lC); } }
        public RegressionLine LineR { get { if (!solved) Solve(); return new RegressionLine(a, b, rC); } }

        public void setDebug(bool deb) { solved = false;  debug = deb;  }
        public LinkedList<MyPointF> PointsL { get { return pointsL; } }
        public LinkedList<MyPointF> PointsR { get { return pointsR; } }
    }

    //class that represents the equation of a line ax+by+c=0
    //Used to calculate intersections and find corners.
    class RegressionLine
    {
        float a, b, c;
        public RegressionLine(float a, float b, float c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public RegressionLine(MyPointF A, MyPointF B)
        {
            c = -1f;
            float det = A.X * B.Y - A.Y * B.X;
            a = (B.Y - A.Y) / det;
            b = (A.X - B.X) / det;
        }

        public MyVectorF GetNormal()
        {
            return new MyVectorF(a, b);
        }

        public MyPointF Intersection(RegressionLine r)
        {
            float max = 1F;
            if (Math.Abs(a) > Math.Abs(b))
            {
                if (Math.Abs(a) > Math.Abs(c)) max = a;
                else max = c;
            }
            else
            {
                if (Math.Abs(b) > Math.Abs(c)) max = b;
                else max = c;
            }
            if (max == 0f) return MyPointF.Empty;
            if (Math.Abs(a / max) > Math.Abs(b / max))
            {
                float d = -r.a * b + r.b * a;
                if (d == 0f) return MyPointF.Empty;
                float y = (r.a * c - r.c * a) / d;
                if (a == 0f) return MyPointF.Empty;
                float x = (-b * y - c) / a;
                return new MyPointF(x, y);
            }
            else
            {
                float d = r.a * b - r.b * a;
                if (d == 0f) return MyPointF.Empty;
                float x = (r.b * c - r.c * b) / d;
                if (b == 0f) return MyPointF.Empty;
                float y = (-a * x - c) / b;
                return new MyPointF(x, y);
            }
        }
    }
}
