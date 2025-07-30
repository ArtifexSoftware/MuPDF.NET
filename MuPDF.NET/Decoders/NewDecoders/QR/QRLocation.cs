using System.Collections;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.QR
{
    //Class to hold a QRcode defined by its 3 finders, always normalized (top-left, top-right and bottom-left
    class QRLocation: BarCodeRegion
    {
        public QRFinder LU, LD, RU;
        public float NominalWidth, NominalHeight;
        public float BaselineLength;
        public MyPointF[] AlignmentSubstitutionPoints;
        public MyPointF UpperLeft, UpperRight, BottomLeft;
        public MyVectorF LeftNormal, DownNormal;
        public bool Inverted = false;

        public float Wul { get { return LU.Width;} }
        public float Hul { get { return LU.Height;} }
        public float Wur { get { return RU.Width;} }
        public float Hur { get { return RU.Height;} }
        public float Wbl { get { return LD.Width;} }
        public float Hbl { get { return LD.Height;} }

        static readonly MyVector RIGHT = new MyVector(1, 0), LEFT = new MyVector(-1, 0), UP = new MyVector(0, -1), DOWN = new MyVector(0, 1);
        static readonly MyVector[] DIRS = new MyVector[] { RIGHT, LEFT, UP, DOWN };
        class Finder
        {
            public QRFinder finder;
            public MyVector dir;
            public Finder[] next;

            public Finder(QRFinder finder, MyVector dir, int N)
            {
                this.finder = finder;
                this.dir = dir;
                this.next = new Finder[N];
            }
            public MyVector[] NextDirs()
            {
                if (dir == new MyVector(0,0)) return DIRS;
                else return new MyVector[] { new MyVector(-dir.Y, dir.X), new MyVector(dir.Y, -dir.X)};
            }
        }

        static void Scan(ImageScaner scan, PatternFinderNoise patternFinders, PatternFinderNoise crossFinder,
            PatternFinderNoise smallPatternFinder, PatternFinderNoise smallCrossFinder, Finder finder, int n)
        {
            QRFinder next = null;
            int d = 0;
            foreach (MyVector dir in finder.NextDirs())
            {
                next = Scan(scan, patternFinders, crossFinder, smallPatternFinder, smallCrossFinder, finder.finder, dir);
                if (next != null)
                {
                    if (dir == UP || dir == DOWN) next.Rotate();
                    finder.next[d] = new Finder(next, dir,2);
                    if (n < 3) Scan(scan, patternFinders, crossFinder, smallPatternFinder, smallCrossFinder, finder.next[d], n + 1);
                }
                d++;
            }
        }

        static QRFinder Scan(ImageScaner scan, PatternFinderNoise patternFinder, PatternFinderNoise crossFinder,
            PatternFinderNoise smallPatternFinder, PatternFinderNoise smallCrossFinder,
            QRFinder f, MyVector dir)
        {
            Bresenham br = null;
            MyPointF A=new MyPointF(0,0),B=new MyPointF(0,0);
            if (dir==RIGHT) {br = new Bresenham(f.A, f.B); A = f.B; B = f.D; }
            else if (dir==LEFT) {br = new Bresenham(f.B, f.A); A = f.A; B = f.C;}
            else if (dir== UP){ br = new Bresenham(f.A, f.C); A = f.C; B = f.D; }
            else if (dir==DOWN){ br = new Bresenham(f.C, f.A); A = f.A; B = f.B; }
            float[] points = new float[] { 3.5f/7f, 2.5f/7f, 4.5f/7f };
            //MyPoint[] points = new MyPoint[] { (A + B) / 2, A*3.5f/7f (A*3+B)/4, (A+B*3)/4};
            var markerSizeSq = (A - B).LengthSq;

            //first step: try looking for full pattern 11311
            QRFactory factory = new QRFactory();
            foreach (float coef in points)
            {
                MyPointF p = A * coef + B * (1f - coef);
                br.MoveTo(p);
                patternFinder.NewSearch(br, false, -1);
                QRFinder next = null;
                while (next == null && patternFinder.NextPattern()>-1)
                {
                    MyPoint a = patternFinder.First;
                    MyPoint b = patternFinder.Last;

                    //check distance between a and A
                    if ((A - (MyPointF)a).LengthSq < markerSizeSq * 0.8f)//too close to first square?
                        continue;

                    //
                    SquareFinder sqFinder = SquareFinder.IsFinder(scan, a, b, crossFinder, factory);
                    if (sqFinder!=null)
                    {
                        next = (QRFinder)sqFinder;
                        if (next != null && ( !Calc.Around(f.Width, next.Width, f.Width * 0.2F) &&
                            !Calc.Around(f.Height, next.Height, f.Height * 0.2F) &&
                            !Calc.Around(f.Width, next.Height, f.Width * 0.2F) &&
                            !Calc.Around(f.Height, next.Width, f.Height * 0.2F) )
                            ) next = null;
                        if (next != null) return next;
                    }
                }
            }

            //secod step: try looking for small pattern 131
            SmallQRFactory smallFactory = new SmallQRFactory();
            foreach (float coef in points)
            {
                MyPointF p = A * coef + B * (1f - coef);
                br.MoveTo(p);
                smallPatternFinder.NewSearch(br, false, -1);
                QRFinder next = null;
                while (next == null && smallPatternFinder.NextPattern() > -1)
                {
                    MyPoint a = smallPatternFinder.First;
                    MyPoint b = smallPatternFinder.Last;
                    SquareFinder sqFinder = SquareFinder.IsFinder(scan, a, b, smallCrossFinder, smallFactory);
                    if (sqFinder != null)
                    {
                        next = (QRFinder)sqFinder;
                        if (next != null && (!Calc.Around(f.Width, next.Width, f.Width * 0.2F) &&
                            !Calc.Around(f.Height, next.Height, f.Height * 0.2F) &&
                            !Calc.Around(f.Width, next.Height, f.Width * 0.2F) &&
                            !Calc.Around(f.Height, next.Width, f.Height * 0.2F))
                            ) next = null;
                        if (next != null) return next;
                    }
                }
            }

            return null;
        }

        const float MaxAspectRatio = 0.5F;

        //given 3 finders add them as a new location if the 3 finders are well placed (90 degree and
        //defining and square). Finders are rotated to be normalized.
        static void AddLocation(ImageScaner scan, ArrayList locations, Finder ff0, Finder ff1, Finder ff2)
        {
            QRFinder f0 = new QRFinder(ff0.finder); //clone finders
            QRFinder f1 = new QRFinder(ff1.finder);
            QRFinder f2 = new QRFinder(ff2.finder);
            MyPointF a = f0.Center();
            MyPointF b = f1.Center();
            MyPointF c = f2.Center();
            MyVectorF AB = a - b;
            MyVectorF BC = b - c;
            MyVectorF CA = c - a;

            float ab = AB.Length;
            float bc = BC.Length;
            float ca = CA.Length;
            QRLocation location = null;
            if (ab > bc && ab > ca)
            {
                if (Calc.Around(bc, ca, bc * MaxAspectRatio))
                {
                    float crossProduct = AB.X * BC.Y - AB.Y * BC.X;
                    if (crossProduct > 0)
                        location = new QRLocation(f2, f1, f0);
                    else
                        location = new QRLocation(f2, f0, f1);
                }
            }
            else if (bc > ab && bc > ca)
            {
                if (Calc.Around(ab, ca, ca * MaxAspectRatio))
                {
                    float crossProduct = BC.X * CA.Y - BC.Y * CA.X;
                    if (crossProduct > 0)
                        location = new QRLocation(f0, f2, f1);
                    else
                        location = new QRLocation(f0, f1, f2);
                }
            }
            else //ca>ab && ca>bc
            {
                if (Calc.Around(bc, ab, ab * MaxAspectRatio))
                {
                    float crossProduct = CA.X * AB.Y - CA.Y * AB.X;
                    if (crossProduct > 0)
                        location = new QRLocation(f1, f0, f2);
                    else
                        location = new QRLocation(f1, f2, f0);
                }
            }
            if (location != null) locations.Add(location);
        }

        //Main method to find finders around a given finder. It wors recursively, but only 2 levels depth.
        //At the first level tries 4 directions, and and second level only 2, and 90º from the incoming direction.
        //All valid combinations are added to the result array.
        public static ArrayList Scan(ImageScaner scan, PatternFinderNoise patternFinder, PatternFinderNoise crossFinder,
            PatternFinderNoise smallPatternFinder, PatternFinderNoise smallCrossFinder, QRFinder lu)
        {
            Finder finders = new Finder(lu, new MyVector(0, 0), 4);
            Scan(scan, patternFinder, crossFinder, smallPatternFinder, smallCrossFinder, finders, 1);

            ArrayList locations = new ArrayList();
            //find valid combinations at depth level 3
            Finder f0=finders;
            for (int i = 0; i < 4; i++) if (f0.next[i] != null)
                {
                    Finder f1 = f0.next[i];
                    for (int j = 0; j < 2; j++) if (f1.next[j] != null)
                            AddLocation(scan, locations, f0, f1, f1.next[j]);
                }
            //find valid combinations at depth level 2
            if (f0.next[0] != null)
            {
                if (f0.next[2] != null) AddLocation(scan, locations, f0, f0.next[0], f0.next[2]);
                if (f0.next[3] != null) AddLocation(scan, locations, f0, f0.next[0], f0.next[3]);
            }
            if (f0.next[1] != null)
            {
                if (f0.next[2] != null) AddLocation(scan, locations, f0, f0.next[1], f0.next[2]);
                if (f0.next[3] != null) AddLocation(scan, locations, f0, f0.next[1], f0.next[3]);
            }

            return locations;            
        }

        //rotate the finder according to the leftNormal and downNormal
        void Align(QRFinder f)
        {
            float h = f.RightNormal * this.LeftNormal;
            if (Calc.Around(h, 0, 0.5F)) f.Rotate();
            h = f.RightNormal * this.LeftNormal;
            if (h<0)  { f.A.swap(ref f.B); f.C.swap(ref f.D); f.RightNormal *= -1.0F; }
            float v = f.DownNormal * this.DownNormal;
            if (v < 0) { f.A.swap(ref f.C); f.B.swap(ref f.D); f.DownNormal *= -1.0F; }
        }

        void Init(QRFinder lu, QRFinder ld, QRFinder ru)
        {            
            LU = lu;
            LD = ld;
            RU = ru;

            UpperLeft = lu.Center();
            UpperRight=ru.Center();
            BottomLeft=ld.Center();

            LeftNormal = (UpperRight - UpperLeft).Normalized;
            DownNormal = (BottomLeft - UpperLeft).Normalized;

            Align(LU);
            Align(LD);
            Align(RU);

            NominalWidth = (Wur + Wul) / 14.0F;
            NominalHeight = (Hul + Hbl) / 14.0F;
            MyVectorF mid = LeftNormal * (NominalWidth / 2.0F)+ DownNormal * (NominalHeight / 2.0F);
            AlignmentSubstitutionPoints = new MyPointF[3];
            AlignmentSubstitutionPoints[0] = lu.B -mid;
            AlignmentSubstitutionPoints[1] = ru.A + new MyVectorF(mid.Y, -mid.X);
            AlignmentSubstitutionPoints[2] = ld.D + new MyVectorF(-mid.Y, mid.X);

            BaselineLength = (UpperLeft - UpperRight).Length;
        }

        public QRLocation(QRFinder lu, QRFinder ld, QRFinder ru)
        {
            Init(lu, ld, ru);
            SetCorners(LD.A, LD.A + (RU.D - LU.C), LU.C, RU.D);
        }

    }
}
