using System;
using System.Collections;

namespace BarcodeReader.Core.GS1DataBar
{
    internal class ProbabilisticCombination
    {
        internal class Candidate : IComparable
        {
            public int value;
            public float error;
            public Candidate(int value, float error)
            {
                this.value = value;
                this.error = error;
            }
            public int CompareTo(object o)
            {
                Candidate b = (Candidate)o;
                return (int)((this.error - b.error) * 100F);
            }
        }

        ArrayList[] incs;
        int[] indexs;
        float error;
        bool end;
        Hashtable h=new Hashtable();

        //set of possible values for each digit, sorted from smaller to greater error.
        public ProbabilisticCombination(ArrayList[] values)
        {
            //init delta table
            error = 0.0F;
            incs = new ArrayList[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                incs[i] = new ArrayList(values[i].Count);
                float next, initial, prev;
                initial = prev = ((Candidate)values[i][0]).error;
                error += initial;
                for (int j = 0; j < values[i].Count - 1; j++)
                {
                    next = ((Candidate)values[i][j + 1]).error;
                    incs[i].Add(next - prev);
                    prev = next;
                }
            }

            //initial combination
            indexs = new int[values.Length]; //initially all to 0
            string s = toString(indexs);
            int pos = findMinInc(indexs, null);
            h.Add(s, new CombItem(error, pos, error+(float)incs[pos][0], incs.Length));
            end = false;

        }

        public int[] Current(out float error)
        {
            error = this.error;
            return indexs;
        }

        class CombItem
        {
            public float baseError, nextError;
            public int nextPos;
            public bool[] visited;
            public CombItem(float baseError, int pos, float nextError, int N)
            {
                this.baseError = baseError;
                this.visited = new bool[N];
                this.nextPos = pos;
                this.nextError = nextError;
            }
        }
        public void Next()
        {
            CombItem minError = null;
            string minStr = null;
            IDictionaryEnumerator i = h.GetEnumerator();
            while (i.MoveNext())
            {
                CombItem e = (CombItem)i.Value;
                if (e != null && (minError == null || e.nextError < minError.nextError))
                {
                    minError = e;
                    minStr = (string)i.Key;
                }
            }
            if (minError == null) end = true;
            else
            {
                error = minError.nextError;
                toIndexs(minStr, indexs); //string to int[]

                //update current comb
                int currentPos = minError.nextPos;
                minError.visited[minError.nextPos] = true;
                int nextPos = minError.nextPos = findMinInc(indexs, minError.visited); //next candidate
                if (nextPos == -1) h[minStr] = null; //comb has no more moviments.
                else minError.nextError = minError.baseError + (float)incs[nextPos][indexs[nextPos]];

                //add new comb
                indexs[currentPos]++;
                string newStr = toString(indexs);
                if (!h.ContainsKey(newStr))
                {
                    nextPos = findMinInc(indexs, null);
                    if (nextPos != -1)
                    {
                        h.Add(newStr, new CombItem(error, nextPos, error + (float)incs[nextPos][0], incs.Length));
                    }
                }
            }
        }

        protected string toString(int[] indexs)
        {
            string s = "";
            for (int i = 0; i < indexs.Length; i++) s += indexs[i];
            return s;
        }

        protected void toIndexs(string s, int[] indexs)
        {
            char[] a = s.ToCharArray();
            for (int i = 0; i < a.Length; i++) indexs[i] = a[i] - '0';
        }

        protected int findMinInc(int[] indexs, bool[] visited)
        {
            float min = 1e10F;
            int pos = -1;
            for (int i = 0; i < indexs.Length; i++) if (indexs[i]<incs[i].Count && (visited==null || !visited[i]))
            {
                float e = (float)incs[i][indexs[i]];
                if (e < min)
                {
                    min = e;
                    pos = i;
                }
            }
            return pos;
        }

        /*
        public void Next()
        {
            float minError = 1e10F;
            int[] minIndexs=null; //next indexs
            for (int i = 0; i < incs.Length; i++) if (indexs[i] < incs[i].Count)
                {
                    //try go forward index i
                    float iError =(float)incs[i][indexs[i]];
                    indexs[i]++;
                    
                    searchMinInc(i, indexs, iError, ref minError, ref minIndexs);

                    //restore
                    indexs[i]--;
                }

            if (minIndexs == null) end = true;
            else
            {
                indexs = minIndexs;
                error += minError;
                string s = "";
                for (int i = 0; i < indexs.Length; i++) s += indexs[i];
                h.Add(s, error);
            }
        }

        void searchMinInc(int forward, int[] indexs, float error, ref float minError, ref int[] minIndexs)
        {
            string s = "";
            for (int i = 0; i < indexs.Length; i++) s += indexs[i];
            if (error < minError && !h.ContainsKey(s)) { minError = error; minIndexs = (int[])indexs.Clone(); } 
            
            for (int i = 0; i < indexs.Length; i++) if (i != forward && indexs[i] > 0)
                {
                    //try backwards
                    indexs[i]--;
                    float e=(float)incs[i][indexs[i]];
                    error -= e;
                    if (error >= 0) //try this branch
                    {
                        searchMinInc(forward, indexs, error, ref minError, ref minIndexs);
                    }

                    //restore
                    indexs[i]++; 
                    error += e;
                }
        }*/

        /*
        //next combination
        public void Next()
        {
            float f;
            bool[] visited = new bool[incs.Length]; //to false;
            int minPos = nextDelta(visited, out f);

            //move to next combination
            end = (minPos==-1);
            bool found = false;
            while (!found && !end)
            {
                error += (float)incs[minPos][indexs[minPos]]; //increment or decrement error
                indexs[minPos]++;
                if (indexs[minPos] >= incs[minPos].Count)
                {
                    indexs[minPos] = 0;
                    visited[minPos] = true;
                    minPos = nextDelta(visited, out f);
                    if (minPos == -1) end = true;
                }
                else
                {
                    found = true;
                }
            }
        }

        int nextDelta(bool[] visited, out float min)
        {
            //search min delta
            int minPos = -1;
            min = 1e10F;
            for (int i = 0; i < incs.Length; i++) if (!visited[i])
                {
                    float m2, m = (float)incs[i][indexs[i]];
                    if (m < 0)
                    {
                        visited[i] = true;
                        nextDelta(visited, out m2);
                        visited[i] = false;
                        m += m2;
                    }
                    if (m >= 0 && m < min) { min = m; minPos = i; }
                }
            return minPos;
        }
         * */

        public bool End()
        {
            return end;
        }
    }
}
