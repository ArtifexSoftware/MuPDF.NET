using System;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using SkiaSharp;
using System.Diagnostics;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core
{
#if CORE_DEV
    public
#else
    internal
#endif
    class SymbologyReader1DTimeOutException : Exception { };

#if CORE_DEV
    public
#else
    internal
#endif
    abstract class SymbologyReader : IBarcodeDecoder
    {
        protected const int IntegerShift = 8;
        protected const int DifferenceScaleFactor = 1 << IntegerShift;

        private IBarcodeConsumer _consumer;

        public BlackAndWhiteImage BWImage { get; private set; }
        public int ScanStep { get; set; } = 1;
        public int MaxNumberOfBarcodesPerPage { get; set; } = 0; // max number of barcodes per page allowed (-1 or 0 for unlimited)
        public virtual bool SearchMirrored { get; set; } = false;
        public bool RequireQuietZones { get; set; } = true;
        public int MinimalDataLength { get; set; }

        protected SymbologyReader(IBarcodeConsumer consumer)
        {
            _consumer = consumer;
        }

        public virtual void Dispose() { }
        public virtual void Reset() { }
        public virtual void BeforeDecoding() { } // to be called before decoding (used in ProductReader to reset previously stored ean13 barcode as new decoding process may work with rotated image )
        public abstract FoundBarcode[] DecodeRow(int rowNumber, XBitArray row);
        public abstract SymbologyType GetBarCodeType();

        public virtual FoundBarcode[] Decode(BlackAndWhiteImage image)
        { 
            return Decode(image, 0f);
        }
        
        public virtual FoundBarcode[] Decode(BlackAndWhiteImage bwImage, float rotationAngle)
        {
            BWImage = bwImage;
            List<FoundBarcode> result = new List<FoundBarcode>();
            int passCount = 1;

			if (SearchMirrored)
			{
				// we'll make two decode passes
				// second pass is made with reversed row (it allows to decode 
				// right-to-left oriented barcodes)
				passCount = 2;
			}

            BeforeDecoding();

            bool foundDecodablePrev = false;

			for (int pass = 0; pass < passCount; pass++)
            {
                // check timeout
                TimeoutCheck();

				for (int y = 0; y < BWImage.Height; y += ScanStep)
                {
                    // check timeout
                    TimeoutCheck();

                    XBitArray row = BWImage.GetRow(y);

                    if (pass == 1)
                        row = row.Reverse();

                    // check timeout
                    TimeoutCheck();

                    bool foundDecodable = false;

					if (row.IsNil())
						Reset();

                    FoundBarcode[] rowResults = DecodeRow(y, row);
                    if (rowResults != null)
                    {
                        bool shouldStopDecoding;
                        foundDecodable = ProcessFound(false, rowResults, pass, row, result, out shouldStopDecoding, BWImage, rotationAngle);

						if (foundDecodable && y == BWImage.Height - 1) // if barcode lasts to the last row having no the bottom quite zone
	                    {
							ProcessFound(true, null, pass, row, result, out shouldStopDecoding, BWImage, rotationAngle);
	                    }

                        if (shouldStopDecoding)
                        {
                            return result.ToArray();
                        }

                        foundDecodablePrev = true;
                    }
                    else
                    {
                        if (foundDecodablePrev)
                        {
                            bool shouldStopDecoding;
                            foundDecodable = ProcessFound(true, null, pass, row, result, out shouldStopDecoding, BWImage, rotationAngle);
                            foundDecodablePrev = false;
							
							if (shouldStopDecoding)
							{
								return result.ToArray();
							}
                        }
                    }

                    if (!foundDecodable)
                    {
                        // current row does not contain decodable barcode
                        if (MaxNumberOfBarcodesPerPage > 0 && result.Count > MaxNumberOfBarcodesPerPage-1) // or we reached max number of barcodes allowed                           
                        {
                            // already have one barcode and were told
                            // to search for only one
                            // OR we have reached max allowed number of barcodes
                            // so, we just ran off last line of previously                            
                            // found barcode. exit
							return result.ToArray();
                        }
                    }

                    // check timeout
                    TimeoutCheck();
                }
            }

	        if (this is GS1DataBar.GS1DataBar)
	        {
				if (result.Count == 0)
			        return result.ToArray();

		        result.Sort(new BarcodeComparer());
		        List<FoundBarcode> newResult = new List<FoundBarcode>();
		        for (int i = 0; i < result.Count; i++)
		        {
			        FoundBarcode a = result[i];
			        if (a.Rect.Height > 1)
			        {
				        bool intersects = false;
				        for (int j = 0; j < newResult.Count && !intersects; j++)
				        {
					        FoundBarcode b = newResult[j];
					        if (a.Rect.IntersectsWith(b.Rect))
						        intersects = true;
				        }
				        if (!intersects)
					        newResult.Add(a);
			        }
		        }

		        return newResult.ToArray();
			}

			return result.ToArray();
        }

        private static Rectangle AdjustBarcodeRectangle(BlackAndWhiteImage bwImage, Rectangle barcodeRect, float rotationAngle)
        {
			if (Math.Abs(rotationAngle) > float.Epsilon)
			{
				SKPointI[] unrotatedPoints = bwImage.Unrotate(barcodeRect);
				//byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
				//GraphicsPath path = new GraphicsPath(unrotatedPoints, pointTypes);

                Rectangle rect = Utils.DrawPath(unrotatedPoints);

                return rect;
			}

	        return barcodeRect;
        }


	    /// <summary>
	    /// Post filtering retrieved results from decoder
	    /// </summary>
	    /// <param name="forceConsolidatePreviouslyFound">True if we need to force consolidate with previously found results</param>
	    /// <param name="rowResults">current results from single row</param>
	    /// <param name="pass">pass id (we have 2 passes if we process negative barcodes by reversing rows</param>
	    /// <param name="row">row itself as bit array</param>
	    /// <param name="result">array of currently approved results. it is used to check if new found barcodes are just update to existing ones (if we scan high barcodes so we have several rows with the same barcode and we should update existing result barcode by increasing its height rather then creating new barcode)</param>
	    /// <param name="shouldStopDecoding">out param if we should stop decoding</param>
	    /// <param name="bwImage"></param>
	    /// <param name="rotationAngle">rotation angle of the currently passed image (as we are trying to decode 0 angle, 90 dergees, 45 degrees, 135 degrees to find rotated barcodes</param>
	    /// <returns>returns true if we found new barcode instead of consolidating with existing one (false in this case)</returns>
	    private bool ProcessFound(
            bool forceConsolidatePreviouslyFound,
            IEnumerable<FoundBarcode> rowResults, int pass,
            XBitArray row, List<FoundBarcode> result, out bool shouldStopDecoding, 
            BlackAndWhiteImage bwImage, float rotationAngle)
        {
            bool foundDecodable = false;
            shouldStopDecoding = false;

            // check if we have empty row results passed so we should extract the latest barcode and fire the event for it
            if (rowResults == null)// && forceConsolidatePreviouslyFound)
            {
				if (result.Count > 0)
				{
					// getting the latest barcode we got previously
					FoundBarcode bb = result[result.Count - 1] as FoundBarcode;
					// no need to adjust its rect as result array contains already adjusted rectangles if needed

					// fire the event for the barcode
					if (_consumer != null && _consumer.consumeBarcode(bb))
					{
						// stop if event told us so
						shouldStopDecoding = true;
					}
				}

                // check if we have exceeded max number of barcodes expected
                if (MaxNumberOfBarcodesPerPage > 0 && result.Count > MaxNumberOfBarcodesPerPage - 1)
                {
	                shouldStopDecoding = true;
                }

                return true;
            }
            
            // else we process each new given results
            foreach (FoundBarcode rowResult in rowResults)
            {
                // check against min required value length
                if (rowResult.Value.Length >= MinimalDataLength)
                {
                    foundDecodable = true;
                    // check if we have duplicated barcode rather then new
                    bool updated = checkIfAlreadyFound(rowResult, pass, row, result);

                    // if we have new barcode
                    if (!updated)
                    {
                        // decoded a new barcode in current row

                        if (MaxNumberOfBarcodesPerPage > 0 && result.Count > MaxNumberOfBarcodesPerPage - 1) // or we reached max number of barcodes allowed                           
                        {
                            // already have one barcode and were told
                            // to search for only one
                            // OR we have reached max allowed number of barcodes
                            // so, ignore newly found barcode and exit
                            shouldStopDecoding = true;
                            break;
                        }

                        // add to main results array
                        result.Add(rowResult);

                        // we are NOT firing event right now!
                        // we are firing the event only when we got forceConsolidatePreviouslyFound = true
                        // see below for forceConsolidatePreviouslyFound=true case
                        // this way we are not firing the event for the first row in the barcode
                        // but we are waiting until we have scanned several rows and got new row with different result
                        //if (forceConsolidatePreviouslyFound)
                        //{
                            // no need to adjust the rectangle any more: the rotation is superseded by BlackAndWhiteImage.
							//rowResult.SrcRect = AdjustBarcodeRectangle(bwImage, rowResult.SrcRect, rotationAngle);

                            // fire the event
                            if (_consumer != null && _consumer.consumeBarcode(rowResult))
                            {
                                shouldStopDecoding = true;
                                break;
                            }
                       // }
                    }
                }
            }

            
            return foundDecodable;
        }

        /// <summary>
        /// Check if we already have this barcode previously found
        /// </summary>
        /// <param name="rowResult"></param>
        /// <param name="pass"></param>
        /// <param name="row"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool checkIfAlreadyFound(FoundBarcode rowResult, int pass, XBitArray row, List<FoundBarcode> result)
        {
            bool updated = false;
            Rectangle curRect = rowResult.Rect;

            if (pass == 1)
            {
                // we should reverse found barcode rect
                curRect.X = row.Size - curRect.Right;
                rowResult.Rect = curRect;
            }

            for (int i = 0; i < result.Count; i++)
            {
                FoundBarcode prevResult = result[i] as FoundBarcode;

                if (
		            (RawDataEquals(prevResult.RawData, rowResult.RawData) || Math.Abs(curRect.Y-prevResult.Rect.Y) < 5) &&
                            prevResult.BarcodeFormat == rowResult.BarcodeFormat &&
		            Math.Abs(prevResult.Confidence - rowResult.Confidence) < 0.1f
		        )
                {
                    Rectangle prevRect = prevResult.Rect;

                    if (Math.Abs(curRect.Left - prevRect.Left) < 0.2 * prevRect.Width)
                    {
                        updated = true;

                        //prevRect.Offset(0, -1);
                        Rectangle newRect = Rectangle.FromLTRB(Math.Min(prevRect.Left, curRect.Left),
                            Math.Min(prevRect.Top, curRect.Top),
                            Math.Max(prevRect.Right, curRect.Right),
                            Math.Max(prevRect.Bottom, curRect.Bottom));

                        prevResult.Rect = newRect;

                        if (rowResult.Value.Length > prevResult.Value.Length)
                        {
                            result[i] = rowResult;
                        }

                        prevResult.Confidence = (1f + (prevResult.Confidence * (float)prevRect.Height + rowResult.Confidence) / (float)(prevRect.Height + 1)) / 2f;

                        break;
                    }
                }
            }

            return updated;
        }

        protected bool readModules(XBitArray row, int offset, int[] moduleWidths)
        {
            Array.Clear(moduleWidths, 0, moduleWidths.Length);

            if (offset >= row.Size)
                return false;

            bool processingWhite = !row[offset];
            int currentModule = 0;
            int x = offset;
            int RowSize = row.Size;

            for (; x < RowSize; x++)
            {
                // if pixel is white and processing white 
                // or
                // pixel is black and processing black
                if (row[x] ^ processingWhite)
                {
                    moduleWidths[currentModule]++;
                }
                else
                {
                    // color changed
                    currentModule++;
                    if (currentModule == moduleWidths.Length)
                        return true;

                    moduleWidths[currentModule] = 1;
                    processingWhite = !processingWhite;
                }
            }

            if (currentModule == moduleWidths.Length - 1 && x == row.Size)
            {
                // last module ended on row boundary
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates difference by measuring widths and getting overall difference
        /// </summary>
        /// <param name="moduleWidths">array with widths from barcode</param>
        /// <param name="pattern">array with widths from pattern</param>
        /// <param name="maxDifference">max difference to stop</param>
        /// <returns>true if equal, false otherwise</returns>
        protected static double calcDifference(int[] moduleWidths, int[] pattern, double maxDifference)
        {
            return calcDifference(moduleWidths, pattern, maxDifference, false);
        }

        /// <summary>
        /// Minimizes module widths array to use minimal possible values
        /// For example we give: 2 4 8 and it outputs 1 2 4
        /// </summary>
        /// <param name="moduleWidths">array with module widths</param>
        protected static void MinimizeModuleWidths(ref int[] moduleWidths) {
            
            // now search for min width in the array of widths
            int minWidth = 0;
            foreach (int iWidth in moduleWidths)
            {
                if (minWidth > 0)
                {
                    // search min value
                    if (minWidth > iWidth)
                        minWidth = iWidth;
                }
                else
                    minWidth = iWidth;
            }

            // now we divide each value in widths array by the min width to find 
            // relative widths (relative to the min width)
            for (int i = 0; i < moduleWidths.Length; i++)
            {
                moduleWidths[i] = (int)Math.Round((float)(moduleWidths[i] / minWidth));
            }

        }

        /// <summary>
        /// Calculates difference by measuring NUMBER of different cells 
        /// I.e. 1 of 4 different values means 25% difference, 2 of 4 different values means 50% etc
        /// </summary>
        /// <param name="moduleWidths">array with widths from barcode</param>
        /// <param name="pattern">array with widths from pattern</param>
        /// <param name="maxDifference">max difference to stop</param>
        /// <returns>true if equal, false otherwise</returns>
        protected static float calcDifference2(int[] moduleWidths, int[] pattern, float maxDifference)
        {
            int index = 0;
            float stepValue = 1.0f / moduleWidths.Length; // get step value 
            float result = 0;
            foreach (int iW in moduleWidths)
            {
                if (iW != pattern[index++])
                    result += stepValue;

                if (result > 1)
                    break;
            }

            return result;
        }

        public static double calcDifference(int[] moduleWidths, int[] pattern, double maxDifference, bool patternIsShorter)
        {
            int[] shortArray;
            if (patternIsShorter)
                shortArray = pattern;
            else
                shortArray = moduleWidths;

            int scaledMaxDifference = (int)(maxDifference * DifferenceScaleFactor);

            int totalWidth = 0;
            int totalPatternWidth = 0;
            for (int i = 0; i < shortArray.Length; i++)
            {
                totalWidth += moduleWidths[i];
                totalPatternWidth += pattern[i];
            }

            if (totalWidth < totalPatternWidth)
            {
                // all modules width less then pattern width
                return 1; // 100% difference
            }

            int moduleScaleFactor = (totalWidth << IntegerShift) / totalPatternWidth;
            scaledMaxDifference = (scaledMaxDifference * moduleScaleFactor) >> IntegerShift;

            int totalDifference = 0;
            for (int x = 0; x < shortArray.Length; x++)
            {
                int scaledModuleWidth = moduleWidths[x] << IntegerShift;
                int scaledPatternModuleWidth = pattern[x] * moduleScaleFactor;
                
                int difference;
                if (scaledModuleWidth > scaledPatternModuleWidth)
                {
                    difference = scaledModuleWidth - scaledPatternModuleWidth;
                }
                else
                {
                    difference = scaledPatternModuleWidth - scaledModuleWidth;
                }

                if (difference > scaledMaxDifference)
                    return 1; // 100% difference

                totalDifference += difference;
             }

            return ((double) totalDifference / totalWidth) / DifferenceScaleFactor;
        }


        /// <summary>
        /// Check for white space before symbol
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="offset">The current offset.</param>
        /// <param name="startOffset">The start offset.</param>
        /// <returns></returns>
        protected bool HaveWhiteSpaceBefore(XBitArray row, int offset, int startOffset)
        {
            return row.IsRange(Math.Max(0, startOffset - (offset - startOffset) / 2), startOffset, false);
        }

        /// <summary>
        /// Check for white space after symbol
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="offset">The current offset.</param>
        /// <param name="nextOffset">The next symbol start offset.</param>
        /// <param name="strict">True if strict checking of the whitespace, not checking bounds.</param>
        /// <returns></returns>
        protected bool HaveWhiteSpaceAfter(XBitArray row, int offset, int nextOffset, bool strict)
        {
            if (strict)
            {
                // return false if outside the row's size
                if (nextOffset > row.Size)
                    return false;

                if (offset >= nextOffset)
                    return false;

                int endOffset = nextOffset;

                // return false if outside the row's size
                if (endOffset >= row.Size)
                    return false;

                return row.IsRange(offset, endOffset, false);
            }
            else
            {
                return row.IsRange(Math.Min(row.Size, nextOffset), Math.Min(row.Size, nextOffset + (nextOffset - offset) / 2), false);
            }
        }

        protected bool HaveWhiteSpaceAfter(XBitArray row, int offset, int nextOffset)
        {
            return HaveWhiteSpaceAfter(row, offset, nextOffset, false);
        }

        /// <summary>
        /// Skips the two modules.
        /// NOTE: we skip two modules in order to retain start module color.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="moduleWidths">The module widths.</param>
        protected void SkipTwoModules(ref int offset, int[] moduleWidths)
        {
            offset += moduleWidths[0] + moduleWidths[1];

            for (int i = 2; i < moduleWidths.Length; i++)
                moduleWidths[i - 2] = moduleWidths[i];

            moduleWidths[moduleWidths.Length - 2] = 0;
            moduleWidths[moduleWidths.Length - 1] = 0;
        }

        protected bool RawDataEquals(int[] left, int[] right)
        {
            if (left == right)
                return true;

            if (left == null || right == null)
                return false;

            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }

        protected int[] AsIntArray(ArrayList list)
        {
            int[] array = new int[list.Count];
            for (int i = 0; i < list.Count; i++)
                array[i] = (int)list[i];

            return array;
        }

        /// <summary>
        /// Timeout (in ticks) to abort decoding if exceeded
        /// 0 means no timeout is checked
        /// stores max allowed time, when the current time is greater 
        /// then we should throw the exception
        /// </summary>
        public long TimeoutTimeInTicks = 0;

	    protected void TimeoutCheck()
        {
            if (TimeoutTimeInTicks == 0)
                return;

            long curTicks = DateTime.Now.Ticks;

            // else check the current time against max end time
            if (TimeoutTimeInTicks < curTicks)
            {
                Debug.WriteLine(String.Format("Timeout: exceeded by {0} mseconds", (curTicks - TimeoutTimeInTicks) * TimeSpan.TicksPerMillisecond));
                throw new SymbologyReader1DTimeOutException();
            }
        }

        public class BarcodeComparer : IComparer<FoundBarcode>
        {
            public int Compare(FoundBarcode x, FoundBarcode y)
            {
                return y.Rect.Height - x.Rect.Height;
            }
        }
    }
}
