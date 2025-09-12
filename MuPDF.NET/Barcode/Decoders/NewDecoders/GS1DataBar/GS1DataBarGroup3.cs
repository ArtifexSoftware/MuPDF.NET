using System;
using System.Collections;

namespace BarcodeReader.Core.GS1DataBar
{
#if CORE_DEV
    public
#else
    internal
#endif
    abstract class GS1DataBarGroup3: GS1DataBar
    {
        public GS1DataBarGroup3(IBarcodeConsumer consumer) : base(consumer)
        {
            this.reverse = false;
            this.finderElements = 5; //15 elements
            this.charElements = 8;
            this.finderDirection = Direction.LeftToRight;
            this.finderStartsWithBlack = false;
            this.xRestriction = false;
            this.charDecoder = new CharDecoder();
            this.finderDecoder = new FinderDecoder();
        }

        //Checks if the widths array correspond to a finder.
        //Widths are always left to right (so even finders must be previously mirrored)
        //Returns the finder index: 0..6
        //Finder in Limites are 18/7 (14 bars and 18 modules) (module=1 narrow bar width).
        //6th and 7th bars are always 1 module length. Therefore, they are not in tFinders.
        const float MIN_RATIO = 0.79166666666666666666666666666667F; // 9.5/12
        const float MAX_RATIO = 0.89285714285714285714285714285714F; // 12.5/14
        //direction in w always left to right. Used to calculate the value 2*index+1 if rightToLeft
        override protected ArrayList IsFinder(int[] w, Direction dir)
        {
            //w1,2 -- w2,3 ratio check
            int sumLeft = w[1] + w[2];
            int sum = sumLeft + w[3] + w[4];
            float ratio = (float)sumLeft / (float)sum;
            if ((MIN_RATIO <= ratio && ratio <= MAX_RATIO))
            {
                ArrayList nw = finderDecoder.getBarcodeChars(w, 15, MinBars.Last, recovery); //finder 15/2
                if (nw.Count > 0)
                {
                    ArrayList a = new ArrayList();
                    foreach (BarcodeChar b in nw)
                    {
                        BarcodeChar c = (BarcodeChar)b.Clone();
                        c.value = c.value * 2 + (dir == Direction.LeftToRight ? 0 : 1);
                        a.Add(c);
                    }
                    return a;
                }
            }
            return new ArrayList(0);
        }


        protected const int N = 17;//number of modules for chars
        const int K = 4; //number of white and black bars. Total bars=2*K
        protected const int MAX_CHARS = 22; //maximun number of chars of a GS1 databar expanded barcode

        protected string decodeChars(ArrayList[] chars, int nChars, int[] indexs)
        {
            //calculate barcode binary string
            string s = "";
            for (int i = 1; i < nChars; i++) 
                s += Convert.ToString(((BarcodeChar)chars[i][indexs[i]]).value, 2).PadLeft(12, '0');

            //decode 
            Encodation enc = Encodation.Factory(s);
            if (enc == null) return null;
            return enc.Code;
        }





        //Calculates the checksum of a complete barcode widths and checks if it correspond to the 
        //checksum stored in both finders.
        override protected bool verifyCheckSum(BarcodeChar[] current, int nChars)
        {
            //extract length and checksum from first char 
            int length = current[0].value / 211 + 4;
            if (length < 4 || length > 22) return false;
            if (length != nChars) return false;
            int checkSum = current[0].value % 211;

            //check finder sequence associated to the given length if needed
            //Expanded Stacked checks finders during scanning
            int[] finderSequence = FINDERS_SEQUENCES[(length - 3) / 2];
            if (nChars > current.Length)
            {
                for (int i = 0; i < finderSequence.Length; i++)
                    if (finderSequence[i] != current[nChars + i].value) return false;
            }

            //calculate checksum
            int sum = 0;
            for (int i = 1; i < length; i++)
                for (int j = 0; j < 8; j++)
                {
                    int weight = WEIGHT_CHAR[2 * finderSequence[i / 2] + i % 2][j];
                    sum += current[i].widths[j] * weight;
                }
            sum = sum % 211;
            return (sum == checkSum);
        }

        const int A1 = 0, A2 = 1, B1 = 2, B2 = 3, C1 = 4, C2 = 5, D1 = 6, D2 = 7, E1 = 8, E2 = 9, F1 = 10, F2 = 11;
        protected static readonly int[][] FINDERS_SEQUENCES = new int[][]{
            new int[]{A1,A2},
            new int[]{A1,B2,B1},
            new int[]{A1,C2,B1,D2},
            new int[]{A1,E2,B1,D2,C1},
            new int[]{A1,E2,B1,D2,D1,F2},
            new int[]{A1,E2,B1,D2,E1,F2,F1},
            new int[]{A1,A2,B1,B2,C1,C2,D1,D2},
            new int[]{A1,A2,B1,B2,C1,C2,D1,E2,E1},
            new int[]{A1,A2,B1,B2,C1,C2,D1,E2,F1,F2},
            new int[]{A1,A2,B1,B2,C1,D2,D1,E2,E1,F2,F1}
        };
        static readonly int[][] WEIGHT_CHAR = new int[][]{
            new int[]{0,0,0,0,0,0,0,0},
            new int[]{1,3,9,27,81,32,96,77},
            new int[]{20,60,180,118,143,7,21,63},
            new int[]{189,145,13,39,117,140,209,205},
            new int[]{193,157,49,147,19,57,171,91},
            new int[]{62,186,136,197,169,85,44,132},
            new int[]{185,133,188,142,4,12,36,108},
            new int[]{113,128,173,97,80,29,87,50},
            new int[]{150,28,84,41,123,158,52,156},
            new int[]{46,138,203,187,139,206,196,166},
            new int[]{76,17,51,153,37,111,122,155},
            new int[]{43,129,176,106,107,110,119,146},
            new int[]{16,48,144,10,30,90,59,177},
            new int[]{109,116,137,200,178,112,125,164},
            new int[]{70,210,208,202,184,130,179,115},
            new int[]{134,191,151,31,93,68,204,190},
            new int[]{148,22,66,198,172,94,71,2},
            new int[]{6,18,54,162,64,192,154,40},
            new int[]{120,149,25,75,14,42,126,167},
            new int[]{79,26,78,23,69,207,199,175},
            new int[]{103,98,83,38,114,131,182,124},
            new int[]{161,61,183,127,170,88,53,159},
            new int[]{55,165,73,8,24,72,5,15},
            new int[]{45,135,194,160,58,174,100,89}
        };    
    
    }


    //Finder Grup3 bar decoder (expanded and stacked expanded
    class FinderDecoder : Decoder
    {
        static readonly string[] tFinders = new string[] { "184", "364", "346", "328", "265", "229" };
        override public int DecodeChar(int[] w, int N, MinBars minBars)
        {
            string code = "" + w[0] + w[1] + w[2];
            int pos = Array.IndexOf(tFinders, code);
            return pos;
        }
    }


    //Group1 char bar decoder (expanded and stacked expanded
    class CharDecoder : Decoder
    {
        //Given a widths array corresponding to a char of N modules and K white bars + K black bars
        //returns the encoded value.
        override public int DecodeChar(int[] w, int N, MinBars minBars)
        {
            int sumOdd, sumEven;
            int[] oddW = getOdd(w, out sumOdd);
            int[] evenW = getEven(w, out sumEven);
            int maxWidthOdd, maxWidthEven, gSum, tOdd, tEven;
            if (!GetCharCharacteristics(sumOdd, N, out maxWidthOdd, out maxWidthEven, out gSum, out tOdd, out tEven)) return -1;
            int oddValue = getValue(oddW, 4, maxWidthOdd, minBars == MinBars.Odd);
            int evenValue = getValue(evenW, 4, maxWidthEven, minBars == MinBars.Even);
            int value = oddValue * tEven + evenValue + gSum;
            return value;
        }

        //Returns encoding characteristics for the given sumOdd, N and K.
        bool GetCharCharacteristics(int sumOdd, int N, out int maxWidthOdd, out int maxWidthEven,
            out int gSum, out int tOdd, out int tEven)
        {
            maxWidthEven = maxWidthOdd = gSum = tOdd = tEven = -1;
            switch (sumOdd)
            {
                case 12:
                    maxWidthOdd = 7; maxWidthEven = 2;
                    gSum = 0; tOdd = 87; tEven = 4;
                    break;
                case 10:
                    maxWidthOdd = 5; maxWidthEven = 4;
                    gSum = 348; tOdd = 52; tEven = 20;
                    break;
                case 8:
                    maxWidthOdd = 4; maxWidthEven = 5;
                    gSum = 1388; tOdd = 30; tEven = 52;
                    break;
                case 6:
                    maxWidthOdd = 3; maxWidthEven = 6;
                    gSum = 2948; tOdd = 10; tEven = 104;
                    break;
                case 4:
                    maxWidthOdd = 1; maxWidthEven = 8;
                    gSum = 3988; tOdd = 1; tEven = 204;
                    break;
            }
            return gSum != -1;
        }
    }
}
