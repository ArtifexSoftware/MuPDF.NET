using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Aztec
{
    class SmallAztecFactory : FinderFactory
    {
        public float GetOffsetFactor() { return 0.75F; }
        public float GetCenterRatio() { return 1F / 7F; }
        public int GetNumModules() { return 7; }
        public bool CanHaveHoles() { return false; }
        public SquareFinder IsFinder(ImageScaner scan, MyPointF A, MyPointF B, MyPointF C, MyPointF D)
        {
            AztecFinder finder = new AztecFinder(A, B, C, D, 7);

            //Sample the finder to be discard wrong candidates.
            //This test is not complete, but seems to be enough.
            MyVectorF luRight = finder.RightNormal * finder.Width / 7F;
            MyVectorF luDown = finder.DownNormal * finder.Height / 7F;
            Grid grid = new Grid(finder.Center(), luRight, luDown);

            int count = 0;
            for (int i = -2; i <= 2; i++)
            {
                if (scan.isBlackSample(grid.GetSamplePointRegular(i, -3),0f)) count++;
                if (!scan.isBlackSample(grid.GetSamplePointRegular(i, -2),0f)) count++;
                if (!scan.isBlackSample(grid.GetSamplePointRegular(i, 2),0f)) count++;
                if (scan.isBlackSample(grid.GetSamplePointRegular(i, 3),0f)) count++;
            }
            if (count > 3) return null;

            return finder;
        }
    }

    class BigAztecFactory : FinderFactory
    {
        public float GetOffsetFactor() { return 0.75F; }
        public float GetCenterRatio() { return 1F / 11F; }
        public int GetNumModules() { return 11; }
        public bool CanHaveHoles() { return false; }
        public SquareFinder IsFinder(ImageScaner scan, MyPointF A, MyPointF B, MyPointF C, MyPointF D)
        {
            AztecFinder finder = new AztecFinder(A, B, C, D, 11);
            //TODO check modules around
            return finder;
        }
    }

    class AztecFinder: SquareFinder
    {
        //Pattern of Aztec finder
        public static readonly int[][] finder = new int[][]{new int[]{ 1, 1, 1, 1, 1, 1, 1 }}; //WBWBWBW 

        public AztecFinder(MyPointF A, MyPointF B, MyPointF C, MyPointF D, int numModules) : base(A, B, C, D, numModules) { }
        public AztecFinder(AztecFinder f) : base(f) { }
    }
}
