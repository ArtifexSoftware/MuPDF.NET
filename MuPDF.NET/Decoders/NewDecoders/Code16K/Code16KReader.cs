using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using BarcodeReader.Core.Common;
using SkiaSharp;

namespace BarcodeReader.Core.Code16K
{
    //Class to read Code16K barcodes. Uses an approach mixture of PDF417 and DM
    //  1.-scan all rows looking for some start patterns. There are 8 start patterns. There are a lot of 
    //     matches because start patterns have only 4 bars (BWBW). 
    //  2.-find the top and bottom corners of the edge where the start pattern is found, tracking the edge
    //  3.-traces a perpendicular in the middle and check that there is a quiet zone. This step reject most
    //     of the false candidates.
    //  4.-tracing a perpendicular (in the right direction) to find stop patterns doesn't work, since there
    //     are too many false stop finders. So, we skip this step and start decoding rows.
    //  5.-traces N perpendicular lines (perpendicular to top-bottom corners) to scan all barcode rows.
    //     This scan is redundant, at bar module step. No CRC check per row is done. All samples of each row 
    //     are stored and, for each symbol, the MOST COMMON decoded value, is used to decode de barcode.
    //  6.- Data is decoded and global CRC is checked. 
#if CORE_DEV
    public
#else
    internal
#endif
    class Code16KReader : SymbologyReader2D
    {
        //Process start patterns of mínim 2 pixels height
        protected int stackedPatternMinHeight = 2;

        //Reject start patterns with and edge out of -45º..45º  --> cos(45)=0.707
        protected float barcodeMaxCosAngle = 0.7f;

        //When check for start quiet zone, check 4 * modules with white pixels
        protected float startPatternQuietZone = 10f;

        protected ImageScaner scan; //object to sample BW image
        protected LinkedList<BarCodeRegion> candidates = new LinkedList<BarCodeRegion>(); //found barcodes

        int[][] startStopPatterns = new int[][] { new int[] { 3, 2, 1, 1 }, new int[] {2,2,2,1 },
            new int[] {2,1,2,2 }, new int[] { 1,4,1,1 }, new int[] { 1,1,3,2 }, 
            new int[] { 1,2,3,1 }, new int[] { 1,1,1,4 }, new int[]{3,1,1,2} }; 
        IPatternFinderNoiseRow startFinder; //object to find finders in a horizontal row
        Code16KDecoder decoder = new Code16KDecoder(); //object to decode data to chars

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Code16K;
		}

        //Scans the image row by row, looking for start patterns. For each pattern found, try to 
        //add this patterns to previously found patterns. This is implemented in the StackedPattern.
        //StackedPatterns are useful to track better the edge and detect the angle of the barcode.
        //Since a code16K barcode have from 2..16 rows, each one starting with a different start pattern
        //we will be able to join only start patterns of the same row. I.e, a single code16K will lead to 
        //N stackedPatterns, where N is the number of rows of barcode.
		protected override FoundBarcode[] DecodeBarcode()
        {
            scan = new ImageScaner(BWImage);
            startFinder = new PatternFinderNoiseRow(startStopPatterns, true, true, 2);
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
                    //foundPattern is the index of the found pattern
                    MyPoint a = new MyPoint(startFinder.First, y);
                    MyPoint b = new MyPoint(startFinder.Last, y);
                    //Check if another pattern was found in the last row. 
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
                    ProcessPattern((StackedPattern)p);
                    // timeout check
                    if (IsTimeout())
                        throw new SymbologyReader2DTimeOutException();
                }
            }

            //clean stackedPatterns
            foreach (Pattern p in foundPatterns)
            {
                ProcessPattern((StackedPattern)p);
                // timeout check
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();
            }

            FoundBarcode[] results = new FoundBarcode[candidates.Count];
            int nn = 0;
            foreach (BarCodeRegion r in candidates)
            {
                FoundBarcode f = new FoundBarcode();
				f.BarcodeType = SymbologyType.Code16K;
                f.Polygon = new SKPoint[] { r.A, r.B, r.C, r.D, r.A };
                f.Color = Color.Blue;
				// Create an SKPath from the polygon
                var path = new SKPath();
                path.MoveTo(f.Polygon[0]);

                for (int i = 1; i < f.Polygon.Length; i++)
                    path.LineTo(f.Polygon[i]);

                path.Close();

                // Calculate bounds
                SKRect bounds = path.Bounds;

                // Assign rectangle (convert SKRect to System.Drawing.Rectangle if needed)
                f.Rect = new System.Drawing.Rectangle(
                    (int)Math.Floor(bounds.Left),
                    (int)Math.Floor(bounds.Top),
                    (int)Math.Ceiling(bounds.Width),
                    (int)Math.Ceiling(bounds.Height)
                );
                f.Value = (r.Data != null ? r.Data[0].ToString() : "?");
				f.Confidence = r.Confidence;
                results[nn++] = f;
            }
            return results;
        }

        //Find the top and bottom corners of the finder, starting at the center point of the pattern,
        //and tracking the edge. Then traces a perpendicular line to the left direction, and check the
        //quiet zone of 10 modules length (this step rejects most of the false candidates).
        void ProcessPattern(StackedPattern p)
        {
            //check minimum aspect ratio
            if (p.y - p.startY > stackedPatternMinHeight) 
            {
                MyPoint center, rCenter; p.Center(out center, out rCenter);
                MyPoint midCenter = center;
                midCenter.X += (int)p.MeanWidth();
                float moduleLength = p.MeanWidth() / 7f;

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
                float cosAngle = (float)Math.Cos(vdX.Angle);
                if (cosAngle > barcodeMaxCosAngle)
                {
                    float rotatedModuleLength = moduleLength * cosAngle;

                    //check left quiet zone
                    Bresenham br = new Bresenham(center, -vdX);
                    while (scan.In(br.Current) && scan.isBlack(br.Current)) br.Next();

                    //check 10 modules length quiet zone
                    bool quietZone = true;
                    while (quietZone && scan.In(br.Current) && (br.Current - center).Length < startPatternQuietZone * rotatedModuleLength)
                        if (scan.isBlack(br.Current)) quietZone = false;
                        else br.Next();

                    if (quietZone)
                    {   //proceed to decode 
                        p.foundPattern.moduleLength = rotatedModuleLength;
                        BarCodeRegion r = decoder.Decode(scan, up, down, p.foundPattern);
                        if (r!=null) candidates.AddLast(r);
                        return;
                    }
                }
            }            
        }


    }
}
