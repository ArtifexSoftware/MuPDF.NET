using System;

namespace BarcodeReader.Core.GS1DataBar
{
    internal class Encodation
    {
        protected string binaryString;
        protected bool link2D;
        protected string GTIN;
        protected string code;

        public Encodation(string s)
        {
            binaryString = s;
            link2D = s.Substring(0, 1) == "1"; //link 2D flag: we don't use it
        }

        protected string Decode(int pos, int[] bitW, int[] digitW)
        {
            string code = "";
            for (int i = 0; i < bitW.Length; i++ )
            {
                int w = bitW[i];
                int d=Decode(pos,w);
                string dd = Convert.ToString(d).PadLeft(digitW[i], '0');
                code += dd;
                pos += w;
            }
            return code;
        }

        class EndException : Exception { }
        protected int Decode(int pos, int w) 
        {
            if (pos + w > binaryString.Length) throw new EndException();
            string bin = binaryString.Substring(pos,w);
            return Convert.ToInt32(bin, 2);
        }

        protected string ZeroPad(int value, int length) 
        {
            string s=Convert.ToString(value);
            return s.PadLeft(length,'0');
        }

        //calculates checksum from a 13 digit gtin string
        static public int GTIN14CheckSum(string gtin)
        {
            int sum = 0;
            for (int i = 0; i < 13; i++) sum += Convert.ToInt32(gtin.Substring(i, 1)) * (i % 2 == 0 ? 3 : 1);
            sum = sum % 10;
            if (sum != 0) sum = 10 - sum;
            return sum;
        }

        private string GetDigit(int Dx)
        {
            if (Dx == 10) return "FNC1";
            return Convert.ToString(Dx);
        }

        private bool Latch(ref int pos, string pattern)
        {
            if (binaryString.Length >= pos + pattern.Length && 
                binaryString.Substring(pos, pattern.Length) == pattern)
            {
                pos += pattern.Length;
                return true;
            }
            return false;
        }

        enum EncodationSchema { NUMERIC, ALPHANUMERIC, ISO646 };
        const string ALPHA_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ*,-./";
        const string ISO_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const string ISO_SYMBOLS = "!\"%&'()*+,-./:;<=>?_ ";
        protected string GeneralPourpose(int pos)
        {
            bool end = false;
            string code = "";
            EncodationSchema sch = EncodationSchema.NUMERIC; //default
            while (pos < binaryString.Length && !end)
            {
                switch (sch)
                {
                    case EncodationSchema.NUMERIC:
                        if (Latch(ref pos, "0000")) 
                            sch = EncodationSchema.ALPHANUMERIC;
                        else
                        {
                            if (binaryString.Length < pos+4) end = true;
                            else if (binaryString.Length < pos + 7)
                            {
                                int D1 = Decode(pos, 4);
                                if (D1 >= 1) code += GetDigit(D1 - 1);
                                end = true;
                            }
                            else
                            {
                                int n = Decode(pos, 7) - 8;
                                int D1 = n / 11; //0..9 digits
                                int D2 = n % 11; //10 FNC1
                                code += GetDigit(D1) + GetDigit(D2);
                                pos += 7;
                            }
                        }
                        break;
                    case EncodationSchema.ALPHANUMERIC:
                        if (binaryString.Substring(pos, 1) == "0")
                        {
                            if (Latch(ref pos, "000")) sch = EncodationSchema.NUMERIC;
                            else if (Latch(ref pos, "00100")) sch = EncodationSchema.ISO646;
                            else if (binaryString.Length < pos + 5) end = true;
                            else 
                            {   //Numbers 5 bits 0xxxx   x=5..14(0..9)  and 15(FNC1)
                                int n = Decode(pos + 1, 4);
                                if (n >= 5 && n <= 14) code += ZeroPad(n - 5, 1);
                                else if (n == 15) code += "FNC1";
                                else code += "?";
                                pos += 5;
                            }
                        }
                        else
                        {   //Letters + Symbols  6 bits 1xxxxx  x=0..31
                            if (binaryString.Length < pos + 6) end = true;
                            else
                            {
                                try
                                {
                                    int n = Decode(pos + 1, 5);
                                    if (n >= 0 && n <= 31)
                                        code += ALPHA_CHARS.Substring(n, 1);
                                    else code += "?";
                                    pos += 6;
                                }
                                catch 
                                {
                                    throw new EndException();
                                }
                            }
                        }
                        break;
                    case EncodationSchema.ISO646:
                        if (binaryString.Substring(pos, 1) == "0")
                        {   
                            if (Latch(ref pos, "000")) sch = EncodationSchema.NUMERIC;
                            else if (Latch(ref pos, "00100")) sch = EncodationSchema.ALPHANUMERIC;
                            else if (binaryString.Length < pos + 5) end = true;
                            else
                            {   //Numbers 5 bits 0xxxx   x=5..14(0..9)  and 15(FNC1)
                                int n = Decode(pos + 1, 4);
                                if (n >= 5 && n <= 14) code += ZeroPad(n - 5, 1);
                                else if (n == 15) code += "FNC1";
                                else code += "?";
                                pos += 5;
                            }
                        }
                        else
                        {
                            if (binaryString.Length < pos + 7) end = true;
                            else
                            {   //Letters Uppercase/Lowecase 7 bits 1xxxxxx   x=0..51
                                int n = Decode(pos+1, 6);
                                if (n >= 0 && n <= 51)
                                {
                                    code += ISO_CHARS.Substring(n, 1);
                                    pos += 7;
                                }
                                else if (binaryString.Length < pos + 8) end = true;
                                else
                                {   //Symbol 8 bits 111xxxxx  x=8..28;
                                    n = Decode(pos + 3, 5);
                                    if (n >= 8 && n <= 28) code += ISO_SYMBOLS.Substring(n - 8, 1);
                                    else code += "?";
                                    pos += 8;
                                }
                            }
                        }
                        break;
                }
            }
            return code;
        }

        public string Code { get { return code; } }

        static public Encodation Factory(string s)
        {
            //get encodation type
            string encodationType = null;
            string[] encodationTypes = { "1", "00", "010", "0110", "0111" };
            foreach (string et in encodationTypes)
                if (s.Substring(1, et.Length) == et) { encodationType = et; break; }
            try
            {
                switch (encodationType)
                {
                    case "1": return new Encodation1(s);
                    case "00": return new Encodation00(s);
                    case "010": return new Encodation010(s);
                    case "0110": return new Encodation0110(s);
                    case "0111": return new Encodation0111(s);
                }
            }
            catch (EndException) { } //if the end has arrived prematurely
            return null;
        }
    }

    //(01)GTIN14 
    internal class Encodation1 : Encodation
    {
        public Encodation1(string s):base(s)
        {
            GTIN=Decode(4,new int[]{ 4, 10, 10, 10, 10 }, new int[]{1,3,3,3,3});
            string general = GeneralPourpose(48); //44
            code = "(01)" + GTIN + GTIN14CheckSum(GTIN) + general;
        }
    }

    //[general pourpose data]
    internal class Encodation00 : Encodation
    {
        public Encodation00(string s):base(s)
        {
            code= GeneralPourpose(5);
        }
    }

    //(01)GTIN14 Weight in Kg or pounds  010x
    internal class Encodation010 : Encodation
    {
        public Encodation010(string s): base(s)
        {
            GTIN = "9" + this.Decode(5, new int[] { 10, 10, 10, 10 }, new int[] { 3, 3, 3, 3 });
            code = "(01)" + GTIN + GTIN14CheckSum(GTIN);

            bool isKg = s.Substring(4, 1) == "0";
            int weight = Decode(45, 15);
            if (isKg) code += "(3103)" + ZeroPad(weight,6);
            else
            {
                if (weight < 10000) code += "(3202)" + ZeroPad(weight,6);
                else code += "(3203)" + ZeroPad(weight-10000,6);
            }
        }
    }

    //(01)GTIN14 + Price + [Currency] + [general pourpose data]  0110x
    internal class Encodation0110 : Encodation
    {
        public Encodation0110(string s): base(s)
        {
            GTIN = "9" + this.Decode(8, new int[] { 10, 10, 10, 10 }, new int[] { 3, 3, 3, 3 });
            code = "(01)" + GTIN + GTIN14CheckSum(GTIN);

            //has currency?
            bool hasCurrency = binaryString.Substring(5, 1) == "1";
            int generalPourposePos = (hasCurrency ? 60 : 50);

            //decimals
            int decimals = Decode(48, 2);

            if (!hasCurrency) code += "(392" + ZeroPad(decimals, 1) + ")";
            else code += "(393" + ZeroPad(decimals, 1) + ")" + ZeroPad(Decode(50, 10), 3);

            //general pourpose
            code += GeneralPourpose(generalPourposePos);            
        }
    }

    //(01)GTIN14 + Weight + Date    0111xxx
    internal class Encodation0111 : Encodation
    {
        public Encodation0111(string s):base(s)
        {
            GTIN = "9" + this.Decode(8, new int[] { 10, 10, 10, 10 }, new int[] { 3, 3, 3, 3 });
            code = "(01)" + GTIN + GTIN14CheckSum(GTIN);

            //weight
            string weightAI = (binaryString.Substring(7, 1) == "0" ? "310" : "320");
            string weight = ZeroPad(Decode(48, 20), 6);
            code += "("+weightAI + weight.Substring(0, 1) + ")0" + weight.Substring(1);

            //date
            string AI = "??", ai = binaryString.Substring(5, 2);
            switch (ai)
            {
                case "00": AI = "(11)"; break;
                case "01": AI = "(13)"; break;
                case "10": AI = "(15)"; break;
                case "11": AI = "(17)"; break;
            }
            int date = Decode(68, 16);
            int yy = date / 384;
            date = date % 384;
            int mm = date / 32 + 1;
            int dd = date % 32;
            code += AI + ZeroPad(yy, 2) + ZeroPad(mm, 2) + ZeroPad(dd, 2);
        }
    }

}
