using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Aztec
{
    internal enum AztecType
    {
        FullRange,
        Compact,
        Rune,
        Undef
    }

    // stores & manages information about the actual logical aztec symbol
    class AztecSymbol
    {
        public readonly int SideModuleCount;

        public readonly int DataCodewordCount;

        public readonly int ModeWord;

        public readonly int BitCount;

        public readonly int RsWordWidth;

        public readonly int LayerCount;

        public readonly AztecType Type;

        public readonly Grid[][] Grids;

        // for the final module read step
        public readonly MyPoint[][] StartPoints;

        public readonly bool[][] Bitarray;

        public float Confidence=0F;

        // construct the object based on the given info (load up reference data)
        private AztecSymbol(AztecType type, int dataCodewordCount, int layerCount, int modeWord, float confidence)
        {
            DataCodewordCount = dataCodewordCount;
            ModeWord = modeWord;
            LayerCount = layerCount;
            Type = type;
            int gridCount;

            switch (Type)
            {
                case AztecType.FullRange:
                    RsWordWidth = AztecUtils.FullRangeBitCount[LayerCount];
                    BitCount = AztecUtils.FullRangeCapacities[LayerCount];
                    SideModuleCount = AztecUtils.FullRangeSizes[LayerCount];
                    gridCount = AztecUtils.FullRangeGridCount[LayerCount];
                    break;
                case AztecType.Compact:
                    RsWordWidth = AztecUtils.CompactBitCount[LayerCount];
                    BitCount = AztecUtils.CompactCapacities[LayerCount];
                    SideModuleCount = AztecUtils.CompactSizes[LayerCount];
                    gridCount = 1;
                    break;
                default:
                    gridCount = 1;
                    break;
            }

            Grids = new Grid[gridCount][];
            StartPoints = new MyPoint[gridCount][];
            for (int i = 0; i < gridCount; ++i)
            {
                Grids[i] = new Grid[gridCount];
                StartPoints[i] = new MyPoint[gridCount];
            }

            Bitarray = new bool[SideModuleCount][];
            for (int i = 0; i < SideModuleCount; ++i)
            {
                Bitarray[i] = new bool[SideModuleCount];
            }
        }

        // point has coordinates > 0
        public bool IsFinderBit(MyPoint point)
        {
            if (Type != AztecType.FullRange)
            {
                return false;
            }
            return (point.X - SideModuleCount / 2) % 16 == 0 || (point.Y - SideModuleCount / 2) % 16 == 0;
        }

        // gets the grid's starting point at x,y
        public MyPoint GetGridStart(int x, int y)
        {
            MyPoint start = new MyPoint(0, 0);
            int gridCountHalf = Grids.Length / 2;

            if (x < gridCountHalf)
            {
                ++x;
            }

            if (y < gridCountHalf)
            {
                ++y;
            }

            start.X += (x - gridCountHalf) * 16;
            start.Y += (y - gridCountHalf) * 16;

            return start;
        }

        // returns the grid that gives the most accurate location for the given coordinates
        public Grid GetGridForCoordinate(int x, int y)
        {
            if (Grids.Length == 1)
            {
                return Grids[0][0];
            }

            int gridHalf = SideModuleCount / 2;
            int gridCountHalf = Grids.Length / 2;

            int xGrid = (x - gridHalf);
            xGrid = xGrid < 0 ? xGrid / 16 - 1 : xGrid / 16;
            xGrid += gridCountHalf;
            if (xGrid < 0)
            {
                xGrid = 0;
            }
            if (xGrid == Grids.Length)
            {
                --xGrid;
            }

            int yGrid = (y - gridHalf);
            yGrid = yGrid < 0 ? yGrid / 16 - 1 : yGrid / 16;
            yGrid += gridCountHalf;
            if (yGrid < 0)
            {
                yGrid = 0;
            }
            if (yGrid == Grids.Length)
            {
                --yGrid;
            }

            return Grids[yGrid][xGrid];
        }

        // reads the image, assuming that we have a full-range symbol
        public static AztecSymbol GetFullRangeSymbol(bool[][] centralPoints, AztecFinder finder)
        {
            AztecOrientation orientation = GetOrientation(centralPoints, 15);
            if (!orientation.IsValid)
            {
                return null;
            }

            //rotate finder
            orientation.Rotate(finder);


            // read the mode words
            int[] modeWords = ReadModeWords(orientation, centralPoints, 11, true);
            ReedSolomon modeCorrector = new ReedSolomon(modeWords, 6, 4, AztecUtils.Polynoms[4], 1);
            float confidence;
            modeCorrector.Correct(out confidence);

            if (!modeCorrector.CorrectionSucceeded)
            {
                return null;
            }

            modeWords = modeCorrector.CorrectedData;
            ushort modeValue = (ushort)(modeWords[0] * 4096 + modeWords[1] * 256 + modeWords[2] * 16 + modeWords[3]);
            int dataCodewordCount = (modeValue & 0x7FF) + 1;
            int layerCount = (modeValue >> 11) + 1;

            return new AztecSymbol(AztecType.FullRange, dataCodewordCount, layerCount, modeValue, confidence);
        }

        // reads the image, assuming that we have a compact symbol
        public static AztecSymbol GetCompactSymbol(bool[][] centralPoints, AztecFinder finder)
        {
            AztecOrientation orientation = GetOrientation(centralPoints, 11);
            if (!orientation.IsValid)
            {
                return null;
            }

            // read the mode words
            int[] modeWords = ReadModeWords(orientation, centralPoints, 7, false);

	    //rotate finder
            orientation.Rotate(finder);

            ReedSolomon modeCorrector = new ReedSolomon(modeWords, 5, 4, AztecUtils.Polynoms[4], 1);
            float confidence;
            modeCorrector.Correct(out confidence);

            if (!modeCorrector.CorrectionSucceeded)
            {
                // check for runes
                for (int i = 0; i < modeWords.Length; i++)
                {
                    modeWords[i] ^= 0x0A;
                }
                modeCorrector = new ReedSolomon(modeWords, 5, 4, AztecUtils.Polynoms[4], 1);
                modeCorrector.Correct(out confidence);
                if (!modeCorrector.CorrectionSucceeded)
                {
                    return null;
                }

                // we have an Aztec rune!
                byte runeMessage = (byte)(modeWords[0] * 16 + modeWords[1]);
                return new AztecSymbol(AztecType.Rune, 1, 0, runeMessage, confidence );
            }

            modeWords = modeCorrector.CorrectedData;
            byte modeValue = (byte)(modeWords[0] * 16 + modeWords[1]);
            int dataCodewordCount = (modeValue & 0x3F) + 1;
            int layerCount = (modeValue >> 6) + 1;

            return new AztecSymbol(AztecType.Compact, dataCodewordCount, layerCount, modeValue, confidence);
        }

        // sets up an orientation object based on bit data
        private static AztecOrientation GetOrientation(bool[][] centralPoints, int gridSize)
        {
            AztecOrientation orientation = new AztecOrientation();
            orientation.AddPattern(new bool[] { centralPoints[0][1], centralPoints[0][0], centralPoints[1][0] },
                                   new MyPoint(0, 0));
            orientation.AddPattern(
                new bool[] { centralPoints[gridSize - 2][0], centralPoints[gridSize - 1][0], centralPoints[gridSize - 1][1] },
                new MyPoint(0, gridSize - 1));
            orientation.AddPattern(
                new bool[]
                    {
                        centralPoints[gridSize - 1][gridSize - 2], centralPoints[gridSize - 1][gridSize - 1],
                        centralPoints[gridSize - 2][gridSize - 1]
                    }, new MyPoint(gridSize - 1, gridSize - 1));
            orientation.AddPattern(
                new bool[] { centralPoints[1][gridSize - 1], centralPoints[0][gridSize - 1], centralPoints[0][gridSize - 2] },
                new MyPoint(gridSize - 1, 0));

            return orientation;
        }

        // reads the mode words from the central bit matrix
        private static int[] ReadModeWords(AztecOrientation orientation, bool[][] centralPoints, int sideLength, bool hasFinder)
        {
            int j = 4 * (sideLength - (hasFinder ? 1 : 0));
            bool[] modeBits = new bool[j--];
            MyVector sideDir = orientation.GetModeScanVector();
            MyPoint nextBit = orientation.StartPoint + sideDir;
            for (int s = 0; s < 4; ++s)
            {
                for (int b = 0; b < sideLength; ++b)
                {
                    nextBit = nextBit + sideDir;
                    if (hasFinder && b == sideLength / 2)
                    {
                        continue;
                    }
                    modeBits[j--] = centralPoints[nextBit.Y][nextBit.X];
                }
                nextBit = nextBit + 2 * sideDir;
                sideDir = orientation.RotateClockwise(sideDir);
                nextBit = nextBit + sideDir;
            }

            int[] modeWords = new int[modeBits.Length / 4];
            for (int i = modeWords.Length - 1, b = 0; i >= 0; --i)
            {
                for (int m = 0; m < 4; ++m, ++b)
                {
                    if (modeBits[b])
                    {
                        modeWords[i] |= (1 << m);
                    }
                }
            }

            return modeWords;
        }
    }
}
