using System;
using System.Collections.Generic;

namespace BarcodeReader.Core.Datamatrix
{
	internal enum DatamatrixType
    {
        ECC200, NonECC200
    }

	internal class Configuration
    {
        // total module count in the X direction (except L and dotted L patterns, ie -2)
        public readonly int FullX;

        // total module count in the Y direction
        public readonly int FullY;

        //total module count except alignement patterns (L)
        public readonly int FullDataX, FullDataY;

        // full symbol count (not bit, symbol = byte)
        public readonly int FullSymbolCount;

        // number of data symbols
        public readonly int DataSymbolCount;

        // region count in the X direction
        public readonly int RegionCountX;

        // region count in the Y direction
        public readonly int RegionCountY;

        // modules per region in the X direction
        public readonly int SubX;

        // modules per region in the Y direction
        public readonly int SubY;

        // number of error correction symbols
        public readonly int ErrorCodeWords;

        // number of RS blocks
        public readonly int ReedSolomonBlocks;

        public readonly DatamatrixType Type;

        public Configuration(int fullX, int fullY, int rCntX, int rCntY, int errorCodeWords, int rsBlocks, DatamatrixType dmType)
        {
            FullX = fullX;
            FullY = fullY;
            RegionCountX = rCntX;
            RegionCountY = rCntY;
            ErrorCodeWords = errorCodeWords;
            ReedSolomonBlocks = rsBlocks;
            FullDataX = FullX - 2 * RegionCountX;
            FullDataY = FullY - 2 * RegionCountY;
            SubX = FullX / RegionCountX;
            SubY = FullY/RegionCountY;
            FullSymbolCount = FullDataX*FullDataY/8;
            DataSymbolCount = FullSymbolCount - ErrorCodeWords;
            Type = dmType;
        }

        // generate the indices of the data symbols for each RS block
        public int[][] GetDataBlocks()
        {
            int[][] result = new int[ReedSolomonBlocks][];
            for (int i = 0; i < ReedSolomonBlocks; ++i)
            {
                int count = (DataSymbolCount - i)/ReedSolomonBlocks;
                if ((DataSymbolCount - i)%ReedSolomonBlocks > 0)
                {
                    count++;
                }
                result[i] = new int[count];
                int nr = i;
                for (int j = 0; j < result[i].Length; ++j)
                {
                    result[i][j] = nr;
                    nr += ReedSolomonBlocks;
                }
            }

            return result;
        }

        // generate the indices of the error correction symbols for each RS block
        public int[][] GetRSBlocks()
        {
            int[][] result = new int[ReedSolomonBlocks][];
            for (int i = 0; i < ReedSolomonBlocks; ++i)
            {
                int count = (ErrorCodeWords - i) / ReedSolomonBlocks;
                if ((ErrorCodeWords - i) % ReedSolomonBlocks > 0)
                {
                    count++;
                }
                result[i] = new int[count];
                int nr = i;
                for (int j = 0; j < result[i].Length; ++j)
                {
                    result[i][j] = DataSymbolCount + nr;
                    nr += ReedSolomonBlocks;
                }
            }

            return result;
        }

        public static LinkedList<Configuration> FindConfiguration(float cols, float rows, bool isECC200)
        {
            float minDist = 1000F;
            int iCols = (int)Math.Round(cols);
            int iRows = (int)Math.Round(rows);
            int i, iMin=-1;
            for (i=0;i<AllConfigurations.Length;i++)
            {
                Configuration cfg = AllConfigurations[i];
                bool matchType = (cfg.Type == (isECC200 ? DatamatrixType.ECC200 : DatamatrixType.NonECC200));
                if (cfg.FullX == iCols && cfg.FullY == iRows && matchType) { iMin=i; minDist = 0F; break; }
                else if (matchType)
                {
                    float dist = (cols - cfg.FullX) * (cols - cfg.FullX) +
                        (rows - cfg.FullY) * (rows - cfg.FullY);
                    if (dist < minDist) {iMin=i; minDist = dist;}
                }
            }

            LinkedList<Configuration> l = new LinkedList<Configuration>();
            if (minDist < (cols + rows) * 0.2F)
            {
                l.AddLast(AllConfigurations[iMin]);
                if (iMin>0) l.AddLast(AllConfigurations[iMin-1]);
                if (iMin<AllConfigurations.Length-1) l.AddLast(AllConfigurations[iMin+1]);
                return l;
            }
            return l;
        }

        static Configuration[] AllConfigurations = new Configuration[51] {
                    new Configuration(49, 49, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(47, 47, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(45, 45, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(43, 43, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(41, 41, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(39, 39, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(37, 37, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(35, 35, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(33, 33, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(31, 31, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(29, 29, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(27, 27, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(25, 25, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(23, 23, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(21, 21, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(19, 19, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(17, 17, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(15, 15, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(13, 13, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(11,11, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(9, 9, 1, 1, 0, 0, DatamatrixType.NonECC200),
                    new Configuration(10, 10, 1, 1, 5, 1, DatamatrixType.ECC200),
                    new Configuration(12, 12, 1, 1, 7, 1, DatamatrixType.ECC200),
                    new Configuration(14, 14, 1, 1, 10, 1, DatamatrixType.ECC200),
                    new Configuration(16, 16, 1, 1, 12, 1, DatamatrixType.ECC200),
                    new Configuration(18, 18, 1, 1, 14, 1, DatamatrixType.ECC200),
                    new Configuration(20, 20, 1, 1, 18, 1, DatamatrixType.ECC200),
                    new Configuration(22, 22, 1, 1, 20, 1, DatamatrixType.ECC200),
                    new Configuration(24, 24, 1, 1, 24, 1, DatamatrixType.ECC200),
                    new Configuration(26, 26, 1, 1, 28, 1, DatamatrixType.ECC200),
                    new Configuration(32, 32, 2, 2, 36, 1, DatamatrixType.ECC200),
                    new Configuration(36, 36, 2, 2, 42, 1, DatamatrixType.ECC200),
                    new Configuration(40, 40, 2, 2, 48, 1, DatamatrixType.ECC200),
                    new Configuration(44, 44, 2, 2, 56, 1, DatamatrixType.ECC200),
                    new Configuration(48, 48, 2, 2, 68, 1, DatamatrixType.ECC200),
                    new Configuration(52, 52, 2, 2, 84, 2, DatamatrixType.ECC200),
                    new Configuration(64, 64, 4, 4, 112, 2, DatamatrixType.ECC200),
                    new Configuration(72, 72, 4, 4, 144, 4, DatamatrixType.ECC200),
                    new Configuration(80, 80, 4, 4, 192, 4, DatamatrixType.ECC200),
                    new Configuration(88, 88, 4, 4, 224, 4, DatamatrixType.ECC200),
                    new Configuration(96, 96, 4, 4, 272, 4, DatamatrixType.ECC200),
                    new Configuration(104, 104, 4, 4, 336, 6, DatamatrixType.ECC200),
                    new Configuration(120, 120, 6, 6, 408, 6, DatamatrixType.ECC200),
                    new Configuration(132, 132, 6, 6, 496, 8, DatamatrixType.ECC200),
                    new Configuration(144, 144, 6, 6, 620, 10, DatamatrixType.ECC200),
                    new Configuration(18, 8, 1, 1, 7, 1, DatamatrixType.ECC200),
                    new Configuration(32, 8, 2, 1, 11, 1, DatamatrixType.ECC200),
                    new Configuration(26, 12, 1, 1, 14, 1, DatamatrixType.ECC200),
                    new Configuration(36, 12, 2, 1, 18, 1, DatamatrixType.ECC200),
                    new Configuration(36, 16, 2, 1, 24, 1, DatamatrixType.ECC200),
                    new Configuration(48, 16, 2, 1, 28, 1, DatamatrixType.ECC200)
        };
    }
}
