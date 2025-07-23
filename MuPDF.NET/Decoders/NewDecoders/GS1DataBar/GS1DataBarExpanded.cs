using System.Collections;
using System.Drawing;

namespace BarcodeReader.Core.GS1DataBar
{
#if CORE_DEV
    public
#else
    internal
#endif
    class GS1DataBarExpanded : GS1DataBarGroup3
    {
		public GS1DataBarExpanded() : this(null)
	    {
	    }

        public GS1DataBarExpanded(IBarcodeConsumer consumer) : base(consumer)
        {
        }

	    public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.GS1DataBarExpanded;
        }

        //Decode char1..char2 around the finder
        // char1 | finder| char2 
        //offsetIn and offsetOut points to the x coord of the first and last bar of the finder.
        override protected FoundBarcode Decode(int rowIndex, XBitArray row, int offsetIn, int offsetOut, ArrayList finder)
        {
            ArrayList[] chars = new ArrayList[MAX_CHARS];  
            ArrayList[] finders = new ArrayList[MAX_CHARS/2];
            
            //first char
            int[] rawWidths = getElements(row, ref offsetIn, charElements, false, true);
            if (rawWidths==null) return null;
            chars[0] = charDecoder.getBarcodeChars(rawWidths, N, MinBars.Odd, recovery);
            if (chars[0].Count == 0) return null;

            //first finder
            finders[0] = finder;

            bool end = false;
            int numChar = 1;
            int numFinder=1;
            while (!end)
            {
                //read next char
                rawWidths = getElements(row, ref offsetOut, charElements, true, numChar % 2 == 0);
                if (rawWidths == null) end = true;
                else
                {
                    //normalized widths for the left char
                    ArrayList ch=charDecoder.getBarcodeChars(rawWidths, N, MinBars.Odd, recovery);
                    if (ch.Count == 0) end = true;
                    else
                    {
                        chars[numChar++] = ch; //add char

                        if (numChar % 2 == 1)  //if next to the char there is a finder...
                        {
                            //read finder 
                            bool leftToRight = (numFinder % 2 == 0);
                            rawWidths = getElements(row, ref offsetOut, finderElements, true, leftToRight);
                            if (rawWidths == null) end = true;
                            else
                            {
                                ch= IsFinder(rawWidths, leftToRight ? Direction.LeftToRight : Direction.RightToLeft);
                                if (ch.Count == 0) end = true;
                                else finders[numFinder++] = ch;
                            }
                        }
                    }
                }
            }

            //validate checksum in finders
            float error;
            int[] indexs = verifyCheckSum(chars, numChar, finders, numFinder, out error);
            if (indexs == null || error > maxError) return null;

            //get binary string and decode to ascii string
            string code = decodeChars(chars, numChar, indexs);
            if (code == null) return null;

            FoundBarcode result = new FoundBarcode();
            offsetIn++; 
            getElements(row, ref offsetIn, 1, false, false);
            getElements(row, ref offsetOut, 1, true, true);
            result.Rect = new Rectangle(offsetIn, rowIndex, offsetOut - offsetIn, 1);
            result.Value = code;
            float tError = (float) (numChar * 7 + numFinder * 4);
            result.Confidence = (tError - error) / tError; 
            result.RawData = getRawData(chars, numChar, indexs);
            return result;
        }

    }
}

