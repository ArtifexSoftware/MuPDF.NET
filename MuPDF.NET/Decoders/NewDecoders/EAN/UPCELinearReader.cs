using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.EAN
{
    /// <summary>
    /// UPC-E reader.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class UPCELinearReader : EAN13LinearReader
    {
        private static int[][] _startPattern = { new int[] {1, 1, 1} };
        private static int[][] _stopPattern = { new int[] { 1, 1, 1, 1, 1 } };

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
            _startPattern,
            _leftPatterns,
            _stopPattern
        };

        static int[] nBars        = { 3, 4, 4, 4, 4, 4, 4, 5 };
        static int[] nLength      = { 3, 7, 7, 7, 7, 7, 7, 5 };
        static int[] tableIndexes = { 0, 1, 1, 1, 1, 1, 1, 2 };

        internal override void GetParams(out int[] startPattern, out int[] stopPattern, out int minModulesPerBarcode, out int maxModulesPerBarcode)
        {
            startPattern = _startPattern[0];
            stopPattern = _stopPattern[0];
            maxModulesPerBarcode = minModulesPerBarcode = 50;

            if (Reader == null)
            {
                Reader = new BarSymbolReader(Scan, nBars, nLength, tableIndexes, false, true, 1, _patterns, UseE, null);
            }
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.UPCE;
        }

        internal override bool Decode(BarCodeRegion reg, int[] row)
        {
            if (row.Length < 8) return false;

            //calc 7 digit
            var digit7 = Calc7Digit(row);

            if (digit7 < 0) return false;

            //decode
            byte[] data = new byte[7];
            data[6] = (byte)digit7;

            for (int i = 1; i < 7; i++)
            {
                if (row[i] < 0) return false;
                data[i - 1] = (byte)(row[i] % 10);
            }

            //decode to UPCA and calc checksum
            var upca = convertUpceToUpca(data);
            if (!VerifyCheckSum(upca)) return false;

            //insert numeric system
            var numSyst = CalcNumberSystem(row);
            Array.Resize(ref data, data.Length + 1);
            for (int i = data.Length - 1; i >= 1; i--)
                data[i] = data[i - 1];
            data[0] = (byte)numSyst;

            reg.Data = new ABarCodeData[] { new NumericBarCodeData(data) };

            return true;
        }

        public static byte[] convertUpceToUpca(byte[] upce)
        {
            byte[] upceCore = null;

            if (upce.Length == 6)
            {
                upceCore = upce;
            }
            else if (upce.Length == 7)
            {
                // truncate last digit, assume it is just check digit
                upceCore = new byte[6];
                Array.Copy(upce, upceCore, 6);
            }
            else if (upce.Length == 8)
            {
                // truncate first and last digit, 
                // assume first digit is number system digit
                // last digit is check digit
                upceCore = new byte[6];
                Array.Copy(upce, 1, upceCore, 0, 6);
            }
            else
            {
                return null;
            }

            byte[] upca = new byte[12];
            byte lastDigit = upceCore[5];
            switch (lastDigit)
            {
                case 0:
                case 1:
                case 2:
                    upca[1] = upceCore[0];
                    upca[2] = upceCore[1];
                    upca[3] = lastDigit;
                    upca[8] = upceCore[2];
                    upca[9] = upceCore[3];
                    upca[10] = upceCore[4];
                    break;
                case 3:
                    upca[1] = upceCore[0];
                    upca[2] = upceCore[1];
                    upca[3] = upceCore[2];
                    upca[9] = upceCore[3];
                    upca[10] = upceCore[4];
                    break;
                case 4:
                    upca[1] = upceCore[0];
                    upca[2] = upceCore[1];
                    upca[3] = upceCore[2];
                    upca[4] = upceCore[3];
                    upca[10] = upceCore[4];
                    break;
                default:
                    upca[1] = upceCore[0];
                    upca[2] = upceCore[1];
                    upca[3] = upceCore[2];
                    upca[4] = upceCore[3];
                    upca[5] = upceCore[4];
                    upca[10] = lastDigit;
                    break;
            }

            int check = calculateUpcaChecksum(upca);
            if (check > -1)
            {
                upca[11] = (byte)check;
            }

            return upca;
        }

        protected static int calculateUpcaChecksum(byte[] data)
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

            return 10 - sum % 10;
        }

        private int Calc7Digit(int[] row)
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
                case 111000: case 000111: return 0;
                case 110100: case 001011: return 1;
                case 110010: case 001101: return 2;
                case 110001: case 001110: return 3;
                case 101100: case 010011: return 4;
                case 100110: case 011001: return 5;
                case 100011: case 011100: return 6;
                case 101010: case 010101: return 7;
                case 101001: case 010110: return 8;
                case 100101: case 011010: return 9;
                default: return -1;
            }
        }

        private int CalcNumberSystem(int[] row)
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
                case 111000: 
                case 110100: 
                case 110010: 
                case 110001: 
                case 101100: 
                case 100110: 
                case 100011: 
                case 101010: 
                case 101001: 
                case 100101: return 0;
                case 000111:
                case 001011:
                case 001101:
                case 001110:
                case 010011:
                case 011001:
                case 011100:
                case 010101:
                case 010110:
                case 011010: return 1;
                default: return -1;
            }
        }
    }
}