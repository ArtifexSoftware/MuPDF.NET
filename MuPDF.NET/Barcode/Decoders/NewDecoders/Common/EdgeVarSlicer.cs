using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BarcodeReader.Core.Common
{
	internal class EdgeVarSlicer
    {
        int width, height;
        int minRad, maxRad;
        bool scanRows, scanColumns;
        LinkedList<EdgeVar> edges; // array with results
        EdgeVar[] prevRow; //segment indexs from the last minDist rows, 
                          //to allow connecting segments in the current row with those at minDist
        EdgeVar[] currentRow; //segment indexs of the current processing row
        float maxVariance = 0.1f;

        int edgeId = 0;

        public EdgeVarSlicer(int minRad, int maxRad, float maxVariance) { newEdgeSlicer(minRad,maxRad, true,true, maxVariance); }
        public EdgeVarSlicer(int minRad, int maxRad, bool scanColumns, float maxVariance) { newEdgeSlicer(minRad, maxRad, true, scanColumns, maxVariance); }
        public EdgeVarSlicer(int minRad, int maxRad, bool scanRows, bool scanColumns, float maxVariance) { newEdgeSlicer(minRad, maxRad, scanRows, scanColumns, maxVariance); }
        protected void newEdgeSlicer(int minRad, int maxRad, bool scanRows, bool scanColumns, float maxVariance)
        {
            this.minRad = minRad;
            this.maxRad = maxRad;
            this.scanRows = scanRows;
            this.scanColumns = scanColumns;
            this.maxVariance = maxVariance;
        }

        public LinkedList<EdgeVar> GetEdges(BlackAndWhiteImage img)
        {            
            this.width = img.Width;
            this.height = img.Height;
            edges = new LinkedList<EdgeVar>();
            
            //scan rows looking for vertical lines
            if (scanRows)
            {
                currentRow = new EdgeVar[width];
                prevRow = new EdgeVar[width];
                for (int i = 0; i < width; i++) prevRow[i] = null;
                for (int y = 0; y < height; y += 1)
                    ScanRow(y, img.GetRow(y), false);
            }
            
            //scan cols looking for horizontal lines
            if (scanColumns)
            {
                currentRow = new EdgeVar[height];
                prevRow = new EdgeVar[height];
                for (int i = 0; i < height; i++) prevRow[i] = null;
                for (int x = 0; x < width; x += 1)
                    ScanRow(x, img.GetColumn(x), true);
            }
            
            //filter parts
            LinkedList<EdgeVar> filtered = new LinkedList<EdgeVar>();
            foreach (EdgeVar p in edges)
            {
                int l = (int)p.Length;
                if (p != null && l > minRad && l < maxRad)
                    filtered.AddLast(p);
            }
            return filtered;
        }

        //Process a row. For each pixel in the row looks for a previously connected pixel. It if is
        //found, the pixel is added to its segment (or Part object). Otherwise, a new id (Part) is created.
        private void ScanRow(int y, XBitArray row, bool isHorizontal)
        {
            int length = row.Size;
            bool isBlackPrev = false, isBlackCurrent;
            for (int x = 0; x < length; x++)
            {
                isBlackCurrent=row[x];
                if (!isBlackPrev && isBlackCurrent)
                {

                    //look in the prev row for a connected edge.
                    EdgeVar found = null;
                    for (int xi = x - 1; xi <= x + 1 && xi < length; xi++) if (xi>=0)
                    {
                        EdgeVar e = prevRow[xi];
                        if (e != null && e.WhiteToBlack == isBlackCurrent)
                        {
                            if (isHorizontal && e.Add(y,x) || !isHorizontal && e.Add(x,y))
                            {
                                found = e;
                                break;
                            }
                        }
                    }

                    if (found == null) //no connected edge is found, so add a new edge
                    {
                        found = isHorizontal?new EdgeVar(y, x, isBlackCurrent, true, maxVariance, edgeId++):new EdgeVar(x, y, isBlackCurrent, false, maxVariance, edgeId++);
                        edges.AddLast(found);
                    }

                    currentRow[x] = found;
                } else currentRow[x] = null;
                isBlackPrev = isBlackCurrent;
            }

            //move index rows
            EdgeVar[] tmp = prevRow;
            prevRow = currentRow;
            currentRow = tmp;
        }    
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class EdgeVar : IComparable
    {
        int id;
        float x = 0;
        int xIn, xEnd, yIn, yEnd;
        bool whiteToBlack, isHorizontal;
        float maxVariance;

        //to calculate variance
        int N;
        float mean, variance, angle;
        bool recalcAngle;

        protected EdgeVar(int edgeId) { this.id = edgeId; }

        public EdgeVar(int x, int y, bool whiteToBlack, bool isHorizontal, float maxVariance, int edgeId)
        {
            this.id = edgeId;
            this.whiteToBlack = whiteToBlack;
            this.isHorizontal = isHorizontal;
            if (!isHorizontal) { int tmp = x; x = y; y = tmp; }//swap x,y
            xIn = xEnd = x;
            yIn = yEnd = y;
            this.variance = this.mean= 0.0f;
            this.N = 0;
            this.recalcAngle = true;
            this.maxVariance = maxVariance;
        }

        public EdgeVar mirror(int edgeId)
        {
            EdgeVar m = new EdgeVar(edgeId);
            m.xIn = this.xEnd; m.yIn = this.yEnd;
            m.xEnd = this.xIn; m.yEnd = this.yIn;
            m.isHorizontal = this.isHorizontal;
            m.variance = this.variance;
            m.angle = this.Angle+(float)Math.PI;
            m.recalcAngle = false;
            m.maxVariance = this.maxVariance;
            return m;
        }

        public override int GetHashCode()
        {
            return this.id;
        }

        //for every new pixel added, the bounding box is updated
        //LinkedList<double> slopes = new LinkedList<double>();
        public bool Add(int x, int y)
        {
            if (!isHorizontal) { int tmp = x; x = y; y = tmp; }//swap x,y

            //update variance
            if (x == xEnd) return false;
            float slope = (float)(y - yIn) / (float)(x - xIn); //yEnd, xEnd
            float nextMean, nextVariance;
            NextVariance(slope, out nextMean, out nextVariance);

            if (nextVariance > maxVariance) return false;
            mean = nextMean;
            variance = nextVariance;

            xEnd = x;
            yEnd = y;
            N++;
            this.recalcAngle = true;
            return true;
        }

        void NextVariance(float x, out float nextMean, out float nextVariance)
        {
            if (N == 0) { nextMean = x; nextVariance = 0.0f; }
            else
            {
                float n = (float)N;
                nextMean = (mean * n + x) / (n + 1f);
                nextVariance = n * variance / (n + 1) + (float)Math.Pow(x - mean, 2) / (n + 1) - (float)Math.Pow(nextMean - mean, 2);
            }
        }

        public SKPointI[] GetBBox()
        {
            return isHorizontal?new SKPointI[] { new SKPointI(xIn - 1, yIn), new SKPointI(xEnd - 1, yEnd)}:
                new SKPointI[] { new SKPointI(yIn - 1, xIn), new SKPointI(yEnd - 1, xEnd) }; ;
        }


        public SKRect GetRectangle()
        {
            return isHorizontal? new SKRect(xIn, yIn, xEnd + 1, yEnd + 1):
                new SKRect(yIn, xIn, yEnd + 1, xEnd + 1); ;
        }

        public float Length
        {
            get
            {
                float ix = (float)(xEnd - xIn);
                float iy = (float)(yEnd - yIn);
                return (float)Math.Sqrt(ix * ix + iy * iy);
            }
        }

        const float PI = (float)Math.PI;
        public bool WhiteToBlack { get {return whiteToBlack; } }
        public SKPointI Center { get { 
            return isHorizontal? new SKPointI((xIn + xEnd) / 2, (yIn + yEnd) / 2):
                new SKPointI((yIn + yEnd) / 2, (xIn + xEnd) / 2);
        }}
        public SKPointI In { get { 
            return isHorizontal? new SKPointI(xIn, yIn) : new SKPointI(yIn, xIn); 
        } }

        public SKPointI End { get { return isHorizontal?new SKPointI(xEnd, yEnd):new SKPointI(yEnd, xEnd); } }

        public float Angle
        {
            get
            {
                if (this.recalcAngle)
                {
                    MyPoint a = In;
                    MyPoint b = End;
                    MyVector vd = b-a;
                    this.angle = vd.Angle;
                    this.recalcAngle = false;
                }
                return this.angle;
            }
        }

        public override String ToString()
        {
            return (isHorizontal?
                "In(" + xIn + "," + yIn + ") - Fi(" + xEnd + "," + yEnd + ")":
                "In(" + yIn + "," + xIn + ") - Fi(" + yEnd + "," + xEnd + ")"
                )+"var:"+this.variance;
        }

        public int CompareTo(Object o)
        {
            EdgeVar p = (EdgeVar)o;
            return (int)((p.x - this.x) * 10);
        }

        public float Variance { get { return this.variance; } }
    }

    class EdgeLengthComparer :System.Collections.IComparer
    {
        public int Compare(object a, object b)
        {
            EdgeVar aa = (EdgeVar)a;
            EdgeVar bb = (EdgeVar)b;

            return aa.Length>bb.Length?1:aa.Length==bb.Length?0:-1;
        }
    }
}
