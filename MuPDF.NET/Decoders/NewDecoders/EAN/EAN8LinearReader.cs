using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.EAN
{
    /// <summary>
    /// EAN8 reader.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class EAN8LinearReader : LinearReader
    {
        private static int[][] _startStopPatterns = { new int[] { 1, 1, 1 } };
        private static int[][] _middlePattern = { new int[] { 1, 1, 1, 1, 1 } };

        private static int[][] _symbols =
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

        private static int[][][] _patterns = new int[][][]
        {
            _startStopPatterns,
            _symbols,
            _middlePattern,
            _symbols,
            _startStopPatterns
        };

        static int[] nBars        = { 3, 4, 4, 4, 4, 5, 4, 4, 4, 4, 3 };
        static int[] nLength      = { 3, 7, 7, 7, 7, 5, 7, 7, 7, 7, 3 };
        static int[] tableIndexes = { 0, 1, 1, 1, 1, 2, 3, 3, 3, 3, 4 };

        public EAN8LinearReader()
        {
            //incerase MaxSkewAngle because EAN can be on curved surface
            MaxSkewAngle = 40 * (float)Math.PI / 180;
        }

        internal override void GetParams(out int[] startPattern, out int[] stopPattern, out int minModulesPerBarcode, out int maxModulesPerBarcode)
        {
            startPattern = stopPattern = _startStopPatterns[0];
            maxModulesPerBarcode = minModulesPerBarcode = 67;

            if (Reader == null)
            {
                Reader = new BarSymbolReader(Scan, nBars, nLength, tableIndexes, false, true, 1, _patterns, UseE, null);
            }
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.EAN8;
        }

        internal override bool Decode(BarCodeRegion reg, int[] row)
        {
            if (row.Length < 11) return false;

            //decode
            byte[] data = new byte[8];

            for (int i = 1; i < 5; i++)
            {
                if (row[i] < 0) return false;
                data[i - 1] = (byte)row[i];
            }

            for (int i = 6; i < 10; i++)
            {
                if (row[i] < 0) return false;
                data[i - 2] = (byte)row[i];
            }

            if (!VerifyCheckSum(data)) return false;

            reg.Data = new ABarCodeData[] { new NumericBarCodeData(data) };

            return true;
        }

        internal virtual bool VerifyCheckSum(byte[] data)
        {
            int sum = 0;
            for (int i = data.Length - 2; i >= 0; i -= 2)
            {
                sum += data[i];
            }

            sum *= 3;

            for (int i = data.Length - 1; i >= 0; i -= 2)
            {
                sum += data[i];
            }

            return sum % 10 == 0;
        }

        private int Calc13Digit(int[] row)
        {
            //calc signature
            var sign = 0;
            for (int i = 1; i < 7; i++)
                if (row[i] < 10)
                    sign = sign * 10 + 0;
                else
                    sign = sign * 10 + 1;
            //
            switch (sign)
            {
                case 000000: return 0;
                case 001011: return 1;
                case 001101: return 2;
                case 001110: return 3;
                case 010011: return 4;
                case 011001: return 5;
                case 011100: return 6;
                case 010101: return 7;
                case 010110: return 8;
                case 011010: return 9;
                default: return -1;
            }
        }
    }
}