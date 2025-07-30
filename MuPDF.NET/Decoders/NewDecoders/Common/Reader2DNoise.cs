using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using BarcodeReader.Core.Code39;
using BarcodeReader.Core.Code93;

namespace BarcodeReader.Core.Common
{
#if CORE_DEV
    public
#else
    internal
#endif
    abstract class Reader2DNoise : SymbologyReader2D
    {
        protected bool startsWithBlack = true;
        protected bool useE = false;
        protected bool requireQuietZones = true;

        //Process start patterns of mínim 2 pixels height
        protected int stackedPatternMinHeight = 2;

        //Reject start patterns with and edge out of -45º..45º  --> cos(45)=0.707
        protected float barcodeMaxCosAngle = 0.7f;

        //When check for start quiet zone, check 4 * modules with white pixels
        protected float startPatternQuietZone = 4f;

        //When check for stop quiet zone, check 7 * modules with white pixels
        protected float stopPatternQuietZone = 7f;

        //If all patterns are different versions of the same pattern
        protected bool singlePattern = false; //by default, all patterns are considered differents

        //For each start pattern found, try to find a valid stop pattern tracing perpendicular lines
        // at 50%, 25%, 75%,... and at diferent angles from the perpendicular: 0rad, 0.2rad, -0.2rad
        protected float[] crossPoints = new float[] { 0.5f, 0.25f, 0.75f, 0.12f, 0.37f, 0.62f, 0.87f }; //needed for really damaged barcodes
        protected float[] angles = new float[] { 0f, 0.2f, -0.2f }; //nedded for skewed barcodes

        //Reject start/stop patterns that are not +/- parallel. Set to 0 for strict parallel or bigger to process
        //skewed barcodes with start/stop patterns bars not parallels
        protected float maxDifferenceStartStopAngle = 0.2f;

        //Reject start/stop patterns that have not the same length. Set to 0 for strict same length, or bigger
        //to process skewed barcodes.
        protected float maxRatioStartStopLength = 0.4F;

        protected ImageScaner scan; //object to sample BW image

        //PatternFinderRow startFinder;
        IPatternFinderNoiseRow startFinder, simpleStopFinder; //object to find finders in a horizontal row
        PatternFinderNoise stopFinder; //object to find stop finders in any direction
        LinkedList<BarCodeRegion> candidates = new LinkedList<BarCodeRegion>(); //found barcodes

        /// found regions (recognized and not recognized)
        protected LinkedList<BarCodeRegion> foundRegions = new LinkedList<BarCodeRegion>();

        protected int[][] startPatterns = null, stopPatterns = null;
        virtual protected BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, MyPoint start, MyPoint end, FoundPattern foundPattern) { return null; }
        virtual protected BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, BarCodeRegion r, FoundPattern foundPattern) { return null; }
        //method to read a barcode without stop pattern
        virtual protected BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, MyPoint start, MyVectorF vd, FoundPattern foundPattern) { return null; }

        public bool StartsWithBlack { get { return startsWithBlack; } set { startsWithBlack = value; } }
        public bool UseE { get { return useE; } set { useE = value; } }
        public bool RequireQuietZones { get { return requireQuietZones; } set { requireQuietZones = value; } }
        public bool SinglePattern { get { return singlePattern; } set { singlePattern = value; } }

        public bool UsePatternFinderNoiseRowEx { get; set; } = false;
        public int NoiseLevel { get; set; } = 2;

        //Scans the image row by row, looking for start patterns. For each pattern found, try to 
        //add this patterns to previously found patterns. This is implemented in the StackedPattern.
        //StackedPatterns are useful to track better the edge and detect the angle of the barcode.
        protected override FoundBarcode[] DecodeBarcode()
        {
            foundRegions.Clear();
            scan = new ImageScaner(BWImage);
            var maxEAcumDist = 1.6f; // moved to 1.6f due to #151     1.5f; //relax error for start pattern, see #138 (927-code39-with-noise.PNG)

            if (UsePatternFinderNoiseRowEx)
            {
                //if (startPatterns.Length != 1)
                //    throw new Exception("PatternFinderNoiseRowEx does not support several start patterns");
                startFinder = new PatternFinderNoiseRowEx(startPatterns[0]);
            }
            else
                startFinder = new PatternFinderNoiseRow(startPatterns, useE, startsWithBlack, NoiseLevel, maxEAcumDist);

            //simpleStopFinder = new PatternFinderNoiseRow(startPatterns, useE, startsWithBlack, 2);
            //startFinder = new PatternFinderRow(startPatterns);
            if (stopPatterns != null) stopFinder = new PatternFinderNoise(BWImage, stopPatterns, startsWithBlack, NoiseLevel);
            LinkedList<Pattern> foundPatterns = new LinkedList<Pattern>();
            LinkedList<Pattern> removedPatterns;
            processedRegions = new LinkedList<BarCodeRegion>();

            // check against the timeout
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            candidates.Clear(); // clear candidates to avoid unwanted caching of results

            for (int y = 0; y < BWImage.Height; y += ScanStep)
            {
                XBitArray row = BWImage.GetRow(y);

                // check against the timeout
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();

                startFinder.NewSearch(row);
                FoundPattern foundPattern;
                while ((foundPattern = startFinder.NextPattern()) != null)
                {
                    if (requireQuietZones && !startFinder.HasQuietZone) continue;
                    //foundPattern is the index of the found pattern
                    MyPoint a = new MyPoint(startFinder.First, y);
                    MyPoint b = new MyPoint(startFinder.Last, y);
                    //Check if another pattern was found in the last row. 
                    if (singlePattern) foundPattern.nPattern = 0; //all patterns are different versions of the same pattern
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
                    if (StopOnFirstFoundBarcodeInTheRow) break;
                }

                // check against the timeout
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();

                //clean old patterns and process them
                removedPatterns = Pattern.RemoveOldPatterns(foundPatterns, y);
                foreach (Pattern p in removedPatterns)
                {
                    // check against the timeout
                    if (IsTimeout())
                        throw new SymbologyReader2DTimeOutException();

                    ProcessPattern((StackedPattern)p);

                    if (CheckStop()) break;

                }

                if (CheckStop()) break;

            }

            //clean stackedPatterns
            foreach (Pattern p in foundPatterns)
            {
                ProcessPattern((StackedPattern)p);

                // check against the timeout
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();

            }

            // check against the timeout
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            FoundBarcode[] results = new FoundBarcode[candidates.Count];
            int nn = 0;
            foreach (BarCodeRegion r in candidates)
            {
                FoundBarcode f = new FoundBarcode() {ParentRegion = r};

				if (this is PZNReader)
					f.BarcodeFormat = SymbologyType.PZN;
				else if (this is UPUReader)
					f.BarcodeFormat = SymbologyType.UPU;
				else if (this is Code39ExtendedReader)
					f.BarcodeFormat = SymbologyType.Code39Ext;
				else if (this is Code39Mod43ExtendedReader)
					f.BarcodeFormat = SymbologyType.Code39Mod43Ext;
				else if (this is Code39Mod43Reader)
					f.BarcodeFormat = SymbologyType.Code39Mod43;
				else if (this is Code39.Code39Reader)
					f.BarcodeFormat = SymbologyType.Code39;
				else if (this is Code93Reader)
					f.BarcodeFormat = SymbologyType.Code93;
				else if (this is Code128.Code128Reader)
					f.BarcodeFormat = SymbologyType.Code128;
				else if (this is MSI.MSIReader)
					f.BarcodeFormat = SymbologyType.MSI;
				else if (this is Pharmacode.PharmaReader)
					f.BarcodeFormat = SymbologyType.Pharmacode;
                else
                    f.BarcodeFormat = GetBarCodeType();

                f.Polygon = new SKPointI[] { r.A, r.B, r.C, r.D, r.A };
                f.Color = Color.Blue;
				//byte[] pointTypes = new byte[5] { (byte)PathPointType.Start, (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line };
				//GraphicsPath path = new GraphicsPath(f.Polygon, pointTypes);
				//f.Rect = Rectangle.Round(path.GetBounds());
                f.Rect = Utils.DrawPath(f.Polygon);
                f.Value = (r.Data != null ? r.Data[0].ToString() : "?");
				f.Confidence = r.Confidence;
                results[nn++] = f;
            }

            // check against the timeout
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            return RemoveDuplicates(results);
        }

        // Removes duplicates from same region.
        // Adding regions to list foundRegions - zone of responsibility of classes inherited from this class. For example - see Code128Reader.
        private FoundBarcode[] RemoveDuplicates(FoundBarcode[] foundBarcodes)
        {
            var bestByRegions = new Dictionary<BarCodeRegion, FoundBarcode>();
            var result = new List<FoundBarcode>();

            foreach (var bar in foundBarcodes)
            {
                //find region for the found barcode
                BarCodeRegion regionForBarcode = null;

                foreach (var reg in foundRegions)
                {
                    if (reg.IntersectsWith(bar.ParentRegion, 10))
                    {
                        regionForBarcode = reg;
                        break;
                    }
                }

                //if not found region - just return barcode as result
                if (regionForBarcode == null)
                    result.Add(bar);
                else
                {
                    //if found region that contains other barcodes - choose best from them (by Confidence)
                    FoundBarcode prevBar = null;
                    if (!bestByRegions.TryGetValue(regionForBarcode, out prevBar))
                        bestByRegions.Add(regionForBarcode, bar);
                    else
                    {
                        if (prevBar.Confidence < bar.Confidence)
                            bestByRegions[regionForBarcode] = bar;
                    }
                }
            }

            //add best barcode from regions to result list
            foreach (var bar in bestByRegions.Values)
                result.Add(bar);

            return result.ToArray();
        }

        //Check if the algorithm must stop when the first number of found barcodes reach the parameter ExpectedNumberOfBarcodes
        private bool CheckStop()
        {
            // check against the timeout
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            return this.ExpectedNumberOfBarcodes > 0 && candidates.Count >= this.ExpectedNumberOfBarcodes;
        }


        //Traces a line starting at p in vdX direction during moduleLength pixels. Count the number of black pixles in this segment. 
        //If it is bigger than the half of a module length, then quiet zone is considered a false quiet zone.
        protected bool CheckQuietZone(MyPoint p, MyVectorF vdX, float moduleLength)
        {
            Bresenham br = new Bresenham(new MyPointF(0.5f, 0.5f) + (MyPointF)p, vdX);
            while (scan.In(br.Current) && scan.isBlack(br.Current)) br.Next();

            //max allowed noise
            int quietZone = (int)Math.Round(moduleLength * 0.5f);
            if (quietZone < 1) quietZone = 1;
            int steps = (int)Math.Ceiling(moduleLength * (vdX.X > 0 ? stopPatternQuietZone : startPatternQuietZone));  //less restrictive for start patterns
            while (quietZone > 0 && scan.In(br.Current) && steps > 0)
            {
                if (scan.isBlack(br.Current)) quietZone--;
                br.Next();
                steps--;
            }
            return quietZone > 0;
        }

        protected virtual void TrackEdge(MyPoint left, MyPoint right, float moduleLength,  out MyPointF up, out MyPointF down)
        {
            EdgeTrack et = new EdgeTrack(scan);
            et.Track(left, new MyVector(-1, 0), moduleLength, true);

            //find the top and bottom points of the edge
            up = et.Up();
            down = et.Down();
        }

        //Find the top and bottom corners of the finder, starting at the center point of the pattern,
        //and tracking the edge. Then traces a perpendicular line to the left direction, and check the
        //quiet zone of 10 modules length (this step rejects most of the false candidates).
        bool ProcessPattern(StackedPattern p)
        {
            //estimate moduleLength (in pixels)
            int nModules = 0;
            foreach (int w in startPatterns[p.nPattern]) nModules += w;
            float moduleLength = p.MeanWidth() / (float)nModules;

            //check minimum aspect ratio
            if (p.y - p.startY <= stackedPatternMinHeight) return false; //invalid start, not tall enough

            MyPoint lCenter, rCenter; p.Center(out lCenter, out rCenter);
            MyPoint midCenter = lCenter;
            midCenter.X += (int)p.MeanWidth();

            foreach (BarCodeRegion c in candidates)
                if (c.In(midCenter)) return true; //valid but already processed

            //Track edge
            MyPointF up, down;
            TrackEdge(lCenter, rCenter, moduleLength, out up, out down);

            //Calculate main directions
            MyVectorF vdY = (up - down);
            vdY = vdY.Normalized;
            MyVectorF vdX = new MyVectorF(-vdY.Y, vdY.X);

            //Calculate rotated module length
            float cosAngle = (float)Math.Cos(vdX.Angle);
            if (cosAngle < barcodeMaxCosAngle) return false; //invalid

            float rotatedModuleLength = moduleLength * cosAngle * cosAngle; //projected module length X axis
            float rotatedLength = p.MeanWidth() * cosAngle * cosAngle;
            if (requireQuietZones && !CheckQuietZone(lCenter, -vdX, rotatedModuleLength)) return false; //invalid start
            p.foundPattern.moduleLength = rotatedModuleLength;

            BarCodeRegion region = null;
            if (stopPatterns == null) //barcode without stop patterns
            {   //proceed to decode 
                BarCodeRegion r = FindBarcode(scan, p.foundPattern == null ? p.nPattern : p.foundPattern.nPattern, new BarCodeRegion(up, up + vdX * rotatedLength, down + vdX * rotatedLength, down), p.foundPattern);
                if (r != null) { candidates.AddLast(r); return true; }
            }
            else
            {
                region = FindStopPattern(p.foundPattern == null ? p.nPattern : p.foundPattern.nPattern, up, down, vdX, vdY, p.foundPattern);
                if (region != null && region.Data != null && region.Data.Length > 0) { candidates.AddLast(region); return true; }
            }

            //if no region is detected, try to read barcode without stop pattern
            region = FindBarcode(scan, p.foundPattern == null ? p.nPattern : p.foundPattern.nPattern, up*0.5f+down*0.5f, vdX, p.foundPattern);
            if (region != null) { candidates.AddLast(region); return true; }


            //if no region is detected, we try to find barcodes for each found start pattern
            //just scanning a single row
            /*foreach (MyPoint q in p.LPoints)
            {
                XBitArray row = BWImage.GetRow(q.Y);
                simpleStopFinder.NewSearch(row, q.X, row.Size, 1, -1);
                while (simpleStopFinder.NextPattern() != null)
                    if (CheckQuietZone(new MyPoint(simpleStopFinder.Last, q.Y), new MyVectorF(1f, 0f), rotatedModuleLength))
                    {
                        BarCodeRegion r = FindBarcode(scan, p.foundPattern.nPattern, q, new MyPoint(simpleStopFinder.Last + (int)(rotatedModuleLength * 1.5f), q.Y), p.foundPattern);
                        if (r != null) { candidates.AddLast(r); return true; }
                    }
            }*/

            bool checkEndLine = MaxDistanceBetweenStartAndStopPatternsInModules >= 0;

            if (stopFinder != null)
            {
                foreach (MyPoint q in p.LPoints)
                {
                    float[] angles = new float[] { 0f, 0.1f, -0.1f };
                    foreach (float angle in angles)
                    {
                        MyVectorF vd = vdX.Rotate(angle);
                        MyVectorF vdy = vdY.Rotate(angle);

                        //create Bresenham

                        //Bresenham br = new Bresenham(q, vd);
                        MyPointF c = q + vd * p.foundPattern.moduleLength * MinDistanceBetweenStartAndStopPatternsInModules * 0.7f;
                        Bresenham br;
                        if (MaxDistanceBetweenStartAndStopPatternsInModules < 0)
                            br = new Bresenham(c, vd);
                        else
                        {
                            var end = q + vd * p.foundPattern.moduleLength * MaxDistanceBetweenStartAndStopPatternsInModules * 1.3f;
                            br = new Bresenham(c, end);
                        }
                        //

                        stopFinder.NewSearch(br, checkEndLine, -1);
                        while (stopFinder.NextPattern() != -1)
                        {
                            var stripWidth = 2;
                            //if (CheckQuietZone(stopFinder.Last, vd, rotatedModuleLength))
                            if (CheckQuietZone(stopFinder.Last + vdy * moduleLength * stripWidth, vd, rotatedModuleLength) &&
                                CheckQuietZone(stopFinder.Last - vdy * moduleLength * stripWidth, vd, rotatedModuleLength))
                            {
                                BarCodeRegion r = FindBarcode(scan, p.foundPattern.nPattern, q, stopFinder.Last, p.foundPattern);
                                if (r != null)
                                {
                                    if (region != null) //if previously we had detected a full region with start/stop patterns
                                    {
                                        region.Data = r.Data;
                                        candidates.AddLast(region);
                                    }
                                    else //otherwise, we return a small region around the successfully scaned line
                                    {
                                        candidates.AddLast(r);
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return true; //not found, but valid start of barcode
        }

        LinkedList<BarCodeRegion> processedRegions; //list of previously processed regions to avoid doing the work twice.

        protected int MinDistanceBetweenStartAndStopPatternsInModules = 0;
        protected int MaxDistanceBetweenStartAndStopPatternsInModules = -1;
        protected float StopPatternExtraShift = 1.5f;

        //A start pattern is found with corners at up and down. This method traces a line at different points of the start pattern
        //going in vdX rotated at different angles. This is a force method to find stop pattern for skewed or damaged barcodes. 
        //Tracing only one line works in good quality, no noise, no skewed barcodes. Once one stop pattern that leads to a readable
        //barcode is found, the method stops.
        protected BarCodeRegion FindStopPattern(int startPattern, MyPointF up, MyPointF down, MyVectorF vdX, MyVectorF vdY, FoundPattern foundPattern)
        {
            var checkEndLine = MaxDistanceBetweenStartAndStopPatternsInModules >= 0;

            BarCodeRegion bestResult = null;
            foreach (float angle in angles)
            {
                MyVectorF vd = vdX.Rotate(angle);
                foreach (float f in crossPoints)
                {
                    //create Bresenham

                    //MyPointF c = up * f + down * (1f - f);// +vd * foundPattern.moduleLength * startPatterns[startPattern].Length;
                    var c = up * f + down * (1f - f);
                    MyPoint ci = c + vd * foundPattern.moduleLength * MinDistanceBetweenStartAndStopPatternsInModules * 0.7f;
                    Bresenham br;
                    if (MaxDistanceBetweenStartAndStopPatternsInModules < 0)
                        br = new Bresenham(ci, vd);
                    else
                    {
                        var end = c + vd * foundPattern.moduleLength * MaxDistanceBetweenStartAndStopPatternsInModules * 1.3f;
                        br = new Bresenham(ci, end);
                    }

                    BarCodeRegion lastResult = null;
                    stopFinder.NewSearch(br, checkEndLine, -1);
                    while (stopFinder.NextPattern() != -1)
                    {
                        if (stopFinder.First == ci)
                            continue;
                        MyPoint end = stopFinder.Last;
                        //Once a stop pattern is found, check if it has a valid quiet zone or if its error is very  low (this way we can process stop patterns of good quality without quiet zone!)
                        bool hasQuietZone = CheckQuietZone(end, vd, foundPattern.moduleLength) || CheckQuietZone(end + vdY, vd, foundPattern.moduleLength) || CheckQuietZone(end - vdY, vd, foundPattern.moduleLength);

                        if (hasQuietZone || !requireQuietZones)
                        if (stopFinder.PatternLength > 5 && stopFinder.Error < 1f || hasQuietZone)
                        {
                            //Find the two stop corners
                            MyPointF endUp = MyPointF.Empty, endDown = MyPointF.Empty;
                            EdgeTrack et = new EdgeTrack(scan);
                            try
                            {
                                et.Track(end, new MyVector(-1, 0), foundPattern.moduleLength, false);
                                endUp = et.Up();
                                endDown = et.Down();
                            }
                            catch (Exception) { break; }
                            if (!endUp.IsEmpty && !endDown.IsEmpty) //in both corners are found
                            {
                                //check left and right sides are parallel
                                MyVectorF left = (up - down).Normalized;
                                MyVectorF right = (endUp - endDown).Normalized;
                                float cosAngle = left * right;
                                if (Calc.Around(cosAngle, 1.0f, maxDifferenceStartStopAngle))
                                {
                                    //check ratio
                                    float dstart = (up - down).Length;
                                    float dstop = (endUp - endDown).Length;
                                    BarCodeRegion r = null;
                                    if (Calc.Around(dstart / dstop, 1F, maxRatioStartStopLength)) //this coeficient must be hight to allow skewed barcodes
                                    {
                                        r = new BarCodeRegion(up, endUp + vd * foundPattern.moduleLength * StopPatternExtraShift, endDown + vd * foundPattern.moduleLength * StopPatternExtraShift, down);
                                    }
                                    else //if aspect ratio is wrong, try cut large side
                                    {
                                        float lud = (up - c).Length;
                                        float ldd = (down - c).Length;
                                        float eud = (endUp - (MyPointF)end).Length;
                                        float edd = (endDown - (MyPointF)end).Length;

                                        float u = (lud > eud ? eud : lud);
                                        float d = (ldd > edd ? edd : ldd);

                                        MyVectorF eY = (endUp - endDown).Normalized;

                                        r = new BarCodeRegion(c + vdY * u, (MyPointF)end + eY * u, (MyPointF)end - eY * d, c - vdY * d);
                                    }
                                    r.startPattern = startPattern;
                                    bool repeated = false;
                                    foreach (BarCodeRegion dd in processedRegions) 
                                        if (dd.SimilarTo(r)) 
                                            repeated = true;
                                    if (repeated)
                                        if (!hasQuietZone)
                                            continue;
                                        else
                                            //if (bestResult != null) // refs #145, commented out as it fixes the issue with pattent.jpg returning null BUT causing issues with Code 128 barcodes from TestCase
                                                return bestResult;// refs #145, this line was causing returning null so the final result was empty too

                                    processedRegions.AddLast(r);
                                    BarCodeRegion rr = FindBarcode(scan, startPattern, r, foundPattern);
                                    if (rr != null)
                                        if (hasQuietZone)
                                            return rr;
                                        else
                                            lastResult = rr; //remember region with Data
                                    else if (lastResult==null || lastResult.Data==null) lastResult= r; //remember region without Data
                                }
                            }
                        }
                        //if (hasQuietZone) return null; //stop search of stop pattern, but it does not work because for damaged barcodes!
                    }
                    if (lastResult != null)
                        if (bestResult == null) 
                            bestResult = lastResult;
                        else if ((bestResult.B - bestResult.A).Length < (lastResult.B - lastResult.A).Length) 
                            bestResult = lastResult;
                }
            }
            return bestResult;
        }
    }
}
