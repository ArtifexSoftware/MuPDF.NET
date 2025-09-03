using System.Collections.Generic;

namespace BarcodeReader.Core.Common
{
    //Magic class to scan a barcode region area, not just a line. It projects the area to a 
    //single line, thus removing most of noise. The projection is loose, it means that it is 
    //not a strict mathematical projection, allowing +-1px margin. For each vertical segment
    //of the area, the algorithm calculates the % of white pixels and the % of black pixels
    //and the higher of them is the used value.
    //The resulting projection is stored in proj[], a float array. This array is read 
    //using 2 different threshods, to be able to read high noise images. The first threshold
    //works well for normal noise images. The second detect noisy narrow bars.
	internal class BarSymbolReaderLooseProjection
    {
        internal IImageScaner scan;
        BarCodeRegion r;
        float moduleLength, lastModuleLength; //to detect missing bars, or noisy bars
        int[] tNBars, tNModules;
        bool startsWithBlack, useLastBar;
        int[][][] ttSymbols, ttE;
        int stopSymbol;
        MyPointF ULsampling, URsampling;
        float length;
        float[] thresholds=new float[]{ 0.2f, 0.05f};
        bool useE = false;

        public BarSymbolReaderLooseProjection(IImageScaner scan, int nBars, int nModules,
            bool startsWithBlack, int[][] tSymbols, int stopSymbol, bool useLastBar, bool useE)
        {
            initialize(scan, new int[] { nBars }, new int[] { nModules }, startsWithBlack, new int[][][] { tSymbols }, stopSymbol, useLastBar, useE);
        }

        public BarSymbolReaderLooseProjection(IImageScaner scan, int[] nBars, int[] nModules,
            bool startsWithBlack, int[][][] tSymbols, int stopSymbol, bool useLastBar, bool useE)
        {
            initialize(scan, nBars, nModules, startsWithBlack, tSymbols, stopSymbol, useLastBar, useE);
        }

        private void initialize(IImageScaner scan, int[] nBars, int[] nModules, 
            bool startsWithBlack, int[][][] tSymbols, int stopSymbol, bool useLastBar, bool useE)
        {
            this.scan = scan;
            this.tNBars = nBars;
            this.tNModules = nModules;
            this.startsWithBlack = startsWithBlack;
            this.ttSymbols = tSymbols;
            this.stopSymbol = stopSymbol;
            this.useLastBar = useLastBar;
            this.useE = useE;

            //initialize E table
            if (useE)
            {
                this.ttE = new int[ttSymbols.Length][][];
                for (int i = 0; i < ttSymbols.Length; i++) //for each table of symbols
                {
                    int[][] ts = ttSymbols[i];
                    int[][] tE = ttE[i] = new int[ts.Length][];
                    for (int j = 0; j < ts.Length; j++) //for each symbol in the table
                    {
                        int[] s = ts[j];
                        int[] e = tE[j] = new int[s.Length];
                        for (int k = 0; k < s.Length; k++)
                            e[k] = s[k] + s[(k + 1) % s.Length];
                    }
                }
            }
        }

        //Scan an area of the region defined by its height.
        public int[] Read(BarCodeRegion r, float moduleLength, float height)
        {
            this.r = r;
            this.lastModuleLength=this.moduleLength = moduleLength;

            //calculate region to sample
            float cc = 0.5f + height / 2f;
            ULsampling = r.A * cc + r.D * (1f - cc);
            URsampling = r.B * cc + r.C * (1f - cc);
            MyPointF a = r.A * (1f - cc) + r.D * cc;
            MyPointF b = r.B * (1f - cc) + r.C * cc;
            Bresenham br=new Bresenham(a,b);
            if (br.Steps > (scan.Width*6)/5) return null;

            this.length = (float)(br.Steps);

            float[] proj=new float[br.Steps+1];
            int nProj = 0;

            //move to the first black
            while (!br.End() && scan.isBlack(br.Current) != startsWithBlack)
            {
                proj[nProj++] = 1f; //assume skiped pixels are black
                br.Next();
            }

            //project all pixels
            while (!br.End() && scan.In(br.Current))
            {
                float tpo = (float)br.Steps / (float)(length);
                MyPoint p = ULsampling * tpo + URsampling * (1f - tpo);

                proj[nProj++] = SampleSegmentLoosely(new Bresenham(br.Current, p)); 
                br.Next();
            }

            int[] symbols = ReadSymbols(proj, true); //try removing noise pixels
            if (symbols == null) symbols = ReadSymbols(proj, false); //try without removing noise pixels
            return symbols;
        }

        int[] ReadSymbols(float[] proj, bool removeNoise)
        {
            //read barcode using different thresholds
            float confidence = 0f, symbolConfidence;
            LinkedList<int> symbols = new LinkedList<int>();
            bool error=false; 
            foreach (float t in thresholds)
            {
                int pos = 0;
                this.lastModuleLength = this.moduleLength;
                symbols.Clear();
                confidence = 0;
                error = false;
                while (pos < proj.Length && !error)
                {
                    int s = ReadSymbol(proj, ref pos, symbols, t, removeNoise, out symbolConfidence);
                    if (s == -1) error = true; //if a bytecode is not read, skip to the next threshold
                    else
                    {
                        symbols.AddLast(s);
                        confidence += symbolConfidence;
                        if (symbols.Count > 1 && s == stopSymbol) break;
                    }
                }
                if (!error) break;//if the whole barcode is read, stop 
            }
            if (error) return null;
            r.Confidence = confidence / (float)symbols.Count;
            int[] ss = new int[symbols.Count];
            symbols.CopyTo(ss, 0);
            return ss;
        }

        /// <summary>
        /// min matching difference
        /// </summary>
        internal float MinMatchDifference = float.MinValue;

        //reads a bytecode starting at pos.
        int ReadSymbol(float[] proj, ref int pos, LinkedList<int> symbols, float threshold, bool removeNoise, out float symbolConfidence)
        {
            symbolConfidence = 0f;
            int startPos = pos;
            int tableIndex = symbols.Count >= tNBars.Length ? tNBars.Length - 1 : symbols.Count;
            int nBars = tNBars[tableIndex];
            int nModules = tNModules[tableIndex];
            int[] w = new int[nBars];
            float[] grays = new float[nBars];
            bool currentIsBlack = true;
            bool moduleIsConfident = false;
            int n = 0, nb = 0, length = 0;
            float gray = 0f;
            float peakThreshold = threshold * 0.7f;
            while (pos < proj.Length && nb < nBars)
            {
                //Decide if the current pixel is white or black based on the threshold
                float pr = proj[pos];
                bool isBlack = pr > 0.5f;
                bool pixelIsConfident = false;
                if (pr > 1f - threshold) { isBlack = true; pixelIsConfident = true; }//it is clearly black
                else if (pr < threshold) { isBlack = false; pixelIsConfident = true; }//it is clearly white
                else if (pos > 0 && pos < proj.Length - 1) //we are not sure
                {
                    //detect peaks to decide if it is black or white. If it is not a peak, use 0.5.
                    float dl = pr - proj[pos - 1];
                    float dr = pr - proj[pos + 1];
                    if (dl > 0 && dr > 0) //max
                    {
                        int l = pos - 1; while (l > 0 && proj[l] > proj[l - 1]) l--;
                        int r = pos + 1; while (r < proj.Length - 1 && proj[r] > proj[r + 1]) r++;
                        dl = pr - proj[l];
                        dr = pr - proj[r];
                        if (dl > peakThreshold && dr > peakThreshold) isBlack = true;
                    }
                    else if (dl < 0 && dr < 0) //min
                    {
                        int l = pos - 1; while (l > 0 && proj[l] < proj[l - 1]) l--;
                        int r = pos + 1; while (r < proj.Length - 1 && proj[r] < proj[r + 1]) r++;
                        dl = pr - proj[l];
                        dr = pr - proj[r];
                        if (dl < -peakThreshold && dr < -peakThreshold) isBlack = false;
                    }
                }

                float confidence = isBlack ? pr : 1f - pr;
                if (isBlack == currentIsBlack) //another pixel of the same color
                {
                    n++; gray += confidence;
                    length++;
                    pos++;
                    moduleIsConfident = moduleIsConfident || pixelIsConfident; 
                }
                else if (nb == 0 || !removeNoise || removeNoise && n > (int)(lastModuleLength / 3.8F)) //a valid module
                {
                    grays[nb] = gray;
                    w[nb++] = n;
                    currentIsBlack = !currentIsBlack;
                    n = 1; gray = confidence;
                    moduleIsConfident = pixelIsConfident;

                    if (nb < nBars) { length++; pos++; }
                }
                else  //consider as noise (join to the previous module)
                {
                    nb--;
                    n = w[nb] + n + 1; gray = grays[nb] + gray + confidence;
                    length++;
                    pos++;
                    currentIsBlack = !currentIsBlack;
                }
            }
            if (n > 0 && nb < nBars)
            {
                grays[nb] = gray;
                w[nb++] = n; //add the last bar
            }

            if (nb < nBars) return -1;

            //check disalignment
            float currentModuleLength = useLastBar ? (float)length / (float)nModules : (float)(length - w[nBars - 1]) / (float)(nModules - 1);
            if (!Calc.Around(currentModuleLength / lastModuleLength, 1f, 0.15f))
                return -1;
            lastModuleLength = currentModuleLength;

            float[] E = new float[nBars];
            float[] W = new float[nBars];
            for (int i = 0; i < w.Length; i++) W[i] = (float)grays[i] / currentModuleLength;
            for (int i = 0; i < w.Length; i++) E[i] = W[i] + W[(i + 1) % nBars];            

            float bestDist;
            bool stop;
            int symbol = useE ?
                BarSymbolReader.BestMatch2(W, E, ttSymbols[tableIndex], ttE[tableIndex], useLastBar, out bestDist, out symbolConfidence)
                :
                BarSymbolReader.BestMatch(MinMatchDifference, E, (useE ? ttE[tableIndex] : ttSymbols[tableIndex]), useE, useLastBar, out bestDist, out symbolConfidence, out stop);

            return symbol;
        }

        //Key method to project the region. Traces a bresenham segment, and count
        //the number of white and black pixels in that segment, allowing a margin of
        //+-1px (with penalisation).
        const float K = 0.3f;
        float SampleSegmentLoosely(Bresenham b)
        {
            float nBlack = 0f, nWhite = 0f;
            int length = b.Steps+1;
            int blackX = 0, whiteX = 0;
            while (!b.End() && scan.In(b.Current))
            {
                MyPoint p=b.Current;
                if (scan.isBlack(p)) { blackX = 0; nBlack+=1f; }
                else
                {
                    p.X++;
                    if (blackX != -1 && scan.isBlack(p)) { blackX = 1; nBlack += K; }
                    else
                    {
                        p.X -= 2;
                        if (blackX != 1 && scan.isBlack(p)) { blackX = -1; nBlack += K; } else blackX=0;
                    } 
                }
                p = b.Current;
                if (!scan.isBlack(p)) { whiteX = 0; nWhite += 1f; }
                else
                {
                    p.X++;
                    if (whiteX != -1 && !scan.isBlack(p)) { whiteX = 1; nWhite += K; }
                    else
                    {
                        p.X -= 2;
                        if (whiteX != 1 && !scan.isBlack(p)) { whiteX = -1; nWhite += K; } else whiteX=0;
                    } 
                }

                b.Next();
            }
            if (moduleLength>2f) 
                return (nBlack > nWhite ? nBlack / (float)length : 1f - nWhite / (float)length);
            else 
                return (nBlack > nWhite ? nBlack / (nBlack+nWhite) : 1f - nWhite / (nBlack+nWhite));
        }
    }
}
