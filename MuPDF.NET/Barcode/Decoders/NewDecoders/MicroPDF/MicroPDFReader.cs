using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using BarcodeReader.Core.Common;
using BarcodeReader.Core.PDF417;

namespace BarcodeReader.Core.MicroPDF
{
    //Class to detect microPDF barcodes. Uses the edge slicer to find all edge candidates and, one
    //by one, tries to find the barcode starting at the edge.
#if CORE_DEV
    public
#else
    internal
#endif
    class MicroPDFReader : SymbologyReader2D
    {
        //Foreach found edge, look for the first LPattern. If it's found far away, it's rejected.
        protected float maxDistanceFromEdgeToLPattern= 10f;

        //If the first LPattern has a matching error too hight, it's rejected
        protected float maxLPatternError = 0.75f;
        protected float maxRPatternError = 0.6f; //a little bit more relaxed

        //When check for start quiet zone, check 4 * modules with white pixels
        protected float startPatternQuietZone = 10f;

        //Max width difference between the left and right pattern
        protected float maxWidthDifferenceBetweenLRPatterns = 0.1f;

        //Max barcode module length.
        protected float maxBarcodeLength = 110f;

        //Reject start/stop patterns that have not the same length. Set to 0 for strict same length, or bigger
        //to process skewed barcodes.
        protected float maxRatioStartStopLength = 0.1F;

        LinkedList<BarCodeRegion> candidates;
        PatternFinderNoise LRowFinder;
        ImageScaner scan;

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.MicroPDF;
		}

        protected override FoundBarcode[] DecodeBarcode()
        {
            EdgeSlicer slicer = new EdgeSlicer(20, 500, false);
            LinkedList<Common.Edge> edges=slicer.GetEdges(BWImage);
            candidates=new LinkedList<BarCodeRegion>();
            scan = new ImageScaner(BWImage);
            LRowFinder = new PatternFinderNoise(BWImage, LRpatterns, true, 2);

            foreach (Edge e in edges) ProcessEdge(e);

            ArrayList results = new ArrayList();
            foreach (BarCodeRegion b in candidates)
            {
                FoundBarcode foundBarcode = new FoundBarcode();
                foundBarcode.BarcodeFormat = SymbologyType.MicroPDF;
                foundBarcode.Polygon = new SKPointI[5] { b.A, b.B, b.D, b.C, b.A };
                foundBarcode.Color = SKColors.Blue;
                //byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
				//GraphicsPath path = new GraphicsPath(foundBarcode.Polygon, pointTypes);
                //foundBarcode.Rect = Rectangle.Round(path.GetBounds());
                foundBarcode.Rect = Utils.DrawPath(foundBarcode.Polygon);
                String data = "";
                if (b.Data != null) foreach (ABarCodeData d in b.Data) data += d.ToString();
                foundBarcode.Value = data;
                foundBarcode.Confidence = b.Confidence;
                results.Add(foundBarcode);
            }
            return (FoundBarcode[])results.ToArray(typeof(FoundBarcode));
        }

        int[] nModules = new int[] { 37, 54, 81, 98}; //number of modules for 1, 2, 3 or 4 cols microPDF

        class Candidate { public MyPoint p; public int nCols; public Candidate(MyPoint p, int nCols) { this.p = p; this.nCols = nCols; } }

        //For each found edge, traces a line crossing the center of the edge. Then looks for the
        //left row pattern (LRowFinder object). If the pattern is no far away from the edge then 
        //checks for the quiet zone at the left of the pattern. If quiet zone exists, then looks for
        //the right row pattern. Usually, we get a lot of false right patterns that fall inside the
        //barcode. All them are stored in the candidates container (var h) and then processed, from the
        //larger to the shorter. 
        bool ProcessEdge(Edge e)
        {
            //Calculate main directions
            MyVectorF vdY = new MyPoint((int)e.In.X, (int)e.In.Y) - new MyPoint((int)e.End.X, (int)e.End.Y);
            vdY = vdY.Normalized;
            MyVectorF vdX = new MyVectorF(-vdY.Y, vdY.X);

            MyPoint mid = new MyPoint((int)(e.Center.X + (vdX * 5).X), (int)(e.Center.Y + (vdX * 5).Y));
            foreach (BarCodeRegion c in candidates)
                if (c.In(mid)) return false;

            MyPoint center = new MyPoint((int)e.Center.X, (int)e.Center.Y);
            Bresenham brR = new Bresenham(center, vdX);
            MyPoint start=brR.Current;
            LRowFinder.NewSearch(brR, false, -1);
            int lRow,rRow;
            while ((lRow=LRowFinder.NextPattern())!=-1) 
            {
                float l = (start - LRowFinder.First).Length;
                if (l > maxDistanceFromEdgeToLPattern) return false; //if finder is 10 pixels far, reject                
                if (LRowFinder.Error > maxLPatternError) continue;

                //estimate moduleLength
                float LFinderLength=(LRowFinder.Last - LRowFinder.First).Length;
                float moduleLength = LFinderLength / 10f;

                //check 10 modules length quiet zone
                Bresenham br = new Bresenham(center, -vdX);
                while (scan.In(br.Current) && scan.isBlack(br.Current)) br.Next();
                bool quietZone = true;
                while (quietZone && scan.In(br.Current) && (br.Current - center).Length < moduleLength * startPatternQuietZone)
                    if (scan.isBlack(br.Current)) quietZone = false;
                    else br.Next();

                if (quietZone)
                {
                    //find Right row pattern candidates
                    MyPoint prev = MyPoint.Empty;
                    SortedList<float, LinkedList<Candidate>> h = new SortedList<float, LinkedList<Candidate>>();                    
                    while ((rRow = LRowFinder.NextPattern()) != -1)
                    {
                        if (LRowFinder.Last == prev) continue; 
                        //check if left finder has +- the same length than the left one
                        float RFinderLength = (LRowFinder.Last - LRowFinder.First).Length;
                        if (!Calc.Around(LFinderLength / RFinderLength, 1.0f, maxWidthDifferenceBetweenLRPatterns)) continue;

                        prev = LRowFinder.Last;
                        l = (start - LRowFinder.Last).Length;
                        if (l > moduleLength * maxBarcodeLength) break; //98+10%
                        if (LRowFinder.Error > maxRPatternError) continue;
                        float m = l / moduleLength;
                        int nCols = -1;
                        float minL = float.MaxValue;
                        for (int n = 0; n < nModules.Length; n++)
                        {
                            float d = (float)Math.Abs(m - nModules[n]);
                            if (d < minL) { nCols = n; minL = d; }
                        }
                        if (nCols != -1 && Calc.Around((float)nModules[nCols], m, (float)nModules[nCols] * 0.07f))
                        { //7% 
                            float f = minL * 4 + nCols;
                            if (!h.ContainsKey(f)) h[f] = new LinkedList<Candidate>();
                            h[f].AddLast(new Candidate(LRowFinder.Last, nCols));
                        }
                    }

                    foreach (float d in h.Keys) foreach(Candidate c in h[d])
                    {
                        //Candidate c=(Candidate)h[d];

                        //Track edge
                        EdgeTrack et = new EdgeTrack(scan);
                        Bresenham brEnd = new Bresenham(c.p, vdX);
                        while (scan.In(brEnd.Current) && scan.isBlack(brEnd.Current)) brEnd.Next();
                        et.Track(brEnd.Current, new MyVector(-1, 0), moduleLength, false);

                        //find the top and bottom points of the edge and checks if the length is
                        //similar to the left side. If left and right edges have no the same length, 
                        //uses the left length to do a last try to read the barcode.
                        float lIn = e.Length;
                        float lEnd = (et.Up() - et.Down()).Length;
                        if (Calc.Around(lIn, lEnd, lIn * maxRatioStartStopLength))
                            if (ScanBarcode(new MyPointF(e.In.X, e.In.Y), new MyPointF(e.End.X, e.End.Y), et.Up(), et.Down(), c.nCols)) return true;
                        MyPointF tempInF = new MyPointF(e.In.X + (vdX * l).X, e.In.Y + (vdX * l).Y);
                        MyPointF tempEndF = new MyPointF(e.In.X + (vdX * l).X, e.In.Y + (vdX * l).Y);
                        if (ScanBarcode(new MyPointF(e.In.X, e.In.Y), new MyPointF(e.End.X, e.End.Y), tempInF, tempEndF, c.nCols)) return true;
                    }

                }
                return false;
            }
            return false;
        }

        //Scans all rows of the barcode doing oversampling (each row is samples many times). The most 
        //common codewords are used as final data. The first time, only left row patterns are decoded, 
        //just to find the size of the microPDF barcode. If a valid size is found, then all 
        //codewords are sampled to read all data.
        bool ScanBarcode(MyPointF lu, MyPointF ld, MyPointF ru, MyPointF rd, int nCols)
        {
            int[][] sizes = sizesAndLCRIndexs[nCols];
            int numModules = nModules[nCols];
            float moduleLength = (lu - ru).Length / (float)numModules;
            moduleLength *= (float)Math.Cos((ru - lu).Normalized.Angle);
            RowSamples rows = new RowSamples();
            BarSymbolReader sr = new BarSymbolReader(scan, NBars[nCols], NModules[nCols], tableIndexs[nCols], true, true, moduleLength, tables, true, new PDF417BestMatch());
            for (float d = 0.01f; d < 1f; d += 0.01f) //samples 100 rows
            {
                MyPointF l = lu * (1f - d) + ld * d;
                MyPointF r = ru * (1f - d) + rd * d;
                float error, maxError, confidence;
                int[] s=sr.Read(l, r, 1, out error, out maxError, out confidence);
                if (s.Length>0 && s[0]!=-1) rows.AddRow(s[0], s);
            }

            bool scanned = false;
            int[] rowIndexs = rows.GetRowIndexs();
            Array.Sort(rowIndexs);
            int i = 0;
            while (i<rowIndexs.Length) 
            {                
                int firstRow=rowIndexs[i];
                int lastRow = firstRow;
                int ii = i + 1;
                while (ii < rowIndexs.Length)
                    if (rowIndexs[ii] == lastRow + 1) { lastRow++; ii++; }
                    else if (rowIndexs[ii] == lastRow + 2) { lastRow += 2; ii++; }
                    else break;
                int nRows = lastRow - firstRow +1;
                int size = -1;
                for (int j = 0; j < sizes.Length && size==-1; j++) if (sizes[j][0] == nRows && (sizes[j][3]-1)==firstRow) size = j;
                if (size != -1)
                {
                    if (!scanned) //scan all data when a valid size is found.
                    {
                        rows.Clear();
                        for (float d = 0.01f; d < 1f; d += 0.01f)
                        {
                            MyPointF l = lu * (1f - d) + ld * d;
                            MyPointF r = ru * (1f - d) + rd * d;
                            float error,maxError,dummyConfidence;
                            int[] s = sr.Read(l, r, out error, out maxError, out dummyConfidence);
                            rows.AddRow(s[0], s);
                        }
                        scanned = true;
                    }

                    //place codewords in a vector, in the right place.
                    int n=0;
                    int[] codewords = new int[(nCols + 1) * nRows];
                    for (int j = firstRow; j <= lastRow; j++)
                    {
                        int[] row = rows.GetBestRow(j);
                        switch (nCols)
                        {
                            case 0: add(codewords, row, 1, ref n); break;
                            case 1: add(codewords, row, 1, ref n); add(codewords, row, 2, ref n); break;
                            case 2: add(codewords, row, 1, ref n); add(codewords, row, 3, ref n); add(codewords, row, 4, ref n); break;
                            case 3: add(codewords, row, 1, ref n); add(codewords, row, 2, ref n); add(codewords, row, 4, ref n); add(codewords, row, 5, ref n); break;
                        }
                    }

                    //Prepare RS correction
                    float confidence=0f;
                    int nEC = sizes[size][1]; //number of error correction codewords for this size
                    PDF417.ReedSolomon rs = new PDF417.ReedSolomon();
                    LinkedList<int> blanks = new LinkedList<int>();
                    for (int l = 0; l < codewords.Length; l++) 
                        if (codewords[l] == -1) { codewords[l] = 0; blanks.AddLast(l); }
                    int[] aBlanks = new int[blanks.Count];
                    blanks.CopyTo(aBlanks, 0);
                    if (aBlanks.Length <= 2 * nEC && aBlanks.Length <= 2 * (codewords.Length - aBlanks.Length) && 
                        rs.Correct(codewords, aBlanks, nEC, out confidence))
                    {

                        // if confidence is zero then stop
                        if (confidence < float.Epsilon)
                            return false;

                        BarCodeRegion r = new BarCodeRegion(lu, ru, ld, rd);
                        int[] data = new int[codewords.Length - nEC];
                        Array.Copy(rs.correcteddata, data, data.Length);
                        r.Data = PDF417Decoder.Decode(data, Encoding, 900);
                        r.Confidence = confidence;
                        candidates.AddLast(r);
                        return true;
                    }
                }
                i++;
            }
            return false;
        }

        //Adds a codeword in its position if it is a valid codeword. 
        void add(int[] codewords, int[] row, int i, ref int n)
        {
            codewords[n++] = (row!=null && row.Length>i?row[i]:-1);
        }

        //Class used in the BarSymbolReader sr object to find the closest codeword of a pattern.
        class PDF417BestMatch : IBestMatch
        {
            ModuleDecoder moduleDecoder;

            public PDF417BestMatch()
            {
                moduleDecoder = new ModuleDecoder("symbolmap.txt", 17); //PDF417 symbols are 17 modules length
            }

            public int bestMatch(float[] e, LinkedList<int> symbols)
            {
                if (e == null) return -1;
                int cluster = (symbols.First.Value % 3) * 3;
                int codeword=moduleDecoder.GetCodeword(cluster, e);
                return codeword;
            }
        }


        //sizes for cols=1, 2, 3 or 4  --> (nrows, num EC words, num Data words, firstL, [firstC], firstR)
        static readonly int[][][] sizesAndLCRIndexs = new int[][][]{
            new int[][]{new int[]{11,7,4,1,9},new int[]{14,7,7,8,8},new int[]{17,7,10,36,36},new int[]{20,8,12,19,19},new int[]{24,8,16,9,17},new int[]{28,8,20,25,33}},
            new int[][]{new int[]{8,8,8,1,1},new int[]{11,9,13,1,9},new int[]{14,9,19,8,8},new int[]{17,10,24,36,36},new int[]{20,11,29,19,19},new int[]{23,13,33,9,17},new int[]{26,15,37,27,35}},
            new int[][]{new int[]{6,12,6,1,1,1}, new int[]{8,14,10,7,7,7},new int[]{10,16,14,15,15,15},new int[]{12,18,18,25,25,25},new int[]{15,21,24,37,37,37},new int[]{20,26,34,1,17,33},new int[]{26,32,46,1,9,17},new int[]{32,38,58,21,29,37},new int[]{38,44,70,15,31,47}, new int[]{44,50,82,1,25,49}},
            new int[][]{new int[]{4,8,8,47,19,43},new int[]{6,12,12,1,1,1},new int[]{8,14,18,7,7,7},new int[]{10,16,24,15,15,15},new int[]{12,18,30,25,25,25},new int[]{15,21,39,37,37,37},new int[]{20,26,54,1,17,33},new int[]{26,32,72,1,9,17},new int[]{32,38,72,21,29,37},new int[]{38,44,106,15,31,47},new int[]{44,50,126,1,25,49}}
        };

        //Left and Right row patterns
        static readonly int[][] LRpatterns = new int[][] { 
            new int[] { 2,2,1,3,1,1}, new int[] {3,1,1,3,1,1 }, new int[] { 3,1,2,2,1,1}, new int[] { 2,2,2,2,1,1}, new int[] { 2,1,3,2,1,1}, 
            new int[]{2,1,4,1,1,1},new int[]{2,2,3,1,1,1},new int[]{3,1,3,1,1,1},new int[]{3,2,2,1,1,1},new int[]{4,1,2,1,1,1},
            new int[] {4,2,1,1,1,1 }, new int[] {3,3,1,1,1,1 }, new int[] {2,4,1,1,1,1 }, new int[] { 2,3,2,1,1,1}, new int[] {2,3,1,2,1,1 }, 
            new int[]{3,2,1,2,1,1},new int[]{4,1,1,2,1,1},new int[]{4,1,1,1,2,1},new int[]{4,1,1,1,1,2},new int[]{3,2,1,1,1,2},
            new int[] { 3,1,2,1,1,2}, new int[] {3,1,1,2,1,2 }, new int[] {3,1,1,2,2,1 }, new int[] {3,1,1,1,3,1 }, new int[] {3,1,1,1,2,2 }, 
            new int[]{3,1,1,1,1,3},new int[]{2,2,1,1,1,3},new int[]{2,2,1,1,2,2},new int[]{2,2,1,1,3,1},new int[]{2,2,1,2,2,1},
     /*31*/ new int[] {2,2,2,1,2,1 }, new int[] { 3,1,2,1,2,1}, new int[] { 3,2,1,1,2,1}, new int[] { 2,3,1,1,2,1}, new int[] {2,3,1,1,1,2 }, 
     /*36*/ new int[]{2,2,2,1,1,2},new int[]{2,1,3,1,1,2},new int[]{2,1,2,2,1,2},new int[]{2,1,2,2,2,1},new int[]{2,1,2,1,3,1},
     /*41*/ new int[] {2,1,2,1,2,2 }, new int[] { 2,1,2,1,1,3}, new int[] { 2,1,1,2,1,3}, new int[] {2,1,1,1,2,3 }, new int[] {2,1,1,1,3,2 },
     /*46*/ new int[]{2,1,1,1,4,1},new int[]{2,1,1,2,3,1},new int[]{2,1,1,2,2,2},new int[]{2,1,1,3,1,2},new int[]{2,1,1,3,2,1},
     /*51*/ new int[] {2,1,1,4,1,1 }, new int[] { 2,1,2,3,1,1}
        };
        //Center row patterns
        static readonly int[][] Cpatterns = new int[][] { 
     /*1*/  new int[] {1,1,2,2,3,1 }, new int[] { 1,2,1,2,3,1}, new int[] { 1,2,2,1,3,1}, new int[] {1,3,1,1,3,1 }, new int[] {1,3,1,2,2,1 }, 
     /*6*/  new int[]{1,3,2,1,2,1},new int[]{1,4,1,1,2,1},new int[]{1,4,1,2,1,1},new int[]{1,4,2,1,1,1},new int[]{1,3,3,1,1,1},
     /*11*/ new int[] {1,3,2,2,1,1 }, new int[] { 1,3,1,3,1,1}, new int[] {1,2,2,3,1,1 }, new int[] {1,2,3,2,1,1 }, new int[] {1,2,3,1,1,1 }, 
     /*16*/ new int[]{1,1,5,1,1,1},new int[]{1,1,4,2,1,1},new int[]{1,1,4,1,2,1},new int[]{1,2,3,1,2,1},new int[]{1,2,3,1,1,2},
     /*21*/ new int[] {1,2,2,2,1,2 }, new int[] {1,2,2,2,2,1 }, new int[] {1,2,1,3,2,1 }, new int[] {1,2,1,4,1,1 }, new int[] {1,1,2,4,1,1 }, 
     /*26*/ new int[]{1,1,3,3,1,1},new int[]{1,1,3,2,1,2},new int[]{1,1,3,2,1,2},new int[]{1,1,3,1,2,2},new int[]{1,2,2,1,2,2},
     /*31*/ new int[] { 1,3,1,1,2,2}, new int[] { 1,3,1,1,1,3}, new int[] {1,2,2,1,1,3 }, new int[] { 1,1,3,1,1,3}, new int[] {1,1,2,2,1,3 },
     /*36*/ new int[]{1,1,2,2,2,2},new int[]{1,1,2,3,1,2},new int[]{1,1,2,3,2,1},new int[]{1,1,1,4,2,1},new int[]{1,1,1,3,3,1},
     /*41*/ new int[] { 1,1,1,3,2,2}, new int[] {1,1,1,2,3,2 }, new int[] {1,1,1,2,2,3 }, new int[] { 1,1,1,1,3,3}, new int[] {1,1,1,1,2,4 }, 
     /*46*/ new int[]{1,1,1,2,1,4},new int[]{1,1,2,1,1,4},new int[]{1,2,1,1,1,4},new int[]{1,2,1,1,2,3},new int[]{1,2,1,1,3,2},
     /*51*/ new int[] {1,1,2,1,3,2 }, new int[] {1,1,2,1,4,1 }
        };

        static readonly int[][] NBars = new int[][] { new int[] { 6, 8, 6 }, new int[] { 6, 8, 8, 6 }, 
            new int[] { 6, 8, 6, 8, 8, 6 }, new int[] { 6, 8, 8,6,8,8,6 }};
        static readonly int[][] NModules = new int[][] { new int[] { 10, 17, 10 }, new int[] { 10, 17, 17, 10 }, 
            new int[] { 10, 17, 10, 17, 17, 10 }, new int[] { 10, 17, 17, 10, 17, 17, 10 }};
        static readonly int[][] tableIndexs = new int[][] { new int[] { 0, -1, 0 }, new int[] {0, -1, -1, 0 }, 
            new int[] { 0, -1, 1, -1, -1, 0}, new int[] {0, -1, -1, 1, -1, -1, 0 } };
        static readonly int[][][] tables = new int[][][] { LRpatterns, Cpatterns };
    }

}
