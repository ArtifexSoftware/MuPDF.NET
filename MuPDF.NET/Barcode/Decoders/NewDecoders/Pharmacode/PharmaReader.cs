using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Pharmacode
{
    //Main class to scan an image looking for pharma barcodes. It inherits from Reader2DNoise,
    //responsible to find start patterns. 
#if CORE_DEV
    public
#else
    internal
#endif
    class PharmaReader : Reader2DNoise
    {
        public PharmaReader()
        {
            //Define start patterns. Pharma has no start patterns, but has only 2 chars and must be
            //3 chars length at least. So, start patterns are defined as any of the 8 possibilities.
            startPatterns = _startPatterns;
            stopPatterns = null;
            useE = false;
        }

        //Method to scan a region. It starts with a scanning the line in the middle of the region
        //to be able to read quickly good quality barcodes. If it fails, then uses the slower
        //loose projection reader.
        int[] nBars = new int[] {6, 6};
        int[] nModules = new int[] { 11, 11 };
        int[] tIndexs = new int[] { 0, 1 };
        int[][][] tables = new int[][][] { _startPatterns, _patterns };
        float[] coefs=new float[]{0.5f, 0.2f, 0.35f, 0.65f, 0.8f};

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Pharmacode;
		}

        override protected BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, BarCodeRegion r, FoundPattern foundPattern)
        {
            BarCodeRegion result = null;
            BarSymbolReader reader = new BarSymbolReader(scan, 2, new int[] { 3, 5 }, false, true, foundPattern.moduleLength, _patterns, useE, null);
            // setting min match difference
            reader.MinMatchDifference = 1E-7f;
            MyVector vd=MyVector.Zero;
            int[] resultRow=null;
            int count=0;

            r.Confidence = 0f;
            foreach (float f in coefs)
            {
                MyPoint a = r.A * f + r.D * (1f-f);
                MyPoint b = r.B * f + r.C * (1f-f);
                float error, maxError, confidence;
                MyPoint last;
                int[] row = reader.Read(a, (b - a), out error, out maxError, out confidence, out last); r.Confidence += confidence;
                if (row != null && row.Length > 3)
                    if (resultRow == null)
                    {
                        resultRow = row;
                        count = 1;
                        vd = last - a;
                    }
                    else if (row.Length == resultRow.Length)
                    {
                        bool equals = true;
                        for (int i = 0; i < row.Length && equals; i++)
                            if (row[i] != resultRow[i]) equals = false;
                        if (equals) count++;
                    }
            }

            if (count>=3)
                if (Decode(r, resultRow))
                {
                    r.B = r.A + vd;
                    r.C = r.D + vd;
                    r.Confidence /= (float)coefs.Length;
                    result = r; 
                }
            return result;
        }

        bool Decode(BarCodeRegion r, int[] row)
        {
            int v = 0;
            string s = "";
            for (int i = 0; i < row.Length; i++)
            {
                v += row[row.Length - 1 - i] == 0 ? (int)Math.Pow(2, i) : (int)Math.Pow(2, i + 1);
                s += row[i];
            }
            string msg = Convert.ToString(v);
#if DEBUG
            //msg=s+"-->"+msg;
#endif
            r.Data = new ABarCodeData[] { new StringBarCodeData(msg)};
            return true;
        }

        static readonly int[][] _startPatterns = new int[][] { 
            new int[] {1,2,1,2,1,2 }, 
            new int[] {1,2,1,2,3,2 }, 
            new int[] {1,2,3,2,1,2 }, 
            new int[] {1,2,3,2,3,2 }, 
            new int[] {3,2,1,2,1,2 }, 
            new int[] {3,2,1,2,3,2 }, 
            new int[] {3,2,3,2,1,2 }, 
            new int[] {3,2,3,2,3,2 }
        };

        static readonly int[][] _patterns = new int[][] { 
            new int[] { 1,2 }, new int[] { 3,2}};
    }
}