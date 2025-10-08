/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// QR Code specification in convenient format. The following data / specifications 
    /// are taken from
    /// "Two dimensional symbol -- QR-code -- Basic Specification" (JIS X0510:2004)
    /// or
    /// "Automatic identification and data capture techniques -- QR Code 2005 bar code symbology specification" (ISO/IEC 18004:2006)
    /// </summary>
    class QRSpec
    {
        /// <summary>
        /// Maximum version (size) of QR-code symbol.
        /// </summary>
        public const int MaximumSymbolVersion = 40;

        /// <summary>
        /// Maximum width of a symbol
        /// </summary>
        public const int MaximumSymbolWidth = 177;

        /// <summary>
        /// Cache of initial frames.
        /// </summary>
        private static byte[][] frames = new byte[][] 
        {
            null, null, null, null, null, null, null, null, null, null, 
            null, null, null, null, null, null, null, null, null, null, 
            null, null, null, null, null, null, null, null, null, null, 
            null, null, null, null, null, null, null, null, null, null, null
        };

        /// <summary>
        /// Table of the capacity of symbols.
        /// See Table 1 (pp.13) and Table 12-16 (pp.30-36), JIS X0510:2004.
        /// </summary>
        private struct SymbolCapacity
        {
            /// <summary>
            /// Edge length of the symbol
            /// </summary>
            public int width;

            /// <summary>
            /// Data capacity (bytes)
            /// </summary>
            public int words;

            /// <summary>
            /// Remainder bits
            /// </summary>
            public int remainder;

            /// <summary>
            /// Number of ECC code (bytes)
            /// </summary>
            public int[] ec;

            public SymbolCapacity(int width, int words, int remainder, int[] ec)
            {
                this.width = width;
                this.words = words;
                this.remainder = remainder;
                this.ec = ec;
            }
        };

        private static SymbolCapacity[] symbolCapacity = {
            new SymbolCapacity( 0,     0, 0, new int[] {  0,    0,    0,    0}),
            new SymbolCapacity(21,    26, 0, new int[] {  7,   10,   13,   17}), // 1
            new SymbolCapacity(25,    44, 7, new int[] { 10,   16,   22,   28}),
            new SymbolCapacity(29,    70, 7, new int[] { 15,   26,   36,   44}),
            new SymbolCapacity(33,   100, 7, new int[] { 20,   36,   52,   64}),
            new SymbolCapacity(37,   134, 7, new int[] { 26,   48,   72,   88}), // 5
            new SymbolCapacity(41,   172, 7, new int[] { 36,   64,   96,  112}),
            new SymbolCapacity(45,   196, 0, new int[] { 40,   72,  108,  130}),
            new SymbolCapacity(49,   242, 0, new int[] { 48,   88,  132,  156}),
            new SymbolCapacity(53,   292, 0, new int[] { 60,  110,  160,  192}),
            new SymbolCapacity(57,   346, 0, new int[] { 72,  130,  192,  224}), //10
            new SymbolCapacity(61,   404, 0, new int[] { 80,  150,  224,  264}),
            new SymbolCapacity(65,   466, 0, new int[] { 96,  176,  260,  308}),
            new SymbolCapacity(69,   532, 0, new int[] {104,  198,  288,  352}),
            new SymbolCapacity(73,   581, 3, new int[] {120,  216,  320,  384}),
            new SymbolCapacity(77,   655, 3, new int[] {132,  240,  360,  432}), //15
            new SymbolCapacity(81,   733, 3, new int[] {144,  280,  408,  480}),
            new SymbolCapacity(85,   815, 3, new int[] {168,  308,  448,  532}),
            new SymbolCapacity(89,   901, 3, new int[] {180,  338,  504,  588}),
            new SymbolCapacity(93,   991, 3, new int[] {196,  364,  546,  650}),
            new SymbolCapacity(97,  1085, 3, new int[] {224,  416,  600,  700}), //20
            new SymbolCapacity(101, 1156, 4, new int[] {224,  442,  644,  750}),
            new SymbolCapacity(105, 1258, 4, new int[] {252,  476,  690,  816}),
            new SymbolCapacity(109, 1364, 4, new int[] {270,  504,  750,  900}),
            new SymbolCapacity(113, 1474, 4, new int[] {300,  560,  810,  960}),
            new SymbolCapacity(117, 1588, 4, new int[] {312,  588,  870, 1050}), //25
            new SymbolCapacity(121, 1706, 4, new int[] {336,  644,  952, 1110}),
            new SymbolCapacity(125, 1828, 4, new int[] {360,  700, 1020, 1200}),
            new SymbolCapacity(129, 1921, 3, new int[] {390,  728, 1050, 1260}),
            new SymbolCapacity(133, 2051, 3, new int[] {420,  784, 1140, 1350}),
            new SymbolCapacity(137, 2185, 3, new int[] {450,  812, 1200, 1440}), //30
            new SymbolCapacity(141, 2323, 3, new int[] {480,  868, 1290, 1530}),
            new SymbolCapacity(145, 2465, 3, new int[] {510,  924, 1350, 1620}),
            new SymbolCapacity(149, 2611, 3, new int[] {540,  980, 1440, 1710}),
            new SymbolCapacity(153, 2761, 3, new int[] {570, 1036, 1530, 1800}),
            new SymbolCapacity(157, 2876, 0, new int[] {570, 1064, 1590, 1890}), //35
            new SymbolCapacity(161, 3034, 0, new int[] {600, 1120, 1680, 1980}),
            new SymbolCapacity(165, 3196, 0, new int[] {630, 1204, 1770, 2100}),
            new SymbolCapacity(169, 3362, 0, new int[] {660, 1260, 1860, 2220}),
            new SymbolCapacity(173, 3532, 0, new int[] {720, 1316, 1950, 2310}),
            new SymbolCapacity(177, 3706, 0, new int[] {750, 1372, 2040, 2430}) //40
        };

        private static int[][] lengthIndicatorTable = 
        {
            new int[] {10, 12, 14},
            new int[] { 9, 11, 13},
            new int[] { 8, 16, 16},
            new int[] { 8, 10, 12}
        };

        /// <summary>
        /// Table of the error correction code (Reed-Solomon block)
        /// See Table 12-16 (pp.30-36), JIS X0510:2004.
        /// </summary>
        private static int[][][] eccTable = {
            new int[][] { new int[] { 0,  0}, new int[] { 0,  0}, new int[] { 0,  0}, new int[] { 0,  0}},
            new int[][] { new int[] { 1,  0}, new int[] { 1,  0}, new int[] { 1,  0}, new int[] { 1,  0}}, // 1
            new int[][] { new int[] { 1,  0}, new int[] { 1,  0}, new int[] { 1,  0}, new int[] { 1,  0}},
            new int[][] { new int[] { 1,  0}, new int[] { 1,  0}, new int[] { 2,  0}, new int[] { 2,  0}},
            new int[][] { new int[] { 1,  0}, new int[] { 2,  0}, new int[] { 2,  0}, new int[] { 4,  0}},
            new int[][] { new int[] { 1,  0}, new int[] { 2,  0}, new int[] { 2,  2}, new int[] { 2,  2}}, // 5
            new int[][] { new int[] { 2,  0}, new int[] { 4,  0}, new int[] { 4,  0}, new int[] { 4,  0}},
            new int[][] { new int[] { 2,  0}, new int[] { 4,  0}, new int[] { 2,  4}, new int[] { 4,  1}},
            new int[][] { new int[] { 2,  0}, new int[] { 2,  2}, new int[] { 4,  2}, new int[] { 4,  2}},
            new int[][] { new int[] { 2,  0}, new int[] { 3,  2}, new int[] { 4,  4}, new int[] { 4,  4}},
            new int[][] { new int[] { 2,  2}, new int[] { 4,  1}, new int[] { 6,  2}, new int[] { 6,  2}}, //10
            new int[][] { new int[] { 4,  0}, new int[] { 1,  4}, new int[] { 4,  4}, new int[] { 3,  8}},
            new int[][] { new int[] { 2,  2}, new int[] { 6,  2}, new int[] { 4,  6}, new int[] { 7,  4}},
            new int[][] { new int[] { 4,  0}, new int[] { 8,  1}, new int[] { 8,  4}, new int[] {12,  4}},
            new int[][] { new int[] { 3,  1}, new int[] { 4,  5}, new int[] {11,  5}, new int[] {11,  5}},
            new int[][] { new int[] { 5,  1}, new int[] { 5,  5}, new int[] { 5,  7}, new int[] {11,  7}}, //15
            new int[][] { new int[] { 5,  1}, new int[] { 7,  3}, new int[] {15,  2}, new int[] { 3, 13}},
            new int[][] { new int[] { 1,  5}, new int[] {10,  1}, new int[] { 1, 15}, new int[] { 2, 17}},
            new int[][] { new int[] { 5,  1}, new int[] { 9,  4}, new int[] {17,  1}, new int[] { 2, 19}},
            new int[][] { new int[] { 3,  4}, new int[] { 3, 11}, new int[] {17,  4}, new int[] { 9, 16}},
            new int[][] { new int[] { 3,  5}, new int[] { 3, 13}, new int[] {15,  5}, new int[] {15, 10}}, //20
            new int[][] { new int[] { 4,  4}, new int[] {17,  0}, new int[] {17,  6}, new int[] {19,  6}},
            new int[][] { new int[] { 2,  7}, new int[] {17,  0}, new int[] { 7, 16}, new int[] {34,  0}},
            new int[][] { new int[] { 4,  5}, new int[] { 4, 14}, new int[] {11, 14}, new int[] {16, 14}},
            new int[][] { new int[] { 6,  4}, new int[] { 6, 14}, new int[] {11, 16}, new int[] {30,  2}},
            new int[][] { new int[] { 8,  4}, new int[] { 8, 13}, new int[] { 7, 22}, new int[] {22, 13}}, //25
            new int[][] { new int[] {10,  2}, new int[] {19,  4}, new int[] {28,  6}, new int[] {33,  4}},
            new int[][] { new int[] { 8,  4}, new int[] {22,  3}, new int[] { 8, 26}, new int[] {12, 28}},
            new int[][] { new int[] { 3, 10}, new int[] { 3, 23}, new int[] { 4, 31}, new int[] {11, 31}},
            new int[][] { new int[] { 7,  7}, new int[] {21,  7}, new int[] { 1, 37}, new int[] {19, 26}},
            new int[][] { new int[] { 5, 10}, new int[] {19, 10}, new int[] {15, 25}, new int[] {23, 25}}, //30
            new int[][] { new int[] {13,  3}, new int[] { 2, 29}, new int[] {42,  1}, new int[] {23, 28}},
            new int[][] { new int[] {17,  0}, new int[] {10, 23}, new int[] {10, 35}, new int[] {19, 35}},
            new int[][] { new int[] {17,  1}, new int[] {14, 21}, new int[] {29, 19}, new int[] {11, 46}},
            new int[][] { new int[] {13,  6}, new int[] {14, 23}, new int[] {44,  7}, new int[] {59,  1}},
            new int[][] { new int[] {12,  7}, new int[] {12, 26}, new int[] {39, 14}, new int[] {22, 41}}, //35
            new int[][] { new int[] { 6, 14}, new int[] { 6, 34}, new int[] {46, 10}, new int[] { 2, 64}},
            new int[][] { new int[] {17,  4}, new int[] {29, 14}, new int[] {49, 10}, new int[] {24, 46}},
            new int[][] { new int[] { 4, 18}, new int[] {13, 32}, new int[] {48, 14}, new int[] {42, 32}},
            new int[][] { new int[] {20,  4}, new int[] {40,  7}, new int[] {43, 22}, new int[] {10, 67}},
            new int[][] { new int[] {19,  6}, new int[] {18, 31}, new int[] {34, 34}, new int[] {20, 61}},//40
        };

        /// <summary>
        /// Positions of alignment patterns.
        /// This array includes only the second and the third position of 
        /// the alignment patterns. Rest of them can be calculated from the 
        /// distance between them.
        /// 
        /// See Table 1 in Appendix E (pp.71) of JIS X0510:2004.
        /// </summary>
        private static int[][] alignmentPatternPos = 
        {
            new int[] { 0,  0},
            new int[] { 0,  0}, new int[] {18,  0}, new int[] {22,  0}, new int[] {26,  0}, new int[] {30,  0}, // 1- 5
            new int[] {34,  0}, new int[] {22, 38}, new int[] {24, 42}, new int[] {26, 46}, new int[] {28, 50}, // 6-10
            new int[] {30, 54}, new int[] {32, 58}, new int[] {34, 62}, new int[] {26, 46}, new int[] {26, 48}, //11-15
            new int[] {26, 50}, new int[] {30, 54}, new int[] {30, 56}, new int[] {30, 58}, new int[] {34, 62}, //16-20
            new int[] {28, 50}, new int[] {26, 50}, new int[] {30, 54}, new int[] {28, 54}, new int[] {32, 58}, //21-25
            new int[] {30, 58}, new int[] {34, 62}, new int[] {26, 50}, new int[] {30, 54}, new int[] {26, 52}, //26-30
            new int[] {30, 56}, new int[] {34, 60}, new int[] {30, 58}, new int[] {34, 62}, new int[] {30, 54}, //31-35
            new int[] {24, 50}, new int[] {28, 54}, new int[] {32, 58}, new int[] {26, 54}, new int[] {30, 58}, //35-40
        };

        /// <summary>
        /// Version information pattern (BCH coded).
        /// See Table 1 in Appendix D (pp.68) of JIS X0510:2004.
        /// </summary>
        private static int[] versionPattern = 
        {
            0x07c94, 0x085bc, 0x09a99, 0x0a4d3, 0x0bbf6, 0x0c762, 0x0d847, 0x0e60d,
            0x0f928, 0x10b78, 0x1145d, 0x12a17, 0x13532, 0x149a6, 0x15683, 0x168c9,
            0x177ec, 0x18ec4, 0x191e1, 0x1afab, 0x1b08e, 0x1cc1a, 0x1d33f, 0x1ed75,
            0x1f250, 0x209d5, 0x216f0, 0x228ba, 0x2379f, 0x24b0b, 0x2542e, 0x26a64,
            0x27541, 0x28c69
        };

        private static int[][] formatInfo = 
        {
            new int[] {0x77c4, 0x72f3, 0x7daa, 0x789d, 0x662f, 0x6318, 0x6c41, 0x6976},
            new int[] {0x5412, 0x5125, 0x5e7c, 0x5b4b, 0x45f9, 0x40ce, 0x4f97, 0x4aa0},
            new int[] {0x355f, 0x3068, 0x3f31, 0x3a06, 0x24b4, 0x2183, 0x2eda, 0x2bed},
            new int[] {0x1689, 0x13be, 0x1ce7, 0x19d0, 0x0762, 0x0255, 0x0d0c, 0x083b}
        };

        /// <summary>
        /// Array of positions of alignment patterns. X and Y coordinates 
        /// are interleaved into 'pos'.
        /// </summary>
        private class AlignmentPatternPosition
        {
            /// <summary>
            /// Number of patterns
            /// </summary>
            public int n;
            public int[] pos;
        };

        /// <summary>
        /// Gets maximum data code length (bytes) for the version.
        /// </summary>
        public static int GetDataLength(int version, QRErrorCorrectionLevel level)
        {
            return symbolCapacity[version].words - symbolCapacity[version].ec[(int)level];
        }

        /// <summary>
        /// Gets maximum error correction code length (bytes) for the version.
        /// </summary>
        private static int getECCLength(int version, QRErrorCorrectionLevel level)
        {
            return symbolCapacity[version].ec[(int)level];
        }

        /// <summary>
        /// Gets a version number that satisfies the input code length.
        /// </summary>
        public static int GetMinimumVersion(int size, QRErrorCorrectionLevel level)
        {
            for (int i = 1; i <= MaximumSymbolVersion; i++)
            {
                int words = symbolCapacity[i].words - symbolCapacity[i].ec[(int)level];
                if (words >= size)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Gets the width of the symbol for the version.
        /// </summary>
        public static int GetWidth(int version)
        {
            return symbolCapacity[version].width;
        }

        /// <summary>
        /// Gets the number of remainder bits.
        /// </summary>
        public static int GetRemainderLength(int version)
        {
            return symbolCapacity[version].remainder;
        }

        /// <summary>
        /// Gets the size of length indicator for the mode and version.
        /// </summary>
        public static int GetLengthIndicator(QREncodeMode mode, int version)
        {
            int l = 2;

            if (version <= 9)
                l = 0;
            else if (version <= 26)
                l = 1;

            return lengthIndicatorTable[(int)mode][l];
        }

        /// <summary>
        /// Gets the maximum length for the mode and version.
        /// </summary>
        public static int GetMaximumWords(QREncodeMode mode, int version)
        {
            int l = 2;
            if (version <= 9)
                l = 0;
            else if (version <= 26)
                l = 1;

            int bits = lengthIndicatorTable[(int)mode][l];
            int words = (1 << bits) - 1;
            if (mode == QREncodeMode.Kanji)
                words *= 2;

            return words;
        }

        /// <summary>
        /// Gets an array of ECC specification.
        /// an array of ECC specification contains as following:
        /// # of type1 blocks, # of data code, # of ecc code, 
        /// # of type2 blocks, # of data code, # of ecc code
        /// </summary>
        public static int[] GetEccSpec(int version, QRErrorCorrectionLevel level)
        {
            int b1 = eccTable[version][(int)level][0];
            int b2 = eccTable[version][(int)level][1];
            int data = QRSpec.GetDataLength(version, level);
            int ecc = QRSpec.getECCLength(version, level);

            int[] array = new int[6];

            if (b2 == 0)
            {
                array[0] = b1;
                array[1] = data / b1;
                array[2] = ecc / b1;
                array[3] = 0;
                array[4] = 0;
                array[5] = 0;
            }
            else
            {
                array[0] = b1;
                array[1] = data / (b1 + b2);
                array[2] = ecc / (b1 + b2);
                array[3] = b2;
                array[4] = array[1] + 1;
                array[5] = (ecc - (array[2] * b1)) / b2;
            }

            return array;
        }

        public static int rsBlockNum(int[] spec)
        {
            return spec[0] + spec[3];
        }

        public static int rsBlockNum1(int[] spec)
        {
            return spec[0];
        }

        public static int rsDataCodes1(int[] spec)
        {
            return spec[1];
        }

        public static int rsEccCodes1(int[] spec)
        {
            return spec[2];
        }

        public static int rsBlockNum2(int[] spec)
        {
            return spec[3];
        }

        public static int rsDataCodes2(int[] spec)
        {
            return spec[4];
        }

        public static int rsEccCodes2(int[] spec)
        {
            return spec[5];
        }

        /// <summary>
        /// Gets positions of alignment patterns.
        /// </summary>
        private static AlignmentPatternPosition getAlignmentPattern(int version)
        {
            if (version < 2)
                return null;

            AlignmentPatternPosition al = new AlignmentPatternPosition();

            int width = symbolCapacity[version].width;
            int d = alignmentPatternPos[version][1] - alignmentPatternPos[version][0];

            int w = 2;
            if (d >= 0)
                w = (width - alignmentPatternPos[version][0]) / d + 2;

            al.n = w * w - 3;
            al.pos = new int[al.n * 2];

            if (al.n == 1)
            {
                al.pos[0] = alignmentPatternPos[version][0];
                al.pos[1] = alignmentPatternPos[version][0];
                return al;
            }

            int posIndex = 0;
            int cx = alignmentPatternPos[version][0];
            for (int x = 1; x < w - 1; x++)
            {
                al.pos[posIndex] = 6;
                al.pos[posIndex + 1] = cx;
                al.pos[posIndex + 2] = cx;
                al.pos[posIndex + 3] = 6;
                cx += d;
                posIndex += 4;
            }

            int cy = alignmentPatternPos[version][0];
            for (int y = 0; y < w - 1; y++)
            {
                cx = alignmentPatternPos[version][0];
                for (int x = 0; x < w - 1; x++)
                {
                    al.pos[posIndex] = cx;
                    al.pos[posIndex + 1] = cy;
                    cx += d;
                    posIndex += 2;
                }

                cy += d;
            }

            return al;
        }

        /// <summary>
        /// Gets BCH encoded version information pattern that is used for 
        /// the symbol of version 7 or greater. Use lower 18 bits.
        /// </summary>
        private static int getVersionPattern(int version)
        {
            if (version < 7 || version > MaximumSymbolVersion)
                return 0;

            return versionPattern[version - 7];
        }

        /// <summary>
        /// Gets BCH encoded format information pattern.
        /// </summary>
        public static int GetFormatInfo(int mask, QRErrorCorrectionLevel level)
        {
            if (mask < 0 || mask > 7)
                return 0;

            return formatInfo[(int)level][mask];
        }

        /// <summary>
        /// Gets a copy of initialized frame. When the same version is 
        /// requested twice or more, a copy of cached frame is returned.
        /// </summary>
        public static byte[] CreateNewFrame(int version)
        {
            if (version < 1 || version > MaximumSymbolVersion)
                return null;

            if (frames[version] == null)
                frames[version] = QRSpec.createFrame(version);

            int width = symbolCapacity[version].width;
            byte[] frame = new byte[width * width];
            Array.Copy(frames[version], frame, width * width);
            return frame;
        }

        private static byte[] createFrame(int version)
        {
            int width = symbolCapacity[version].width;
            byte[] frame = new byte[width * width];
            Array.Clear(frame, 0, width * width);

            /* Finder pattern */
            putFinderPattern(frame, width, 0, 0);
            putFinderPattern(frame, width, width - 7, 0);
            putFinderPattern(frame, width, 0, width - 7);

            /* Separator */
            int pIndex = 0;
            int qIndex = width * (width - 7);
            for (int y = 0; y < 7; y++)
            {
                frame[pIndex + 7] = 0xc0;
                frame[pIndex + width - 8] = 0xc0;
                frame[qIndex + 7] = 0xc0;
                pIndex += width;
                qIndex += width;
            }

            for (int i = 0; i < 8; i++)
                frame[width * 7 + i] = 0xc0;

            for (int i = 0; i < 8; i++)
                frame[width * 8 - 8 + i] = 0xc0;

            for (int i = 0; i < 8; i++)
                frame[width * (width - 8) + i] = 0xc0;

            /* Mask format information area */

            for (int i = 0; i < 9; i++)
                frame[width * 8 + i] = 0x84;

            for (int i = 0; i < 8; i++)
                frame[width * 9 - 8 + i] = 0x84;

            pIndex = 8;
            for (int y = 0; y < 8; y++)
            {
                frame[pIndex] = 0x84;
                pIndex += width;
            }

            pIndex = width * (width - 7) + 8;
            for (int y = 0; y < 7; y++)
            {
                frame[pIndex] = 0x84;
                pIndex += width;
            }

            /* Timing pattern */
            pIndex = width * 6 + 8;
            qIndex = width * 8 + 6;
            for (int x = 1; x < width - 15; x++)
            {
                frame[pIndex] = (byte)(0x90 | (x & 1));
                frame[qIndex] = (byte)(0x90 | (x & 1));
                pIndex++;
                qIndex += width;
            }

            /* Alignment pattern */
            AlignmentPatternPosition alignment = QRSpec.getAlignmentPattern(version);
            if (alignment != null)
            {
                for (int x = 0; x < alignment.n; x++)
                    putAlignmentPattern(frame, width, alignment.pos[x * 2], alignment.pos[x * 2 + 1]);
            }

            /* Version information */
            if (version >= 7)
            {
                int verinfo = QRSpec.getVersionPattern(version);

                pIndex = width * (width - 11);
                int v = verinfo;
                for (int x = 0; x < 6; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        frame[pIndex + width * y + x] = (byte)(0x88 | (v & 1));
                        v = v >> 1;
                    }
                }

                pIndex = width - 11;
                v = verinfo;
                for (int y = 0; y < 6; y++)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        frame[pIndex + x] = (byte)(0x88 | (v & 1));
                        v = v >> 1;
                    }
                    pIndex += width;
                }
            }

            frame[width * (width - 8) + 8] = 0x81;
            return frame;
        }

        /// <summary>
        /// Puts an alignment pattern. ox,oy is center coordinate of the pattern
        /// </summary>
        private static void putAlignmentPattern(byte[] frame, int width, int ox, int oy)
        {
            byte[] finder = 
            {
                0xa1, 0xa1, 0xa1, 0xa1, 0xa1,
                0xa1, 0xa0, 0xa0, 0xa0, 0xa1,
                0xa1, 0xa0, 0xa1, 0xa0, 0xa1,
                0xa1, 0xa0, 0xa0, 0xa0, 0xa1,
                0xa1, 0xa1, 0xa1, 0xa1, 0xa1,
            };

            int frameIndex = (oy - 2) * width + ox - 2;
            int finderIndex = 0;
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                    frame[frameIndex + x] = finder[finderIndex + x];

                frameIndex += width;
                finderIndex += 5;
            }
        }

        /// <summary>
        /// Puts a finder pattern. ox,oy is upper-left coordinate of the pattern
        /// </summary>
        private static void putFinderPattern(byte[] frame, int width, int ox, int oy)
        {
            byte[] finder = 
            {
                0xc1, 0xc1, 0xc1, 0xc1, 0xc1, 0xc1, 0xc1,
                0xc1, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc1,
                0xc1, 0xc0, 0xc1, 0xc1, 0xc1, 0xc0, 0xc1,
                0xc1, 0xc0, 0xc1, 0xc1, 0xc1, 0xc0, 0xc1,
                0xc1, 0xc0, 0xc1, 0xc1, 0xc1, 0xc0, 0xc1,
                0xc1, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc1,
                0xc1, 0xc1, 0xc1, 0xc1, 0xc1, 0xc1, 0xc1,
            };

            int frameIndex = oy * width + ox;
            int finderIndex = 0;
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 7; x++)
                    frame[frameIndex + x] = finder[finderIndex + x];

                frameIndex += width;
                finderIndex += 7;
            }
        }
    }
}
