using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Code128
{
    //Main class to scan an image looking for code128 barcodes. It inherits from Reader2DNoise,
    //responsible to find start and stop patterns in the image, and calling FindBarcode methods
    //to read the barcode in between.
#if CORE_DEV
    public
#else
    internal
#endif
    class Code128Reader : Reader2DNoise
    {
        public Code128Reader()
        {
            //Define start and stop patterns. Code128 has 3 start patterns startA, startB, startC
            //that defines the initial encoding table, and one unique stop pattern.
            startPatterns = _startPatterns;
            stopPatterns = _stopPatterns;
            useE = true;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Code128;
		}

        //Method to scan a single row 
        protected override BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, MyPoint start, MyPoint end, FoundPattern foundPattern)
        {
            BarSymbolReader reader = new BarSymbolReader(scan, 6, 11, true, true, foundPattern.moduleLength, _patterns, -1, false, null);
            float error, maxError, confidence;
            int[] row = reader.Read(start, end, out error, out maxError, out confidence);
            row[0] = startPattern % 3 + StartA;
            if (error < 1f)
            {
                BarCodeRegion r = new BarCodeRegion(start, reader.Current, reader.Current + new MyPoint(0, 5), start + new MyPoint(0, 5));
                r.Confidence = confidence;
                if (Decode(r, row)) return r;
            }
            return null;
        }

        //Method to scan a region. It starts with a scanning the line in the middle of the region
        //to be able to read quickly good quality barcodes. If it fails, then uses the slower
        //loose projection reader.
        int[] nBars = new int[] {6, 6};
        int[] nModules = new int[] { 11, 11 };
        int[] tIndexs = new int[] { 0, 1 };
        int[][][] tables = new int[][][] { _startPatterns, _patterns };
        override protected BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, BarCodeRegion r, FoundPattern foundPattern)
        {
            BarSymbolReader reader = new BarSymbolReader(scan, nBars, nModules, tIndexs, true, true, foundPattern.moduleLength, tables, useE, null);

            MyPoint a = r.A * 0.5f + r.D * 0.5f;
            MyPoint b = r.B * 0.5f + r.C * 0.5f;

            float error, maxError, confidence;
            int[] row = reader.Read(a, b, out error, out maxError, out confidence);
            r.Confidence = confidence;
            if (row != null && row.Length > 0)
            {
#if DEBUG
                var line = scan.GetPixels(a, b);
                DebugHelper.AddDebugItem("old " + a.ToString(), line, a, b);
#endif

                //add region to list of "good" regions
                if (confidence > 0.5f)
                    foundRegions.AddLast(r);
                
                //try to decode
                row[0] = startPattern % 3 + StartA;
                if (Decode(r, row)) return r; //if sampling is good enough, then use simple row scanning
            }

            //
            BarSymbolReaderLooseProjection reader2 = new BarSymbolReaderLooseProjection(scan, 6, 11, true, _patterns, 106, true, useE);
            float[] widths = new float[] { 0.5f, 0.9f };
            foreach (float w in widths)
            {
                row = reader2.Read(r, foundPattern.moduleLength, w);
                if (row != null && row.Length > 0)
                {
                    row[0] = startPattern % 3 + StartA;
                    if (Decode(r, row)) return r;
                }
            }

            return null;
        }


        //method to read barcodes without stop pattern
        protected override BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, MyPoint start, MyVectorF vd, FoundPattern foundPattern)
        {
            BarSymbolReader reader = new BarSymbolReader(scan, nBars, nModules, tIndexs, true, true, foundPattern.moduleLength, tables, useE, null, true);            
            MyPoint end;
            float error, maxError, confidence;
            int[] row = reader.Read(start, vd, out error, out maxError, out confidence, out end); ;
            if (row != null && row.Length > 0)
            {
                row[0] = startPattern % 3 + StartA;
                if (error < 1f)
                {
                    BarCodeRegion r = new BarCodeRegion(start, reader.Current, reader.Current + new MyPoint(0, 5), start + new MyPoint(0, 5));
                    r.Confidence = confidence;
                    if (Decode(r, row)) return r;
                }
            }
            return null;
        }

        //Method to decode bytecodes into the final string.
        protected bool Decode(BarCodeRegion r, int[] row)
        {
            int offset=0;
            int end = 0; while (end < row.Length && row[end++] != Stop);
            if (row[end - 1] != Stop) { end++; offset = 1; }
            int[] rawData = new int[end];
            Array.Copy(row, rawData, end-offset);
            if (offset==1) rawData[end - 1] = Stop;

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
            {
                if (symbols[i] == -1)
                    return false;

                total += symbols[i] * i;
            }

            return (total % 103) == symbols[symbols.Length - 2];
        }

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

        static readonly int[][] _startPatterns =  new int[][] { 
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
    }
}