using System;
using System.Text;
using System.Collections;
using System.Drawing;
using System.Collections.Generic;

namespace BarcodeReader.Core.LegacyDecoders
{

    /// <summary>
    /// internal enum
    /// interleaved 2 of 5 submodes
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    enum I2OF5SubMode
    {
        /// <summary>
        /// interleaved 2 of 5
        /// </summary>
        Interleaved2of5,
        /// <summary>
        /// itf-14 (
        /// </summary>
        ITF14,
        GTIN14,
        Circular
    };

    /// <summary>
    /// ITF-14 decoder (similar to Interleaved 2 of 5 but requires 14 digits only)
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class ITF14Reader : I2of5Reader
    {
        public ITF14Reader() : this(null)
        {
        }

        public ITF14Reader(IBarcodeConsumer consumer) : base(consumer)
        {
            SubMode = I2OF5SubMode.ITF14; // switch to itf-14 mode
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.ITF14;
        }
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class GTIN14Reader : I2of5Reader
    {
        public GTIN14Reader() : this(null)
        {
        }

        public GTIN14Reader(IBarcodeConsumer consumer) : base(consumer)
        {
            SubMode = I2OF5SubMode.GTIN14; // switch to gtin-14 mode
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.GTIN14;
        }
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class CircularI2of5Reader : I2of5Reader
    {
        public CircularI2of5Reader() : this(null)
        {
        }

        public CircularI2of5Reader(IBarcodeConsumer consumer) : base(consumer)
        {
            SubMode = I2OF5SubMode.Circular; // switch to gtin-14 mode

            // values found during experiments

            // "after" quote zone is quite large as it is inside the "circle"
            CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR = 2.2f; // 2.2f;
            // "before" quite zone is quite small as it may start anywhere
            CONST_QUIET_ZONE_BEFORE_WIDTH_FACTOR = 0.1f;// 2.2f;

            // values found during experiments with circle barcodes 
            MaxAverageDifference = 0.5f;
            MaxSymbolDifference = 0.7f;
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.CircularI2of5;
        }
    }

    /// <summary>
    /// Interleaved 2 of 5 decoder
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class I2of5Reader : SymbologyReader
    {
        /// <summary>
        /// defines the width of the "after" quiet zone (in stop symbol width)
        /// </summary>
        protected float CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR = 2.2f;

        /// <summary>
        /// defines the width of the "before" quiet zone (in stop symbol width)
        /// </summary>
        protected float CONST_QUIET_ZONE_BEFORE_WIDTH_FACTOR = 2.2f;

        protected float MaxAverageDifference = 0.4f;
        protected float MaxSymbolDifference = 0.7f;

        private List<FoundBarcode> _cachedBarcodes = new List<FoundBarcode>();

        /// <summary>
        /// Max factor allowed for most wide bar width to current bar width 
        /// We are using this factor to filter allowed bars (i.e. we are not accepting ones which are "too" wide)
        /// </summary>
        private const float MOST_WIDE_MAX_FACTOR = 1.34f; // 1.34f is the factor that works OK for most images including noisy ones. Larger factor leads to more false positives

        private static string _alphabet = "0123456789";

        private static int[] _startPattern = { 1, 1, 1, 1 };

        // stop patter gets built when needed. it helps to
        // construct valid proportion between wide and narrow widths
        private int[] _stopPattern = { 2, 1, 1 };

        // Interleaved 2 of 5 symbology uses wide-narrow patterns for
        // it's symbols (each two symbols are packed in interleaved manner)
        // In order to simplify and speed up comparison, we pack individual
        // patterns into ints using following rule:
        // wnnnw -> 10001 -> 0x11

        private static int[] _patterns =
        {
            0x06, 0x11, 0x09, 0x18, 0x05,
            0x14, 0x0c, 0x03, 0x12, 0x0a
        };


        private ArrayList _wideModuleWidths = new ArrayList();
        private ArrayList _narrowModuleWidths = new ArrayList();

        /// <summary>
        /// internal sub mode switch to use 
        ///  ITF14 mode (similar to i2of5 but 14 digits only)
        ///  GTIN14 mode (similar to i2of5 but 14 digits only)
        ///  Circular (circular barcode based on i2of5)
        ///  or 
        ///  Normal (interleaved 2 of 5)
        /// </summary>
        protected I2OF5SubMode SubMode = I2OF5SubMode.Interleaved2of5;

        public I2of5Reader() : this(null)
        {
        }

        public I2of5Reader(IBarcodeConsumer consumer) : base(consumer)
        {
            // require quite zones by default!
            BeforeDecoding();
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.I2of5;
        }

        public override void BeforeDecoding()
        {
            // enable quiet zones by default
            RequireQuietZones = true;
            // clear cached barcodes
            _cachedBarcodes.Clear();
        }



        /// <summary>
        /// override i2of5 to always require quiet zones
        /// </summary>
        new public bool RequireQuietZones
        {
            get
            {
                // always require quite zone
                return true;
            }
            set {; }
        }


        public override FoundBarcode[] DecodeRow(int rowNumber, XBitArray row)
        {
            int offset = 0;
            while (offset < row.Size)
            {
                if (row[offset])
                    break;

                offset++;
            }

            // offset now points to a first black pixel
            int symbolStart = offset;
            bool processingWhite = false;

            // start symbol for Interleaved 2 of 5 consists of 4 modules
            int currentModule = 0;
            int[] moduleWidths = new int[4];

            for (int x = offset; x < row.Size; x++)
            {
                if (row[x] ^ processingWhite)
                {
                    moduleWidths[currentModule]++;
                }
                else
                {
                    if (currentModule == moduleWidths.Length - 1)
                    {
                        double difference = calcDifference(moduleWidths, _startPattern, MaxSymbolDifference);
                        if (difference < MaxAverageDifference)
                        {
                            int iWhiteSpace = 0;
                            foreach (int ii in moduleWidths)
                                iWhiteSpace += ii;

                            if (!RequireQuietZones || HaveWhiteSpaceBefore(row, x + (int) Math.Round(iWhiteSpace * CONST_QUIET_ZONE_BEFORE_WIDTH_FACTOR, 0), symbolStart))
                            {
                                // we have white space that is half of symbol long
                                // try to decode consecutive symbols
                                FoundBarcode found = decodeFromStartSymbol(rowNumber, row, x, symbolStart);

                                if (found == null)
                                    return null;

                                return new FoundBarcode[] { found };
                            }
                        }

                        SkipTwoModules(ref symbolStart, moduleWidths);
                        currentModule--;
                    }
                    else
                    {
                        currentModule++;
                    }

                    moduleWidths[currentModule] = 1;
                    processingWhite = !processingWhite;
                }
            }

            return null;
        }

        private FoundBarcode decodeFromStartSymbol(int rowNumber, XBitArray row, int offset, int startOffset)
        {
            _wideModuleWidths.Clear();
            _narrowModuleWidths.Clear();

            // each Interleaved 2 of 5 symbol consists of 5 modules,
            // but symbols are encoded in pairs, so we should read 10 modules
            // and the split them into two symbols
            int[] moduleWidths = new int[10];
            int[] blackModules = new int[5];
            int[] whiteModules = new int[5];

            ArrayList symbols = new ArrayList();

            int offsetBeforeSymbol = offset;
            for (; ; )
            {
                offsetBeforeSymbol = offset;
                bool read = readModules(row, offset, moduleWidths);
                if (read)
                {
                    for (int i = 0, k = 0; i < 10; i += 2, k++)
                    {
                        blackModules[k] = moduleWidths[i];
                        whiteModules[k] = moduleWidths[i + 1];
                    }

                    int whitePattern = -1;
                    int blackPattern = buildWideNarrowPattern(blackModules);
                    if (blackPattern != -1)
                        whitePattern = buildWideNarrowPattern(whiteModules);

                    if (blackPattern != -1 && whitePattern != -1)
                    {
                        int blackIndex = patternToIndex(blackPattern);
                        if (blackIndex == -1)
                            break;

                        int whiteIndex = patternToIndex(whitePattern);
                        if (whiteIndex == -1)
                            break;

                        symbols.Add(blackIndex);
                        symbols.Add(whiteIndex);

                        for (int i = 0; i < moduleWidths.Length; i++)
                            offset += moduleWidths[i];
                    }
                    else
                    {
                        // 10 modules were read successfully, but they do not
                        // describe valid symbol. but may be first 3 of them
                        // describe stop pattern
                        if (goodForStopSymbol(moduleWidths))
                        {
                            // calculate the width of the detected stop symbol
                            int stopSymbolWidthSum = 0;
                            int moduleWidthsLength = moduleWidths.Length;

                            if (SubMode == I2OF5SubMode.Circular)
                            {
                                for (int ii = 0; ii < 3; ii++)
                                {
                                    stopSymbolWidthSum += moduleWidths[ii];
                                }
                            }
                            else // non circular
                            {
                                for (int ii = 0; ii < 3; ii++)
                                {
                                    stopSymbolWidthSum += moduleWidths[moduleWidthsLength - 1 - ii];
                                }
                            }

                            // check if we have a quiet zone 
                            if (RequireQuietZones)
                            {
                                if (SubMode == I2OF5SubMode.Circular)
                                {
                                    if (RequireQuietZones && HaveWhiteSpaceAfter(row, offsetBeforeSymbol + stopSymbolWidthSum, offset + stopSymbolWidthSum + (int) Math.Round(stopSymbolWidthSum * CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR, 0), true))
                                        break;
                                }
                                else
                                {
                                    if (RequireQuietZones && HaveWhiteSpaceAfter(row, offsetBeforeSymbol + stopSymbolWidthSum, offset + (int) Math.Round(stopSymbolWidthSum * CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR, 0)))
                                        break;
                                }
                            }
                            else  // does not require white space after so break instantly
                                break;
                        }
                        return null;
                    }
                }
                else
                {
                    // failed to read 10 modules, but maybe there were enough
                    // modules (3, actually) for stop pattern
                    if (goodForStopSymbol(moduleWidths))
                    {
                        // calculate the width of the detected stop symbol
                        int stopSymbolWidthSum = 0;
                        int moduleWidthsLength = moduleWidths.Length;


                        if (SubMode == I2OF5SubMode.Circular)
                        {
                            for (int ii = 0; ii < 3; ii++)
                            {
                                stopSymbolWidthSum += moduleWidths[ii];
                            }
                        }
                        else // non circular
                        {
                            for (int ii = 0; ii < 3; ii++)
                            {
                                stopSymbolWidthSum += moduleWidths[moduleWidthsLength - 1 - ii];
                            }
                        }

                        // check if we have a quiet zone 
                        if (RequireQuietZones)
                        {
                            if (SubMode == I2OF5SubMode.Circular)
                            {
                                if (RequireQuietZones && HaveWhiteSpaceAfter(row, offsetBeforeSymbol + stopSymbolWidthSum, offset + (int) Math.Round(stopSymbolWidthSum * CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR, 0), true))
                                    break;
                            }
                            else
                            {
                                if (RequireQuietZones && HaveWhiteSpaceAfter(row, offsetBeforeSymbol + stopSymbolWidthSum, offset + (int) Math.Round(stopSymbolWidthSum * CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR, 0)))
                                    break;
                            }
                        }
                        else  // does not require white space after so break instantly
                            break;
                    }


                    return null;
                }
            }

            int stopSymbolWidth = 0;
            for (int i = 0; i < 3; i++)
                stopSymbolWidth += moduleWidths[i];

            if (SubMode == I2OF5SubMode.Circular)
            {
                if (RequireQuietZones && !HaveWhiteSpaceAfter(row, offsetBeforeSymbol + stopSymbolWidth, offset + (int) Math.Round(stopSymbolWidth * CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR, 0), true))
                    return null;
            }
            else
            {
                if (RequireQuietZones && !HaveWhiteSpaceAfter(row, offsetBeforeSymbol + stopSymbolWidth, offset + (int) Math.Round(stopSymbolWidth * CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR, 0)))
                    return null;
            }
            offset += stopSymbolWidth;

            if (
                (SubMode == I2OF5SubMode.GTIN14 || SubMode == I2OF5SubMode.ITF14)
                && symbols.Count != 14
                )
                return null;

            if (symbols.Count == 0)
                return null;

            float fConfidence = VerifyCheckSumAndGetConfidence(ref symbols);

            // no confidence so exit
            if (fConfidence < float.Epsilon)
                return null;

            // circular barcode
            if (SubMode == I2OF5SubMode.Circular)
            {
                // if confidence is not 1.00 and we have ODD number of symbols
                // then try to find the latest with the checkbox
                /*
                while (fConfidence < 0.98f && symbols.Count > 2)
                {
                    // we should try to remove the very last digit and try again
                    symbols.RemoveAt(symbols.Count - 1);
                    fConfidence = VerifyCheckSumAndGetConfidence(ref symbols);

                    if (fConfidence > 0.95f)
                        symbols.RemoveAt(symbols.Count - 1);
                    else
                        fConfidence = 0.0f;
                }
                 */

                if (symbols.Count > 2)
                    fConfidence = 0.0f;
                else
                    fConfidence = 1.0f;

                // again: no confidence so exit
                if (fConfidence < float.Epsilon)
                    return null;
            }

            int[] rawdata = AsIntArray(symbols);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < rawdata.Length; i++)
            {
                int symbol = rawdata[i];
                sb.Append(_alphabet[symbol]);
            }

            if (!CheckIfShouldBeProcessed(sb.ToString(), offset, startOffset, rowNumber, stopSymbolWidth))
                return null;

            // interleabed 2 of 5 
            // checking minimal allowed length
            if (SubMode != I2OF5SubMode.Circular)
            {
                int length = sb.Length;
                if (length < 4 || length % 2 != 0)
                {
                    // Interleaved 2 of 5 barcode must have length 
                    // greater than 6 (yeah, we try to reduce parasites)
                    // and also length must be even.
                    return null;
                }
            }

            FoundBarcode result = new FoundBarcode();
            result.Confidence = fConfidence;

            if (SubMode == I2OF5SubMode.ITF14) //(this is ITF14Reader)
                result.BarcodeFormat = SymbologyType.ITF14;
            else if (SubMode == I2OF5SubMode.GTIN14) // (this is GTIN14Reader)
                result.BarcodeFormat = SymbologyType.GTIN14;
            else if (SubMode == I2OF5SubMode.Circular)
                result.BarcodeFormat = SymbologyType.CircularI2of5;
            else
                result.BarcodeFormat = SymbologyType.I2of5;

            result.Rect = new Rectangle(startOffset, rowNumber, offset - startOffset, 1);
            result.RawData = rawdata;


            result.Value = sb.ToString();

            // check and save into cached if not yet
            AddBarcodeToCache(result);
            return result;
        }

        /// <summary>
        /// adds the barcode to the cache
        /// </summary>
        /// <param name="result"></param>
        protected void AddBarcodeToCache(FoundBarcode result)
        {
            _cachedBarcodes.Add(result);
        }


        /// <summary>
        /// Checks if new found barcode should be added 
        /// </summary>
        /// <param name="valueToCheckAsExisting"></param>
        /// <param name="offset"></param>
        /// <param name="startOffset"></param>
        /// <param name="rowNumber"></param>
        /// <param name="stopSymbolWidth"></param>
        /// <returns>Returns False if this barcode is already cached
        /// True (i.e. continue wth this barcode) otherwise</returns>
        protected bool CheckIfShouldBeProcessed(string valueToCheckAsExisting, int offset, int startOffset, int rowNumber, int stopSymbolWidth)
        {
            // checkin against cached barcodes
            // we filter barcodes which are close to the existing i2of5 more than quiet zone
            // and if new barcode is the sub-string of the existing one
            foreach (FoundBarcode exBar in _cachedBarcodes)
            {
                // check if valueToCheckAsExisting is the substring of the 
                // existing barcode
                if (
                        // if new value falls into one of existing or equal!
                        exBar.Value.IndexOf(valueToCheckAsExisting) > -1 &&
                        // if new value length LESSER than existing
                        exBar.Value.Length >= valueToCheckAsExisting.Length
                    )
                {
                    // now also check if new barcode is quite close to existing ones 
                    // first check if has the same align
                    // as we scan from top to bottom so new barcode can appear only below existing ones
                    if (
                        (
                            Math.Abs(startOffset - exBar.Rect.Left) < (int) Math.Round(stopSymbolWidth * CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR, 0) //&&
                                                                                                                                                 // bottom is quiet close to the existing barcode
                                                                                                                                                 //Math.Abs(rowNumber - exBar.SrcRect.Bottom) < (int)Math.Round(stopSymbolWidth * CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR, 0)
                        )
                        ||
                        (
                            // barcode cutted from the left or from the right
                            (
                                startOffset >= exBar.Rect.Left &&
                                Math.Abs(offset - exBar.Rect.Right) < (int) Math.Round(stopSymbolWidth * CONST_QUIET_ZONE_AFTER_WIDTH_FACTOR, 0)
                            )
                        )

                        )
                        return false; // return false as new barcode is too close to the existing one

                }
            }
            return true;
        }

        /// <summary>
        ///  verifies the checksum of the given data using mod 10 checksum algorithm
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private float VerifyCheckSumAndGetConfidence(ref ArrayList data)
        {
            int datalength = data.Count; // length of data without the very last one 
            // output result flag
            bool checksumMatched = false;
            // 2 cases:
            // 1) EVEN number of digits with checksum:
            // - should contain ODD number of digits + single checksum digit
            // if checksum matches then return confidence 1.00
            // 2) EVEN number of digits without checksum:
            // return 0.5 as confidence
            int iRetries = 0;
            for (iRetries = 0; iRetries < 2; iRetries++)
            {
                int sum = 0;
                for (int i = 0; i < datalength - 1; i++)
                    sum += (int) (data[i]) * (i % 2 == 0 ? 3 : 1);
                sum = sum % 10;
                if (sum != 0)
                    sum = 10 - sum;

                // getting the result
                checksumMatched = sum == (int) data[datalength - 1];

                if (checksumMatched)
                    break;

                // when trying for the first time then we may try to remove the very last symbol and try again
                // if checksum is not matching then we should check if we have odd number of symbols
                // like 13 symbols or 15
                // but in I2of5 we should have even number 
                // for example we may have: 16444118888780 (14 digits) and this is not correct with checksum (checksum is not zero)
                // but 1644411888878 is correct (checksum = 8 and with 13 digits)

                if (iRetries == 0)
                {
                    // try to decrease the data length the very last digit and try again
                    if (data.Count > 2 && (int) data[data.Count - 1] == 0)
                        datalength--;
                    else
                        break;
                }
                else
                    break;
            }

            if (checksumMatched)
            {
                return 1.00f;
            }
            else
            {
                if (data.Count % 2 > 0) // if ODD number of digits
                    return 0.0f;
                else
                    return 0.5f;

            }
        }


        private int buildWideNarrowPattern(int[] moduleWidths)
        {
            int currentNarrowModuleWidth = 0;
            int wideModuleCount;
            float mostWide;

            for (; ; )
            {
                mostWide = 0;

                // find thinnest module bigger then previously found one
                int minWidth = int.MaxValue;
                for (int i = 0; i < moduleWidths.Length; i++)
                {
                    int width = moduleWidths[i];
                    if (width < minWidth && width > currentNarrowModuleWidth)
                        minWidth = width;
                }

                currentNarrowModuleWidth = minWidth;
                wideModuleCount = 0;
                int wideModulesWidth = 0;
                int pattern = 0;
                for (int i = 0; i < moduleWidths.Length; i++)
                {
                    if (moduleWidths[i] > currentNarrowModuleWidth)
                    {
                        pattern |= 1 << (moduleWidths.Length - 1 - i);
                        if (mostWide < moduleWidths[i])
                            mostWide = moduleWidths[i];

                        wideModuleCount++;
                        wideModulesWidth += moduleWidths[i];
                    }
                }

                if (wideModuleCount < 2)
                {
                    // each Interleaved 2 of 5 symbol must contain 2 wide modules
                    break;
                }

                if (wideModuleCount == 2)
                {
                    // check wide modules relative width
                    for (int i = 0; i < moduleWidths.Length; i++)
                    {
                        if (moduleWidths[i] > currentNarrowModuleWidth)
                        {
                            if ((mostWide / moduleWidths[i]) > MOST_WIDE_MAX_FACTOR)
                            {
                                // this wide module width is too different
                                // from the most wide module width
                                return -1;
                            }
                        }
                    }

                    _wideModuleWidths.Add(wideModulesWidth >> 1);
                    _narrowModuleWidths.Add(currentNarrowModuleWidth);
                    return pattern;
                }
            }

            return -1;
        }

        private static int patternToIndex(int pattern)
        {
            for (int i = 0; i < _patterns.Length; i++)
            {
                if (_patterns[i] == pattern)
                    return i;
            }

            return -1;
        }

        private bool goodForStopSymbol(int[] moduleWidths)
        {

            if (SubMode == I2OF5SubMode.Circular)
            {

                if (_wideModuleWidths.Count == 0 || _narrowModuleWidths.Count == 0)
                {
                    return false;
                }
            }
            else
            {
                if (_wideModuleWidths.Count == 0 || _narrowModuleWidths.Count == 0 ||
                    _wideModuleWidths.Count % 2 != 0 ||
                    _narrowModuleWidths.Count % 2 != 0)
                {
                    return false;
                }
            }

            int wideModuleWidth = 0;
            foreach (object o in _wideModuleWidths)
                wideModuleWidth += (int) o;
            wideModuleWidth /= _wideModuleWidths.Count;

            int narrowModuleWidth = 0;
            foreach (object o in _narrowModuleWidths)
                narrowModuleWidth += (int) o;
            narrowModuleWidth /= _narrowModuleWidths.Count;

            wideModuleWidth /= narrowModuleWidth;
            //_stopPattern[0] = wideModuleWidth;
            //_stopPattern[1] = 1;
            //_stopPattern[2] = 1;

            double difference = calcDifference(moduleWidths, _stopPattern, MaxSymbolDifference, true);
            if (difference < MaxAverageDifference)
                return true;

            return false;
        }
    }
}
