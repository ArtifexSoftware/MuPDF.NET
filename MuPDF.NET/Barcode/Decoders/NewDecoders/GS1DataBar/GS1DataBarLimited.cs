using SkiaSharp;
using System;
using System.Collections;
using System.Drawing;

namespace BarcodeReader.Core.GS1DataBar
{
#if CORE_DEV
    public
#else
    internal
#endif
    class GS1DataBarLimited : GS1DataBar
    {
		public GS1DataBarLimited() : this(null)
	    {
	    }

        public GS1DataBarLimited(IBarcodeConsumer consumer) : base(consumer)
        {
            reverse = false;
            finderElements = 14 * 3; //14 elements + finder + 14 elements = 42 elements
            charElements = 14;
            finderDirection = Direction.LeftToRight;
            finderStartsWithBlack = false;
            xRestriction = false;
            this.charDecoder = new CharDecoder();
            this.finderDecoder = new FinderDecoder();
        }

	    public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.GS1DataBarLimited;
        }

        int[] N = { 26, 26 };//number of modules for char1...char2
        //Decode char1..char2 around the finder
        // char1 | finder| char2 
        //offsetIn and offsetOut points to the x coord of the first and last bar of the finder.
        override protected FoundBarcode Decode(int rowIndex, XBitArray row, int offsetIn, int offsetOut, ArrayList finders)
        {
            //raw widths
            int origin = offsetIn, end = offsetOut;
            int[][] charWidths = new int[2][];
            charWidths[0] = getElements(row, ref offsetIn, charElements, true, true);        //  16/4
            charWidths[1] = getElements(row, ref offsetOut, charElements, false, true);       //  15/4
            if (charWidths[0] == null || charWidths[1] == null)
                return null;

            //get normalized widths from raw widths
            ArrayList[] chars = new ArrayList[2];
            for (int i = 0; i < 2; i++)
            {
                chars[i] = charDecoder.getBarcodeChars(charWidths[i], N[i], MinBars.Even, recovery);
                if (chars[i].Count == 0) return null;
            }

            //validate checksum in finders
            float error;
            int[] indexs = verifyCheckSum(chars, new ArrayList[] {finders}, out error);
            if (indexs == null || error > maxError) return null;

            //calculate barcode value
            long symbol = 2013571L * ((BarcodeChar)chars[0][indexs[0]]).value + ((BarcodeChar)chars[1][indexs[1]]).value;
            FoundBarcode result = new FoundBarcode();
            origin++;
            getElements(row, ref origin, 1, false, false);
            getElements(row, ref end, 2, true, true);
            result.Rect = new SKRect(origin, rowIndex, end, rowIndex+1);
            result.Value = fullGTIN14(Convert.ToString(symbol));
            result.Confidence = (39.0F - error) / 39.0F;
            result.RawData = getRawData(chars, indexs);
            return result;
        }

        //Checks if the widths array correspond to a finder.
        //Widths are always left to right (so right finders must be previously mirrored)
        //Returns the finder index: 0..89
        //Finder in Limites are 18/7 (14 bars and 18 modules) (module=1 narrow bar width).
        //6th and 7th bars are always 1 module length. Therefore, they are not in tFinders.
        const float MIN_RATIO = 1.3611111111111111111111111111111F; // (26-1.5)/18
        const float MAX_RATIO = 1.5277777777777777777777777777778F; // (26+1.5)/18
        //Limited finder is always left to right
        override protected ArrayList IsFinder(int[] w, Direction dir)
        {
            int[] sum = new int[3];
            for (int i = 0; i < 42; i++) sum[i / 14] += w[i];

            float ratio1 = (float)sum[0] / (float)sum[1];
            float ratio2 = (float)sum[2] / (float)sum[1];
            if ((MIN_RATIO <= ratio1 && ratio1 <= MAX_RATIO) &&
                (MIN_RATIO <= ratio2 && ratio2 <= MAX_RATIO))
            {
                //extract finder
                int[] finderW = new int[14];
                for (int i = 14, j = 0; i < 28; i++) finderW[j++] = w[i];

                ArrayList nw = finderDecoder.getBarcodeChars(finderW, 18, MinBars.Last, recovery);//finder (18/7)
                if (nw.Count > 0) return nw;
            }
            return new ArrayList(0);
        }


        //Calculates the checksum of a complete barcode widths and checks if it correspond to the 
        //checksum stored in both finders.
        int[][] WEIGHT_CHAR ={new int[]{1, 3, 9, 27, 81, 65, 17, 51, 64, 14, 42, 37, 22, 66},
            new int[]{20, 60, 2, 6, 18, 54, 73, 41, 34, 13, 39, 28, 84, 74}};
        const int FINDER = 2;
        //nChars always 3
        override protected bool verifyCheckSum(BarcodeChar[] current, int nChars)
        {
            int sum = 0;
            for (int i = 0; i < 2; i++) for (int j = 0; j < 14; j++) sum += current[i].widths[j] * WEIGHT_CHAR[i][j];
            sum = sum % 89;
            return (sum == current[FINDER].value);
        }
        



        //Finder Limited bar decoder
        class FinderDecoder : Decoder
        {
            static readonly string[] tFinders = new string[]{"111111111133","111111111232","111111111331","111111121132"
            ,"111111121231","111111131131","111112111132","111112111231","111112121131","111113111131","111211111132"
            ,"111211111231","111211121131","111212111131","111311111131","121111111132","121111111231","121111121131"
            ,"121112111131","121211111131","131111111131","111111112123","111111112222","111111112321","111111122122"
            ,"111111122221","111111132121","111112112122","111112112221","111112122121","111113112121","111211112122"
            ,"111211112221","111211122121","111212112121","111311112121","121111112122","121111112221","121111122121"
            ,"121112112121","121211112121","131111112121","111111113113","111111113212","111111123112","111211113112"
            ,"121111113112","111111211123","111111211222","111111211321","111111221122","111211211122","111211211221"
            ,"111211221121","111212211121","111311211121","121111211122","121111211221","121211211121","111121111123"
            ,"111121111222","111121111321","111121121122","111121121221","111122111122","121121111122","121121111221"
            ,"121121121121","121122111121","121221111121","131121111121","112111111123","112111111222","112111111321"
            ,"112111121122","112111121221","112111131121","112112111122","112112111221","112211111122","211111111222"
            ,"211111111321","211111121122","211111121221","211111131121","211112111221","211112121121","211211111221"
            ,"211111112212"};

            override public int DecodeChar(int[] w, int N, MinBars minBars)
            {
                string code = "";
                for (int i = 0; i < 12; i++) code += w[i];
                int pos = Array.IndexOf(tFinders, code);
                return pos;
            }
        }


        //Limited char bar decoder
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
                int oddValue = getValue(oddW, 7, maxWidthOdd, minBars == MinBars.Odd);
                int evenValue = getValue(evenW, 7, maxWidthEven, minBars == MinBars.Even);
                int value = oddValue * tEven + evenValue + gSum;
                return value;
            }

            //Returns encoding characteristics for the given sumOdd, N.
            bool GetCharCharacteristics(int sumOdd, int N, out int maxWidthOdd, out int maxWidthEven,
                out int gSum, out int tOdd, out int tEven)
            {
                maxWidthEven = maxWidthOdd = gSum = tOdd = tEven = -1;
                switch (sumOdd)
                {
                    case 17:
                        maxWidthOdd = 6; maxWidthEven = 3;
                        gSum = 0; tOdd = 6538; tEven = 28;
                        break;
                    case 13:
                        maxWidthOdd = 5; maxWidthEven = 4;
                        gSum = 183064; tOdd = 875; tEven = 728;
                        break;
                    case 9:
                        maxWidthOdd = 3; maxWidthEven = 6;
                        gSum = 820064; tOdd = 28; tEven = 6454;
                        break;
                    case 15:
                        maxWidthOdd = 5; maxWidthEven = 4;
                        gSum = 1000776; tOdd = 2415; tEven = 203;
                        break;
                    case 11:
                        maxWidthOdd = 4; maxWidthEven = 5;
                        gSum = 1491021; tOdd = 203; tEven = 2408;
                        break;
                    case 19:
                        maxWidthOdd = 8; maxWidthEven = 1;
                        gSum = 1979845; tOdd = 17094; tEven = 1;
                        break;
                    case 7:
                        maxWidthOdd = 1; maxWidthEven = 8;
                        gSum = 1996939; tOdd = 1; tEven = 16632;
                        break;
                }
                return gSum != -1;
            }
        }
    }
}
