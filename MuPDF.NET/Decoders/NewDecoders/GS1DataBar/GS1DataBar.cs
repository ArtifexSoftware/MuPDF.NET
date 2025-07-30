using System.Collections;

namespace BarcodeReader.Core.GS1DataBar
{
#if CORE_DEV
    public
#else
    internal
#endif
    enum Direction { LeftToRight, RightToLeft };

#if CORE_DEV
    public
#else
    internal
#endif
    abstract class GS1DataBar : SymbologyReader
    {
        protected bool reverse; //used in derived classes to add a 2nd reversed pass in the decoder.
        protected bool reversed; //used in derived classes to know if the received row is reversed or not
        protected Direction finderDirection;
        protected bool finderStartsWithBlack;
        protected int finderElements;
        protected Decoder charDecoder, finderDecoder;
        public const float GS1_DATABAR_ALLOWED_CONFIDENCE = 0.9f;

        protected bool xRestriction;
        protected int lastStartX, lastEndX; //when part=1, interval where part 0 was found.
        protected int lastRowNumber; // row number where the barcode begins

        protected int charElements;
        protected bool recovery;
        protected static readonly float maxError = 100.0F;
        protected static float maxBarError = 0.4F;

        protected abstract ArrayList IsFinder(int[] w, Direction dir);
        protected abstract FoundBarcode Decode(int rowIndex, XBitArray row, int offsetIn, int offsetOut, ArrayList leftFinder);

        public GS1DataBar(IBarcodeConsumer consumer) : base(consumer)
        {
            //recovery = false;
            //maxError = 1e10F;

            recovery = true;
        }

        //Called for each row in the image, from 1..Height
        //Looks for a left finder in the row. When found, try to decode chars around it.
        // Checks checksum, and if success, returns the code.
        //left finders start with a white bar, and are left to right order.
        //right finders start with a black bar, and are right to left order.
        public override FoundBarcode[] DecodeRow(int rowNumber, XBitArray xx)
        {
            FoundBarcode[] forwards, backwards=null;

            initDecodeRow(rowNumber);
            XBitArray row=xx;
            reversed = false;
            forwards = iDecodeRow(rowNumber, row);            
            if (reverse)
            {
                row=xx.Reverse(); //needed for expanded stacked in even rows
                reversed = true;
                backwards = iDecodeRow(rowNumber, row);
            }
            if (forwards==null) return backwards;
            if (backwards==null) return forwards;
            FoundBarcode[] merge=new FoundBarcode[forwards.Length+backwards.Length];
            int count=0;
            foreach(FoundBarcode b in forwards) merge[count++]=b;
            foreach(FoundBarcode b in backwards) merge[count++]=b;
            return merge;
        }

        /// <summary>
        /// Gs1 Databar only: checks if found barcode provides required level of the confidence
        /// </summary>
        /// <param name="foundBarcode">found barcode object</param>
        /// <returns>true if confidence level is enough</returns>
        public bool IsAllowedConfidenceForGS1Databar(FoundBarcode foundBarcode)
        {
            // as GS1 Databar barcodes tends to generate lot of 
            // results with 0.75 confidnce or even less
            // else we check if this confidence is allowed
            return foundBarcode.Confidence > GS1_DATABAR_ALLOWED_CONFIDENCE;
        }
            
        private FoundBarcode[] iDecodeRow(int rowNumber, XBitArray row)
        {
            //set limits of the finder search. If part=0, from 0 to row.Size. If part=1, parevious limits.
            int startX = 0, endX = row.Size;
            if (xRestriction)
            {
                int inc = (rowNumber - lastRowNumber) / 10; //10%
                startX = lastStartX - inc;
                if (startX < 0) startX = 0;
                endX = lastEndX + inc;
                if (endX > row.Size) endX = row.Size;
            }

            //skip black (if finder starts with white) or white(if black) pixels
            while (startX < endX && (row[startX] ^ (finderStartsWithBlack))) startX++;

            //look for the finder
            int currentElement = 0, n = 0;
            int[] elementWidths = new int[finderElements];
            int symbolStart = startX;
            bool processingWhite = !finderStartsWithBlack;
            for (int x = startX; x < endX; x++)
            {
                if (row[x] ^ processingWhite) n++;
                else
                {
                    elementWidths[currentElement++] = n;
                    if (currentElement == finderElements)
                    {
                        ArrayList finders = IsFinder(elementWidths, finderDirection);
                        if (finders.Count>0)
                        {
                            FoundBarcode result = Decode(rowNumber, row, symbolStart, x, finders);
                            if (result != null)
                            {
                                if (IsAllowedConfidenceForGS1Databar(result))
                                {
									result.BarcodeFormat = GetBarCodeType();
                                    return new FoundBarcode[] { result };
                                }
                            }
                        }
                        SkipTwoModules(ref symbolStart, elementWidths);
                        currentElement -= 2;
                    }
                    n = 1;
                    processingWhite = !processingWhite;
                }
            }
            return null;
        }

        virtual protected void initDecodeRow(int rowNumber) { }

        //Adds the application identifier (01) and the check digit to a 13 digit GTIN value.
        protected string fullGTIN14(string value)
        {
            value = "00000000000000" + value;
            value = value.Substring(value.Length - 13);
            int sum = Encodation.GTIN14CheckSum(value);
            return "(01)" + value + sum;
        }

        //returns a widths array from the row. Starting at offset, look for numElements bars.
        //forward=true --> offset is increased. 
        //forward=false--> offset is decreased.
        //leftToRight=false, then elements are mirrored.
        protected int[] getElements(XBitArray row, ref int offset, int numElements, bool forward, bool leftToRight)
        {
            int[] w = new int[numElements];
            int currentElement = 0;
            int incOffset = (forward ? 1 : -1);
            if (!forward) offset--;
            if (offset < 0 || offset >= row.Size) return null; //empty set
            bool isWhite = !row[offset];
            int n = 0;
            while (offset >= 0 && offset < row.Size)
            {
                if (row[offset] ^ isWhite) n++;
                else
                {
                    if (forward && leftToRight || !forward && !leftToRight) w[currentElement++] = n;
                    else w[numElements - 1 - currentElement++] = n;
                    if (currentElement == numElements) break;
                    isWhite = !isWhite;
                    n = 1;
                }
                offset += incOffset;
            }
            return (currentElement == numElements ? w : null);
        }


        abstract protected bool verifyCheckSum(BarcodeChar[] current, int nChars);

        //find the combination of chars that leads to a valid checksum
        //For each char (0..3) we receive an array of possible widths due to statistical recovery 
        protected int[] verifyCheckSum(ArrayList[] chars, ArrayList[] finders, out float error)
        {
            return verifyCheckSum(chars, chars.Length, finders, finders.Length, out error);
        }

        const int MAX_COMBINATIONS = 1000;
        protected int[] verifyCheckSum(ArrayList[] chars, int nChars, ArrayList[] finders, int nFinders, out float error)
        {
            int N = nChars + nFinders;
            int[] currentIndexs = new int[N];
            BarcodeChar[] current = new BarcodeChar[N];

            //max depth of chars or finders
            int max = 0;
            for (int i = 0; i < nChars; i++) if (max < chars[i].Count) max = chars[i].Count;
            for (int i = 0; i < nFinders; i++) if (max < finders[i].Count) max = finders[i].Count;

            //initial combination
            for (int i = 0; i < currentIndexs.Length; i++) currentIndexs[i] = 0; //initial combination
            int currentMax = 0;
            int nCombinations = 0;
            while (currentMax <= max && nCombinations++<MAX_COMBINATIONS)
            {
                // check timeout
                TimeoutCheck();

                for (int i = 0; i < nChars; i++) current[i] = (BarcodeChar)chars[i][currentIndexs[i]];

                for (int i = 0, j = nChars; i < nFinders; i++, j++) current[j] = (BarcodeChar)finders[i][currentIndexs[j]];

                if (verifyCheckSum(current, nChars))
                {
                    //calculate error of the combination
                    error = 0.0F;
                    for (int i = 0; i < N; i++) error += ((BarcodeChar)current[i]).error;
                    return currentIndexs;
                }

                //next combination
                bool done = false;
                while (!done && currentMax <= max)
                {
                    // check timeout
                    TimeoutCheck();

                    int j = currentIndexs.Length - 1;
                    while (j >= 0 && !done)
                    {
                        currentIndexs[j]++;
                        int M = (j < nChars ? chars[j].Count : finders[j-nChars].Count);
                        if (currentIndexs[j] < M && currentIndexs[j] <= currentMax) done = true;
                        else currentIndexs[j--] = 0;
                    }
                    if (!done)
                    {
                        currentMax++;
                        for (int i = 0; i < currentIndexs.Length; i++)
                            currentIndexs[i] = 0;
                        currentIndexs[currentIndexs.Length - 1] = currentMax - 1; //the next look will be increased to currentMax!
                    }
                    else
                    {
                        //check that at least one currentMax exist (otherwise it is a repeated comb)
                        done = false;
                        for (int i = 0; i < currentIndexs.Length; i++)
                            if (currentIndexs[i] == currentMax) done = true;
                    }
                }
            }
            error = -1.0F;
            return null;
        }

        protected int[] getRawData(ArrayList[] chars, int[] indexs)
        {
            return getRawData(chars, chars.Length, indexs);
        }
        protected int[] getRawData(ArrayList[] chars, int n, int[] indexs)
        {
            int[] raw = new int[n];
            for (int i=0;i<n;i++)
                raw[i]=((BarcodeChar)chars[i][indexs[i]]).value;
            return raw;
        }

        public bool Recovery { get { return recovery; } set { recovery = value; } }
        public static float MaxError { get { return maxError; } }
        public static float MaxBarError { get { return maxBarError; }  }
    }
}
