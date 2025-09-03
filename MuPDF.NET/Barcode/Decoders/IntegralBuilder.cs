using BarcodeReader.Core;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core
{
#if CORE_DEV
    public
#else
    internal
#endif
    static class IntegralBuilder
    {
        public static int[][] Build(IBlackAndWhiteFilter img, int w, int h)
        {
            var res = new int[w + 1][];

            for (int x = 0; x < w + 1; x++)
                res[x] = new int[h + 1];

            XBitArray[] rows = new XBitArray[h];

            for (int y = 0; y < h; y++)
                rows[y] = img.GetRow(y);

            //sum by rows
            Parallel.For(0, h, (y) =>
            {
                int sumRow = 0;
                for (int x = 0; x < w; x++)
                {
                    sumRow += rows[y][x] ? 1 : 0;
                    res[x + 1][y + 1] = sumRow;
                }
            }
            );

            //sum by cols
            Parallel.For(0, w, (x) =>
            {
                var col = res[x + 1];
                for (int y = 1; y < h; y++)
                {
                    col[y + 1] += col[y];
                }
            }
            );

            return res;
        }

        public static int GetSum(int[][] integral, int x, int y, int width, int height)
        {
            return integral[x + width][y + height] + integral[x][y] - integral[x+ width][y] - integral[x][y + height];
        }

        public static int GetSumSafe(int[][] integral, int x, int y, int width, int height)
        {
            if (x + width >= integral.Length)
            {
                width = integral.Length - 1 - x;
            }

            if (y + height >= integral[0].Length)
            {
                height = integral[0].Length - 1 - y;
            }
            return integral[x + width][y + height] + integral[x][y] - integral[x + width][y] - integral[x][y + height];
        }
    }
}
