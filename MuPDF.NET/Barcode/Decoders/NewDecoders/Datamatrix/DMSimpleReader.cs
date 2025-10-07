using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Datamatrix
{
	internal partial class DMSimpleReader 
    {
        //Maximum number of edges to process. The solution is "always" found in the largests edges. Thus, processing only the largests
        //ones saves time. 
        // USED in the Inherited Classes only!
        internal int decodingMaxRetries;

        // if we should check barcode inside using diagonal beaming line 
        // and see if there is some dots inside so this could be a barcode
        // or not. ATTENTION - it works for simple barcodes only! But NOT working for damaged ones
        internal bool UseQuickCheckForBacodeInside = false;

        // max hole size between _ and | lines in relative to the largest side (15% = 0.15d etc)
        internal double MaxAllowedHoleBetweenLinesInLPattern;
        //Max dist between corners to find the center of L pattern
        internal double MAX_DIST_TO_CONSIDER_LPATTERN = 8d;

        internal int MaxNumberOfBarcodes = -1;
 
        //Scan image rows main step
        protected int scanRowStep = 1;

        //Min Y/X of L pattern (edges of DM barcode)
        protected float dmMinAspectRatio = 0.6f;

        //Max Y/X of L pattern (edges of DM barcode)
        protected float dmMaxAspectRatio = 6f;

        //Max difference angle of the L pattern (from perpendicular)
        protected float dmMaxAngleDifference = 0.15f;

        //Min edge length
        protected int MIN_SIZE = 20;

        //Max dispersion of ends of L pattern (in units)
        public int LPatternDispersion { get; set; } = 0;

        //Dispersion step (in fractions of edge length)
        public float LPatternDispersionStep { get; set; } = 1 / 40f;

        protected int width, height;
        protected int minAllowedBarcodeSideSize, maxAllowedBarcodeSideSize;
        
        protected BlackAndWhiteImage bwSourceImage = null;
        protected DMLPatternReader lPatternReader;
        int numLPatternsFound = 0; //number of L patterns found 

        //class to sort edges in length, from larger to shorter
        //class ReverseInt : IComparer<int> { public int Compare(int x, int y) { return y - x; } }

        internal long TimeoutTimeInTicks = 0;

        protected bool IsTimeout()
        {
            if (TimeoutTimeInTicks == 0)
                return false;

            if (TimeoutTimeInTicks < DateTime.Now.Ticks)
                return true;

            return false;
        }
        
        public FoundBarcode[] DecodeBarcode(BlackAndWhiteImage bwImage, DMLPatternReader lPatternReader, int minAllowedBarcodeSideSize, int maxAllowedBarcodeSideSize)
        {
            this.bwSourceImage = bwImage;
            this.lPatternReader = lPatternReader;
            this.width = bwImage.Width;
            this.height = bwImage.Height;
            int minSize = this.width > this.height ? this.height : this.width;
            this.minAllowedBarcodeSideSize = minSize * minAllowedBarcodeSideSize / 100;
            this.maxAllowedBarcodeSideSize = minSize * maxAllowedBarcodeSideSize / 100;

            return Scan();
        }

        //object that holds the bw image + methods to sample, follow vertices,...
        protected ImageScaner scan;
        //list of edges in construction
        internal LinkedList<Edge> edges;
        //list of found codes found and correctly decoded.
        internal Dictionary<int, LinkedList<Edge>> ledges;
        protected LinkedList<BarCodeRegion> candidates;

        protected void CreateLists()
        {
            scan = new ImageScaner(bwSourceImage);
            edges = new LinkedList<Edge>();
            ledges = new Dictionary<int, LinkedList<Edge>>();
            candidates = new LinkedList<BarCodeRegion>();
            numLPatternsFound = 0;        
        }

        internal virtual FoundBarcode[] Scan()
        {
            CreateLists();
#if DEBUG
            var temp = bwSourceImage.GetAsBitmap();
            //temp.Save(@"out.png");
            Utils.SaveSKBitmap(temp, @"out.png");
#endif

            //main loop to scan horizontal lines
            XBitArray rowPrev = bwSourceImage.GetRow(0);
            XBitArray row = bwSourceImage.GetRow(1);
            for (int y = 2; y < height; y += scanRowStep)
            {
                XBitArray rowNext = bwSourceImage.GetRow(y);
                ScanBits(y - 1, rowPrev, row, rowNext, true);
                rowPrev = row;
                row = rowNext;
            }
            
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            //main loop to scan vertical lines
            XBitArray colPrev = bwSourceImage.GetColumn(0);
            XBitArray col = bwSourceImage.GetColumn(1);
            for (int x = 2; x < width; x += scanRowStep)
            {
                XBitArray colNext = bwSourceImage.GetColumn(x);
                ScanBits(x - 1, colPrev, col, colNext, false);
                colPrev = col;
                col = colNext;
            }
            
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            return FindDM(ledges, true);
        }

        internal virtual FoundBarcode[] FindDM(Dictionary<int, LinkedList<Edge>> ledges, bool simpleMode)
        {

#if FIND_EDGES
            foreach (LinkedList<Edge> l in ledges.Values) 
                foreach(Edge e in l) 
                    candidates.AddLast(new BarCodeRegion(e.start, e.end, e.start, e.end));
#else
            ArrayList result = new ArrayList();
            int[] lengths = new int[ledges.Count];
            ledges.Keys.CopyTo(lengths, 0);

            // sort keys
            Array.Sort(lengths);
            // reverse to make larger first
            Array.Reverse(lengths);


            //Look to find connected edges (dist < MAX_DIST_TO_CONSIDER_LPATTERN) at 90º with a correct ratio
            //This is a O(N^2) algorightm, optimized to check only edges with similar lengths
            // (this is relaxed to allow rectangular barcodes, or skewed ones).
            int processedEdges = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (MaxNumberOfBarcodes > 0 && candidates.Count >= MaxNumberOfBarcodes)
                    break;

                int currentLength = lengths[i];

                if (currentLength >= ledges.Count)
                {
                    continue; // skip too long edges
                }

                foreach (Edge A in ledges[currentLength])
                {
                    processedEdges++;

                    // if we have simple mode ON 
                    // or if we have simpleMode=false and need to make iterations
                    if (simpleMode || (processedEdges < decodingMaxRetries))
                    {
                        for (int j = i; j >= 0; j--)
                        {

                            if (MaxNumberOfBarcodes > 0 && candidates.Count >= MaxNumberOfBarcodes)
                                break;

                            //check lengths ratio
                            int length = lengths[j];
                            float ratio = (float)length / (float)currentLength;
                            if (ratio < dmMinAspectRatio || ratio > dmMaxAspectRatio || length >= ledges.Count)
                            {
                                continue; // break; //from sqare to max rectangular ratio
                            }
                            foreach (Edge B in ledges[length])
                            {
                                //check angle around 90º
                                MyVectorF a = A.start - A.end;
                                MyVectorF b = B.start - B.end;
                                float prodEsc = (a * b) / a.Length / b.Length;

                                //check if angle is perpendicular enough
                                if (Calc.Around(prodEsc, 0F, dmMaxAngleDifference))
                                {
                                    //check if edges are close enough (<MAX_DIST_TO_CONSIDER_LPATTERN)
                                    double d1 = (A.start - B.start).Length;
                                    double d2 = (A.start - B.end).Length;
                                    double d3 = (A.end - B.start).Length;
                                    double d4 = (A.end - B.end).Length;
                                    double la = a.Length;
                                    double lb = b.Length;

                                    double maxDistInPercents = MaxAllowedHoleBetweenLinesInLPattern;
                                    double maxDist = la > lb ? la * maxDistInPercents : lb * maxDistInPercents; //20% of max length                                   
                                    if (d1 < maxDist || d2 < maxDist || d3 < maxDist || d4 < maxDist)
                                    {
                                        MyPoint center, corner1, corner2;
                                        center = corner1 = corner2 = MyPoint.Empty;
                                        if (d1 < MAX_DIST_TO_CONSIDER_LPATTERN) { center = (A.end + B.end) / 2; corner1 = A.end; corner2 = B.end; }
                                        else if (d2 < MAX_DIST_TO_CONSIDER_LPATTERN) { center = (A.end + B.start) / 2; corner1 = A.end; corner2 = B.start; }
                                        else if (d3 < MAX_DIST_TO_CONSIDER_LPATTERN) { center = (A.start + B.end) / 2; corner1 = A.start; corner2 = B.end; }
                                        else { center = (A.start + B.start) / 2; corner1 = A.start; corner2 = B.start; }

                                        //check diagonal for 50% of black pixels if this is a sqare DM
                                        bool isCandidate = false;
                                        if (UseQuickCheckForBacodeInside)
                                        {
                                            float l1 = (A.end - A.start).Length;
                                            float l2 = (B.end - B.start).Length;

                                            if (l1 < l2) { float tmp = l1; l1 = l2; l2 = tmp; } //==> l1>l2
                                            if (l1 / l2 < 4f)
                                            {
                                                Bresenham bre = new Bresenham(corner1, corner2);
                                                int n = 0, nBlack = 0;
                                                while (!bre.End())
                                                {
                                                    n++;
                                                    if (scan.isBlack(bre.Current)) nBlack++;
                                                    bre.Next();
                                                }
                                                float grayLevel = (float)nBlack / (float)n;
                                                if (grayLevel > 0.20f & grayLevel < 0.9f)
                                                    isCandidate = true;
                                            }
                                            else isCandidate = true;
                                        }
                                        else
                                            isCandidate = true; // else set to true everytime

                                        if (isCandidate)
                                        {

#if !FIND_FINDER
                                            bool processed = false;
                                            foreach (BarCodeRegion br in candidates)
                                                if (br.In(center))
                                                {
                                                    processed = true;
                                                    break;
                                                }

                                            if (!processed)
#endif
                                                // calls either base LPattern reader
                                                // or inherited one (from DMConnectedReader for example)
                                                NewLPattern(A, B);

                                            if (MaxNumberOfBarcodes > 0 && candidates.Count >= MaxNumberOfBarcodes)
                                                break;

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endif

            ArrayList results = new ArrayList();
            if (candidates != null)
            {
                foreach (BarCodeRegion b in candidates)
                {
                    FoundBarcode foundBarcode = new FoundBarcode();
					foundBarcode.BarcodeFormat = SymbologyType.DataMatrix;

					String data = "";
					if (b.Data != null) 
                        foreach (ABarCodeData d in b.Data) 
                            data += d.ToString();
					foundBarcode.Value = data;
                    foundBarcode.Color = SKColors.Blue;

                    foundBarcode.Polygon = new SKPointI[5] { b.A, b.B, b.D, b.C, b.A };
					//byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
					//GraphicsPath path = new GraphicsPath(foundBarcode.Polygon, pointTypes);
					//foundBarcode.Rect = Rectangle.Round(path.GetBounds());
                    foundBarcode.Rect = Utils.DrawPath(foundBarcode.Polygon);

                    foundBarcode.Confidence = b.Confidence;

                    if (b.Confidence < 0.001f && foundBarcode.Value.Length < 2)
                        continue;

                    results.Add(foundBarcode);
                }
            }
            return (FoundBarcode[])results.ToArray(typeof(FoundBarcode));
        }


        internal virtual void NewLPattern(Edge A, Edge B)
        {
            numLPatternsFound++;
#if FIND_FINDER
            float d1 = (A.start - B.start).Length;
            float d2 = (A.start - B.end).Length;
            float d3 = (A.end - B.start).Length;
            float d4 = (A.end - B.end).Length;
            BarCodeRegion reg=null;
            if (d1 < MAX_HOLE_SIZE_BETWEEN_LINES_IN_PATTERN) reg = new BarCodeRegion(A.start, A.end, B.end, B.end+(A.end-A.start));
            else if (d2 < MAX_HOLE_SIZE_BETWEEN_LINES_IN_PATTERN) reg = new BarCodeRegion(A.start, A.end, B.start, B.start+(A.end-A.start));
            else if (d3 < MAX_HOLE_SIZE_BETWEEN_LINES_IN_PATTERN) reg = new BarCodeRegion(A.end, A.start, B.end, B.end+(A.start-A.end));
            else  reg = new BarCodeRegion(A.end, A.start, B.start, B.start+(A.start-A.end));
            if (reg!=null) candidates.AddLast(reg);
#else
            //Track edge crossing the center point of the start/reversed stop pattern
            EdgeTrack etA = new EdgeTrack(scan);
            MyPoint centerA = A.Center();
            MyVector vdY = A.start - A.end;
            MyVector vdX = (vdY.isHorizontal() ? new MyVector(0, A.isBlack ? -1 : 1) : new MyVector(A.isBlack ? -1 : 1, 0));
            etA.Track(centerA, vdX, 3F, true);
            MyPointF Aup = etA.Up(0);
            MyPointF Adown = etA.Down(0);
            Adown = Adown - ((MyVectorF)vdY).Normalized;
            if (Aup.IsInfinity || Adown.IsInfinity) return;
            RegressionLine lineA = etA.GetLine();

            EdgeTrack etB = new EdgeTrack(scan);
            MyPoint centerB = B.Center();
            vdY = B.start - B.end;
            vdX = (vdY.isHorizontal() ? new MyVector(0, B.isBlack ? -1 : 1) : new MyVector(B.isBlack ? -1 : 1, 0));
            etB.Track(centerB, vdX, 3, true);
            MyPointF Bup = etB.Up(0);
            MyPointF Bdown = etB.Down(0);
            if (Bup.IsInfinity || Bdown.IsInfinity) return;
            Bdown = Bdown - ((MyVectorF)vdY).Normalized;
            RegressionLine lineB = etB.GetLine();


            //find intersection
            MyPointF cross = lineA.Intersection(lineB); if (cross.IsEmpty) return;

            //detect orientation of A and B edges
            /*
            float d1 = (Aup - Bup).Length;
            float d2 = (Aup - Bdown).Length;
            float d3 = (Adown - Bup).Length;
            float d4 = (Adown - Bdown).Length;
            MyPointF a, b, a2, b2;
            if (d1 < MAX_HOLE_SIZE_BETWEEN_LINES_IN_PATTERN) { a = Adown; b = Bdown; a2 = A.end; b2 = B.end; }
            else if (d2 < MAX_HOLE_SIZE_BETWEEN_LINES_IN_PATTERN) { a = Adown; b = Bup; a2 = A.end; b2 = B.start; }
            else if (d3 < MAX_HOLE_SIZE_BETWEEN_LINES_IN_PATTERN) { a = Aup; b = Bdown; a2 = A.start; b2 = B.end; }
            else if (d4 < MAX_HOLE_SIZE_BETWEEN_LINES_IN_PATTERN) { a = Aup; b = Bup; a2 = A.start; b2 = B.start; }
            else return;*/
            MyPointF a, b, a2, b2;
            float d1 = (Aup - cross).Length;
            float d2 = (Adown - cross).Length;
            if (d1 > d2) { a = Aup; a2 = A.start; } else { a = Adown; a2 = A.end; }
            float d3 = (Bup - cross).Length;
            float d4 = (Bdown - cross).Length;
            if (d3 > d4) { b = Bup; b2 = B.start; } else { b = Bdown; b2 = B.end; }

            //clockwise?
            MyVectorF v1 = a - cross;
            MyVectorF v2 = b - cross;
            float k = v1.X * v2.Y - v1.Y * v2.X;
            if (k > 0) { MyPointF d = a; a = b; b = d; }
            MyPointF c = b + (a - cross);
            MyPointF c2 = b2 + (a2 - cross);

            ReadBarcode(cross, a, b, c);
#endif
        }

        protected virtual bool ReadBarcode(MyPointF A, MyPointF B, MyPointF C, MyPointF D)
        {
            var step = (B - A).Length * LPatternDispersionStep;
            var d1 = (B - A).Normalized * step;
            var d2 = (C - A).Normalized * step;

            //enumerate different offsets of points along L pattern
            for (int iOffset = 0; iOffset <= LPatternDispersion; iOffset++)
            {
                var a = A;
                var b = B + d1 * iOffset;
                var c = C + d2 * iOffset;
                var d = D + (d1 + d2) * iOffset;

#if DEBUG_IMAGE
                //Console.WriteLine("A: " + A + " B: " + B);
                DebugHelper.DrawArrow(a.X, a.Y, b.X, b.Y, Color.Red);
                DebugHelper.DrawArrow(a.X, a.Y, c.X, c.Y, Color.Blue);
                DebugHelper.DrawSquare(Color.Red, a, b, c, d);
                DebugHelper.SaveImage();
                //DebugHelper.ReInitImage();
#endif

                //try to recognize
                BarCodeRegion reg = lPatternReader.ReadBarcode(a, b, c, d);
                if (reg != null)
                {
                    candidates.AddLast(reg);
                    //break enumerate points if barcode is recognized
                    return true;
                }
            }

            return false;
        }

        //class to define a found edge during the scanning process.
        internal class Edge
        {
            public MyPoint start, end;
            public float sumAngle, angle;
            public bool isBlack; //white to black edge
            LinkedList<MyPoint> points;
            LinkedList<Edge> connections;
            int N;

            public Edge(MyPoint a, MyPoint e)
            {
                this.start = a;
                this.end = e;
            }

            public Edge(MyPoint p, float angle, bool isBlack)
            {
                start = end = p;
                this.sumAngle = this.angle = angle;
                this.N = 1;
                this.isBlack = isBlack;
                this.points = new LinkedList<MyPoint>();
                this.connections = new LinkedList<Edge>();
            }

            public int Belongs(MyPoint p, float angle, bool isBlack, bool isHorizontal)
            {
                if (this.isBlack == isBlack)
                {
                    int h = isHorizontal ? p.Y - end.Y : p.X - end.X;
                    if (h > 0) //join to an edge without added points at this step
                    {
                        int dw = isHorizontal ? p.X - end.X : p.Y - end.Y;
                        if (dw < 0) dw = -dw;
                        if (dw < 3) {
                            float dist = this.angle - angle; if (dist < 0) dist = -dist;
                            if (dist > (float)Math.PI) dist = (float)(2 * Math.PI) - dist;
                            if (dist<0.8F) return dw;
                        }
                    }
                }
                return -1;
            }

            public void Add(MyPoint p, float angle)
            {
                float dist = this.angle - angle;
                if (dist > (float)Math.PI) { angle += (float)(2*Math.PI); }
                else if (dist < -(float)Math.PI) { angle -= (float)(2 * Math.PI); }

                this.sumAngle += angle;
                this.N++;
                this.angle = this.sumAngle / this.N;
                this.end = p;
                this.points.AddLast(p);
            }

            public int Length(bool isHorizontal) { return isHorizontal ? this.end.Y - this.start.Y : this.end.X - this.start.X; }
            public float RealLength() { return (this.end - this.start).Length; }
            public void AddConnection(Edge e) { this.connections.AddLast(e); }
            public LinkedList<Edge> GetConnections() { return this.connections; }
            public void FindConnections(LinkedList<Edge> l, bool isHorizontal, int minSize, double MaxHoleSizeInside)
            {
                foreach (Edge b in l) if (this.ContinuesFrom(b, isHorizontal, minSize, MaxHoleSizeInside) != -1)
                        this.AddConnection(b);
            }
            public int ContinuesFrom(Edge e, bool isHorizontal, int minSize, double MaxHoleSizeInside)
            {
                if (e.Length(isHorizontal) < minSize) return -1;

                int totalDist = isHorizontal ? e.start.Y - this.end.Y : e.start.X - this.end.X;
                if (totalDist < 0) totalDist = -totalDist;
                int dist = isHorizontal ? e.end.X - this.start.X : e.end.Y - this.start.Y;
                if (dist < 0) dist = -dist;
                int length = this.Length(isHorizontal);
                bool connect = dist < MaxHoleSizeInside;

                if (length > 0 && connect)
                {
                    MyVectorF a = this.end - this.start;
                    MyVectorF b = e.end - e.start;
                    float prodEsc = a * b / a.Length / b.Length;
                    if (!Calc.Around(prodEsc, 1f, 0.2f)) connect = false;
                }
                return connect ? dist : -1;
            }
            public void ScanConnections(MyPoint end, bool isHorizontal, int minSize, ref LinkedList<Edge> newEdges, double MaxHoleSizeInside)
            {
                Edge best = null;
                int minD = int.MaxValue;
                foreach (Edge f in this.connections)
                {
                    int d = this.ContinuesFrom(f, isHorizontal, minSize, MaxHoleSizeInside);
                    if (d!=-1 && d < minD) { minD = d; best = f; }
                }

                if (minD < int.MaxValue)
                {
                    Edge n = new Edge(best.start, end);
                    newEdges.AddLast(n);
                    best.ScanConnections(end, isHorizontal, minSize, ref newEdges, MaxHoleSizeInside); //recursion                
                }
            }


            public MyPoint Center()
            {
                if (points != null && points.Count > 0)
                {
                    LinkedList<MyPoint>.Enumerator e = points.GetEnumerator();
                    for (int i = 0; i < points.Count / 2; i++) e.MoveNext();
                    return e.Current;
                }
                else
                {
                    return (start + end) / 2;
                }
            }

            public override string ToString()
            {
                return start.ToString() + " -->" + end.ToString();
            }
        }


        //Scans a horizontal 
        internal virtual void ScanBits(int id, XBitArray bitsPrev, XBitArray bits, XBitArray bitsNext, bool isHorizontal)
        {
            bool prevIsBlack = bits[1];
            int i = 2;
            while (i < bits.Size - 1)
            {
                bool currentIsBlack = bits[i];
                if (prevIsBlack ^ currentIsBlack)
                {
                    //sobel angle
                    if (currentIsBlack) i--; //calculate sobel from a white pixel
                    int c00 = bitsPrev[i - 1] ? 1 : 0, c01 = bitsPrev[i] ? 1 : 0, c02 = bitsPrev[i + 1] ? 1 : 0;
                    int c10 = bits[i - 1] ? 1 : 0, c11 = bits[i] ? 1 : 0, c12 = bits[i + 1] ? 1 : 0;
                    int c20 = bitsNext[i - 1] ? 1 : 0, c21 = bitsNext[i] ? 1 : 0, c22 = bitsNext[i + 1] ? 1 : 0;
                    if (currentIsBlack) i++; //remove offset from i
                    else i--; //move to the previous black pixel

                    int Gx = c02 + c22 - c00 - c20 + 2 * (c12 - c10);
                    int Gy = c20 + c22 - c00 - c02 + 2 * (c21 - c01);
                    int G = (Gx > 0 ? Gx : -Gx) + (Gy > 0 ? Gy : -Gy);
                    if (G >= 2) //4
                    {
                        //Hough
                        double Gangle = isHorizontal ? Math.Atan2(Gy, Gx) : Math.Atan2(Gx, Gy);
                        //if (Gangle < 0) Gangle += Math.PI;
                        //else while (Gangle + 0.01 > Math.PI) Gangle -= Math.PI;
                        MyPoint p = isHorizontal ? new MyPoint(i, id) : new MyPoint(id, i);
                        int best = int.MaxValue, j = 0;
                        Edge eBest = null;
                        foreach (Edge e in edges)
                        {
                            int d = e.Belongs(p, (float)Gangle, currentIsBlack, isHorizontal);
                            if (d != -1 && d < best)
                            {
                                best = d;
                                eBest = e;
                            }
                            j++;
                        }
                        if (eBest != null)
                        {
                            eBest.Add(p, (float)Gangle);
                        }
                        else
                        {
                            edges.AddLast(new Edge(p, (float)Gangle, currentIsBlack));
                        }
                    }
                    if (!currentIsBlack) i++;
                }
                prevIsBlack = currentIsBlack;
                i++;
            }

            //purge terminated edges
            LinkedList<Edge> removed = new LinkedList<Edge>();
            foreach (Edge e in edges)
                if (isHorizontal && (id == scan.Height - 2 || e.end.Y < id - 3) ||
                    !isHorizontal && (id == scan.Width - 2 || e.end.X < id - 3))
                    removed.AddLast(e);
            foreach (Edge e in removed)
            {
                edges.Remove(e);
                int k = (int)(e.end - e.start).Length;
                if (k>MIN_SIZE && k > minAllowedBarcodeSideSize && k<maxAllowedBarcodeSideSize) //Min and max edge length,always >20 pixels
                {
                    if (!ledges.ContainsKey(k)) ledges.Add(k, new LinkedList<Edge>());
                    ledges[k].AddLast(e);
                }
            }
        }

        public int NumLPattnerns { get { return numLPatternsFound; } }
    }
}
