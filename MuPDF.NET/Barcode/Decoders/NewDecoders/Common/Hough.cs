using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BarcodeReader.Core.Common
{
    //Implementation of the discrete Hough transform used to find aligned points in 
    // a 2D plane. 
    internal class Hough
    {
        int width, height, diagonal;
        int hWidth, hHeight;
        float angleInc, distInc;
        int count;
        HoughCell[][] h;
        int maxH;
        float[] cosO, sinO;

        //width and height: image size
        //hWidth, hHeight: level of discretization of the hough transform
        //hWidth: angle discretization
        //hHeight: distance discretization
        public Hough(int width, int height, int hWidth, int hHeight)
        {
            this.width = width;
            this.height = height;
            this.diagonal = (int) Math.Ceiling(Math.Sqrt(width * width + height * height));

            this.hWidth = hWidth;
            this.hHeight = hHeight;

            this.angleInc = (float)Math.PI / (float)hWidth;
            this.distInc = (float) diagonal * 2F / (float)hHeight;

            this.h = new HoughCell[hWidth][]; //initialize hough resulting array
            this.cosO = new float[hWidth]; //precompute sin and cos tables
            this.sinO = new float[hWidth];
            double angle = 0.0, inc = Math.PI / (double)hWidth;
            for (int i = 0; i < hWidth; i++)
            {
                h[i] = new HoughCell[hHeight];
                cosO[i] = (float)Math.Cos(angle);
                sinO[i] = (float)Math.Sin(angle);
                angle += inc;
            }
            this.maxH = 1;
            this.count = 0;
        }

        public void Clean()
        {
            for (int i=0;i<hWidth;i++)
                Array.Clear(h[i], 0, hHeight);
            maxH = 1;
            count = 0;
        }

        //Distance from p to the line that represents the cell.
        public float Dist(HoughCell cell, SKPointI p)
        {
            return (float)p.X * cosO[cell.Angle] + (float)p.Y * sinO[cell.Angle];
        }

        //Distance from p to the origin of coordinates projected to the line that represents the cell.
        //Used to sort points in a cell, from left to right
        public float XDist(HoughCell cell, SKPointI p)
        {
            return +(float)p.Y * cosO[cell.Angle] - (float)p.X * sinO[cell.Angle];
        }

        //Add a point to the cendidate cells
        public void Add(SKPointI p, int inc, object o)
        {
            Add(p, inc, o, 0, hWidth);
        }

        public void Add(SKPointI p, int inc, object o, int angleIn, int angleEnd)
        {
            for (int i = angleIn; i < angleEnd; i++)
            {
                float D = ((float)p.X * cosO[i] + (float)p.Y * sinO[i]) / distInc;
                int d = (int)(Math.Floor((double)(D)));
                float remainder = D - (float)d;

                d += hHeight / 2;

                Add(i, d, inc, o);
                if (remainder < 0.2F) Add(i, d - 1, inc-1, o);
                else if (remainder > 0.8F) Add(i, d + 1, inc-1, o);
            }
        }

        //Add all points of an edge from which we know its angle
        const int ANGLE_INTERVAL = 3;
        public void Add(SKPointI pIn, SKPointI pEnd, int inc, object o, int iAngle)
        {
            int angleIn = iAngle - ANGLE_INTERVAL; if (angleIn < 0) angleIn = 0;
            int angleEnd = iAngle + ANGLE_INTERVAL + 1; if (angleEnd > hWidth) angleEnd = hWidth;

            for (int i = angleIn; i < angleEnd; i++)
            {
                float DIn = ((float)pIn.X * cosO[i] + (float)pIn.Y * sinO[i]) / distInc;
                float DEnd = ((float)pEnd.X * cosO[i] + (float)pEnd.Y * sinO[i]) / distInc;
                int dIn = (int)(Math.Floor((double)(DIn)));
                int dEnd = (int)(Math.Floor((double)(DEnd)));
                if (dIn > dEnd) { int tmp = dIn; dIn = dEnd; dEnd = tmp; }

                dIn += hHeight / 2;
                dEnd += hHeight / 2;

                int dist = i - iAngle; dist *= dist;
                //int iInc = inc / (1+dist);

                int iInc = ANGLE_INTERVAL*ANGLE_INTERVAL + 1 - dist;
                for (int d=dIn; d<=dEnd; d++) Add(i, d, iInc, o);
            }
        }

        void Add(int angle, int d, int inc, object o)
        {
            if (h[angle][d] == null) { h[angle][d] = new HoughCell(angle, d); count++; }
            int n = h[angle][d].Add(inc, o);
            if (n > maxH) maxH = n;
        }

        //Add a point to the candidate cells knowing its direction
        public void Add(SKPointI pIn, SKPointI pEnd, float angle, int inc, object o)
        {
            if (angle<0F) angle+=(float)Math.PI;
            int iAngle = (int)Math.Floor(angle * (float)hWidth / (float)Math.PI);
            Add(pIn, pEnd, inc, o, iAngle);
        }

        //debug method to get an image of the hough transform
        public SKBitmap GetImage()
        {
            var bmp = new SKBitmap(hWidth, hHeight);
            using (var canvas = new SKCanvas(bmp))
            {
                canvas.Clear(SKColors.Black);

                // Draw yellow cross lines
                using (var yellowPaint = new SKPaint
                {
                    Color = SKColors.Yellow,
                    StrokeWidth = 1,
                    IsAntialias = true
                })
                {
                    canvas.DrawLine(new SKPoint(0, hHeight / 2), new SKPoint(hWidth, hHeight / 2), yellowPaint);
                    canvas.DrawLine(new SKPoint(hWidth / 2, 0), new SKPoint(hWidth / 2, hHeight), yellowPaint);
                }

                int minAng = -1, minCount = hHeight;

                for (int i = 0; i < hWidth; i++)
                {
                    int count = 0;
                    for (int d = 0; d < hHeight; d++)
                    {
                        if (h[i][d] != null && h[i][d].Count > 0)
                        {
                            count++;
                            int g = h[i][d].Count * 255 / maxH;
                            g *= 4;
                            if (g > 255) g = 255;
                            if (g > 0)
                            {
                                // Draw pixel in grayscale
                                bmp.SetPixel(i, d, new SKColor((byte)g, (byte)g, (byte)g));
                            }
                        }
                    }
                    if (count < minCount)
                    {
                        minCount = count;
                        minAng = i;
                    }
                }

                // Draw the angle text
                using (var textPaint = new SKPaint
                {
                    Color = SKColors.Blue,
                    TextSize = 14,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial")
                })
                {
                    string text = ((float)minAng * 180F / (float)hWidth).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    canvas.DrawText(text, 10, 20, textPaint);
                }

                return bmp;
            }
        }
        /*
        public SKBitmap GetImage()
        {
            SKBitmap bmp = new SKBitmap(hWidth, hHeight);
            Graphics gc = Graphics.FromImage(bmp);
            gc.FillRectangle(Brushes.Black, 0, 0, hWidth, hHeight);
            gc.DrawLine(Pens.Yellow, new SKPointI(0, hHeight / 2), new SKPointI(hWidth, hHeight / 2));
            gc.DrawLine(Pens.Yellow, new SKPointI(hWidth / 2, 0), new SKPointI(hWidth / 2, hHeight));
            int minAng = -1, minCount = hHeight;
            for (int i = 0; i < hWidth; i++)
            {
                int count = 0;
                for (int d = 0; d < hHeight; d++) if (h[i][d] != null && h[i][d].Count > 0)
                    {
                        count++;
                        int g = h[i][d].Count * 255 / maxH;
                        g *= 4; if (g > 255) g = 255;
                        if (g > 0) bmp.SetPixel(i, d, Color.FromArgb(g, g, g));
                    }                
                if (count<minCount) {minCount=count; minAng=i;}
            }
            gc.DrawString(Convert.ToString((float)minAng*180F/(float)hWidth),new Font("Arial",10),Brushes.Blue, new PointF(10,10));
            gc.Dispose();
            return bmp;            
        }
        */

        //Sort all cells in the hough space. This allows to process first cells 
        //with more candidates.
        public HoughCell[] Sort()
        {
            int n=0;
            HoughCell[] cells=new HoughCell[count];
            for (int i=0;i<hWidth;i++) 
                for (int d=0;d<hHeight;d++)
                    if (h[i][d]!=null) cells[n++]=h[i][d];
            Array.Sort(cells);
            return cells;
        }

        static readonly float PI_2 = (float)Math.PI / 2F;
        static readonly float PI = (float)Math.PI;
        //debug method to display the bounding box of a cell.
        public SKPointI[] GetBBox(HoughCell c)
        {
            float a = (float)c.Angle * angleInc; //0..PI
            float d = (float)(c.Distance - hHeight/2)* distInc; //-diagonal..diagonal
            int xIn = 0, yIn = 0, xEnd = 0, yEnd = 0;

            if (a >= 0F && a <= PI_2)
            {
                float cos = (float)Math.Cos(a); if (cos < 0) cos = 1e-15F;
                float sin = (float)Math.Sin(a); if (sin < 0) sin = 1e-15F;
                float dw = (float)width * cos;
                float dh = (float)height * sin;
                if (dw < dh)
                    if (d < dw) { yIn = (int)(d / sin); xEnd = (int)(d / cos); }
                    else if (d < dh)
                    {
                        yIn = (int)(d / sin);
                        yEnd = (int)((d - (float)width * cos) / sin); xEnd = width;
                    }
                    else
                    {
                        xIn = (int)((d - (float)height * sin) / cos); yIn = height;
                        yEnd = (int)((d - (float)width * cos) / sin); xEnd = width;
                    }
                else //dh<dw
                    if (d < dh) { yIn = (int)(d / sin); xEnd = (int)(d / cos); }
                    else if (d < dw)
                    {
                        xIn = (int)((d - (float)height * sin) / cos); yIn = height;
                        xEnd = (int)(d / cos);
                    }
                    else
                    {
                        xIn = (int)((d - (float)height * sin) / cos); yIn = height;
                        yEnd = (int)((d - (float)width * cos) / sin); xEnd = width;
                    }
            }
            else if (a > PI_2 && a <= PI)
            {
                float cos = (float)Math.Cos(a); if (cos > 0) cos = -1e-15F;
                float sin = (float)Math.Sin(a); if (sin < 0) sin = 1e-15F;
                float dwh = (float)width * cos + (float)height * sin;
 
                if (dwh<0F)
                    if (d < dwh) {  
                        xIn=(int)(d/cos) ; yIn=0;
                        xEnd = width; yEnd=(int)((d-(float)width*cos)/sin);
                    }
                    else if (d < 0F) 
                    {
                        xIn = (int)(d / cos); yIn = 0;
                        xEnd = (int)((d - (float)height * sin) / cos); yEnd = height;
                    }
                    else 
                    {
                        xIn = 0; yIn = (int)(d / sin);
                        xEnd = (int)((d - (float)height * sin) / cos); yEnd = height;
                    }
                else
                    if (d < 0F) { 
                        xIn=(int)(d/cos); yIn=0;  
                        xEnd=width; yEnd=(int)((-d-(float)width*cos)/sin);
                    }
                    else if (d < dwh) 
                    {
                        xIn=0; yIn=(int)(d/sin);
                        xEnd=width; yEnd=(int)((d-(float)width*cos)/sin);
                    }
                    else 
                    { 
                        xIn=0;yIn=(int)(d/sin);
                        xEnd=(int)((d-(float)height*sin)/cos); yEnd=height;
                    }
            }
            return new SKPointI[] { new SKPointI(xIn, yIn), new SKPointI(xIn, yIn + (int)distInc), new SKPointI(xEnd, yEnd + (int)distInc), new SKPointI(xEnd, yEnd), new SKPointI(xIn, yIn) };
        }

    }

    //A cell in the hough space, defined by its angle (x coord) and dist (y coord)
    //Holds a list of all objects (Parts) and an object counter and a weight accumulator.
    //Weight allows to sort cells not only for the number of contained objects (for example, 
    //if we want to priorize big sized parts...).
	internal class HoughCell : IComparable
    {
        int angle,dist;
        int count, weight;
        LinkedList<object> objects;
        public HoughCell(int angle, int dist)
        {
            this.angle = angle;
            this.dist = dist;
            this.count = this.weight = 0;
            this.objects = new LinkedList<object>();
        }
        public int Add(int n, object o)
        {
            count += 1; 
            weight+= n;
            if (o!=null) objects.AddLast(o);
            return count;
        }

        public int CompareTo(object o) {
            HoughCell c = (HoughCell)o;
            return c.weight- this.weight;
        }

        public int Count { get { return count; } }
        public int Angle { get { return angle; } }
        public int Distance { get { return dist; } }
        public LinkedList<object> Objects { get { return objects; } }
    }
}
