using System;
using System.Collections;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Aztec
{
    class AztecSymbolDecoder
    {
        // Text charset
        public static readonly int[] TextSet = new int[]
                                                   {
                                                       (int) TextShiftLatch.PunctShift, 32, 65, 66, 67, 68, 69, 70, 71,
                                                       72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87,
                                                       88, 89, 90, (int) TextShiftLatch.LowerLatch,
                                                       (int) TextShiftLatch.MixedLatch, (int) TextShiftLatch.DigitLatch,
                                                       (int) TextShiftLatch.Special,
                                                       (int) TextShiftLatch.PunctShift, 32, 97, 98, 99, 100, 101, 102,
                                                       103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115,
                                                       116, 117, 118, 119, 120, 121, 122,
                                                       (int) TextShiftLatch.UpperShift, (int) TextShiftLatch.MixedLatch,
                                                       (int) TextShiftLatch.DigitLatch, (int) TextShiftLatch.Special,
                                                       (int) TextShiftLatch.PunctShift, 32, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                                                       10, 11, 12, 13, 27, 28, 29, 30, 31, 64, 92, 94, 95, 96, 124, 126,
                                                       127, (int) TextShiftLatch.LowerLatch,
                                                       (int) TextShiftLatch.UpperLatch, (int) TextShiftLatch.PunctLatch,
                                                       (int) TextShiftLatch.Special,
                                                       (int) TextShiftLatch.Special, 13, (int) TextShiftLatch.Special,
                                                       (int) TextShiftLatch.Special, (int) TextShiftLatch.Special,
                                                       (int) TextShiftLatch.Special, 33, 34, 35, 36, 37, 38, 39, 40, 41,
                                                       42, 43, 44, 45, 46, 47, 58, 59, 60, 61, 62, 63, 91, 93, 123, 125,
                                                       (int) TextShiftLatch.UpperLatch,
                                                       (int) TextShiftLatch.PunctShift, 32, 48, 49, 50, 51, 52, 53, 54,
                                                       55, 56, 57, 44, 46, (int) TextShiftLatch.UpperLatch,
                                                       (int) TextShiftLatch.UpperShift
                                                   };

        public const int TextPageLength = 32;

        private enum TextShiftLatch
        {
            UpperLatch = -1, LowerLatch = -2, MixedLatch = -3, PunctLatch = -4, DigitLatch = -5, UpperShift = -6, PunctShift = -9, DigitShift = -10, Special = 0
        }

        // decodes the given symbols using Text encodation
        public static ABarCodeData[] DecodeText(BitArray bitStream, System.Text.Encoding encoding)
        {
            ArrayList resultList = new ArrayList();
            StringBuilder chars = new StringBuilder();
            int lastLatch = -1;
            int latch = 0;
            int sliceSize = 5;
            for (int i = 0; i <= bitStream.Length - sliceSize; )
            {
                int code = AztecUtils.BitSliceValue(bitStream, ref i, sliceSize);
                int value = TextSet[code + TextPageLength * latch];
                if (value == 0)
                {
                    if (chars.Length > 0)
                    {
                        resultList.Add(new StringBarCodeData(chars.ToString()));
                        chars = new StringBuilder();
                    }

                    ABarCodeData[] special = HandleSpecial(code, latch, bitStream, ref i, encoding);
                    if (special.Length > 0)
                    {
                        resultList.AddRange(special);
                    }
                    if (lastLatch >= 0)
                    {
                        latch = lastLatch;
                        lastLatch = -1;
                    }
                }
                else if (value < 0)
                {
                    if (value < -5)
                    {
                        lastLatch = latch;
                        value += 5;
                    }

                    latch = -1 - value;
                }
                else
                {
                    chars.Append((char)value);
                    if (lastLatch >= 0)
                    {
                        latch = lastLatch;
                        lastLatch = -1;
                    }
                }

                sliceSize = (-1 - latch) == (int)TextShiftLatch.DigitShift ||
                            (-1 - latch) == (int)TextShiftLatch.DigitLatch
                                ? 4
                                : 5;
            }
            if (chars.Length > 0)
            {
                resultList.Add(new StringBarCodeData(chars.ToString()));
            }

            ABarCodeData[] result = new ABarCodeData[resultList.Count];
            resultList.CopyTo(result);

            return result;
        }

        private static ABarCodeData[] HandleSpecial(int value, int latch, BitArray bitStream, ref int index, System.Text.Encoding encoding)
        {
            // Handle byte encoding
            if (latch < 3 && value == 31)
            {
                int length = AztecUtils.BitSliceValue(bitStream, ref index, 5);
                if (length == 0)
                {
                    length = AztecUtils.BitSliceValue(bitStream, ref index, 11) + 31;
                }

                // if exceed stream length, return empty (issue #1242)
                if (bitStream.Count <= index)
                {
                    return new ABarCodeData[0];
                }

                byte[] chars = new byte[length];
                for (int i = 0; i < length; ++i)
                {
                    chars[i] = (byte)AztecUtils.BitSliceValue(bitStream, ref index, 8);
                }

                return new ABarCodeData[] { new Base256BarCodeData(chars, encoding) };
            }
            else if (latch == 3)
            {
                switch (value)
                {
                    case 0: // FLG(n)
                        int flag = AztecUtils.BitSliceValue(bitStream, ref index, 3);
                        switch (flag)
                        {
                            case 0: // ASCII 29
                                return new ABarCodeData[] { new StringBarCodeData(new byte[] { 29 }) };
                            default: // ECI switch
                                int eci = 0;
                                for (int i = 0; i < flag; ++i)
                                {
                                    int code = AztecUtils.BitSliceValue(bitStream, ref index, 4);
                                    int digit = TextSet[4 * TextPageLength + code] - 48;
                                    eci = eci * 10 + digit;
                                }
                                return new ABarCodeData[] { new ECISwitchSymbol(eci) };
                        }
                    case 2:
                        return new ABarCodeData[] { new StringBarCodeData("\r\n") };
                    case 3:
                        return new ABarCodeData[] { new StringBarCodeData(". ") };
                    case 4:
                        return new ABarCodeData[] { new StringBarCodeData(", ") };
                    case 5:
                        return new ABarCodeData[] { new StringBarCodeData(": ") };
                    default:
                        throw new Exception("Invalid special value");
                }
            }

            throw new Exception("Invalid special value");
        }

    }
}
