using BarcodeReader.Core.Common;
using SkiaSharp;
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BarcodeReader.Core.FormLines
{
    //Class to detect horizontal lines in a document. It is based on Hough transform to detect 
    //subsets of aligned edges (transitions from white to black) in the image.
    //Then a time consuming algorithm is executed to find continous lines in each point (cell) of the hough transform.
    //Hough only detects aligned pixels, not continuous!
#if CORE_DEV
    public
#else
    internal
#endif
    class HorizontalLinesDecoder : LinesDecoder
    {
        public HorizontalLinesDecoder()
        {
            scanHorizonal = true; scanVertical = false;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.HorizontalLine;
		}
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class VerticalLinesDecoder : LinesDecoder
    {
        public VerticalLinesDecoder()
        {
            scanHorizonal = false; scanVertical = true;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.VerticalLine;
		}
    }


#if CORE_DEV
    public
#else
    internal
#endif
    abstract class LinesDecoder : SymbologyReader2D
    {
        ArrayList result;
        protected ImageScaner scan;
        protected int width, height; //width and height of the image
        protected int minRad = 5; //min long of accepted lines
        protected int maxRad = 5000; //max long of accepted lines
        protected int distToJoin = 20; //min dist to join segments that are aligned
        protected int minLength = 50; //min dist to join pixels during segmentation
        protected bool scanHorizonal=true, scanVertical=true;

		protected override FoundBarcode[] DecodeBarcode()
        {
            scan = new ImageScaner(BWImage);
			LineSlicer slicer = new LineSlicer(BWImage);
            ArrayList[] angles = slicer.GetLineEdges(minRad, maxRad, distToJoin, minLength, scanHorizonal, scanVertical);
            result = new ArrayList();
            foreach(ArrayList angle in angles)
                foreach (LineEdge edge in angle)
                {
                    FoundBarcode b = new FoundBarcode();
					b.BarcodeType = scanHorizonal ? SymbologyType.HorizontalLine : SymbologyType.VerticalLine;
                    b.Polygon = new SKPoint[] { edge.a, edge.b, edge.a};
                    b.Color = Color.Orange;
                    // Create an SKPath from the polygon
                    var path = new SKPath();
                    path.MoveTo(b.Polygon[0]);

                    for (int i = 1; i < b.Polygon.Length; i++)
                        path.LineTo(b.Polygon[i]);

                    path.Close();

                    // Calculate bounds
                    SKRect bounds = path.Bounds;

                    // Assign rectangle (convert SKRect to System.Drawing.Rectangle if needed)
                    b.Rect = new System.Drawing.Rectangle(
                        (int)Math.Floor(bounds.Left),
                        (int)Math.Floor(bounds.Top),
                        (int)Math.Ceiling(bounds.Width),
                        (int)Math.Ceiling(bounds.Height)
                    );
                    b.Rect = new Rectangle(b.Rect.Left, b.Rect.Top,
						b.Rect.Width == 0 ? 1 : b.Rect.Width, 
						b.Rect.Height == 0 ? 1 : b.Rect.Height);
                    b.Value = "line";
                    result.Add(b);
                }
            
            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }

        

        public int MinRad { get { return minRad; } set { minRad = value; } }
        public int MaxRad { get { return maxRad; } set { maxRad = value; } }
        public int DistToJoin { get { return distToJoin; } set { distToJoin = value; } }
        public int MinLength { get { return minLength; } set { minLength = value; } }
    }
}
