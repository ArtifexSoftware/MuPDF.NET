using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.AustraliaPostCode
{
	//Each 4-state bar encodes a value 0..3. This defines a base-4 message, incluing RS correction bits.
    //The code starts with 4 bars defining the FCC, the kind of barcode. There are 3 possible message lenghts: 11, 16, 21
    //Then follows a DPID code (destination postcode identifier), and finally user data.
    //There are 2 tables to decode de barcode, table N and tableC. They are used in different parts of the message
    //FCC and DPID are encoded using tableN. User date is encoded using tableC.
    internal class PostCodeDecoder : IDecoderAscDescBars
    {
        static readonly string[] tableN = new string[] { "0", "1", "2", null, "3", "4", "5", null, "6", "7", "8", null, "9" };
        static readonly string[] tableC = new string[]{"A","B","C"," ","D","E","F","#","G","H","I","a","b",
            "c","d","e","J","K","L","f","M","N","O","g","P","Q","R","h","i","j","k","l","S","T","U",
            "m","V","W","X","n","Y","Z","0","o","p","q","r","s","1","2","3","t","4","5","6","u","7",
            "8","9","v","w","x","y","z"};


        public PostCodeDecoder()
        {
        }

        //Generic method to decode some bars from pIn, to pIn+N, in groups of step, using the given char table.
        protected string DecodeChars(int[] base4, int pIn, int N, int step, string[] table)
        {
            string code = "";
            for (int i = 0; i < N; i++)
            {
                int value = 0;
                for (int j = 0; j < step; j++) value = value*4 + base4[pIn++];
                if (value > table.Length || table[value] == null) code += "*"; //ERROR
                else code += table[value];
            }
            return code;
        }

        //Each 4-state bar encodes a value 0..3. This defines a base-4 message, incluing RS correction bits.
        //The bit string is converted to triplets of 4-state values. So, each triplet has 6 bits length.
        //There are 3 possible message lenghts: 11, 16, 21
        //Then apply Reed Solomon to this message and check if it equals to the RS bits in the message
        //IMPORTANT: Reed Solomon parameters are: words of 6 bits, add 4 correction words, and uses 67 as 
        // basis to generate polinomials!!!
        public virtual string Decode(bool[][] samples, out float confidence)
        {
            //To base 4 code
            int N=samples.Length; // 2+ 20+[1+16+31]+12 + 2= 37, 52, 67
            int[] base4 = new int[N-4]; //without start and stop bars 33, 48, 63
            for (int i = 2; i < samples.Length - 2; i++)
                base4[i - 2] = (samples[i][0] && samples[i][1] ? 0 : 
                    samples[i][0] && !samples[i][1] ? 1 : 
                    !samples[i][0] && samples[i][1] ? 2 : 3);


            //Reed Solomon
            int[] words = new int[N == 37 ? 11 : N == 52 ? 16 : 21];
            int j = 0;
            for (int i = 0; i < words.Length; i++)
                words[i] = base4[j++] * 16 + base4[j++] * 4 + base4[j++];

            ReedSolomon rs = new ReedSolomon(words, 4, 6, 67, 1);
            rs.Correct(out confidence);
            if (!rs.CorrectionSucceeded) return null;
            words=rs.CorrectedData;

            j = 0;
            for (int i = 0; i < words.Length; i++, j+=3)
            {
                int n = words[i];
                for (int k = 2; k >=0; k--) { base4[j + k] = n % 4; n >>= 2; }
            }

            
            //Decode 
            string FCC = DecodeChars(base4, 0, 2, 2, tableN); //FCC: 4 bars tableN
            string DPID= DecodeChars(base4, 4, 8, 2, tableN); //DPID: 16 base tableN
            string CUSTOMER="";
            if (FCC=="11" && N==37) {}
            else if (FCC=="59" && N==52) CUSTOMER=DecodeChars(base4,20,5,3,tableC);
            else if (FCC=="62" && N==67) CUSTOMER=DecodeChars(base4,20,10,3,tableC); 
            else return null;

            return FCC + DPID + CUSTOMER ;
        }
    }
}
