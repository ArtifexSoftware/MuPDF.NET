using System.Collections;
using System.Drawing;
using SkiaSharp;
using BarcodeReader.Core.Common;

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
					b.BarcodeFormat = scanHorizonal ? SymbologyType.HorizontalLine : SymbologyType.VerticalLine;
                    b.Polygon = new SKPointI[] { edge.a, edge.b, edge.a};
                    b.Color = SKColors.Orange;
					//byte[] pointTypes = new byte[3] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line };
					//GraphicsPath path = new GraphicsPath(b.Polygon, pointTypes);
					//b.Rect = Rectangle.Round(path.GetBounds());
                    b.Rect = Utils.DrawPath(b.Polygon);
                    b.Rect = new SKRect(b.Rect.Left, b.Rect.Top,
						b.Rect.Width == 0 ? b.Rect.Left+1 : b.Rect.Right, 
						b.Rect.Height == 0 ? b.Rect.Top+1 : b.Rect.Bottom);
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
