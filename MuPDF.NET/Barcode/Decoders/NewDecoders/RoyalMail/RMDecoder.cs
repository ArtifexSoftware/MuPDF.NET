using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.RoyalMail
{
    //In RM, each 4 bars encodes a different char. Ascendent bars encodes a value (indexs table). 
    //The same applies to descendent bars. These 2 indexs are used to access the char table.
    //RM has not CRC 
    class RMDecoder : IDecoderAscDescBars
    {
        static readonly int[] indexs = new int[] { -1, -1, -1, 1, -1, 2, 3, -1, -1, 4, 5, -1, 0, -1, -1, -1 };
        string[][] chars = new string[][] { 
            new string[]{ "Z", "U", "V", "W", "X", "Y" },
            new string[]{ "5", "0", "1", "2", "3", "4" }, 
            new string[]{ "B", "6", "7", "8", "9", "A" },
            new string[]{ "H", "C", "D", "E", "F", "G" },
            new string[]{ "N", "I", "J", "K", "L", "M" }, 
            new string[]{ "T", "O", "P", "Q", "R", "S" }
        };

        public RMDecoder()
        {
        }


        //First decode ascendent and descendent bars to 2 indexs, and then access to the char array.
        protected string DecodeChar(bool[][] samples, int index) { int a,b; return DecodeChar(samples,index,out a, out b);}
        protected string DecodeChar(bool[][] samples, int index, out int iAsc, out int iDesc)
        {
            int ascendant = 0;
            int descendant = 0;
            for (int j = 0; j < 4; j++)
            {
                ascendant = (ascendant << 1) + (samples[index + j][0] ? 1 : 0);
                descendant = (descendant << 1) + (samples[index + j][1] ? 1 : 0);
            }

            iAsc = indexs[ascendant];
            iDesc = indexs[descendant];
            if (iAsc == -1 || iDesc == -1) return null;
            return chars[iAsc][iDesc];
        }

        //Decode each group of 4 bars using the sample bool array.
        public virtual string Decode(bool[][] samples, out float confidence)
        {
            //Bars to chars
            confidence = 1.0f;
            string code = "";
            int sumAsc = 0, sumDesc = 0;
            for (int i = 1; i < samples.Length - 1; i += 4)
            {
                int iAsc, iDesc;
                string ch = DecodeChar(samples, i, out iAsc, out iDesc);
                if (i == samples.Length - 5)
                {
                    if (iAsc != sumAsc % 6 || iDesc != sumDesc % 6) confidence=0f;
                }
                else
                {
                    code += ch;
                    sumAsc += iAsc;
                    sumDesc += iDesc;
                }
            }
            return code;
        }
    }
}
