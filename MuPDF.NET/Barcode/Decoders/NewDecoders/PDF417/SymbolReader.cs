using System;
using System.Collections.Generic;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.PDF417
{
    class SymbolReader
    {
        ImageScaner scan;
        Bresenham br;
        float moduleLength;
        int nBars;
        bool currentIsBlack;
        bool checkEnd;

        public SymbolReader(ImageScaner scan, MyPointF a, MyPointF b, int nBars, bool startsWithBlack, bool checkEnd, float moduleLength)
        {
            this.scan = scan;
            br = new Bresenham((MyPoint)a, (MyPoint)b);
            this.nBars = nBars;
            this.moduleLength = moduleLength;
            while (!br.End() && scan.getGrayIsBlack(br.Current) != startsWithBlack)
                br.Next();
            currentIsBlack = startsWithBlack;
            this.checkEnd = checkEnd;
        }

        public float ModuleLength { get { return moduleLength; } }
        public MyPoint Current { get { return br.Current; } }

        public void MoveTo(MyPointF p)
        {
            br.MoveTo((MyPoint)p);
        }

        public MyPoint Adjust(MyPointF p)
        {
            Bresenham l = new Bresenham(br);
            int maxN = (int)Math.Round(moduleLength*1.5F);
            l.MoveTo(p);
            if (scan.isBlack(l.Current))
            {   
                MyPoint a = AdjustBack(l, maxN);
                if (a == MyPoint.Empty)
                {
                    l = new Bresenham(br);
                    l.MoveTo(p);
                    a = AdjustForward(l, maxN);
                }
                return a;
            }
            else
            {   //go fowards
                MyPoint a = AdjustForward(l, maxN);
                if (a == MyPoint.Empty)
                {
                    l = new Bresenham(br);
                    l.MoveTo(p);
                    a = AdjustBack(l, maxN);
                }
                return a;               
            }
        }

        MyPoint AdjustBack(Bresenham l, int maxN)
        {
            int n = 0;
            MyPoint prev = l.Current;
            maxN++; //because we are going 1px beyond 
            //make sure we are on black
            while (scan.In(l.Current) && n < maxN && !scan.isBlack(l.Current))
            { l.Previous(); n++; }
            //search previous white
            while (scan.In(l.Current) && n < maxN && scan.isBlack(l.Current))
            { prev = l.Current; l.Previous(); n++; }
            return n < maxN ?prev : MyPoint.Empty;
            //return n<maxN?FindBorder(prev, l.Current):MyPoint.Empty;
        }

        MyPoint AdjustForward(Bresenham l, int maxN)
        {
            int n = 0;
            MyPoint prev = l.Current;
            //make sure we are on white
            while (scan.In(l.Current) && n < maxN && scan.isBlack(l.Current))
            { l.Next(); n++; }
            //search next black            
            while (scan.In(l.Current) && n < maxN && !scan.isBlack(l.Current))
            { prev = l.Current; l.Next(); n++; }
            return n < maxN ? l.Current : MyPoint.Empty;
            //return n < maxN ? FindBorder(prev, l.Current) : MyPoint.Empty;
        }

        MyPoint FindBorder(MyPoint prev, MyPoint current)
        {
            MyPoint p=prev;
            /*
            if (prev.X == current.X) p.X = (prev.X + current.X) / 2;
            else if (prev.X < current.X) p.X = current.X;
            else p.X = prev.X;
            if (prev.Y == current.Y) p.Y = (prev.Y + current.Y) / 2;
            else if (prev.Y < current.Y) p.Y = current.Y;
            else p.Y = prev.Y;*/
            return p;
        }

        public float Offset { get { return br.CurrentLength; } }
        public MyVectorF Vd { get { return br.Vd; } }

        //simple symbol extract method. Since we don't know where the symbol ends, we 
        //can not process noisy images. Used only to scan columns 0 and 1, because at 
        //these moment we don't know the modules length.
        public float[] NextSymbol() { return NextSymbol(this.nBars, 17); }
        public float[] NextSymbol(int numBars, int numModules) 
        {
            if (checkEnd && br.End()) return null;
            int[] barLenghts = new int[numBars];
            int nTransitions = 0;
            int n=0;
            int sumBarLengths = 0;
            while ((checkEnd && !br.End() || scan.In(br.Current)) && nTransitions < numBars)
            {
                if (scan.getGrayIsBlack(br.Current) == currentIsBlack) n++;
                else if (n > (int)(moduleLength/3F)) //a valid module
                {
                    barLenghts[nTransitions++] = n;
                    sumBarLengths += n;
                    n = 1;
                    currentIsBlack = !currentIsBlack;
                }
                else //consider as noise (join to the previous module)
                {
                    if (nTransitions > 0)
                    {
                        nTransitions--;
                        n = barLenghts[nTransitions] + n + 1;
                    }
                    else n++;
                    currentIsBlack = !currentIsBlack;                    
                }
                if (nTransitions<numBars) br.Next();
            }
            if (nTransitions==numBars) 
            {
                float currentModuleLength = (float)sumBarLengths / (float)numModules;
                float[] E = new float[nTransitions];
                for (int i = 0; i < nTransitions; i++)
                {
                    float e = (float)(barLenghts[i] + barLenghts[(i + 1)%nTransitions]) / currentModuleLength;
                    //int ie = (int)Math.Round(e);
                    //if (ie < 2) ie = 2;
                    E[i] = e;

                }
                return E;
            }
            return null;
        }

        //Method to extract possible symbols from a start and end point. 
        //It calculates the mean color of each module (it can range from 0..1).
        //Modules <0.4 are cosidered white
        //Modules >0.6 are considered black
        //Modules between 0.4 and 0.6 can be white or black --> the algorithm must follow 2 different paths.
        private float blackLevel, whiteLevel;
        public LinkedList<int[]> NextSymbol(MyPoint a, MyPoint b, float mid)
        {
            //Console.Write("a:" + a.ToString() + " -- b:" + b.ToString()+" ==> ");
            float[] modules = new float[17];
            float ml = (a - b).Length / 17F; //module length
            Bresenham l = new Bresenham(a, b);
            int n = 0;
            float currentModule = 0F;
            float nextModule = ml;
            float lastD = 0F;
            float offset = 0F;
            if (!scan.isBlack(l.Current))
            { l.Next(); offset = 1F; }

            while (!l.End())
            {
                bool isBlack = scan.isBlack(l.Current);
                l.Next();
                float d = (l.Current - a).Length - offset;
                if (d > nextModule)
                {
                    if (isBlack) currentModule += (nextModule - lastD);
                    modules[n++] = currentModule / ml;
                    currentModule = (isBlack ? d - nextModule : 0F);
                    nextModule += ml;
                }
                else
                {
                    if (isBlack) currentModule += (d - lastD);
                }
                lastD = d;
            }

            modules[0] = 1F; //first must be black
            modules[16] = 0F; //last must be white

            //foreach(float m in modules) Console.Write(" - "+m);
            //Console.WriteLine();

            this.blackLevel = mid - 0.1f;
            this.whiteLevel = mid + 0.1f;
            Lengths ll = new Lengths(blackLevel, whiteLevel);
            LinkedList<int[]> solutions = new LinkedList<int[]>();
            ProcessModules(modules, 1, ll, solutions);
            return solutions;
        }

        //alternative way to read symbols based on a simpler algorithm
        public LinkedList<int[]> NextSymbolSimple(MyPoint a, MyPoint b)
        {
            float[] modules = new float[17];
            float ml = (a - b).Length/17F; //module length
            Bresenham l = new Bresenham(a, b);
            float nextModule=ml;
            if (!scan.isBlack(l.Current))
            { l.Next();}

            //second method--> scan bar lengths
            int nBars=0;
            int[] bars = new int[8];
            bool currentIsBlack = true;
            int nErrors = 0;

            while (!l.End())
            {
                bool isBlack=scan.isBlack(l.Current);
                l.Next();
                if (currentIsBlack != isBlack) { nBars++; currentIsBlack = isBlack; }
                if (nBars < 8) bars[nBars]++; else nErrors++;
            }

            LinkedList<int[]> solutions = new LinkedList<int[]>();
            if (nBars >= 7 && nErrors < ml)
            { 
                int length = 0; for (int i = 0; i < 8; i++) length += bars[i];
                double moduleLength = (double)length / 17f;
                int[] E = new int[8];
                for (int i = 0; i < 8; i++) E[i] = (int)Math.Round((double)(bars[i] + bars[(i + 1) % 8]) / moduleLength);
                solutions.AddLast(E);
            }

            return solutions;
        }

        //Recursive method that explores all possible paths given an array of modules mean color.
        void ProcessModules(float[] modules, int i, Lengths l, LinkedList<int[]> solutions) 
        {
            while (i < 17 && (modules[i] < blackLevel || modules[i] > whiteLevel))
                l.AddModule(modules[i++]);

            if (i < 17)
            {
                if (modules[i] < (blackLevel+whiteLevel)/2f)
                {
                    float m = modules[i];
                    modules[i] = 0F;
                    ProcessModules(modules, i, new Lengths(l), solutions);
                    modules[i] = 1F;
                    ProcessModules(modules, i, new Lengths(l), solutions);
                    modules[i] = m;
                }
                else
                {
                    float m = modules[i];
                    modules[i] = 1F;
                    ProcessModules(modules, i, new Lengths(l), solutions);
                    modules[i] = 0F;
                    ProcessModules(modules, i, new Lengths(l), solutions);
                    modules[i] = m;
                }
            }
            else
            {
                if (l.Finish()) solutions.AddLast(l.ToE());
            }
        }
    }

    class Lengths {
        public int[] lengths=new int[8];
        public int nBlock=0; //current index;
        public bool processingBlack = true;
        public int n = 1;
        public float blackLevel, whiteLevel;

        public Lengths(float blackLevel, float whiteLevel)
        {
            this.blackLevel = blackLevel;
            this.whiteLevel = whiteLevel;
        }

        public Lengths(Lengths l)
        {
            this.lengths = l.lengths; //all share the same array!
            this.nBlock = l.nBlock;
            this.processingBlack = l.processingBlack;
            this.n = l.n;
            this.blackLevel = l.blackLevel;
            this.whiteLevel = l.whiteLevel;
        }

        public void AddModule(float g)
        {
            if (nBlock < 8) //if not full
            {
                if (g < blackLevel && processingBlack || g > whiteLevel && !processingBlack)
                {
                    lengths[nBlock++] = n;
                    n = 1;
                    processingBlack = !processingBlack;
                }
                else n++;
            }
        }

        public bool Finish()
        {
            if (nBlock != 7) return false;
            lengths[7] = n;
            return true;
        }

        public int[] ToE()
        {
            int[] E = new int[8];
            for (int i = 0; i < 8; i++) E[i] = lengths[i] + lengths[(i + 1)%8];
            return E;
        }
    }
}
