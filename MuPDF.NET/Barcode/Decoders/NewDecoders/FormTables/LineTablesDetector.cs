using System.Collections;
using System.Drawing;
using SkiaSharp;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.FormTables
{
    //Class to detect tables with borders.
    //The algorithm uses horizontal lines returned by LineSlicer.
#if CORE_DEV
    public
#else
    internal
#endif
    class LineTablesDetector : SymbologyReader2D
    {
        ImageScaner scan;
        ArrayList[] angles;
        ArrayList result; //array to hold all found results
        int minRad = 5; //min long of segments
        int maxRad = 5000; //max long of segments
        int distanceBetweenSegmentsToSeeIfWeShouldJoinThem = 20; //max dist to join lines
        int minLengthOfSegments = 50; //min length of result lines
        int scanColumnsHeight = 10; //if a vertical line has >=scanColumnsHeight, consider it as a column

        //when true check for a left vertical line connecting two horizontal segments,
        //in order to consider them in the same table
        bool checkVerticalLinesToJoinRowsInTables = true;

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.Table;
        }

        int OFFSET = 1;
        protected override FoundBarcode[] DecodeBarcode()
        {
            scan = new ImageScaner(BWImage);
            LineSlicer slicer = new LineSlicer(BWImage);
            angles = slicer.GetLineEdges(minRad, maxRad, distanceBetweenSegmentsToSeeIfWeShouldJoinThem, minLengthOfSegments, true, false);
            result = new ArrayList();

            //find parallel segments of the same length
            for (int i = 0; i < angles.Length; i++)
                for (int j = 0; j < angles[i].Count; j++) if (angles[i][j] != null)
                    {
                        LineEdge a = (LineEdge) angles[i][j];
                        //angles[i][j] = null; //remove the current line to not to be used later

                        ArrayList parallels = new ArrayList();
                        parallels.Add(a);
                        //Look for parallel lines to a of the same length
                        for (int o = -OFFSET; o <= OFFSET; o++)
                        {
                            int ii = (i + o + angles.Length) % angles.Length;
                            for (int jj = 0; jj < angles[ii].Count; jj++) if (angles[ii][jj] != null && angles[ii][jj] != a)
                                {
                                    parallels.Add(angles[ii][jj]);
                                }
                        }

                        parallels.Sort();


                        //a = (LineEdge)parallels[0];
                        float maxIncX = a.length * 0.01f;
                        float maxIncLength = a.length * 0.1f;
                        if (maxIncLength > (float) distanceBetweenSegmentsToSeeIfWeShouldJoinThem)
                            maxIncLength = (float) distanceBetweenSegmentsToSeeIfWeShouldJoinThem * 0.97f + a.length * 0.03f;  //limit to minDist pixels, to avoid joining large lines

                        ArrayList segments = new ArrayList();
                        ArrayList lostEdges = new ArrayList();
                        //segments.Add(a);
                        //Look for parallel lines to a of the same length
                        for (int ii = 0; ii < parallels.Count; ii++)
                        {
                            LineEdge b = (LineEdge) parallels[ii];
                            if (Calc.Around(a.a.X, b.a.X, maxIncLength * 0.6f) && Calc.Around(a.length, b.length, maxIncLength))
                            {
                                segments.Add(b);
                                //a = b; //use b as the next reference edge --> more flexible than use only the first edge                            
                            }
                            else
                            {
                                lostEdges.Add(b);
                            }
                        }
                        //if 2 or more parallel lines of the same length are found process 
                        //each row in order to detect vertical lines and split rows in cells
                        if (segments.Count > 1)
                        {
                            //alignSegments(segments);
                            ArrayList tables = findConnectedSegments(segments);

                            foreach (ArrayList table in tables) if (table.Count > 1)
                                {
                                    alignSegments(table);
                                    ArrayList tag = new ArrayList();
                                    for (int k = 1; k < table.Count; k++)
                                    {
                                        //find short edges that falls between k-1 and k
                                        LineEdge prev = (LineEdge) table[k - 1];
                                        LineEdge next = (LineEdge) table[k];
                                        int minY = prev.a.Y;
                                        int maxY = next.a.Y;
                                        int minX = prev.a.X;
                                        int maxX = prev.b.X;
                                        ArrayList shortEdges = new ArrayList();
                                        ArrayList reminderEdges = new ArrayList();
                                        foreach (LineEdge e in lostEdges)
                                            if (e.a.Y >= minY && e.a.Y <= maxY) shortEdges.Add(e);
                                            else reminderEdges.Add(e);
                                        lostEdges = reminderEdges;

                                        //split the current row in cells
                                        ArrayList cells = new ArrayList();
                                        ArrayList usedEdges = new ArrayList();
                                        SplitColumns(next, prev.b - next.b, shortEdges, cells, usedEdges);
                                        tag.Add(cells);

                                        //remove edges from angles
                                        removeEdges(angles, table);
                                        removeEdges(angles, usedEdges); //remove only used shortEdges
                                    }

                                    LineEdge first = (LineEdge) table[0];
                                    LineEdge last = (LineEdge) table[table.Count - 1];

                                    StringBuilder cellCounts = new StringBuilder();
                                    foreach (ArrayList row in tag)
                                        cellCounts.AppendFormat(" {0}", row.Count);

                                    FoundBarcode foundBarcode = new FoundBarcode();
                                    foundBarcode.Value = "table" + cellCounts;
                                    foundBarcode.Polygon = new SKPointI[] { first.a, first.b, last.b, last.a, first.a };
                                    foundBarcode.Tag = tag;
                                    foundBarcode.Color = SKColors.Orange;

                                    // get bounding rectangle
                                    //byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
                                    //using (GraphicsPath path = new GraphicsPath(foundBarcode.Polygon, pointTypes))
                                    //    foundBarcode.Rect = Rectangle.Round(path.GetBounds());
                                    foundBarcode.Rect = Utils.DrawPath(foundBarcode.Polygon);

                                    result.Add(foundBarcode);
                                }
                        }
                    }
            return (FoundBarcode[]) result.ToArray(typeof(FoundBarcode));
        }


        void removeEdges(ArrayList[] angles, ArrayList edges)
        {
            for (int i = 0; i < angles.Length; i++)
                for (int j = 0; j < angles[i].Count; j++) if (angles[i][j] != null)
                        foreach (LineEdge e in edges) if (e == angles[i][j])
                                angles[i][j] = null;
        }

        //x-align start and end of all segments to be aligned with the first and last segments. 
        void alignSegments(ArrayList segments)
        {
            LineEdge last = (LineEdge) segments[segments.Count - 1];
            MyVectorF vdY = last.NormalizedVdY();
            for (int i = 0; i < segments.Count; i++)
            {
                alignSegments(last, (LineEdge) segments[i], vdY);
            }
        }

        //align start and end of b with a using the given normalized up vector
        void alignSegments(LineEdge a, LineEdge b, MyVectorF vdY)
        {
            float dist = a.Dist(b.a);
            b.a = a.a + vdY * dist;
            b.b = a.b + vdY * dist;
        }


        //if checkVerticalLinesToJoinRowsInTables is true join segments in tables if 
        //they have a left vertical line connecting them.
        ArrayList findConnectedSegments(ArrayList segments)
        {
            LineEdge top = (LineEdge) segments[0];
            LineEdge bottom = (LineEdge) segments[segments.Count - 1];
            MyVectorF vdY = top.a - bottom.a;
            vdY = vdY.Normalized;
            ArrayList tables = new ArrayList();
            if (checkVerticalLinesToJoinRowsInTables)
            {
                ArrayList currentTable = new ArrayList();
                LineEdge prev = (LineEdge) segments[0];
                currentTable.Add(prev);
                for (int i = 1; i < segments.Count; i++)
                {
                    LineEdge current = (LineEdge) segments[i];
                    if (connectedSegments(prev, current, vdY)) currentTable.Add(current);
                    else
                    {
                        tables.Add(currentTable);
                        currentTable = new ArrayList();
                        currentTable.Add(current);
                    }
                    prev = current;
                }
                tables.Add(currentTable);
            }
            else tables.Add(segments);
            return tables;
        }

        //two segments are connectes if there is a left vertical line connecting them.
        bool connectedSegments(LineEdge top, LineEdge bottom, MyVectorF vdY)
        {
            MyVectorF vdX = (bottom.b - bottom.a); vdX = vdX.Normalized;
            Bresenham br = new Bresenham(bottom.a, vdY);
            float offset = bottom.length * 0.02f; if (offset < 5f) offset = 5f; //search at least around -5..+5 pixels
            Bresenham brX = new Bresenham(bottom.a - vdX * offset, bottom.a + vdX * offset);

            while (!brX.End())
            {
                br.MoveTo(brX.Current);
                if (hasColumn(br, (int) top.Dist(brX.Current))) return true;
                brX.Next();
            }
            return false;
        }

        //split a row in columns
        void SplitColumns(LineEdge bottom, MyVector up, ArrayList shortEdges, ArrayList cells, ArrayList usedEdges)
        {
            int height = (int) up.Length;
            Bresenham br = new Bresenham(bottom.a, up);
            Bresenham brX = new Bresenham(bottom.a, bottom.b);
            MyPoint start = bottom.a;

            int x = 0;
            //scans the line top from left to right. For each pixel, traces a vertical line
            //and counts black pixels. If they are >scanColumnsHeight, this is considered as a 
            //cell separator. 
            while (!brX.End())
            {
                br.MoveTo(brX.Current);
                if (x > 10 && hasColumn(br, height))
                {
                    if (ProcessColumn(start, brX.Current, up, shortEdges, cells, usedEdges))
                    {
                        x = 0;
                        start = brX.Current;
                    }
                }
                brX.Next();
                x++;
            }
            if (x > 10)
            {
                ProcessColumn(start, brX.Current, up, shortEdges, cells, usedEdges);
            }
        }



        bool ProcessColumn(MyPoint start, MyPoint end, MyVector up, ArrayList shortEdges, ArrayList cells, ArrayList usedEdges)
        {
            float xDist = (end - start).Length;
            float ratio = up.Length / xDist;
            if (ratio > 3f && xDist < 20) return false;

            int xIn = start.X;
            int xEnd = end.X;

            //find short edges crossing the vertical line (column)
            ArrayList innerEdges = new ArrayList();
            ArrayList rowEdges = new ArrayList();
            bool clean = true;
            for (int i = 0; i < shortEdges.Count; i++) if (shortEdges[i] != null)
                {
                    LineEdge e = (LineEdge) shortEdges[i];
                    if (e.b.X - 10 < xIn || e.a.X + 10 > xEnd)
                    {
                        /*edges at the left or right of the cell --> discard these edges */
                    }
                    else
                    {
                        if (e.a.X + 10 < xEnd && xEnd < e.b.X - 10) clean = false;
                        if (Calc.Around(xEnd, e.b.X, 10)) rowEdges.Add(e);
                        else innerEdges.Add(e);
                    }
                }

            if (clean)
            {
                if (rowEdges.Count == 0) cells.Add(new SKPointI[] { start, start + up, end + up, end, start });
                else SplitRows(new LineEdge(start, end, 0f), up, rowEdges, innerEdges, cells, usedEdges);
                return true;
            }
            return false;
        }



        //split a cell in rows
        void SplitRows(LineEdge bottom, MyVector up, ArrayList rowEdges, ArrayList innerEdges, ArrayList cells, ArrayList usedEdges)
        {
            LineEdge prev = bottom;
            MyVectorF vdY = bottom.NormalizedVdY();
            for (int i = rowEdges.Count - 1; i >= 0; i--)
            {
                LineEdge e = (LineEdge) rowEdges[i];
                alignSegments(prev, e, vdY);
                usedEdges.Add(e);
                SplitColumns(prev, e.b - prev.b, innerEdges, cells, usedEdges);
                prev = e;
            }
            SplitColumns(prev, up - (prev.b - bottom.b), innerEdges, cells, usedEdges);
        }

        //Scans br to check if it follows a black line
        bool hasColumn(Bresenham br, int height)
        {
            int h = 0;
            br.Next();
            while (scan.In(br.Current) && scan.isBlack(br.Current) && h < scanColumnsHeight)
            {
                br.Next();
                h++;
            }
            if (h >= scanColumnsHeight)
            {
                int offset = 0;
                int offsetLimit = height / 10; // 10%
                MyVectorF left = br.Vd.Perpendicular.Normalized;
                MyVectorF right = new MyVectorF(-left.X, -left.Y);
                while (scan.In(br.Current) && h < height && offset >= -offsetLimit && offset <= offsetLimit)
                {
                    if (scan.isBlack(br.Current)) h++;
                    else if (scan.isBlack(br.CurrentF + left))
                    {
                        h++;
                        br.MoveTo(br.CurrentF + left);
                        offset--;
                    }
                    else if (scan.isBlack(br.CurrentF + right))
                    {
                        h++;
                        br.MoveTo(br.CurrentF + right);
                        offset++;
                    }
                    br.Next();
                }
                return h > height * 9 / 10;
            }
            return false;
        }

        public int MinRad { get { return minRad; } set { minRad = value; } }
        public int MaxRad { get { return maxRad; } set { maxRad = value; } }
        public int DistanceBetweenSegmentsToSeeIfWeShouldJoinThem { get { return distanceBetweenSegmentsToSeeIfWeShouldJoinThem; } set { distanceBetweenSegmentsToSeeIfWeShouldJoinThem = value; } }
        public int MinLengthOfSegments { get { return minLengthOfSegments; } set { minLengthOfSegments = value; } }
        public bool CheckVerticalLinesToJoinRowsInTables { get { return checkVerticalLinesToJoinRowsInTables; } set { checkVerticalLinesToJoinRowsInTables = value; } }
    }
}
