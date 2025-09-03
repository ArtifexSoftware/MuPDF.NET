using System;
using System.Collections;

namespace BarcodeReader.Core.Common
{
    //Abstract class to find 4-state or 2-state barcodes (RM, IM, KIX, PostCode, PostNet)
    //Extends Reader2D, a generic 4state barcode reader
    //Finder: 4 or 2-state barcodes have not a pattern, but the track region can be used as a pattern. 
    //Large patterns are very sensible to rotations (can only be detected for small rotations), thus 
    //we look for a small part of the track region. Exactly 5 black + 5 white bars. This allows to detect
    //barcodes rotated +-10º.
    //Once found, we still need to detect the rotation of the barcode. Since there is not a main edge 
    //in the barcode, and because track region has short bars, detecting the rotation based on edge 
    //detection fails easily. Instead, we trace lines at different angles (0..10º every 1º) counting
    //bars for each angle. If we are able to count as many bars as the length of the barcode, we
    //have found the rotation of the barcode. Then we are ready to read and decode it.
#if CORE_DEV
    public
#else
    internal
#endif
    abstract class TwoFourStateBarcodesReader: Reader2D
    {
        //Number of elements of a finder: 5 black bars + 5 white spaces
        //Actually, does not search a finder, but a track region (i.e. 101010101010)
        protected static readonly int finderElements = 10;

        //All bars in the finder must be more or less of the same width. This is the max error of their widths.
        protected float finderMaxWidthError = 0.3f;

        //When check for start quiet zone, check 2 * modules with white pixels
        protected float startPatternQuietZone = 2f;

        //Max scan angle in order to find the line crossing the whole barcode
        float scanMaxAngle = (float)Math.PI / 4f;

        //Max and Min allowed difference between width bars of the finder and the rest of bars of the barcode
        float maxBarWidthDifference = 0.2f;
        float minBarWidthDifference = 2f;

        //Max number of black pixels (noise) accepted when finding a white line on top/bottom of the barcode to find the barcode height
        int whiteLineMaxNoise = 5;

        //Max offset difference between consecutive bars
        float maxDifferenceBetweenBarOffsets = 0.1f;

        //If the larger bar is too short compared to the bounding box heigth then reject the barcode
        float maxDifferenceBetweenLargerBarAndBoundingBox = 0.75f;

        //Tri and four state bar length thresholds, to decide if a bar is a large or shor bar.
        float fourStateHeightThreshold = 0.33f;
        float triStateHeightThreshold = 0.5f;

        //Tri state barcodes have all bars aligned to the base. This is the max allowed error.
        float maxBaseAlignmentError = 0.25f;
        
        //methods defined in derived classes
        protected abstract bool IsFourState(); //true for four state barcodes, false for two state barcodes
        protected abstract bool CheckNBars(int nBars);
        protected abstract bool CheckRatio(float ratio); //checks if ratio is in the expected range
        protected abstract IDecoderAscDescBars GetDecoder();

        protected override int getFinderNElements() { return finderElements; }

        Hashtable h = new Hashtable();
        int[] w = new int[finderElements / 2];

        //Method to check if widths are a valid finder for four-state or two-state barcodes
        //Actually, does not search a finder, but a track region (i.e. 101010101010)
        protected override bool IsFinder(int[] widths, out float meanWidth)
        {
            //calculate 2 bars widths (to compensate white-black image brightness)
            for (int i = 0, j = 0; i < finderElements; i += 2, j++) w[j] = widths[i] + widths[i + 1];

            //search the median width bar (not the mean!)
            h.Clear();
            for (int i = 0; i < finderElements / 2; i++)
                if (h.ContainsKey(w[i])) { int n = (int)h[w[i]]; h[w[i]] = (n + 1); }
                else h[w[i]] = 1;
            int m = 0, mid = finderElements / 4;
            float mean = -1f;
            foreach (int i in h.Keys)
            {
                m += (int)h[i];
                if (m >= mid) { mean = (float)i; break; }
            }

            //check if all bars are 1 module length. Otherwise return false.
            int numModules = 0;
            meanWidth = 0f;
            for (int i = 0; i < finderElements / 2; i++)
            {
                meanWidth += (float)w[i];
                double proportional = ((double)w[i]) / mean;
                if (!Calc.Around((float)proportional, 1f, finderMaxWidthError)) return false;
                numModules++;
            }
            meanWidth /= (float)numModules;
            return true;
        }


        //check if the left side of the finder is white area, for 2 modules (meanWidth=1 module length)
        protected override bool LeftFree(int x0, int y, float meanWidth)
        {
            XBitArray row = BWImage.GetRow(y);
            int d = (int)(meanWidth * startPatternQuietZone);
            for (int x = x0 - 1, i = 0; i < d && x >= 0; i++, x--) if (!(row[x] ^ startsWithBlack)) return false;
            return true;
        }


        //check if pixel x in r is black
        int isBlack(XBitArray r, int x)
        {
            if (r == null || x < 0 || x >= r._size) return 0; //defaults to white
            return r[x] ? 1 : 0;
        }

        //Algorithm to detect the rotation of the barcode. Traces several lines at 0..10º each 1º 
        //looking for CheckNBars bars. Seems slow, but works pretty fast.
        protected override BarCodeRegion FindBarcode(int x0, int y, float meanWidth)
        {

            //first estimate gradient to reject not vertical -10..10º edges
            XBitArray prevRow = (y == 0 ? null : BWImage.GetRow(y - 1));
            XBitArray row = BWImage.GetRow(y);
            XBitArray nextRow = (y == BWImage.Height - 1 ? null : BWImage.GetRow(y + 1));

            int dy = isBlack(prevRow, x0 - 1) + 2 * isBlack(prevRow, x0) + isBlack(prevRow, x0 + 1) -
                isBlack(nextRow, x0 - 1) - 2 * isBlack(nextRow, x0) - isBlack(nextRow, x0 + 1);

            //double angle = Math.Atan2((double)dy, (double)dx);
            if (dy < -1 || dy > 1) return null;

            //trace several lines at 0..10º each 1º and, if not found, the same for 0..-10º
            float incAngle = (float)Math.PI / 360f;
            MyPoint pIn = new MyPoint(x0, y), pEnd;
            
            float angle = 0f;
            int nBars = countBars(pIn, meanWidth, startsWithBlack, angle, out pEnd);
            int nBars0 = nBars;
            while (!CheckNBars(nBars) && nBars >= nBars0 && angle < scanMaxAngle)
            {
                angle += incAngle;
                nBars = countBars(pIn, meanWidth, startsWithBlack, angle, out pEnd);
            }
            if (!CheckNBars(nBars))
            {
                // check timeout
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();

                angle = 0f;
                nBars = countBars(pIn, meanWidth, startsWithBlack, angle, out pEnd);
                while (!CheckNBars(nBars) && nBars >= nBars0 && angle > -scanMaxAngle)
                {
                    angle -= incAngle;
                    nBars = countBars(pIn, meanWidth, startsWithBlack, angle, out pEnd);

                    // check timeout
                    if (IsTimeout())
                        throw new SymbologyReader2DTimeOutException();
                }
            }

            //if we have found all bars then read the barcode
            if (CheckNBars(nBars))
            {
                //First detect the height of the barcode, looking for a white line parallel to the track region
                MyVector incX = pEnd - pIn;
                MyVectorF vdX = new MyVectorF((float)Math.Cos(angle), -(float)Math.Sin(angle));
                MyVectorF vdY = new MyVectorF(-(float)Math.Sin(angle), -(float)Math.Cos(angle));

                Bresenham brup = new Bresenham(pIn, vdY);
                while (BWImage.In(brup.Current)) if (WhiteLine(brup.Current, incX)) break; else brup.Next();
                Bresenham brdown = new Bresenham(pIn, -vdY);
                while (BWImage.In(brdown.Current)) if (WhiteLine(brdown.Current, incX)) break; else brdown.Next();
                MyPoint pUp = brup.Current;
                MyPoint pDown = brdown.Current;

                //Now check ratio and read the barcode
                float ratio = (pEnd - pIn).Length / (pUp - pDown).Length;
                if (CheckRatio(ratio))
                {
                    BarCodeRegion region = new BarCodeRegion(pUp, pUp + incX, pDown + incX, pDown);
                    return Decode(pIn, pEnd, region, nBars);
                }
            }
            return null;
        }

        //count bars starting at pIn, with module length=meanWidth, current=true is starts with black bar,
        //and a given angle.
        //Returns the number of found bars and the end point pEnd
        //Allows noisy pixels
        private int countBars(MyPoint pIn, float meanWidth, bool current, float angle, out MyPoint pEnd)
        {
            int nBars = 0, n = 0;
            bool processing = !current;
            float margin = meanWidth * maxBarWidthDifference; 
            if (margin < minBarWidthDifference) margin = minBarWidthDifference; //min 2 pixels difference
            MyVectorF vdX = new MyVectorF((float)Math.Cos(angle), -(float)Math.Sin(angle));

            pEnd = pIn;
            Bresenham br = new Bresenham(pIn, vdX);
            while (BWImage.In(br.Current))
            {
                XBitArray row = BWImage.GetRow(br.Current.Y);
                if (row[br.Current.X] ^ processing)
                {
                    n++; //current equals processing
                }
                else //transition detected
                {
                    //n++; //move below
                    if (processing == current) //2 bars read (BW or WB)
                    {
                        if (Calc.Around(meanWidth, (float)n, margin)) n = 1;
                        else if ((float)n > meanWidth) break;
                    }
                    else
                    {
                        n++;
                        pEnd = br.Current; nBars++;
                    }
                    processing = !processing;
                }
                br.Next();

                // check timeout
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();

            }
            if (!BWImage.In(br.Current) && processing != current)
            {
                pEnd = br.Current; nBars++;
            }
            return nBars;
        }


        //check if the line starting at p in vd direction is white, or has only a few black pixels
        protected bool WhiteLine(MyPoint p, MyVector vd)
        {
            Bresenham br = new Bresenham(p, p + vd);
            int n= whiteLineMaxNoise; //allow max 5 black noisy pixels
            while (!br.End()) if (scan.isBlack(br.Current) && n-- == 0) return false;
                else br.Next();
            return true;
        }



        //region:     A------B
        //            |      |
        //            D------C
        //Sample the barcode region and calls the decoder
        protected BarCodeRegion Decode(MyPoint pIn, MyPoint pEnd, BarCodeRegion region, int nBars)
        {
            bool[][] samples = Sample(pIn, pEnd, region, nBars);
            if (samples == null) return null;
            IDecoderAscDescBars decoder = GetDecoder();
            float confidence;
            string code = decoder.Decode(samples, out confidence);
            if (code != null)
            {
                region.Data = new ABarCodeData[] { new StringBarCodeData(code) };
                region.Confidence = confidence;
            }
            else region = null;
            return region;
        }




        //Since the rotation angle is not exact, we can not sample bars in a regular way. Instead, we 
        //trace bars starting from the track region. Then we can distingish between long up, short, or 
        //long down bars, which sets the 4-state bar state.
        protected bool[][] Sample(MyPoint pIn, MyPoint pEnd, BarCodeRegion region, int N)
        {
            int iSample = 0;
            MyVector vdUp = new MyVector(0, -1), vdDown = new MyVector(0, 1);
            float[][] bars = new float[N][];
            bool[][] samples = new bool[N][];
            float max = 0f, min = 1000f, minBase = 0f;
            float length=(pIn-pEnd).Length;
            int noise = (int)Math.Round(length / (float)(2 * N - 1)); //level of accepted noise for tracking bars
            if (noise < 1) noise = 1;
            float moduleLength=2f*length/(float)(2*N-1);

            bool current = BWImage.GetRow(pIn.Y)[pIn.X];
            bool processing = !current;
            float lastD = 0f;
            Bresenham br = new Bresenham(pIn, pEnd);
            while (!br.End())
            {
                XBitArray row = BWImage.GetRow(br.Current.Y);
                if (row[br.Current.X] ^ processing)
                {
                    if (processing != current) //for each white-black transition trace vertical up and down bars
                    {
                        float d = (br.Current - pIn).Length;
                        float x=d/moduleLength;
                        bool goodDistFromPrevious=Calc.Around(d-lastD, moduleLength, moduleLength*maxDifferenceBetweenBarOffsets);
                        //if x rounds to the correct position, or the distance from the previous bar is good ->proceed with this bar
                        if (Calc.Around(x, (float)Math.Round(x), 0.4f) || goodDistFromPrevious)
                        {
                            MyPoint up = trackVLine(br.Current, current, vdUp, noise);
                            MyPoint down = trackVLine(br.Current, current, vdDown, noise);

                            float dup = (up - br.Current).Length; //up bar length
                            float ddown = (down - br.Current).Length; //down bar length
                            if (ddown > minBase) minBase = ddown;
                            float l = dup + ddown;
                            if (l > max) max = l;
                            if (l < min) min = l;

                            int pos = (int)Math.Round(x);
                            if (pos == iSample || goodDistFromPrevious)
                            {
                                if (iSample < bars.Length)
                                {
                                    bars[iSample] = new float[2];
                                    bars[iSample][0] = dup;
                                    bars[iSample][1] = ddown;
                                }
                                lastD = d;
                                iSample++;
                            }
                            else return null;
                        }
                    }
                    processing = !processing;
                }
                br.Next();
            }

            //convert up and down bar lengths to binary. 
            //For 4-state barcodes we use the max bar lenght/3 as a threshold
            //For 2-state barcodes use length/2
            float maxHeight = (region.A - region.D).Length;
            if (max> maxHeight) max = maxHeight;
            if (max < maxHeight * maxDifferenceBetweenLargerBarAndBoundingBox) return null;
            float step = (IsFourState() ? max * fourStateHeightThreshold : max *triStateHeightThreshold);
            for (int i = 0; i < N; i++) if (bars[i] != null)
                {
                    //if 2 state barcode, check if bases are aligned
                    if (!IsFourState() && minBase - bars[i][1] > max * maxBaseAlignmentError) return null;

                    float l = bars[i][0] + bars[i][1];
                    float ls = l / step;
                    int type = (IsFourState()?(ls < 1.5f ? 0 : ls < 2.5f ? 1 : 2):(ls>1.5f?1:0));
#if DEBUG
                    //Console.Write("-" + type);
#endif


                    samples[i] = new bool[2];
                    if (IsFourState())
                    {
                        if (type == 2) samples[i][0] = samples[i][1] = true;
                        else if (type == 0) samples[i][0] = samples[i][1] = false;
                        else if (bars[i][0] > bars[i][1])
                        {
                            samples[i][0] = true; samples[i][1] = false;
                        }
                        else
                        {
                            samples[i][0] = false; samples[i][1] = true;
                        }
                    }
                    else
                    {
                        if (type == 1) samples[i][0] = true;
                        else samples[i][0] = false;
                    }
                }
                else samples[i] = new bool[2];
#if DEBUG
            //Console.WriteLine("\nSamples:");
            //for (int i = 0; i < N; i++) Console.Write((samples[i][0] ? "1" : " "));
            //Console.WriteLine("");
            //for (int i = 0; i < N; i++) Console.Write((samples[i][1] ? "1" : " "));
            //Console.WriteLine("");
#endif
            return samples;
        }



        //Find a vertical transition (from current color to !current) starting at point q
        //current=true --> white to black transition
        private bool findTransition(ref MyPoint q, bool current, int offset)
        {
            XBitArray row = (XBitArray)BWImage.GetRow(q.Y);
            int x = q.X;
            int xMin = x - offset;
            int xMax = x + offset;
            //move to left WHITE pixel (if current is BLACK)
            while (x >= 0 && row[x] == current && x >= xMin) x--;
            if (x >= 0 && row[x] == current) return false; //if not found return false

            //move to the right BLACK pixel
            while (x < 0 || x < BWImage.Width && row[x] != current && x <= xMax) x++;
            if (current && x >= BWImage.Width) return false; // if we are looking for a black pixel and we are out of the image, return false 
            int d = q.X - x;
            if (d < offset && d > -offset)
            {
                q.X = x;
                return true;
            }
            return false;
        }

        //Trace a vertical edge and stops at the vertex, starting at point q in dir direction
        //Allows noise pixels
        private MyPoint trackVLine(MyPoint q, bool current, MyVector dir, int noise)
        {
            bool end = false;
            MyPoint prev = q;
            int offset = 1;
            while (q.Y >= 0 && q.Y < BWImage.Height && !end)
                if (!findTransition(ref q, current, offset)) { if (offset++ > noise) end = true; }
                else { prev = q; q.Y += dir.Y; offset = 1; }
            return prev;
        }

    }
}
