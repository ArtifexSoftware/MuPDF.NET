using System;
using System.Collections.Generic;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.MaxiCode
{
    //Class to find maxicode barcodes from a found horizontal pattern
    class MaxiCodeScaner
    {
        ImageScaner scan;
        IPatternFinderNoiseRow pf90;
        PatternFinderNoise pf45, pf135;

        //patterns to match: there are 2 patterns to allow different diameters.
        public static readonly int[][] finder = new int[][] { new int[] { 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1 }, new int[] { 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 2 } }; //BWBWBWWBWBWB 


        public MaxiCodeScaner(ImageScaner scan, IPatternFinderNoiseRow pf90, PatternFinderNoise pf45, PatternFinderNoise pf135)
        {
            this.scan = scan;
            this.pf90 = pf90;
            this.pf45 = pf45;
            this.pf135 = pf135;
        }

        //Main method to see if a found horizontal pattern with borders at (a-b) is a valid 
        //maxicode barcode. center is the middle of the central white segment (not necessary 
        //the center of a-b).
        //First checks if a-b is a valid finder, then turns around the finder looking for 
        //alignment patterns. Once all 360º are scaned, then tries to detect all possible 
        //sets of 6-alignment patterns, ordered by its quality. Finally takes the best quality 
        //set and find the border of the barcode region, using 2 different rectification coefficient.
        public BarCodeRegion[] ScanFinder(MyPoint a, MyPoint b, MyPoint center)
        {
            BarCodeRegion[] barcodes = null;
            MaxiCodeFinder finder = IsFinder(a, b, center);
            if (finder != null)
            {
                PatternFound[][] patterns = ScanAlignmentPatterns(finder);
                LinkedList<MaxiCodeAlignedPatterns> matches = FindAlignedPatterns(patterns);
                barcodes = new BarCodeRegion[matches.Count > 0 ? 3 : 0];
                int n = 0;
                foreach (MaxiCodeAlignedPatterns m in matches)
                {
                    //find barcode corners (region) using different coeficients for tracking modules.
		            barcodes[n++] = m.ScanRegion(scan, 0.2F);
                    barcodes[n++] = m.ScanRegion(scan, 0.1F);
                    barcodes[n++] = m.ScanRegion(scan, 0.05F);
                    break; //only process the best quality aligned patterns (seems to be enough!)
                }
            }
            return barcodes;
        }

        //A finder is valid if has another crossing pattern, vertical or diagonal
        public MaxiCodeFinder IsFinder(MyPoint a, MyPoint b, MyPoint center)
        {
            MyPoint centerV;
            MyPoint pUp90, pDown90, pUp45, pDown45, pUp135, pDown135;

            bool is90 = CheckCrossPattern(a, b, center, pf90, out pUp90, out pDown90, out centerV);
            bool is45 = CheckCrossPattern(a, b, centerV, Math.PI / 4, pf45, out pUp45, out pDown45);
            bool is135 = CheckCrossPattern(a, b, centerV, 3 * Math.PI / 4, pf135, out pUp135, out pDown135);
            int count = (is90 ? 1 : 0) + (is45 ? 1 : 0) + (is135 ? 1 : 0);
            if (count > 0)
            {
                scan.setBWThreshold(a, b);
#if DEBUG_IMAGE
                scan.Reset();
#endif
                Sample[] samples = null;
                if (is90) samples = new Sample[] { new Sample(b, 0F, -1), new Sample(pUp90, 90F, 0), new Sample(a, 180F, 0), new Sample(pDown90, 270F, -1) };
                else if (is45) samples = new Sample[] { new Sample(b, 0F, -1), new Sample(pUp45, 45F, 0), new Sample(a, 180F, 0), new Sample(pDown45, 225F, -1) };
                else if (is135) samples = new Sample[] { new Sample(b, 0F, -1), new Sample(pUp135, 135F, 0), new Sample(a, 180F, 0), new Sample(pDown135, 315F, -1) };
                else return null;
#if DEBUG_IMAGE
                foreach (Sample s in samples) scan.setPixel(new MyPointF(0.5F, 0.5F) + (MyPointF)s.point, System.Drawing.Color.Orange);
#endif
                //Creates a new maxicode finder based on the borders of the found patterns.
                //This allows to trace skewed circles around the finder.
                MaxiCodeFinder circle = new MaxiCodeFinder(scan, centerV, samples);
                
                //checks external black circle: radius 4*W
                SampledCircle[] circles = circle.traceCircle(new float[] { 4F });
                int black = 0;
                foreach (MyPoint p in circles[0].points) if (scan.isBlack(p)) black++;
                float allBlack = (float)black / (float)circles[0].points.Length;
                if (allBlack < 0.6F) return null;

                //checks external white circle: radius 3.3*W (adjusted empirically)
                circles = circle.traceCircle(new float[] { 3.3F });
                int white = 0;
                foreach (MyPoint p in circles[0].points) if (!scan.isBlack(p)) white++;
                float allWhite = (float)white / (float)circles[0].points.Length;
                if (allWhite < 0.5F) return null;

#if DEBUG_IMAGE
                scan.Save(@"d:\out.png");
#endif
                return circle;
            }
            return null;
        }


        class PatternFound
        {
            public float quality;
            public MyPointF p;
            public PatternFound(float q, MyPointF pp) { quality = q; p = pp; }
        }

        enum Color { Black, White, Unknown };
        static readonly Color[][] patterns = new Color[][] { 
                        new Color[]{Color.Black, Color.White, Color.Black, Color.Black, Color.White, Color.Black}, //5F
                        new Color[]{Color.Black, Color.White, Color.Black, Color.Black, Color.Black, Color.White}, //5.5F
                        new Color[]{Color.White, Color.White, Color.Black, Color.White, Color.Black, Color.Black}}; //6F

        static float same(float f, Color color)
        {
            if (color == Color.White) { if (f < 0.4F) return 1F - f; }
            else if (color == Color.Black) { if (f > 0.6F) return f; }
            return 0F;
        }

        //Turns 360º around the finder looking for alignment patterns. Angles are discretized
        //at pixel scale. For each angle, tries to find the alignment pattern at different 
        //distances (to detect them even for skewed barcodes!).
        PatternFound[][] ScanAlignmentPatterns(MaxiCodeFinder finder)
        {
            SampledCircle[] circles = finder.traceCircle(new float[] { 7F });
            PatternFound[][] patternsFound = new PatternFound[circles[0].points.Length][];
            for (int i = 0; i < circles[0].points.Length; i++)
            {
                MyPointF p = circles[0].points[i];
                MyVectorF vdX = (p - finder.center).Normalized;
                MyVectorF vdY = vdX.Perpendicular;

                float angle = circles[0].angles[i];
                float d, v, w, h;
                finder.getRadius(angle, out d, out v, out w, out h);
                MyPointF pEnd = p - vdX * (w * 2F); //scans alignment patterns from distance 5W to 7W
                float qualityThreshold = 0.6F;

                MyVectorF W = vdX * w;
                MyVectorF W2H = vdX * (0.5F * w) - vdY * h;

                patternsFound[i] = new PatternFound[6];
                Bresenham br = new Bresenham(pEnd, p);
                while (!br.End())
                {
                    MyPointF p1 = br.CurrentF;
                    MyPointF p0 = p1 - W;
                    MyPointF p2 = p1 + W;
                    MyPointF p3 = p1 + W2H;

                    float s0 = scan.getModuleGrayLevel(p0, w);
                    float s1 = scan.getModuleGrayLevel(p1, w);
                    float s2 = scan.getModuleGrayLevel(p2, w);
                    float s3 = scan.getModuleGrayLevel(p3, w);

                    for (int j = 0; j < 6; j++)
                    {
                        float q0 = (j != 1 ? same(s0, Color.White) : 1F); //do not check for pattern 0 (WWW)
                        float q1 = same(s1, patterns[0][j]);
                        float q2 = same(s2, patterns[2][j]);
                        float q3 = same(s3, patterns[1][j]);
                        if (q0 > qualityThreshold && q1 > qualityThreshold && q2 > qualityThreshold && q3 > qualityThreshold)
                        {
                            float q = (float)Math.Sqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
                            if (patternsFound[i][j] == null || patternsFound[i][j].quality < q)
                                patternsFound[i][j] = new PatternFound(q, p1);
                        }
                    }
#if DEBUG_IMAGE
                    /*if (false)
                    {
                        scan.Save(@"d:\out.png");
                        scan.Reset();
                    }*/
#endif
                    br.Next();
                }
            }
#if DEBUG_IMAGE
            scan.Save(@"d:\out.png");
#endif
            return patternsFound;
        }

        //Once alignment patterns 360º around the finder are scanned, this method find all
        //sets of 6 alignment patterns and order them based on its quality.
        LinkedList<MaxiCodeAlignedPatterns> FindAlignedPatterns(PatternFound[][] patternsFound)
        {
            int offset = (patternsFound.Length) / 100;
            if (offset < 1) offset = 1;

            LinkedList<MaxiCodeAlignedPatterns> matches = new LinkedList<MaxiCodeAlignedPatterns>();
            for (int i = 0; i < patternsFound.Length; i++)
            {
                MaxiCodeAlignedPatterns m = new MaxiCodeAlignedPatterns();
                for (int j = 0; j < 6; j++)
                {
                    int pos = i + (j * patternsFound.Length) / 6;
                    float best = 0;
                    int iBest = -1;
                    MyPointF pBest = MyPointF.Empty;
                    for (int k = -offset; k <= offset; k++)
                    {
                        int pk = pos + k;
                        if (pk < 0) pk += patternsFound.Length;
                        if (pk >= patternsFound.Length) pk -= patternsFound.Length; //loop
                        if (patternsFound[pk][j] != null && patternsFound[pk][j].quality > best)
                        {
                            iBest = j;
                            best = patternsFound[pk][j].quality;
                            pBest = patternsFound[pk][j].p;
                        }
                    }
                    if (iBest == -1) { m = null; break; }
                    m.Add(best, pBest);
                }
                if (m != null)
                {   //add sorted by quality
                    LinkedListNode<MaxiCodeAlignedPatterns> j = matches.First;
                    while (j != null) if (j.Value.quality < m.quality) break; else j = j.Next;
                    if (j == null) matches.AddLast(m);
                    else matches.AddBefore(j, m);
                }
            }
            return matches;
        }


        //Fast check to see if a and b (start and end points of a horizontal pattern)
        //has a VERTICAL pattern crossing the center.
        //Used to fast accept/reject horizontal patterns found in the scan line process. 
        public bool CheckCrossPattern(MyPoint a, MyPoint b, MyPoint center, IPatternFinderNoiseRow patternFinder, out MyPoint minpUp, out MyPoint minpDown, out MyPoint centerV)
        {
            XBitArray col = scan.GetColumn(center.X);

            //cross pattern search
            MyPoint pUp, pDown;
            pUp = pDown = center;
            minpUp = minpDown = MyPoint.Empty;
            centerV = center;

            int d = (int)((float)(b.X - a.X) * 0.75F) + 1;
            int start = center.Y - d; if (start < 0) start = 0;
            int end = center.Y + d; if (end > col.Size) end = col.Size;
            patternFinder.NewSearch(col, start, end, 1, -1);
            int minDist = int.MaxValue;
            while (patternFinder.NextPattern() != null)
            {
                pUp.Y = patternFinder.First;
                pDown.Y = patternFinder.Last;
                float dh = (float)(b.X - a.X);
                float dv = (float)(pDown.Y - pUp.Y);
                if (Calc.Around(dh / dv, 1.0F, 0.3F)) //valid ratio
                {
                    int dist = patternFinder.Center - a.Y;
                    if (dist < 0) dist = -dist;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minpUp = pUp;
                        minpDown = pDown;
                        centerV.Y = patternFinder.Center;
                    }
                }
            }
            if ((float)minDist <= (float)(b.X - a.X) / 12F) return true;
            return false;
        }

        //Fast check to see if a and b (start and end points of a horizontal pattern)
        //has a DIAGONAL pattern crossing the center.
        //Used to fast accept/reject horizontal patterns found in the scan line process. 
        public bool CheckCrossPattern(MyPoint a, MyPoint b, MyPoint center, double angle, PatternFinderNoise patternFinder, out MyPoint minpUp, out MyPoint minpDown)
        {
            //cross pattern search
            MyPoint pUp, pDown;
            pUp = pDown = center;
            minpUp = minpDown = MyPoint.Empty;

            double d = (double)(b.X - a.X) * 0.75 + 1;
            MyVector dd = new MyVector((int)(d * Math.Cos(angle)), -(int)(d * Math.Sin(angle)));
            MyPoint start = center + dd;
            MyPoint end = center - dd;
            Bresenham br = new Bresenham(start, end);
            patternFinder.NewSearch(br, true, 0);
            float minDist = float.MaxValue;
            float hDist = (a - b).Length;
            while (patternFinder.NextPattern()>-1)
            {
                pUp = patternFinder.First;
                pDown = patternFinder.Last;
                float angleDist = (pUp - pDown).Length;
                if (Calc.Around(hDist / angleDist, 1.0F, 0.1F)) //valid ratio
                {
                    float dist = ((pUp + pDown) / 2 - center).Length;
                    if (dist < 0) dist = -dist;
                    if (dist < minDist) { minDist = dist; minpUp = pUp; minpDown = pDown; }
                }
            }
            if (minDist <= (float)(b.X - a.X) / 12F) return true;
            return false;
        }
    }
}

