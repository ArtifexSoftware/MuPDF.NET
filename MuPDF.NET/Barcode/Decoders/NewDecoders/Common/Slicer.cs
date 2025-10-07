using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace BarcodeReader.Core.Common
{
	/// <summary>
	/// Slicer extract connected black pixels segments (or Parts) from a black/white image. 
	/// Main parameters are:
	/// - minRad and maxRad: to discard too small or too big segments
	/// - minDist: to connect unconnected segments
	/// - maxEntropy: to discard too noisy segments
	///
	/// Scans the image row by row and assign an identifier to each pixel. Connected pixels 
	/// are assigned with the same identifier. The algorithm uses a cache of the last minDist
	/// processed rows (prevRows) to find pixel connections and assign the appropiate id.
	/// An important step in this algorithm is when two different segments A and B joins. When it happens, 
	/// B's index is moved to A's index, and B's index in no more used. 
	/// </summary>
	internal class Slicer
    {
        int minDist, minRad, maxRad, maxEntropy; //main parameters
        float aspect;  // min incX/incY
        float aspectMax; // max incX/incY
        bool recordPoints;
        int width, height; //image size
        ArrayList parts; // array with results
        int[][] prevRows; //segment indexs from the last minDist rows, 
                          //to allow connecting segments in the current row with those at minDist
        int[] currentRow; //segment indexs of the current processing row

        /// <summary>
        /// Slicer object
        /// Set aspectMax = 0f to skip checking for maxAspect
        /// </summary>
        /// <param name="minDist"></param>
        /// <param name="minRad"></param>
        /// <param name="maxRad"></param>
        /// <param name="maxEntropy"></param>
        /// <param name="aspect"></param>
        /// <param name="aspectMax"></param>
        /// <param name="recordPoints"></param>
        public Slicer(int minDist, int minRad, int maxRad, int maxEntropy, float aspect, float aspectMax, bool recordPoints)
        {
            this.minRad = minRad;
            this.maxRad = maxRad;
            this.minDist = minDist;
            this.maxEntropy = maxEntropy;
            this.prevRows = new int[minDist][];
            this.aspect = aspect;
            this.aspectMax = aspectMax;
            this.recordPoints = recordPoints;
        }

        public ArrayList GetParts(BlackAndWhiteImage img)
        {            
            this.width = img.Width;
            this.height = img.Height;

            for (int k = 0; k < minDist; k++)
            {
                prevRows[k] = new int[width];
                for (int i = 0; i < width; i++) prevRows[k][i] = -1;
            }

            currentRow = new int[width];
            parts = new ArrayList();

            for (int y = 0; y < height; y += 1)
                ScanRow(y, img.GetRow(y));

            //filter parts
            ArrayList filtered = new ArrayList();
            foreach (Segment p in parts)
            {
                if (p == null)
                    continue;

                bool widthAndHeightAreEqual = p.Width == p.Height;
                float aspectCalculated = (float)p.Width / (float)p.Height;
                float aspectCalculated2 = (float)p.Height / (float)p.Width;

                if (
                    (p.Width > minRad || p.Height > minRad) &&
                    (p.Width < maxRad && p.Height < maxRad)
                    )
                {

                    bool canAdd = false;

                    // if aspectMax is set to zero so not checking it
                    if (aspectMax<float.Epsilon)
                    {
                     canAdd = (((p.Width <= p.Height && (float)p.Width / (float)p.Height > aspect) ||
                       (p.Width >= p.Height && (float)p.Height / (float)p.Width > aspect))
                        && p.GetEntropy(img, maxEntropy) < maxEntropy);
                    }
                    else 
                    {
                         // checking if we can add based on AspectMax
                        canAdd = 
                                    // check for min ratio
                                (
                                    ((p.Width > p.Height || widthAndHeightAreEqual) && (aspectCalculated > aspect || widthAndHeightAreEqual))
                                    ||
                                    ((p.Width < p.Height || widthAndHeightAreEqual) && (aspectCalculated2 > aspect || widthAndHeightAreEqual))
                                )
                                &&
                                    // check for max ratio
                                (

                                    ((p.Width > p.Height || widthAndHeightAreEqual) && (aspectCalculated < aspectMax || widthAndHeightAreEqual))
                                    ||
                                    ((p.Height > p.Width || widthAndHeightAreEqual) && (aspectCalculated2 < aspectMax || widthAndHeightAreEqual))
                                )
                                && p.GetEntropy(img, maxEntropy) < maxEntropy;                    
                    };

                    if (canAdd)
                        filtered.Add(p);
                }
            }
            return filtered;
        }

        static bool FloatsAreEqual(float a, float b)
        {
            return Math.Abs(a - b) < float.Epsilon;
        }


        //Process a row. For each pixel in the row looks for a previously connected pixel. It if is
        //found, the pixel is added to its segment (or Part object). Otherwise, a new id (Part) is created.
        private void ScanRow(int y, XBitArray row)
        {
            for (int x = 0; x < width; x++)
            {
                int nPart = -1;
                if (row[x]) //only process black pixels
                {
                    nPart = -1; //by now, no connected pixel is found

                    //look in the left of the current row for a connected pixel.
                    for (int xi = x - 1; xi >= x - minDist && xi >= 0; xi--)
                        if (currentRow[xi] != -1)
                        {
                            nPart = currentRow[xi]; //a connected pixel if found, and assign its id
                            ((Segment)parts[nPart]).Add(x, y); //add the new pixel to the segment (Part).
                            break;
                        }

                    //look in the minDist previous rows for a connected pixel.
                    for (int iRow = 0; iRow < minDist; iRow++)
                        for (int xi = x - minDist; xi <= x + minDist; xi++) if (xi >= 0 && xi < width)
                            {
                                int prevPart = prevRows[iRow][xi];
                                if (prevPart != -1) //a connected pixel if found
                                    if (nPart == -1)
                                    {//if it is the first connected pixel found, add the current pixel to it.
                                        nPart = prevPart;
                                        ((Segment)parts[nPart]).Add(x, y);
                                    }
                                    else if (nPart != prevPart)
                                    {//if the current pixel is connected to + than 1 pixel, join parts
                                        ((Segment)parts[nPart]).Join((Segment)parts[prevPart]);
                                        //remove prevPart id, and move prevPart id to nPart in the prevRows
                                        parts[prevPart] = null;
                                        for (int k = 0; k < minDist; k++)
                                            for (int j = 0; j < width; j++)
                                                if (prevRows[k][j] == prevPart) prevRows[k][j] = nPart;
                                        for (int j = 0; j < x; j++) if (currentRow[j] == prevPart) currentRow[j] = nPart;

                                    }
                            }

                    if (nPart == -1) //no connected pixel is found, so add a new part
                    {
                        nPart = parts.Count;
                        parts.Add(new Segment(new MyPoint(x, y),recordPoints));
                    }
                }
                currentRow[x] = nPart;
            }
            //move index rows
            int[] tmp = prevRows[minDist - 1];
            for (int k = minDist - 1; k > 0; k--) prevRows[k] = prevRows[k - 1];
            prevRows[0] = currentRow; currentRow = tmp;
        }
    }

    //class to hold connected pixels. Actually it does not store the pixels, but its bounding box (xin, xend, yin, yend)
#if CORE_DEV
    public
#else
    internal
#endif
    class Segment : IComparable
    {
        bool scaned = false;
        int xIn, xEnd, yIn, yEnd;
        float x;
        int blackPixelsCounter=0;
        LinkedList<MyPoint> points;

        public Segment(MyPoint p, bool recordPoints)
        {
            xIn = xEnd = p.X;
            yIn = yEnd = p.Y;
            blackPixelsCounter = 1;
            if (recordPoints)
            {
                points = new LinkedList<MyPoint>();
                points.AddLast(p);
            }
        }

        //for every new pixel added, the bounding box is updated
        public void Add(int x, int y)
        {
            if (xIn > x) xIn = x;
            else if (xEnd < x) xEnd = x;

            if (yEnd < y) yEnd = y;

            blackPixelsCounter++;
            if (points != null) points.AddLast(new MyPoint(x, y));
        }

        //joining parts means updating their bounding boxes
        public void Join(Segment p)
        {
            if (xIn > p.xIn) xIn = p.xIn;
            if (xEnd < p.xEnd) xEnd = p.xEnd;
            if (yIn > p.yIn) yIn = p.yIn;
            if (yEnd < p.yEnd) yEnd = p.yEnd;
            blackPixelsCounter += p.blackPixelsCounter;
            if (points != null && p.points != null) foreach (MyPoint pp in p.points) points.AddLast(pp);
        }

        public SKPointI[] GetBBox()
        {
            return new SKPointI[] { new SKPointI((int)xIn - 1, (int)yIn - 1), new SKPointI((int)xEnd + 1, (int)yIn - 1), new SKPointI((int)xEnd + 1, (int)yEnd + 1), new SKPointI((int)xIn - 1, (int)yEnd + 1), new SKPointI((int)xIn - 1, (int)yIn - 1) };
        }

        //A simple measure of noise (or entropy). Simply count horizontal and vertical black-white intervals, 
        //for each row and column. For example, a totally black segment returns entropy 0...
        public int GetEntropy(BlackAndWhiteImage img, int maxEntropy)
        {
            int hIntervals = 0;
            int max = maxEntropy * (yEnd - yIn +1)/ 100;
            for (int y = yIn; y <= yEnd; y++)
            {
                XBitArray row= img.GetRow(y);
                bool isBlack=row[xIn];
                for (int x = xIn+1; x <= xEnd; x++)
                {
                    if (row[x]^isBlack) 
                    {
                        hIntervals++;
                        if (hIntervals > max) return maxEntropy;
                        isBlack = !isBlack;
                    }
                }
            }
            int vIntervals = 0;
            
            for (int x = xIn ; x <= xEnd; x++)
            {
                XBitArray col = img.GetColumn(x);
                bool isBlack = col[yIn];
                for (int y = yIn +1; y <= yEnd; y++)
                {
                    if (col[y] ^ isBlack)
                    {
                        vIntervals++;
                        if (vIntervals > max) return maxEntropy;
                        isBlack = !isBlack;
                    }
                }
            }
            //returns the average of vertical and horizontal entropy
            return (hIntervals * 100 / (yEnd - yIn + 1) + vIntervals * 100 / (xEnd - xIn + 1)) / 2;
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
        public bool Scaned { get { return scaned; } set { scaned = value; } }
        public float X { get { return x; } set { x = value; } }
        public int BlackPixelCounter { get { return blackPixelsCounter; } }

        public SKPointI Center { get { return new SKPointI((xIn + xEnd) / 2, (yIn + yEnd) / 2); } }
        public MyPointF CenterF { get { return new MyPointF((float)(xIn + xEnd + 1) / 2f, (float)(yIn + yEnd + 1) / 2f); } }
        public SKPointI LU { get { return new SKPointI(xIn, yIn); } }
        public SKPointI LD { get { return new SKPointI(xIn, yEnd); } }
        public SKPointI RU { get { return new SKPointI(xEnd, yIn); } }
        public SKPointI RD { get { return new SKPointI(xEnd, yEnd); } }
        public LinkedList<MyPoint> Points { get { return points; } }

        public float Dist(Segment p)
        {
            SKPointI b = p.Center;
            SKPointI a = this.Center;
            return (float)(Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)));
        }
 
        public int CompareTo(Object o)
        {
            Segment p = (Segment)o;
            return (int)((p.x - this.x) * 10);
        }

        public override String ToString()
        {
            return "X:" + x + " LU(" + XIn + "," + YIn + ")";
        }
    }

    class BBox
    {
        float d;
        MyPointF center;
        MyVectorF vd, vdY;
        float minX, maxX, minY, maxY;
        bool first;

        public BBox(MyPoint center, MyVector vd, float d)
        {
            this.d = d;
            this.center = center;
            this.vd = ((MyVectorF)vd) / vd.Length; //normalized
            this.vdY = this.vd.Perpendicular;
            this.first = true;
        }

        //returns the distance from p to the main axis, i.e the Y coordinate
        public float GetY(MyPointF p)
        {
            MyVectorF pc = (MyPointF)p - center;
            float dy = pc.X * vd.Y - pc.Y * vd.X;
            return dy;
        }

        public float GetHeight() { return maxY - minY; }

        //add a new point to the bounding box, i.e. simply update minx, miny, maxx, maxy when needed.
        public void Update(MyPoint p, ref float ymax, ref float ymin)
        {
            MyVectorF pc = (MyPointF)p - center;
            float dx = pc * vd;
            if (dx < minX || first) minX = dx;
            if (dx > maxX || first) maxX = dx;
            float dy = pc.X * vd.Y - pc.Y * vd.X;
            if (dy < minY || first) minY = dy;
            if (dy > maxY || first) maxY = dy;
            if (dy < ymin) ymin = dy;
            if (dy > ymax) ymax = dy;
            first = false;
        }

        public float GetArea()
        {
            return (float)(maxX-minX+1)*(float)(maxY-minY+1);
        }

        public SKRect GetRectangle()
        {
            return new SKRect(0, 0, (maxX - minX + 4*d) + 1, (maxY - minY) + 1);
        }

        bool axisDone = false;
        MyPointF O;

        // Returns the coordinate of a point (x,y) using the bbox axis. 
        public MyPointF GetPoint(int x, int y)
        {
            if (!axisDone)
            {
                O = center + vd * ( minX - d*2F) + vdY * maxY + new MyPointF(0.5F, 0.5F);
                axisDone = true;
            }
            return O + vd * (float)x - vdY * (float)y;
        }

        //returns the corners of the bounding box
        public SKPointI[] GetBBox()
        {
            MyVectorF up = vdY * maxY;
            MyVectorF down = vdY * minY;

            MyPointF l = center + vd * (minX - d);
            MyPointF lu = l + up;
            MyPointF ld = l + down;

            MyPointF r = center + vd * (maxX + d);
            MyPointF ru = r + up;
            MyPointF rd = r + down;

            return new SKPointI[] { (SKPointI)lu, (SKPointI)ld, (SKPointI)rd, (SKPointI)ru, (SKPointI)lu };
        }
    }
}
