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
    class GS1DataBarExpandedStacked : GS1DataBarGroup3
    {
		public GS1DataBarExpandedStacked() : this(null)
	    {
	    }

        public GS1DataBarExpandedStacked(IBarcodeConsumer consumer) : base(consumer)
        {
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.GS1DataBarExpandedStacked;
		}

        public override void Reset()
        {
            //reset decoder
            partialSolutions = null;
            firstFinderSize = -1;
            charsInFirstRow = -1;
            reverse = false;    //initially only process rows forwards
            xStart = xEnd = -1; //bounding box 
            yStart = 0;
        }
        override protected void initDecodeRow(int rowNumber)
        {
            if (rowNumber == 0) Reset(); 
        }

        class PartialSolution
        {
            int nChars;
            ArrayList[] chars;
            int[] finderSequence;
            int length, lastIndex;

            public PartialSolution(int length, ArrayList chars, int charIndex, int[] finderSequence)
            {
                this.length = length;
                this.lastIndex = 0;  //index of the last char added
                this.chars = new ArrayList[length];
                this.nChars = 0;
                foreach (ArrayList ch in chars) this.chars[this.nChars++] = ch;
                this.chars[0] = new ArrayList();
                this.chars[0].Add(((ArrayList)chars[0])[charIndex]);
                this.finderSequence = finderSequence;
            }

            //PRE: finders[0] contains a start finder A1 (value 0)
            static public PartialSolution CanStart(ArrayList firstRowchars, int firstCharIndex, ArrayList finders)
            {
                ArrayList firstChar = (ArrayList)firstRowchars[0];
                int length = ((BarcodeChar)firstChar[firstCharIndex]).value / 211 + 4;
                if (length < 4 || length > 22) return null;
                //check finders sequence
                int[] finderSequence = FINDERS_SEQUENCES[(length - 3) / 2];
                for (int i = 1; i < finders.Count; i++)
                {
                    ArrayList f = (ArrayList)finders[i];
                    bool found = false;
                    foreach (BarcodeChar bc in f) if (bc.value == finderSequence[i]) { found = true; break; }
                    if (!found) return null;
                }
                return new PartialSolution(length, firstRowchars, firstCharIndex, finderSequence);
            }

            //returns an array of PartialSolutions that can start given a row (chars+finders).
            static public ArrayList CanStart(ArrayList chars, ArrayList finders)
            {
                //search A1 finder (value 0)
                if (finders.Count == 0) return null;
                ArrayList f = (ArrayList)finders[0];
                bool start = false;
                foreach (BarcodeChar bc in f) if (bc.value == 0) { start = true; break; }
                if (!start) return null;

                ArrayList sols = new ArrayList();
                ArrayList firstChar = (ArrayList)chars[0];
                Hashtable h = new Hashtable();
                for (int i = 0; i < firstChar.Count; i++)
                {
                    int value=((BarcodeChar)firstChar[i]).value;
                    if (!h.ContainsKey(value))
                    {
                        PartialSolution ps = PartialSolution.CanStart(chars, i, finders);
                        if (ps != null)
                        {
                            sols.Add(ps);
                            h.Add(value,value);
                        }
                    }
                }
                return sols;
            }

            //if finders follow finder sequence in the partial solution, add chars to the partial solution
            //returns true if new chars are added.
            public bool AddRow(ArrayList chars, ArrayList finders)
            {
                //try to add finders to the end
                bool right = true;
                for (int i = 0; i < finders.Count; i++)
                {
                    int nextFinderInSeq=nChars / 2 + i;
                    if (nextFinderInSeq>=finderSequence.Length) {right=true; break;} //
                    ArrayList f = (ArrayList)finders[i];
                    bool found = false;
                    foreach (BarcodeChar bc in f) if (bc.value == finderSequence[nextFinderInSeq])
                        { found = true; break; }
                    if (!found) { right = false; break; }
                }
                if (right) //if finders are in correct order, add chars to the partial solution
                {
                    int start = nChars;
                    foreach (ArrayList ch in chars) if (this.nChars<this.chars.Length) 
                        this.chars[this.nChars++]=ch;
                    lastIndex = start;
                    return true;
                }
                return false;
            }

            public bool Completed()
            {
                return nChars >= length;
            }

            public void RollBack()
            {
                nChars = lastIndex;
            }

            public ArrayList[] Chars { get { return chars; } }
        }

        ArrayList partialSolutions = null;
        int firstFinderSize = -1;
        int charsInFirstRow = -1;
        int xStart, xEnd, yStart;

        //Decode char1..char2 around the finder
        // char1 | finder| char2 
        //offsetIn and offsetOut points to the x coord of the first and last bar of the finder.
        override protected FoundBarcode Decode(int rowIndex, XBitArray row, int offsetIn, int offsetOut, ArrayList finder)
        {
            ArrayList chars = new ArrayList();
            ArrayList finders = new ArrayList();
            int finderSize = offsetOut - offsetIn; //remember finder size
            if (this.firstFinderSize != -1) //check finder size
            {
                int diff = Math.Abs(finderSize - firstFinderSize);
                int inc = firstFinderSize / 10;
                if (diff > inc) return null;
            }

            //first row char
            int[] rawWidths = getElements(row, ref offsetIn, charElements, false, true);
            if (rawWidths == null) return null;
            ArrayList ch= charDecoder.getBarcodeChars(rawWidths, N, MinBars.Odd, recovery);
            if (ch.Count== 0) return null;
            chars.Add(ch);
            int rowEnd=offsetOut, rowStart = offsetIn; //remember where row starts and ends

            //first row finder
            finders.Add(finder);

            bool end = false;
            while (!end)
            {
                //read next char
                rawWidths = getElements(row, ref offsetOut, charElements, true, chars.Count % 2 == 0);
                if (rawWidths == null) end = true;
                else
                {
                    //normalized widths for the char
                    ch = charDecoder.getBarcodeChars(rawWidths, N, MinBars.Odd, recovery);
                    if (ch.Count == 0) end = true;
                    else
                    {
                        chars.Add(ch); //add char
                        rowEnd = offsetOut;

                        if (chars.Count % 2 == 1)  //if next to the char there is a finder...
                        {
                            //read finder 
                            bool leftToRight = (finders.Count % 2 == 0);
                            rawWidths = getElements(row, ref offsetOut, finderElements, true, leftToRight);
                            if (rawWidths == null) end = true;
                            else
                            {
                                ch = IsFinder(rawWidths, leftToRight ? Direction.LeftToRight : Direction.RightToLeft);
                                if (ch.Count == 0) end = true;
                                else finders.Add(ch);
                            }
                        }
                    }
                }
            }


            if (partialSolutions == null) //first row
            {
                if (chars.Count < 4 || chars.Count % 2 != 0) return null;
                partialSolutions = PartialSolution.CanStart(chars, finders);
                this.firstFinderSize = finderSize;
                this.charsInFirstRow = chars.Count;

                UpdateMinMax(row, rowStart, rowEnd, finders.Count);
                yStart = rowIndex;
                reverse = true; //enable row reverse process.
             }
            else
            {
                foreach (PartialSolution ps in partialSolutions)
                    if (ps.AddRow(chars, finders))
                    {
                        UpdateMinMax(row, rowStart, rowEnd, finders.Count);
                        if (ps.Completed())
                        {
                            //validate checksum in finders
                            float error;
                            int[] indexs = verifyCheckSum(ps.Chars, ps.Chars.Length, null, 0, out error);
                            if (indexs == null || error > maxError) return null;

                            //rollback last update, so next rows also finish the partialSolution
                            ps.RollBack();

                            //get binary string and decode to ascii string
                            string code = decodeChars(ps.Chars, ps.Chars.Length, indexs);
                            if (code == null) return null;

                            FoundBarcode result = new FoundBarcode();
                            result.Rect = new Rectangle(xStart, yStart, xEnd - xStart, rowIndex-yStart);
							result.Value = code;
                            float tError = (float) (ps.Chars.Length * 7 + ps.Chars.Length / 2 * 4);
							result.Confidence = (tError - error) / tError;
                            result.RawData = getRawData(ps.Chars, indexs);
                            yStart = rowIndex;
                            return result;
                        }
                    }
            }
            return null; //no complete solution found
        }

        void UpdateMinMax(XBitArray row, int offsetIn, int offsetOut, int nFinders)
        {
            offsetIn++;
            getElements(row, ref offsetIn,1, false, false);
            getElements(row, ref offsetOut, nFinders%2==0?2:1, true, true);
            if (reversed) { 
                int tmp = offsetOut;  
                offsetOut = row.Size - offsetIn; 
                offsetIn = row.Size - tmp; 
            }
            if (xStart == -1 || offsetIn < xStart) xStart = offsetIn;
            if (xEnd == -1 || offsetOut > xEnd) xEnd = offsetOut;
        }
    }
}
