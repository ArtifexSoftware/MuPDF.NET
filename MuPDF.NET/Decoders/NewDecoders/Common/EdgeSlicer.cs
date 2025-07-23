using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BarcodeReader.Core.Common
{
    internal class EdgeSlicer
    {
        int width, height;
        int minRad, maxRad;
        bool scanRows, scanColumns;
        LinkedList<Edge> edges; // array with results
        Edge[] prevRow; //segment indexs from the last minDist rows, 
                          //to allow connecting segments in the current row with those at minDist
        Edge[] currentRow; //segment indexs of the current processing row

        public EdgeSlicer(int minRad, int maxRad) { newEdgeSlicer(minRad,maxRad,true,true); }
        public EdgeSlicer(int minRad, int maxRad, bool scanColumns) { newEdgeSlicer(minRad, maxRad, true, scanColumns); }
        public EdgeSlicer(int minRad, int maxRad, bool scanRows, bool scanColumns) { newEdgeSlicer(minRad, maxRad, scanRows, scanColumns); }
        protected void newEdgeSlicer(int minRad, int maxRad, bool scanRows, bool scanColumns)
        {
            this.minRad = minRad;
            this.maxRad = maxRad;
            this.scanRows = scanRows;
            this.scanColumns = scanColumns;
        }

        public LinkedList<Edge> GetEdges(BlackAndWhiteImage img)
        {            
            this.width = img.Width;
            this.height = img.Height;
            edges = new LinkedList<Edge>();
            
            //scan rows
            if (scanRows)
            {
                currentRow = new Edge[width];
                prevRow = new Edge[width];
                for (int i = 0; i < width; i++) prevRow[i] = null;
                for (int y = 0; y < height; y += 1)
                    ScanRow(y, img.GetRow(y), true);
            }
            
            //scan cols
            if (scanColumns)
            {
                currentRow = new Edge[height];
                prevRow = new Edge[height];
                for (int i = 0; i < height; i++) prevRow[i] = null;
                for (int x = 0; x < width; x += 1)
                    ScanRow(x, img.GetColumn(x), false);
            }
            
            //filter parts
            LinkedList<Edge> filtered = new LinkedList<Edge>();
            foreach (Edge p in edges)
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
                if (isBlackPrev ^ isBlackCurrent) //only process edges
                {

                    //look in the prev row for a connected edge.
                    Edge found=null;
                    for (int xi = x - 1; xi <= x + 1 && xi < length; xi++) if (xi>=0)
                    {
                        Edge e = prevRow[xi];
                        if (e != null && e.WhiteToBlack == isBlackCurrent)
                        {
                            if (e.Add(x, y))
                            {
                                found = e;
                                break;
                            }
                        }
                    }

                    if (found == null) //no connected edge is found, so add a new edge
                    {
                        found = new Edge(x, y, isBlackCurrent, isHorizontal);
                        edges.AddLast(found);
                    }

                    currentRow[x] = found;
                } else currentRow[x] = null;
                isBlackPrev = isBlackCurrent;
            }

            //move index rows
            Edge[] tmp = prevRow;
            prevRow = currentRow;
            currentRow = tmp;
        }    
    }

	internal class Edge : IComparable
    {
        bool scaned;
        float x, angle;
        int xIn, xEnd, yIn, yEnd, min, max;
        bool whiteToBlack, isHorizontal;

        public Edge(int x, int y, bool whiteToBlack, bool isHorizontal)
        {
            angle = float.MinValue;
            scaned = false;
            min = max = x;
            this.whiteToBlack = whiteToBlack;
            this.isHorizontal = isHorizontal;
            if (!isHorizontal) { int tmp = x; x = y; y = tmp; }//swap x,y
            xIn = xEnd = x;
            yIn = yEnd = y;
        }

        //for every new pixel added, the bounding box is updated
        public bool Add(int x, int y)
        {
            if (isHorizontal)
            {
                if (x < xIn) { if (x > min + 2) return false; }
                else if (x < max - 2) return false;
                if (x > max) max = x;
                if (x < min) min = x;
            }
            else
            {
                int tmp = x; x = y; y = tmp; //swap x,y
                if (y < yIn) { if (y > min + 2) return false; }
                else if (y < max - 2) return false;
                if (y > max) max = y;
                if (y < min) min = y;
            }

            xEnd = x;
            yEnd = y;
            return true;
        }

        public SKPoint[] GetBBox()
        {
            return new SKPoint[] { new SKPoint(xIn - 1, yIn), new SKPoint(xEnd - 1, yEnd)};
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
        public int Width { get { return xEnd - xIn + 1; } }
        public int Height { get { return yEnd - yIn + 1; } }
        public bool WhiteToBlack { get {return whiteToBlack; } }
        public bool Scaned { get {return scaned; } set { scaned=value;} }
        public float X { get { return x; } set { x = value; } }
        public float Angle { get {
            if (angle == float.MinValue)
            {
                angle = (float)Math.Atan2((double)(yEnd - yIn), (double)(xEnd - xIn));
                if (angle > PI) angle -= PI;
                else if (angle < 0) angle += PI;
            }
            return angle;
        } }
        public float Perpendicular { get { float p = Angle + PI / 2F; if (p > PI) p -= PI; return p; } }
        public SKPoint Center { get { return new SKPoint((xIn + xEnd) / 2, (yIn + yEnd) / 2); } }
        public SKPoint In { get { return new SKPoint(xIn, yIn); } }
        public SKPoint End { get { return new SKPoint(xEnd, yEnd); } }

        public float Dist(Edge p)
        {
            SKPoint b = p.Center;
            SKPoint a = this.Center;
            return (float)(Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)));
        }

        public override String ToString()
        {
            return "X:" + x + " In(" + xIn + "," + yIn + ") - Fi(" + xEnd + "," + yEnd + ")";
        }

        public int CompareTo(Object o)
        {
            Edge p = (Edge)o;
            return (int)((p.x - this.x) * 10);
        }
    }
}
