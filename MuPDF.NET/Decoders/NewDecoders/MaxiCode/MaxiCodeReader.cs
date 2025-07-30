using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.MaxiCode
{
#if CORE_DEV
    public
#else
    internal
#endif
    class MaxiCodeReader : SymbologyReader2D
    {
        //Scan image rows main step
        protected int scanRowStep = 1;

        //Max difference between the center of the finder and the center of the mid segment of the finder
        protected float finderMaxCentersDifference = 0.1f;

        //Max distance in pixels between the center of the finder and the center of the mid segment of the finder
        protected int finderMaxCentersDistanceInPixels = 2;

        int width, height;

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.MaxiCode;
		}

		protected override FoundBarcode[] DecodeBarcode()
        { 
            this.width = BWImage.Width;
            this.height = BWImage.Height;

            return Scan();
        }


        //object that holds the bw image + methods to sample, follow vertices,...
        ImageScaner scan;
        //list of found codes found and correctly decoded.
        LinkedList<BarCodeRegion> candidates;
        //list of found and rejected finders, and codes to avoid exploring twice the same barcode
        LinkedList<BarCodeRegion> exclusion;

        //patternFinder is used in the main scan loop (scanning horizontal lines).  
        //crossFinder is used for each horizontal pattern found, to check if it is also 
        //a pattern verically
        IPatternFinderNoiseRow pf0, pf90;
        PatternFinderNoise pf45, pf135;

        //Patterns detected are not processed immediately. They are stored in foundPatters and 
        //joined with patterns in next rows. Once a pattern has no continuity (it is added to the
        //removedPatterns list), then it is processed. This way we have a better measure of the 
        //center of the pattern.
        LinkedList<Pattern> foundPatterns;
        LinkedList<Pattern> removedPatterns;

        //Object to find barcodes from a detected horizontal scan line pattern.
        MaxiCodeScaner maxiCodeFinder;

        FoundBarcode[] Scan()
        {
            scan = new ImageScaner(BWImage);
            pf0 = new PatternFinderNoiseRow(MaxiCodeScaner.finder, true, true, 2);
            pf90 = new PatternFinderNoiseRow(MaxiCodeScaner.finder, true, true, 2);
			pf45 = new PatternFinderNoise(BWImage, MaxiCodeScaner.finder[0], true, 2);
			pf135 = new PatternFinderNoise(BWImage, MaxiCodeScaner.finder[0], true, 2);
            candidates = new LinkedList<BarCodeRegion>();
            exclusion = new LinkedList<BarCodeRegion>();
            foundPatterns = new LinkedList<Pattern>();
            maxiCodeFinder = new MaxiCodeScaner(scan, pf90, pf45, pf135);

            //main loop to scan horizontal lines
            for (int y = 0; y < height && (ExpectedNumberOfBarcodes <= 0 || candidates.Count < ExpectedNumberOfBarcodes); y += scanRowStep)
            {
                ScanRow(y, BWImage.GetRow(y));
                // timeout check
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();
            }

            ArrayList result = new ArrayList();
            foreach (BarCodeRegion c in candidates)
            {
                FoundBarcode foundBarcode = new FoundBarcode();
				foundBarcode.BarcodeFormat = SymbologyType.MaxiCode;
                
				String data = "";
                if (c.Data != null) foreach (ABarCodeData d in c.Data) data += d.ToString();

                foundBarcode.Value = data;
                
				foundBarcode.Polygon = new SKPointI[5] { c.A, c.B, c.D, c.C, c.A };
                foundBarcode.Color = Color.Blue;

				//byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
				//GraphicsPath path = new GraphicsPath(foundBarcode.Polygon, pointTypes);
				//foundBarcode.Rect = Rectangle.Round(path.GetBounds());
                foundBarcode.Rect = Utils.DrawPath(foundBarcode.Polygon);

                foundBarcode.Confidence = c.Confidence;
                result.Add(foundBarcode);
            }
            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }

        //Scans a horizontal line looking for finder patterns (1010101010101). 
        //Patterns detected are not processed immediately. They are stored in foundPatters and 
        //joined with patterns in next rows. Once a pattern has no continuity (it is added to the
        //removedPatterns list), then it is processed. This way we have a better measure of the 
        //center of the pattern.
        private void ScanRow(int y, XBitArray row)
        {
            //look for the finder
            pf0.NewSearch(row);
            while (pf0.NextPattern() != null)
            {
                MyPoint a = new MyPoint(pf0.First, y);
                MyPoint b = new MyPoint(pf0.Last, y);
                //MyPoint center = new MyPoint(pf0.Center, a.Y);
                MyPoint center = new MyPoint((a.X + b.X) / 2, y);
                int xCenter = (a.X + b.X) / 2;
                int d = xCenter - center.X;
                if (d < 0) d = -d;
                if (Calc.Around(d, 0F, (float)(b.X - a.X) * finderMaxCentersDifference ) || d < finderMaxCentersDistanceInPixels)
                {
                    Pattern p = new Pattern(null, a.X, b.X, y);
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
                        StackedPattern sp = new StackedPattern(null, a.X, b.X, y);
                        foundPatterns.AddLast(sp);
                    }
                }
            }

            //clean old patterns and process them
            removedPatterns = Pattern.RemoveOldPatterns(foundPatterns, y);
            foreach (Pattern p in removedPatterns)
            {
                StackedPattern sp = (StackedPattern)p;
                ProcessPattern(sp);
            }
        }

        private bool ProcessPattern(StackedPattern p)
        {
            MyPoint a, b;
            p.MidPoints(out a, out b);
            MyPoint center = (a + b) / 2;

            //If the finder pattern is found in a place where another finder was found previously, it is skipped.
            foreach (BarCodeRegion c in exclusion) if (c.In(center)) return false;

#if FIND_PATTERN
            MyPoint Y = new MyPoint(0, 1);
            SquareFinder f = new SquareFinder(a + Y,b + Y, a, b, 7);
            candidates.AddLast(f);
#else
            //checks if a vertical pattern cross the horizontal pattern in the middle
            BarCodeRegion[] barcodes = maxiCodeFinder.ScanFinder(a, b, center);
            if (barcodes != null) foreach (BarCodeRegion r in barcodes)
            {
#if FIND_FINDER
                candidates.AddLast(r);
#else
                byte[] bytes=MaxiCodeSampler.ScanMatrix(scan, r);
                float confidence;
                string msg = MaxiCodeCharDecoder.Decode(bytes, out confidence);
                if (msg != null)
                {
                    r.Data = new ABarCodeData[] { new StringBarCodeData(msg) };
                    r.Confidence = confidence;
                    exclusion.AddFirst(r);
                    candidates.AddLast(r);
                    return true;
                }
#endif
            }
#endif
            return false;
        }
    }
}
