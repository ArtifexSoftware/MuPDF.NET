using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using BarcodeReader.Core.Common;
using MuPDF.NET;
using SkiaSharp;

namespace BarcodeReader.Core.FormOMR
{
#if CORE_DEV
    public
#else
    internal
#endif
    abstract class FormOMRLPattern : SymbologyReader2D
    {
        protected LinkedList<BoundingBox> candidates;
        protected ImageScaner scan;
        protected int width, height; //width and height of the image
        protected int minRad = 5; //min long of accepted lines
        protected int maxRad = 5000; //max long of accepted lines
        protected int distToJoin = 20; //min dist to join segments that are aligned

        protected virtual bool checkRatio(SKPoint[] pp) { return true; }
        public bool QuiteZoneRequired = true; // quite zone requirement

        public FormOMRLPattern()
        {
            // setting to use .WholeImage as the thershold filter so it provides thinner lines
            ThresholdFilterMethodToUse = ThresholdFilterMethod.WholeImage;
        }

        const int K = 10;
		protected override FoundBarcode[] DecodeBarcode()
        {
            scan = new ImageScaner(BWImage);
            int W = scan.Width / K, H = scan.Height / K;
            LineSlicer slicer = new LineSlicer(BWImage);
            slicer.MaxVariance = 0.001f;
            LinkedList<EdgeVar> edges = slicer.GetEdges(minRad, maxRad, true, true);
            ArrayList aEdges = new ArrayList(edges);
            aEdges.Sort(new EdgeLengthComparer());


            Grid grid = new Grid(W, H, K);
            foreach (EdgeVar e in edges) grid.add(e, slicer);

            candidates = new LinkedList<BoundingBox>();

            foreach (EdgeVar edge in aEdges)
            {
                follow(edge.End, edge, grid);
                follow(edge.In, slicer.mirror(edge), grid);
            }

            ArrayList result = new ArrayList();
            foreach (BoundingBox c in candidates) result.Add(c.FoundBarcode);
            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }

        void follow(MyPoint p, EdgeVar edge, Grid grid)
        {
            foreach (EdgeVar e in grid.get(p))
                if (edge != e && edge.Variance + e.Variance < 0.1f && checkRatio(e, edge))
                {
                    MyPoint vA = e.In;
                    MyPoint vB = e.End;
                    float dist = (p - vA).Length;
                    float dist2 = (p - vB).Length;
                    if (dist < dist2 && dist < distToJoin)
                    {
                        float diff = (edge.Angle - e.Angle);
                        float sinDiff = (float)Math.Sin(diff);
                        bool isCorner = Calc.Around(sinDiff, 1f, 0.1f);
                        if (isCorner)
                        {
                            SKPoint[] points = checkShape(edge, e);
                            if (points != null)
                            {
                                //detect if box is empty or checked
                                FoundBarcode f = new FoundBarcode();
								f.BarcodeType = SymbologyType.Checkbox;
                                f.Polygon = points;

								byte[] pointTypes = new byte[5] { (byte) 0, (byte) 1, (byte) 1, (byte) 1, (byte) 1 };
                                SKPath path = new SKPath();
                                SKPoint[] skPoints = f.Polygon;

                                if (skPoints.Length > 0)
                                {
                                    path.MoveTo(skPoints[0]);
                                    for (int i = 1; i < skPoints.Length; i++)
                                    {
                                        path.LineTo(skPoints[i]);
                                    }
                                    path.Close(); // Close the polygon if needed

                                    SKRect bounds;
                                    path.GetBounds(out bounds);
                                    f.Rect = new Rectangle((int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height);
                                }

                                BoundingBox bb = new BoundingBox(f);
                                bool done = false;
                                foreach (BoundingBox i in candidates) if (bb.contains(i)) { done = true; break; }
                                if (!done)
                                {
									f.Value = boxEmpty(points) ? "0" : "1";
                                    candidates.AddLast(bb);
                                }
                            }
                        }
                    }
                }
        }

        protected virtual bool checkRatio(EdgeVar a, EdgeVar b) { return false; }
        protected virtual SKPoint[] checkShape(EdgeVar a, EdgeVar b) { return null; }


        //looks for a parallel segment from p to p+vdX (with length l) in the vdY direction, scaning from "from" to "to"
        //returns true if found, and the vertex of the twin segment.
        protected bool hasTwin(MyPoint p, float l, float from, float to, MyVectorF vdX, MyVectorF vdY, out MyPointF A, out MyPointF B)
        {
            A = B = MyPointF.Empty;
            for (float offset = from; offset <= to; offset += 1f)
            {
                MyPoint a = p + vdY * offset;
                MyPoint b = a + vdX * l;
                Bresenham br = new Bresenham(a, b);
                int white = 0;
                while (!br.End())
                {
                    if (!scan.isBlack(br.Current)) white++;
                    br.Next();
                }
                if (white < l / 5)
                {
                    A = a; B = b;
                    return true;
                }
            }
            return false;
        }

        protected bool boxEmpty(SKPoint[] points) //MyPoint p, float l, MyVectorF vdX, MyVectorF vdY)
        {
            MyPoint p = (MyPoint)points[0];
            MyPoint q = (MyPoint)points[3];
            MyPoint pp = (MyPoint)points[1];
            MyVector vdX = pp - p;
            int border = Convert.ToInt32(Math.Ceiling(vdX.Length / 10f)); //10% of the width of the square
            int nLines = 0, nBlackLines = 0;
            Bresenham brY = new Bresenham(p, q);
            while (!brY.End())
            {
                MyPoint b = brY.Current + vdX;
                Bresenham brX = new Bresenham(brY.Current, b);

                //skip white pixels if any, and if they are just a few
                int n = 0;
                while (!brX.End() && !scan.isBlack(brX.Current)) { brX.Next(); n++; }
                if (n < border) //consider white pixels as noise. So, now skip the border of the edge
                {
                    while (!brX.End() && scan.isBlack(brX.Current)) brX.Next();
                }

                //now we are on the first INNER white pixel
                bool isBlack = false;
                int nBlack = 0, nCurrent = 0, prevBlack = 0;
                while (!brX.End())
                {
                    if (scan.isBlack(brX.Current) != isBlack) //transition 
                    {
                        if (isBlack) //black to white transition
                        { nBlack += nCurrent; prevBlack = nCurrent; }
                        nCurrent = 0;
                        isBlack = !isBlack;
                    }
                    else
                    {
                        nCurrent++;
                    }
                    brX.Next();
                }
                if (!isBlack && nCurrent < border) nBlack -= prevBlack;
                if (nBlack > 0) nBlackLines++;
                nLines++;
                brY.Next();
            }
            return nBlackLines <= 2;//;nLines / 4;
        }

        protected SKPoint[] box(MyPointF aIn, MyPointF aEnd, MyPointF AIn, MyPointF AEnd,
            MyPointF bIn, MyPointF bEnd, MyPointF BIn, MyPointF BEnd)
        {
            RegressionLine La = new RegressionLine(aIn, aEnd);
            RegressionLine LA = new RegressionLine(AIn, AEnd);
            RegressionLine Lb = new RegressionLine(bIn, bEnd);
            RegressionLine LB = new RegressionLine(BIn, BEnd);

            MyPointF Q1 = La.Intersection(Lb);
            MyPointF Q2 = La.Intersection(LB);
            MyPointF Q3 = LA.Intersection(LB);
            MyPointF Q4 = LA.Intersection(Lb);
            
            if (QuiteZoneRequired)
            {
                if (checkQuietZone(new MyPointF[] { Q1, Q2, Q3, Q4 }))
                    return new SKPoint[] { Q1, Q2, Q3, Q4, Q1 };
                else
                    return null;
            }
            else
            {
                return new SKPoint[] { Q1, Q2, Q3, Q4, Q1 };
            }
        }

        protected bool checkQuietZone(MyPointF[] box)
        {
            Bresenham[] br = new Bresenham[8];
            for (int i = 0; i < 4; i++)
            {
                MyPointF a = box[i];
                MyPointF b = box[(i + 1) % 4];
                br[2 * i] = new Bresenham(a, a - b);
                br[2 * i + 1] = new Bresenham(b, b - a);
            }

            int pass = 0;
            while (pass < 10)
            {
                bool allWhite = true;
                for (int i = 0; i < 8 && allWhite; i++)
                {
                    if (scan.isBlack(br[i].Current)) allWhite = false;
                }
                if (allWhite) return true;
                for (int i = 0; i < 8; i++) br[i].Next();
                pass++;
            }
            return false;
        }


        class Grid
        {
            int W, H, K;
            public LinkedList<EdgeVar> edges;
            ArrayList[][] cells;
            public Grid(int W, int H, int K)
            {
                this.W = W; this.H = H; this.K = K;
                cells = new ArrayList[W + 1][];
                for (int i = 0; i <= W; i++)
                {
                    cells[i] = new ArrayList[H + 1];
                    for (int j = 0; j <= H; j++) cells[i][j] = new ArrayList();
                }
                edges = new LinkedList<EdgeVar>();
            }

            public void add(int x, int y, EdgeVar e)
            {
                if (x >= 0 && x <= W && y >= 0 && y <= H) cells[x][y].Add(e);
            }

            public ArrayList get(SKPoint p)
            {
                return cells[(int)(p.X / K)][(int)(p.Y / K)];
            }

            private void _add(EdgeVar e)
            {
                MyPoint p = e.In;
                int x = p.X / K, y = p.Y / K;
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        add(x + i, y + j, e);
                edges.AddLast(e);
            }

            public void add(EdgeVar e, LineSlicer slicer)
            {
                _add(e);
                EdgeVar m = slicer.mirror(e);
                _add(m);
            }
        }


        public int MinRad { get { return minRad; } set { minRad = value; } }
        public int MaxRad { get { return maxRad; } set { maxRad = value; } }
        public int DistToJoin { get { return distToJoin; } set { distToJoin = value; } }
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class FormOMRSquareLPattern : FormOMRLPattern
    {
		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Checkbox;
		}
        protected override bool checkRatio(EdgeVar a, EdgeVar b)
        {
            return Calc.Around(a.Length, b.Length, b.Length / 7);
        }

        protected override SKPoint[] checkShape(EdgeVar a, EdgeVar b)
        {
            float la = a.Length;
            float lb = b.Length;
            float l = la > lb ? la : lb;

            MyVectorF vdA = (MyPoint)a.End - (MyPoint)a.In;
            MyVectorF vdB = (MyPoint)b.End - (MyPoint)b.In;
            vdA = vdA.Normalized;
            vdB = vdB.Normalized;

            MyPointF AIn, AEnd, BIn, BEnd;
            if (hasTwin(a.In, l, l * 0.7f, l * 1.3f, vdA, vdB, out AIn, out AEnd) &&
                hasTwin(b.In, l, l * 0.7f, l * 1.3f, vdB, -vdA, out BIn, out BEnd))
                return box(a.In, a.End, AIn, AEnd, b.In, b.End, BIn, BEnd);
            return null;
        }
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class FormOMRRectangleLPattern : FormOMRLPattern
    {
		public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.Checkbox;
        }

        protected override bool checkRatio(EdgeVar A, EdgeVar B)
        {
            if (Calc.Around((float)Math.Cos(A.Angle), 1f, 0.1f)) return checkRectangleOrientation(A, B);
            else if (Calc.Around((float)Math.Cos(B.Angle), 1f, 0.1f)) return checkRectangleOrientation(B, A);
            return false;
        }

        protected virtual bool checkRectangleOrientation(EdgeVar A, EdgeVar B) { return false; }


        protected override SKPoint[] checkShape(EdgeVar a, EdgeVar b)
        {
            float la = a.Length;
            float lb = b.Length;
            float l = la > lb ? la : lb;

            MyVectorF vdA = (MyPoint)a.End - (MyPoint)a.In;
            MyVectorF vdB = (MyPoint)b.End - (MyPoint)b.In;
            vdA = vdA.Normalized;
            vdB = vdB.Normalized;

            MyPointF AIn, AEnd, BIn, BEnd;
            if (hasTwin(a.In, la, lb * 0.8f, lb * 1.2f, vdA, vdB, out AIn, out AEnd) &&
                hasTwin(b.In, lb, la * 0.8f, la * 1.2f, vdB, -vdA, out BIn, out BEnd))
                return box(a.In, a.End, AIn, AEnd, b.In, b.End, BIn, BEnd);
            return null;
        }
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class FormOMRRectangleLPatternVert : FormOMRRectangleLPattern
    {
	    public override SymbologyType GetBarCodeType()
	    {
            return SymbologyType.Checkbox;
	    }

	    protected override bool checkRectangleOrientation(EdgeVar A, EdgeVar B)
        {
            float a = A.Length, b = B.Length;
            if (a < b)
            {
                float r = a / b;
                return r > 0.5f && r < 0.8f;
            }
            return false;
        }
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class FormOMRRectangleLPatternHoriz : FormOMRRectangleLPattern
    {
	    public override SymbologyType GetBarCodeType()
	    {
            return SymbologyType.Checkbox;
	    }

	    protected override bool checkRectangleOrientation(EdgeVar A, EdgeVar B)
        {
            float a = A.Length, b = B.Length;
            if (a > b)
            {
                float r = b / a;
                return r > 0.5f && r < 0.8f;
            }
            return false;
        }
    }
}
