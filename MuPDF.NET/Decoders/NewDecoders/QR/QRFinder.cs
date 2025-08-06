using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.QR
{
    class QRFactory : FinderFactory
    {
        public float GetOffsetFactor() { return 1F; } //0.75 + deformation
        public float GetCenterRatio() { return 3F / 7F; }
        public int GetNumModules() { return 7; }
        public bool CanHaveHoles() { return true; }
        public SquareFinder IsFinder(ImageScaner scan, MyPointF A, MyPointF B, MyPointF C, MyPointF D)
        {
            QRFinder finder = null;
            finder = new QRFinder(A, B, C, D);

            //Sample the finder to be discard wrong candidates.
            //This test is not complete, but seems to be enough.
            MyVectorF luRight = finder.RightNormal * finder.Width / 7F;
            MyVectorF luDown = finder.DownNormal * finder.Height / 7F;
            Grid grid = new Grid(finder.Center(), luRight, luDown);
            int maxErrors = 6;
            //out border horizontal
            for (int i = -3; i <=3; i++)
            {
                if (scan.getSample(grid.GetSamplePointRegular(i, -3),0f) > 0.7F) if (--maxErrors == 0) return null;
                if (scan.getSample(grid.GetSamplePointRegular(i, 3),0f) > 0.7F) if (--maxErrors == 0) return null;
            }

            //out vertical border
            for (int i = -2; i <= 2; i++)
            {
                if (scan.getSample(grid.GetSamplePointRegular(-3, i),0f) > 0.7F) if (--maxErrors == 0) return null;
                if (scan.getSample(grid.GetSamplePointRegular(3, i),0f) > 0.7F) if (--maxErrors == 0) return null;
            }

            for (int i = -2; i <= 2; i++)
            {
                if (scan.getSample(grid.GetSamplePointRegular(i, -2),0f) < 0.3F) if (--maxErrors == 0) return null;
                if (scan.getSample(grid.GetSamplePointRegular(i, 2),0f) < 0.3F) if (--maxErrors == 0) return null;
            }
            for (int i = -1; i <= 1; i++)
            {
                if (scan.getSample(grid.GetSamplePointRegular(-2, i),0f) < 0.3F) if (--maxErrors == 0) return null;
                if (scan.getSample(grid.GetSamplePointRegular(2, i),0f) < 0.3F) if (--maxErrors == 0) return null;
            }

            //inner black 
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (scan.getSample(grid.GetSamplePointRegular(i, j),0f) > 0.7F) if (--maxErrors == 0) return null;

            return finder;
        }
    }

    class SmallQRFactory : FinderFactory
    {
        public float GetOffsetFactor() { return 1F; } //0.75 + deformation
        public float GetCenterRatio() { return 3F / 7F; }
        public int GetNumModules() { return 5; }
        public bool CanHaveHoles() { return true; }
        public SquareFinder IsFinder(ImageScaner scan, MyPointF A, MyPointF B, MyPointF C, MyPointF D)
        {
            QRFinder finder = null;
            finder = new QRFinder(A, B, C, D);

            //Sample the finder to be discard wrong candidates.
            //This test is not complete, but seems to be enough.
            MyVectorF luRight = finder.RightNormal * finder.Width / 5F;
            MyVectorF luDown = finder.DownNormal * finder.Height / 5F;
            Grid grid = new Grid(finder.Center(), luRight, luDown);
            int maxErrors = 6;

            //out border horizontal
            for (int i = -3; i <= 3; i++)
            {
                if (scan.getSample(grid.GetSamplePointRegular(i, -3), 0f) > 0.7F) if (--maxErrors == 0) return null;
                if (scan.getSample(grid.GetSamplePointRegular(i, 3), 0f) > 0.7F) if (--maxErrors == 0) return null;
            }

            //out vertical border
            for (int i = -2; i <= 2; i++)
            {
                if (scan.getSample(grid.GetSamplePointRegular(-3, i), 0f) > 0.7F) if (--maxErrors == 0) return null;
                if (scan.getSample(grid.GetSamplePointRegular(3, i), 0f) > 0.7F) if (--maxErrors == 0) return null;
            }

            for (int i = -2; i <= 2; i++)
            {
                if (scan.getSample(grid.GetSamplePointRegular(i, -2), 0f) < 0.3F) if (--maxErrors == 0) return null;
                if (scan.getSample(grid.GetSamplePointRegular(i, 2), 0f) < 0.3F) if (--maxErrors == 0) return null;
            }
            for (int i = -1; i <= 1; i++)
            {
                if (scan.getSample(grid.GetSamplePointRegular(-2, i), 0f) < 0.3F) if (--maxErrors == 0) return null;
                if (scan.getSample(grid.GetSamplePointRegular(2, i), 0f) < 0.3F) if (--maxErrors == 0) return null;
            }

            //inner black 
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (scan.getSample(grid.GetSamplePointRegular(i, j), 0f) > 0.7F) if (--maxErrors == 0) return null;

            MyVectorF d1 = (A - D)/5f;
            MyVectorF d2 = (B - C) / 5f;

            A += d1;
            D -= d1;
            B += d2;
            C -= d2;

            return new QRFinder(A,B,C,D);
        }
    }


    class QRFinder: SquareFinder
    {
        //Pattern of QR finder
        public static readonly int[][] finder = new int[][] { new int[] { 1, 1, 3, 1, 1 } };
        public static readonly int[][] smallFinder = new int[][] { new int[] { 1, 3, 1 } };
        public static readonly int[] midFinder = { 1, 1 };

        public QRFinder(MyPoint A, MyPoint B, MyPoint C, MyPoint D) : base(A,B,C,D,7) { }
        public QRFinder(QRFinder f) : base(f) { }
    }
}
