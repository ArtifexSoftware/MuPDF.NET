using System;
using System.Text;
using System.Collections;
using SkiaSharp;

namespace BarcodeReader.Core.LegacyDecoders
{
#if CORE_DEV
    public
#else
    internal
#endif
    class CodabarReader : SymbologyReader
    {
        private static string _alphabet = "0123456789-$:/.+ABCD";
        private static int _startA = _alphabet.IndexOf('A');

        // Codabar symbology uses wide-narrow patterns for it's symbols
        // In order to simplify and speed up comparison, we pack individual
        // patterns into ints using following rule:
        // wnwnnnw -> 1010001 -> 0x51

        private static int[] _patterns =
        {
            0x03, 0x06, 0x09, 0x60, 0x12, 0x42,
            0x21, 0x24, 0x30, 0x48, 0x0c, 0x18,
            0x45, 0x51, 0x54, 0x15, 0x1a, 0x29,
            0x0b, 0x0e,
        };

        // temporary array used by buildWideNarrowPattern
        private float[] _wideModulesWidths = new float[7];

        public CodabarReader() : base(null)
        {
        }
		
		public CodabarReader(IBarcodeConsumer consumer) : base(consumer)
        {
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Codabar;
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

            // each Codabar symbol consists of 7 modules
            int currentModule = 0;
            int[] moduleWidths = new int[7];

            for (int x = offset; x < row.Size; x++)
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
                    if (currentModule == moduleWidths.Length - 1)
                    {
                        // we've completed symbol
                        int pattern = buildWideNarrowPattern(moduleWidths);
                        int symbol = -1;
                        for (int i = _startA; i < _startA + 4; i++)
                        {
                            if (pattern == _patterns[i])
                            {
                                symbol = i;
                                break;
                            }
                        }

                        if (symbol >= 0)
                        {
                            // found start symbol
                            if (!RequireQuietZones || HaveWhiteSpaceBefore(row, x, symbolStart))
                            {
                                // we have white space that is half of symbol long
                                // try to decode consecutive symbols
                                FoundBarcode found = decodeFromStartSymbol(rowNumber, row, x, symbolStart, symbol);
                                
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

        private int buildWideNarrowPattern(int[] moduleWidths)
        {
            int currentNarrowModuleWidth = 0;
            int wideModuleCount;
            float mostWide;

            for (; ; )
            {
                Array.Clear(_wideModulesWidths, 0, 7);
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
                int pattern = 0;
                for (int i = 0; i < moduleWidths.Length; i++)
                {
                    if (moduleWidths[i] > currentNarrowModuleWidth)
                    {
                        pattern |= 1 << (moduleWidths.Length - 1 - i);

                        _wideModulesWidths[wideModuleCount] = moduleWidths[i];
                        if (mostWide < moduleWidths[i])
                            mostWide = moduleWidths[i];

                        wideModuleCount++;
                    }
                }

                if (wideModuleCount < 2)
                {
                    // each Codabar symbol should contain at least 2 wide modules
                    // some special and start/stop characters contain 3 wide modules
                    break;
                }

                if (wideModuleCount == 2 || wideModuleCount == 3)
                {
                    bool shouldContinue = false;
                    for (int i = 0; i < wideModuleCount; i++)
                    {
                        if ((mostWide / _wideModulesWidths[i]) > 1.6f)
                        {
                            if (wideModuleCount == 2)
                            {
                                // we have only two wide modules and they are
                                // to distant from each other
                                return -1;
                            }

                            // we have three wide modules that are too distant
                            // from each other. try to continue, maybe we'll 
                            // find two good wide modules.
                            shouldContinue = true;
                        }
                    }

                    if (shouldContinue)
                        continue;

                    return pattern;
                }
            }

            return -1;
        }

        private FoundBarcode decodeFromStartSymbol(int rowNumber, XBitArray row, int offset, int startOffset, int startSymbol)
        {
            int[] moduleWidths = new int[7];
            
            ArrayList symbols = new ArrayList();
            symbols.Add(startSymbol);

            bool done = false;
            int offsetBeforeSymbol = offset;
            while (!done)
            {
                // skip inter-character gap (white space between symbols)
                int gap = 0;
                while (offset < row.Size)
                {
                    if (!row[offset])
                    {
                        offset++;
                        gap++;
                    }
                    else
                        break;
                }

                if (offset == row.Size)
                {
                    // while skipping gap we've ran off image boundary
                    // revert offset changes and break
                    offset -= gap;
                    break;
                }

                offsetBeforeSymbol = offset;
                int symbolIndex = decodeSymbol(row, offset, moduleWidths);
                if (symbolIndex != -1)
                {
                    symbols.Add(symbolIndex);

                    for (int i = 0; i < moduleWidths.Length; i++)
                        offset += moduleWidths[i];

                    if (symbolIndex >= _startA)
                        done = true;
                }
                else
                    done = true;
            }

            if (symbols.Count < 2)
            {
                // should contain at least 2 symbols
                return null;
            }

            if (RequireQuietZones && !HaveWhiteSpaceAfter(row, offsetBeforeSymbol, offset))
                return null;

            FoundBarcode result = new FoundBarcode();
			result.BarcodeFormat = SymbologyType.Codabar;
            result.Rect = new SKRect(startOffset, rowNumber, offset, rowNumber+1);
            result.RawData = AsIntArray(symbols);
            
            StringBuilder sb = new StringBuilder();
            bool symbolComplete = false;
            for (int i = 1; i < result.RawData.Length && !symbolComplete; i++)
            {
                int symbol = result.RawData[i];
                if (symbol == _startA || symbol == (_startA + 1) ||
                    symbol == (_startA + 2) || symbol == (_startA + 3))
                {
                    // found stop symbol
                    symbolComplete = true;
                    break;
                }

                sb.Append(_alphabet[symbol]);
            }

            if (!symbolComplete)
                return null;

			result.Value = sb.ToString();
            if (result.Value.Length == 0)
                return null;

            return result;
        }

        private int decodeSymbol(XBitArray row, int offset, int[] moduleWidths)
        {
            bool read = readModules(row, offset, moduleWidths);
            if (!read)
                return -1;

            int pattern = buildWideNarrowPattern(moduleWidths);
            if (pattern < 0)
                return -1;

            char decodedChar = patternToChar(pattern);
            if (decodedChar == (char)0)
                return -1;

            return _alphabet.IndexOf(decodedChar);
        }

        private static char patternToChar(int pattern)
        {
            for (int i = 0; i < _patterns.Length; i++)
            {
                if (_patterns[i] == pattern)
                    return _alphabet[i];
            }

            return (char)0;
        }
    }
}
