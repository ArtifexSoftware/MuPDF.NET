using BarcodeReader.Core.Common;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

namespace BarcodeReader.Core.PDF417
{
#if CORE_DEV
    public
#else
    internal
#endif
    partial class PDF417Reader
    {
        //Scan image rows main step
        protected int scanRowStep = 1;

        //Process start patterns of mínim 2 pixels height
        protected int stackedPatternMinHeight = 2;

        //Reject start/stop patterns that have not the same length. Set to 0 for strict same length, or bigger
        //to process skewed barcodes.
        protected float maxRatioStartStopLength = 0.2F;


        //object that holds the bw image + methods to sample, track vertices,...
        ImageScaner scan;
        //list of found codes found and correctly decoded.
        LinkedList<BarCodeRegion> candidates;
        //list of found and rejected finders, and codes to avoid exploring twice the same barcode
        LinkedList<BarCodeRegion> exclusion;

        //The main idea is to find start patterns, then track the edge at de beginnig of 
        //the start pattern, and then trace a perpendicular scan line, looking for the stop pattern.
        //(It also applies finding a reversed stop pattern and then lookind for a reverse start pattern.)

        //Edge track works better if it starts in the middle of the edge. So, once the first scan line
        //detects a start pattern we don't process it. We wait until the last scan line detects this 
        //start pattern. For this reason, we have a list of previously found start patterns (foundPatterns)
        //and this list is updated at each scan line. 

        //We consider a start pattern as a new pattern, if in the foundPatterns list there isn't another 
        //patterns starting and ending 1 pixel left or right.
        //A partern is processed when has no coincident patterns for 2 rows. 
        
        //At the end of each scan line, foundPatterns are purged and those that have not continuity
        //are added to the removedPatterns and then processed.

        //List of patterns found but not finished. They will be processed when 
        LinkedList<Pattern> foundPatterns;
        LinkedList<Pattern> removedPatterns;


        //patternFinder is used in the main scan loop (scanning horizontal lines).  
        IPatternFinderNoiseRow startFinder;
        //stopFinder or startFinderReverse are used for each horizontal pattern found, to check if it is also 
        //it has a stop or reversed start pattern
        PatternFinderNoise stopFinder, startFinderReverse;
        
        //flag to allow vertical scan process
        bool verticalProcess = true;

        public bool VerticalProcess { get { return verticalProcess; } set { verticalProcess = value; } }

        ModuleDecoder moduleDecoder;

        FoundBarcode[] Scan()
        {
            scan = new ImageScaner(BWImage);
            startFinder = new PatternFinderNoiseRow(PDF417Finder.start, true, true, 2);
            stopFinder = new PatternFinderNoise(BWImage, PDF417Finder.stop, true, 2);
            startFinderReverse = new PatternFinderNoise(BWImage, PDF417Finder.startReverse, false, 2);
            candidates = new LinkedList<BarCodeRegion>();
            exclusion = new LinkedList<BarCodeRegion>();
            foundPatterns = new LinkedList<Pattern>();
            removedPatterns = new LinkedList<Pattern>();

            moduleDecoder = new ModuleDecoder("symbolmap.txt", 17); //PDF417 symbols are 17 modules length

#if DEBUG_IMAGE
            BWImage.GetAsBitmap().Save(@"out.png");
#endif
            //main loop to scan horizontal lines
            for (int y = 0; y < height && (ExpectedNumberOfBarcodes <= 0 || candidates.Count < ExpectedNumberOfBarcodes); y += scanRowStep)
            {
                ScanRow(y, BWImage.GetRow(y));
                // timeout check
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();
            }
            //process all pending patterns
            foreach (StackedPattern sp in foundPatterns)
            {
                ProcessPattern(sp, true);
                // timeout check
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();
            }


            if (verticalProcess)
            {
                foundPatterns.Clear();
                //main loop to scan vertical lines
                for (int x = 0; x < width && (ExpectedNumberOfBarcodes <= 0 || candidates.Count < ExpectedNumberOfBarcodes); x += scanRowStep)
                {
                    ScanCol(x, BWImage.GetColumn(x));
                    // timeout check
                    if (IsTimeout())
                        throw new SymbologyReader2DTimeOutException();
                }

                //process all vertical pending patterns
                foreach (StackedPattern sp in foundPatterns)
                {
                    ProcessPattern(sp, false);
                    // timeout check
                    if (IsTimeout())
                        throw new SymbologyReader2DTimeOutException();
                }
            }
            ArrayList result = new ArrayList();
            foreach (BarCodeRegion c in candidates)
            {
                FoundBarcode foundBarcode = new FoundBarcode();
				foundBarcode.BarcodeType = SymbologyType.PDF417;
	            String data = "";

                if (c.Data!=null) foreach (ABarCodeData d in c.Data) data += d.ToString();

				foundBarcode.Value = data;

                foundBarcode.Polygon = new SKPoint[5] { c.A, c.B, c.D, c.C, c.A };
                foundBarcode.Color = Color.Blue;

                // Build the SKPath from the SKPoint[] polygon
                var path = new SKPath();
                path.MoveTo(foundBarcode.Polygon[0]);

                for (int i = 1; i < foundBarcode.Polygon.Length; i++)
                    path.LineTo(foundBarcode.Polygon[i]);

                path.Close(); // Close the path to form a complete shape

                // Get the bounding rectangle
                SKRect bounds = path.Bounds;

                // Convert to integer rectangle if needed
                foundBarcode.Rect = new System.Drawing.Rectangle(
                    (int)Math.Floor(bounds.Left),
                    (int)Math.Floor(bounds.Top),
                    (int)Math.Ceiling(bounds.Width),
                    (int)Math.Ceiling(bounds.Height)
                );

                foundBarcode.Confidence = c.Confidence;
                result.Add(foundBarcode);

            }
            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }



        //Scans a horizontal line looking for start or reversed stop patterns. 
        //For each finder pattern found tracks the edge and follows its perpendicular
        // line looking for the stop or revesed start pattern.
        private void ScanRow(int y, XBitArray row)
        {
            //look for the finder
            startFinder.NewSearch(row);
            FoundPattern foundPattern;
            while ((foundPattern = startFinder.NextPattern()) != null)
            {
                //foundPattern is the index of the found pattern: 0 start, 1 reversed stop
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
            removedPatterns=Pattern.RemoveOldPatterns(foundPatterns, y);
            foreach (Pattern p in removedPatterns)
            {
                StackedPattern sp = (StackedPattern)p;
                if (sp.y - sp.startY > stackedPatternMinHeight) ProcessPattern(sp, true);
            }
        }

        //Equivalent to the previous method but for vertical scans.
        //Atention!!! for vertical scans, we use the same data structure than for 
        //horizontal scans but exchanging x and y coordinates. 
        private void ScanCol(int x, XBitArray col)
        {
            //look for the finder
            startFinder.NewSearch(col);
            FoundPattern foundPattern;
            while ((foundPattern = startFinder.NextPattern()) != null)
            {
                MyPoint a = new MyPoint(x, startFinder.First);
                MyPoint b = new MyPoint(x, startFinder.Last);
                //Check if the same pattern was processed in the last row. 
                Pattern p = new Pattern(foundPattern, a.Y, b.Y, x);
                LinkedListNode<Pattern> prev = foundPatterns.Find(p);
                if (prev != null)
                {   //pattern already processed the last row
                    if (prev.Value.y != x)
                    {
                        StackedPattern sp = (StackedPattern)prev.Value;
                        sp.NewRow(a.Y, b.Y, x);
                    }
                }
                else
                {   //new
                    StackedPattern sp = new StackedPattern(foundPattern, a.Y, b.Y, x);
                    foundPatterns.AddLast(sp);
                }
            }

            //clean old patterns
            removedPatterns = Pattern.RemoveOldPatterns(foundPatterns, x);
            foreach (Pattern p in removedPatterns)
            {
                StackedPattern sp = (StackedPattern)p;
                if (sp.y - sp.startY > stackedPatternMinHeight) ProcessPattern(sp, false);
            }
        }

        void ProcessPattern(StackedPattern p, bool isHorizontal)
        {
            //Console.WriteLine("start: " + p.startXIn + " " + p.startY);
            MyPoint center, rCenter; p.Center(out center, out rCenter);
            if (!isHorizontal) center = new MyPoint(center.Y, center.X);
            MyPoint midCenter = center;

            //for vertical scans, we receive x and y coordinates exchanged
            if (isHorizontal) midCenter.X += (int)p.MeanWidth();
            else midCenter.Y += (int)p.MeanWidth();

            //If the finder pattern is found in a place where another finder was found previously, it is skipped.
            bool done = false;
            foreach (BarCodeRegion c in exclusion)
                if (c.In(midCenter)) { done = true; break; }

            if (!done)
            {
#if FIND_PATTERN
                MyPoint a, b, c, d;
                if (isHorizontal)
                {
                    a = new MyPoint(p.startXIn, p.y);
                    b = new MyPoint(p.startXEnd, p.y);
                    c = new MyPoint(p.startXIn, p.startY);
                    d = new MyPoint(p.startXEnd, p.startY);
                }
                else
                {
                    a = new MyPoint(p.y, p.startXIn);
                    b = new MyPoint(p.y, p.startXEnd);
                    c = new MyPoint(p.startY, p.startXIn);
                    d = new MyPoint(p.startY, p.startXEnd);
                }
                BarCodeRegion br = new BarCodeRegion(a, b, c, d);
                candidates.AddLast(br);
#else
                //first approach of module length (it is the horizontal or vertical 
                //projection of the real length)
                float moduleLength=PDF417Finder.ModuleLength(p.MeanWidth());

                //Track edge crossing the center point of the start/reversed stop pattern
                EdgeTrack et = new EdgeTrack(scan);
                et.Track(center, (isHorizontal? new MyVector(-1, 0): new MyVector(0,-1)), moduleLength, true);

                //find the top and bottom points of the edge
                MyPointF up = et.Up();
                MyPointF down = et.Down();

                if (!up.IsEmpty && !down.IsEmpty)
                {
                    //Calculate main directions
                    MyVectorF vdY = (up - down);
                    vdY = vdY.Normalized;
                    MyVectorF vdX = new MyVectorF(-vdY.Y, vdY.X);

                    //throw 3 perpendicular scan lines to detect the stop / reversed start pattern
                    //Normally scanning 1 perpendicular line will be enough. The other is needed for
                    //damaged or noisy images
                    MyPointF[] crossPoints = new MyPointF[] { up * 0.5F + down * 0.5F}; //, up * 0.25F + down * 0.75F, up * 0.75F + down * 0.25F };
                    foreach (MyPointF c in crossPoints)
                    {
                        Bresenham br = new Bresenham(c, vdX);
                        PatternFinderNoise finder = null;
                        //if main scanning has found a start pattern, now look for stop pattern
                        if (p.nPattern == 0) finder = stopFinder;
                        //if main scanning has found a reversed stop pattern, now look for reversed start pattern
                        else finder = startFinderReverse;

                        finder.NewSearch(br, false, -1);
                        while (finder.NextPattern() != -1)
                        for (int trackMethod = 0; trackMethod < 2;trackMethod++ )
                            {
                                //Track edge, find pattern top and bottom, and check if length
                                //is similar to the first pattern length
                                MyPointF endUp, endDown;
                                if (trackMethod==0)
                                {
                                    MyPoint end = (p.nPattern == 0 ? finder.First : finder.Last);
                                    Console.WriteLine("end: " + end);
                                    et.Track(end, (isHorizontal ? new MyVector(-1, 0) : new MyVector(0, -1)), moduleLength, p.nPattern == 0 ? true : false);
                                    endUp = et.Up();
                                    endDown = et.Down();
                                    if (p.nPattern == 0)
                                    {
                                        float d = (finder.Last - finder.First).Length;
                                        endUp += (endUp - up).Normalized * d;
                                        endDown += (endDown - down).Normalized * d;
                                    }
                                }
                                else
                                {
                                    MyPointF mid = (p.nPattern == 0 ? (MyPointF)finder.First + vdX * (moduleLength * 3.5f) : (MyPointF)finder.Last - vdX * (moduleLength * 4f));
                                    MyPointF elu, eld, eru, erd;
                                    et.TrackBar(mid, (isHorizontal ? new MyVector(-1, 0) : new MyVector(0, -1)), moduleLength, true, out elu, out eld, out eru, out erd);
                                    if (p.nPattern == 0) //stop pattern
                                    {
                                        float d = (finder.Last - finder.First).Length;
                                        endUp = elu + (elu - up).Normalized * d;
                                        endDown = eld + (eld - down).Normalized * d;
                                    }
                                    else //reversed start pattern
                                    {
                                        endUp = eru;
                                        endDown = erd;
                                    }
                                }


                                float dstart = (up - down).Length;
                                float dstop = (endUp - endDown).Length;
                                if (Calc.Around(dstart / dstop, 1F, maxRatioStartStopLength))
                                {
                                    if (p.nPattern == 0)
                                    {
                                        Console.WriteLine("up {0}, down{1}, endUp{2}, endDown{3}", up, down, endUp, endDown);
                                        if (ReadBarcode(up, down, endUp, endDown, moduleLength, true))
                                            return;
                                    }
                                    else
                                    {   //reversed
                                        vdX = (endUp - up).Normalized;
                                        up -= vdX; down -= vdX;
                                        //REMOVE end -= vdX; endDown -= vdX;
                                        Console.WriteLine("up {0}, down{1}, endUp{2}, endDown{3}", up, down, endUp, endDown);
                                        if (ReadBarcode(endDown, endUp, down, up, moduleLength, true))
                                            return;
                                    }
                                }
                            }
                    }
                    //try to read without stop pattern
                    if (p.nPattern == 0) 
                        ReadBarcode(up, down, up + vdX * 100F, down + vdX * 100F, moduleLength, false);
                }
#endif
            }
        }


        //up, down, endUp and endDown are the vertices of a candidate barcode.
        //module length is a first aprox. of the module length
        //hasEnd is true if both, start and stop patterns, where found. If hasEnd is false, 
        //endUp and endDown only give the direction where the stop pattern should be.

        //This methods has 2 parts: 
        //1.- scans the firsts 2 columns to read the number of rows, columnes, and error level. It
        //  does oversampling, to avoid erroneous scans. Then take the most common value for each of them.
        //2.- If rows, columns and error level are detected, scans the rest of columns for codewords.
        bool ReadBarcode(MyPointF up, MyPointF down, MyPointF endUp, MyPointF endDown, float moduleLength, bool hasEnd)
        {
            MyVectorF vdY = (up - down);
            float startLength = vdY.Length;
            vdY = vdY.Normalized;
            MyVectorF vdX = new MyVectorF(-vdY.Y, vdY.X);

            MyVectorF endVdY = (endUp - endDown).Normalized;
            float d = moduleLength/2f;
            bool rowsColsErrRead = false;
            Hashtable rowsDiv3 = new Hashtable();
            Hashtable rowsMod3 = new Hashtable();
            Hashtable cols = new Hashtable();
            Hashtable errorCorrectionLevel = new Hashtable();
            int crd3 = 0, crm3 = 0, cc = 0, ce = 0;
            SortedList<float, int> mLenghts=new SortedList<float,int>();
            int nMLengths = 0;
            int minSamplesCount = Math.Max(3, (int)(((down - up).Length / moduleLength) / 6));

            //first part: scan the 2nd column to extract row, columns and error level info.
            //For each start pattern correctly read, we store its length to obtain a good mean 
            //module length
            while (!rowsColsErrRead && d < startLength)
            {
                MyPointF a = up - vdY * d;
                MyPointF b = endUp - endVdY * d;
                SymbolReader sr = new SymbolReader(scan, a, b, 8,true, hasEnd, moduleLength);
                MyPoint p0 = sr.Current;
                float[] start = sr.NextSymbol(); //skip start
                if (start != null && ArrayEquals(start, PDF417Finder.startE))
                {
                    //update the hash of module lengths
                    float mLength = (sr.Current - p0).Length / 17F;
                    if (mLenghts.ContainsKey(mLength)) mLenghts[mLength]++;
                    else mLenghts.Add(mLength, 1);
                    nMLengths++;

                    //if the 2nd row can read extract info
                    float[] lRow = sr.NextSymbol();
                    if (lRow != null)
                    {
                        int cluster1 = (int)(Math.Round(lRow[0]) - Math.Round(lRow[1]) + Math.Round(lRow[4]) - Math.Round(lRow[5]) + 9) % 9;
                        int cluster2 = ((int)Math.Round(lRow[0] - lRow[2] + lRow[4] - lRow[6] + 9)) % 9;
                        int cluster = cluster1 % 3 == 0 ? cluster1 : cluster2;

                        int codeword = moduleDecoder.GetCodeword(cluster, lRow);
                        if (codeword != -1)
                        {
                            if (cluster == 0)
                            {
                                add(rowsDiv3, codeword % 30);
                                crd3++;
                            }
                            else if (cluster == 3)
                            {
                                add(rowsMod3, (codeword % 30) % 3);
                                add(errorCorrectionLevel, (codeword % 30) / 3);
                                crm3++;
                                ce++;

                            }
                            else if (cluster == 6)
                            {
                                add(cols, codeword % 30);
                                cc++;
                            }

                            //stop when we have enough samples of each parameter
                            rowsColsErrRead = crd3 > minSamplesCount && crm3 > minSamplesCount && cc > minSamplesCount && ce > minSamplesCount;
                        }
                    }
                }
                d += moduleLength/2f;
            }

            //If parameters are read (even if they don't have enough samples)
            if (crd3>0 && crm3>0 && cc>0 && ce>0)
            {
                //extract 2 most probably parameters:
                int nCols1 = most(cols);
                int nCols2 = mostExcept(cols, nCols1);
                int[] nColsArray = new int[] { nCols1 + 1, nCols2 + 1};


                int nRows = most(rowsDiv3) * 3 + most(rowsMod3) % 3 + 1;
                int nError = most(errorCorrectionLevel);
                float mLength = Median(mLenghts, nMLengths);

                foreach (var nCols in nColsArray)
                {
                    if (nCols <= 0)
                        continue;

                    //Define the barcode region. 
                    BarCodeRegion bc;
                    if (hasEnd) bc = new BarCodeRegion(down, endDown, up, endUp);
                    else
                    {   //if stop pattern is not found, use an estimate
                        MyVectorF l = vdX * 17F * (4F + (float)nCols) * moduleLength;
                        bc = new BarCodeRegion(down, down + l, up, up + l);
                    }

                    //read code words and perform error correction
                    float confidence = 0f;
                    int[] data = null;

                    if (data == null) data = ReadSymbol(bc, mLength, nRows, nCols, nError, hasEnd, out confidence);
                    if (data == null) data = ReadSymbol3(bc, mLength, nRows, nCols, nError, out confidence); // improved version of ReadSymbol2
                    if (data == null) data = ReadSymbol2(bc, mLength, nRows, nCols, nError, out confidence);
                    if (data == null) data = ReadSymbolSimple(bc, mLength, nRows, nCols, nError, hasEnd, out confidence);

                    if (data != null)
                    {
                        //Decode bytes to data
                        ABarCodeData[] result = DecodeData(data);
                        if (result != null)
                        {
                            bc.Data = result;
                            bc.Confidence = confidence;
                            candidates.AddLast(bc);
                            exclusion.AddLast(bc);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool ArrayEquals(float[] a, int[] b)
        {
            float e = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float ee = a[i] - b[i];
                e += ee * ee;
            }
            return e < 2f;

            /*for (int i = 0; i < b.Length; i++)
                if ((int)Math.Round(a[i]) != b[i]) return false;
            return true;*/
        }

        float Median(SortedList<float, int> l, int n)
        {
            int c = 0;
            int mid = n / 2;
            foreach (float m in l.Keys)
            {
                c += l[m];
                if (c > mid) return m;
            }
            return 0F;
        }

        void add(Hashtable h, int v)
        {
            if (h.ContainsKey(v)) h[v]=(int)h[v] + 1;
            else h[v] = 1;
        }

        int most(Hashtable h)
        {
            if (h.Count == 0) return -1;
            int max = 0, val = -1;
            foreach (int i in h.Keys)
                if ((int)h[i] > max)
                {
                    max = (int)h[i];
                    val = i;
                }
            return val;
        }


        int mostExcept(Hashtable h, int exceptedValue)
        {
            if (h.Count == 0) return -1;
            int max = 0, val = -1;
            foreach (int i in h.Keys)
                if ((int)h[i] > max && i != exceptedValue)
                {
                    max = (int)h[i];
                    val = i;
                }
            return val;
        }

        int mostNotRepeat(Hashtable h)
        {
            int m = most(h);
            if (m != -1)
            {
                int max = (int)h[m];
                foreach (int i in h.Keys)
                    if (i != m && (int)h[i] == max) return -1; //there are 2 values with the same number of repetitions
            }
            return m;
        }


        // Now that we know how the barcode looks like, try to read the symbols (or at 
        //least enough of them to attempt error correction). 
        int[] ReadSymbol(BarCodeRegion r, float moduleLength, int rows, int cols, int errorCorrectionLevel, bool hasEnd, out float confidence)
        {
            confidence = 0F;
            
            int errorCodeWordCount = 2 << errorCorrectionLevel;
            if (errorCodeWordCount > rows * cols) return null;

#if DEBUG_IMAGE
            scan.Reset();
#endif
            ReedSolomon rs = new ReedSolomon();

            float[] exposure = new float[] { 0.6f, 0.4f, 0.5f };
            for (int iExposure = 0; iExposure < exposure.Length; iExposure++)
            {
                int[] data = new int[rows * cols];

                Grid g = new Grid((cols + 4) * 17 + 2, rows, r.C, r.A, r.D, r.B, true);
                MyVectorF vdYStart = (r.A - r.C) / rows;
                MyVectorF vdYStop = (r.B - r.D) / rows;


                float[] offsets = new float[] { 0.5f, 0.3F, 0.7F };
                for (int n = 0; n < offsets.Length; n++)
                {
                    // intitialize data before the next try
                    // IMPORTANT: refs #150 Decoders: we should re-initialize data before each iteration
                    // i.e. we must NOT mix data with other iterations
                    // as in some RARE cases it may cause RS correction to pass the incorrect data made mixed from some INTERSECTED rows
                    // the root of the issue lays in rare cases when 2 rows are using the data from the actualy same physical points (due to offset)
                    // see PDF417\2014\pdf417-1\04.png
                    // initialize data array with -1 (means error or erasure) that will contain data's codewords
                    for (int i = 0; i < data.Length; i++) data[i] = -1; //initialize

                    float offset = offsets[n];
                    for (int row = 0; row < rows; row++)
                    {
                        //Console.WriteLine("-------------ROW:" + row);
                        MyPointF a = r.C + vdYStart * ((float)row + offset);
                        MyPointF b = r.D + vdYStop * ((float)row + offset);
                        SymbolReader sr = new SymbolReader(scan, a, b, 8, true, false, moduleLength);

                        //find the start point: calculate theoric and adjust (if possible)
                        MyPointF theoric = a;
                        MyPoint previous = sr.Adjust(theoric);
                        if (previous == MyPoint.Empty) previous = theoric;

                        //scan row: rows 0 and 1 are processed just to adjust the start point of each symbol
                        MyVectorF vdX = (b - a).Normalized;
                        float ml = moduleLength * 17F; //initial value. Will be updated at each step.
                        for (int col = 0; col < cols + 2; col++)
                        {
                            //Calculates the end point. First estimates theoric point, then adjust it.
                            theoric = (hasEnd ? g.GetSamplePoint((float)((col + 1) * 17)+0.5f, (float)row+offset) : previous + vdX * ml);
                            MyPoint adjusted = sr.Adjust(theoric);
                            if (adjusted == MyPoint.Empty) adjusted = theoric;

#if DEBUG_IMAGE
                        scan.setPixel(theoric, Color.Blue);
                        scan.setPixel(adjusted, Color.Orange);
#endif

                            if (col > 1)
                            {
                                //obtain a list of possible symbols (module lenghts in E format)
                                //Since we give the start and end point of the symbol, we can 
                                //solve noise pixels. The result is a list of possible symbols, since
                                //gray modules lead to *2 solutions.
                                LinkedList<int[]> ES = sr.NextSymbol(previous, adjusted, exposure[iExposure]);

                                //for each possible symbol, tries to get the codeword
                                int codeword = -1;
                                float minD = float.MaxValue;
                                foreach (int[] E in ES)
                                {
                                    float[] fE = new float[8];
                                    for (int i = 0; i < 8; i++) fE[i] = (float)E[i];
                                    float dist;
                                    int c = moduleDecoder.GetCodeword((row % 3) * 3, fE, out dist);
                                    //if (c != -1) { data[row * cols + col - 2].AddLast(new Codeword(c,dist)); codeword = c; }
                                    if (c != -1 && dist < minD) { minD = dist; codeword = c; break; }
                                }

                                //if symbol is correctly converted to codeword then add to the data array
                                if (codeword != -1)
                                {
                                    data[row * cols + col - 2] = codeword;
                                    if (!hasEnd) ml = (adjusted - previous).Length; //update module length
                                }
                            }

                            previous = adjusted;
                        }
                    }
#if DEBUG_IMAGE
                scan.Save(@"outScan.png");
#endif
                    List<int> blanks = new List<int>();
                    for (int i = 0; i < data.Length; i++)
                        if (data[i] == -1)
                        {
                            blanks.Add(i);
                        }
                    int[] aBlanks = new int[blanks.Count];
                    blanks.CopyTo(aBlanks, 0);

                    if (rs.Correct(data, aBlanks, errorCodeWordCount, out confidence))
                    {
                        return rs.correcteddata;
                    }

                }  // for offsets

            }

            return null;
        }

        //another approach to read PDF417 barcodes. This version finds out the starting edge of 
        //each column, and then scans all rows of that column.
        int[] ReadSymbol2(BarCodeRegion r, float moduleLength, int rows, int cols, int errorCorrectionLevel, out float confidence)
        {
            confidence = 0F;

            int errorCodeWordCount = 2 << errorCorrectionLevel;
            if (errorCodeWordCount > rows * cols) return null;

#if DEBUG_IMAGE
            scan.Reset();
#endif
            ReedSolomon rs = new ReedSolomon();
            MyPoint[][] colPoints = FindCols(r, moduleLength, rows, cols);
            MyVectorF vdY = (r.A - r.C) / rows;
            SymbolReader sr = new SymbolReader(scan, MyPoint.Empty, MyPoint.Empty, 8, true, false, moduleLength);

            Hashtable[] data = new Hashtable[rows * cols];
            for (int i = 0; i < data.Length; i++) data[i] = new Hashtable(); //initialize

            float[] exposure = new float[] { 0.6f, 0.4f, 0.5f };
            for (int iExposure = 0; iExposure < exposure.Length; iExposure++)
            {
                //scan row: rows 0 and 1 are processed just to adjust the start point of each symbol
                int[] realRow = new int[rows * 3]; //due to oversampling x3
                for (int i = 0; i < realRow.Length; i++) realRow[i] = i / 3; //initial guess of row numbers

                for (int col = 1; col < cols + 2; col++)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        float[] offsets = new float[] { 0f, 0.3f, -0.3f };
                        for (int n = 0; n < offsets.Length; n++)
                        {
                            float offset = offsets[n];
                            MyPoint aa = findOffset(colPoints[row][col], vdY, offset);
                            MyPoint bb = findOffset(colPoints[row][col + 1], vdY, offset);
                            int index = row * 3 + n; //to index realRow;

#if DEBUG_IMAGE
                        scan.setPixel(aa, Color.Blue);
#endif

                            //obtain a list of possible symbols (module lenghts in E format)
                            //Since we give the start and end point of the symbol, we can 
                            //solve noise pixels. The result is a list of possible symbols, since
                            //gray modules lead to *2 solutions.
                            LinkedList<int[]> ES = sr.NextSymbol(aa, bb, exposure[iExposure]);

                            //for each possible symbol, tries to get the codeword
                            int codeword = -1, cluster = -1;
                            foreach (int[] E in ES)
                            {
                                float[] fE = new float[8];
                                for (int i = 0; i < 8; i++) fE[i] = (float)E[i];
                                float dist;
                                cluster = (realRow[index] % 3);
                                int c = moduleDecoder.GetCodeword(cluster * 3, fE, out dist);
                                if (c == -1 && realRow[index] < rows - 1)
                                {
                                    cluster = (cluster + 1) % 3;
                                    c = moduleDecoder.GetCodeword(cluster * 3, fE, out dist);
                                    if (c != -1) realRow[index] += 1;
                                }
                                if (c == -1 && realRow[index] > 0)
                                {
                                    cluster = (cluster + 1) % 3;
                                    c = moduleDecoder.GetCodeword(cluster * 3, fE, out dist);
                                    if (c != -1) realRow[index] -= 1;
                                }
                                if (c != -1) { codeword = c; break; }
                            }

                            //if symbol is correctly converted to codeword then add to the data array
                            if (codeword != -1)
                            {
                                if (col == 1)
                                {                                      
                                    realRow[index] = (codeword / 30) * 3 + cluster;
                                }
                                else
                                {
                                    int i = realRow[index] * cols + col - 2;
                                    if (i < data.Length)
                                    {
                                        Hashtable h = data[realRow[index] * cols + col - 2];
                                        if (!h.ContainsKey(codeword)) h.Add(codeword, 1);
                                        else h[codeword] = (int)h[codeword] + 1;
                                    }
                                }
                            }
                        }
                    }
                }
#if DEBUG_IMAGE
                scan.Save(@"outScan.png");
#endif



                LinkedList<int> blanks = new LinkedList<int>();
                int[] bytes = new int[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    int m = mostNotRepeat(data[i]);
                    if (m==-1) { blanks.AddLast(i); bytes[i] = -1; }
                    bytes[i] = m;
                }
                int[] aBlanks = new int[blanks.Count];
                blanks.CopyTo(aBlanks, 0);
                if (rs.Correct(bytes, aBlanks, errorCodeWordCount, out confidence))
                {
                    return rs.correcteddata;
                }

                /*ZXing.PDF417.Internal.EC.ErrorCorrection ec = new ZXing.PDF417.Internal.EC.ErrorCorrection();
                int errorLocationsCount;
                for (int i = 0; i < bytes.Length; i++) if (bytes[i] == -1) bytes[i] = 0;
                blanks.CopyTo(aBlanks, 0);
                if (ec.decode(bytes, errorCodeWordCount, aBlanks, out errorLocationsCount))
                    return bytes;
                */
            }
            return null;

        }

        //another approach to read PDF417 barcodes. This version finds out the starting edge of 
        //each column, and then scans all rows of that column.
        //this is improved version of ReadSymbol2
        int[] ReadSymbol3(BarCodeRegion r, float moduleLength, int rows, int cols, int errorCorrectionLevel, out float confidence)
        {
            confidence = 0F;

            int errorCodeWordCount = 2 << errorCorrectionLevel;
            if (errorCodeWordCount > rows * cols) return null;

#if DEBUG_IMAGE
            scan.Reset();
#endif
            ReedSolomon rs = new ReedSolomon();
            MyPoint[][] colPoints = FindCols2(r, ref moduleLength, rows, cols);
            MyVectorF vdY = (r.A - r.C) / rows;
            SymbolReader sr = new SymbolReader(scan, MyPoint.Empty, MyPoint.Empty, 8, true, false, moduleLength);

            Hashtable[] data = new Hashtable[rows * cols];
            for (int i = 0; i < data.Length; i++) data[i] = new Hashtable(); //initialize

            //scan row: rows 0 and 1 are processed just to adjust the start point of each symbol
            int[] realRow = new int[rows * 3]; //due to oversampling x3
            for (int i = 0; i < realRow.Length; i++) realRow[i] = i / 3; //initial guess of row numbers

            for (int col = 1; col < cols + 2; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    MyPoint aa = colPoints[row][col];
                    MyPoint bb = colPoints[row][col + 1];
                    int index = row * 3; //to index realRow;

                    MyPoint adjusted = sr.Adjust(aa);
                    if (adjusted != MyPoint.Empty) aa = adjusted;

                    adjusted = sr.Adjust(bb);
                    if (adjusted != MyPoint.Empty) bb = adjusted;

#if DEBUG_IMAGE
                    scan.setPixel(aa, Color.Lime);
                    scan.setPixel(bb, Color.Red);
#endif

                    //obtain a list of possible symbols (module lenghts in E format)
                    //Since we give the start and end point of the symbol, we can 
                    //solve noise pixels. The result is a list of possible symbols, since
                    //gray modules lead to *2 solutions.
                    LinkedList<int[]> ES = sr.NextSymbol(aa, bb, 0.5f);
                    //LinkedList<int[]> ES = sr.NextSymbolSimple(aa, bb);

                    //for each possible symbol, tries to get the codeword
                    int codeword = -1, cluster = -1;
                    foreach (int[] E in ES)
                    {
                        float[] fE = new float[8];
                        for (int i = 0; i < 8; i++) fE[i] = (float)E[i];
                        float dist;
                        cluster = (realRow[index] % 3);
                        int c = moduleDecoder.GetCodeword(cluster * 3, fE, out dist);
                        if (c == -1 && realRow[index] < rows - 1)
                        {
                            cluster = (cluster + 1) % 3;
                            c = moduleDecoder.GetCodeword(cluster * 3, fE, out dist);
                            if (c != -1) realRow[index] += 1;
                        }
                        if (c == -1 && realRow[index] > 0)
                        {
                            cluster = (cluster + 1) % 3;
                            c = moduleDecoder.GetCodeword(cluster * 3, fE, out dist);
                            if (c != -1) realRow[index] -= 1;
                        }
                        if (c != -1) { codeword = c; break; }
                    }

                    //if symbol is correctly converted to codeword then add to the data array
                    if (codeword != -1)
                    {
                        if (col == 1)
                        {
                            realRow[index] = (codeword / 30) * 3 + cluster;
                        }
                        else
                        {
                            int i = realRow[index] * cols + col - 2;
                            if (i < data.Length)
                            {
                                Hashtable h = data[realRow[index] * cols + col - 2];
                                if (!h.ContainsKey(codeword)) h.Add(codeword, 1);
                                else h[codeword] = (int)h[codeword] + 1;
                            }
                        }
                    }
                }
            }

#if DEBUG_IMAGE
            scan.Save(@"outScan.png");
#endif

            LinkedList<int> blanks = new LinkedList<int>();
            int[] bytes = new int[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                int m = mostNotRepeat(data[i]);
                if (m == -1) { blanks.AddLast(i); bytes[i] = -1; }
                bytes[i] = m;
            }
            int[] aBlanks = new int[blanks.Count];
            blanks.CopyTo(aBlanks, 0);
            //Console.WriteLine("aBlanks: " + aBlanks.Length);
            if (rs.Correct(bytes, aBlanks, errorCodeWordCount, out confidence))
            {
                return rs.correcteddata;
            }

            /*ZXing.PDF417.Internal.EC.ErrorCorrection ec = new ZXing.PDF417.Internal.EC.ErrorCorrection();
            int errorLocationsCount;
            for (int i = 0; i < bytes.Length; i++) if (bytes[i] == -1) bytes[i] = 0;
            blanks.CopyTo(aBlanks, 0);
            if (ec.decode(bytes, errorCodeWordCount, aBlanks, out errorLocationsCount))
                return bytes;
            */

            return null;

        }

        // Simplified version of the read symbol algorithm
        int[] ReadSymbolSimple(BarCodeRegion r, float moduleLength, int rows, int cols, int errorCorrectionLevel, bool hasEnd, out float confidence)
        {
            confidence = 0F;

            int errorCodeWordCount = 2 << errorCorrectionLevel;
            if (errorCodeWordCount > rows * cols) return null;
            ReedSolomon rs = new ReedSolomon();

            int[] data = new int[rows * cols];

            Grid g = new Grid((cols + 4) * 17 + 2, rows, r.C, r.A, r.D, r.B, true);
            MyVectorF vdYStart = (r.A - r.C) / rows;
            MyVectorF vdYStop = (r.B - r.D) / rows;

            // initialize data array with -1 (means error or erasure) that will contain data's codewords
            for (int i = 0; i < data.Length; i++) data[i] = -1; //initialize

            float offset =0.5f;
            for (int row = 0; row < rows; row++)
            {
                //Console.WriteLine("-------------ROW:" + row);
                MyPointF a = r.C + vdYStart * ((float)row + offset);
                MyPointF b = r.D + vdYStop * ((float)row + offset);
                SymbolReader sr = new SymbolReader(scan, a, b, 8, true, false, moduleLength);

                //find the start point: calculate theoric and adjust (if possible)
                MyPointF theoric = a;
                MyPoint previous = sr.Adjust(theoric);
                if (previous == MyPoint.Empty) previous = theoric;

                //scan row: rows 0 and 1 are processed just to adjust the start point of each symbol
                MyVectorF vdX = (b - a).Normalized;
                float ml = moduleLength * 17F; //initial value. Will be updated at each step.
                for (int col = 0; col < cols + 2; col++)
                {
                    //Calculates the end point. First estimates theoric point, then adjust it.
                    theoric = (hasEnd ? g.GetSamplePoint((float)((col + 1) * 17) + 0.5f, (float)row + offset) : previous + vdX * ml);
                    MyPoint adjusted = sr.Adjust(theoric);
                    if (adjusted == MyPoint.Empty) adjusted = theoric;

                    if (col > 1)
                    {
                        LinkedList<int[]> ES = sr.NextSymbolSimple(previous, adjusted);

                        //for each possible symbol, tries to get the codeword
                        int codeword = -1;
                        float minD = float.MaxValue;
                        foreach (int[] E in ES)
                        {
                            float[] fE = new float[8];
                            for (int i = 0; i < 8; i++) fE[i] = (float)E[i];
                            float dist;
                            int c = moduleDecoder.GetCodeword((row % 3) * 3, fE, out dist);
                            if (c != -1 && dist < minD) { minD = dist; codeword = c; break; }
                        }

                        //if symbol is correctly converted to codeword then add to the data array
                        if (codeword != -1)
                        {
                            data[row * cols + col - 2] = codeword;
                            if (!hasEnd) ml = (adjusted - previous).Length; //update module length
                        }
                    }

                    previous = adjusted;
                }
            }

            List<int> blanks = new List<int>();
            for (int i = 0; i < data.Length; i++)
                if (data[i] == -1)
                {
                    blanks.Add(i);
                }
            int[] aBlanks = new int[blanks.Count];
            blanks.CopyTo(aBlanks, 0);

            if (rs.Correct(data, aBlanks, errorCodeWordCount, out confidence))
            {
                return rs.correcteddata;
            }   

            return null;
        }

        MyPoint findOffset(MyPoint p, MyVectorF vd, float offset)
        {
            MyPoint q = p;
            if (offset != 0f)
            {
                int k = 1;
                while (q.Equals(q) && k<10)
                {
                    q = p + vd * (offset * (float)k);
                    k++;
                }
            }
            return q;
        }

        MyPoint[][] FindCols(BarCodeRegion r, float moduleLength, int rows, int cols)
        {
            MyPoint[][] colPoints = new MyPoint[rows][];
            MyVectorF vdYStart = (r.A - r.C) / rows;
            MyVectorF vdYEnd = (r.B - r.D) / rows;
            SymbolReader[] readers = new SymbolReader[rows];
            float[][] E = new float[rows][];
            for (int i = 0; i < rows; i++)
            {
                MyPointF a = r.C + vdYStart * ((float)i + 0.5f);
                MyPointF b = r.D + vdYEnd * ((float)i +0.5f);
                readers[i] = new SymbolReader(scan, a, b, 8, true, false, moduleLength);
                colPoints[i] = new MyPoint[cols+1+4];
                colPoints[i][0] = a;
            }

            Hashtable dists = new Hashtable();
            float lastDist=0f;
            float resolution = moduleLength;
            for (int col = 0; col < cols+4; col++)
            {
                dists.Clear();
                for (int row = 0; row < rows; row++)
                {
                    E[row] = readers[row].NextSymbol();
                    int d = (int)Math.Round(readers[row].Offset / resolution, MidpointRounding.AwayFromZero);
                    if (d < lastDist + (17+7))
                        if (!dists.ContainsKey(d)) dists.Add(d, 1);
                        else dists[d] = (int)dists[d] + 1;
                }
                int dist = most(dists);

                for (int row = 0; row < rows; row++)
                {
                    int d = (int)Math.Round(readers[row].Offset / resolution, MidpointRounding.AwayFromZero);
                    if (d == dist) colPoints[row][col + 1] = readers[row].Current;
                    else
                    {
                        colPoints[row][col + 1] = colPoints[row][0] + readers[row].Vd * (resolution * (float)dist);
                    }
                }
                lastDist=dist;
            }

            return colPoints;
        }

        MyPoint[][] FindCols2(BarCodeRegion r, ref float moduleLength, int rows, int cols)
        {
            MyPoint[][] colPoints = new MyPoint[rows][];

            var vdYStart = (r.A - r.C);
            var vdYEnd = (r.B - r.D);

            //scan barcode horizontally
            //build horizontal histogramm
            float[] maximums = FindColumns(r, cols);

            for (int iRow = 0; iRow < rows; iRow++)
                colPoints[iRow] = new MyPoint[cols + 1 + 4];

            var sumModules = 0f;
            var countModules = 0;

            for (int iCol = 0; iCol < maximums.Length; iCol++)
            {
                //calc column width
                var columnWidth = 0f;
                if (iCol < maximums.Length - 1)
                    columnWidth = maximums[iCol + 1] - maximums[iCol];
                else
                    columnWidth = maximums[iCol] - maximums[iCol - 1];

                //find rows
                var calculatedRows = FindRows(r, maximums[iCol], columnWidth, rows);

                //copy to output array
                for (int iRow = 0; iRow < rows; iRow++)
                {
                    //var k = (iRow + 0.5f) / rows;
                    var k = calculatedRows[iRow];
                    var from = r.C + vdYStart * k;
                    var to = r.D + vdYEnd * k;

                    colPoints[iRow][iCol] = MyPointF.Lerp(from, to, maximums[iCol]);

                    if (iCol > 1)
                    {
                        sumModules += (colPoints[iRow][iCol] - colPoints[iRow][iCol - 1]).Length / 17f;
                        countModules++;
                    }
                }
            }

            //calc avg moduleLength
            if (countModules > 0)
            {
                moduleLength = sumModules / countModules;
            }

            return colPoints;
        }

        private float[] FindRows(BarCodeRegion r, float startX, float columnWidthNorm, int rows)
        {
            var res = new float[rows];
            var vdYStart = (r.A - r.C);
            var vdYEnd = (r.B - r.D);
            //calc abs coordinates of Column rect
            var c = MyPointF.Lerp(r.C, r.D, startX);
            var d = MyPointF.Lerp(r.C, r.D, startX + columnWidthNorm);
            var a = MyPointF.Lerp(r.A, r.B, startX);
            var b = MyPointF.Lerp(r.A, r.B, startX + columnWidthNorm);
            var vdY = a - c;
            var vdXStart = d - c;
            var vdXEnd = b - a;

            //calc vert histogramm
            var histLength = (int)Math.Round(vdY.Length);
            var histogramm = new float[histLength];
            var linesCount = (int)Math.Round(vdXStart.Length);

            for (int iLine = 0; iLine < linesCount; iLine++)
            {
                var from = c + vdXStart * (iLine + 1) / (linesCount + 1);
                var to = a + vdXEnd * (iLine + 1) / (linesCount + 1);

                var br = new Bresenham(from, to); 
                var steps = br.Steps;
                var prev = false;
                while (!br.End())
                {
                    var pixel = scan.isBlack(br.Current.X, br.Current.Y);
                    if (prev != pixel)
                    {
                        var index = histLength * (steps - br.Steps) / steps;
                        for (int i = -2; i <= 2; i++)
                        {
                            var ii = index + i;
                            if (ii >= 0 && ii < histLength)
                                histogramm[ii] += 1 - Math.Abs(i) / 3f;
                        }
                    }

                    prev = pixel;
                    br.Next();
                }
            }

            //find best row
            var delta = (int)Math.Round((1f * histLength / rows) / 6f);
            for (int iRow = 0; iRow < rows; iRow++)
            {
                var bestRowPos = (iRow + 0.5f) / rows;
                var minDisp = float.MaxValue;
                var index = (int)Math.Round(histLength * bestRowPos);
                //find minimal dispersion of histogramm
                if (delta > 0)
                {
                    for (int i = index - delta; i <= index + delta; i++)
                        if (i >= 0 && i < histLength)
                        {
                            if (histogramm[i] < minDisp)
                            {
                                minDisp = histogramm[i];
                                bestRowPos = 1f * i / histLength;
                            }
                        }
                }

                //
                res[iRow] = bestRowPos;
            }

            return res;
        }

        private float[] FindColumns(BarCodeRegion r, int cols)
        {
            var vdYStart = (r.A - r.C);
            var vdYEnd = (r.B - r.D);

            var rowsCount = (int)((r.A - r.C).Length);
            var colsCount = (int)((r.D - r.C).Length);
            var histogramm = new int[colsCount * 2];
            var histLength = histogramm.Length;
            for (int iRow = 0; iRow < rowsCount; iRow += 2)
            {
                var k = (float)iRow / rowsCount;
                var from = r.C + vdYStart * k;
                var to = r.D + vdYEnd * k;

                var br = new Bresenham(from, to);
                var steps = br.Steps;
                var prev = false;
                while (!br.End())
                {
                    var pixel = scan.isBlack(br.Current.X, br.Current.Y);
                    if (prev == false && pixel == true)
                    {
                        var index = histLength * (steps - br.Steps) / steps;
                        for (int i = index - 1; i <= index + 1; i++)
                            if (i >= 0 && i < histLength)
                                histogramm[i]++;
                    }

                    prev = pixel;
                    br.Next();
                }
            }

            //find maximum for each column
            var maximums = new float[cols + 5];
            var d = histLength / (cols + 4) / 7;
            for (int iCol = 0; iCol <= cols + 4; iCol++)
            {
                var expectedX = histLength * iCol / (cols + 4);
                var best = 0;
                var bestX = 0f;
                for (var x = expectedX - d; x <= expectedX + d; x++)
                    if (x >= 0 && x < histLength)
                        if (histogramm[x] > best)
                        {
                            best = histogramm[x];
                            bestX = x;
                        }
                //normalized point x for maximum
                maximums[iCol] = bestX / histLength;
            }

            return maximums;
        }

        void ArrToConsole(int[] arr)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
                sb.Append(arr[i].ToString() + "\t");

            Console.WriteLine(sb.ToString());
        }

        // decode the data, according to the indicated encodation type
        ABarCodeData[] DecodeData(int[] data)
        {
            int dataLength = data[0] - 1;
            if (dataLength > data.Length || dataLength<1) return null;
            int[] encodedData = new int[dataLength];
            Array.Copy(data, 1, encodedData, 0, dataLength);
            return PDF417Decoder.Decode(encodedData, Encoding,900);
        }
    }
}
