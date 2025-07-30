using System;
using System.Collections.Generic;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Code128
{
    /// <summary>
    /// Code128 reader.
    /// </summary>
    [Obsolete("This class does not pass all tests.")]
#if CORE_DEV
    public
#else
    internal
#endif
    class Code128LinearReader : LinearReader
    {
        private bool HighPrecision = false;

        #region Patterns

        private const int Shift = 98;

        private const int CodeC = 99;
        private const int CodeB = 100;
        private const int CodeA = 101;

        private const int FNC1 = 102;
        private const int FNC2 = 97;
        private const int FNC3 = 96;
        private const int FNC4A = 101;
        private const int FNC4B = 100;

        private const int StartA = 103;
        private const int StartB = 104;
        private const int StartC = 105;
        private const int Stop = 106;

        static readonly int[][] _startPatterns = new int[][] {
            new int[] { 2, 1, 1, 4, 1, 2 },
            new int[] { 2, 1, 1, 2, 1, 4 },
            new int[] { 2, 1, 1, 2, 3, 2 }
        };
        static readonly int[][] _stopPatterns = new int[][] {
            new int[] { 2, 3, 3, 1, 1, 1, 2 }
        };

        static readonly int[][] _patterns = new int[][] {
            new int[] { 2, 1, 2, 2, 2, 2 },//0
            new int[] { 2, 2, 2, 1, 2, 2 },
            new int[] { 2, 2, 2, 2, 2, 1 },
            new int[] { 1, 2, 1, 2, 2, 3 },
            new int[] { 1, 2, 1, 3, 2, 2 },
            new int[] { 1, 3, 1, 2, 2, 2 },
            new int[] { 1, 2, 2, 2, 1, 3 },
            new int[] { 1, 2, 2, 3, 1, 2 },
            new int[] { 1, 3, 2, 2, 1, 2 },
            new int[] { 2, 2, 1, 2, 1, 3 },
            new int[] { 2, 2, 1, 3, 1, 2 },//10
            new int[] { 2, 3, 1, 2, 1, 2 },
            new int[] { 1, 1, 2, 2, 3, 2 },
            new int[] { 1, 2, 2, 1, 3, 2 },
            new int[] { 1, 2, 2, 2, 3, 1 },
            new int[] { 1, 1, 3, 2, 2, 2 },
            new int[] { 1, 2, 3, 1, 2, 2 },
            new int[] { 1, 2, 3, 2, 2, 1 },
            new int[] { 2, 2, 3, 2, 1, 1 },
            new int[] { 2, 2, 1, 1, 3, 2 },
            new int[] { 2, 2, 1, 2, 3, 1 },//20
            new int[] { 2, 1, 3, 2, 1, 2 },
            new int[] { 2, 2, 3, 1, 1, 2 },
            new int[] { 3, 1, 2, 1, 3, 1 },
            new int[] { 3, 1, 1, 2, 2, 2 },
            new int[] { 3, 2, 1, 1, 2, 2 },
            new int[] { 3, 2, 1, 2, 2, 1 },
            new int[] { 3, 1, 2, 2, 1, 2 },
            new int[] { 3, 2, 2, 1, 1, 2 },
            new int[] { 3, 2, 2, 2, 1, 1 },
            new int[] { 2, 1, 2, 1, 2, 3 },//30
            new int[] { 2, 1, 2, 3, 2, 1 },
            new int[] { 2, 3, 2, 1, 2, 1 },
            new int[] { 1, 1, 1, 3, 2, 3 },
            new int[] { 1, 3, 1, 1, 2, 3 },
            new int[] { 1, 3, 1, 3, 2, 1 },
            new int[] { 1, 1, 2, 3, 1, 3 },
            new int[] { 1, 3, 2, 1, 1, 3 },
            new int[] { 1, 3, 2, 3, 1, 1 },
            new int[] { 2, 1, 1, 3, 1, 3 },
            new int[] { 2, 3, 1, 1, 1, 3 },//40
            new int[] { 2, 3, 1, 3, 1, 1 },
            new int[] { 1, 1, 2, 1, 3, 3 },
            new int[] { 1, 1, 2, 3, 3, 1 },
            new int[] { 1, 3, 2, 1, 3, 1 },
            new int[] { 1, 1, 3, 1, 2, 3 },
            new int[] { 1, 1, 3, 3, 2, 1 },
            new int[] { 1, 3, 3, 1, 2, 1 },
            new int[] { 3, 1, 3, 1, 2, 1 },
            new int[] { 2, 1, 1, 3, 3, 1 },
            new int[] { 2, 3, 1, 1, 3, 1 },//50
            new int[] { 2, 1, 3, 1, 1, 3 },
            new int[] { 2, 1, 3, 3, 1, 1 },
            new int[] { 2, 1, 3, 1, 3, 1 },
            new int[] { 3, 1, 1, 1, 2, 3 },
            new int[] { 3, 1, 1, 3, 2, 1 },
            new int[] { 3, 3, 1, 1, 2, 1 },
            new int[] { 3, 1, 2, 1, 1, 3 },
            new int[] { 3, 1, 2, 3, 1, 1 },
            new int[] { 3, 3, 2, 1, 1, 1 },
            new int[] { 3, 1, 4, 1, 1, 1 },//60
            new int[] { 2, 2, 1, 4, 1, 1 },
            new int[] { 4, 3, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 2, 2, 4 },
            new int[] { 1, 1, 1, 4, 2, 2 },
            new int[] { 1, 2, 1, 1, 2, 4 },
            new int[] { 1, 2, 1, 4, 2, 1 },
            new int[] { 1, 4, 1, 1, 2, 2 },
            new int[] { 1, 4, 1, 2, 2, 1 },
            new int[] { 1, 1, 2, 2, 1, 4 },
            new int[] { 1, 1, 2, 4, 1, 2 },//70
            new int[] { 1, 2, 2, 1, 1, 4 },
            new int[] { 1, 2, 2, 4, 1, 1 },
            new int[] { 1, 4, 2, 1, 1, 2 },
            new int[] { 1, 4, 2, 2, 1, 1 },
            new int[] { 2, 4, 1, 2, 1, 1 },
            new int[] { 2, 2, 1, 1, 1, 4 },
            new int[] { 4, 1, 3, 1, 1, 1 },
            new int[] { 2, 4, 1, 1, 1, 2 },
            new int[] { 1, 3, 4, 1, 1, 1 },
            new int[] { 1, 1, 1, 2, 4, 2 },//80
            new int[] { 1, 2, 1, 1, 4, 2 },
            new int[] { 1, 2, 1, 2, 4, 1 },
            new int[] { 1, 1, 4, 2, 1, 2 },
            new int[] { 1, 2, 4, 1, 1, 2 },
            new int[] { 1, 2, 4, 2, 1, 1 },
            new int[] { 4, 1, 1, 2, 1, 2 },
            new int[] { 4, 2, 1, 1, 1, 2 },
            new int[] { 4, 2, 1, 2, 1, 1 },
            new int[] { 2, 1, 2, 1, 4, 1 },
            new int[] { 2, 1, 4, 1, 2, 1 },//90
            new int[] { 4, 1, 2, 1, 2, 1 },
            new int[] { 1, 1, 1, 1, 4, 3 },
            new int[] { 1, 1, 1, 3, 4, 1 },
            new int[] { 1, 3, 1, 1, 4, 1 },
            new int[] { 1, 1, 4, 1, 1, 3 },
            new int[] { 1, 1, 4, 3, 1, 1 },
            new int[] { 4, 1, 1, 1, 1, 3 },
            new int[] { 4, 1, 1, 3, 1, 1 },
            new int[] { 1, 1, 3, 1, 4, 1 },
            new int[] { 1, 1, 4, 1, 3, 1 },//100
            new int[] { 3, 1, 1, 1, 4, 1 },
            new int[] { 4, 1, 1, 1, 3, 1 },
            new int[] { 2, 1, 1, 4, 1, 2 }, //startA 102
            new int[] { 2, 1, 1, 2, 1, 4 }, //startB 103
            new int[] { 2, 1, 1, 2, 3, 2 }, //startC 104
            new int[] { 2, 3, 3, 1, 1, 1 }  //stop without last bar 105
        };

        int[] nBars = new int[] { 6, 6 };
        int[] nModules = new int[] { 11, 11 };
        int[] tIndexs = new int[] { 0, 1 };
        int[][][] tables = new int[][][] { _startPatterns, _patterns };

        #endregion

        public Code128LinearReader()
        {
            UseE = true;
            MinConfidence = 0.4f;
            MaxReadError = 70;
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.Code128;
        }

        private BarSymbolReaderLooseProjection reader2;

        internal override void GetParams(out int[] startPattern, out int[] stopPattern, out int minModulesPerBarcode, out int maxModulesPerBarcode)
        {
            startPattern = new int[] { 2, 1, 1 };
            stopPattern = new int[] { 2, 3, 3, 1, 1, 1, 2 };

            minModulesPerBarcode = 24;
            maxModulesPerBarcode = int.MaxValue;

            if (Reader == null)
            {
                //Reader = new BarSymbolReader(Scan, 6, 11, false, true, 1, _patterns, -1, UseE, null);
                Reader = new BarSymbolReader(Scan, nBars, nModules, tIndexs, false, true, 1, tables, UseE, null);

                reader2 = new BarSymbolReaderLooseProjection(Scan, 6, 11, true, _patterns, 106, true, UseE);
            }
        }

        internal override bool ReadSymbols(BarCodeRegion region, Pattern startPattern, Pattern stopPattern, MyPoint from, MyPoint to, float module, bool firstPass)
        {
            float error, maxError, confidence;

            //go 2 bars back from end of line (because stop pattern contains +1 bar)
            //var toCorrected = GotoBar(to, from, 1);//!!!!!
            var toCorrected = to;

#if DEBUG
            var line = Scan.GetPixels(from, toCorrected);//!!!!!
            DebugHelper.AddDebugItem(region.A.ToString(), line, from, toCorrected);
#endif

            int[] row = ReadSymbols(startPattern, stopPattern, from, toCorrected, module, out error, out maxError, out confidence);

            if (error < MaxReadError)
            {
                if (confidence >= MinConfidence)
                    if (confidence > region.Confidence)
                    {
                        if(row[0] < 3)
                            row[0] = row[0] + StartA;
                        if (Decode(region, row)) //try to decode
                        {
                            region.Confidence = confidence;
                            return true;
                        }
                    }
            }

            //try BarSymbolReaderLooseProjection
            if (HighPrecision)
            if (confidence > 0.6f)
            {
                float[] widths = new float[] {0.5f, 0.9f};
                foreach (float w in widths)
                {
                    row = reader2.Read(region, module, w);
                    if (row != null && row.Length > 0)
                    {
                        if (row[0] < 3)
                            row[0] = row[0] + StartA;
                        if (Decode(region, row))
                        {
                            region.Confidence = confidence;
                            return true;
                        }
                    }
                }
            }

            //try naive reader
            //toCorrected = GotoBar(to, from, 2);
            //var ReaderNaive = new BarSymbolReaderNaive(6, 11, _patterns);
            //row = ReaderNaive.Read(Scan.GetPixels(from, toCorrected), out error, out maxError, out confidence);
            ////var row = ReaderNaive.Read(Scan.GetPixels(from, to, 1.4f), out error, out maxError, out confidence);
            ////row = ReaderNaive.Read(Scan.GetPixels3Lines(from, to, 1.4f, 3.5f), out error, out maxError, out confidence);

            //if (confidence >= MinConfidence && maxError < 0.5f)
            //    if (confidence > region.Confidence)
            //        if (Decode(region, row)) //try to decode
            //        {
            //            region.Confidence = confidence;
            //            return true;
            //        }

            return false;
        }

        private MyPoint GotoBar(MyPoint from, MyPoint to, int barsCount)
        {
            var br = new Bresenham(from, to);
            //go to first black bar
            while (!br.End())
            {
                if (Scan.isBlack(br.Current))
                    break;
                br.Next();
            }

            //skip barsCount
            var isBlack = true;
            while (!br.End() && barsCount > 0)
            {
                if (Scan.isBlack(br.Current) ^ isBlack)
                {
                    barsCount--;
                    isBlack = !isBlack;
                }
                br.Next();
            }

            return br.Current;
        }

        #region Decoders

        //Method to decode bytecodes into the final string.
        internal override bool Decode(BarCodeRegion r, int[] row)
        {
            int offset = 0;
            int end = 0; while (end < row.Length && row[end++] != Stop) ;
            if (row[end - 1] != Stop) { end++; offset = 1; }
            int[] rawData = new int[end];
            Array.Copy(row, rawData, end - offset);
            if (offset == 1) rawData[end - 1] = Stop;

            if (rawData == null || rawData.Length < 3) return false;
            if (!verifyCheckSum(rawData)) return false;
            int currentCodeSet;
            switch (rawData[0])
            {
                case StartA:
                    currentCodeSet = CodeA;
                    break;
                case StartB:
                    currentCodeSet = CodeB;
                    break;
                case StartC:
                    currentCodeSet = CodeC;
                    break;
                default:
                    // shouldn't happen :-)
                    return false;
            }

            StringBuilder sb = new StringBuilder();
            bool gotShift = false;

            for (int i = 1; i < rawData.Length - 2; i++)
            {
                bool shiftInEffect = gotShift;
                gotShift = false;

                int symbol = rawData[i];

                switch (currentCodeSet)
                {
                    case CodeA:
                        if (symbol < 64)
                        {
                            sb.Append((char)(symbol + ' '));
                        }
                        else if (symbol < 96)
                        {
                            sb.Append((char)(symbol - 64));
                        }
                        else
                        {
                            switch (symbol)
                            {
                                case FNC1: sb.Append("<FNC1>"); break;
                                case FNC2: sb.Append("<FNC2>"); break;
                                case FNC3: sb.Append("<FNC3>"); break;
                                case FNC4A: sb.Append("<FNC4A>"); break;

                                case Shift:
                                    gotShift = true;
                                    currentCodeSet = CodeB;
                                    break;

                                case CodeB:
                                    currentCodeSet = CodeB;
                                    break;

                                case CodeC:
                                    currentCodeSet = CodeC;
                                    break;
                            }
                        }
                        break;

                    case CodeB:
                        if (symbol < 96)
                        {
                            sb.Append((char)(symbol + ' '));
                        }
                        else
                        {
                            switch (symbol)
                            {
                                case FNC1: sb.Append("<FNC1>"); break;
                                case FNC2: sb.Append("<FNC2>"); break;
                                case FNC3: sb.Append("<FNC3>"); break;
                                case FNC4B: sb.Append("<FNC4B>"); break;

                                case Shift:
                                    gotShift = true;
                                    currentCodeSet = CodeC;
                                    break;

                                case CodeA:
                                    currentCodeSet = CodeA;
                                    break;

                                case CodeC:
                                    currentCodeSet = CodeC;
                                    break;
                            }
                        }
                        break;

                    case CodeC:
                        if (symbol < 100)
                        {
                            if (symbol < 10)
                                sb.Append('0');

                            sb.Append(symbol);
                        }
                        else
                        {
                            switch (symbol)
                            {
                                case FNC1: sb.Append("<FNC1>"); break;

                                case CodeA:
                                    currentCodeSet = CodeA;
                                    break;

                                case CodeB:
                                    currentCodeSet = CodeB;
                                    break;
                            }
                        }
                        break;
                }

                if (shiftInEffect)
                {
                    switch (currentCodeSet)
                    {
                        case CodeA:
                            currentCodeSet = CodeC;
                            break;
                        case CodeB:
                            currentCodeSet = CodeA;
                            break;
                        case CodeC:
                            currentCodeSet = CodeB;
                            break;
                    }
                }
            }

            if (sb.Length == 0)
                return false;

            r.Data = new ABarCodeData[] { new StringBarCodeData(sb.ToString()) };
            return true;
        }

        private bool verifyCheckSum(int[] symbols)
        {
            if (symbols.Length < 3)
            {
                // at least StartX, Stop and checksum should be in every Code128 code
                return false;
            }

            int total = symbols[0];
            for (int i = 1; i < symbols.Length - 2; i++)
                total += symbols[i] * i;

            return (total % 103) == symbols[symbols.Length - 2];
        }

        #endregion

    }
}
