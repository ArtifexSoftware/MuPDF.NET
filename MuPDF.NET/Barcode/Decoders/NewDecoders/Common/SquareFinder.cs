namespace BarcodeReader.Core.Common
{
    interface FinderFactory
    {
        SquareFinder IsFinder(ImageScaner scan, MyPointF left, MyPointF right, MyPointF top, MyPointF bottom);
        float GetOffsetFactor();
        float GetCenterRatio();
        int GetNumModules();
        bool CanHaveHoles();
    }

    class SquareFinder: BarCodeRegion
    {
        //Max accepted ratio Y/X for finders
        protected static readonly float patternMaxRatio = 0.3F;

        //Max difference coeficient between the 4 edges of the finder
        protected static readonly float maxFinderEdgeLenghtDifference = 0.3F;

        //Max angle difference for finder edges
        protected static readonly float maxFinderAngleDifference = 0.25F;

        //Max height difference for finder edges
        protected static readonly float maxFinderHeightDifference = 0.3F;


            
        public float Width;
        public float Height;
        public MyVectorF RightNormal;
        public MyVectorF DownNormal;
        public float ModuleWidth, ModuleHeight;
        public MyVectorF ModuleRight, ModuleDown;

        public SquareFinder(SquareFinder f): base(f)
        {
            this.Width = f.Width;
            this.Height = f.Height;
            this.RightNormal = f.RightNormal;
            this.DownNormal = f.DownNormal;
            this.ModuleWidth = f.ModuleWidth;
            this.ModuleHeight = f.ModuleHeight;
            this.ModuleRight = f.ModuleRight;
            this.ModuleDown = f.ModuleDown;
        }

        //fast check to see if a and b (start and end points of a horizontal pattern)
        //has a vertical pattern crossing the center.
        //Used to fast accept/reject horizontal patterns found in the scan line process. 
        public static bool CheckCrossPattern(ImageScaner scan, MyPoint a, MyPoint b, MyPoint center, IPatternFinderNoiseRow patternFinder, FinderFactory factory, out MyPoint minpUp, out MyPoint minpDown)
        {
            //cross pattern search
            MyPoint pUp, pDown;
            pUp = pDown = center;
            minpUp = minpDown = MyPoint.Empty;

            int d = b.X - a.X;
            //for big patterns, scan the border of the mid black square, to avoid holes in the center produced by the BW conversion
            // d<350 defines how large square finders could be
            // refs #147, see 1028-qrcode-rotated-45-LARGE-x2.png and 1028-qrcode-rotated-45-LARGE.png
            XBitArray col = (d < 350 || !factory.CanHaveHoles() ? scan.GetColumn(center.X) : scan.GetColumn(center.X - d / 7));

            d = (int)((float)(d) * factory.GetOffsetFactor()) + 1;
            int start = center.Y - d;
            if (start < 0) start = 0;
            int end = center.Y + d;
            //if (end > col.Size) end = col.Size;//!!!!!
            int minNumModules =d/factory.GetNumModules()/3;
            patternFinder.NewSearch(col, start, end,  1, minNumModules>0?minNumModules:-1);
            int minDist = int.MaxValue;
            while (patternFinder.NextPattern()!=null)
            {
                pUp.Y = patternFinder.First;
                pDown.Y = patternFinder.Last;
                float dh = (float)(b.X - a.X);
                float dv = (float)(pDown.Y - pUp.Y);
                if (Calc.Around(dh / dv, 1.0F, patternMaxRatio)) //valid ratio
                {
                    int dist=patternFinder.Center-a.Y;
                    if (dist < 0) dist = -dist;
                    if (dist < minDist) { minDist = dist; minpUp = pUp; minpDown = pDown; }
                }
            }
            if ((float)minDist <= (float)(b.X - a.X) / factory.GetCenterRatio()) return true;
            return false;
        }

        //checks if points left, right, top and bottom lays on the 4 edges of a square finder
        public static SquareFinder IsFinder(ImageScaner scan, MyPoint left, MyPoint right, MyPoint top, MyPoint bottom, FinderFactory factory)
        {
            //Detect if the left-right are horizontal or vertical
            MyVector vd = right - left;
            bool isHorizontal = vd.isHorizontal();
            MyVector vdH, vdV;
            if (isHorizontal)
            {
                if (left.X < right.X) { vdH = new MyVector(-1, 0); vdV = new MyVector(0, -1); }
                else { vdH = new MyVector(1, 0); vdV = new MyVector(0, 1); }
            }
            else
            {
                if (left.Y < right.Y) { vdH = new MyVector(0, -1); vdV = new MyVector(1, 0); }
                else { vdH = new MyVector(0, 1); vdV = new MyVector(0, -1); }
            }

            MyPointF A, B, C, D;
            Regression rV, rH;
            EdgeTrack et = new EdgeTrack(scan);
            float moduleLength = vd.Length / factory.GetNumModules();
            bool startWithBlack = scan.isBlack(left);

            rV = et.Track(left, right, vdH, moduleLength, startWithBlack);
            rH = et.Track(top, bottom, vdV, moduleLength, startWithBlack);
            findVertexs(rV, rH, out A, out B, out C, out D);
            if (A.IsEmpty || B.IsEmpty || C.IsEmpty || D.IsEmpty) return null;

            //Checks if corner distances are right (they should approximate a square)
            MyVectorF ba = B - A, ca = C - A; 
            float ac = ca.Length;
            float ab = ba.Length;
            float bd = (B - D).Length;
            float cd = (C - D).Length;
            float epsilon = ab * maxFinderEdgeLenghtDifference;
            if (ac >= 6F && ab >= 6F && bd >= 6F && cd >= 6F && Calc.Around(ac, bd, epsilon) && Calc.Around(ab, cd, epsilon)
                && Calc.Around(ab, ac, epsilon * 3))
            {
                //check angle
                float cos = (ba * ca) / (ab * ac);
                if (Calc.Around(cos, 0.0F, maxFinderAngleDifference))
                {
                    SquareFinder finder = factory.IsFinder(scan, A, B, C, D);
                    return finder;
                }
            }
            return null;
        }

        static void findVertexs(Regression rV, Regression rH, out MyPointF A, out MyPointF B, out MyPointF C, out MyPointF D)
        {
            RegressionLine rLeft = rV.LineL;
            RegressionLine rRight = rV.LineR;
            RegressionLine rTop = rH.LineL;
            RegressionLine rBottom = rH.LineR;

            A = rLeft.Intersection(rBottom); 
            B = rBottom.Intersection(rRight); 
            C = rLeft.Intersection(rTop); 
            D = rTop.Intersection(rRight); 
        }


        //checks if a and b (start and end points of a pattern in ANY DIRECTION)
        //has a vertical pattern crossing the center.
        public static SquareFinder IsFinder(ImageScaner scan, MyPoint a, MyPoint b, PatternFinderNoise patternFinder, FinderFactory factory)
        {
            MyPoint pUp, pDown;
            MyPoint center=pUp=pDown = (a + b) / 2;
            MyVector vd=(b-a);
            MyVector up = new MyVector(vd.Y*3/4, -vd.X*3/4);
            if (vd.Length > 50f && factory.CanHaveHoles()) center = center - vd / (float)factory.GetNumModules();
            Bresenham br = new Bresenham(center + up, center - up);
            int minNumModules=(int)vd.Length/factory.GetNumModules()/3;
            patternFinder.NewSearch(br, true, minNumModules>0?minNumModules:-1);
            while (patternFinder.NextPattern()!=-1)
            {
                pUp = patternFinder.First;
                pDown = patternFinder.Last;

                float dh = (b - a).Length;
                float dv = (pDown - pUp).Length;
                if (Calc.Around(dh / dv, 1.0F, maxFinderHeightDifference))
                {
                    SquareFinder finder = IsFinder(scan, a, b, pUp, pDown, factory);
                    if (finder != null) return finder;
                }
            }
            return null;
        }


        //checks if a and b (start and end points of a HORIZONTAL pattern found during scan line)
        //PRE: a and b has a cross pattern (previously checked).
        public static SquareFinder IsFinder(ImageScaner scan, MyPoint left, MyPoint right, MyPoint center, PatternFinderNoise finder, PatternFinderNoise crossFinder,  FinderFactory factory)
        {
            //Detect if the left-right are horizontal or vertical
            MyVector vd = right - left;
            MyVector vdH = new MyVector(-1, 0);
            MyVector vdV = new MyVector(0, -1); 

            //Calculate angle using the left edge
            float moduleLength = vd.Length / factory.GetNumModules();
            bool startWithBlack = scan.isBlack(left);

            EdgeTrack le = new EdgeTrack(scan);
            le.Track(left, vdH, moduleLength, startWithBlack);
            MyVectorF mainH=le.GetLine().GetNormal().Normalized;
            float d = mainH * vdH;
            if (d > 0) mainH = mainH * -1F;
            else d = -d;
            float midLength =  vd.Length *0.75F / d;

            //find good edge points 
            finder.NewSearch(new Bresenham(center- mainH*midLength ,center + mainH * midLength), true, 0);
            while (finder.NextPattern()!=-1)
            {
                MyPoint l = finder.First;
                MyPoint r = finder.Last;
                SquareFinder f = IsFinder(scan, l, r, crossFinder, factory);
                if (f != null)
                    if (f.In(center)) 
                        return f;
            }
            return null;
        }


        public SquareFinder(MyPointF A, MyPointF B, MyPointF C, MyPointF D, int NumModules): base(A,B,C,D)
        {
            Width = ( (A - B).Length + (C - D).Length )/2.0F;
            Height = ((A - C).Length +(B - D).Length )/2.0F;

            RightNormal = (B - A).Normalized;
            DownNormal = (A - C).Normalized;

            ModuleWidth = Width / (float)NumModules;
            ModuleHeight = Height / (float)NumModules;

            ModuleRight = RightNormal * ModuleWidth;
            ModuleDown = DownNormal * ModuleHeight;
        }

        //Calculate the center of the finder
        public MyPointF Center()
        {
            MyPointF m = ((A + B) / 2.0F + (C + D) / 2.0F) / 2.0F;
            return m;
        }

        //Rotate 90º clockwise the 4 corners of the finder. Used to normalize finders of a QR code.
        public void Rotate()
        {
            MyPointF p = A; A = B; B = D; D = C; C = p;
            float w = Width; Width = Height; Height = w;
            MyVectorF v = RightNormal; RightNormal = DownNormal *-1F; DownNormal = v;
            float m = ModuleWidth; ModuleWidth = ModuleHeight; ModuleHeight = m;
            v = ModuleRight; ModuleRight = ModuleDown * -1F; ModuleDown = v; 
        }
    }
}
