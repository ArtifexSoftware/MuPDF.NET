using System;
using System.Collections.Generic;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Datamatrix
{
	internal class DMConnectReader: DMSimpleReader
    {
        LinkedList<Edge> allEdges, lastEdges;

        //allow connect 10 pixels holes
        internal double MaxHoleSizeInsideLines;

        internal override FoundBarcode[] Scan()
        {
            CreateLists();
            
            //main loop to scan horizontal lines
            lastEdges = new LinkedList<Edge>();
            allEdges = new LinkedList<Edge>();
            XBitArray rowPrev = bwSourceImage.GetRow(0);
            XBitArray row = bwSourceImage.GetRow(1);
            for (int y = 2; y < height; y += scanRowStep)
            {
                XBitArray rowNext = bwSourceImage.GetRow(y);
                ScanBits(y - 1, rowPrev, row, rowNext, true);
                rowPrev = row;
                row = rowNext;
            }
            //connect vertical edges
            LinkedList<Edge> verticalEdges = allEdges;
            
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();
           
           //main loop to scan vertical lines
            lastEdges = new LinkedList<Edge>();
            allEdges = new LinkedList<Edge>();
            XBitArray colPrev = bwSourceImage.GetColumn(0);
            XBitArray col = bwSourceImage.GetColumn(1);
            for (int x = 2; x < width; x += scanRowStep)
            {
                XBitArray colNext = bwSourceImage.GetColumn(x);
                ScanBits(x - 1, colPrev, col, colNext, false);                
                colPrev = col;
                col = colNext;
            }
            //connect horizontal edges
            LinkedList<Edge> horizontalEdges = allEdges;
            
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            //classify edges by length
            ledges = new Dictionary<int, LinkedList<Edge>>(); // (new ReverseInt2());
            classifyEdges(verticalEdges);
            classifyEdges(horizontalEdges);

            LinkedList<Edge> newVerticalEdges = new LinkedList<Edge>();
            foreach (Edge e in verticalEdges) e.ScanConnections(e.end, true, MIN_SIZE, ref newVerticalEdges, this.MaxHoleSizeInsideLines);
            if (newVerticalEdges.Count < 2 * verticalEdges.Count) classifyEdges(newVerticalEdges);

            LinkedList<Edge> newHorizontalEdges = new LinkedList<Edge>();
            foreach (Edge e in horizontalEdges) e.ScanConnections(e.end, false, MIN_SIZE, ref newHorizontalEdges, this.MaxHoleSizeInsideLines);
            if (newHorizontalEdges.Count < 2 * horizontalEdges.Count) classifyEdges(newHorizontalEdges);
            
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            return FindDM(ledges, false);
        }

        void classifyEdges(LinkedList<Edge> l)
        {
            foreach (Edge e in l)
            {
                int k = (int)(e.end - e.start).Length;
                if (!ledges.ContainsKey(k)) 
                {
                        ledges.Add(k, new LinkedList<Edge>());
                }
                ledges[k].AddLast(e);
            }
        }


        internal override void NewLPattern(Edge A, Edge B)
        {
#if FIND_FINDER
            float d1 = (A.start - B.start).Length;
            float d2 = (A.start - B.end).Length;
            float d3 = (A.end - B.start).Length;
            float d4 = (A.end - B.end).Length;
            BarCodeRegion reg=null;
            if (d1 < MAX_DIST_TO_CONSIDER_LPATTERN) reg = new BarCodeRegion(A.start, A.end, B.end, B.end+(A.end-A.start));
            else if (d2 < MAX_DIST_TO_CONSIDER_LPATTERN) reg = new BarCodeRegion(A.start, A.end, B.start, B.start+(A.end-A.start));
            else if (d3 < MAX_DIST_TO_CONSIDER_LPATTERN) reg = new BarCodeRegion(A.end, A.start, B.end, B.end+(A.start-A.end));
            else  reg = new BarCodeRegion(A.end, A.start, B.start, B.start+(A.start-A.end));
            if (reg!=null) candidates.AddLast(reg);
#else
            RegressionLine lineA, lineB;
            CrossEdges(A, B, out lineA, out lineB);
            bool found = ReadLPattern(lineA, A.start, A.end, lineB, B.start, B.end);
            if (!found)
            {
                MyPointF Aup, Adown, Bup, Bdown;
                TrackEdges(A, B, out lineA, out Aup, out Adown, out lineB, out Bup, out Bdown);
                found=ReadLPattern(lineA, Aup, Adown, lineB, Bup, Bdown);
            }

#endif
        }

        void TrackEdges(Edge A, Edge B, out RegressionLine lineA, out MyPointF Aup, out MyPointF Adown, 
                                        out RegressionLine lineB, out MyPointF Bup, out MyPointF Bdown)
        {
            lineA = lineB = null;
            Aup = Adown = Bup = Bdown = MyPointF.Empty;

            //Track edge crossing the center point of the start/reversed stop pattern
            EdgeTrack etA = new EdgeTrack(scan);
            MyPoint centerA = A.Center();
            MyVector vdY = A.start - A.end;
            MyVector vdX = (vdY.isHorizontal() ? new MyVector(0, A.isBlack ? -1 : 1) : new MyVector(A.isBlack ? -1 : 1, 0));
            etA.Track(centerA, vdX, 3F, true);
            Aup = etA.Up();
            Adown = etA.Down();
            Adown = Adown - ((MyVectorF)vdY).Normalized;
            if (Aup.IsInfinity || Adown.IsInfinity) return;
            lineA = etA.GetLine();

            EdgeTrack etB = new EdgeTrack(scan);
            MyPoint centerB = B.Center();
            vdY = B.start - B.end;
            vdX = (vdY.isHorizontal() ? new MyVector(0, B.isBlack ? -1 : 1) : new MyVector(B.isBlack ? -1 : 1, 0));
            etB.Track(centerB, vdX, 3, true);
            Bup = etB.Up();
            Bdown = etB.Down();
            if (Bup.IsInfinity || Bdown.IsInfinity) return;
            Bdown = Bdown - ((MyVectorF)vdY).Normalized;
            lineB = etB.GetLine();
        }

        void CrossEdges(Edge A, Edge B, out RegressionLine lineA, out RegressionLine lineB)
        {
            lineA = new RegressionLine(A.start, A.end);
            lineB = new RegressionLine(B.start, B.end);
        }


        bool ReadLPattern(RegressionLine lineA, MyPointF Aup, MyPointF Adown, RegressionLine lineB, MyPointF Bup, MyPointF Bdown)
        {
            //find intersection
            MyPointF cross = lineA.Intersection(lineB);

            //check if point is out of image's borders (with tolerance)
            if (cross.IsEmpty) return false;
            if (!bwSourceImage.In(cross, (width + height) / 4))
                return false;

            //detect orientation of A and B edges
            MyPointF a, b;
            float d1 = (Aup - cross).Length;
            float d2 = (Adown - cross).Length;
            if (d1 > d2) { a = Aup;} else { a = Adown; }
            float d3 = (Bup - cross).Length;
            float d4 = (Bdown - cross).Length;
            if (d3 > d4) { b = Bup; } else { b = Bdown; }

            //clockwise?
            MyVectorF v1 = a - cross;
            MyVectorF v2 = b - cross;
            float k = v1.X * v2.Y - v1.Y * v2.X;
            if (k > 0) { MyPointF d = a; a = b; b = d; }
            MyPointF c = b + (a - cross);

            return ReadBarcode(cross, a, b, c);
        }

        //Scans a horizontal  
        internal override void ScanBits(int id, XBitArray bitsPrev, XBitArray bits, XBitArray bitsNext, bool isHorizontal)
        {
            bool prevIsBlack = bits[1];
            int i=2;
            while (i < bits.Size-1)
            {
                bool currentIsBlack = bits[i];
                if (prevIsBlack ^ currentIsBlack)
                {
                    //sobel angle
                    if (currentIsBlack) i--; //calculate sobel from a white pixel
                    int c00 = bitsPrev[i - 1] ? 1 : 0, c01 = bitsPrev[i] ? 1 : 0, c02 = bitsPrev[i+1] ? 1 : 0;
                    int c10 = bits[i - 1] ? 1 : 0, c11 = bits[i ] ? 1 : 0, c12 = bits[i+1] ? 1 : 0;
                    int c20 = bitsNext[i - 1] ? 1 : 0, c21 = bitsNext[i] ? 1 : 0, c22 = bitsNext[i+1] ? 1 : 0;
                    if (currentIsBlack) i++; //remove offset from i
                    //else i--; //move to the previous black pixel

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
                        int best = int.MaxValue, j=0;
                        Edge eBest=null;
                        foreach(Edge e in edges) {
                            int d=e.Belongs(p, (float)Gangle, currentIsBlack, isHorizontal);
                            if (d!=-1 && d< best)
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
                            Edge e=new Edge(p, (float)Gangle, currentIsBlack);
                            edges.AddLast(e);

                            //find if the new edge connect with previous ones
                            e.FindConnections(lastEdges, isHorizontal,MIN_SIZE, this.MaxHoleSizeInsideLines);
                            e.FindConnections(edges, isHorizontal,MIN_SIZE, this.MaxHoleSizeInsideLines);
                        }
                    }
                }
                prevIsBlack = currentIsBlack;
                i++;
            }

            //purge terminated edges
            LinkedList<Edge> removed=new LinkedList<Edge>();
            foreach (Edge e in edges)
                if (isHorizontal && (id== scan.Height-2 || e.end.Y < id - 3) || 
                    !isHorizontal && (id == scan.Width-2 || e.end.X < id - 3)) 
                    removed.AddLast(e);

            foreach (Edge e in removed)
            {
                edges.Remove(e);
                if (e.Length(isHorizontal) > MIN_SIZE)
                {
                    MyPoint end = e.end;
                    if (isHorizontal) end.Y++;
                    else end.X++;
                    e.Add(end, e.angle); //add last point prepared to be converted to float pixel MyPixelF

                    lastEdges.AddLast(e);
                    allEdges.AddLast(e);
                }
            }

            //clean cache of last found edges
            LinkedList<Edge> toRemove = new LinkedList<Edge>();
            if (lastEdges != null)
            foreach (Edge e in lastEdges) if (isHorizontal && id - e.end.Y > 25 || !isHorizontal && id-e.end.X>25) toRemove.AddLast(e);
            foreach (Edge e in toRemove) 
                lastEdges.Remove(e);
        }
    }
}
