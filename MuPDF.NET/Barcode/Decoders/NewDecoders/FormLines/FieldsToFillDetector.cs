using System;
using System.Collections;
using System.Drawing;
using SkiaSharp;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.FormLines
{
    //Class to detect lines in a document with a white area above.
    //The area of the box must be between minHeight and maxHeight, otherwise they are discarded.
    //In a posproces, boxes distant <distanceToJoin are joined.
    //The algorithm uses lines returned by HorizontalLinesDecoder.
#if CORE_DEV
    public
#else
    internal
#endif
    class FieldsToFillDetector : HorizontalLinesDecoder
    {
        int minHeight = 20;  //minimun height of the area above the line.
        int maxHeight = 40; //maximum height of the area above the line.
        ArrayList result; //array to hold all found results

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.UnderlinedField;
		}

		protected override FoundBarcode[] DecodeBarcode()
        {
            FoundBarcode[] lines = base.DecodeBarcode(); //find all lines
            result = new ArrayList();
            foreach (FoundBarcode r in lines)
                CheckBox(r.Polygon[0], r.Polygon[1]); //check if the line has a white area above
            //remove to short //or too hight boxes
            for (int i = result.Count - 1; i >= 0; i--)
            {
                FoundBarcode f = (FoundBarcode)result[i];
                MyPoint a = f.Polygon[0];
                MyPoint b = f.Polygon[1];
                MyPoint c = f.Polygon[2];
                int width = (int)Math.Round((b - a).Length);
                //int height = (int)Math.Round((c - b).Length);   //uncomment to remove boxes with height>maxHeight
                if (width < minRad /*|| height >= maxHeight*/) result.RemoveAt(i);
            }
            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }

        //Main method to find boxes. Starting from a line from a to b, first searches how many
        //white pixels there are on top of each pixel of the line a-b. This is stored in the array height.
        //In a second step, loops trough the height array looking for consistent parts, i.e grouped heights
        //between minHeight and maxHeight.
        void CheckBox(MyPoint a, MyPoint b)
        {
            //first step: find the height array, and also bottom and top points
            MyVectorF vdX = (b - a); vdX = vdX.Normalized;
            MyVectorF vdY = vdX.Perpendicular;
            Bresenham br = new Bresenham(a, vdY);
            Bresenham brX = new Bresenham(a, b);
            MyPoint[] bottom = new MyPoint[brX.Steps + 1];
            MyPoint[] top = new MyPoint[brX.Steps + 1];
            int[] height = new int[brX.Steps + 1];
            int x = 0;
            while (!brX.End())
            {
                int h = 0;
                bottom[x] = brX.Current;
                br.MoveTo(brX.Current);
                br.Next();
                while (scan.In(br.Current) && scan.isBlack(br.Current) && h < minRad) { br.Next(); h++; } //skip line black pixels
                while (scan.In(br.Current) && !scan.isBlack(br.Current) && h < maxHeight) { br.Next(); h++; } //scan white pixels
                top[x] = br.Current;
                height[x] = h;
                brX.Next();
                x++;
            }

            //second step: loop through the height array to find clusters of heights >minHeight and <maxHeight
            int x0, segmentMinHeight;
            x0 = 0; segmentMinHeight = maxHeight;
            for (x = 0; x <= height.Length; x++) //<=height.length to process the last cluster
            {
                //Initially only divide the line when height<minHeight. This allows to join an area >maxHeight
                //with a correct height area.
                if (x == height.Length || height[x] < minHeight) //division
                {
                    if (x > x0) //has more than 1 pixel
                    {
                        //find segmentMinHeight discarding both extremes (minDist)
                        segmentMinHeight=maxHeight;
                        for (int i=x0+minRad; i<x-minRad;i++)
                            if (segmentMinHeight > height[i]) segmentMinHeight = height[i];

                        //join with prev boxes, if close enough
                        MyVector up = (MyVector)(vdY * (float)segmentMinHeight); //vector height
                        MyPoint[] polygon = new MyPoint[] { bottom[x0], bottom[x - 1], bottom[x - 1] + up, bottom[x0] + up };

                        for (int i=result.Count-1; i>=0; i--)
                        {
                            FoundBarcode f = (FoundBarcode)result[i];
                            MyPoint q0 = f.Polygon[0]; //left bottom point
                            MyPoint q1 = f.Polygon[1]; //right bottom point
                            MyPoint q2 = f.Polygon[2]; //right top point (to calculate vdY)
                            if ((int)((polygon[0] - q1).Length) < minRad && //join to a left previous box
                                isWhite(polygon[0], q1, vdY, segmentMinHeight)) //the area in between is white
                            {
                                float prevHeight = (q2 - q1).Length;
                                float newHeight = (float)segmentMinHeight > prevHeight ? prevHeight : (float)segmentMinHeight;
                                up = (MyVector)(vdY * newHeight); //vector height
                                segmentMinHeight = (int)newHeight;
                                
                                //update Polygon to join previous box
                                polygon[0] = f.Polygon[0];
                                polygon[2] = polygon[1] + up;
                                polygon[3] = polygon[0] + up;
                                //remove previous box
                                result.RemoveAt(i); 
                            }
                            else if ((int)((polygon[1] - q0).Length) < minRad && //join to a right previous box
                                isWhite(polygon[1], q0, vdY, segmentMinHeight)) //the area in between is white
                            {
                                float prevHeight = (q2 - q1).Length;
                                float newHeight = (float)segmentMinHeight > prevHeight ? prevHeight : (float)segmentMinHeight;
                                up = (MyVector)(vdY * newHeight); //vector height
                                segmentMinHeight = (int)newHeight;

                                //update Polygon to join previous box
                                polygon[1] = f.Polygon[1];
                                polygon[2] = polygon[1] + up;
                                polygon[3] = polygon[0] + up;
                                //remove previous box
                                result.RemoveAt(i); 
                            }
                        }

                        FoundBarcode foundBarcode = new FoundBarcode();
						foundBarcode.BarcodeFormat = SymbologyType.UnderlinedField;
						foundBarcode.Value = "box";
                        foundBarcode.Polygon = new SKPointI[] { polygon[0], polygon[1], polygon[2], polygon[3], polygon[0] };
                        foundBarcode.Color = SKColors.Blue;
						//byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
						//GraphicsPath path = new GraphicsPath(foundBarcode.Polygon, pointTypes);
						//foundBarcode.Rect = Rectangle.Round(path.GetBounds());
                        foundBarcode.Rect = Utils.DrawPath(foundBarcode.Polygon);
                        result.Add(foundBarcode);
                    }
                    x0 = x + 1;
                }                
            }
        }


        //scans a region to detect blackpixels, with noise tolerance
        bool isWhite(MyPoint a, MyPoint b, MyVectorF vdY, int height)
        {
            int n = 0; //black pixels counter
            Bresenham br = new Bresenham(a, b);
            Bresenham up = new Bresenham(br.Current, vdY);
            while (!br.End())
            {
                int i = 0;
                up.MoveTo(br.Current);
                while (i < height)
                {
                    if (scan.isBlack(up.Current)) n++;
                    up.Next();
                    i++;
                }
                br.Next();
            }
            return n < height / 2; //allow a level of black pixels
        }

        public int MinHeight { get { return minHeight; } set { minHeight = value; } }
        public int MaxHeight { get { return maxHeight; } set { maxHeight = value; } }
    }
}
