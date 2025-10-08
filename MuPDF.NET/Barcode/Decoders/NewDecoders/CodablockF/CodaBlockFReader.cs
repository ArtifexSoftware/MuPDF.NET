using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.CodaBlockF
{
    //Class to read CodeBlock-F barcodes. Uses an approach similar to PDF417:
    //  1.-scan all rows looking for a finder. There is only 1 finder (start-A code).
    //  2.-find the top and bottom corners of the finder, tracking the edge
    //  3.-traces a perpendicular (from top-bottom corners) line looking for the stop pattern.
    //  4.-find the top and bottom corners of the stop, tracking the edge.
    //  5.-traces N perpendicular lines (perpendicular to top-bottom corners) to scan all barcode rows.
    //     This scan is redundant, at bar module step. If CRC is right, data is added to the result.
    //  6.- Data is decoded and global CRC is checked. This step must be done at once, since CRC weights
    //     depend on the decoded data.
#if CORE_DEV
    public
#else
    internal
#endif
    class CodaBlockFReader : SymbologyReader2D
    {
        //Process start patterns of mínim 2 pixels height
        protected int stackedPatternMinHeight = 2;

        //Process stacked patterns with Y/X ratio bigger than this
        protected float stackedPatternMinRatio=1f;

        //try 3 perpendicular scan lines to find the stop pattern
        protected float[] crossPoints = new float[] { 0.5f, 0.75f, 0.25f};

        //Reject start/stop patterns that have not the same length. Set to 0 for strict same length, or bigger
        //to process skewed barcodes.
        protected float maxRatioStartStopLength = 0.2F;

        protected ImageScaner scan; //object to sample BW image
        protected LinkedList<BarCodeRegion> candidates = new LinkedList<BarCodeRegion>(); //found barcodes

        int[][] startPattern = new int[][] { new int[] { 2, 1, 1, 4, 1, 2 } }; //the only finder (start-A)
        int[] stopPattern = new int[] {  3, 3, 1, 1, 1, 2 }; //without last black bar
        IPatternFinderNoiseRow startFinder; //object to find finders in a horizontal row
        PatternFinderNoise stopFinder; //object to find finders using a bresenham line
        CodaBlockFDecoder decoder = new CodaBlockFDecoder(); //object to decode data to chars

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.CodablockF;
		}

        //Scans the image row by row, looking for start-A pattern. For each pattern found, try to 
        //add this patterns to previously found patterns. This is implemented in the StackedPattern.
        //StackedPatterns are useful to track better the edge and detect the angle of the barcode.
        protected override FoundBarcode[] DecodeBarcode()
        {
            scan = new ImageScaner(BWImage);
            startFinder = new PatternFinderNoiseRow(startPattern, true, true, 2);
			stopFinder = new PatternFinderNoise(BWImage, stopPattern, false, 2);
            LinkedList<Pattern> foundPatterns = new LinkedList<Pattern>();
            LinkedList<Pattern> removedPatterns;            

			for (int y = 0; y < BWImage.Height; y += ScanStep)
            {
                // timeout check
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();

                XBitArray row = BWImage.GetRow(y);
                startFinder.NewSearch(row);
                FoundPattern foundPattern;
                while ((foundPattern = startFinder.NextPattern()) != null)
                {
                    // timeout check
                    if (IsTimeout())
                        throw new SymbologyReader2DTimeOutException();

                    //foundPattern is the index of the found pattern
                    MyPoint a = new MyPoint(startFinder.First, y);
                    MyPoint b = new MyPoint(startFinder.Last, y);
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
                    if (sp.y - sp.startY > stackedPatternMinHeight && (float)(sp.y - sp.startY) / (float)(sp.startXEnd - sp.startXIn) > stackedPatternMinRatio)
                        ProcessPattern(sp);

                    // timeout check
                    if (IsTimeout())
                        throw new SymbologyReader2DTimeOutException();
                }
            }

            //clean stackedPatterns
            foreach (Pattern p in foundPatterns)
            {
                StackedPattern sp = (StackedPattern)p;
                //check ratio and process the stacked pattern
                if (sp.y - sp.startY > stackedPatternMinHeight && (float)(sp.y - sp.startY) / (float)(sp.startXEnd - sp.startXIn) > stackedPatternMinRatio)
                    ProcessPattern(sp);

                // timeout check
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();
            }

            FoundBarcode[] results = new FoundBarcode[candidates.Count];
            int nn = 0;
            foreach (BarCodeRegion r in candidates)
            {
                FoundBarcode f = new FoundBarcode();
				f.BarcodeFormat = SymbologyType.CodablockF;
                f.Polygon = new SKPointI[] { r.A, r.B, r.C, r.D, r.A };
                f.Color = SKColors.Blue;
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

        //Find the top and bottom corners of the finder, starting at the center point of the pattern,
        //and tracking the edge. Then traces 3 perpendicular lines (at 50%, 25% and 75%) looking for 
        //the stop pattern. Once is found, check ratio, sample the region and decode data.
        void ProcessPattern(StackedPattern p)
        {
            MyPoint center, rCenter; p.Center(out center, out rCenter);
            MyPoint midCenter = center;
            midCenter.X += (int)p.MeanWidth();
            float moduleLength = p.MeanWidth() / 11f;  //Start char has 11 modules

            foreach (BarCodeRegion c in candidates)
                if (c.In(midCenter)) return;

            //Track edge
            EdgeTrack et = new EdgeTrack(scan);
            et.Track(center, new MyVector(-1, 0), moduleLength, true);

            //find the top and bottom points of the edge
            MyPointF up = et.Up();
            MyPointF down = et.Down();

            //Calculate main directions
            MyVectorF vdY = (up - down);
            vdY = vdY.Normalized;
            MyVectorF vdX = new MyVectorF(-vdY.Y, vdY.X);

            //Calculate rotated module length
            float rotatedModuleLength = moduleLength * (float)Math.Cos(vdX.Angle) * (float)Math.Cos(vdX.Angle);
            p.foundPattern.moduleLength = rotatedModuleLength;

            //try 3 perpendicular scan lines to find the stop pattern
            foreach (float cc in crossPoints)
            {
                MyPointF c = up * cc + down * (1f - cc);
                Bresenham br = new Bresenham(c, vdX);
                stopFinder.NewSearch(br, false, -1);
                while (stopFinder.NextPattern()!=-1)
                {
                    MyPoint end = stopFinder.Last;
                    MyPointF endUp, endDown;
                    try
                    {
                        et.Track(end, new MyVector(-1, 0), moduleLength, false);
                        endUp = et.Up();
                        endDown = et.Down();
                    }
                    catch (Exception) { break; }
                    float dstart = (up - down).Length;
                    float dstop = (endUp - endDown).Length;
                    //check ratio
                    if (Calc.Around(dstart / dstop, 1F, maxRatioStartStopLength))
                    {
                        BarCodeRegion r = decoder.Decode(scan, up, endUp, endDown, down, p.foundPattern);
                        if (r != null) { candidates.AddLast(r); return; }
                    }
                }
            }
        }
    }
}
