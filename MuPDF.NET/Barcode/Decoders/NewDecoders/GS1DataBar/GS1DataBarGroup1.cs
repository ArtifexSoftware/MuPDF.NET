using System;
using System.Collections;

namespace BarcodeReader.Core.GS1DataBar
{
#if CORE_DEV
    public
#else
    internal
#endif
    abstract class GS1DataBarGroup1: GS1DataBar
    {
        public GS1DataBarGroup1(IBarcodeConsumer consumer) : base(consumer)
        {
            this.reverse = false;
            this.finderElements = 5;
            this.charElements = 8;
            this.charDecoder = new CharDecoder();
            this.finderDecoder = new FinderDecoder();
        }

        //Checks if the widths array correspond to a finder.
        //Widths are always left to right (so right finders must be previously mirrored)
        //Returns the finder index: 0..9
        //Finder in Group1 are 5/15 (5 bars and 15 modules) (module=1 narrow bar width).
        //4th and 5th bars are always 1 module length. Therefore, they are not in tFinders.
        const float MIN_RATIO = 0.79166666666666666666666666666667F; //9.5/12;
        const float MAX_RATIO = 0.89285714285714285714285714285714F; //12.5/14;

        override protected ArrayList IsFinder(int[] w, Direction dir)
        {
            //w1,2 -- w2,3 ratio check
            int leftPair, rightPair;
            if (dir == Direction.LeftToRight) { leftPair = w[1] + w[2]; rightPair = w[3] + w[4]; }
            else { leftPair = w[3] + w[2]; rightPair = w[1] + w[0]; }
            int sum = leftPair + rightPair;
            if (sum == 0) return new ArrayList(0);
            float ratio1 = (float)leftPair / sum;
            if ((MIN_RATIO <= ratio1 && ratio1 <= MAX_RATIO))
            {
                if (dir==Direction.RightToLeft) { int tmp; tmp=w[0]; w[0]=w[4]; w[4]=tmp; tmp=w[1]; w[1]=w[3]; w[3]=tmp;}
                ArrayList nw = finderDecoder.getBarcodeChars(w, 15, MinBars.Last, recovery);
                if (nw.Count > 0) return nw;
            }
            return new ArrayList(0);
        }

        //Calculates the checksum of a complete barcode widths and checks if it correspond to the 
        //checksum stored in both finders.
        int[][] WEIGHT_CHAR ={new int[]{1, 3, 9, 27, 2, 6, 18, 54},new int[]{4, 12, 36, 29, 8, 24, 72, 58},
            new int[]{16, 48, 65, 37, 32, 17, 51, 74}, new int[]{64, 34, 23, 69, 49, 68, 46, 59}};
        const int LEFT_FINDER = 4;
        const int RIGHT_FINDER = 5;
        override protected bool verifyCheckSum(BarcodeChar[] current, int nChars)
        {
            int sum = 0;
            for (int i = 0; i < 4; i++) for (int j = 0; j < 8; j++) sum += current[i].widths[j] * WEIGHT_CHAR[i][j];
            sum = sum % 79;
            int tmp = sum;
            if (tmp >= 8) tmp++;
            if (tmp >= 72) tmp++;
            int leftF = tmp / 9;
            int rightF = tmp % 9;
            return (leftF == current[LEFT_FINDER].value) && (rightF == current[RIGHT_FINDER].value);
        }




        //Finder Grup1 bar decoder
        class FinderDecoder : Decoder
        {
            static readonly string[] tFinders = { "382", "355", "337", "319", "274", "256", "238", "157", "139" };
            override public int DecodeChar(int[] w, int N, MinBars minBars)
            {
                string code = "" + w[0] + w[1] + w[2];
                int pos = Array.IndexOf(tFinders, code);
                return pos;
            }
        }


        //Group1 char bar decoder
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
                int oddValue = getValue(oddW, 4, maxWidthOdd, minBars==MinBars.Odd);
                int evenValue = getValue(evenW, 4, maxWidthEven, minBars==MinBars.Even);
                int value = -1;
                if (N == 16) value = oddValue * tEven + evenValue + gSum;
                else if (N == 15) value = evenValue * tOdd + oddValue + gSum;
                return value;
            }


            //Returns encoding characteristics for the given sumOdd, N and K.
            bool GetCharCharacteristics(int sumOdd, int N, out int maxWidthOdd, out int maxWidthEven,
                out int gSum, out int tOdd, out int tEven)
            {
                maxWidthEven = maxWidthOdd = gSum = tOdd = tEven = -1;
                if (N == 16)
                {
                    switch (sumOdd)
                    {
                        case 12:
                            maxWidthOdd = 8; maxWidthEven = 1;
                            gSum = 0; tOdd = 161; tEven = 1;
                            break;
                        case 10:
                            maxWidthOdd = 6; maxWidthEven = 3;
                            gSum = 161; tOdd = 80; tEven = 10;
                            break;
                        case 8:
                            maxWidthOdd = 4; maxWidthEven = 5;
                            gSum = 961; tOdd = 31; tEven = 34;
                            break;
                        case 6:
                            maxWidthOdd = 3; maxWidthEven = 6;
                            gSum = 2015; tOdd = 10; tEven = 70;
                            break;
                        case 4:
                            maxWidthOdd = 1; maxWidthEven = 8;
                            gSum = 2715; tOdd = 1; tEven = 126;
                            break;
                    }
                }
                else if (N == 15)
                {
                    switch (sumOdd)
                    {
                        case 5:
                            maxWidthOdd = 2; maxWidthEven = 7;
                            gSum = 0; tOdd = 4; tEven = 84;
                            break;
                        case 7:
                            maxWidthOdd = 4; maxWidthEven = 5;
                            gSum = 336; tOdd = 20; tEven = 35;
                            break;
                        case 9:
                            maxWidthOdd = 6; maxWidthEven = 3;
                            gSum = 1036; tOdd = 48; tEven = 10;
                            break;
                        case 11:
                            maxWidthOdd = 8; maxWidthEven = 1;
                            gSum = 1516; tOdd = 81; tEven = 1;
                            break;
                    }
                }
                return gSum != -1;
            }
        }
    }
}
