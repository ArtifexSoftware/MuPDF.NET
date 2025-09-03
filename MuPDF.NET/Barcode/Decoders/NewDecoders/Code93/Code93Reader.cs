using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Code93
{
    //Main class to scan an image looking for code93 barcodes. It inherits from Reader2DNoise,
    //responsible to find start and stop patterns in the image, and calling FindBarcode methods
    //to read the barcode in between.
#if CORE_DEV
    public
#else
    internal
#endif
    class Code93Reader : Reader2DNoise
    {
        public Code93Reader()
        {
            //Define start and stop patterns. Code93 has one unique start/stop pattern.
            startPatterns = stopPatterns = _startStopPatterns;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Code93;
		}

        //Method to scan a single row 
        protected override BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, MyPoint start, MyPoint end, FoundPattern foundPattern)
        {
            BarSymbolReader reader = new BarSymbolReader(scan, 6, 9, true, true, foundPattern.moduleLength, _patterns, -1, false, null);
            float error, maxError, confidence;
            int[] row = reader.Read(start, end, out error, out maxError, out confidence);
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
        int[] nModules = new int[] { 9, 9 };
        int[] tIndexs = new int[] { 0, 1 };
        int[][][] tables = new int[][][] { _startStopPatterns, _patterns };
        override protected BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, BarCodeRegion r, FoundPattern foundPattern)
        {
            BarSymbolReader reader = new BarSymbolReader(scan, nBars, nModules, tIndexs, true, true, foundPattern.moduleLength, tables, false, null);
            MyPoint a = r.A * 0.5f + r.D * 0.5f;
            MyPoint b = r.B * 0.5f + r.C * 0.5f;
            float error, maxError, confidence;
            int[] row = reader.Read(a, b, out error, out maxError, out confidence); r.Confidence = confidence;
            if (Decode(r, row)) 
                return r; //if sampling is good enough, then use simple row scanning

            BarSymbolReaderLooseProjection reader2 = new BarSymbolReaderLooseProjection(scan, 6, 9, true, _patterns, Stop, true, useE);
            float[] widths = new float[] { 0.5f, 0.9f };
            foreach (float w in widths)
            {
                row = reader2.Read(r, foundPattern.moduleLength, w);
                if (Decode(r, row)) 
                    return r;
            }

            return null;
        }

        //Method to decode bytecodes into the final string.
        protected bool Decode(BarCodeRegion r, int[] rawData)
        {
            if (rawData == null) return false;

            int end = rawData.Length-1; while (end>0 && rawData[end] != Stop) end--;
            int[] row = new int[end+1];
            Array.Copy(rawData, row, end+1);

            if (row != null && row.Length > 4) //start + 2 checksum + stop
                if (checkChecksum(row))
                {
                    string s = "";;
                    for (int i = 1; i < row.Length - 3; i++)
                        if (row[i] >= 0 && row[i] < _patterns.Length)
                            if (row[i] < 43) s += _alphabet.Substring(row[i], 1);
                            else if (i < row.Length - 4) switch (row[i])
                                {
                                    case 43: s += "["+tableA[row[i + 1] - 10]+"]"; i++; break;
                                    case 44: s += tableB[row[i + 1] - 10]; i++; break;
                                    case 45: s += tableC[row[i + 1] - 10]; i++; break;
                                    case 46: s += (char)(row[i + 1] - 10 + (int)'a'); i++; break;
                                    default: s += "(" + _alphabet.Substring(row[i], 1) + ")"; break;
                                }
                            else s += "(" + _alphabet.Substring(row[i], 1) + ")";
                        else s += "[???]";
                    r.Data = new ABarCodeData[] { new StringBarCodeData(s) };
                    return true;
                }
            return false;
        }

        private static bool checkChecksum(int[] row)
        {
            return checkChecksum(row, row.Length - 3, 20) && checkChecksum(row, row.Length - 2, 15);
        }

        private static bool checkChecksum(int[] row, int maxPos, int maxW)
        {
            int w = 1;
            int sum = 0;
            int i=maxPos-1;
            while (i>=0) 
            {
                sum += w * row[i--];
                if (w++ == maxW) w = 1;
            }
            return row[maxPos] == sum % 47;
        }

        static readonly int[][] _startStopPatterns =  new int[][] { new int[] { 1, 1, 1, 1, 4, 1 } };

        static readonly string _alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%$%/+*********";
        static readonly string[] tableA = new string[] { "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL", "BS", "HT", "LF", "VT", "FF", "CR", "SO", "SI", "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB" };
        static readonly string[] tableB = new string[] { "[ESC]", "[FS]", "[GS]", "[RS]", "[US]", ";", "<", "=", ">", "?", "[", "\\", "]", "^", "_", "{", "[???]", "}", "~", "[DEL]", "[NUL]", "@", "`", "[DEL]", "[DEL]", "[DEL]" };
        static readonly string[] tableC = new string[] { "!", "\"", "#", "$", "%", "&", "'", "(", ")", "*", "+", ",", "[???]", "[???]", "/", "[???]", "[???]", "[???]", "[???]", "[???]", "[???]", "[???]", "[???]", "[???]", "[???]", ":" };

        static readonly int Stop = 47;

        static readonly int[][] _patterns = new int[][] { 
            new int[]{1,3,1,1,1,2}, //0
            new int[]{1,1,1,2,1,3},
            new int[]{1,1,1,3,1,2},
            new int[]{1,1,1,4,1,1},
            new int[]{1,2,1,1,1,3},
            new int[]{1,2,1,2,1,2},
            new int[]{1,2,1,3,1,1},
            new int[]{1,1,1,1,1,4},
            new int[]{1,3,1,2,1,1},
            new int[]{1,4,1,1,1,1},
            new int[]{2,1,1,1,1,3},//10
            new int[]{2,1,1,2,1,2},
            new int[]{2,1,1,3,1,1},
            new int[]{2,2,1,1,1,2},
            new int[]{2,2,1,2,1,1},
            new int[]{2,3,1,1,1,1},
            new int[]{1,1,2,1,1,3},
            new int[]{1,1,2,2,1,2},
            new int[]{1,1,2,3,1,1},
            new int[]{1,2,2,1,1,2},
            new int[]{1,3,2,1,1,1},//20
            new int[]{1,1,1,1,2,3},
            new int[]{1,1,1,2,2,2},
            new int[]{1,1,1,3,2,1},
            new int[]{1,2,1,1,2,2},
            new int[]{1,3,1,1,2,1},
            new int[]{2,1,2,1,1,2},
            new int[]{2,1,2,2,1,1},
            new int[]{2,1,1,1,2,2},
            new int[]{2,1,1,2,2,1},
            new int[]{2,2,1,1,2,1},//30
            new int[]{2,2,2,1,1,1},
            new int[]{1,1,2,1,2,2},
            new int[]{1,1,2,2,2,1},
            new int[]{1,2,2,1,2,1},
            new int[]{1,2,3,1,1,1},
            new int[]{1,2,1,1,3,1},
            new int[]{3,1,1,1,1,2},
            new int[]{3,1,1,2,1,1},
            new int[]{3,2,1,1,1,1},
            new int[]{1,1,2,1,3,1},//40
            new int[]{1,1,3,1,2,1},
            new int[]{2,1,1,1,3,1},
            new int[]{1,2,1,2,2,1},
            new int[]{3,1,2,1,1,1},
            new int[]{3,1,1,1,2,1},
            new int[]{1,2,2,2,1,1},//46
            new int[]{1,1,1,1,4,1},//47 start, stop
            new int[]{1,1,4,1,1,1},//48 reversed start
            new int[]{4,1,1,1,1,1},// unused
            new int[]{1,1,1,1,3,2},//50
            new int[]{1,1,1,2,3,1},
            new int[]{1,1,3,1,1,2},
            new int[]{1,1,3,2,1,1},
            new int[]{2,1,3,1,1,1},
            new int[]{2,1,2,1,2,1}
        };
    }
}