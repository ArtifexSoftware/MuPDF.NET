using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.MSI
{
    //MSI have 6 different checksums algorithms. By default, ALL are tried.
#if CORE_DEV
    public
#else
    internal
#endif
    enum MSIChecksum { ALL, MOD10, MOD11_IBM, MOD11_NCR, MOD1010, MOD1110_IBM, MOD1110_NCR };

    //Main class to scan an image looking for code39 barcodes. It inherits from Reader2DNoise,
    //responsible to find start and stop patterns in the image, and calling FindBarcode methods
    //to read the barcode in between.
#if CORE_DEV
    public
#else
    internal
#endif
    class MSIReader : Reader2DNoise
    {
        MSIChecksum mode = MSIChecksum.ALL;
        public MSIChecksum Mode { get { return mode; } set { mode = value; } }

        public MSIReader()
        {
            //Define start and stop patterns. 
            startPatterns = _largeStartPatterns;
            stopPatterns = _stopPatterns;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.MSI;
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
        int[] nBars = new int[] {2, 8};
        int[] nModules = new int[] { 3, 12 };
        int[] tIndexs = new int[] { 0, 1 };
        int[][][] tables = new int[][][] { _startPatterns, _patterns };
        override protected BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, BarCodeRegion r, FoundPattern foundPattern)
        {
            BarSymbolReader reader = new BarSymbolReader(scan, nBars, nModules, tIndexs, true, true, foundPattern.moduleLength, tables, false, null);
            MyPoint a = r.A * 0.5f + r.D * 0.5f;
            MyPoint b = r.B * 0.5f + r.C * 0.5f;
            float error, maxError, confidence;

            int[] row = reader.Read(a, b, out error, out maxError, out confidence); r.Confidence = confidence;
            if (Decode(r, row)) 
                return r; //if sampling is good enough, then use simple row scanning

            BarSymbolReaderLooseProjection reader2 = new BarSymbolReaderLooseProjection(scan, nBars, nModules, true, tables, -1, true, useE);
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

            int end = rawData.Length-1; while (end>0 && rawData[end] == -1) end--;
            int n = 0; for (int i = end; i >= 0; i--) if (rawData[i] == -1) n++;
            if (n==0 && end > 1) //minimun 1checkdigit
            {
                int[] row = new int[end];
                Array.Copy(rawData, 1, row, 0, end);

                if (row != null && row.Length > 1) //1 checksum 
                {
                    bool valid = false;
                    if (mode==MSIChecksum.ALL || mode==MSIChecksum.MOD10) valid|=checkChecksum10(row, row.Length);
                    if (mode==MSIChecksum.ALL || mode==MSIChecksum.MOD11_IBM) valid|=checkChecksum11(row, row.Length,7);
                    if (mode==MSIChecksum.ALL || mode==MSIChecksum.MOD11_NCR) valid|=checkChecksum11(row, row.Length,9);
                    if (mode==MSIChecksum.ALL || mode==MSIChecksum.MOD1010) valid|=checkChecksum1010(row);
                    if (mode==MSIChecksum.ALL || mode==MSIChecksum.MOD1110_IBM) valid|=checkChecksum1110(row, 7);
                    if (mode==MSIChecksum.ALL || mode==MSIChecksum.MOD1110_NCR) valid|=checkChecksum1110(row, 9);
                    if (valid)
                    {
                        string s = "";
                        for (int i = 0; i < row.Length - 1; i++) s += row[i];
                        r.Data = new ABarCodeData[] { new StringBarCodeData(s) };
                        return true;
                    }
                    else if (row.Length > 4) // trying to recover if the value is larger than 4 symbols
                    {
                        // if check digit is not valid but we still will try to provide the output value
                        // but lowering the confidence by 0.3                        
                        // see TestCase/msi/
                        r.Confidence *= 0.3f;
                        string s = "";
                        for (int i = 0; i < row.Length - 1; i++) s += row[i];
                        // adding the check digit
                        //s += "(" + row[row.Length - 1] + ")";
                        r.Data = new ABarCodeData[] { new StringBarCodeData(s) };
                        return true;

                    }
                }
            }
            return false;
        }

        private static bool checkChecksum10(int[] row, int N)
        {
            int sum = 0;
            for (int i = N-2; i >= 0; i--)
            {
                int d = row[i];
                if ((N - i) % 2 == 0)
                {
                    d *= 2;
                    if (d > 9) d = 1 + d % 10;
                }
                sum += d;
            }
            sum *= 9;
            int checkDigit = sum % 10;
            return checkDigit == row[N-1];
        }

        //limit can be 7 for IBM modulo11, or 9 for NCR modulo 11.
        private static bool checkChecksum11(int[] row, int N, int limit)
        {
            int weight = 2;
            int sum = 0;
            for (int i = N - 2; i >= 0; i--) {
                sum += row[i] * weight;
                if (++weight > limit) weight = 2;
            }
            int checkDigit = (11 - (sum % 11)) % 11;
            return checkDigit==row[N-1];
        }

        private static bool checkChecksum1010(int[] row)
        {
            return checkChecksum10(row, row.Length-1) && checkChecksum10(row, row.Length);
        }

        private static bool checkChecksum1110(int[] row, int limit)
        {
            return checkChecksum11(row, row.Length - 1, limit) && checkChecksum10(row, row.Length);
        }

        static readonly int[][] _largeStartPatterns = new int[][] 
        { 
            new int[]{2,1,1,2,1,2}, //0
            new int[]{2,1,1,2,2,1}, //4
            new int[]{2,1,2,1,1,2} //8
        };
        static readonly int[][] _startPatterns = new int[][] { new int[] { 2, 1 } };
        static readonly int[][] _stopPatterns = new int[][] { new int[] { 1, 2, 1 } };

        static readonly int[][] _patterns = new int[][] { 
            new int[]{1,2,1,2,1,2,1,2}, //0
            new int[]{1,2,1,2,1,2,2,1}, //1
            new int[]{1,2,1,2,2,1,1,2}, //2
            new int[]{1,2,1,2,2,1,2,1}, //3
            new int[]{1,2,2,1,1,2,1,2}, //4
            new int[]{1,2,2,1,1,2,2,1}, //5
            new int[]{1,2,2,1,2,1,1,2}, //6
            new int[]{1,2,2,1,2,1,2,1}, //7
            new int[]{2,1,1,2,1,2,1,2}, //8
            new int[]{2,1,1,2,1,2,2,1}, //9
        };

    }
}
