using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.EAN
{
    /// <summary>
    /// EAN13 reader.
    /// This calss also can read UPC-A.
    /// Also it can read supplemented EAN2 and EAN5.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class EAN13LinearReader : LinearReader
    {
        public bool FindSupplementalEAN2 { get; set; } = false;
        public bool FindSupplementalEAN5 { get; set; } = false;

        private static int[][] _startStopPatterns = { new int[] { 1, 1, 1 } };
        private static int[][] _middlePattern = { new int[] { 1, 1, 1, 1, 1 } };

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
            _startStopPatterns,
            _leftPatterns,
            _middlePattern,
            _leftOddPatterns,
            _startStopPatterns
        };

        static int[] nBars        = { 3, 4, 4, 4, 4, 4, 4, 5, 4, 4, 4, 4, 4, 4, 3 };
        static int[] nLength      = { 3, 7, 7, 7, 7, 7, 7, 5, 7, 7, 7, 7, 7, 7, 3 };
        static int[] tableIndexes = { 0, 1, 1, 1, 1, 1, 1, 2, 3, 3, 3, 3, 3, 3, 4 };

        public EAN13LinearReader()
        {
            //incerase MaxSkewAngle because EAN can be on curved surface
            MaxSkewAngle = 40 *(float)Math.PI / 180;
        }

        internal override void GetParams(out int[] startPattern, out int[] stopPattern, out int minModulesPerBarcode, out int maxModulesPerBarcode)
        {
            startPattern = stopPattern = _startStopPatterns[0];
            maxModulesPerBarcode = minModulesPerBarcode = 89;

            if (Reader == null)
            {
                Reader = new BarSymbolReader(Scan, nBars, nLength, tableIndexes, false, true, 1, _patterns, UseE, null);
            }
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.EAN13;
        }

        internal override bool Decode(BarCodeRegion reg, int[] row)
        {
            if (row.Length < 15) return false;

            //calc 13 digit
            var digit13 = Calc13Digit(row);

            if (digit13 < 0) return false;

            //decode
            byte[] data = new byte[13];
            data[0] = (byte)digit13;

            for (int i = 1; i < 7; i++)
            {
                if (row[i] < 0) return false;
                data[i] = (byte)(row[i] % 10);
            }

            for (int i = 8; i < 14; i++)
            {
                if (row[i] < 0) return false;
                data[i - 1] = (byte)(row[i] % 10);
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

        public override FoundBarcode[] Decode(BlackAndWhiteImage bwImage)
        {
            //call base method
            var res = base.Decode(bwImage);

            //try to find supplimental barcodes (EAN2, EAN5)
            if (FindSupplementalEAN2 || FindSupplementalEAN5)
            {
                var list = new List<FoundBarcode>(res);

                for (int i = 0; i < list.Count; i++)
                {
                    var parent = list[i];

                    if (FindSupplementalEAN5)
                    {
                        var supp = FindSupplemental(parent, new EAN5LinearReader(), bwImage);
                        if (supp != null)
                        {
                            list.Insert(i + 1, supp);
                            i++;
                            continue;
                        }
                    }

                    if (FindSupplementalEAN2)
                    {
                        var supp = FindSupplemental(parent, new EAN2LinearReader(), bwImage);
                        if (supp != null)
                        {
                            list.Insert(i + 1, supp);
                            i++;
                            continue;
                        }
                    }
                }

                res = list.ToArray();
            }

            return res;
        }

        private FoundBarcode FindSupplemental(FoundBarcode parent, LinearReader reader, BlackAndWhiteImage bwImage)
        {
            //copy parameters
            CopyTo(reader);
            // excepts one barcode
            reader.ExpectedNumberOfBarcodes = 1;
            //calc ROI
            var d = (parent.ParentRegion.B - parent.ParentRegion.A);
            var d1 = d;
            var d2 = d * 2;
            if (parent.ParentRegion.Reversed)
            {
                //reversed
                d1 = -d * 2;
                d2 = -d;
            }
                
            var r = new BarCodeRegion(parent.ParentRegion);//clone region
            r.SetCorners(r.A + d1, r.B + d2, r.C + d2, r.D + d1);//offset region
            reader.ROI = r.GetBounds();
            reader.ROI.Inflate(5, 5);
            //find barcode
            var found = reader.Decode(bwImage);

            return found.Length >  0 ?  found[0] : null;
        }
    }
}