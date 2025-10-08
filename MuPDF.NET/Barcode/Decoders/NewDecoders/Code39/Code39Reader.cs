using System;
using System.Text;
using BarcodeReader.Core.Common;
using SkiaSharp;

namespace BarcodeReader.Core.Code39
{
    //Main class to scan an image looking for code39 barcodes. It inherits from Reader2DNoise,
    //responsible to find start and stop patterns in the image, and calling FindBarcode methods
    //to read the barcode in between.
#if CORE_DEV
    public
#else
    internal
#endif
    class Code39Reader : Reader2DNoise
    {
        /// <summary>
        /// do decode extended Code 39 symbols or not (disabled by default)
        /// </summary>
        protected bool DoDecodeExtended = false;
        
        public enum Mode { None, CheckMod43Checksum, CheckMod11ChecksumPZN8, CheckMod11ChecksumISBN10, CheckMod11ChecksumUPU };
        public Mode CheckSumMode = Mode.None;

        /// <summary>
        /// Enable recognition of barcodes with space between codewords
        /// It is useful to read barcodes drawn with font
        /// </summary>
        public bool AllowToRecognizeFontBarcode = true;

        public Code39Reader()
        {
            //Define start and stop patterns. Code39 has an unique start/stop pattern, but
            //we define 3 different ratios for narrow&wide bars 1:2 (the normal), 1:3, or 1:4
            //This allows to detect more start patterns, than using only 1:2 ratio.
            startPatterns = stopPatterns = new int[][] { new int[] { 1, 2, 1, 1, 2, 1, 2, 1, 1 } , new int[] { 1, 3, 1, 1, 3, 1, 3, 1, 1 }};//, new int[] { 1, 4, 1, 1, 4, 1, 4, 1, 1 } };
            useE = true;
            singlePattern = true;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Code39;
		}

        protected override void TrackEdge(MyPoint left, MyPoint right, float moduleLength, out MyPointF up, out MyPointF down)
        {
            EdgeTrack et = new EdgeTrack(scan);

            //first try using the first bar (thin bar)
            et.Track(left, new MyVector(-1, 0), moduleLength, true);

            //find the top and bottom points of the edge
            MyPointF fup = et.Up();
            MyPointF fdown = et.Down();


            MyPoint mid = (left + right) / 2;
            et.Track(mid, new MyVector(-1, 0), moduleLength, true);

            //find the top and bottom points of the edge
            up = et.Up();
            down = et.Down();

            if ((up - down).Length > (fup - fdown).Length)
            {
                //Calculate main directions
                MyVectorF vdY = (up - down);
                vdY = vdY.Normalized;
                MyVectorF vdX = new MyVectorF(-vdY.Y, vdY.X);

                //Calculate rotated module length
                float cosAngle = (float)Math.Cos(vdX.Angle);
                if (cosAngle < barcodeMaxCosAngle) cosAngle = 1f; //invalid angle

                float rotatedModuleLength = moduleLength * cosAngle * cosAngle; //projected module length X axis

                up -= vdX * rotatedModuleLength * 5f;
                down -= vdX * rotatedModuleLength * 5f;
            }
            else
            {
                up = fup;
                down = fdown;
            }
        }

        //Method to scan a single row 
        protected override BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, MyPoint start, MyPoint end, FoundPattern foundPattern)
        {
            BarCodeRegion result = null;
            BarSymbolReader reader = new BarSymbolReader(scan, 10, 13, false, true, foundPattern.moduleLength, _patterns, -1, false, null);
            float error, maxError, confidence;

            int[] row = reader.Read(start, end, out error, out maxError, out confidence);
            if (row!=null && row.Length>2 && row[0]==43 &&  row[row.Length-1]==43 && error < 1f)
            {
                BarCodeRegion r = new BarCodeRegion(start, reader.Current, reader.Current + new MyPoint(0, 5), start + new MyPoint(0, 5));
                r.Confidence = confidence;
                if (Decode(r, row)) result= r;
            }
            
            return result;
        }

        bool IsCodewordCountAround(FoundPattern pattern, int actual, int expected)
        {
            var isFontBarcode = pattern.lastModule > pattern.moduleLength * 3;

            if (isFontBarcode && AllowToRecognizeFontBarcode)
            {
                return actual < expected && expected - actual < 5 || actual >= expected && actual - expected < 2;
            }
            else
            {
                return actual < expected && expected - actual < 2 || actual >= expected && actual - expected < 2;
            }
        }

        //Method to scan a region. It starts with a scanning the line in the middle of the region
        //to be able to read quickly good quality barcodes. If it fails, then uses the slower
        //loose projection reader.
        override protected BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, BarCodeRegion r, FoundPattern foundPattern)
        {
#if DEBUG
            DebugHelper.DrawRegion(SKColors.Red, r);
#endif
            BarCodeRegion result = null;
            BarSymbolReader reader = new BarSymbolReader(scan, 10, 13, false, true, foundPattern.moduleLength, _patterns, -1, false, null);
            float error, maxError,confidence;

            // setting min match difference
            reader.MinMatchDifference = 1E-7f; 

            //expected number of codewords
            int nCodewords = (int)Math.Round((r.B - r.A).Length / foundPattern.moduleLength / 13f, MidpointRounding.AwayFromZero);


            //scan different lines of region
            int[] row = null;
            float[] midPoints = new float[] {0.5f, 0.3f, 0.7f, 0.4f, 0.6f, 0.1f, 0.9f, 0.2f, 0.8f};
            bool anyFound = false;

            float bestConfidence = 0;

            foreach(var mid in midPoints)
            {
                MyPoint a = MyPointF.Lerp(r.A, r.D, mid);
                MyPoint b = MyPointF.Lerp(r.B, r.C, mid);

                row = reader.Read(a, b, out error, out maxError, out confidence);
                r.Confidence = confidence;

                if (confidence > 0.7f && row != null && maxError < 3f * error && row[0] == 43 && row[row.Length - 1] == 43 && IsCodewordCountAround(foundPattern, row.Length, nCodewords))
                    if (Decode(r, row))
                    {
                        if (r.Confidence > bestConfidence)//find best confidence
                        {
                            result = r; //if sampling is good enough, then use simple row scanning
                            bestConfidence = r.Confidence;
                        }
                    }

                if (row != null && row.Length >= 3) anyFound = true;
            }

            //if found - return
            if (result != null)
            {
                return result;
            }

            //
            //scan of region is failed :(
            //will use BarSymbolReaderLooseProjection...
            //

            if (!anyFound) return null;

            BarSymbolReaderLooseProjection reader2 = new BarSymbolReaderLooseProjection(scan, 10, 13, true, _patterns, 43, false, useE);
            float[] widths =  new float[]{0.5f, 0.9f};

            // setting min match difference
            reader2.MinMatchDifference = 1E-7f; 

            foreach (float w in widths)
            {
                row = reader2.Read(r, foundPattern.moduleLength, w);
                if (row != null && row.Length > 2 && row[0] == 43 && row[row.Length - 1] == 43 && IsCodewordCountAround(foundPattern, row.Length, nCodewords))
                    if (Decode(r, row)) { result = r;  break; }
#if DEBUG
                /*string s = "";
                for (int i = 0; i < row.Length; i++) s += row[i] + ",";
                Debug.WriteLine(":" + r + "-->" + w + "-->" + s);*/
#endif
            }

            return result;
        }

#if OLD_FindBarcode
        //Method to scan a region. It starts with a scanning the line in the middle of the region
        //to be able to read quickly good quality barcodes. If it fails, then uses the slower
        //loose projection reader.
        override protected BarCodeRegion FindBarcode(ImageScaner scan, int startPattern, BarCodeRegion r, FoundPattern foundPattern)
        {
            BarCodeRegion result = null;
            BarSymbolReader reader = new BarSymbolReader(scan, 10, 13, false, true, foundPattern, _patterns, -1, false, null);
            MyPoint a = r.A * 0.5f + r.D * 0.5f;
            MyPoint b = r.B * 0.5f + r.C * 0.5f;
            float error, maxError, confidence;

            // setting min match difference
            reader.MinMatchDifference = 1E-7f;

            //expected number of codewords
            int nCodewords = (int)Math.Round((r.B - r.A).Length / foundPattern.moduleLength / 13f, MidpointRounding.AwayFromZero);

            int[] row = reader.Read(a, b, out error, out maxError, out confidence); r.Confidence = confidence;
            if (confidence > 0.7f && row != null && maxError < 3f * error && row[0] == 43 && row[row.Length - 1] == 43 && Around(row.Length, nCodewords))
                if (Decode(r, row)) result = r; //if sampling is good enough, then use simple row scanning

            if (result != null) return result;

            if (row == null || row.Length < 3) return null;
            //int c = 0; for (int i = 0; i < row.Length; i++) if (row[i] != -1) c++;
            //if (c < row.Length / 4) return null;

            BarSymbolReaderLooseProjection reader2 = new BarSymbolReaderLooseProjection(scan, 10, 13, true, _patterns, 43, false, useE);
            float[] widths = new float[] { 0.5f, 0.9f };

            // setting min match difference
            reader2.MinMatchDifference = 1E-7f;

            foreach (float w in widths)
            {
                row = reader2.Read(r, foundPattern.moduleLength, w);
                if (row != null && row.Length > 2 && row[0] == 43 && row[row.Length - 1] == 43 && Around(row.Length, nCodewords))
                    if (Decode(r, row)) { result = r; break; }
#if DEBUG
                /*string s = "";
                for (int i = 0; i < row.Length; i++) s += row[i] + ",";
                Debug.WriteLine(":" + r + "-->" + w + "-->" + s);*/
#endif
            }

            return result;
        }

#endif

        //Method to decode bytecodes into the final string.
        protected virtual bool Decode(BarCodeRegion r, int[] row) { return Decode(r, row, 1, row!=null?row.Length - 1:0); }
        protected virtual bool Decode(BarCodeRegion r, int[] row, int iStart, int iEnd) 
        {
            string pre = "";
            if (row != null && row.Length > 2)
            {
                if (CheckSumMode==Mode.CheckMod43Checksum) 
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
                    int[] weights = new int[] { 1,2,3,4,5,6,7 };
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
                        for (int i = iStart; i < iEnd; i++) sum += row[i] * weights[i - iStart+1];
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

                string s = pre;
                for (int i = iStart; i < iEnd; i++)
                    if (row[i] >= 0 && row[i] < _patterns.Length)
                        s += _alphabet.Substring(row[i], 1);
                    else return false;

                if (DoDecodeExtended)
                    s = DecodeExtended(s);
                r.Data = new ABarCodeData[] { new StringBarCodeData(s) };
                return true;
            }
            return false;
        }

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
