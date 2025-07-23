using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.TriOptic
{
    /// <summary>
    /// TriOptic reader.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class TriOpticLinearReader : LinearReader
    {
        /// <summary>
        /// do decode extended Code 39 symbols or not (disabled by default)
        /// </summary>
        protected bool DoDecodeExtended = false;
        /// <summary>
        /// Check sum mode
        /// </summary>
        public Code39.Code39LinearReader.Mode CheckSumMode = Code39.Code39LinearReader.Mode.None;

        public enum Mode { None, CheckMod43Checksum, CheckMod11ChecksumPZN8, CheckMod11ChecksumISBN10, CheckMod11ChecksumUPU };


        //private static int[][] _startStopPatterns = { new int[] { 1, 2, 1, 2, 1, 2, 1, 1, 1 } };
        private static int[][] _startStopPatterns = { new int[] { 1 } };

        string _alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. *$/+%";

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

        public TriOpticLinearReader()
        {
            MaxPatternSymbolDifference = 1.9f;//1.2f;// 0.83f;
            MaxPatternAverageSymbolDifference = 1.6f;//0.8//2 /0.55f;
            //MaxReadError = 1f;//1.3f;

            MaxLeftAndRightModulesDifference = 1.9f;
            UseE = false;
            MinConfidence = 0.7f;
            MaxReadError = 1f;

            MidPoints = new float[] {0.5f};
            MinQuietZone = 3.5f;
        }

        private BarSymbolReaderNaive reader;

        internal override void GetParams(out int[] startPattern, out int[] stopPattern, out int minModulesPerBarcode, out int maxModulesPerBarcode)
        {
            startPattern = stopPattern = _startStopPatterns[0];

            minModulesPerBarcode = 13 * 3;
            maxModulesPerBarcode = int.MaxValue;

            if (reader == null)
            {
                reader = new BarSymbolReaderNaive(10, 13, _patterns);
            }
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.TriopticCode39;
        }

        internal override int[] ReadSymbols(Pattern leftPattern, Pattern rightPattern, MyPointF from, MyPointF to, float module, out float error, out float maxError, out float confidence)
        {
            var line = Scan.GetPixels(from, to, 1.3f);//1.17

            var res = reader.Read(line, out error, out maxError, out confidence);

            if (maxError > 0.99f)
                return new int[0];

            if (confidence > 0)
            {
#if DEBUG
                DebugHelper.AddDebugItem(from.ToString(), line, from, to);
                DebugHelper.DrawArrow(from.X, from.Y, to.X, to.Y, Color.Magenta);
#endif
            }

            return res;
        }

        internal override bool Decode(BarCodeRegion r, int[] row)
        {
            if (row == null)
                return false;

            var res = Decode(r, row, 1, row.Length - 1);
            if (res)
            {
                var data = r.Data[0] as StringBarCodeData;
                if (data.Value.Length == 6)
                {
                    var p1 = data.Value.Substring(0, 3);
                    var p2 = data.Value.Substring(3, 3);
                    data.Value = p2 + p1;
                }
            }

            return res;
        }

        internal virtual bool Decode(BarCodeRegion r, int[] row, int iStart, int iEnd)
        {
            string pre = "";
            if (row != null && row.Length > 2)
            {
                if (CheckSumMode == Code39.Code39LinearReader.Mode.CheckMod43Checksum)
                {
                    iEnd--;
                    int sum = 0;
                    for (int i = iStart; i < iEnd; i++) sum += row[i];
                    sum = sum % 43;
                    if (sum != row[iEnd]) return false;
                }
                else if (CheckSumMode == Code39.Code39LinearReader.Mode.CheckMod11ChecksumPZN8)
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
                else if (CheckSumMode == Code39.Code39LinearReader.Mode.CheckMod11ChecksumISBN10)
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
                else if (CheckSumMode == Code39.Code39LinearReader.Mode.CheckMod11ChecksumUPU)
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
                                newChar = (char)(next - 38);
                                break;
                            }
                            else if (next >= 'F' && next <= 'W')
                            {
                                newChar = (char)(next - 11);
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
