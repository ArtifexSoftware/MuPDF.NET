using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.QR
{
#if OLD_QRReader

#if CORE_DEV
    public
#else
    internal
#endif
    partial class QRReader
    {
        //Scan image rows main step
        protected int scanRowStep = 1;


        //object that holds the bw image + methods to sample, follow vertices,...
        ImageScaner scan;
        //list of found QR and QRMicro codes found and correctly decoded.
        LinkedList<BarCodeRegion> candidates;
        //list of found and rejected finders, and QR codes to avoid exploring twice the same barcode
        LinkedList<BarCodeRegion> exclusion; 
        //objects to find finder patterns. PatternFinder mantains state, so a patternfinder can not be reused
        //until the scan is completed. patternFinder is used in the main scan loop (scanning horizontal lines). 
        //When a finder is found, the second patterFinder2 is used to find the other two finders of the QRcode.
        //Then the main scan can be resumed (since patternFinder is not lost).
        
        PatternFinderNoiseRow patternFinder, crossFinder;
        PatternFinderNoise patternFinder2, crossFinder2;
        PatternFinderNoise smallPatternFinder2, smallCrossFinder2;
        LinkedList<Pattern> foundPatterns;
        LinkedList<Pattern> removedPatterns;

        FoundBarcode[] Scan()
        {
            scan = new ImageScaner(BWImage);
            patternFinder = new PatternFinderNoiseRow(QRFinder.finder,true, true, 2);
            crossFinder = new PatternFinderNoiseRow(QRFinder.finder, true, true, 2, patternFinder.Hash);
            patternFinder2 = new PatternFinderNoise(BWImage, QRFinder.finder[0], true, 2, patternFinder.Hash); //both share the same cache to speed up detection.
            crossFinder2 = new PatternFinderNoise(BWImage, QRFinder.finder[0], true, 2, patternFinder.Hash); //both share the same cache to speed up detection.
            smallPatternFinder2 = new PatternFinderNoise(BWImage, QRFinder.smallFinder[0], false, 2);
            smallCrossFinder2 = new PatternFinderNoise(BWImage, QRFinder.smallFinder[0], false, 2, smallPatternFinder2.Hash);
            candidates = new LinkedList<BarCodeRegion>();
            exclusion = new LinkedList<BarCodeRegion>();
            foundPatterns = new LinkedList<Pattern>();


#if DEBUG_IMAGE
           // bwSourceImage.GetAsBitmap().Save(@"out.png");
#endif
            //main loop to scan horizontal lines
            for (int y = 0; y < height && (ExpectedNumberOfBarcodes <= 0 || candidates.Count < ExpectedNumberOfBarcodes); y += scanRowStep)
            {
                ScanRow(y, BWImage.GetRow(y));
                // timeout check
                if (IsTimeout())
                    break;
            }

            ArrayList result = new ArrayList();
            foreach (BarCodeRegion c in candidates)
            {
                FoundBarcode foundBarcode = new FoundBarcode();
				foundBarcode.BarcodeType = SymbologyType.QRCode;
                
                String data = "";
                if (c.Data!=null) foreach (ABarCodeData d in c.Data) {
                    string s=d.ToString();
                    data += s;
                    if (s.StartsWith("]Q2\\MI")) {
                        int pos1=s.IndexOf("\\MO1");
                        int pos2=s.IndexOf("\\MF001\\MY");
                        foundBarcode.StructureAppendIndex=Convert.ToInt32(s.Substring(6,pos1-6));
                        foundBarcode.StructureAppendCount=Convert.ToInt32(s.Substring(pos1+4,pos2-(pos1+4)));
                    }
                }
                foundBarcode.Value = data;
                foundBarcode.Color = Color.Blue;
                foundBarcode.Polygon = new SKPoint[5] { c.A, c.B, c.D, c.C, c.A };
				byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
				GraphicsPath path = new GraphicsPath(foundBarcode.Polygon, pointTypes);
				foundBarcode.Rect = Rectangle.Round(path.GetBounds());
                foundBarcode.Confidence = c.Confidence;
                result.Add(foundBarcode);

            }

            if (this.mergePartialBarcodes)
            {
                //group partial barcodes by their count
                ArrayList mergedResults = new ArrayList();
                SortedDictionary<int, FoundBarcode[]> structureAppend = new SortedDictionary<int, FoundBarcode[]>();
                foreach (FoundBarcode f in result) 
                    if (f.StructureAppendCount!=-1)
                    {
                        if (!structureAppend.ContainsKey(f.StructureAppendCount))
                        {
                            structureAppend.Add(f.StructureAppendCount, new FoundBarcode[f.StructureAppendCount]);
                        }
                        if (f.StructureAppendIndex >= 1 && f.StructureAppendIndex <= f.StructureAppendCount)
                        {
                            structureAppend[f.StructureAppendCount][f.StructureAppendIndex - 1] = f;
                        }
                    }
                    else
                    {
                        mergedResults.Add(f);
                    }

                foreach(FoundBarcode[] mergedResult in structureAppend.Values) {
                    bool hasAllParts = true;
                    foreach (FoundBarcode f in mergedResult) if (f == null) hasAllParts = false;
                    if (hasAllParts)
                    {
                        FoundBarcode m = new FoundBarcode();
                        int minX=Int32.MaxValue, minY=Int32.MaxValue, maxX=Int32.MinValue, maxY=Int32.MinValue;
                        m.Value = "";
                        m.Confidence = 1f;
                        foreach (FoundBarcode f in mergedResult)
                        {
                            int pos2 = f.Value.IndexOf("\\MF001\\MY");
                            m.Value += f.Value.Substring(pos2+9);
                            this.updateMinMaxPolygon(f.Polygon, ref minX, ref minY, ref maxX, ref maxY);
                            m.Confidence *= f.Confidence;
                        }
                        m.Polygon = new SKPoint[5] { new SKPoint(minX, minY), new SKPoint(maxX, minY), new SKPoint(maxX, maxY), new SKPoint(minX, maxY), new SKPoint(minX, minY) };
                        m.Color = Color.Blue;
                        mergedResults.Add(m);
                    }
                    else
                    {
                        foreach (FoundBarcode f in mergedResult) if (f!=null) mergedResults.Add(f);
                    }
                }

                result = mergedResults; 
            }
            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }

        void updateMinMaxPolygon(SKPoint[] p, ref int minX, ref int minY, ref int maxX, ref int maxY)
        {
            foreach (SKPoint q in p)
            {
                if (minX > q.X) minX = q.X;
                if (minY > q.Y) minY = q.Y;
                if (maxX < q.X) maxX = q.X;
                if (maxY < q.Y) maxY = q.Y;
            }
        }

        //Scans a horizontal line looking for finder patterns (1011101). 
        //For each finder pattern found tries to find the finder.
        //If the finder is found, then tries to locate the other two finders of the QR code. 
        //Thus, uses the patternFinder2 to trace lines (0, 90, 180, 270 degree, 4 directions). If another
        //finder is found, traces 2 more perpendicular lines (at 90 and 270 degrees from the incoming direction).
        //Foreach valid location (group of 3 finders), the decoding algorithm is executed. Notice that the algorithm
        //can find goups of 3 finders comming from different QRcodes, close one from each other.
        private void ScanRow(int y, XBitArray row)
        {
            //expand size to find micro QR
            var size = row.Size + 1;

            //look for the finder
            patternFinder.NewSearch(row, 0, size, 1, 0);
            while (patternFinder.NextPattern()!=null)
            {
                MyPoint a = new MyPoint(patternFinder.First, y);
                MyPoint b = new MyPoint(patternFinder.Last, y);
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

            //clean old patterns and process them
            removedPatterns = Pattern.RemoveOldPatterns(foundPatterns, y);
            foreach (Pattern p in removedPatterns)
            {
                //Debug.WriteLine(p.xIn + " " + p.xEnd);
                StackedPattern sp = (StackedPattern)p;
                ProcessPattern(sp);
            }
        }

        private bool ProcessPattern(StackedPattern p)
        {
            MyPoint a, b;
            p.MidPoints(out a, out b);
            MyPoint center = (a+b)/2;

            foreach (BarCodeRegion br in candidates) if (br.In(center)) return false;

#if FIND_PATTERN
                MyPoint Y = new MyPoint(0, 1);
                SquareFinder f = new SquareFinder(a + Y, b + Y, a, b, 7);
                candidates.AddLast(f);
#else
            //checks if a vertical pattern crosses the horizontal pattern in the middle
            MyPoint pUp, pDown;
            QRFactory factory = new QRFactory();
            QRFinder finder = null;
            if (SquareFinder.CheckCrossPattern(scan, a, b, center, crossFinder, factory, out pUp, out pDown))
            {
                //calculates black - white threshold using the pixels in the pattern
                scan.setBWThreshold(a, b);
                //tries to create a new finder from horizontal and vertical pattenrs
                //finder = (QRFinder)SquareFinder.IsFinder(scan, a, b, center, patternFinder2, crossFinder2, factory);
                
                finder = (QRFinder)SquareFinder.IsFinder(scan, a, b, pUp, pDown, factory);
                if (finder != null)
                {
                    //add finder to the exclusion list, so no more patterns in this area will be processed.
                    exclusion.AddFirst(finder);
#if FIND_FINDER
                                QRLocation l = new QRLocation(finder, finder, finder);
                                candidates.AddLast(l);
#else
                    //detect finders around the first found finder (at 0, 90, 180, 270 degrees), and
                    //finders at 90 and 270º from them. Max: 4 * 2 leafs.
                    //Returns an array of valid triplets of finders. Finders are always normalized (rotated)
                    //to match a top-left, top-right and bottom-left finders.
                    ArrayList locations = QRLocation.Scan(scan, patternFinder2, crossFinder2, smallPatternFinder2, smallCrossFinder2, finder);
                    QRSymbol symbol = null;
                    BarCodeRegion best = null;
                    if (locations != null && locations.Count > 0)
                    {
                        foreach (QRLocation location in locations)
                        {
#if FIND_LOCATION
                            candidates.AddLast(location);
                            exclusion.AddFirst(location);
#else
							int[] columns = HoughScan(location.A, location.B, location.C, location.D, location.Wbl / 7f, location.Hbl / 7f);
	                        int[] rows = HoughScan(location.B, location.D, location.A, location.C, location.Hbl / 7f, location.Wbl / 7f);
	                        int version = (int)Math.Round((float)(columns.Length - 1 - 17) / 4f); //-1 because columns.length has 1 counts transitions, and transitions are modules+1
	                        if (version < 1) version = 1; else if (version > 44) version = 44;
	                        symbol = new QRSymbol(version);
	                        if (columns.Length == rows.Length)
	                        {
		                        if (symbol.SideModuleCount == columns.Length - 1)
		                        {
			                        scanHough(location, symbol, columns, rows);
			                        BarCodeRegion br = ReadSymbol(symbol, location);
			                        if (br != null && (best == null || best.Confidence < br.Confidence)) { best = br; break; }
		                        }
	                        }
	                        else
	                        {
		                        columns = HoughScan2(location.A, location.B, location.C, location.D, location.Wbl / 7f, location.Hbl / 7f, symbol.SideModuleCount);
		                        rows = HoughScan2(location.B, location.D, location.A, location.C, location.Hbl / 7f, location.Wbl / 7f, symbol.SideModuleCount);
		                        scanHough(location, symbol, columns, rows);
		                        BarCodeRegion br = ReadSymbol(symbol, location);
		                        if (br != null && (best == null || best.Confidence < br.Confidence)) { best = br; break; }
	                        }

	                        symbol = CountModulesForQRCode(location);
	                        if (symbol != null)
	                        {
		                        scanSimple(symbol, location);
		                        BarCodeRegion br = ReadSymbol(symbol, location);
		                        if (br != null && (best == null || best.Confidence < br.Confidence)) { best = br; break; }
		                        else if (scanAdaptive(symbol, location))
		                        {
			                        //second pass
			                        br = ReadSymbol(symbol, location);
			                        if (br != null && (best == null || best.Confidence < br.Confidence)) { best = br; break; }
		                        }
	                        }
#endif
						}
                    }
                    else //Try microQR
                    {
                        //try different angles (0, 90, 180, 270)
                        for (int i = 0; i < 4; i++)
                        {
                            symbol = CountModulesForMicroQRCode(scan, finder);
                            if (symbol != null)
                            {
                                //recognize
                                best = ReadSymbol(symbol, finder);
                                //calc correct MicroQR rectangle
                                CalcMicroQRrectangle(finder, symbol);
                                //
                                break;
                            }
                            //rotate on 90 degree
                            finder.RightNormal = finder.RightNormal.Rotate((float) (Math.PI / 2));
                            finder.DownNormal = finder.DownNormal.Rotate((float) (Math.PI / 2));
                        }
                    }

                    if (best != null)
                    {
                        candidates.AddFirst(best);
                        exclusion.AddFirst(best);
                    }
#endif
                }
            }
#endif
            return false;
        }

        private void CalcMicroQRrectangle(QRFinder finder, QRSymbol symbol)
        {
            var points = new MyPointF[4] { finder.A, finder.B, finder.C, finder.D };
            //find main corner
            var axis = (finder.RightNormal + finder.DownNormal);
            var O = new MyPointF(-axis.X, -axis.Y) * 100000;//virtual center of coordinates
            var min = float.MaxValue;
            var corner = finder.A;
            foreach (var p in points)
            {
                var r = (p - O).LengthSq;//distance from center of coordinates
                if (r < min)//find point nearest to O 
                {
                    corner = p;
                    min = r;
                }
            }
            //calc ABCD
            var modR = finder.ModuleRight.Length * symbol.SideModuleCount;
            var modD = finder.ModuleDown.Length * symbol.SideModuleCount;
            var right = new MyPointF(finder.RightNormal.X * modR, finder.RightNormal.Y * modR);
            var down = new MyPointF(finder.DownNormal.X * modD, finder.DownNormal.Y * modD);
            var A = corner + down;
            var B = corner + down + right;
            var C = corner;
            var D = corner + right;
            //set ABCD
            finder.SetCorners(A, B, C, D);
        }

        private void scanHough(QRLocation location, QRSymbol symbol, int[] cols, int[]rows)
        {
#if DEBUG_IMAGE
            scan.Reset();         
#endif
            MyPointF A = location.A, B = location.B, C = location.C, D = location.D;
            B.Y += 1; //right down corner
            C.X -= 1; //upper left corner

            MyVectorF vdL = C - A;
            MyVectorF vdR = D - B;
            bool[][] m=symbol.MaskedBitarray;
            int lRows=rows[rows.Length-1]-rows[0];
            int lCols=cols[cols.Length-1]-cols[0];
            for (int row = 0; row < symbol.SideModuleCount; row++)
            {
                float r=((float)(rows[row]+rows[row+1])/2f-(float)rows[0])/(float)lRows;
                MyPointF L = A + vdL * r;
                MyPointF R = B + vdR * r;
                for (int col = 0; col < symbol.SideModuleCount; col++)
                {
                    float c=((float)(cols[col]+cols[col+1])/2f-(float)cols[0])/(float)lCols;
                    MyPointF p = L * (1f - c) + R * c;
                    bool isBlack = scan.isBlackSample(p, 0f);
                    symbol.MaskedBitarray[symbol.SideModuleCount-1-row][col] = isBlack;
                }
            }            
#if DEBUG_IMAGE
            scan.Save(@"outSamples.png");           
#endif
        }
                

        private int[] HoughScan(MyPointF A, MyPointF B, MyPointF C, MyPointF D, float hModuleLength, float vModuleLength) {
            MyVectorF vdL = C - A;
            MyVectorF vdR = D - B;
            float width = (B - A).Length;
            float height=(C-A).Length;
            int nVModules = (int)Math.Round(height / vModuleLength);
            int nHModules = (int)Math.Round(width / hModuleLength);
            float incY = 1f / (1f* nVModules); //one sample per row
            int MODULE_DISCRETIZATION = 5;
            int N=(nHModules+1)*MODULE_DISCRETIZATION; //add half module at the left, and half at the right
            int[] h = new int[N];
            MyVectorF vdX = (B - A).Normal;
            A = A - vdX * (hModuleLength / 2f);
            B = B + vdX * (hModuleLength / 2);
            for (float y = incY / 2f; y <= 1f; y += incY)
            {
                MyPointF L = A + vdL * y;
                MyPointF R = B + vdR * y;
                Bresenham br = new Bresenham(L, R);
                float length=(R-L).Length;
                bool isBlack = scan.isBlack(br.Current);
                while (!br.End())
                {
                    if (isBlack == scan.isBlack(br.Current)) { }
                    else
                    { //histogram each transition
                        float x = (br.CurrentF - L).Length / length; //from 0..1
                        int iX = (int)Math.Round(x * N);
                        if (iX < 0) iX = 0; else if (iX >= N) iX = N - 1;
                        h[iX]++;
                        isBlack = !isBlack;
                    }
                    br.Next();
                }
            }

            //find local max around moduleLength*i
            /*
            LinkedList<int> transitions = new LinkedList<int>();
            int currentModuleLength = h.Length / (nModules+1);
            int i = -currentModuleLength / 2;
            int first = -1;
            while (i < N)
            {
                int halfModule=currentModuleLength/2;
                int x = i +currentModuleLength - halfModule;
                int max = -1;
                for (int j = 0; j <= currentModuleLength; j++, x++) if (x < N && isMax(h, x)) { max = x; break;}
                if (max==-1 ) max=i+currentModuleLength;//if not found, use the same step
                if (max < h.Length)
                {
                    if (transitions.Count == 0) first = max;
                    else currentModuleLength = (max - first) / transitions.Count;
                    transitions.AddLast(max);
                }
                i = max;
            }
             * */
            LinkedList<int> maxs=new LinkedList<int>();
            int lastMax = -1;
            for (int i = 0; i < h.Length; i++) if (isMax(h, i)) { maxs.AddLast(i); lastMax = i; }
            int[] aMaxs = new int[maxs.Count];
            maxs.CopyTo(aMaxs, 0);

            aMaxs = removeSmallPeaks(h, aMaxs);

            aMaxs = removePeaksWithoutValley(h, aMaxs);

            aMaxs = removePeaksTooClose(h, aMaxs, MODULE_DISCRETIZATION / 2);

            
            LinkedList<int> transitions = new LinkedList<int>();
            for (int i = 0; i < aMaxs.Length; i++)
            {
                    if (i>0)
                    {
                        int lastPeak=aMaxs[i-1];
                        int d = aMaxs[i] - lastPeak;
                        float lostPeaks = (float)d / (float)MODULE_DISCRETIZATION;
                        if (lostPeaks > 1.7f)
                        {
                            int n = (int)Math.Round(lostPeaks); //n>=2
                            float l = (float)d / (float)n;
                            for (int j = 1; j < n; j++) transitions.AddLast(lastPeak + (int)Math.Round((float)j * l)); //interpolate lost peaks
                        }
                    }
                    transitions.AddLast(aMaxs[i]);
            }            

            /*                        
            Array.Sort<int>(aMaxs, new InverseComparer(h));

            int minModuleLength = 3; // MODULE_DISCRETIZATION / 2; //5-->2
            ArrayList transitions = new ArrayList();
            for (int i = 0; i < aMaxs.Length; i++)
            {
                int pos = canAddNewMax(transitions, aMaxs[i], minModuleLength);
                if (pos != -1) transitions.Insert(pos, aMaxs[i]);
            }*/
            int[] aTransitions=new int[transitions.Count];
            transitions.CopyTo(aTransitions, 0);
            return aTransitions;
        }

	    private int[] HoughScan2(MyPointF A, MyPointF B, MyPointF C, MyPointF D, float hModuleLength, float vModuleLength, int nModules)
	    {
		    MyVectorF vdL = C - A;
		    MyVectorF vdR = D - B;
		    float width = (B - A).Length;
		    float height = (C - A).Length;
		    int nVModules = (int)Math.Round(height / vModuleLength);
		    int nHModules = (int)Math.Round(width / hModuleLength);
		    float incY = 1f / (1f * nVModules); //one sample per row
		    int MODULE_DISCRETIZATION = 5;
		    int N = (nHModules + 1) * MODULE_DISCRETIZATION; //add half module at the left, and half at the right
		    int[] h = new int[N];
		    MyVectorF vdX = (B - A).Normal;
		    A = A - vdX * (hModuleLength / 2f);
		    B = B + vdX * (hModuleLength / 2);
		    for (float y = incY / 2f; y <= 1f; y += incY)
		    {
			    MyPointF L = A + vdL * y;
			    MyPointF R = B + vdR * y;
			    Bresenham br = new Bresenham(L, R);
			    float length = (R - L).Length;
			    bool isBlack = scan.isBlack(br.Current);
			    while (!br.End())
			    {
				    if (isBlack == scan.isBlack(br.Current)) { }
				    else
				    { //histogram each transition
					    float x = (br.CurrentF - L).Length / length; //from 0..1
					    int iX = (int)Math.Round(x * N);
					    if (iX < 0) iX = 0; else if (iX >= N) iX = N - 1;
					    h[iX]++;
					    isBlack = !isBlack;
				    }
				    br.Next();
			    }
		    }

		    int[] aTransitions = new int[nModules + 1];
		    aTransitions[0] = findFirstMax(h, 0, 1, MODULE_DISCRETIZATION);
		    aTransitions[nModules] = findFirstMax(h, h.Length - 1, -1, MODULE_DISCRETIZATION);
		    float moduleLength = ((float)(aTransitions[nModules] - aTransitions[0])) / ((float)nModules);
		    float pos = (float)aTransitions[0];
		    for (int i = 1; i < nModules; i++)
		    {
			    pos += moduleLength;
			    int estimated = (int)Math.Round(pos);
			    int prevMax = findFirstMax(h, estimated, -1, MODULE_DISCRETIZATION);
			    int nextMax = findFirstMax(h, estimated, 1, MODULE_DISCRETIZATION);
			    if (prevMax - aTransitions[i - 1] < MODULE_DISCRETIZATION / 2) aTransitions[i] = nextMax;
			    else if (estimated - prevMax < nextMax - estimated) aTransitions[i] = prevMax;
			    else aTransitions[i] = nextMax;
		    }
		    return aTransitions;
	    }

	    int findFirstMax(int[] h, int pos, int inc, int max)
	    {
		    int offset = inc < 0 ? 1 : 0;
		    int p = pos + offset * inc;
		    while (offset < max && p >= 0 && p < h.Length)
		    {
			    if (isMax(h, p)) break;
			    offset++;
			    p += inc;
		    }
		    return p;
	    }

		class InverseComparer : IComparer<int> { 
            private int[] h; 
            public InverseComparer(int[] h) { this.h = h; } 
            public int Compare(int x, int y) { return h[y] - h[x]; } 
        }

        //can we add a new transition x in transitions? return position
        private int canAddNewMax(ArrayList transitions, int x, int minModuleLength)
        {
            if (transitions.Count==0) return 0; //yes, add to 0
            int i = 0; 
            while (i < transitions.Count && (int)transitions[i] < x) i++;
            if (i > 0) //check if i is far enough from the previous one
            {
                int l = x - (int)transitions[i - 1];
                if (l <= minModuleLength) return -1;
            }
            if (i <transitions.Count) //check if i is far enough from the previous one
            {
                int r = (int)transitions[i] - x;
                if (r <= minModuleLength) return -1;
            }
            return i;
        }

        private bool isMax(int[] h, int x)
        {
            //exists a valley in betweein lastMax and x?
            /*bool deep = false;
            if (lastMax == -1) { deep = true; }
            else
            {
                int yDeep = h[lastMax] / 2;
                int i = lastMax + 1;
                while (i < x && !deep)
                    if (h[i] <= yDeep) deep = true;
                    else i++;
            }
            if (!deep) return false;
            */
            //it is a max?
            int l=x-1;
            while (l>=0 && h[x] == h[l]) l--;

            int r = x + 1;
            while (r<h.Length && h[x] == h[r]) r++;

            return (l < 0 || h[x] > h[l]) && (r == h.Length || h[x] > h[r]);
        }

        private int[] removeSmallPeaks(int[] h, int[] peaks)
        {
            int maxPeak = -1;
            for (int i = 0; i < peaks.Length; i++) if (maxPeak == -1 || h[peaks[i]] > h[maxPeak]) maxPeak = peaks[i];

            int threshold = h[maxPeak] / 4;
            LinkedList<int> l = new LinkedList<int>();
            for (int i = 0; i < peaks.Length; i++)
                if (h[peaks[i]] > threshold) l.AddLast(peaks[i]);

            int[] filtered = new int[l.Count];
            l.CopyTo(filtered, 0);
            return filtered;
        }

        private int[] removePeaksWithoutValley(int[] h, int[] peaks)
        {
            LinkedList<int> l = new LinkedList<int>();

            //find deeper points between peaks
            ArrayList aPeaks=new ArrayList(peaks);
            ArrayList depth = new ArrayList();
            for (int i = 0; i < peaks.Length - 1; i++)
            {
                int current= peaks[i];
                int next=peaks[i+1];
                int min=current;
                for (int j = current + 1; j < next; j++)
                    if (h[j] < h[min]) min = j;
                depth.Add(h[min]);
            }

            //remove peaks without enough deep around
            bool done = false;
            while (!done)
            {
                int found = -1;
                for (int i = 0; i < depth.Count && found == -1; i++)
                {
                    int lPeak = h[(int)aPeaks[i]];
                    int rPeak = h[(int)aPeaks[i + 1]];
                    int d = (int)depth[i];
                    if (d > lPeak / 3 || d > rPeak / 3) found = i;
                }
                if (found != -1) //found a valley not deep enough
                {
                    int lPeak = h[(int)aPeaks[found]];
                    int rPeak = h[(int)aPeaks[found + 1]];
                    if (lPeak < rPeak) { 
                        aPeaks.RemoveAt(found);
                        if (found>1 && (int)depth[found] < (int)depth[found - 1]) depth[found - 1] = depth[found];
                        depth.RemoveAt(found);
                    }
                    else
                    {
                        aPeaks.RemoveAt(found+1);
                        if (found +1 <depth.Count && (int)depth[found] < (int)depth[found + 1]) depth[found + 1] = depth[found];
                        depth.RemoveAt(found);
                    }
                }
                else done = true;
            }

            int[] filtered = new int[aPeaks.Count];
            aPeaks.CopyTo(filtered, 0);
            return filtered;
        }


        private int[] removePeaksTooClose(int[] h, int[] peaks, int minDist)
        {
            LinkedList<int> l = new LinkedList<int>();

            ArrayList aPeaks = new ArrayList();
            aPeaks.Add(peaks[0]);
            for (int i = 1; i < peaks.Length; i++)
            {
                int dist = peaks[i] - peaks[i - 1];
                if (dist > minDist) aPeaks.Add(peaks[i]);
            }

            int[] filtered = new int[aPeaks.Count];
            aPeaks.CopyTo(filtered, 0);
            return filtered;
        }





        //count modules and extract them for MicroQR
        //Uses a regular (no perspective deformation) grid using horizontal and vertical vectors from the finder
        private QRSymbol CountModulesForMicroQRCode(ImageScaner scan, QRFinder finder)
        {
            bool[][] patternUnit = new bool[1][];
            patternUnit[0] = new bool[2];

            Grid grid = new Grid( finder.Center(), finder.RightNormal * finder.Width / 7F, finder.DownNormal * finder.Height/7F);

            // horizontal checks
            int vx = 0;
            for (; vx < 5; ++vx)
            {
                try
                {
                    grid.ExtractPointsRegular(scan, patternUnit, new MyPoint(4 + 2 * vx, -3), new MyPoint(0, 0), 2, 1);
                    if (!(!patternUnit[0][0] && patternUnit[0][1])) // test for alternating timing pattern
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }

            // vertical checks
            patternUnit = new bool[2][];
            patternUnit[0] = new bool[1];
            patternUnit[1] = new bool[1];
            int vy = 0;
            for (; vy < 5; ++vy)
            {
                try
                {
                    grid.ExtractPointsRegular(scan,patternUnit,  new MyPoint(-3, 4 + 2 * vy), new MyPoint(0, 0), 1, 2);
                    if (!(!patternUnit[0][0] && patternUnit[1][0])) // test for alternating timing pattern
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }

            int version = Math.Min(vx, vy);
            if (version < 2 || version > 5)
            {
                return null;
            }

            QRSymbol symbol= new QRSymbol(39 + version);
            int cy = symbol.MaskedBitarray.Length;
            int cx = cy > 0 ? symbol.MaskedBitarray[0].Length : 0;
            grid.ExtractPointsRegular(scan, symbol.MaskedBitarray, new MyPoint(-3, -3), new MyPoint(0, 0), cx, cy);

            return symbol;
        }



        //estimate module count from distance between finders (roughVersion).
        //If roughVersion is >7 then count modules sampling the image.
        //Then samples the image locating alignment patterns. The number of sampling
        //regions depend on the number of alignment patterns. For each region, an irregular
        //sampling is done (allowing perspective deformation).
        private QRSymbol CountModulesForQRCode(QRLocation location)
        {
#if DEBUG_IMAGE
            scan.Reset();
#endif
            QRSymbol symbol;
            // determine the symbol version
            int roughVersion = (int)Math.Round((location.BaselineLength / location.NominalWidth - 10) / 4);
            if (roughVersion < 1) return null;
            if (roughVersion < 7)
            {
                symbol = new QRSymbol(roughVersion);
            }
            else
            {
                int fineVersion = QRUtils.ExtractSymbolVersion(scan, location);
                if (fineVersion < 0)
                {
                    // let's give it one more try, maybe the image is so bad that we miscalculated the version slightly
                    if (roughVersion == 7)
                    {
                        fineVersion = 6;
                    }
                    else fineVersion = roughVersion;
                }
                if (fineVersion > 40) return null;
                symbol = new QRSymbol(fineVersion);
            }

            location.NominalWidth = (location.UpperLeft - location.UpperRight).Length / (symbol.SideModuleCount - 7);
            location.NominalHeight = (location.UpperLeft - location.BottomLeft).Length / (symbol.SideModuleCount - 7);
            if (symbol.patternCoords == null) return null;
            int patternsPerSide = symbol.patternCoords.Length;

            //horizontal and vertical directions for each finder
            MyVectorF luRight = location.LU.RightNormal * location.LU.Width / 7F;
            MyVectorF luDown = location.LU.DownNormal * location.LU.Height / 7F;
            MyVectorF ldRight = location.LD.RightNormal * location.LD.Width / 7F;
            MyVectorF ldDown = location.LD.DownNormal * location.LD.Height / 7F;
            MyVectorF ruRight = location.RU.RightNormal * location.RU.Width / 7F;
            MyVectorF ruDown = location.RU.DownNormal * location.RU.Height / 7F;

            //If only one sampling region (no alignment patterns)
            if (patternsPerSide != 0)
            {
                //fill region coordinates (numOfAlignmentPatterns+2)*(numOfAlignmentPatterns+2)
                int N = patternsPerSide + 2;
                MyPointF[][] align = symbol.align= new MyPointF[N][];
                float[][] moduleW = symbol.moduleW = new float[N][];
                float[][] moduleH = symbol.moduleH = new float[N][];
                for (int y = 0; y < N; y++)
                {
                    align[y] = new MyPointF[N];
                    moduleW[y] = new float[N];
                    moduleH[y] = new float[N];
                    for (int x = 0; x < N; x++)
                    {
                        align[y][x] = MyPointF.Empty;
                        moduleW[y][x] = 0f;
                        moduleH[y][x] = 0f;
                    }
                }

                //first: alignment points from QRFinders corners
                align[0][0] = location.UpperLeft - luRight * 3.5F - luDown * 3.5F;
                align[0][1] = location.UpperLeft + luRight * 2.5F - luDown * 3.5F;
                align[1][0] = location.UpperLeft - luRight * 3.5F + luDown * 2.5F;
                align[1][1] = location.UpperLeft + luRight * 2.5F + luDown * 2.5F;
                moduleW[0][0] = moduleW[0][1] = moduleW[1][0] = moduleW[1][1] = location.LU.ModuleWidth;
                moduleH[0][0] = moduleH[0][1] = moduleH[1][0] = moduleH[1][1] = location.LU.ModuleHeight;

                align[0][N - 1] = location.UpperRight + ruRight * 3.5F - ruDown * 3.5F;
                align[1][N - 1] = location.UpperRight + ruRight * 3.5F + ruDown * 2.5F;
                moduleW[0][N - 1] = moduleW[1][N - 1] = location.RU.ModuleWidth;
                moduleH[0][N - 1] = moduleH[1][N - 1] = location.RU.ModuleHeight;

                align[N - 1][0] = location.BottomLeft - ldRight * 3.5F + ldDown * 3.5F;
                align[N - 1][1] = location.BottomLeft + ldRight * 2.5F + ldDown * 3.5F;
                moduleW[N - 1][0] = moduleW[N - 1][1] = location.LD.ModuleWidth;
                moduleH[N - 1][0] = moduleH[N - 1][1] = location.LD.ModuleHeight;

                //If the last alignment pattern falls close to the finder it is not draws. 
                //So, alignment coordinates are computed from the finder coords.
                int d = symbol.SideModuleCount - symbol.patternCoords[patternsPerSide - 1];
                if (d <= 11)
                {
                    float dd = (float)(d) - 3.5F;
                    align[0][N - 2] = location.UpperRight - ruRight * dd - ruDown * 3.5F;
                    align[1][N - 2] = location.UpperRight - ruRight * dd + ruDown * 2.5F;
                    moduleW[0][N - 2] = moduleW[1][N - 2] = location.RU.ModuleWidth;
                    moduleH[0][N - 2] = moduleH[1][N - 2] = location.RU.ModuleHeight;

                    align[N - 2][0] = location.BottomLeft - ldRight * 3.5F - ldDown * dd;
                    align[N - 2][1] = location.BottomLeft + ldRight * 2.5F - ldDown * dd;
                    moduleW[N - 2][0] = moduleW[N - 2][1] = location.LD.ModuleWidth;
                    moduleH[N - 2][0] = moduleH[N - 2][1] = location.LD.ModuleHeight;
                }

                //second: top border. Adaptative interpolation between the top left and top right finders.
                //Module length starts with top-left module length, and ends with top-right module length.
                for (int i = 1; i < patternsPerSide - 1; i++)
                {
                    MyPointF fromLeft = align[0][0] + luRight * symbol.patternCoords[i];
                    MyPointF fromRight = align[0][N - 1] - ruRight * (symbol.SideModuleCount - 1 - symbol.patternCoords[i]);
                    float rightFactor = (float)symbol.patternCoords[i] / (float)(symbol.SideModuleCount - 1);
                    float leftFactor = 1F - rightFactor;
                    rightFactor *= rightFactor;
                    leftFactor *= leftFactor;
                    MyPointF mean = (fromLeft * leftFactor + fromRight * rightFactor) / (leftFactor + rightFactor);
                    align[0][1 + i] = mean;
                    moduleW[0][1 + i] = (moduleW[0][0] * leftFactor + moduleW[0][N - 1] * rightFactor) / (leftFactor + rightFactor);
                    moduleH[0][1 + i] = (moduleW[0][0] * leftFactor + moduleW[0][N - 1] * rightFactor) / (leftFactor + rightFactor);
                }

                //third: left border. Also adaptative interpolation between top-left and bottom-left.
                for (int i = 1; i < patternsPerSide - 1; i++)
                {
                    MyPointF fromTop = align[0][0] + luDown * symbol.patternCoords[i];
                    MyPointF fromBottom = align[N - 1][0] - ldDown * (symbol.SideModuleCount - 1 - symbol.patternCoords[i]);
                    float bottomFactor = (float)symbol.patternCoords[i] / (float)(symbol.SideModuleCount - 1);
                    float topFactor = 1F - bottomFactor;
                    bottomFactor *= bottomFactor;
                    topFactor *= topFactor;
                    MyPointF mean = (fromTop * topFactor + fromBottom * bottomFactor) / (topFactor + bottomFactor);
                    align[1 + i][0] = mean;
                    moduleW[1 + i][0] = (moduleW[0][0] * bottomFactor + moduleW[N - 1][0] * topFactor) / (bottomFactor + topFactor);
                    moduleH[1 + i][0] = (moduleW[0][0] * bottomFactor + moduleW[N - 1][0] * topFactor) / (bottomFactor + topFactor);
                }

                //four: find alignment patterns. Main loop to scan each region. If all 4 corners are already 
                // computed, only sampling is done. If the bottom-right corner is empty, the alignment pattern will 
                // be found. 
                for (int y = 1; y < N; y++)
                {
                    for (int x = 1; x < N; x++)
                    {
                        if (align[y][x].IsEmpty)
                        {
                            MyPointF p = MyPoint.Empty;
                            if (x > 1 && y > 1)
                            {
                                MyVectorF up = align[y - 1][x] - align[y - 2][x];
                                MyVectorF left = align[y][x - 1] - align[y][x - 2];
                                if (up.Length > 15f && left.Length > 15f) //otherwise use default calculation
                                    if (x < N - 1 && y < N - 1) //mid align patterns
                                    {
                                        Regression v = new Regression(align[y-2][x]);
                                        v.AddPointL(align[y - 2][x]);
                                        v.AddPointL(align[y - 1][x]);
                                        Regression h = new Regression(align[y][x-2]);
                                        h.AddPointL(align[y][x - 2]);
                                        h.AddPointL(align[y][x - 1]);
                                        RegressionLine vl = v.LineL;
                                        RegressionLine hl = h.LineL;
                                        p = vl.Intersection(hl);
                                    }
                                    else if (x < N - 1) //bottom  (y==N-1)
                                    {
                                        MyVectorF lu = align[y - 1][x - 1] - align[y - 2][x - 1];
                                        MyVectorF ld = align[y][x - 1] - align[y - 1][x - 1];
                                        MyVectorF ru = align[y - 1][x] - align[y - 2][x];
                                        float factor = ld.Length / lu.Length;
                                        p = align[y - 1][x] + ru * factor;
                                    }
                                    else //left
                                    {
                                        MyVectorF ul = align[y - 1][x - 1] - align[y - 1][x - 2];
                                        MyVectorF ur = align[y - 1][x] - align[y - 1][x - 1];
                                        MyVectorF dl = align[y][x - 1] - align[y][x - 2];
                                        float factor = ur.Length / ul.Length;
                                        p = align[y][x - 1] + dl * factor;
                                    }
                            }
                            if (p.IsEmpty)
                            {
                                //first aproximation assuming no deformation
                                p = align[y][x - 1] + (align[y - 1][x] - align[y - 1][x - 1]);
                            }
                            align[y][x] = p;
                            moduleW[y][x] = (moduleW[y][x - 1] + moduleW[y - 1][x]) / 2f; //by now just the mean of previous ones
                            moduleH[y][x] = (moduleH[y][x - 1] + moduleH[y - 1][x]) / 2f;


                            //Only in a non skewed barcodes, alignment pattern falls in p. 
                            //This loop tries to find the alignment patterns near to p using 
                            //neibourhood array (the displacement from p)
                            if (x != N - 1 && y != N - 1)
                            {
                                MyPointF p0 = p;
                                float width = location.NominalWidth * 3F;
                                MyPoint[] neibourhood = new MyPoint[] { new MyPoint(0, 0), new MyPoint(1, 0), new MyPoint(-1, 0), new MyPoint(0, 1), new MyPoint(0, -1),
                                                new MyPoint(2, 0), new MyPoint(-2, 0), new MyPoint(0, 2), new MyPoint(0, -2)};
                                foreach (MyPoint n in neibourhood)
                                {
                                    float w, h;
                                    if (FindAlignmentPattern(location, p0, n, width, out p, out w, out h))
                                    {
                                        //correct interpoled borders, in case
                                        if (y == 1) align[0][x] += (p - p0);
                                        if (x == 1) align[y][0] += (p - p0);
                                        align[y][x] = p;
                                        //moduleW[y][x] = w;
                                        //moduleH[y][x] = h;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                location.B=align[N-1][N-1];
            }
            return symbol;
        }


        bool scanSimple(QRSymbol symbol, QRLocation location)
        {
            int patternsPerSide = symbol.patternCoords.Length;
           
            //If only one sampling region (no alignment patterns)
            if (patternsPerSide == 0)
            {
                NotUniformGrid grid = new NotUniformGrid(symbol.SideModuleCount, symbol.SideModuleCount, 7, location.LU, location.LD, location.RU);
                grid.ExtractPoints(scan, symbol.MaskedBitarray);
            }
            else
            {
                int N = patternsPerSide + 2;
                //scan barcode
                int prevRow = 0;
                for (int y = 1; y < N; y++)
                {
                    int row = y <= patternsPerSide ? symbol.patternCoords[y - 1] : symbol.SideModuleCount;
                    int prevCol = 0;
                    for (int x = 1; x < N; x++)
                    {
                        int col = x <= patternsPerSide ? symbol.patternCoords[x - 1] : symbol.SideModuleCount;
                        //Once the alignment pattern is found, sample the image using these 4 corners
                        MyPointF[][] align = symbol.align;
                        Grid grid = new Grid(col - prevCol, row - prevRow, align[y - 1][x - 1], align[y][x - 1], align[y - 1][x], align[y][x], true);
                        grid.ExtractPoints(scan, symbol.MaskedBitarray, new MyPoint(0, 0),
                           new MyPoint(prevCol, prevRow), col - prevCol, row - prevRow);
#if DEBUG_IMAGE
                        //scan.Save(@"outSamples.png");
#endif
                        prevCol = col;
                    }
                    prevRow = row;
                }
            }
#if DEBUG_IMAGE
            scan.Save(@"outSamples.png");           
#endif
            return true;
        }

        bool scanAdaptive(QRSymbol symbol, QRLocation location)
        {
            int patternsPerSide = symbol.patternCoords.Length;
#if DEBUG_IMAGE
            scan.Reset();
#endif

            //If only one sampling region (no alignment patterns)
            if (patternsPerSide != 0)
            {
                for (int i = 0; i < symbol.MaskedBitarray.Length; i++)
                    for (int j = 0; j < symbol.MaskedBitarray[i].Length; j++)
                        symbol.MaskedBitarray[i][j] = false;

                int N = patternsPerSide + 2;
                //scan barcode
                int prevRow = 0;
                for (int y = 1; y < N; y++)
                {
                    int row = y <= patternsPerSide ? symbol.patternCoords[y - 1] : symbol.SideModuleCount;
                    int prevCol = 0;
                    for (int x = 1; x < N; x++)
                    {
                        int col = x <= patternsPerSide ? symbol.patternCoords[x - 1] : symbol.SideModuleCount;
                        //Once the alignment pattern is found, sample the image using these 4 corners
                        //Grid grid = new Grid(col - prevCol, row - prevRow, align[y-1][x-1], align[y][x-1], align[y-1][x], align[y][x], true);
                        //grid.ExtractPoints(scan, symbol.MaskedBitarray, new MyPoint(0, 0),
                        //   new MyPoint(prevCol, prevRow), col - prevCol, row - prevRow);
                        MyPointF[][] align = symbol.align;
                        float[][] moduleW = symbol.moduleW;
                        float[][] moduleH = symbol.moduleH;
                        AdaptiveGrid grid = new AdaptiveGrid(col - prevCol, row - prevRow,
                            align[y - 1][x - 1], align[y][x - 1], align[y - 1][x], align[y][x],
                            moduleW[y - 1][x - 1], moduleW[y][x - 1], moduleW[y - 1][x], moduleW[y][x],
                            moduleH[y - 1][x - 1], moduleH[y][x - 1], moduleH[y - 1][x], moduleH[y][x]);
                        grid.ExtractPoints(scan, symbol.MaskedBitarray, new MyPoint(prevCol, prevRow));
#if DEBUG_IMAGE
                        //scan.Save(@"outSamples.png");
#endif
                        prevCol = col;
                    }
                    prevRow = row;
                }                          
#if DEBUG_IMAGE
            scan.Save(@"outSamples.png");
#endif
                return true;
            }
            return false;
        }





        //Starting from the estimated center of the alignment pattern (p0+offset), finds the next transition 
        //from white to black at left, right, up and down. Usually, the center of the alignment pattern 
        //produces an error in one direction, vertical or horizontal. Calculate h and v distances of the 
        //found limits and use the bigger one to recalculate the other 2.
        bool FindAlignmentPattern(QRLocation location, MyPointF p0, MyPoint offset, float width, out MyPointF corrected, out float W, out float H)
        {
            //adjunt alignment pattern center
            MyPointF p = p0 + location.LeftNormal * (location.NominalWidth * (0.5F + (float)offset.X/2f)) +
                location.DownNormal * (location.NominalHeight * (0.5F + (float)offset.Y/2f));
            MyPoint ip=new MyPoint((int)Math.Truncate(p.X),(int)Math.Truncate(p.Y));
            int halfModuleLength = (int)Math.Round(width / 3F /2F)-1;
            if (halfModuleLength < 0) halfModuleLength = 0;

            MyPointF up = scan.NextBlack(ip, -location.DownNormal, halfModuleLength);
            MyPointF down = scan.NextBlack(ip, location.DownNormal, halfModuleLength);
            MyPointF left = scan.NextBlack(ip, -location.LeftNormal, halfModuleLength);
            MyPointF right = scan.NextBlack(ip, location.LeftNormal, halfModuleLength);
            float h = (left - right).Length;
            float v = (up - down).Length;

            if (!Calc.Around(h / v, 1.0F, 0.3F) || !Calc.Around(h / width, 1.0F, 0.2F))
                if (h > v)
                {
                    p = left * (5F / 6F) + right * (1F / 6F);
                    ip = new MyPoint((int)Math.Truncate(p.X), (int)Math.Truncate(p.Y));
                    up = scan.NextBlack(ip, -location.DownNormal, halfModuleLength);
                    down = scan.NextBlack(ip, location.DownNormal, halfModuleLength);
                    v = (up - down).Length;
                }
                else
                {
                    p = up * (5F / 6F) + down * (1F / 6F);
                    ip = new MyPoint((int)Math.Truncate(p.X), (int)Math.Truncate(p.Y));
                    left = scan.NextBlack(ip, -location.LeftNormal, halfModuleLength);
                    right = scan.NextBlack(ip, location.LeftNormal, halfModuleLength);
                    h = (left - right).Length;
                }

            W = H = 0f;
            
            //if h and v are regular enough, we consider them as a good alignment pattern and claculate 
            // the top-left coordinates of the center of the alignment pattern.
            if (Calc.Around(h / v, 1.0F, 0.4F) && Calc.Around(h / width, 1.0F, 0.3F))
            {
                MyVectorF vdD = (down - up);
                p = new MyPointF(0.5F + ip.X, 0.5F + ip.Y);
                MyVectorF vdR = (right - left);
                float toUp = (p - up).Length;
                float toDown = (p - down).Length;
                float factor = toUp / (toUp + toDown);
                MyPointF LU = left - vdD * factor;               
                corrected = LU + vdD / 3F + vdR / 3F;

                W = h / 3f;
                H = v / 3f;
                return true;
            }
            corrected = MyPointF.Empty;
            return false;
        }

        BarCodeRegion ReadSymbol(QRSymbol symbol, BarCodeRegion result)
        {
            if (symbol != null)
            {
                float confidence = 1F;
                BitArray stream = ReadSymbol(symbol, out confidence);
                if (stream != null)
                {
                    ABarCodeData[] data = DecodeData(symbol, stream);
                    if (Encoding != DefaultEncoding)
                        foreach (ABarCodeData d in data)
                            if (d is Base256BarCodeData) 
                                (d as Base256BarCodeData).encoding = Encoding;

                    if (data != null) 
                    {
                        result.Data = data;
                        result.Confidence = confidence;
                        return result;
                    }
                }
            }
            return null;
        }


        //method form the old code to create the byte array from array bit, and correct them.
        BitArray ReadSymbol(QRSymbol symbol, out float confidence)
        {
            confidence = 1F;
            if (!symbol.EnrichFormatInformation()) return null;
            symbol.ReadCodewords();
            int[][] blockInfo = symbol.GenerateECBlockInfo();
            int blockCount = blockInfo.Length;
            int[][] blocks = new int[blockCount][];
            for (int i = 0; i < blockCount; i++)
            {
                blocks[i] = new int[blockInfo[i][0] + blockInfo[i][1]];
            }

            // rearrange the data codewords to their original order
            int[] indices = new int[blockCount];
            int blockIndex = 0;
            int failsNumber = 0;
            for (int i = 0; i < symbol.CodewordCount && failsNumber<blockCount; )
            {
                // if we did not reach the data block boundary, or we are already arranging the error codewords, increase the block index
                if (indices[blockIndex] < blockInfo[blockIndex][0]  || i >= symbol.DataCodewordCount)
                {
                    if (indices[blockIndex] < blocks[blockIndex].Length ) 
                        blocks[blockIndex][indices[blockIndex]] = symbol.RawCodewords[i];
                    ++indices[blockIndex];
                    ++i;
                }
                else failsNumber++;

                blockIndex = (blockIndex + 1) % blockCount;
            }

            if (failsNumber >=blockCount) throw new Exception("ERROR: wrong QR parameters table");


            // attempt to correct errors
            float meanConfidence = 0F, conf;
            for (int i = 0; i < blockCount; ++i)
            {
                ReedSolomon rs = new ReedSolomon(blocks[i], blockInfo[i][1], 8, 285, 0);
                rs.Correct(out conf);

                if (!rs.CorrectionSucceeded)
                    return null;

                //RS rs2 = new RS(new GF(2, 8, 285), blocks[i], blockInfo[i][1],true);
                //rs2.correct();

                meanConfidence += conf;

                blocks[i] = rs.CorrectedData;
            }
            confidence = meanConfidence / (float)blockCount;

            BitArray dataStream = new BitArray(symbol.DataCodewordCount * 8);
            for (int di = 0, bi = 0; bi < blockCount; ++bi)
            {
                for (int i = 0; i < blockInfo[bi][0]; ++i)
                {
                    for (int b = 7; b >= 0; --b)
                    {
                        dataStream[di++] = (blocks[bi][i] & (1 << b)) != 0;
                    }
                }
            }

            if (symbol.Version == (int)MicroQRVersion.M1 || symbol.Version == (int)MicroQRVersion.M3)
            {
                dataStream.Length -= 4;
            }
            return dataStream;
        }

        ABarCodeData[] DecodeData(QRSymbol symbol, BitArray dataStream)
        {
            ABarCodeData[] ddsData = null;
            ArrayList data = new ArrayList();
            int index = 0;

            if (symbol.Version == (int)MicroQRVersion.M1)
            {
                ddsData = new ABarCodeData[] { SymbolDecoder.DecodeNumeric((int)MicroQRVersion.M1, dataStream, ref index) };
                return null;
            }

            int indicatorLength;
            Hashtable decoderMap;
            switch (symbol.Version)
            {
                case (int)MicroQRVersion.M2:
                    indicatorLength = 1;
                    decoderMap = SymbolDecoder.QRDecoderMapM2;
                    break;
                case (int)MicroQRVersion.M3:
                    indicatorLength = 2;
                    decoderMap = SymbolDecoder.QRDecoderMapM3;
                    break;
                case (int)MicroQRVersion.M4:
                    indicatorLength = 3;
                    decoderMap = SymbolDecoder.QRDecoderMapM4;
                    break;
                default:
                    indicatorLength = 4;
                    decoderMap = SymbolDecoder.QRDecoderMap;
                    break;
            }

            while (index < dataStream.Length && !QRUtils.IsStreamEnd(dataStream, index, symbol.Version))
            {
                int mode = QRUtils.BitSliceValue(dataStream, ref index, indicatorLength);
                if (decoderMap.ContainsKey(mode))
                {
                    Decoder decoder = (Decoder)decoderMap[mode];
                    ABarCodeData newData = decoder(symbol.Version, dataStream, ref index);
                    if (newData != null)
                    {
                        data.Add(newData);
                    }
                }
                else if (mode == 0) break; //terminator
            }

            ddsData = new ABarCodeData[data.Count];
            data.CopyTo(ddsData);
            return ddsData;
        }
    }
#endif

}
