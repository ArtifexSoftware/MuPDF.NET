using System;
using System.Collections;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Datamatrix
{
    // non-ecc200 dataformat, with enum values corresponding to values found in the data itself
	internal enum NonECC200DataFormat
    {
        Base11 = 0, Base27 = 1, Base41 = 2, Base37 = 3, ASCII = 4, Base256 = 5
    }

    // Used to decode compressed data. Supports ECC200 and ECC000-140 formats as well
	internal class SymbolDecoder
    {
        #region Charsets
        public static readonly char[] C40Set = new char[]
                                                   {
                                                       '¦', '¦', '¦', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8',
                                                       '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L',
                                                       'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y',
                                                       'Z', (char) 0, (char) 1, (char) 2, (char) 3, (char) 4, (char) 5,
                                                       (char) 6, (char) 7, (char) 8, (char) 9, (char) 10, (char) 11,
                                                       (char) 12, (char) 13, (char) 14, (char) 15, (char) 16, (char) 17,
                                                       (char) 18, (char) 19, (char) 20, (char) 21, (char) 22, (char) 23,
                                                       (char) 24, (char) 25, (char) 26, (char) 27, (char) 28, (char) 29,
                                                       (char) 30, (char) 31, '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '!'
                                                       , '“', '#', '$', '%', '&', '‘', '(', ')', '*', '+', ',', '-', '.'
                                                       , '/', ':', ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^',
                                                       '_', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦',
                                                       '¦', '‘', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k',
                                                       'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
                                                       'y', 'z', '{', '|', '}', '~', (char) 8, '¦', '¦', '¦', '¦', '¦',
                                                       '¦', '¦', '¦'
                                                   };

        public static readonly char[] TextSet = new char[]
                                                    {
                                                        '¦', '¦', '¦', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8',
                                                        '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l',
                                                        'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y',
                                                        'z', (char) 0, (char) 1, (char) 2, (char) 3, (char) 4, (char) 5,
                                                        (char) 6, (char) 7, (char) 8, (char) 9, (char) 10, (char) 11,
                                                        (char) 12, (char) 13, (char) 14, (char) 15, (char) 16, (char) 17,
                                                        (char) 18, (char) 19, (char) 20, (char) 21, (char) 22, (char) 23,
                                                        (char) 24, (char) 25, (char) 26, (char) 27, (char) 28, (char) 29,
                                                        (char) 30, (char) 31, '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '!',
                                                        '“', '#', '$', '%', '&', '‘', '(', ')', '*', '+', ',', '-', '.',
                                                        '/', ':', ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_',
                                                        '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦', '¦',
                                                        '‘', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L',
                                                        'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y',
                                                        'Z', '{', '|', '}', '~', (char) 8, '¦', '¦', '¦', '¦', '¦', '¦',
                                                        '¦', '¦'
                                                    };

        public static readonly char[] X12Set = new char[]
                                                   {
                                                       (char) 13, (char) 42, (char) 62, ' ', '0', '1', '2', '3', '4',
                                                       '5', '6', '7', '8',
                                                       '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L',
                                                       'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y',
                                                       'Z'
                                                   };

        public static readonly byte[] Base27Set = new byte[]
                                                      {
                                                          32, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
                                                          80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90
                                                      };


        public static readonly byte[] Base37Set = new byte[]
                                                      {
                                                          32, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
                                                          80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 48, 49, 50, 51, 52,
                                                          53, 54, 55, 56, 57
                                                      };

        public static readonly byte[] Base41Set = new byte[]
                                                      {
                                                          32, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
                                                          80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 48, 49, 50, 51, 52,
                                                          53, 54, 55, 56, 57, 46, 44, 45, 47
                                                      };

        public static readonly byte[] Base11Set = new byte[] {32, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57};

        public const int C40TextPageLength = 40;

        public const byte ASCIIUpperShift = 235;

        public const byte EDIFACTUnlatch = 31;
        #endregion

        private static readonly Hashtable SectionLengthBase = new Hashtable();

        private static readonly Hashtable BaseIdToBase = new Hashtable();

        private static readonly Hashtable BaseLookupTables = new Hashtable();

        static SymbolDecoder()
        {
            BaseIdToBase.Add(NonECC200DataFormat.Base11, 11);
            BaseIdToBase.Add(NonECC200DataFormat.Base27, 27);
            BaseIdToBase.Add(NonECC200DataFormat.Base37, 37);
            BaseIdToBase.Add(NonECC200DataFormat.Base41, 41);

            SectionLengthBase.Add(11, new byte[] {4, 7, 11, 14, 18, 21});
            SectionLengthBase.Add(27, new byte[] {5, 10, 15, 20, 24});
            SectionLengthBase.Add(37, new byte[] {6, 11, 16, 21});
            SectionLengthBase.Add(41, new byte[] {6, 11, 17, 22});

            BaseLookupTables.Add(11, Base11Set);
            BaseLookupTables.Add(27, Base27Set);
            BaseLookupTables.Add(37, Base37Set);
            BaseLookupTables.Add(41, Base41Set);
        }

        public static ABarCodeData[] DecodeASCII(int[] symbols, ref int index)
        {
            ArrayList resultList = new ArrayList();
            while (index < symbols.Length && (symbols[index] < 230 && symbols[index] != 129 || symbols[index] == ASCIIUpperShift))
            {
                StringBuilder chars = new StringBuilder();
                while (index < symbols.Length && (symbols[index] < 129 || symbols[index] == ASCIIUpperShift))
                {
                    int symb = symbols[index];
                    if (symb == ASCIIUpperShift)
                    {
                        ++index;
                        if (index >= symbols.Length) break;
                        symb = symbols[index];
                        chars.Append((char)(symb + 127));
                    }
                    else
                    {
                        chars.Append((char)(symb - 1));
                    }
                    ++index;
                }
                if (chars.Length > 0)
                {
                    resultList.Add(new StringBarCodeData(chars.ToString()));
                }

                ArrayList digits = new ArrayList();
                while (index < symbols.Length && symbols[index] > 129 && symbols[index] < 230)
                {
                    digits.Add((byte) ((symbols[index] - 130)/10));
                    digits.Add((byte) ((symbols[index] - 130)%10));
                    ++index;
                }
                if (digits.Count > 0)
                {
                    byte[] value = new byte[digits.Count];
                    digits.CopyTo(value);
                    resultList.Add(new NumericBarCodeData(value));
                }
            }
            if (resultList.Count > 0)
            {
                ABarCodeData[] data = new ABarCodeData[resultList.Count];
                resultList.CopyTo(data);
                return data;
            }

            return null;
        }

        public static ABarCodeData[] DecodeBase256(int[] symbols, ref int index, System.Text.Encoding encoding)
        {
            int sequenceEnd;
            byte d1 = UnRandomise256(symbols[index], index + 1);
            if (d1 == 0)
            {
                sequenceEnd = symbols.Length;
                ++index;
            }
            else if (d1 < 250)
            {
                ++index;
                sequenceEnd = index + d1;
                
            }
            else
            {
                int d2 = UnRandomise256(symbols[index + 1], index + 2);
                index += 2;
                sequenceEnd = index + (d1 - 249)*250 + d2;
            }

            byte[] data = new byte[sequenceEnd - index];
            int offset = index;
            for (; index < sequenceEnd && index<symbols.Length; index++)
            {
                data[index - offset] = UnRandomise256(symbols[index], index + 1);
            }
            return new ABarCodeData[] {new Base256BarCodeData(data, encoding)};
        }

        public static ABarCodeData[] DecodeC40Text(int[] symbols, ref int index, char[] charSet)
        {
            int[] c40Symbols = UnfoldC40TextX12(symbols, ref index);
            StringBuilder resultBuilder = new StringBuilder();
            bool upperShift = false;
            int shift = 0;
            for (int i = 0; i < c40Symbols.Length; ++i)
            {
                int sym = c40Symbols[i];
                if (shift == 0 && sym < 3)
                {
                    upperShift = upperShift && sym < 3;
                    shift = sym + 1;
                } else if (shift == 2 && sym == 30)
                {
                    upperShift = true;
                    shift = 0;
                }
                else
                {
                    char c = charSet[shift * C40TextPageLength + sym];
                    c = upperShift ? (char) (c + 128) : c;
                    resultBuilder.Append(c);
                    upperShift = false;
                    shift = 0;
                }
            }

            return new ABarCodeData[] {new StringBarCodeData(resultBuilder.ToString())};
        }

        public static ABarCodeData[] DecodeX12(int[] symbols, ref int index)
        {
            int[] c40Symbols = UnfoldC40TextX12(symbols, ref index);
            StringBuilder resultBuilder = new StringBuilder();
            for (int i = 0; i < c40Symbols.Length; ++i)
            {
                int sym = c40Symbols[i];
                char c = X12Set[sym];
                resultBuilder.Append(c);
            }

            return new ABarCodeData[] { new StringBarCodeData(resultBuilder.ToString()) };
        }

        public static ABarCodeData[] DecodeEdifact(int[] symbols, ref int index)
        {
            int bitIndex = 0;
            ArrayList edifatData = new ArrayList();
            do
            {
                int ediByte;
                if (bitIndex < 3)
                {
                    ediByte = (symbols[index] >> (2-bitIndex)) & 63 /*binary 111111*/;
                    bitIndex += 6;
                    if (bitIndex > 7)
                    {
                        bitIndex -= 8;
                        ++index;
                    }
                }
                else
                {
                    bitIndex -= 2;
                    ediByte = (symbols[index++] << bitIndex) & 63; //binary 111111
                    if (index >= symbols.Length) break;
                    ediByte |= (symbols[index] >> (8-bitIndex));                    
                }

                if (ediByte == EDIFACTUnlatch)
                {
                    if (bitIndex != 0)
                    {
                        ++index;
                    }
                    break;
                }

                edifatData.Add(ediByte);
            } while (index < symbols.Length);

            StringBuilder resultBuilder = new StringBuilder();
            foreach (int edd in edifatData)
            {
                if (edd < 32)
                {
                    resultBuilder.Append((char) (edd + 64));
                }
                else
                {
                    resultBuilder.Append((char)edd);
                }
            }

            return new ABarCodeData[] { new StringBarCodeData(resultBuilder.ToString())};
        }

        // decodes and ECI switch sequence
        public static ABarCodeData DecodeECI(int[] symbols, ref int index)
        {
            int c1 = symbols[index++];
            if (c1 < 128)
            {
                return new ECISwitchSymbol(c1 - 1);
            }

            int c2 = symbols[index++];
            if (c1 < 192)
            {
                return new ECISwitchSymbol((c1 - 128)*254 + c2 + 126);
            }

            int c3 = symbols[index++];
            return new ECISwitchSymbol((c1 - 192)*64516 + (c2 - 1)*254 + c3 + 16382);
        }

        // decodes a structured append sequence
        public static ABarCodeData DecodeStructuredAppend(int[] symbols, ref int index)
        {
            index += 3;
            return new StructuredAppendSymbol(symbols[index - 3], symbols[index - 2], symbols[index - 1]);
        }

        // decodes Base<X> coded data. Used in ECC000-140
        public static byte[] DecodeBaseX(BitArray bitData, int dataLength, NonECC200DataFormat dataFormat)
        {
            int codeBase = (int) BaseIdToBase[dataFormat];
            byte[] set = (byte[]) BaseLookupTables[codeBase];
            byte[] sectionLengths = (byte[]) SectionLengthBase[codeBase];

            byte[] originalData = new byte[dataLength];
            for (int bitIndex = 0, i = 0; i < dataLength;)
            {
                int chunksize = Math.Min(sectionLengths.Length, dataLength - i);
                int bitCount = sectionLengths[chunksize - 1];
                int value = 0;
                for (int b = 0; b < bitCount; ++b, ++bitIndex)
                {
                    if (bitIndex >= bitData.Length)
                    {
                        return null;
                    }
                    if (bitData[bitIndex])
                    {
                        value |= (1 << b);
                    }
                }

                for (int b = 0; b < chunksize; ++b, ++i)
                {
                    originalData[i] = set[value%codeBase];
                    value /= codeBase;
                }
            }

            return originalData;
        }

        // extracts C40, Text and X12 bytes from a compressed datastream
        private static int[] UnfoldC40TextX12(int[] symbols, ref int index)
        {
            int c40Length = 0;
            while (index + c40Length +1 < symbols.Length && symbols[index + c40Length] != 254)
                c40Length += 2;

            int[] c40Data = new int[c40Length / 2 * 3];
            for (int i = 0,  c = 0; i < c40Length; i+=2, index+=2)
            {
                int word = symbols[index]*256 + symbols[index + 1] - 1;
                byte c1 = (byte) (word/1600);
                word %= 1600;
                byte c2 = (byte) (word/40);
                byte c3 = (byte) (word%40);
                c40Data[c++] = c1;
                c40Data[c++] = c2;
                c40Data[c++] = c3;
            }
            if (index<symbols.Length && symbols[index] == 254) ++index;
            return c40Data;
        }

        // implements the Base256 unrandomising algorithm used in ECC200
        private static byte UnRandomise256(int codeWord, int position)
        {
            int pseudoRandomNumber = ((149*position)%255) + 1;
            int tmp = codeWord - pseudoRandomNumber;
            if (tmp >= 0)
            {
                return (byte)tmp;
            }

            return (byte) (tmp + 256);
        }
    }
}
