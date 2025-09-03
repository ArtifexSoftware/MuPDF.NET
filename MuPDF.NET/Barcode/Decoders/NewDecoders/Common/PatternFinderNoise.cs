using System.Collections.Generic;

namespace BarcodeReader.Core.Common
{
#if CORE_DEV
    public
#else
    internal
#endif
    class Pattern
    {
        public FoundPattern foundPattern;
        public int xIn, xEnd, y;

        public Pattern(FoundPattern foundPattern, int xIn, int xEnd, int y)
        {
            this.foundPattern = foundPattern;
            this.xIn = xIn;
            this.xEnd = xEnd;
            this.y = y;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(Pattern))
            {
                Pattern p = (Pattern)obj;
                return (this.foundPattern==null || p.foundPattern==null || this.foundPattern.nPattern==p.foundPattern.nPattern) 
                    && dist(p.xIn, xIn) <= 3 && dist(p.xEnd, xEnd) <= 3;
            }
            return base.Equals(obj);
        }

        int dist(int a, int b)
        {
            int d = a - b;
            if (d < 0) return -d;
            return d;
        }

        public static LinkedList<Pattern> RemoveOldPatterns(LinkedList<Pattern> foundPatterns, int y)
        {
            /*LinkedList<Pattern> removed = new LinkedList<Pattern>(); 
            foreach (Pattern p in foundPatterns)
                if (p.y < y - 2) removed.AddLast(p);
            foreach (Pattern p in removed)
                foundPatterns.Remove(p);
            return removed;*/

            LinkedList<Pattern> removed = new LinkedList<Pattern>();

            var node = foundPatterns.First;

            while (node != null)
            {
                var next = node.Next;
                if (node.Value.y < y - 2)
                {
                    foundPatterns.Remove(node);
                    removed.AddLast(node.Value);
                }
                node = next;
            }

            return removed;
        }       

        public static LinkedList<Pattern> RemoveOldPatterns(LinkedList<Pattern> foundPatterns, int y, float tpc)
        {
            LinkedList<Pattern> removed = new LinkedList<Pattern>(); 
            foreach (StackedPattern p in foundPatterns)
                if ((y - p.y)>(int)((float)(p.y-p.startY)*tpc)) removed.AddLast(p);
            foreach (Pattern p in removed)
                foundPatterns.Remove(p);
            return removed;
        }       
    }

    class StackedPattern : Pattern
    {
        public int nPattern;
        public int startXIn, startXEnd, startY;
        LinkedList<MyPoint> starts, ends;
        int sumWidths;

        public StackedPattern(FoundPattern foundPattern, int xIn, int xEnd, int y): base(foundPattern, xIn, xEnd, y)
        {
            this.nPattern = foundPattern==null?0:foundPattern.nPattern;
            startXIn = xIn;
            startXEnd = xEnd;
            startY = y;
            starts = new LinkedList<MyPoint>();
            starts.AddLast(new MyPoint(xIn, y));
            ends = new LinkedList<MyPoint>();
            ends.AddLast(new MyPoint(xEnd, y));
            sumWidths = (xEnd - xIn);
        }

        public void NewRow(int xIn, int xEnd, int y)
        {
            this.xIn = xIn;
            this.xEnd = xEnd;
            this.y = y;
            starts.AddLast(new MyPoint(xIn, y));
            ends.AddLast(new MyPoint(xEnd, y));
            sumWidths += (xEnd - xIn);
        }

        public void Center(out MyPoint left, out MyPoint right)
        {
            LinkedList<MyPoint>.Enumerator l = starts.GetEnumerator();
            LinkedList<MyPoint>.Enumerator r = ends.GetEnumerator();
            for (int i = 0; i < starts.Count / 2; i++)
            {
                l.MoveNext();
                r.MoveNext();
            }
            left = l.Current;
            right = r.Current;
        }

        public void MidPoints(out MyPoint start, out MyPoint end)
        {
            LinkedList<MyPoint>.Enumerator s = starts.GetEnumerator();
            LinkedList<MyPoint>.Enumerator e = ends.GetEnumerator();
            e.MoveNext();
            s.MoveNext(); //move to the first element
            for (int i = 0; i < starts.Count / 2; i++) { s.MoveNext(); e.MoveNext(); }
            start=s.Current;
            end = e.Current;
        }

        public float MeanWidth()
        {
            return (float)sumWidths / (float)starts.Count;
        }

        public LinkedList<MyPoint> LPoints { get { return this.starts; } }
        public LinkedList<MyPoint> RPoints { get { return this.ends; } }
    }

    //Class to find bit patterns allowing noise removing. Noise level definies the number of pixels considered as noise.
    //noiseLevel=1: accept modules 1 pixel wide. 
    //noiseLevel=2: accept modules 2 pixel wide. 
    //...
    //PatternFinder mantains an state. When a pattern is found the state is saved and the method returns. When 
    // NextPattern is called, the state is restored and the search continues.
    class PatternFinderNoise
    {
        BlackAndWhiteImage image;
        int[][] pattern;
        bool startsWithBlack;
        int noiseLevel, defaultNoiseLevel;

        int[][] patternE;
        int patternLength, patternELength;
        int[] N; //number of modules of each pattern

        Bresenham l;
        bool end; //stops at the end of l or at the end of the image

        LevelStatus[] status;
        int foundAtLevel;
        int currentY;
        XBitArray row;

        private void Initialize(BlackAndWhiteImage image, int[][] pattern, bool startsWithBlack, int noiseLevel, Dictionary<int[], object> hash)
        {
            this.image = image;
            this.pattern = pattern;
            this.startsWithBlack = startsWithBlack;
            this.defaultNoiseLevel = noiseLevel;

            this.patternLength = pattern[0].Length; 
            this.patternELength = pattern[0].Length-1;
            this.patternE = new int[pattern.Length][];
            this.N = new int[pattern.Length];
            for (int j = 0; j < pattern.Length; j++)
            {
                int[] P = pattern[j];
                int[] E = patternE[j] = new int[patternELength];
                int n = 0;
                for (int i = 0; i < patternELength; i++)
                {
                    E[i] = P[i] + P[i + 1];
                    n += P[i];
                }
                N[j]= n + P[patternELength];
            }
            this.status = new LevelStatus[defaultNoiseLevel];
        }

        public PatternFinderNoise(BlackAndWhiteImage image, int[] pattern, bool startsWithBlack, int noiseLevel)
        {
            Initialize(image, new int[][] { pattern }, startsWithBlack, noiseLevel, new Dictionary<int[], object>(1000));
        }

        public PatternFinderNoise(BlackAndWhiteImage image, int[] pattern, bool startsWithBlack, int noiseLevel, Dictionary<int[], object> hash)
        {
            Initialize(image, new int[][]{pattern}, startsWithBlack, noiseLevel, hash);
        }

        public PatternFinderNoise(BlackAndWhiteImage image, int[][] pattern, bool startsWithBlack, int noiseLevel)
        {
            Initialize(image, pattern, startsWithBlack, noiseLevel, new Dictionary<int[], object>(1000));
        }

        public PatternFinderNoise(BlackAndWhiteImage image, int[][] pattern, bool startsWithBlack, int noiseLevel, Dictionary<int[], object> hash)
        {
            Initialize(image, pattern, startsWithBlack, noiseLevel, hash);
        }

        public PatternFinderNoise NewFinder()
        {
            return new PatternFinderNoise(image, pattern, startsWithBlack, noiseLevel);
        }

        bool End()
        {
            if (end) return l.End() || !image.In(l.Current.X, l.Current.Y);
            return !image.In(l.Current.X, l.Current.Y);
        }

        int minModuleLength;

        //end=true if the searchs stops at the end of l or until the end of the image size.
        public void NewSearch(Bresenham l) { NewSearch(l, true, -1); }
        public void NewSearch(Bresenham l, bool end, int minModuleLength)
        {
            this.l = l;
            this.end = end;
            this.minModuleLength = minModuleLength;

            //moves bresenham into the image
            while (!l.End() && !image.In(l.Current.X, l.Current.Y)) l.Next();

            //jump to the first pixel equals to startsWithBlack
            currentY = -1;
            bool found = false;
            while (!End() && !found)
            {
                if (l.Current.Y != currentY) row = image.GetRow(currentY = l.Current.Y);
                if (row[l.Current.X] ^ startsWithBlack) l.Next();
                else found = true;
            }

            //prepare search
            if (minModuleLength > 0)
            {
                noiseLevel = 1;
                status[0] = new LevelStatus(minModuleLength, patternLength, l.Current, startsWithBlack);
            }
            else
            {
                noiseLevel = defaultNoiseLevel;
                for (int i = 0; i < noiseLevel; i++)
                    status[i] = new LevelStatus(i, patternLength, l.Current, startsWithBlack);
            }

            foundAtLevel = -1;
        }        

        public int NextPattern()
        {
            //terminate last step 
            if (foundAtLevel != -1) status[foundAtLevel].skipCandidate();

            //continue...
            int nPattern = -1;
            while (!End())
            {
                if (l.Current.Y != currentY) row = image.GetRow(currentY = l.Current.Y);
                nPattern=processPixel(l.Current, row[l.Current.X], foundAtLevel+1);
                if (nPattern!=-1) return nPattern;
                l.Next();
                foundAtLevel = -1;
            }

            //finalize (last module is not processed in the main loop
            nPattern=terminate(foundAtLevel, l.Current);
            if (nPattern!=-1) return nPattern;

            return -1;
        }

        int processPixel(MyPoint p, bool isBlack, int startLevel)
        {
            for (int i = startLevel; i < this.noiseLevel; i++)
            {
                if (status[i].isCandidate(p, isBlack)) //detect new transition that ends a candidate pattern
                {
	                int found = IsPattern(i);

                    if (found!=-1)
                    {
                        this.foundAtLevel = i;
                        return found;
                    }
                    status[i].skipCandidate();
                }
            }
            return -1;
        }


        int terminate(int startLevel, MyPoint p)
        {
            for (int i = startLevel+1; i < this.noiseLevel; i++)
            {
                if (status[i].terminate(p))
                {
                    int found = IsPattern(i);
                    
                    if (found!=-1)
                    {
                        this.foundAtLevel = i;
                        return found;
                    }
                }
            }
            return -1;
        }


        int IsPattern(int statusLevel)
        {
            int[] elementWidths = status[statusLevel].elementWidths;

            //total width
            int p = 0;
            for (int i = 0; i < patternLength; i++) p += elementWidths[i];

            int iMin = -1;
            float minE = float.MaxValue;
            status[statusLevel].error = -1f;
            for (int j = 0; j < patternE.Length; j++)
            {
                int[] patE = patternE[j];
                //compute normalized pair widths
                bool found = true;
                float acumE = 0f;
                for (int i = 0; i < patternELength && found; i++)
                {
                    float e = (float)(elementWidths[i] + elementWidths[i + 1]) * N[j] / p;
                    float err = e - (float)patE[i]; //real part
                    acumE += err * err;
                    if (err > 1F || err < -1F) found=false;
                }
                if (found && acumE < minE) { iMin = j; minE = acumE; status[statusLevel].error = acumE; }
            }
            return iMin;
/*
                if (i > 0)
                {
                    //compensate neigbour
                    if (errPrev < 0F && err > 0F)
                        if (-errPrev < err) { err += errPrev; errPrev = 0F; }
                        else { errPrev += err; err = 0F; }
                    else if (errPrev > 0F && err < 0F)
                        if (errPrev < -err) { err += errPrev; errPrev = 0F; }
                        else { errPrev += err; err = 0F; }

                    //recalculate
                    if (errPrev<-0.5F || errPrev>0.5F) return false;
                }
                errPrev = err;
 
            }
            //check last E
            if (errPrev < -0.5F || errPrev > 0.5F) return false;
*/
            
        }

        //public int[] ElementWidths { get { return elementWidths; } }
        public MyPoint First { get { return status[this.foundAtLevel].points[0]; } }
        public MyPoint Last { get { return status[this.foundAtLevel].points[patternLength];} }
        public int PatternLength { get { return status[this.foundAtLevel].patternLength; } }
        public float Error { get { return status[this.foundAtLevel].error; } }

        public void setBWThreshold(ImageScaner scan)
        {
            MyPoint p = First;
            float grayWhite = 0F, grayBlack = 1F;
            for (int x = First.X; x < Last.X; x++)
            {
                p.X = x;
                float g=scan.getGray(p);
                if (scan.isBlack(p) && g<grayBlack) grayBlack=g;
                if (!scan.isBlack(p) && g > grayWhite) grayWhite = g;
            }
            scan.BWThreshold = (grayWhite  + grayBlack) / 2F;
        }
    }

    class LevelStatus
    {
        int noiseLevel;
        public int patternLength;
        int currentElement;
        int n;
        MyPoint nextPoint;
        public bool processingWhite;
        public int[] elementWidths;        
        public MyPoint[] points;
        public float error;
        public LevelStatus(int noiseLevel, int patternLength, MyPoint startPoint, bool startsWithBlack)
        {
            this.noiseLevel = noiseLevel;
            this.patternLength = patternLength;
            this.elementWidths = new int[patternLength];
            this.points = new MyPoint[patternLength+1];
            this.processingWhite = !startsWithBlack;

            this.n = 0;
            this.nextPoint = startPoint;
            this.currentElement = 0;
            this.points[0] = startPoint;
        }

        public bool isCandidate(MyPoint p, bool isBlack)
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
                            nextPoint = p;
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

        public bool terminate(MyPoint p)
        {
            if (currentElement == patternLength-1)
            {
                elementWidths[currentElement++] = n;
                points[currentElement] = p;
            }
            if (n > noiseLevel && currentElement == patternLength)
            {
                return true; //elementWidths ready to check if it is a pattern
            }
            return false;
        }

        public void skipCandidate()
        {
            for (int i = 0; i < patternLength - 2; i++)
            {
                elementWidths[i] = elementWidths[i + 2];
                points[i] = points[i + 2];
            }
            elementWidths[patternLength - 2] = n;
            points[patternLength - 2] = points[patternLength];
            points[patternLength - 1] = nextPoint;
            currentElement = patternLength - 1;
            n = 1;
            processingWhite = !processingWhite;
        }
    }
}
