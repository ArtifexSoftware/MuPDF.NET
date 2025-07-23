using System;
using System.Collections;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.QR
{
    enum ErrorCorrectionLevel
    {
        L,
        M,
        Q,
        H,
        DetectionOnly
    }

    enum MicroQRVersion
    {
        M1 = 41,
        M2 = 42,
        M3 = 43,
        M4 = 44
    }
   
    delegate bool MaskFunction(int x, int j);

    

    class QRUtils
    { 
        #region Reference data
        private static readonly int[] VersionData = new int[]
                                                       {
                                                           0x07C94, 0x085BC, 0x09A99, 0x0A4D3, 0x0BBF6, 0x0C762, 0x0D847,
                                                           0x0E60D, 0x0F928, 0x10B78, 0x1145D, 0x12A17, 0x13532, 0x149A6,
                                                           0x15683, 0x168C9, 0x177EC, 0x18EC4, 0x191E1, 0x1AFAB, 0x1B08E,
                                                           0x1CC1A, 0x1D33F, 0x1ED75, 0x1F250, 0x209D5, 0x216F0, 0x228BA,
                                                           0x2379F, 0x24B0B, 0x2542E, 0x26A64, 0x27541, 0x28C69
                                                       };

        public const Int16 FormatMask = 21522;

        public const Int16 FormatMaskMicro = 17477;

        public static readonly ErrorCorrectionLevel[] ErrorCorrectionLevels = new ErrorCorrectionLevel[]
                                                                                  {
                                                                                      ErrorCorrectionLevel.M,
                                                                                      ErrorCorrectionLevel.L,
                                                                                      ErrorCorrectionLevel.H,
                                                                                      ErrorCorrectionLevel.Q
                                                                                  };

        public static readonly ErrorCorrectionLevel[] MicroErrorCorrectionLevels = new ErrorCorrectionLevel[]
                                                                                       {
                                                                                           ErrorCorrectionLevel.DetectionOnly,
                                                                                           ErrorCorrectionLevel.L,
                                                                                           ErrorCorrectionLevel.M,
                                                                                           ErrorCorrectionLevel.L,
                                                                                           ErrorCorrectionLevel.M,
                                                                                           ErrorCorrectionLevel.L,
                                                                                           ErrorCorrectionLevel.M,
                                                                                           ErrorCorrectionLevel.Q
                                                                                       };

        public static readonly MyPoint[] MicroFormatInfoLocation = {
                                                                       new MyPoint(8, 1), new MyPoint(8, 2),
                                                                       new MyPoint(8, 3), new MyPoint(8, 4),
                                                                       new MyPoint(8, 5), new MyPoint(8, 6),
                                                                       new MyPoint(8, 7), new MyPoint(8, 8),
                                                                       new MyPoint(7, 8), new MyPoint(6, 8),
                                                                       new MyPoint(5, 8), new MyPoint(4, 8),
                                                                       new MyPoint(3, 8), new MyPoint(2, 8),
                                                                       new MyPoint(1, 8)
                                                                   };

        public static readonly MyPoint[] PrimaryFormatInfoLocation = {
                                                                          new MyPoint(8, 0), new MyPoint(8, 1),
                                                                          new MyPoint(8, 2), new MyPoint(8, 3),
                                                                          new MyPoint(8, 4), new MyPoint(8, 5),
                                                                          new MyPoint(8, 7), new MyPoint(8, 8),
                                                                          new MyPoint(7, 8), new MyPoint(5, 8),
                                                                          new MyPoint(4, 8), new MyPoint(3, 8),
                                                                          new MyPoint(2, 8), new MyPoint(1, 8),
                                                                          new MyPoint(0, 8)
                                                                      };

        public static readonly int[] FormatData = new int[]
                                                      {
                                                          0x5412, 0x5125, 0x5E7C, 0x5B4B, 0x45F9, 0x40CE, 0x4F97, 0x4AA0,
                                                          0x77C4, 0x72F3, 0x7DAA, 0x789D, 0x662F, 0x6318, 0x6C41, 0x6976,
                                                          0x1689, 0x13BE, 0x1CE7, 0x19D0, 0x0762, 0x0255, 0x0D0C, 0x083B,
                                                          0x355F, 0x3068, 0x3F31, 0x3A06, 0x24B4, 0x2183, 0x2EDA, 0x2BED
                                                      };

        public static readonly int[] FormatDataMicro = new int[]
                                                           {
                                                               0x4445, 0x4172, 0x4E2B, 0x4B1C, 0x55AE, 0x5099, 0x5FC0,
                                                               0x5AF7, 0x6793, 0x62A4, 0x6DFD, 0x68CA, 0x7678, 0x734F,
                                                               0x7C16, 0x7921, 0x06DE, 0x03E9, 0x0CB0, 0x0987, 0x1735,
                                                               0x1202, 0x1D5B, 0x186C, 0x2508, 0x203F, 0x2F66, 0x2A51,
                                                               0x34E3
                                                           };

        public static readonly int[] CodewordCount = new int[]
                                                         {
                                                             26, 44, 70, 100, 134, 172, 196, 242, 292, 346, 404, 466,
                                                             532, 581, 655, 733, 815, 901, 991, 1085, 1156, 1258, 1364,
                                                             1474, 1588, 1706, 1828, 1921, 2051, 2185, 2323, 2465,
                                                             2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706, 5, 10, 17, 24
                                                         };

        public static readonly int[][] ErrorCorrectionCodewordCount = new int[][]
                                                                    {
                                                                        new int[] {7, 10, 13, 17},
                                                                        new int[] {10, 16, 22, 28},
                                                                        new int[] {15, 26, 36, 44},
                                                                        new int[] {20, 36, 52, 64},
                                                                        new int[] {26, 48, 72, 88},
                                                                        new int[] {36, 64, 96, 112},
                                                                        new int[] {40, 72, 108, 130},
                                                                        new int[] {48, 88, 132, 156},
                                                                        new int[] {60, 110, 160, 192},
                                                                        new int[] {72, 130, 192, 224},
                                                                        new int[] {80, 150, 224, 264},
                                                                        new int[] {96, 176, 260, 308},
                                                                        new int[] {104, 198, 288, 352},
                                                                        new int[] {120, 216, 320, 384},
                                                                        new int[] {132, 240, 360, 432},
                                                                        new int[] {144, 280, 408, 480},
                                                                        new int[] {168, 308, 448, 532},
                                                                        new int[] {180, 338, 504, 588},
                                                                        new int[] {196, 364, 546, 650},
                                                                        new int[] {224, 416, 600, 700},
                                                                        new int[] {224, 442, 644, 750},
                                                                        new int[] {252, 476, 690, 816},
                                                                        new int[] {270, 504, 750, 900},
                                                                        new int[] {300, 560, 810, 960},
                                                                        new int[] {312, 588, 870, 1050},
                                                                        new int[] {336, 644, 952, 1110},
                                                                        new int[] {360, 700, 1020, 1200},
                                                                        new int[] {390, 728, 1050, 1260},
                                                                        new int[] {420, 784, 1140, 1350},
                                                                        new int[] {450, 812, 1200, 1440},
                                                                        new int[] {480, 868, 1290, 1530},
                                                                        new int[] {510, 924, 1350, 1620},
                                                                        new int[] {540, 980, 1440, 1710},
                                                                        new int[] {570, 1036, 1530, 1800},
                                                                        new int[] {570, 1064, 1590, 1890},
                                                                        new int[] {600, 1120, 1680, 1980},
                                                                        new int[] {630, 1204, 1770, 2100},
                                                                        new int[] {660, 1260, 1860, 2220},
                                                                        new int[] {720, 1316, 1950, 2310},
                                                                        new int[] {750, 1372, 2040, 2430},
                                                                        // Micro QR data
                                                                        new int[] {-1, -1, -1, 2},
                                                                        new int[] {5, 6, -1, -1},
                                                                        new int[] {6, 8, -1, -1},
                                                                        new int[] {8, 10, 14, -1}
                                                                    };

        public static readonly int[][] ErrorCorrectionBlockCount = new int[][]
                                                         {
                                                             new int[] {1, 1, 1, 1},
                                                             new int[] {1, 1, 1, 1}, new int[] {1, 1, 2, 2},
                                                             new int[] {1, 2, 2, 4}, new int[] {1, 2, 4, 4},
                                                             new int[] {2, 4, 4, 4},
                                                             new int[] {2, 4, 6, 5}, new int[] {2, 4, 6, 6},
                                                             new int[] {2, 5, 8, 8}, new int[] {4, 5, 8, 8},
                                                             new int[] {4, 5, 8, 11}, new int[] {4, 8, 10, 11},
                                                             new int[] {4, 9, 12, 16},
                                                             new int[] {4, 9, 16, 16}, new int[] {6, 10, 12, 18},
                                                             new int[] {6, 10, 17, 16}, new int[] {6, 11, 16, 19},
                                                             new int[] {6, 13, 18, 21}, new int[] {7, 14, 21, 25},
                                                             new int[] {8, 16, 20, 25},
                                                             new int[] {8, 17, 23, 25}, new int[] {9, 17, 23, 34},
                                                             new int[] {9, 18, 25, 30}, new int[] {10, 20, 27, 32},
                                                             new int[] {12, 21, 29, 35}, new int[] {12, 23, 34, 37},
                                                             new int[] {12, 25, 34, 40},
                                                             new int[] {13, 26, 35, 42}, new int[] {14, 28, 38, 45},
                                                             new int[] {15, 29, 40, 48}, new int[] {16, 31, 43, 51},
                                                             new int[] {17, 33, 45, 54}, new int[] {18, 35, 48, 57},
                                                             new int[] {19, 37, 51, 60},
                                                             new int[] {19, 38, 53, 63}, new int[] {20, 40, 56, 66},
                                                             new int[] {21, 43, 59, 70}, new int[] {22, 45, 62, 74},
                                                             new int[] {24, 47, 65, 77}, new int[] {25, 49, 68, 81},
                                                             new int[] {1, 1, 1, 1}, new int[] {1, 1, 1, 1},
                                                             new int[] {1, 1, 1, 1}, new int[] {1, 1, 1, 1}
                                                         };

        public static readonly int[] QRLocatorPattern = new int[] { 1, 1, 3, 1, 1 };

        public static readonly int[] QRAlignmentPattern = new int[] { 1, 1, 1 };

        public static readonly int[][] AlignmentPatternLocations = new int[][]
                                                                       {
                                                                           new int[] {},
                                                                           new int[] {6, 18},
                                                                           new int[] {6, 22},
                                                                           new int[] {6, 26},
                                                                           new int[] {6, 30},
                                                                           new int[] {6, 34},
                                                                           new int[] {6, 22, 38},
                                                                           new int[] {6, 24, 42},
                                                                           new int[] {6, 26, 46},
                                                                           new int[] {6, 28, 50},
                                                                           new int[] {6, 30, 54},
                                                                           new int[] {6, 32, 58},
                                                                           new int[] {6, 34, 62},
                                                                           new int[] {6, 26, 46, 66},
                                                                           new int[] {6, 26, 48, 70},
                                                                           new int[] {6, 26, 50, 74},
                                                                           new int[] {6, 30, 54, 78},
                                                                           new int[] {6, 30, 56, 82},
                                                                           new int[] {6, 30, 58, 86},
                                                                           new int[] {6, 34, 62, 90},
                                                                           new int[] {6, 28, 50, 72, 94},
                                                                           new int[] {6, 26, 50, 74, 98},
                                                                           new int[] {6, 30, 54, 78, 102},
                                                                           new int[] {6, 28, 54, 80, 106},
                                                                           new int[] {6, 32, 58, 84, 110},
                                                                           new int[] {6, 30, 58, 86, 114},
                                                                           new int[] {6, 34, 62, 90, 118},
                                                                           new int[] {6, 26, 50, 74, 98, 122},
                                                                           new int[] {6, 30, 54, 78, 102, 126},
                                                                           new int[] {6, 26, 52, 78, 104, 130},
                                                                           new int[] {6, 30, 56, 82, 108, 134},
                                                                           new int[] {6, 34, 60, 86, 112, 138},
                                                                           new int[] {6, 30, 58, 86, 114, 142},
                                                                           new int[] {6, 34, 62, 90, 118, 146},
                                                                           new int[] {6, 30, 54, 78, 102, 126, 150},
                                                                           new int[] {6, 24, 50, 76, 102, 128, 154},
                                                                           new int[] {6, 28, 54, 80, 106, 132, 158},
                                                                           new int[] {6, 32, 58, 84, 110, 136, 162},
                                                                           new int[] {6, 26, 54, 82, 110, 138, 166},
                                                                           new int[] {6, 30, 58, 86, 114, 142, 170}
                                                                       };

        public static readonly MaskFunction[] Masks = {
                                                          new MaskFunction(Mask000), new MaskFunction(Mask001),
                                                          new MaskFunction(Mask010), new MaskFunction(Mask011),
                                                          new MaskFunction(Mask100), new MaskFunction(Mask101),
                                                          new MaskFunction(Mask110), new MaskFunction(Mask111)
                                                      };

        public static readonly MaskFunction[] MicroMasks = {
                                                          new MaskFunction(Mask001), new MaskFunction(Mask100),
                                                          new MaskFunction(Mask110), new MaskFunction(Mask111)
                                                      };

        private static bool Mask000(int x, int y)
        {
            return (y + x) % 2 == 0;
        }

        private static bool Mask001(int x, int y)
        {
            return y % 2 == 0;
        }

        private static bool Mask010(int x, int y)
        {
            return x % 3 == 0;
        }

        private static bool Mask011(int x, int y)
        {
            return (y + x) % 3 == 0;
        }

        private static bool Mask100(int x, int y)
        {
            return (y / 2 + x / 3) % 2 == 0;
        }

        private static bool Mask101(int x, int y)
        {
            return y * x % 2 + y * x % 3 == 0;
        }

        private static bool Mask110(int x, int y)
        {
            return (y * x % 2 + y * x % 3) % 2 == 0;
        }

        private static bool Mask111(int x, int y)
        {
            return ((y + x) % 2 + y * x % 3) % 2 == 0;
        }

        #endregion
        public static int ExtractSymbolVersion(ImageScaner scan, QRLocation location)
        {
            float nominalWur = location.Wur / 7.0F;
            float nominalHur = location.Hur / 7.0F;
            int versionValue = ExtractVersionBits(scan, location.UpperRight, location.LeftNormal * nominalWur,
                                                  location.DownNormal * nominalHur, false);
            int distance;
            int index = FindClosestMatchIndex(VersionData, versionValue, out distance);
            if (distance < 4)
            {
                return index + 7;
            }

            versionValue = ExtractVersionBits(scan, location.BottomLeft, location.DownNormal * nominalWur,
                                              location.LeftNormal * nominalWur, false);
            index = FindClosestMatchIndex(VersionData, versionValue, out distance);
            if (distance < 4)
            {
                return index + 7;
            }

#if QR_SUPPORT_NONSTANDARD
            versionValue = ExtractVersionBits(scan, location.UpperRight, location.LeftNormal * nominalWur ,
                                              location.DownNormal * nominalHur, true);
            index = FindClosestMatchIndex(VersionData, versionValue, out distance);
            if (distance < 4)
            {
                return index + 7;
            }
            versionValue = ExtractVersionBits(scan, location.BottomLeft, location.DownNormal * nominalWur,
                                              location.LeftNormal * nominalWur,true);
            index = FindClosestMatchIndex(VersionData, versionValue, out distance);
            if (distance < 4)
            {
                return index + 7;
            }
#endif
            return -1;
        }

        private static int ExtractVersionBits(ImageScaner scan, MyPointF center, MyVectorF perpendicularNorm, MyVectorF parallelNorm, bool alternativeDirection)
        {
            Grid grid = new Grid(center, parallelNorm, perpendicularNorm);

            bool[][] versionBits = new bool[3][];
            for (int i = 0; i < 3; ++i)
            {
                versionBits[i] = new bool[6];
            }

            grid.ExtractPointsRegular(scan, versionBits, new MyPoint(-3,-7), new MyPoint(0,0),6,3);
            int bitIndex = alternativeDirection ? 17 : 0;
            int version = 0;
            if (alternativeDirection)
            {
                for (int y = 0; y < 3; ++y)
                {
                    for (int x = 0; x < 6; ++x, --bitIndex)
                    {
                        if (versionBits[y][x])
                        {
                            version |= (1 << bitIndex);
                        }
                    }
                }
            }
            else
            {
                for (int x = 0; x < 6; ++x)
                {
                    for (int y = 0; y < 3; ++y, ++bitIndex)
                    {
                        if (versionBits[y][x])
                        {
                            version |= (1 << bitIndex);
                        }
                    }
                }
            }

            return version;
        }

        public static int FindClosestMatchIndex(int[] sourceArray, int value, out int distance)
        {
            distance = int.MaxValue;
            int result = 0;
            for (int i = 0; i < sourceArray.Length; ++i)
            {
                int d = HammingDistance(value, sourceArray[i]);
                if (d <= distance)
                {
                    distance = d;
                    result = i;
                }
            }

            return result;
        }

        public static int HammingDistance(int value1, int value2)
        {
            int hammingDistance = 0;
            int difference = value1 ^ value2;
            for (int i = 0; i < 32; ++i)
            {
                if ((difference & (1 << i)) != 0)
                {
                    hammingDistance++;
                }
            }

            return hammingDistance;
        }

        public static int BitSliceValue(BitArray dataStream, ref int index, int length)
        {
            int value = 0;
            for (int i = length - 1; i >= 0; --i, ++index)
            {
                if (index < dataStream.Count && dataStream[index]) //TODO index<dataStream.Count added to avoid crash
                {
                    value |= (1 << i);
                }
            }

            return value;
        }

        public static bool IsStreamEnd(BitArray dataStream, int index, int version)
        {
            int zeros = version <= 40 ? 4 : 3 + (version - 41) * 2;
            zeros = Math.Min(dataStream.Length - index, zeros);

            int dummyIndex = index;
            if (BitSliceValue(dataStream, ref dummyIndex, zeros) == 0)
            {
                return true;
            }

            return false;
        }
    }

    class CodeWalker
    {
        private readonly MyVector[][] transitions = new MyVector[][]
                                               {
                                                   new MyVector[] {new MyVector(-1, 0), new MyVector(1, -1),},
                                                   new MyVector[] {new MyVector(-1, 0), new MyVector(1, 1),}
                                               };

        private int transitionMode = 0;

        private int transitionStep = 0;

        private readonly int codeSize;

        private readonly bool[][] exclusionMask;

        private MyPoint position;

        private readonly bool isMicroCode;

        public CodeWalker(int size, bool[][] eMask, bool isMicro)
        {
            codeSize = size;
            exclusionMask = eMask;
            position = new MyPoint(codeSize - 1, codeSize - 1);
            isMicroCode = isMicro;
        }

        public MyPoint NextFreePosition()
        {
            MyPoint nextPoint = position;

            do
            {
                MoveToNextPosition();
            } while ((position.X >= 0 && position.Y >= 0) && exclusionMask[position.Y][position.X]);

            return nextPoint;
        }

        private void MoveToNextPosition()
        {
            MyPoint nextPosition = position + transitions[transitionMode][transitionStep];
            transitionStep = 1 - transitionStep;
            if (nextPosition.Y < 0 || nextPosition.Y == codeSize)
            {
                position.X--;
                if (position.X == 6 && !isMicroCode) // this is because of the silly vertical timing pattern ruining the otherwise simple transition logic.
                {
                    position.X--;
                }
                transitionMode = 1 - transitionMode;
            }
            else
            {
                position = nextPosition;
            }
        }
    }
}
