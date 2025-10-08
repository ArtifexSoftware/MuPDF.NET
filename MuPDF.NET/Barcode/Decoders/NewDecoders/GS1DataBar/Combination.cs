using System.Collections;

namespace BarcodeReader.Core.GS1DataBar
{
    internal class Combination
    {
        ArrayList[] values;
        int[] indexs, current;
        bool end;

        public Combination(ArrayList[] values)
        {
            this.values = values;
            this.indexs = new int[values.Length];
            this.current = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
                indexs[i] = 0;
            end = false;
        }

        public int[] Current()
        {
            for (int i = 0; i < values.Length; i++)
                current[i] = (int)values[i][indexs[i]];
            return current;
        }

        //next combination
        public void Next()
        {
            bool done = false;
            int i = values.Length - 1;
            while (i >= 0 && !done)
            {
                indexs[i]++;
                if (indexs[i] < values[i].Count) done = true;
                else indexs[i--] = 0;
            }
            end = !done;
        }

        public bool End()
        {
            return end;
        }
    }
}
