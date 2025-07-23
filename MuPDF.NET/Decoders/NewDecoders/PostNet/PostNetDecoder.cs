using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.PostNet
{   
    //Class to decode a set of two-state bars. There are only 10 digits, from 0..9.
    class PostNetDecoder : IDecoderAscDescBars
    {
        int[][] chars = new int[][] { 
            new int[]{ 1,1,0,0,0 }, 
            new int[]{ 0,0,0,1,1 }, 
            new int[]{ 0,0,1,0,1 },
            new int[]{ 0,0,1,1,0 },
            new int[]{ 0,1,0,0,1 },
            new int[]{ 0,1,0,1,0 },
            new int[]{ 0,1,1,0,0 },
            new int[]{ 1,0,0,0,1 },
            new int[]{ 1,0,0,1,0 },
            new int[]{ 1,0,1,0,0 }
        };

        public PostNetDecoder()
        {
        }

        //Decodes a set of two-state samples in groups of 5 bars. Each group leads to 1 digit (0..9).
        //Receives a double array of samples, but two-state barcodes only use the upper subset of samples.
        protected int DecodeChar(bool[][] samples, int index)
        {
            //find best match
            int minDist = Int32.MaxValue;
            int minChar = -1;
            for (int i = 0; i < chars.Length; i++)
            {
                int d = 0;
                for (int j = 0; j < 5; j++)
                {
                    d += (chars[i][j] - (samples[index + j][0]?1:0) == 0 ? 0 : 1);
                }
                if (d < minDist) { minDist = d; minChar = i; }
            }

            if (minDist < 3) return minChar;
            return -1;
        }

        //Decode each group of 5 bars using the sample bool array.
        public virtual string Decode(bool[][] samples, out float confidence)
        {
            //Bars to chars
            confidence = 1.0f;
            string code = "";
            int sum = 0;
            for (int i = 1; i < samples.Length - 1; i += 5)
            {
                int ch = DecodeChar(samples, i);
                if (i == samples.Length - 6) //checksum
                {
                    sum = sum % 10;
                    if (sum != 0) sum = 10 - sum;
                    if (sum != ch) confidence = (confidence == 1f ? 0.1f : 0f);
                }
                else
                {
                    if (ch != -1) { code += Convert.ToString(ch); sum += ch; }
                    else { code += "*"; confidence = 0.0f; }                    
                }
            }
            return code;
        }
    }
}
