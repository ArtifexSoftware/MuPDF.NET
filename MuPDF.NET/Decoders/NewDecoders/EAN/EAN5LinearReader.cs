using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.EAN
{
    /// <summary>
    /// EAN5 reader.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class EAN5LinearReader : LinearReader
    {
        private static int[][] _startPatterns = { new int[] { 1, 1, 2 } };
        private static int[][] _stopPatterns = { new int[] {} };
        private static int[][] _separator = { new int[] { 1, 1 } };

        // Odd parity patterns for left-hand encoding
        private static int[][] _leftOddPatterns =
        {
            new int[] {3, 2, 1, 1}, // 0
            new int[] {2, 2, 2, 1}, // 1
            new int[] {2, 1, 2, 2}, // 2
            new int[] {1, 4, 1, 1}, // 3
            new int[] {1, 1, 3, 2}, // 4
            new int[] {1, 2, 3, 1}, // 5
            new int[] {1, 1, 1, 4}, // 6
            new int[] {1, 3, 1, 2}, // 7
            new int[] {1, 2, 1, 3}, // 8
            new int[] {3, 1, 1, 2}  // 9
        };

        // All left-hand encoding patterns (odd parity and even parity)
        private static int[][] _leftPatterns =
        {
            // odd parity
            _leftOddPatterns[0], _leftOddPatterns[1],
            _leftOddPatterns[2], _leftOddPatterns[3],
            _leftOddPatterns[4], _leftOddPatterns[5],
            _leftOddPatterns[6], _leftOddPatterns[7],
            _leftOddPatterns[8], _leftOddPatterns[9],

            // even parity
            new int[] {1, 1, 2, 3}, // 0
            new int[] {1, 2, 2, 2}, // 1
            new int[] {2, 2, 1, 2}, // 2
            new int[] {1, 1, 4, 1}, // 3
            new int[] {2, 3, 1, 1}, // 4
            new int[] {1, 3, 2, 1}, // 5
            new int[] {4, 1, 1, 1}, // 6
            new int[] {2, 1, 3, 1}, // 7
            new int[] {3, 1, 2, 1}, // 8
            new int[] {2, 1, 1, 3}  // 9
        };

        private static int[][][] _patterns = new int[][][]
        {
            _startPatterns,
            _leftPatterns,
            _separator
        };

        static int[] nBars        = { 3, 4, 2, 4, 2, 4, 2, 4, 2, 4 };
        static int[] nLength      = { 4, 7, 2, 7, 2, 7, 2, 7, 2, 7 };
        static int[] tableIndexes = { 0, 1, 2, 1, 2, 1, 2, 1, 2, 1 };

        public EAN5LinearReader()
        {
            //incerase MaxSkewAngle because EAN can be on curved surface
            MaxSkewAngle = 40 * (float)Math.PI / 180;
        }

        internal override void GetParams(out int[] startPattern, out int[] stopPattern, out int minModulesPerBarcode, out int maxModulesPerBarcode)
        {
            startPattern = _startPatterns[0];
            stopPattern = _stopPatterns[0];
            maxModulesPerBarcode = minModulesPerBarcode = 47;
            if (MinQuietZone > 3)
                MinQuietZone = 3;//decrease quiet zone because between EAN13 and EAN5 can be small interval
            if (Reader == null)
            {
                Reader = new BarSymbolReader(Scan, nBars, nLength, tableIndexes, false, true, 1, _patterns, UseE, null);
            }
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.EAN5;
        }

        internal override bool Decode(BarCodeRegion reg, int[] row)
        {
            if (row.Length < 10) return false;

            var digit = CalcDigit(row);
            if (digit < 0)
                return false;

            //decode
            byte[] data = new byte[5];

            for (int i = 1, j = 0; i < 10; i+=2, j++)
            {
                if (row[i] < 0) return false;
                data[j] = (byte)(row[i] % 10);
            }

            if (!VerifyCheckSum(data, digit)) return false;

            reg.Data = new ABarCodeData[] { new NumericBarCodeData(data) };

            return true;
        }

        internal virtual bool VerifyCheckSum(byte[] data, int digit)
        {
            int sum = 0;

            for (int i = 0; i < 5; i++)
            {
                sum += data[i] * (i % 2 == 0 ? 3 : 9);
            }
            
            return sum % 10 == digit;
        }

        private int CalcDigit(int[] row)
        {
            //calc signature
            var sign = 0;
            for (int i = 1; i < 10; i+=2)
                if (row[i] < 10)
                    sign = sign * 10 + 0;
                else
                    sign = sign * 10 + 1;
            //
            switch (sign)
            {
                case 11000: return 0;
                case 10100: return 1;
                case 10010: return 2;
                case 10001: return 3;
                case 01100: return 4;
                case 00110: return 5;
                case 00011: return 6;
                case 01010: return 7;
                case 01001: return 8;
                case 00101: return 9;

                default: return -1;
            }
        }
    }
}