using System;
using System.Collections;
using System.Collections.Generic;

namespace BarcodeReader.Core.Common
{
    internal interface IBestMatch { int bestMatch(float[] e, LinkedList<int> symbols);}

    //Class to read barcode symbols of nBars bars and match against a set of available symbols
    //Optionally (set in parameter useE), converts the widths table to E table (sum of two consecutive bars, white and 
    //black or black and white).
    internal class BarSymbolReader
    {
        internal IImageScaner scan;
        Bresenham br;
        MyPoint lastBlack; //last black pixel read. To determine the bounding box of the barcode if no stop pattern.
        internal float moduleLength;
        int[] nBars, nModules;
        bool startsWithBlack, currentIsBlack, useLastBar;
        bool checkEnd;
        int[] tableIndexs;
        int[][][] ttSymbols, ttE;
        int stopSymbol;
        bool useE;
        IBestMatch iBestMatch;
        bool oneTable, differentSymbolLengths;


        //nBars - count of black and white "bars" per each symbol
        //nModules - length in modules of each symbol

        //symbols with chars with the same number of nBars, and of the same length (nModules)
        public BarSymbolReader(IImageScaner scan, int nBars, int nModules, bool useLastBar, bool startsWithBlack, float moduleLength, int[][] symbols, int stopSymbol, bool useE, IBestMatch iBestMatch)
        {
            this.oneTable = true;
            this.differentSymbolLengths = false;
            Initialize(scan, new int[] { nBars }, new int[] { nModules }, new int[] { 0 }, useLastBar, startsWithBlack, moduleLength, new int[][][] { symbols }, stopSymbol, useE, iBestMatch);
        }

        //symbols with chars with different number of nBars, and different lengths (nModules) at a predefined position
        public BarSymbolReader(IImageScaner scan, int[] nBars, int[] nModules, int[] tableIndexs, bool useLastBar, bool startsWithBlack, float moduleLength, int[][][] symbols, bool useE, IBestMatch iBestMatch)
        {
            this.oneTable = false;
            this.differentSymbolLengths = false;
            Initialize(scan, nBars, nModules, tableIndexs, useLastBar, startsWithBlack, moduleLength, symbols, -1, useE, iBestMatch);
        }

        //symbols with chars with different number of nBars, and different lengths (nModules) at a predefined position
        public BarSymbolReader(IImageScaner scan, int[] nBars, int[] nModules, int[] tableIndexs, bool useLastBar, bool startsWithBlack, float moduleLength, int[][][] symbols, bool useE, IBestMatch iBestMatch, bool oneTable)
        {
            this.oneTable = oneTable;
            this.differentSymbolLengths = false;
            Initialize(scan, nBars, nModules, tableIndexs, useLastBar, startsWithBlack, moduleLength, symbols, -1, useE, iBestMatch);
        }

        //symbols with chars with the same number of nBars, and different lengths (nModules) at any position -->pharma
        public BarSymbolReader(IImageScaner scan, int nBars, int[] nModules, bool useLastBar, bool startsWithBlack, float moduleLength, int[][] symbols, bool useE, IBestMatch iBestMatch)
        {
            this.oneTable = true;
            this.differentSymbolLengths = true;
            Initialize(scan, new int[] { nBars }, nModules, new int[] { 0 }, useLastBar, startsWithBlack, moduleLength, new int[][][] { symbols }, -1, useE, iBestMatch);
        }


        private void Initialize(IImageScaner scan, int[] nBars, int[] nModules, int[] tableIndexs, bool useLastBar, bool startsWithBlack, float moduleLength, int[][][] ttSymbols, int stopSymbol, bool useE, IBestMatch iBestMatch)
        {
            this.scan = scan;
            this.nBars = nBars;
            this.nModules = nModules;
            this.useLastBar = useLastBar;
            this.startsWithBlack = startsWithBlack;
            this.moduleLength = moduleLength;
            this.tableIndexs = tableIndexs;
            this.ttSymbols = ttSymbols;
            this.stopSymbol = stopSymbol;
            this.useE = useE;
            this.iBestMatch = iBestMatch;

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


        //main method to read a barcode row between point a and b. Reads groups of nBars bars, and 
        //match them against the pattern table.
        public int[] Read(MyPointF a, MyPointF b, int n, out float error, out float maxError, out float confidence) { return Read(new Bresenham(a, b), true, n, out error, out maxError, out confidence); }
        public int[] Read(MyPointF a, MyPointF b, out float error, out float maxError, out float confidence) { return Read(new Bresenham(a, b), true, -1, out error, out maxError, out confidence); }
        public int[] Read(MyPointF a, MyVectorF vd, out float error, out float maxError, out float confidence, out MyPoint last) { int[] row = Read(new Bresenham(a, vd), false, 1, out error, out maxError, out confidence); last = lastBlack; return row; }

        public int[] Read(IImageScaner scanner, float module, MyPointF a, MyPointF b, out float error, out float maxError, out float confidence)
        {
            this.scan = scanner;
            this.moduleLength = module;
            return Read(new Bresenham(a, b), true, -1, out error, out maxError, out confidence);
        }

        public int[] Read(Bresenham br, bool checkEnd, int nCodewords, out float error, out float maxError, out float confidence)
        {
            float e;
            this.checkEnd = checkEnd;
            error = 0f; maxError = 0f;
            this.br = br;
            LinkedList<int> symbols = new LinkedList<int>();

            confidence = 0f;
            if (br.Steps > (scan.Width*6)/5) return null;

            //move to first black
            if (checkEnd) while (!br.End() && scan.isBlack(br.Current) != startsWithBlack) br.Next();
            else while (scan.In(br.Current) && scan.isBlack(br.Current) != startsWithBlack) br.Next();
            currentIsBlack = startsWithBlack;

            int s = -1;
            bool stop = false;
            float symbolConfidence;
            lastModuleLength = moduleLength;
            if (checkEnd)
                while (scan.In(br.Current) && !br.End() && (stopSymbol == -1 || s != stopSymbol) && nCodewords != 0)
                {
                    s = ReadSymbol(symbols, out e, out symbolConfidence, out stop);
                    confidence += symbolConfidence;
                    if (!br.End()) { error += e; if (e > maxError) maxError = e; }
                    symbols.AddLast(s);
                    nCodewords--;
                }
            else
                while (scan.In(br.Current) && (oneTable || symbols.Count < nBars.Length) && (stopSymbol == -1 || s != stopSymbol))
                { s = ReadSymbol(symbols, out e, out symbolConfidence, out stop); if (stopSymbol == -1 && s == -1) break; confidence += symbolConfidence;  error += e; if (e > maxError) maxError = e;  symbols.AddLast(s); if (stop) break; }
            error /= (float)symbols.Count; //mean error
            confidence /= (float)symbols.Count; //mean confidence
            int[] ss = new int[symbols.Count];
            symbols.CopyTo(ss, 0);
            return ss;
        }

        /// <summary>
        /// min match difference
        /// </summary>
        internal float MinMatchDifference = float.MinValue;

        float lastModuleLength;
        int ReadSymbol(LinkedList<int> symbols, out float bestDist, out float symbolConfidence, out bool stop)
        {
            stop = false;
            bestDist = 1000; symbolConfidence = 0f;
            float[] e = NextSymbolE(symbols.Count); //next block of nBars bars widths
            if (e == null) return -1;

            int[][][] table = (useE ? ttE : ttSymbols);
            int symbol = -1;
            int i = (symbols.Count < tableIndexs.Length ? symbols.Count : tableIndexs.Length - 1);
            if (tableIndexs[i] == -1)
            {
                if (iBestMatch != null) symbol = iBestMatch.bestMatch(e, symbols);
                else symbol=0; //there is no table of symbols, so return a symbolic 0.
            }
            else symbol = BestMatch(MinMatchDifference, e, table[tableIndexs[i]], useE, useLastBar, out bestDist, out symbolConfidence, out stop);

            return symbol;
        }

        public static int BestMatch(float minDifference, float[] e, int[][] table, bool useE, bool useLastBar, out float bestDist, out float confidence, out bool stop)
        {
            float distToNext = 1000f;
            bestDist = 1000f;
            confidence = 0f;
            stop = false;
            if (e != null)
            {
                //black and white compensation
                float sumBlack = 0f, sumWhite = 0f, lastBlack = 0f, lastWhite = 0f;
                if (!useE)
                {
                    for (int i = 0; i < e.Length; i++)
                        if (i % 2 == 0) sumBlack += e[i]; else sumWhite += e[i];
                    if (e.Length % 2 == 0) lastWhite = e[e.Length - 1];
                    else lastBlack = e[e.Length - 1];
                }

                int bestMatch = -1;
                float prevBestDist = 1000f;
                for (int index = 0; index < table.Length; index++) if (table[index].Length == e.Length)
                    {
                        bool thisStop = false;
                        int[] E = table[index];

                        int N = e.Length;
                        if (!useLastBar && !Calc.Around(e[N - 1], E[N - 1], 1f))
                        {
                            N--; //don't use the last white module to find the closest symbol
                            thisStop = true; //stop at the next symbol
                        }

                        //black and white compensation
                        float blackCompensationMIN = 0f;
                        if (!useE)
                        {
                            float sumEBlack = 0f, sumEWhite=0f;
                            for (int i = 0; i < N; i++)
                                if (i % 2 == 0) sumEBlack += E[i]; else sumEWhite += E[i];
                            blackCompensationMIN = (sumWhite - sumEWhite - sumBlack + sumEBlack -(thisStop?lastWhite+lastBlack:0f)) / (float)N;
                        }

                        //if (Math.Abs(blackCompensationMIN) < 1)
                        {
                            float dist = 0f;
                            for (int i = 0; i < N; i++)
                            {
                                float d = e[i] - (float)E[i] + (i % 2 == 0 ? blackCompensationMIN : -blackCompensationMIN);;
                                d = d*d;

                                if (d < 4f) dist += d;
                                else { dist = -1f; break; }
                            }
                            if (dist != -1f)
                            {
                                dist += blackCompensationMIN*blackCompensationMIN;
                                if (dist < bestDist)
                                {
                                    prevBestDist = bestDist;
                                    bestDist = dist;
                                    bestMatch = index;
                                    stop = thisStop;
                                } 
                                else if (dist < prevBestDist) prevBestDist = dist;
                            }
                        }
                    }
                distToNext = prevBestDist - bestDist;
                confidence = 1f - bestDist / prevBestDist;
                if (distToNext < minDifference) return -1;  //if the difference to the previous best match is small, reject
                return bestMatch;
            }
            return -1;
        }

        public static int BestMatch2(float[] w, float[] e, int[][] tableW, int[][] tableE, bool useLastBar, out float bestDist, out float confidence)
        {
            bestDist = 1000f;
            confidence = 0f;
            if (e != null)
            {
                int bestMatch = -1;
                for (int index = 0; index < tableW.Length; index++) if (tableW[index].Length == e.Length)
                    {
                        int[] W = tableW[index];
                        int[] E = tableE[index];
                        float dist = 0f;
                        int last = useLastBar ? E.Length : E.Length - 1;
                        for (int i = 0; i < last; i++)
                        {
                            float dW = w[i] - (float)W[i]; dW *= dW;
                            float dE = e[i] - (float)E[i]; dE *= dE;
                            float d = useLastBar ? dE + dW : dW;

                            if (useLastBar && (dE > 3f || dE > 3f)) { dist = -1f; break; }

                            if (d < 8f) dist += d;
                            else { dist = -1f; break; }
                        }
                        if (dist != -1f && dist < bestDist) { bestDist = dist; bestMatch = index; }
                    }
                confidence = (bestDist < (float)e.Length ? 1f - bestDist / (float)e.Length : 0f);
                return bestMatch;
            }
            return -1;
        }

        public static int BestMatchInc(float[] e, int[][] table, bool useLastBar, out float bestDist, out float confidence)
        {
            bestDist = 1000f;
            confidence = 0f;
            if (e != null)
            {
                int bestMatch = -1;
                for (int index = 0; index < table.Length; index++) if (table[index].Length == e.Length)
                    {
                        int[] E = table[index];
                        float dist = 0f;
                        int last = useLastBar ? E.Length : E.Length - 1;
                        for (int i = 0; i < last; i++)
                        {
                            float d = (e[(i+1)%last] - e[i])  - (float)(E[(i+1)%last] - E[i]);
                            d = d * d;

                            if (d < 8f) dist += d;
                            else { dist = -1f; break; }
                        }
                        if (dist != -1f && dist < bestDist) { bestDist = dist; bestMatch = index; }
                    }
                confidence = (bestDist < (float)e.Length ? 1f - bestDist / (float)e.Length : 0f);
                return bestMatch;
            }
            return -1;
        }



        public float[] NextSymbolE(int nSymbol)
        {
            if (checkEnd && br.End()) return null;
            int N = nBars.Length;
            int cnBars = nBars[nSymbol < N ? nSymbol : N - 1];
            int cnModules = nModules[nSymbol < N ? nSymbol : N - 1];

            int[] barLenghts = new int[cnBars];
            int nTransitions = 0;
            int n = 0;
            int sumBarLengths = 0;
            Bresenham br2 = new Bresenham(br); //copy bresenham state
            while (((checkEnd && !br.End()) || !checkEnd) && scan.In(br.Current) && nTransitions < cnBars)
            {
                //scans next pixel, only 1 pixel is samplingWidth=1
                float gray = 0f;
                bool isBlack = scan.isBlack(br.Current); 
                gray = isBlack ? 1f : 0f;

                if (isBlack == currentIsBlack)  n++;
                else if (n > (int)(lastModuleLength / 3F)) //a valid module
                {
                    barLenghts[nTransitions++] = n;
                    sumBarLengths += n;
                    n = 1;
                    if (currentIsBlack) lastBlack = br.Current;
                    currentIsBlack = !currentIsBlack;
                }
                else //consider as noise (join to the previous module)
                {
                    if (nTransitions > 0)
                    {
                        nTransitions--;
                        n = barLenghts[nTransitions] + n + 1;
                        sumBarLengths -= barLenghts[nTransitions];
                    }
                    else n++;
                    currentIsBlack = !currentIsBlack;
                }
                if (nTransitions == cnBars && !differentSymbolLengths)
                {
                    //check if there are noisy bars 
                    float currentModuleLength = (float)sumBarLengths / (float)cnModules;
                    if (!Calc.Around(currentModuleLength / lastModuleLength, 1.0f, 0.3f))  //0.14f
                        return null;
                }

                if (nTransitions < cnBars) br.Next();
            }

            if (nTransitions < cnBars)
            {
                barLenghts[nTransitions++] = n;
                sumBarLengths += n;
                if (currentIsBlack) lastBlack = br.Current;
                currentIsBlack = !currentIsBlack;
            }

            if (nTransitions >= cnBars-1)
            {
                nTransitions = cnBars;
                //check symbol length, that can vary incrementally
                float currentModuleLength = differentSymbolLengths?lastModuleLength:(float)sumBarLengths / (float)cnModules;
                lastModuleLength = currentModuleLength;
                float[] E = new float[nTransitions];
                for (int i = 0; i < nTransitions; i++)
                {
                    float e = 0f;
                    if (useE)
                    {
                        e = (float)(barLenghts[i] + barLenghts[(i + 1) % nTransitions]) / currentModuleLength;
                        if (e < 2f) e = 2f;
                    }
                    else
                    {
                        e = (float)(barLenghts[i]) / currentModuleLength;
                        //if (e < 1f) e = 1f;
                    }
                    E[i] = e;
                }
                return E;
            }
            return null;
        }

        public MyPoint Current { get { return br.Current; } }
    }






    //Auxiliar class to store the number of occurrencies of a codeword. The most common one will
    //be used to decode the barcode.
    internal class SymbolSample : IComparable
    {
        public int symbol;
        public int N; //number of repetitions
        public SymbolSample(int symbol) { this.symbol = symbol; this.N = 1; }
        public int CompareTo(object o) { SymbolSample s = (SymbolSample)o; return s.N - N; }
    }

    //Auxiliar class to store samples of a codeword of the barcode. Each row in the barcode will be scanned
    //several times, so each codeword is samples also several times. All samples values are stored, and the 
    //most common one is used to decode the barcode.
    internal class SymbolSamples
    {
        public ArrayList samples = new ArrayList();
        public void add(int symbol)
        {
            foreach (SymbolSample s in samples) if (s.symbol == symbol) { s.N++; return; }
            samples.Add(new SymbolSample(symbol));
        }
        public void sort() { samples.Sort(); }
    }

    //data structure to store all redundant samples of each row.
    internal class RowSamples
    {         
         Dictionary<Int32, SymbolSamples[]> symbols = new Dictionary<Int32, SymbolSamples[]>();
         public void Clear() { symbols.Clear(); }

         public void AddRow(int nRow, int[] codewords)
         {
             if (!symbols.ContainsKey(nRow))
             {
                 SymbolSamples[] s = symbols[nRow] = new SymbolSamples[codewords.Length];
                 for (int i = 0; i < codewords.Length; i++) s[i] = new SymbolSamples();
             }
             SymbolSamples[] samples = symbols[nRow];
             for (int i = 0; i < codewords.Length; i++) 
                 if (codewords[i]!=-1 && i<samples.Length) samples[i].add(codewords[i]);
         }

         //extract codewords
         public int[] GetBestRow(int nRow)
         {
             if (!symbols.ContainsKey(nRow)) return null;
             SymbolSamples[] row = symbols[nRow];
             int[] codewords = new int[row.Length];
             for (int i = 0; i< row.Length; i++)
             {
                row[i].sort();
                 codewords[i] = (row[i].samples.Count>0? ((SymbolSample)row[i].samples[0]).symbol : -1);
             }
             return codewords;
         }

         public int[] GetRowIndexs() { 
             int[] indexs=new int[symbols.Count];
             symbols.Keys.CopyTo(indexs, 0);
             return indexs; 
         }
    }
}
