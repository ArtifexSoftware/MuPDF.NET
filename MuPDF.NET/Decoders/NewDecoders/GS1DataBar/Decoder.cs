using System;
using System.Collections;

namespace BarcodeReader.Core.GS1DataBar
{
#if CORE_DEV
    public
#else
    internal
#endif
    class BarcodeChar : IComparable, ICloneable
    {
        public int[] widths;
        public int[] iE;
        public int value;
        public float error;
        public BarcodeChar(int[] widths, int[] iE, int value, float error)
        {
            this.widths = widths;
            this.iE = iE;
            this.value = value;
            this.error = error;
        }
        public int CompareTo(object o)
        {
            BarcodeChar b = (BarcodeChar)o;
            return (int)((this.error - b.error) * 100F);
        }
        public object Clone()
        {
            return new BarcodeChar((int[])widths.Clone(), (int[])iE.Clone(), value, error);
        }
    }

#if CORE_DEV
    public
#else
    internal
#endif
    enum MinBars {Even, Odd, Last, None};

#if CORE_DEV
    public
#else
    internal
#endif
    abstract class Decoder
    {
        Hashtable cache = new Hashtable();

        //given an array of widths, returns the associated value.
        //minBars sets if the odd or even subset requires at least 1 width of 1 module.
        //dir especifies the direction of the char. Only used in expanded finders, to allow to calculate its value.
        abstract public int DecodeChar(int[] w, int N, MinBars minBars);

        int iDecodeChar(int[] w, int N, MinBars minBars)
        {
            for (int i = 0; i < w.Length; i++) if (w[i] < 1) return -1;
            return DecodeChar(w, N, minBars);
        }

        //receives raw bar widths, that correspond to N modules
        //and returns an array with possible decoded chars, using minBars 
        //restriction (different for each databar type: omni, limited or expanded).
        public ArrayList getBarcodeChars(int[] w, int N, MinBars restriction, bool recovery)
        {
            ArrayList chars = new ArrayList();
            //bool isEven = w.Length % 2 == 0;
            int[] iE;
            float[] fE;
            string sW;
            if (getEFromW(w, N, out iE, out fE, out sW))
            {
                int barSum;
                int[] nw;
                int ch;

                if (cache.ContainsKey(sW)) return (ArrayList)cache[sW];
                else if (getWFromE(iE, N, restriction, out barSum, out nw) &&
                    (ch = iDecodeChar(nw, N, restriction)) != -1)
                {
                    //ISO fast solution
                    chars.Add(new BarcodeChar(nw, iE, ch, 0.0F));
                    //cache[sE] = chars;
                }
                else if (recovery)
                {
                    //chars = recovery3powDigits(iE, fE, N, restriction);
                    chars = recoveryFastProbabilistic(iE, fE, N, restriction);
                }
            }
            cache[sW] = chars;
            return chars;
        }

        ArrayList recovery3powDigits(int[] iE, float[] fE, int N, MinBars restriction)
        {
            ArrayList chars = new ArrayList();
            ArrayList[] values = new ArrayList[iE.Length];
            for (int i = 0; i < iE.Length; i++)
            {
                ArrayList a = values[i] = new ArrayList(3);
                int e = (int)Math.Truncate(fE[i]);
                a.Add(e - 1);
                a.Add(e);
                a.Add(e + 1);
            }
            if (restriction == MinBars.Last) values[iE.Length - 1] = new ArrayList(new int[] { 2 });

            float maxError = 0.25F * iE.Length; //empirical error limit
            int barSum, ch;
            int[] nw, jE;
            Combination comb = new Combination(values);
            while (!comb.End())
            {
                jE = comb.Current();
                float error = 0.0F;
                for (int i = 0; i < jE.Length; i++)
                {
                    float e = fE[i] - jE[i];
                    error += e * e;
                }
                if (error < maxError && getWFromE(jE, N, restriction, out barSum, out nw) &&
                    (ch = iDecodeChar(nw, N, restriction)) != -1)
                {
                    chars.Add(new BarcodeChar(nw, (int[])jE.Clone(), ch, error));
                }
                comb.Next();
            }
            chars.Sort();
            return chars;
        }


        ArrayList recoveryFastProbabilistic(int[] iE, float[] fE, int N, MinBars restriction)
        {
            ArrayList chars = new ArrayList();
            ArrayList[] values = new ArrayList[iE.Length];
            for (int i = 0; i < iE.Length; i++)
            {
                ArrayList a = values[i] = new ArrayList(3);
                float fe = fE[i];
                int ie=(int)Math.Truncate(fe);
                a.Add(new ProbabilisticCombination.Candidate(ie, Math.Abs(fe-ie)));
                a.Add(new ProbabilisticCombination.Candidate(ie+1, Math.Abs(fe-ie-1)));
                a.Add(new ProbabilisticCombination.Candidate(ie-1, Math.Abs(fe-ie+1)));
                a.Sort();
            }
            if (restriction == MinBars.Last)
            {
                values[iE.Length - 1] = new ArrayList();
                values[iE.Length - 1].Add(new ProbabilisticCombination.Candidate(2, 0.0F));
            }

            float error=0F; //error of the current combination
            int[] jE = new int[values.Length]; //current combination
            ProbabilisticCombination comb = new ProbabilisticCombination(values);
            int count = 0;
            int MAX = iE.Length * 10;
            float MAX_ERROR = iE.Length * GS1DataBar.MaxBarError;
            while (!comb.End() && count++<MAX && error<MAX_ERROR)
            {
                int[] indexs = comb.Current(out error);
                for (int i = 0; i < indexs.Length; i++)
                    jE[i] = ((ProbabilisticCombination.Candidate)values[i][indexs[i]]).value;

                int barSum, ch;
                int[] nw;
                if (getWFromE(jE, N, restriction, out barSum, out nw) &&
                    (ch = iDecodeChar(nw, N, restriction)) != -1)
                {
                    chars.Add(new BarcodeChar(nw, (int[])jE.Clone(), ch, error));
                }
                comb.Next();
            }
            chars.Sort();
            return chars;
        }

        //Returns a normalized (rounded to int and float) E array 
        //E=width sum of each pair of bars, except the last pair.
        //w=array of element widths
        //N=num of modules
        protected bool getEFromW(int[] w, int N, out int[] iE, out float[] fE, out string sW)
        {
            //total width
            int p = 0;
            for (int i = 0; i < w.Length; i++) p += w[i];

            //compute normalized pair widths
            sW = "";
            iE = new int[w.Length - 1];
            fE = new float[w.Length - 1];
            for (int i = 0; i < w.Length - 1; i++)
            {
                float fe = (float)(w[i] + w[i + 1]) * N / p;
                int ie = (int)Math.Floor(fe + 0.5F);
                if (fe < 1.5F && fe > 1.0F) { fe=1.5F; ie = 2;}   //slight recovery
                else if (fe > 12.5F && fe <= 13.0F) {fe=12.5F; ie = 12;} //slight recovery
                else if (ie < 2 || ie > 12) return false;
                fE[i] = fe;
                iE[i] = ie;
                //sE += ie+".";
                sW += w[i] + ".";
            }
            sW += w[w.Length - 1];
            return true;
        }

        /* derive element widths from normalized edge-to-similar-edge measurements */
        protected bool getWFromE(int[] E, int N, MinBars restriction, out int barSum, out int[] widths)
        {
            widths = new int[E.Length + 1];
            if (restriction == MinBars.Last) //only for finders ending in xxx...xx11
            {
                widths[E.Length] = widths[E.Length - 1] = 1;
                barSum = 2;
                for (int i = E.Length - 2; i >= 0; i--)
                {
                    widths[i] = E[i] - widths[i + 1];
                    barSum += widths[i];
                }
                return (barSum == N);
            }
            else //chars with even widths
            {
                barSum = 0;
                bool found=false;
                for (int first = 1; !found && first < E[0]; first++)
                {
                    barSum = widths[0] = first;
                    int minEven = 10, minOdd = 10; /* start with a too big minimum */
                    if (widths[0] < minOdd) minOdd = widths[0];
                    for (int i = 1; i < E.Length + 1; i++)
                    {
                        widths[i] = E[i - 1] - widths[i - 1];
                        barSum += widths[i];
                        if (i % 2 == 1 && widths[i] < minEven) minEven = widths[i];
                        if (i % 2 == 0 && widths[i] < minOdd) minOdd = widths[i];
                    }
                    if (barSum != N) return false; //there is no solution because E has an error
                    else if (minEven >= 1 && minOdd >= 1)
                    {
                        if (restriction == MinBars.Even && minEven == 1 ||
                            restriction == MinBars.Odd && minOdd == 1 ||
                            restriction == MinBars.None)
                            return true;
                    }
                }
                return false;
            }
        }

        //returns odd widths of the w array
        protected int[] getOdd(int[] w, out int sum)
        {
            int[] odd = new int[w.Length / 2];
            sum = 0;
            for (int i = 0; i < w.Length; i += 2) sum += odd[i / 2] = w[i];
            return odd;
        }

        //returns even widths of the w array
        protected int[] getEven(int[] w, out int sum)
        {
            int[] even = new int[w.Length / 2];
            sum = 0;
            for (int i = 1; i < w.Length; i += 2) sum += even[i / 2] = w[i];
            return even;
        }

        //================= From ISO =================================================
        /* maxWidth = maximum module width of an element*/
        /* minOneModule = true will skip patterns without a one module wide element*/
        protected int getValue(int[] widths, int elements, int maxWidth, bool minOneModule)
        {
            int i, n;
            int val = 0;
            int elmWidth;
            int narrowMask = 0;
            for (n = 0, i = 0; i < elements; i++) n += widths[i];

            for (int bar = 0; bar < elements - 1; bar++)
            {
                for (elmWidth = 1, narrowMask |= (1 << bar); elmWidth < widths[bar];
                    elmWidth++, narrowMask &= ~(1 << bar))
                {
                    /* get all nk combinations */
                    int subVal = combins(n - elmWidth - 1, elements - bar - 2);
                    /* less combinations with no narrow */
                    if ((minOneModule) && (narrowMask == 0) && (n - elmWidth - (elements - bar - 1) >= elements - bar - 1))
                        subVal -= combins(n - elmWidth - (elements - bar), elements - bar - 2);

                    /* less combinations with elements > maxVal */
                    if (elements - bar - 1 > 1)
                    {
                        int lessVal = 0;
                        for (int mxwElement = n - elmWidth - (elements - bar - 2); mxwElement > maxWidth; mxwElement--)
                        {
                            lessVal += combins(n - elmWidth - mxwElement - 1, elements - bar - 3);
                        }
                        subVal -= lessVal * (elements - 1 - bar);
                    }
                    else if (n - elmWidth > maxWidth)
                    {
                        subVal--;
                    }
                    val += subVal;
                }
                n -= elmWidth;
            }
            return val;
        }

        // combins(n,r): returns the number of Combinations of r selected from n:
        // Combinations = n! / ((n – r)! * r!)
        int combins(int n, int r)
        {
            int minDenom, maxDenom;
            if (n - r > r)
            {
                minDenom = r;
                maxDenom = n - r;
            }
            else
            {
                minDenom = n - r;
                maxDenom = r;
            }
            int val = 1;
            int j = 1;
            for (int i = n; i > maxDenom; i--)
            {
                val *= i;
                if (j <= minDenom)
                {
                    val /= j;
                    j++;
                }
            }
            for (; j <= minDenom; j++)
            {
                val /= j;
            }
            return val;
        }
    }
}
