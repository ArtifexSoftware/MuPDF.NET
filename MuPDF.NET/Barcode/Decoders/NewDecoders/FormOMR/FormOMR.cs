using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.FormOMR
{
    //Base class used to find shapes (circles, ovals and squares) in an image.
    //Uses an slicer to find segments of the image (segments are connected black pixels). For each segment, 
    //the algorithm finds the outline and subclasses will check if this outline has a concrete shape.
#if CORE_DEV
    public
#else
    internal
#endif
    abstract partial class FormOMR : SymbologyReader2D
    {
		protected ImageScaner scan;
        Slicer slicer;
        ArrayList result;

        protected int minRad = 5;
        protected int maxRad = 300;
        protected int minDist = 2;  //default: connect 1 pixel separate segments
        protected float minRatio = 0.5f; //min shortSide/largeSide
        protected float maxRatio = float.MaxValue; //max shortSide/largeSide

        protected int whiteZone = 5; //default no white zone
        protected float PI = (float)Math.PI;

        /// <summary>
        /// Enables extended mode.
        /// 
        /// This mode finds template in recognized segements.
        /// And then finds unrecognized segments that are like the template.
        /// This method can find partially filled, strike outed sqaures and circles.
        /// 
        /// This mode is enough long!
        /// </summary>
        public bool ExtendedMode = false;
            
        protected override FoundBarcode[] DecodeBarcode()
        {
            scan = new ImageScaner(BWImage);
            slicer = new Slicer(minDist, minRad, maxRad, 45000, minRatio, maxRatio, true);
            result = new ArrayList();
            recognizedSegments.Clear();
            unrecognizedSegments.Clear();
#if DEBUG_IMAGE
            //bwImage.GetAsBitmap().Save(@"out.png");
#endif
            //Get an array of all segments meeting minDist, minRad, maxRad restrictions
            ArrayList segments = slicer.GetParts(BWImage);

            ScanParts(segments);

            if (ExtendedMode)
                ExtendedSearch();

            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }

        private List<Segment> recognizedSegments = new List<Segment>();
        private List<Segment> unrecognizedSegments = new List<Segment>();

        //debug method to show all segments, and matching symbol.
        protected const int N=24;

        void ScanParts(ArrayList parts)
        {
            foreach (Segment p in parts)
            if (p != null)
            {
                if (CheckSegment(p) && checkWhiteZone(p))
                {
                    recognizedSegments.Add(p);
                    AddFoundBarcode(p);
                }else
                {
                    unrecognizedSegments.Add(p);
                }
            }
        }

        private void AddFoundBarcode(Segment p)
        {
            FoundBarcode foundBarcode = new FoundBarcode();

            if (this is RawSlicer)
                foundBarcode.BarcodeFormat = SymbologyType.Segment;
            else if (this is FormOMRCircle)
                foundBarcode.BarcodeFormat = SymbologyType.Circle;
            else if (this is FormOMROval)
                foundBarcode.BarcodeFormat = SymbologyType.Oval;
            else if (this is FormOMRSquare)
                foundBarcode.BarcodeFormat = SymbologyType.Checkbox;

            foundBarcode.Color = Color.Orange;
            foundBarcode.Polygon = p.GetBBox();
            foundBarcode.Rect = p.GetRectangle();
            foundBarcode.Value = IsFilled(p) ? "1" : "0";
            result.Add(foundBarcode);
        }


        //finds a discretized outline of the segment and calls derived classes to check the outline.
        //The outline is defined by N points, corresponding to N arcs of 2PI/N radians.
        bool CheckSegment(Segment p)
        {
            MyPointF center=p.CenterF;
            float w = (float)p.Width / 2f;
            float h = (float)p.Height / 2f;

            //find outline of the segment
            float[] outline = new float[N];
            MyPoint[] pOut = new MyPoint[N];
            for (int i=0;i<outline.Length;i++) outline[i]=0f;
            float cornerAngle = (float)Math.Atan2(h, w);
            foreach (MyPointF o in p.Points)
            {
                MyPointF oo = new MyPointF(o.X + 0.5f, o.Y + 0.5f);
                float angle = (oo - center).Angle;
                if (angle < 0) angle += (float)Math.PI * 2f;
                float d = (oo - center).Length;
                int discreteAngle = 0;
                if (angle < cornerAngle) 
                    discreteAngle = (int)Math.Truncate(angle * 3f / cornerAngle);
                else if (angle < PI - cornerAngle) 
                    discreteAngle = 3 +  (int)Math.Truncate((angle - cornerAngle) * 6f / (PI - 2f * cornerAngle));
                else if (angle < PI + cornerAngle) 
                    discreteAngle = 9 + (int)Math.Truncate((angle - (PI - cornerAngle)) * 6f / (2f * cornerAngle));
                else if (angle < 2f*PI - cornerAngle) 
                    discreteAngle = 15 + (int)Math.Truncate((angle - (PI + cornerAngle)) * 6f / (PI - 2f * cornerAngle));
                else 
                    discreteAngle = 21 + (int)Math.Truncate((angle - (2f*PI - cornerAngle)) * 3f / cornerAngle);

                if (d > outline[discreteAngle]) { outline[discreteAngle] = d; pOut[discreteAngle] = o; }
            }

#if DEBUG
            foreach (var o in p.Points)
                DebugHelper.DrawSquare(Color.Blue, o);

            DebugHelper.DrawSquare(Color.Red, p.Center);
#endif

            return CheckOutline(p, outline);
        }

        abstract protected bool CheckOutline(Segment p, float[] outline);

        //checks if the segment is filled or not. This algorithm is an approximation that seems to work.
        //It only scans the circle interior to the segment. 
        virtual protected bool IsFilled(Segment p)
        {
            MyPointF center = p.CenterF;
            float w = (float)p.Width / 2f;
            float h = (float)p.Height / 2f;
            int R = (int)Math.Truncate((w < h ? w : h)*0.6f);

            int n = 0, black=0;
            for (int y = -R; y <= R; y++)
            {
                int x = (int)Math.Truncate(Math.Sqrt(R * R - y * y));
                Bresenham br = new Bresenham(new MyPointF(center.X - x, center.Y + y), new MyPointF(center.X + x, center.Y + y));
                while (!br.End())
                {
                    if (scan.isBlack(br.Current)) black++;
                    n++;
                    br.Next();
                }
            }
            return (float)black / (float)n > 0.1f;
        }

        //check a white zone around the segment, actually only to the left and right of the segment.
        bool checkWhiteZone(Segment p)
        {
            if (whiteZone == -1) return true;
            bool white = true;
            Bresenham br = new Bresenham(p.LU, p.LD);
            MyPoint q=p.LU; q.X--;
            for (int i = 0; i < whiteZone && white; i++)
            {
                br.MoveTo(q);
                while (white && !br.End()) if (scan.isBlack(br.Current)) white = false; else br.Next();
                q.X--;
            }
            br = new Bresenham(p.RU, p.RD);
            q = p.RU; q.X++;
            for (int i = 0; i < whiteZone && white; i++)
            {
                br.MoveTo(q);
                while (white && !br.End()) if (scan.isBlack(br.Current)) white = false; else br.Next();
                q.X++;
            }
            return white;
        }     

        public int MinRad { get { return minRad; } set { minRad = value; } }
        public int MaxRad { get { return maxRad; } set { maxRad = value; } }
        public int MinDist { get { return minDist; } set { minDist = value; } }
        public int WhiteZone { get { return whiteZone; } set { whiteZone = value; } }
    }
}
