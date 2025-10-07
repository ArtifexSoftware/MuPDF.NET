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
    class GS1DataBarStacked : GS1DataBarGroup1
    {
        int part = 0; //the current part of the stacked barcode. Can be 0..1
        bool isFirst; //true if is looking for the first row of the second part of a stacked barcode.
        ArrayList[] chars = new ArrayList[4];
        ArrayList[] finders = new ArrayList[2];

		public GS1DataBarStacked() : this(null)
	    {
	    }

        public GS1DataBarStacked(IBarcodeConsumer consumer) : base(consumer)
        {
        }

	    public override SymbologyType GetBarCodeType()
	    {
		    return SymbologyType.GS1DataBarStacked;
	    }

	    public override void Reset()
        {
            //reset decoder
            part = 0;
        }

        override protected void initDecodeRow(int rowNumber)
        {
            if (rowNumber == 0) Reset();
            finderDirection = (part == 0 ? Direction.LeftToRight : Direction.RightToLeft);
            finderStartsWithBlack = (part==1); 
            xRestriction = (part == 1);
        }


	    //Decode left and right chars around the finder. If part=0 decodes chars around left finder. 
        //If part=1 decodes chars around right finder.
        // char1 | left finder| char2 | char4 | right finder| char 3
        //For right finder, chars must be swapped!
        //offsetIn and offsetOut points to the x coord of the first and last bar of the finder.
        int[] N = { 16, 15 }; //number of modules for right and left char
        override protected FoundBarcode Decode(int rowIndex, XBitArray row, int offsetIn, int offsetOut, ArrayList finder)
        {
            //raw widths.
            int[][] charWidths = new int[2][];
            charWidths[part] = getElements(row, ref offsetIn, charElements, false, true);        
            charWidths[1-part] = getElements(row, ref offsetOut, charElements, true, false);     
            if (charWidths[0] == null || charWidths[1] == null) return null;

            //get normalized widths from raw widths
            for (int i = 0; i < 2; i++)
            {
                chars[2*part+i] = charDecoder.getBarcodeChars(charWidths[i], N[i], i % 2 == 0 ? MinBars.Even : MinBars.Odd, recovery);
                if (chars[2 * part + i].Count == 0) return null;
            }

            if (part == 0)
            {
                finders[0] = finder;
                part = 1;
                lastStartX = offsetIn;
                lastEndX = offsetOut;
                lastRowNumber = rowIndex;
                isFirst = true;
                return null;
            }
            else
            {
                finders[1] = finder;

                //validate checksum in finders
                float error;
                int[] indexs = verifyCheckSum(chars, finders, out error);
                if (indexs == null || error>maxError) return null;

                //calculate barcode value
                int leftPair = 1597 * ((BarcodeChar)chars[0][indexs[0]]).value + ((BarcodeChar)chars[1][indexs[1]]).value;
                int rightPair = 1597 * ((BarcodeChar)chars[2][indexs[2]]).value + ((BarcodeChar)chars[3][indexs[3]]).value;
                long symbol = 4537077 * (long)leftPair + (long)rightPair;
                FoundBarcode result = new FoundBarcode();
                int height = (isFirst ? rowIndex - lastRowNumber : 1);
                int y = (isFirst ? lastRowNumber : rowIndex);
                offsetIn++;
                getElements(row, ref offsetIn, 1, false, false);
                getElements(row, ref offsetOut, 2, true, true);
                result.Rect = new SKRect(offsetIn, y, offsetOut, y+height);
                result.Value = fullGTIN14(Convert.ToString(symbol));
				result.Confidence = (36.0F - error) / 36.0F;
                result.RawData = getRawData(chars, indexs);
                isFirst = false;
                return result;
            }
        }

    }
}
