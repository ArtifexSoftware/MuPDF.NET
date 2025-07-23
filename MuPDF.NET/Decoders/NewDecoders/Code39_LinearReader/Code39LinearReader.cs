using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Code39
{
    /// <summary>
    /// Code39 reader.
    /// </summary>
    [Obsolete("This class does not pass all tests.")]
#if CORE_DEV
    public
#else
    internal
#endif
    class Code39LinearReader : LinearReader
    {
        /// <summary>
        /// do decode extended Code 39 symbols or not (disabled by default)
        /// </summary>
        protected bool DoDecodeExtended = false;
        /// <summary>
        /// Check sum mode
        /// </summary>
        public Mode CheckSumMode = Mode.None;

        private bool HighPrecision = false;//!!!!!

        public enum Mode { None, CheckMod43Checksum, CheckMod11ChecksumPZN8, CheckMod11ChecksumISBN10, CheckMod11ChecksumUPU };


        private static int[][] _startStopPatterns = { new int[] { 1, 2, 1, 1, 2, 1, 2, 1, 1 } };

        string _alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*";

        int[][] _patterns = new int[][] {
            new int[]{1,1,1,2,2,1,2,1,1,1},//0
            new int[]{2,1,1,2,1,1,1,1,2,1},
            new int[]{1,1,2,2,1,1,1,1,2,1},
            new int[]{2,1,2,2,1,1,1,1,1,1},
            new int[]{1,1,1,2,2,1,1,1,2,1},
            new int[]{2,1,1,2,2,1,1,1,1,1},//5
            new int[]{1,1,2,2,2,1,1,1,1,1},
            new int[]{1,1,1,2,1,1,2,1,2,1},
            new int[]{2,1,1,2,1,1,2,1,1,1},
            new int[]{1,1,2,2,1,1,2,1,1,1},
            new int[]{2,1,1,1,1,2,1,1,2,1},//10
            new int[]{1,1,2,1,1,2,1,1,2,1},
            new int[]{2,1,2,1,1,2,1,1,1,1},
            new int[]{1,1,1,1,2,2,1,1,2,1},
            new int[]{2,1,1,1,2,2,1,1,1,1},
            new int[]{1,1,2,1,2,2,1,1,1,1},//15
            new int[]{1,1,1,1,1,2,2,1,2,1},
            new int[]{2,1,1,1,1,2,2,1,1,1},
            new int[]{1,1,2,1,1,2,2,1,1,1},
            new int[]{1,1,1,1,2,2,2,1,1,1},
            new int[]{2,1,1,1,1,1,1,2,2,1},//20
            new int[]{1,1,2,1,1,1,1,2,2,1},
            new int[]{2,1,2,1,1,1,1,2,1,1},
            new int[]{1,1,1,1,2,1,1,2,2,1},
            new int[]{2,1,1,1,2,1,1,2,1,1},
            new int[]{1,1,2,1,2,1,1,2,1,1},//25
            new int[]{1,1,1,1,1,1,2,2,2,1},
            new int[]{2,1,1,1,1,1,2,2,1,1},
            new int[]{1,1,2,1,1,1,2,2,1,1},
            new int[]{1,1,1,1,2,1,2,2,1,1},
            new int[]{2,2,1,1,1,1,1,1,2,1},//30
            new int[]{1,2,2,1,1,1,1,1,2,1},
            new int[]{2,2,2,1,1,1,1,1,1,1},
            new int[]{1,2,1,1,2,1,1,1,2,1},
            new int[]{2,2,1,1,2,1,1,1,1,1},
            new int[]{1,2,2,1,2,1,1,1,1,1},//35
            new int[]{1,2,1,1,1,1,2,1,2,1},
            new int[]{2,2,1,1,1,1,2,1,1,1},
            new int[]{1,2,2,1,1,1,2,1,1,1},
            new int[]{1,2,1,2,1,2,1,1,1,1},
            new int[]{1,2,1,2,1,1,1,2,1,1},//40
            new int[]{1,2,1,1,1,2,1,2,1,1},
            new int[]{1,1,1,2,1,2,1,2,1,1},
            new int[]{1,2,1,1,2,1,2,1,1,1}
        };

        private BarSymbolReaderLooseProjection LooseProjectionReader;

        public Code39LinearReader()
        {           
            //MaxPatternSymbolDifference = 0.9f;//1.2f;// 0.83f;
            //MaxPatternAverageSymbolDifference = 0.6f;//0.8//2 /0.55f;
            //MaxReadError = 1f;//1.3f;

            MaxLeftAndRightModulesDifference = 1.9f;
            UseE = true;
        }

        internal override void GetParams(out int[] startPattern, out int[] stopPattern, out int minModulesPerBarcode, out int maxModulesPerBarcode)
        {
            //startPattern = stopPattern = _startStopPatterns[0];

            //!!!!!!!
            startPattern = new int[] {1, 2, 1, 1};
            stopPattern = new int[] {2, 1, 1};

            minModulesPerBarcode = 26;
            maxModulesPerBarcode = int.MaxValue;

            //
            //MidPoints = new float[] {0.5f, 0.3f, 0.7f};
            MinConfidence = 0.7f;
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.Code39;
        }

        private BarSymbolReaderNaive ReaderNaive;

        internal override bool ReadSymbols(BarCodeRegion region, Pattern startPattern, Pattern stopPattern, MyPoint from, MyPoint to, float module, bool firstPass)
        {
            if (Reader == null)
            {
                Reader = new BarSymbolReader(Scan, 10, 13, false, true, 1, _patterns, -1, UseE, null);
                LooseProjectionReader = new BarSymbolReaderLooseProjection(Scan, 10, 13, true, _patterns, 43, false, UseE);
                // setting min match difference
                LooseProjectionReader.MinMatchDifference = 1E-7f;

                ReaderNaive = new BarSymbolReaderNaive(10, 13, _patterns);
            }

            float error, maxError, confidence;

            int[] row = ReadSymbols(startPattern, stopPattern, from, to, module, out error, out maxError, out confidence);

            if (error < MaxReadError)
            {
                if (confidence >= MinConfidence)
                    if (confidence > region.Confidence)
                        if (Decode(region, row)) //try to decode
                        {
                            region.Confidence = confidence;
                            return true;
                        }
            }

            //try naive reader
            row = ReaderNaive.Read(Scan.GetPixels(from, to), out error, out maxError, out confidence);
            //var row = ReaderNaive.Read(Scan.GetPixels(from, to, 1.4f), out error, out maxError, out confidence);
            //row = ReaderNaive.Read(Scan.GetPixels3Lines(from, to, 1.4f, 3.5f), out error, out maxError, out confidence);

            if (confidence >= MinConfidence && maxError < 0.5f)
                if (confidence > region.Confidence)
                    if (Decode(region, row)) //try to decode
                    {
                        region.Confidence = confidence;
                        return true;
                    }

            //try LooseProjection reader
            if (firstPass && HighPrecision)
            if (row != null && row.Length > 3)
            {
                //expected number of codewords
                int nCodewords = (int)Math.Round((region.B - region.A).Length / module / 13f, MidpointRounding.AwayFromZero);

                float[] widths = new float[] { 0.5f, 0.9f };
                LooseProjectionReader.scan = Scan;

                foreach (float w in widths)
                {
                    row = LooseProjectionReader.Read(region, module, w);
                    if (row != null && row.Length > 2 && row[0] == 43 && row[row.Length - 1] == 43 && Around(row.Length, nCodewords))
                        if (Decode(region, row))
                        {
                            return true;
                        }
                }
            }

            return false;
        }

        bool Around(int a, int b)
        {
            return a < b && b - a < 2 || a >= b && a - b < 2;
        }

        internal override bool Decode(BarCodeRegion r, int[] row)
        {
            if (row == null)
                return false;

            if (row[0] != 43 || row[row.Length - 1] != 43)
                return false;

            return Decode(r, row, 1, row != null ? row.Length - 1 : 0);
        }

        internal virtual bool Decode(BarCodeRegion r, int[] row, int iStart, int iEnd)
        {
            string pre = "";
            if (row != null && row.Length > 2)
            {
                if (CheckSumMode == Mode.CheckMod43Checksum)
                {
                    iEnd--;
                    int sum = 0;
                    for (int i = iStart; i < iEnd; i++) sum += row[i];
                    sum = sum % 43;
                    if (sum != row[iEnd]) return false;
                }
                else if (CheckSumMode == Mode.CheckMod11ChecksumPZN8)
                {
                    iEnd--;
                    int sum = 0;
                    int[] weights = new int[] { 1, 2, 3, 4, 5, 6, 7 };
                    bool isPzn7 = false;
                    // check if it is PZN8
                    if (iEnd - iStart != 8)
                    {
                        // check if it is PZN7
                        isPzn7 = iEnd - iStart == 7;

                        // otherwise exit
                        if (!isPzn7)
                            return false;
                    }

                    if (row[iStart] != 36) return false; //PZN starts with -
                    iStart++;

                    if (isPzn7)
                        // count weights as 2,3,4,5,6..
                        for (int i = iStart; i < iEnd; i++) sum += row[i] * weights[i - iStart + 1];
                    else
                        // PZN8, count weights as 1,2,3,4,5,6
                        for (int i = iStart; i < iEnd; i++) sum += row[i] * weights[i - iStart];
                    int checkdigit = sum % 11;
                    if (checkdigit == 10) checkdigit = 0;
                    if (checkdigit != row[iEnd]) return false;
                    pre = "";// "PZN-";
                }
                else if (CheckSumMode == Mode.CheckMod11ChecksumISBN10)
                {
                    iEnd--;
                    int sum = 0;
                    int[] weights = new int[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
                    if (iEnd - iStart != 9) return false;
                    for (int i = iStart; i < iEnd; i++) sum += row[i] * weights[i - iStart];
                    int checkdigit = 11 - sum % 11;
                    if (checkdigit == 10) checkdigit = 0;
                    else if (checkdigit == 11) checkdigit = 5;
                    if (checkdigit != row[iEnd]) return false;
                }
                else if (CheckSumMode == Mode.CheckMod11ChecksumUPU)
                {
                    iEnd--;
                    int sum = 0;
                    int[] weights = new int[] { 8, 6, 4, 2, 3, 5, 9, 7 };
                    if (iEnd - iStart != 8) return false; //UPU has just 8 digits + Checksum
                    for (int i = iStart; i < iEnd; i++) sum += row[i] * weights[i - iStart];
                    int checkdigit = 11 - sum % 11;
                    if (checkdigit == 10) checkdigit = 0;
                    else if (checkdigit == 11) checkdigit = 5;
                    if (checkdigit != row[iEnd]) return false;
                }

                var s = new StringBuilder();
                for (int i = iStart; i < iEnd; i++)
                if (row[i] >= 0 && row[i] < _patterns.Length)
                    s.Append(_alphabet.Substring(row[i], 1));
                else
                    return false;

                var res = s.ToString();
                if (DoDecodeExtended)
                    res = DecodeExtended(res);

                r.Data = new ABarCodeData[] { new StringBarCodeData(res) };
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if value using extended alphabet and changes value accordingly.
        /// </summary>
        string DecodeExtended(string value)
        {
            StringBuilder result = new StringBuilder();
            char newChar;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (i < value.Length - 1 && "$/+%".IndexOf(c) != -1)
                {
                    char next = value[i + 1];
                    switch (c)
                    {
                        case '$':
                            if (next >= 'A' && next <= 'Z')
                            {
                                newChar = (char)(next - 64);
                                break;
                            }

                            newChar = next;
                            result.Append(c);
                            break;

                        case '/':
                            if (next >= 'A' && next <= 'O')
                            {
                                newChar = (char)(next - 32);
                                break;
                            }
                            else if (next == 'Z')
                            {
                                newChar = ':';
                                break;
                            }

                            newChar = next;
                            result.Append(c);
                            break;

                        case '+':
                            if (next >= 'A' && next <= 'Z')
                            {
                                newChar = (char)(next + 32);
                                break;
                            }

                            newChar = next;
                            result.Append(c);
                            break;

                        case '%':
                            if (next >= 'A' && next <= 'E')
                            {
                                newChar = (char) (next - 38);
                                break;
                            }
                            else if (next >= 'F' && next <= 'J')
                            {
                                newChar = (char) (next - 11);
                                break;
                            }
                            else if (next >= 'K' && next <= 'O')
                            {
                                newChar = (char) (next + 16);
                                break;
                            }
                            else if (next >= 'P' && next <= 'T')
                            {
                                newChar = (char) (next + 43);
                                break;
                            }
                            else if (next == 'U')
                            {
                                newChar = (char) 0;
                                break;
                            }
                            else if (next == 'V')
                            {
                                newChar = '@';
                                break;
                            }
                            else if (next == 'W')
                            {
                                newChar = '`';
                                break;
                            }
                            else if (next >= 'X' && next <= 'Z')
                            {
                                newChar = (char) 127; // DEL
                                break;
                            }

                            newChar = next;
                            result.Append(c);
                            break;

                        default:
                            return null;
                    }

                    result.Append(newChar);
                    i++;
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}