using System.Drawing;
using System.Collections;
using System;
using SkiaSharp;

namespace BarcodeReader.Core.GS1DataBar
{
#if CORE_DEV
    public
#else
    internal
#endif
    class GS1DataBarOmnidirectional : GS1DataBarGroup1
    {
		public GS1DataBarOmnidirectional() : this(null)
	    {
	    }

        public GS1DataBarOmnidirectional(IBarcodeConsumer consumer) : base(consumer)
        {
            finderDirection = Direction.LeftToRight;
            finderStartsWithBlack = false;
            xRestriction = false;
        }

	    public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.GS1DataBarOmnidirectional;
        }

        int[] N = { 16, 15, 16, 15 };//number of modules for char1...char4
        //Decode char1..char2 around the finder, and next char4 rightFinder char3. 
        // char1 | left finder| char2 | char4 | right finder| char 3
        //offsetIn and offsetOut points to the x coord of the first and last bar of the finder.
        override protected FoundBarcode Decode(int rowIndex, XBitArray row, int offsetIn, int offsetOut, ArrayList leftFinders)
        {
            //raw widths
            int[][] charWidths = new int[4][];
            charWidths[0] = getElements(row, ref offsetIn, charElements, false, true);        //  16/4
            charWidths[1] = getElements(row, ref offsetOut, charElements, true, false);       //  15/4
            charWidths[3] = getElements(row, ref offsetOut, charElements, true, true);        //  15/4
            int[] rightFinderWidths = getElements(row, ref offsetOut, finderElements, true, false); //  right finder
            charWidths[2] = getElements(row, ref offsetOut, charElements, true, false);       //  16/4
            if (charWidths[0] == null || charWidths[1] == null || charWidths[2] == null || charWidths[3] == null || rightFinderWidths == null)
                return null;

            //get normalized widths from raw widths
            ArrayList[] chars = new ArrayList[4];
            for (int i= 0; i < 4; i++)
            {
                chars[i] = charDecoder.getBarcodeChars(charWidths[i], N[i], i%2==0?MinBars.Even:MinBars.Odd, recovery);
                if (chars[i].Count == 0) return null;
            }

            //decode finder
            ArrayList rightFinders = IsFinder(rightFinderWidths, Direction.LeftToRight);
            if (rightFinders.Count == 0) return null;
            ArrayList[] finders = new ArrayList[2];
            finders[0] = leftFinders;
            finders[1] = rightFinders;

            //validate checksum in finders
            float error;
            int[] indexs=verifyCheckSum(chars, finders, out error);
            if (indexs==null || error>maxError) return null;

            //calculate barcode value
            int leftPair = 1597 * ((BarcodeChar)chars[0][indexs[0]]).value + ((BarcodeChar)chars[1][indexs[1]]).value;
            int rightPair = 1597 * ((BarcodeChar)chars[2][indexs[2]]).value + ((BarcodeChar)chars[3][indexs[3]]).value;
            long symbol = 4537077 * (long)leftPair + (long)rightPair;
            FoundBarcode result=new FoundBarcode();
            offsetIn++;
            getElements(row, ref offsetIn, 1, false, false);
            getElements(row, ref offsetOut, 2, true, true); 
            result.Rect = new SKRect(offsetIn, rowIndex, offsetOut, rowIndex+1);
            result.Value = fullGTIN14(Convert.ToString(symbol));
            result.Confidence = (36.0F - error) / 36.0F;
            result.RawData = getRawData(chars, indexs); 
            return result;
        }
    }
}