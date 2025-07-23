using System;
using System.Text;
using System.Drawing;
using System.Collections;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.LegacyDecoders
{
    /// <summary>
    /// Reads "product barcodes" (EAN-13, EAN-8, UPC-A, UPC-E)
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class EANReader_old : SymbologyReader
    {
        private static int[] _startStopPattern = { 1, 1, 1 };
        private static int[] _middlePattern = { 1, 1, 1, 1, 1 };

        // Please take a look at:
        // http://www.barcodeisland.com/ean13.phtml
        // for the additional information about encoding table

        // Odd parity patterns for left-hand encoding
        private static int[][] _leftOddPatterns = 
        {
            new int[] {3, 2, 1, 1}, // 0
            new int[] {2, 2, 2, 1}, // 1
            new int[] {2, 1, 2, 2}, // 2
            new int[] {1, 4, 1, 1}, // 3
            new int[] {1, 1, 3, 2}, // 4
            new int[] {1, 2, 3, 1}, // 5
            new int[] {1, 1, 1, 4}, // 6
            new int[] {1, 3, 1, 2}, // 7
            new int[] {1, 2, 1, 3}, // 8
            new int[] {3, 1, 1, 2}  // 9
        };

        // All left-hand encoding patterns (odd parity and even parity)
        private static int[][] _leftPatterns =
        {
            // odd parity
            _leftOddPatterns[0], _leftOddPatterns[1],
            _leftOddPatterns[2], _leftOddPatterns[3],
            _leftOddPatterns[4], _leftOddPatterns[5],
            _leftOddPatterns[6], _leftOddPatterns[7],
            _leftOddPatterns[8], _leftOddPatterns[9],

            // even parity
            new int[] {1, 1, 2, 3}, // 0
            new int[] {1, 2, 2, 2}, // 1
            new int[] {2, 2, 1, 2}, // 2
            new int[] {1, 1, 4, 1}, // 3
            new int[] {2, 3, 1, 1}, // 4
            new int[] {1, 3, 2, 1}, // 5
            new int[] {4, 1, 1, 1}, // 6
            new int[] {2, 1, 3, 1}, // 7
            new int[] {3, 1, 2, 1}, // 8
            new int[] {2, 1, 1, 3}  // 9
        };

        // In order to simplify and speed up comparison, we pack individual
        // patterns of first digits into ints using following rule:
        // 5 (Even, Even, Odd, Odd, Even) -> 11001 -> 0x19

        private static int[] _ean13FirstDigitPatterns = 
        {
            0x00, 0x0b, 0x0d, 0x0e, 0x13,
            0x19, 0x1c, 0x15, 0x16, 0x1a
        };

        private static int[] _upceStopPattern = { 1, 1, 1, 1, 1, 1 };

        // In order to simplify and speed up comparison, we pack individual
        // patterns of first digits into ints using following rule:
        // 5 (Even, Odd, Odd, Even, Even, Odd) -> 100110 -> 0x26 for number system 0
        // 5 (Odd, Even, Even, Odd, Odd, Even) -> 011001 -> 0x19 for number system 1

        private static int[][] _upcePatterns =
        {
            // for number system 0
            new int[] {0x38, 0x34, 0x32, 0x31, 0x2c, 0x26, 0x23, 0x2a, 0x29, 0x25},

            // for number system 1
            new int[] {0x07, 0x0b, 0x0d, 0x0e, 0x13, 0x19, 0x1c, 0x15, 0x16, 0x1a}
        };

        private static int[] _supplementStartPattern = { 1, 1, 2 };
        private static int[] _supplementSeparatorPattern = { 1, 1 };

        // In order to simplify and speed up comparison, we pack individual
        // patterns of EAN-5 structures into ints using following rule:
        // 5 (Odd, Odd, Even, Even, Odd) -> 00110 -> 0x06
        private static int[] _ean5Structures = 
        {
            0x18, 0x14, 0x12, 0x11, 0x0c, 0x06, 0x03, 0x0a, 0x09, 0x05
        };

        private const double MaxAverageDifference = 0.42f;
        private const double MaxSymbolDifference = 0.6f;

        private const float MaxAverageDifferenceEAN2 = 0.42f;
        private const float MaxSymbolDifferenceEAN2 = 0.6f;

        private const float MaxAverageDifferenceEAN5 = 0.42f;
        private const float MaxSymbolDifferenceEAN5 = 0.6f;

        private SymbologyType _currentSymbology = SymbologyType.Unknown;
       
        private bool _findEan13;
        private bool _findEan8;
        private bool _findUpca;
        private bool _findUpce;
        private bool _findEan2;
        private bool _findEan5;
        private bool _findOrphanedSupplementals;

        /// <summary>
        /// Indicates if we have found EAN13 already so we should scan for EAN2 or EAN5 (if _findOrphanedSupplementals == true)
        /// </summary>
        private bool _ean13found = false;
        private Rectangle _lastEan13Rectangle = new Rectangle();

        private int[] _moduleWidths = new int[4];

        private int _patternOffset;
        private int _afterPatternOffset;

        private ArrayList _digits = new ArrayList();

		public EANReader_old() : this(null)
	    {
	    }

        public EANReader_old(IBarcodeConsumer consumer) : base(consumer)
        {
            BeforeDecoding();
        }

        public bool FindEan13
        {
            get { return _findEan13; }
            set { _findEan13 = value; }
        }

        public bool FindEan8
        {
            get { return _findEan8; }
            set { _findEan8 = value; }
        }

        public bool FindUpca
        {
            get { return _findUpca; }
            set { _findUpca = value; }
        }

        public bool FindUpce
        {
            get { return _findUpce; }
            set { _findUpce = value; }
        }

        public bool FindEan2
        {
            get { return _findEan2; }
            set { _findEan2 = value; }
        }
        public bool FindEan5
        {
            get { return _findEan5; }
            set { _findEan5 = value; }
        }

        public bool FindOrphanedSupplementals
        {
            get { return _findOrphanedSupplementals; }
            set { _findOrphanedSupplementals = value; }
        }

		public override SymbologyType GetBarCodeType()
		{
			return _currentSymbology;
		}

        public override void BeforeDecoding()
        {
            //reset decoder
            _ean13found = false; // set that we have not found ean13 yet
        }

        public override FoundBarcode[] DecodeRow(int rowNumber, XBitArray row)
        {
            _patternOffset = 0;
            _afterPatternOffset = 0;

            for (; ; )
            {
                if (!findPattern(row, _afterPatternOffset, false, _startStopPattern))
                    return null;

                int patternWidth = _afterPatternOffset - _patternOffset;
                if (!RequireQuietZones || HaveWhiteSpaceBefore(row, _patternOffset + 2 * patternWidth, _patternOffset))
                {
                    // we have white space that have the same length as the
                    // start pattern width
                    // try to decode consecutive symbols
                    FoundBarcode found = decodeFromStartSymbol(rowNumber, row);

                    if (found != null)
                    {

                        if (found.BarcodeType == SymbologyType.EAN13)
                        {
                            _ean13found = true; // indicate that we found ean13                        
                            // save last ean13 rect
                            _lastEan13Rectangle = found.Rect;

                            // if we have found ean13 already but we should NOT search for supplemental ean2 or ean5
                            // so we should exit as well
                            if (!_findOrphanedSupplementals)
                                return new FoundBarcode[] { found };
                        }
                        else // non ean 13 barcode so we simply return found barcode
                            return new FoundBarcode[] { found };

                    }

                    // if we do not have previous ean13 so we should not continue anyway
                    if (!_ean13found && !FindOrphanedSupplementals)
                        return null;

                    // we go here if we have ean13 and we need to search for supplemental barcode

                    // if we have NOT found ean13 previously then we should exit anyway
                    //if (_findOrphanedSupplementals && found.Type != SymbologyType.EAN13))
                    //    _afterPatternOffset = 0;

                    FoundBarcode foundSupplement = null;
                    if (_findEan2 || _findEan5)
                    {
                        if (findPattern(row, _afterPatternOffset, false, _supplementStartPattern))
                            foundSupplement = decodeSupplementFromStartSymbol(rowNumber, row);
                    }

                    if (foundSupplement != null)
                    {
                        // filter supplemented barcodes from main barcode
                        // first checking if rectangles intersects with last ean13

                        Rectangle rectSupplemental = foundSupplement.Rect;
                        bool allowThisSupplemental = !rectSupplemental.IntersectsWith(_lastEan13Rectangle);

                        // NOTE: supplemental EAN2/5 should be placed at the right (if aligned horizontally form left to right)
                        // OR at the bottom (if aligned vertically from top to bottom)

                        // additional check if supplemental do not crossing largest
                        // side of ean 13 barcode
                        if (allowThisSupplemental){
                            // checking by width (both ean13 and ean2/5 in the same row)
                            if (_lastEan13Rectangle.Width > _lastEan13Rectangle.Height){
                                allowThisSupplemental =
                                    // placed at the right and do not intersect
                                    (rectSupplemental.Left > _lastEan13Rectangle.Right && rectSupplemental.Right > _lastEan13Rectangle.Right) &&
                                    // and located at max 50% of the ean13 width 
                                    (rectSupplemental.Left < (_lastEan13Rectangle.Right + _lastEan13Rectangle.Width / 2)) &&
                                    // and not far away down as well
                                    (rectSupplemental.Top < (_lastEan13Rectangle.Bottom + _lastEan13Rectangle.Width / 2));

                            }
                            else {
                                // else checking by the height (both ean13 and ean2/5 in the same column)
                                allowThisSupplemental =
                                    // placed at the bottom and do not intersect
                                    (rectSupplemental.Top > _lastEan13Rectangle.Bottom && rectSupplemental.Top > _lastEan13Rectangle.Bottom) &&
                                    // located at least less than 50% of the original barcode width
                                    (rectSupplemental.Top < (_lastEan13Rectangle.Bottom + _lastEan13Rectangle.Height / 2)) &&
                                    // and not far away right as well
                                    (rectSupplemental.Left < (_lastEan13Rectangle.Right + _lastEan13Rectangle.Height/ 2));

                            }
                        }

                        if (allowThisSupplemental)
                        {
                            // so we have supplemental
                            // so we should reset found ean13 flag and its rectangle as 
                            // we could have one supplemental per single ean13 only
                            _ean13found = false;

                            if (found != null)
                                // return the pair of current ean 13 barcode and found supplement
                                return new FoundBarcode[] { found, foundSupplement };
                            else
                                // return the supplemental barcode only
                                return new FoundBarcode[] { foundSupplement };
                        }
                        else
                        {
                            if (found != null)
                                return new FoundBarcode[] { found };
                            else
                                return null;
                        }

                    }
                    else if (found != null)
                        return new FoundBarcode[] { found };

                    return null;
                }
            }
        }

        protected bool findPattern(XBitArray row, int offset, bool patternStartsFromWhite, int[] pattern)
        {
            if (patternStartsFromWhite)
            {
                while (offset < row.Size)
                {
                    if (!row[offset])
                        break;

                    offset++;
                }
            }
            else
            {
                while (offset < row.Size)
                {
                    if (row[offset])
                        break;

                    offset++;
                }
            }

            bool processingWhite = patternStartsFromWhite;

            int[] moduleWidths = new int[pattern.Length];
            int currentModule = 0;

            for (int x = offset; x < row.Size; x++)
            {
                if (row[x] ^ processingWhite)
                {
                    moduleWidths[currentModule]++;
                }
                else
                {
                    if (currentModule == pattern.Length - 1)
                    {
                        if (calcDifference(moduleWidths, pattern, MaxSymbolDifference) < MaxAverageDifference)
                        {
                            _patternOffset = offset;
                            _afterPatternOffset = x;
                            return true;
                        }

                        SkipTwoModules(ref offset, moduleWidths);
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

            return false;
        }

        private FoundBarcode decodeFromStartSymbol(int rowNumber, XBitArray row)
        {
            int startSymbolOffset = _patternOffset;
            int firstDigitOffset = _afterPatternOffset;

            if (!decodeFromStartImpl(row, startSymbolOffset, firstDigitOffset))
                return null;

            if (_digits.Count < 6)
                return null;

            return packResult(row, rowNumber, startSymbolOffset);
        }

        private FoundBarcode packResult(XBitArray row, int rowNumber, int startSymbolOffset)
        {
            int patternWidth = 3 * (_afterPatternOffset - _patternOffset);
            if (RequireQuietZones && !HaveWhiteSpaceAfter(row, _afterPatternOffset - patternWidth, _afterPatternOffset))
            {
                // sorry, but we should have white space that have the same
                // length as the stop pattern width
                // try to decode consecutive symbols
                return null;
            }

            int[] rawData = AsIntArray(_digits);
            if (!verifyCheckSum(rawData))
                return null;

            FoundBarcode result = new FoundBarcode();
			result.BarcodeType = _currentSymbology;
			result.Value = decodeRawData(rawData);
            result.Rect = new Rectangle((int)startSymbolOffset, rowNumber, _afterPatternOffset - startSymbolOffset, 1);
            result.RawData = rawData;

            if (_currentSymbology == SymbologyType.EAN13)
            {
                // found a value encoded with EAN-13 
                // we need to check if this value can and should be converted 
                // to UPC-A or should be ignored.

                if (!_findEan13 && result.Value[0] == '0' && _findUpca)
                {
                    // converting to UPC-A
					result.Value = result.Value.Substring(1);
					result.BarcodeType = SymbologyType.UPCA;
                }
                else
                {
                    // this value can't be converted to UPC-A
                    // so, left it unchanged or ignore, if EAN-13 was
                    // not requested

                    if (!_findEan13)
                        return null;
                }
            }

            return result;
        }

        private bool decodeFromStartImpl(XBitArray row, int startSymbolOffset, int firstDigitOffset)
        {
            if (_findEan13 || _findUpca)
            {
                if (decodeEan13(row, firstDigitOffset))
                    return true;
            }

            if (_findEan8)
            {
                if (decodeEan8(row, firstDigitOffset))
                    return true;
            }

            if (_findUpce)
            {
                if (decodeUpce(row, firstDigitOffset))
                    return true;
            }

            return false;
        }

        private FoundBarcode decodeSupplementFromStartSymbol(int rowNumber, XBitArray row)
        {
            int startSymbolOffset = _patternOffset;

            if (_findEan5)
            {
                if (decodeEan5(row, _afterPatternOffset))
                    return packResult(row, rowNumber, startSymbolOffset);
            }

            if (_findEan2)
            {
                if (decodeEan2(row, _afterPatternOffset))
                    return packResult(row, rowNumber, startSymbolOffset);
            }



            return null;
        }

        protected virtual bool verifyCheckSum(int[] data)
        {
            if (_currentSymbology == SymbologyType.EAN5 ||
                _currentSymbology == SymbologyType.EAN2)
            {
                // already checked.
                return true;
            }

            int[] valueToCheck = data;
            if (_currentSymbology == SymbologyType.UPCE)
            {
                // in case of UPCE it should always start with ZERO 
                if (data[0] != 0)
                    return false; // return false if not starting with zero

                // converting value from UPCE to UPCA for the further verification
                valueToCheck = convertUpceToUpca(data);
            }
            
            if (valueToCheck == null || valueToCheck.Length == 0)
                return false;

            int sum = 0;
            for (int i = valueToCheck.Length - 2; i >= 0; i -= 2)
            {
                int digit = valueToCheck[i];
                if (digit < 0 || digit > 9)
                    return false;

                sum += digit;
            }

            sum *= 3;

            for (int i = valueToCheck.Length - 1; i >= 0; i -= 2)
            {
                int digit = valueToCheck[i];
                if (digit < 0 || digit > 9)
                    return false;

                sum += digit;
            }

            return sum % 10 == 0;
        }

        public static int[] convertUpceToUpca(int[] upce)
        {
            int[] upceCore = null;

            if (upce.Length == 6)
            {
                upceCore = upce;
            }
            else if (upce.Length == 7)
            {
                // truncate last digit, assume it is just check digit
                upceCore = new int[6];
                Array.Copy(upce, upceCore, 6);
            }
            else if (upce.Length == 8)
            {
                // truncate first and last digit, 
                // assume first digit is number system digit
                // last digit is check digit
                upceCore = new int[6];
                Array.Copy(upce, 1, upceCore, 0, 6);
            }
            else
            {
                return null;
            }

            int[] upca = new int[12];
            int lastDigit = upceCore[5];
            switch (lastDigit)
            {
                case 0:
                case 1:
                case 2:
                    upca[1] = upceCore[0];
                    upca[2] = upceCore[1];
                    upca[3] = lastDigit;
                    upca[8] = upceCore[2];
                    upca[9] = upceCore[3];
                    upca[10] = upceCore[4];
                    break;
                case 3:
                    upca[1] = upceCore[0];
                    upca[2] = upceCore[1];
                    upca[3] = upceCore[2];
                    upca[9] = upceCore[3];
                    upca[10] = upceCore[4];
                    break;
                case 4:
                    upca[1] = upceCore[0];
                    upca[2] = upceCore[1];
                    upca[3] = upceCore[2];
                    upca[4] = upceCore[3];
                    upca[10] = upceCore[4];
                    break;
                default:
                    upca[1] = upceCore[0];
                    upca[2] = upceCore[1];
                    upca[3] = upceCore[2];
                    upca[4] = upceCore[3];
                    upca[5] = upceCore[4];
                    upca[10] = lastDigit;
                    break;
            }

            int check = calculateUpcaChecksum(upca);
            if (check > -1)
            {
                upca[11] = check;
            }

            return upca;
        }

        protected static int calculateUpcaChecksum(int[] data)
        {

            if (data == null || data.Length == 0)
                return -1;

            int sum = 0;
            for (int i = data.Length - 2; i >= 0; i -= 2)
            {
                int digit = data[i];
                if (digit < 0 || digit > 9)
                    return -1;

                sum += digit;
            }

            sum *= 3;

            for (int i = data.Length - 1; i >= 0; i -= 2)
            {
                int digit = data[i];
                if (digit < 0 || digit > 9)
                    return -1;

                sum += digit;
            }

            return sum % 10;
        }

        protected int decodeSymbol(XBitArray row, int offset, int[][] patterns){
            return decodeSymbol(row, offset, patterns, MaxSymbolDifference, MaxAverageDifference);
        }

        protected int decodeSymbol(XBitArray row, int offset, int[][] patterns,double maxSymbolDifference, double maxAverageDifference)
        {
            bool read = readModules(row, offset, _moduleWidths);
            if (!read)
                return -1;

            double smallestDifference = maxAverageDifference;
            int symbol = -1;

            for (int i = 0; i < patterns.Length; i++)
            {
                double difference = calcDifference(_moduleWidths, patterns[i], maxSymbolDifference);
                if (difference < smallestDifference)
                {
                    symbol = i;
                    smallestDifference = difference;
                }
            }

            if (symbol >= 0)
                return symbol;

            return -1;
        }

        protected bool decodeEan13(XBitArray row, int firstDigitOffset)
        {
            _currentSymbology = SymbologyType.EAN13;

            _digits.Clear();
            Array.Clear(_moduleWidths, 0, 4);

            int pattern = 0;
            int rowOffset = firstDigitOffset;
            for (int x = 0; x < 6 && rowOffset < row.Size; x++)
            {
                int symbol = decodeSymbol(row, rowOffset, _leftPatterns);
                if (symbol < 0 || symbol > 19)
                    return false;

                _digits.Add(symbol % 10);
                for (int i = 0; i < _moduleWidths.Length; i++)
                    rowOffset += _moduleWidths[i];

                if (symbol >= 10)
                    pattern |= 1 << (5 - x);
            }

            int firstDigit = patternToFirstEanDigit(pattern);
            if (firstDigit == -1)
                return false;

            _digits.Insert(0, firstDigit);

            if (!findPattern(row, rowOffset, true, _middlePattern))
                return false;

            rowOffset = _afterPatternOffset;
            for (int x = 0; x < 6 && rowOffset < row.Size; x++)
            {
                int symbol = decodeSymbol(row, rowOffset, _leftOddPatterns);
                if (symbol < 0 || symbol > 9)
                    return false;

                _digits.Add(symbol);
                for (int i = 0; i < _moduleWidths.Length; i++)
                    rowOffset += _moduleWidths[i];
            }

            if (_digits.Count != 12 && _digits.Count != 13)
                return false;

            return findPattern(row, rowOffset, false, _startStopPattern);
        }

        protected bool decodeEan8(XBitArray row, int firstDigitOffset)
        {
            _currentSymbology = SymbologyType.EAN8;

            _digits.Clear();
            Array.Clear(_moduleWidths, 0, 4);

            int rowOffset = firstDigitOffset;
            for (int x = 0; x < 4 && rowOffset < row.Size; x++)
            {
                int symbol = decodeSymbol(row, rowOffset, _leftOddPatterns);
                if (symbol < 0 || symbol > 9)
                    return false;

                _digits.Add(symbol);
                for (int i = 0; i < _moduleWidths.Length; i++)
                    rowOffset += _moduleWidths[i];
            }

            if (!findPattern(row, rowOffset, true, _middlePattern))
                return false;

            rowOffset = _afterPatternOffset;
            for (int x = 0; x < 4 && rowOffset < row.Size; x++)
            {
                int symbol = decodeSymbol(row, rowOffset, _leftOddPatterns);
                if (symbol < 0 || symbol > 9)
                    return false;

                _digits.Add(symbol);
                for (int i = 0; i < _moduleWidths.Length; i++)
                    rowOffset += _moduleWidths[i];
            }

            if (_digits.Count != 8)
                return false;

            return findPattern(row, rowOffset, false, _startStopPattern);
        }

        protected bool decodeUpce(XBitArray row, int firstDigitOffset)
        {
            _currentSymbology = SymbologyType.UPCE;

            _digits.Clear();
            Array.Clear(_moduleWidths, 0, 4);

            int pattern = 0;
            int rowOffset = firstDigitOffset;
            for (int x = 0; x < 6 && rowOffset < row.Size; x++)
            {
                int symbol = decodeSymbol(row, rowOffset, _leftPatterns);
                if (symbol < 0 || symbol > 19)
                    return false;

                _digits.Add(symbol % 10);
                for (int i = 0; i < _moduleWidths.Length; i++)
                    rowOffset += _moduleWidths[i];

                if (symbol >= 10)
                    pattern |= 1 << (5 - x);
            }

            if (_digits.Count != 6)
                return false;

            if (!postProcessUpce(pattern))
                return false;

            return findPattern(row, rowOffset, true, _upceStopPattern);
        }

        protected bool decodeEan2(XBitArray row, int firstDigitOffset)
        {
            _currentSymbology = SymbologyType.EAN2;

            _digits.Clear();
            Array.Clear(_moduleWidths, 0, 4);

            bool firstIsOdd = false;
            bool secondIsOdd = false;

            int rowOffset = firstDigitOffset;
            for (int x = 0; x < 2 && rowOffset < row.Size; x++)
            {
                int symbol = decodeSymbol(row, rowOffset, _leftPatterns, MaxSymbolDifferenceEAN2, MaxAverageDifferenceEAN2);
                if (symbol < 0 || symbol > 19)
                    return false;

                _digits.Add(symbol % 10);
                _patternOffset = rowOffset;

                for (int i = 0; i < _moduleWidths.Length; i++)
                    rowOffset += _moduleWidths[i];

                if (x == 0)
                    firstIsOdd = symbol < 10;
                else
                    secondIsOdd = symbol < 10;

                if (x != 1)
                {
                    int[] separatorModuleWidth = new int[2];
                    bool read = readModules(row, rowOffset, separatorModuleWidth);
                    if (!read)
                        return false;

                    double difference = calcDifference(separatorModuleWidth, _supplementSeparatorPattern, MaxSymbolDifferenceEAN2);
                    if (difference > MaxAverageDifferenceEAN2)
                        return false;

                    for (int i = 0; i < separatorModuleWidth.Length; i++)
                        rowOffset += separatorModuleWidth[i];
                }
            }

            if (_digits.Count != 2)
                return false;

            if (validateEan2(firstIsOdd, secondIsOdd))
            {
                _afterPatternOffset = rowOffset;
                return true;
            }

            return false;
        }

        protected bool decodeEan5(XBitArray row, int firstDigitOffset)
        {
            _currentSymbology = SymbologyType.EAN5;

            _digits.Clear();
            Array.Clear(_moduleWidths, 0, 4);

            int pattern = 0;
            int rowOffset = firstDigitOffset;
            for (int x = 0; x < 5 && rowOffset < row.Size; x++)
            {
                int symbol = decodeSymbol(row, rowOffset, _leftPatterns, MaxSymbolDifferenceEAN5, MaxAverageDifferenceEAN5);
                if (symbol < 0 || symbol > 19)
                    return false;

                _digits.Add(symbol % 10);
                _patternOffset = rowOffset;

                for (int i = 0; i < _moduleWidths.Length; i++)
                    rowOffset += _moduleWidths[i];

                if (symbol >= 10)
                    pattern |= 1 << (4 - x);

                if (x != 4)
                {
                    int[] separatorModuleWidth = new int[2];
                    bool read = readModules(row, rowOffset, separatorModuleWidth);
                    if (!read)
                        return false;

                    double difference = calcDifference(separatorModuleWidth, _supplementSeparatorPattern, MaxSymbolDifferenceEAN5);
                    if (difference > MaxAverageDifferenceEAN5)
                        return false;

                    for (int i = 0; i < separatorModuleWidth.Length; i++)
                        rowOffset += separatorModuleWidth[i];
                }
            }

            if (_digits.Count != 5)
                return false;

            int checkSum = patternToEan5CheckSumDigit(pattern);
            if (checkSum == -1)
                return false;

            if (validateEan5CheckSum(checkSum))
            {
                _afterPatternOffset = rowOffset;
                return true;
            }

            return false;
        }

        private static int patternToFirstEanDigit(int pattern)
        {
            for (int i = 0; i < 10; i++)
            {
                if (pattern == _ean13FirstDigitPatterns[i])
                    return i;
            }

            return -1;
        }

        private bool postProcessUpce(int pattern)
        {
            for (int numberSystem = 0; numberSystem <= 1; numberSystem++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (pattern == _upcePatterns[numberSystem][i])
                    {
                        _digits.Insert(0, numberSystem);
                        _digits.Add(i);
                        return true;
                    }
                }
            }

            return false;
        }

        private string decodeRawData(int[] rawData)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int i in rawData)
                sb.Append((char)('0' + i));

            return sb.ToString();
        }

        private static int patternToEan5CheckSumDigit(int pattern)
        {
            for (int i = 0; i < 10; i++)
            {
                if (pattern == _ean5Structures[i])
                    return i;
            }

            return -1;
        }

        private bool validateEan5CheckSum(int checkSum)
        {
            int total = 0;
            for (int i = 0; i < 5; i++)
            {
                if (i % 2 == 0)
                    total += (int)_digits[i] * 3;
                else
                    total += (int)_digits[i] * 9;
            }

            return (total % 10) == checkSum;
        }

        private bool validateEan2(bool firstIsOdd, bool secondIsOdd)
        {
            int data = (int)_digits[0] * 10 + (int)_digits[1];
            int pattern = data % 4;

            if (pattern == 0)
                return (firstIsOdd && secondIsOdd);

            if (pattern == 1)
                return (firstIsOdd && !secondIsOdd);

            if (pattern == 2)
                return (!firstIsOdd && secondIsOdd);

            if (pattern == 3)
                return (!firstIsOdd && !secondIsOdd);

            // make compiler happy
            return false;
        }
    }
}
