using System;
using System.IO;
using System.Reflection;

namespace BarcodeReader.Core.PDF417
{
    // holds all the possible symbols, and their patterns
    class ModuleDecoder
    {
        private readonly float[][][] mappings = new float[3][][];

        private int nModules;

        // returns the codeword associated with the given pattern in the given cluster
        public int GetCodeword(int cluster, float[] patternData)
        {
            float dist;
            int codeword= GetCodeword(cluster, patternData, out dist);
            if (dist < 0.5f)  return codeword;
            return -1;
        }
        
        public int GetCodeword(int cluster, float[] patternData, out float dist)
        {
            return GetCodewordX(cluster, patternData, out dist);
        }

        public ModuleDecoder(string resourceName, int nModules)
        {
            this.nModules = nModules;
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourcePath = null;
            foreach (string n in assembly.GetManifestResourceNames())
                if (n.EndsWith(resourceName, StringComparison.Ordinal))
                {
                    resourcePath = n;
                    break;
                }
            
            var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
                throw new Exception("Could not load embedded resource.");
            StreamReader reader = new StreamReader(stream);
            string data = reader.ReadToEnd();
            string[] mappingsData = data.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            mappings[0] = new float[mappingsData.Length][];
            mappings[1] = new float[mappingsData.Length][];
            mappings[2] = new float[mappingsData.Length][];
            int i=0;
            foreach (string mp in mappingsData)
            {
                string[] parts = mp.Split(' ');
                int codeWord = int.Parse(parts[0]);
                int cluster0 = int.Parse(parts[1]);
                int cluster3 = int.Parse(parts[2]);
                int cluster6 = int.Parse(parts[3]);

                mappings[0][i]=toE(cluster0);
                mappings[1][i]=toE(cluster3);
                mappings[2][i]=toE(cluster6);
                i++;
            }

            stream.Close();
        }

        float[] toE(int w)
        {
            int[] ws = new int[8]; //PDF417 has 8 segments BWBWBWBW
            int i = 7;
            while (i >= 0) { ws[i--] = w % 10; w /= 10; }
            float[] e = new float[8];
            for (int j = 0; j < 8; j++) e[j] = (float)( ws[j] + ws[(j + 1) % 8]);
            return e;
        }

        //Min dist search
        private int GetCodewordX(int cluster, float[] patternData, out float minD)
        {
            int min = -1;
            minD = float.MaxValue;

            if (cluster % 3 != 0 || cluster > 6 || cluster < 0) return -1;

            float[][] tE = mappings[cluster/3];

            for(int k=0;k<tE.Length;k++)
            {
                float[] E=tE[k];
                float dist = 0f;
                for (int i = 0; i < E.Length; i++)
                {
                    float d = E[i] - patternData[i];
                    dist += d * d;
                }
                if (dist < minD) { minD = dist; min = k; }
            }
            if (minD < 2f) return min;
            return -1;
        }
    }
}