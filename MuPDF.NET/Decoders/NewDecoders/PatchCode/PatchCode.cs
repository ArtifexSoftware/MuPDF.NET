using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.PatchCode
{
    //Patch code has only 6 different codes. There are no messages. 
    //The approach is to consider patch codes as finders and use generic methods to find finders
    //The current version scans only patch codes with vertical bars. To read horizontal bars, the image 
    //should be rotated up front.

    //Once a finder is found, we add it to a StackedPattern to accumulate next finders. Other possible 
    //implementations should track start and end edges to detect the region of the barcode.
#if CORE_DEV
    public
#else
    internal
#endif
    class PatchCodeReader : SymbologyReader2D
    {
        //Process start patterns of mínim 2 pixels height
        protected int stackedPatternMinHeight = 2;

        //Min aspect ratio Y/X for stacked patterns
        protected float stackedPatternMinAspectRatio = 1.5f;

        //When check for start quiet zone, check 4 * modules with white pixels
        protected float startPatternQuietZone = 3f;

        //Min number of pixels to check for a quiet zone
        protected int minStartPatternQuiezZone=5;

        //Proportion of noise pixels accepted in quiet zone
        protected float acceptedNoiseInQuietZone=0.15f;

        protected ImageScaner scan;
        protected LinkedList<BarCodeRegion> candidates = new LinkedList<BarCodeRegion>();

        int[][] finders = new int[][] { new int[]{2,1,2,1,1,1,1} , new int[]{2,1,1,1,1,1,2}, 
            new int[]{2,1,1,1,2,1,1}, new int[]{1,1,2,1,2,1,1}, new int[]{1,1,2,1,1,1,2}, 
            new int[]{1,1,1,1,2,1,2} };
        string[] names = new string[] { "1", "2", "3", "4", "T", "6" };

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.PatchCode;
		}

		protected override FoundBarcode[] DecodeBarcode()
        {
#if DEBUG_IMAGE
           // image.Save(@"d:\out.png");
#endif
            scan = new ImageScaner(BWImage);
            var maxEDist = 0.5f;
            IPatternFinderNoiseRow finderSearch = new PatternFinderNoiseRow(finders, true, true, 2, -1, maxEDist);
            LinkedList<Pattern> foundPatterns=new LinkedList<Pattern>();
            LinkedList<Pattern> removedPatterns;
            LinkedList<BarCodeRegion> candidates = new LinkedList<BarCodeRegion>();

			for (int y = 0; y < BWImage.Height; y += ScanStep)
            {
                // timeout check
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();

                XBitArray row = BWImage.GetRow(y);
                finderSearch.NewSearch(row);
                FoundPattern foundPattern;
                while ((foundPattern = finderSearch.NextPattern()) != null)
                {
                    //foundPattern is the index of the found pattern
                    MyPoint a = new MyPoint(finderSearch.First, y);
                    MyPoint b = new MyPoint(finderSearch.Last, y);
                    //Check if the same pattern was processed in the last row. 
                    Pattern p = new Pattern(foundPattern, a.X, b.X, y);
                    LinkedListNode<Pattern> prev = foundPatterns.Find(p);
                    if (prev != null)
                    {   //pattern already processed the last row
                        if (prev.Value.y != y)
                        {
                            StackedPattern sp = (StackedPattern)prev.Value;
                            sp.NewRow(a.X, b.X, y);
                        }
                    }
                    else
                    {   //new
                        StackedPattern sp = new StackedPattern(foundPattern, a.X, b.X, y);
                        foundPatterns.AddLast(sp);
                    }
                }

                //clean old patterns and process them
                removedPatterns = Pattern.RemoveOldPatterns(foundPatterns, y);
                foreach (Pattern p in removedPatterns)
                {
                    StackedPattern sp = (StackedPattern)p;
                    if (sp.y - sp.startY > stackedPatternMinHeight && (float)(sp.y - sp.startY) / (float)(sp.startXEnd - sp.startXIn) > stackedPatternMinAspectRatio)
                        if (checkQuietZone(sp))
                        {
                            BarCodeRegion r = new BarCodeRegion(new MyPoint(sp.startXIn, sp.startY),
                                new MyPoint(sp.startXEnd, sp.startY), new MyPoint(sp.xEnd, sp.y),
                                new MyPoint(sp.xIn, sp.y));
                            r.Data = new ABarCodeData[] { new StringBarCodeData(names[sp.nPattern]) };
                            candidates.AddLast(r);
                        }
                    // timeout check
                    if (IsTimeout())
                        throw new SymbologyReader2DTimeOutException();
                }
            }

            FoundBarcode[] results = new FoundBarcode[candidates.Count];
            int nn = 0;
            foreach (BarCodeRegion r in candidates)
            {
                FoundBarcode f = new FoundBarcode();
				f.BarcodeFormat = SymbologyType.PatchCode;
                f.Polygon = new SKPointI[] { r.A, r.B, r.C, r.D, r.A };
                f.Color = Color.Blue;
				//byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
				//GraphicsPath path = new GraphicsPath(f.Polygon, pointTypes);
				//f.Rect = Rectangle.Round(path.GetBounds());
                f.Rect = Utils.DrawPath(f.Polygon);
                f.Value = (r.Data != null ? r.Data[0].ToString() : "?");
				f.Confidence = r.Confidence;
                results[nn++] = f;
            }
            return results;
        }

        bool checkQuietZone(StackedPattern sp)
        {
            MyPoint pIn, pEnd;
            sp.MidPoints(out pIn, out pEnd);
            int moduleLength = (int)(startPatternQuietZone * (pEnd - pIn).Length / 9f); //3 bars length quiet zone
            if (moduleLength < minStartPatternQuiezZone) moduleLength = minStartPatternQuiezZone;
            int MAX = (int)(acceptedNoiseInQuietZone*(float)sp.LPoints.Count*(float)moduleLength); 

            int n = 0;
            foreach (MyPoint p in sp.LPoints)
                if ((n += countBlackPixels(p, -1, moduleLength)) > MAX) return false;
            foreach (MyPoint p in sp.RPoints)
                if ((n += countBlackPixels(p, 1, moduleLength)) > MAX) return false;
            return true;
        }

        int countBlackPixels(MyPoint p, int incX, int count)
        {
            int n = 0;
            p.X += incX; //move 1px since the first pixel is still on the bar
            while (scan.In(p) && count > 0) { if (scan.isBlack(p)) n++; p.X += incX; count--; }
            return n;
        }

        bool checkQuietZone(MyPoint p, int incX, int moduleLentgh)
        {
            int n = 0;
            while (scan.In(p) && scan.isBlack(p)) { p.X += incX; n++; }
            if (n > moduleLentgh) return false;

            int i = 0;
            while (scan.In(p) && i <moduleLentgh)
            {
                if (scan.isBlack(p)) return false;
                p.X += incX;
                i++;
            }
            return true;
        }
    }
}
