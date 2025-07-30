using System;
using System.Collections.Generic;

namespace BarcodeReader.Core.Common
{
    interface IPatternFinderNoiseRow
    {
        void NewSearch(XBitArray row);
        void NewSearch(XBitArray row, int startX, int endX, int inc, int minModuleLength);
        FoundPattern NextPattern();
        bool HasQuietZone { get; }
        int First { get; }
        int Last { get; }
        int Center { get; }
    }

    //Optimized version of PatternFinderNois for horizontal or vertical lines.
    //It receives an array of bits.

    //Class to find bit patterns allowing noise removing. Noise level definies the number of pixels considered as noise.
    //noiseLevel=1: accept modules 1 pixel wide. 
    //noiseLevel=2: accept modules 2 pixel wide. 
    //...
    //PatternFinder mantains an state. When a pattern is found the state is saved and the method returns. When 
    //NextPattern is called, the state is restored and the search continues.
    class PatternFinderNoiseRow : IPatternFinderNoiseRow
    {
        bool useE = true;
        int[][] pattern; //main patterns
        bool startsWithBlack;
        int defaultNoiseLevel, noiseLevel;

        int[][] patternE; //pattern of black-white and white-black added modules
        int nPatterns, maxPatternLength, maxPatternELength;
        int[] N; //number of modules of each pattern
        int x, inc;

        LevelStatusRow[] status; //status of each noise level scan.
        int foundAtLevel; //local at which the last pattern was found
        XBitArray row;

        private void Initialize(int[][] pattern, bool useE, bool startsWithBlack, int noiseLevel, Dictionary<int[], object> hash)
        {
            this.pattern = pattern;
            this.useE = useE;
            this.startsWithBlack = startsWithBlack;
            this.defaultNoiseLevel = noiseLevel;

            this.nPatterns = pattern.Length;
            this.maxPatternLength = -1; //the max length
            this.patternE = new int[nPatterns][];
            this.N = new int[nPatterns];
            for (int i = 0; i < nPatterns; i++)
            {
                int n = 0;
                int[] currentPattern = pattern[i];
                int ELength = useE ? currentPattern.Length - 1 : currentPattern.Length;
                patternE[i] = new int[ELength];
                for (int j = 0; j < ELength; j++)
                {
                    if (useE) patternE[i][j] = currentPattern[j] + currentPattern[j + 1];
                    else patternE[i][j] = currentPattern[j];
                    n += currentPattern[j];
                }
                if (useE) N[i] = n + currentPattern[ELength];
                else N[i] = n;
                if (currentPattern.Length > maxPatternLength)
                    maxPatternLength = currentPattern.Length;
            }
            this.maxPatternELength = maxPatternLength - 1;


            this.status = new LevelStatusRow[defaultNoiseLevel];
        }

        public PatternFinderNoiseRow(int[][] pattern, bool useE, bool startsWithBlack, int noiseLevel, float maxEAcumDist = -1, float maxEDist = -1)
        {
            if(maxEAcumDist >= 0)
                this.maxEAcumDist = maxEAcumDist;
            if (maxEDist >= 0)
                this._maxEDist = maxEDist;
            Initialize(pattern, useE, startsWithBlack, noiseLevel, new Dictionary<int[], object>(1000));
        }

        public PatternFinderNoiseRow(int[][] pattern, bool useE, bool startsWithBlack, int noiseLevel, Dictionary<int[], object> hash)
        {
            Initialize(pattern, useE, startsWithBlack, noiseLevel, hash);
        }

        public PatternFinderNoiseRow NewFinder()
        {
            return new PatternFinderNoiseRow(pattern, useE, startsWithBlack, defaultNoiseLevel);
        }

        bool End()
        {
            return x>=row.Size;
        }
        
        int minModuleLength;
        int startX, endX;

        public void NewSearch(XBitArray row) { NewSearch(row, 0, row.Size, 1, 0); }
        public void NewSearch(XBitArray row, int startX, int endX, int inc, int minModuleLength)
        {
            this.row = row;
            this.inc = inc;
            if (inc > 0) { this.startX = startX; this.endX = endX; }
            else { this.startX = endX; this.endX = startX; }
            this.minModuleLength = minModuleLength;

            //jump to the first pixel equals to startsWithBlack
            bool found = false;
            x = startX;
            while (x >= this.startX && x < endX && !found)
            {
                if (row[x] ^ startsWithBlack) x += inc;
                else found = true;
            }

            //prepare search
            if (minModuleLength > 0)
            {
                noiseLevel = 0;
                status[noiseLevel++] = new LevelStatusRow(minModuleLength, maxPatternLength, x, startsWithBlack);
                if (minModuleLength>0) status[noiseLevel++] = new LevelStatusRow(minModuleLength-1, maxPatternLength, x, startsWithBlack);
            }
            else
            {
                noiseLevel = defaultNoiseLevel;
                for (int i = 0; i < noiseLevel; i++)
                    status[i] = new LevelStatusRow(i, maxPatternLength, x, startsWithBlack);
            }

            foundAtLevel = -1;
        }        

        public FoundPattern NextPattern()
        {
            //terminate last step 
            if (foundAtLevel != -1) status[foundAtLevel].skipCandidate();

            //continue...
            while (x>=startX && x<endX)
            {
                FoundPattern patternFound = processPixel(x, row[x], foundAtLevel + 1);
                if (patternFound != null)
                {
                    return patternFound;
                }
                x +=inc;
                foundAtLevel = -1;
            }

            //finalize (last module is not processed in the main loop
            var res =  terminate(foundAtLevel);

            return res;
        }

        FoundPattern processPixel(int p, bool isBlack, int startLevel)
        {
            for (int i = startLevel; i < this.noiseLevel; i++)
            {
                if (status[i].isCandidate(p, isBlack)) //detect new transition that ends a candidate pattern
                {
                    FoundPattern found = IsPattern(i);

                    if (found!=null)
                    {
                        this.foundAtLevel = i;
                        this.status[i].foundPattern = found;
                        return found;
                    }
                    status[i].skipCandidate();
                }
            }
            return null;
        }

        //tries to finalize all levels. It is called when the row is ended.
        FoundPattern terminate(int startLevel)
        {
            for (int i = startLevel+1; i < this.noiseLevel; i++)
            {
                if (status[i].terminate())
                {
					FoundPattern found = IsPattern(i);

                    if (found!=null)
                    {
                        this.foundAtLevel = i;
                        this.status[i].foundPattern = found;
                        return found;
                    }
                }
            }
            return null;
        }

        float _maxEDist = 1.0f;
        float maxEAcumDist = 1.2f;
        public float MaxEDist { get { return _maxEDist; } set { _maxEDist = value; } }
        public float MaxEAcumDist { get { return maxEAcumDist; } set { maxEAcumDist = value; } }


        FoundPattern IsPattern(int statusLevel)
        {
            int[] elementWidths = status[statusLevel].elementWidths;
            FoundPattern bestMatch = null;
            float bestDist = 0f;
            for (int np = 0; np < pattern.Length; np++)
            {
                //total width
                int p = 0, cp=0 ;
                var pat = pattern[np];
                for (int i = 0; i < pat.Length; i++) { p += elementWidths[i]; cp += pat[i]; }
                int[] currentPatternE = patternE[np];
                float moduleLength = (float)p / (float)cp;
                float maxEDist = _maxEDist;
                if (maxEDist == 1f)
                {
                    if (moduleLength <= 5f) maxEDist = 1.0f;
                    else maxEDist = 0.8f;
                }

                float blackCompensation = 1f, whiteCompensation=1f;
                if (!useE)
                {
                    int nBlackPattern=0, nBlackBarcode=0, nWhitePattern=0, nWhiteBarcode=0;
                    for (int i = 0; i < pat.Length; i++)
                        if (i % 2 == 0) 
                        { 
                            nBlackPattern+=currentPatternE[i]; 
                            nBlackBarcode += elementWidths[i];
                        }
                        else 
                        {
                            nWhitePattern += currentPatternE[i];
                            nWhiteBarcode += elementWidths[i];
                        }
                    float k=((float)nBlackBarcode+(float)nWhiteBarcode)/((float)nBlackPattern+(float)nWhitePattern);
                    blackCompensation = ((float)nBlackPattern / (float)nBlackBarcode) * k;
                    whiteCompensation = ((float)nWhitePattern / (float)nWhiteBarcode) * k;
                }


                //compute normalized pair widths
                float acum = 0F, absAcum=0F;
                bool found=true;
                int n = N[np];
                for (int i = 0; i < currentPatternE.Length; i++)
                {
                    float E = (float)currentPatternE[i];

                    float current = (useE? (float)(elementWidths[i] + elementWidths[i + 1]):(float)elementWidths[i]) * n / p;
                    if (i % 2 == 0) current *= blackCompensation;
                    else current *= whiteCompensation;

                    float dist = current - E;
                    acum += current - E;
                    absAcum+=dist*dist;

                    if (dist < -maxEDist || dist > maxEDist || acum < -maxEAcumDist || acum > maxEAcumDist) { found = false; break; }
                }
                float compensation=blackCompensation>1f?blackCompensation/1f:1f/blackCompensation;
                absAcum *= compensation*compensation;
                if (found && (bestMatch == null || absAcum < bestDist))
                {
                    var points = status[statusLevel].points;
                    var nextX = status[statusLevel].nextX;
                    var lastPoint = points.Length > 0 ? points[points.Length - 1] : nextX;
                    bestMatch = new FoundPattern(np, blackCompensation, whiteCompensation, (float)p/(float)n, Math.Abs(nextX - lastPoint));
                    bestDist = absAcum;                   
                }
            }

            return bestMatch;
        }

        public bool HasQuietZone
        {
            get
            {
                int[] w=status[this.foundAtLevel].elementWidths;
                int l = 0; for (int i = 0; i < w.Length; i++) l += w[i];

                int[] pattern=this.pattern[status[this.foundAtLevel].foundPattern.nPattern];
                int modules=0; for (int i = 0; i < pattern.Length; i++) modules += pattern[i];
                l = (int)((float)l * 5f / (float)modules);
                return status[this.foundAtLevel].previousWidth > l;
            }
        }

        public int First { get { return status[this.foundAtLevel].points[0]; } }
        public int Last
        {
            get
            {
                LevelStatusRow s = status[this.foundAtLevel];
                return s.points[pattern[s.foundPattern.nPattern].Length];
            }
        }
        public int Center
        {
            get
            {
                LevelStatusRow s = status[this.foundAtLevel];
                int mid = pattern[s.foundPattern.nPattern].Length / 2;
                int[] points = s.points;
                return (points[mid] + points[mid + 1]) / 2;
            }
        }
    }

    class LevelStatusRow
    {
        int noiseLevel;
        public FoundPattern foundPattern;
        int patternLength;
        int currentElement;
        int n;
        public int nextX;
        public bool processingWhite;
        public int[] elementWidths;        
        public int[] points;
        public int previousWidth; //to check if it has quiet zone
        public LevelStatusRow(int noiseLevel, int patternLength, int startX, bool startsWithBlack)
        {
            this.noiseLevel = noiseLevel;
            this.patternLength = patternLength;
            this.elementWidths = new int[patternLength];
            this.points = new int[patternLength+1];
            this.processingWhite = !startsWithBlack;
            this.previousWidth = 1000;

            this.n = 0;
            this.nextX = startX;
            this.currentElement = 0;
            this.points[0] = startX;
            this.foundPattern = null;
        }

        public bool isCandidate(int p, bool isBlack)
        {
            if (isBlack ^ processingWhite) //no transition
            {
                n++;
            }
            else //transition at this noise level
            {
                if (currentElement == -1) //searching for the first module
                {
                    currentElement = 0;
                    n = 1;
                    points[0] = p;
                }
                else
                {
                    //check if module is not noise at this level
                    if (n > noiseLevel)
                    {
                        if (currentElement < patternLength) //first modules
                        {
                            elementWidths[currentElement++] = n;
                            points[currentElement] = p;
                            n = 1;
                        }
                        else
                        {
                            nextX = p;
                            return true; //elementWidths ready to check if it is a pattern
                        }
                    }
                    else //if it is noise, add to the previous module
                    {
                        if (currentElement > 0)
                        {
                            n = elementWidths[currentElement - 1] + n +1;
                            currentElement--;
                        }
                        else //invalidate the start of the module, it is too narrow
                            currentElement = -1;
                    }
                }
                processingWhite = !processingWhite;
            }
            return false;
        }

        public bool terminate()
        {
            if (n > noiseLevel && currentElement == patternLength)
            {
                return true; //elementWidths ready to check if it is a pattern
            }
            return false;
        }

        public void skipCandidate()
        {
            previousWidth = elementWidths[1]; //remember last white module to check quiet zone in the next fount pattern
            for (int i = 0; i < patternLength - 2; i++)
            {
                elementWidths[i] = elementWidths[i + 2];
                points[i] = points[i + 2];
            }
            elementWidths[patternLength - 2] = n;
            points[patternLength - 2] = points[patternLength];
            points[patternLength - 1] = nextX;
            currentElement = patternLength - 1;
            n = 1;
            processingWhite = !processingWhite;
        }
    }
}
