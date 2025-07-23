using System;
using System.Collections;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.PDF417
{
    // Handles the decoding of PDF417 data
    class SymbolDecoder
    {
        #region Text Decoder
        // Text charset
        public static readonly int[] TextSet = new int[]
                                                   {
                                                       65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
                                                       81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 32,
                                                       (int) TextShiftLatch.LowerLatch, (int) TextShiftLatch.MixedLatch,
                                                       (int) TextShiftLatch.PunctShift, 97, 98, 99, 100, 101, 102, 103,
                                                       104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116,
                                                       117, 118, 119, 120, 121, 122, 32, (int) TextShiftLatch.AlphaShift
                                                       , (int) TextShiftLatch.MixedLatch,
                                                       (int) TextShiftLatch.PunctShift, 48, 49, 50, 51, 52, 53, 54, 55,
                                                       56, 57, 38, 13, 9, 44, 58, 35, 45, 46, 36, 47, 43, 37, 42, 61, 94
                                                       , (int) TextShiftLatch.PunctLatch, 32, (int) TextShiftLatch.LowerLatch,
                                                       (int) TextShiftLatch.AlphaLatch, (int) TextShiftLatch.PunctShift,
                                                       59, 60, 62, 64, 91, 92, 93, 95, 96, 126, 33, 13, 9, 44, 58, 10,
                                                       45, 46, 36, 47, 34, 124, 42, 40, 41, 63, 123, 125, 39,
                                                       (int) TextShiftLatch.AlphaLatch
                                                   };

        public const int TextPageLength = 30;

        private enum TextShiftLatch
        {
            AlphaLatch = -1, LowerLatch = -2, MixedLatch = -3, PunctLatch = -4, AlphaShift = -5, PunctShift = -8
        }

        // decodes the given symbols using Text encodation
        int rememberLatch=0;
        public ABarCodeData[] DecodeText(int[] symbols, ref int index, bool rememberLastLatch)
        {
            int[] charData = UnfoldText(symbols, ref index);
            StringBuilder chars = new StringBuilder();
            int lastLatch = -1;
            int latch = rememberLastLatch?rememberLatch:0;
            for (int i = 0; i < charData.Length; ++i)
            {
                int value = TextSet[charData[i] + TextPageLength*latch];
                if (value < 0)
                {
                    if (value < -4)
                    {
                        lastLatch = latch;
                        //value /= 2;
                        value += 4;
                    }

                    latch = -1 - value;
                }
                else
                {
                    chars.Append((char) value);
                    if (lastLatch >= 0)
                    {
                        latch = lastLatch;
                        lastLatch = -1;
                    }
                }
            }

            //detect padding with 29
            if (index<symbols.Length && symbols[index] == 913 && charData[charData.Length - 1] == 29)
            {
                if (latch == 0 /*alpha*/) rememberLatch = latch;
                else rememberLatch = lastLatch;
            } 
            else rememberLatch = latch;

            if (chars.Length > 0)
            { 
                StringBarCodeData result = new StringBarCodeData(chars.ToString());
                return new ABarCodeData[] {result};
            }

            return null;
        }

        // extracts the text bytes from symbols
        private int[] UnfoldText(int[] symbols, ref int index)
        {
            ArrayList charData = new ArrayList();
            while (index < symbols.Length && symbols[index] < 900)
            {
                charData.Add(symbols[index]/TextPageLength);
                charData.Add(symbols[index]%TextPageLength);
                ++index;
            }

            int[] result = new int[charData.Count];
            charData.CopyTo(result);

            return result;
        }

        #endregion

        public ABarCodeData[] DecodeBase900(int[] symbols, ref int index, int length)
        {
            int start = index;
            while (index < symbols.Length && symbols[index] < 900 && length != 0)
            {
                index++;
                length--;
            }
            int[] data = new int[index - start];
            Array.Copy(symbols, start, data, 0, (index - start));
            return new ABarCodeData[] { new Base900BarCodeData(data) };
        }

        // Applies Base 900 --> Base 256 conversion to get byte data
        public ABarCodeData[] DecodeBase256(int[] symbols, ref int index, bool isFull, System.Text.Encoding encoding)
        {
            ArrayList base900Data = new ArrayList();
            while (index < symbols.Length && symbols[index] < 900)
            {
                base900Data.Add(symbols[index++]);
            }

            if (base900Data.Count == 0) return null;

            if (isFull && base900Data.Count%5 != 0)
            {
                throw new Exception(
                    "Invalid Byte compacted block found (said to be full, but has a wrong number of codewords)");
            }

            int blocks5 = base900Data.Count/5;
            if (!isFull && base900Data.Count%5 == 0)
            {
                --blocks5;
            }

            int remainder = base900Data.Count - 5*blocks5;
            byte[] base256Data = new byte[6*blocks5 + remainder];
            for (int i = 0; i < blocks5; ++i)
            {
                long b900 = 0;
                for (int b = 0; b < 5; ++b)
                {
                    b900 = b900*900 + (int) base900Data[5*i + b];
                }

                for (int b = 0; b < 6; ++b)
                {
                    base256Data[6*i + 5 - b] = (byte) (b900%256);
                    b900 = b900/256;
                }
            }

            for (int i = 5*blocks5, o = 6*blocks5; i < base900Data.Count; ++i, ++o)
            {
                base256Data[o] = (byte) ((int) base900Data[i]);
            }

            return new ABarCodeData[] {new Base256BarCodeData(base256Data, encoding)};
        }

        #region Numeric Decoder

        public ABarCodeData[] DecodeNumeric(int[] symbols, ref int index, int lenght)
        {
            int[] block15 = new int[15];
            int symbolCount = 0;
            ArrayList fullDigitList = new ArrayList();
            while (index < symbols.Length && symbols[index] < 900 && lenght!=0)
            {
                if (symbolCount < 15)
                {
                    block15[symbolCount++] = symbols[index++];
                    lenght--;
                    continue;
                }

                fullDigitList.AddRange(UnfoldNumeric(block15, symbolCount));
                symbolCount = 0;
            }
            fullDigitList.AddRange(UnfoldNumeric(block15, symbolCount));

            byte[] result = new byte[fullDigitList.Count];
            fullDigitList.CopyTo(result);
            return new ABarCodeData[] { new NumericBarCodeData(result)};
        }

        // extracts numeric bytes from PDF symbols
        private ArrayList UnfoldNumeric(int[] block15, int symbolCount)
        {
            /*long b900 = 0;
            for (int b = 0; b < symbolCount; ++b)
            {
                b900 = b900 * 900 + block15[b];
            }*/

            // cut the array
            int[] block = new int[symbolCount];
            Array.Copy(block15, block, symbolCount);
            Array.Reverse(block);

            /*ArrayList digitList = new ArrayList();
            while (b900 > 0)
            {
                digitList.Add((byte)(b900 % 10));
                b900 /= 10;                
            }*/

            ArrayList digitList = new ArrayList(BaseConverter.Convert(900, 10, block));
            if (digitList.Count > 0)
            {
                digitList.RemoveAt(digitList.Count - 1);
            }
            digitList.Reverse();

            return digitList;
        }

        #endregion
    }

    // used to convert number bases for very, very long numbers
    class BaseConverter
    {
        // inNumber has the least significant digit first, so does the output
        public static byte[] Convert(int fromBase, int toBase, int[] inNumber)
        {
            if (inNumber == null || inNumber.Length == 0)
            {
                throw new Exception("Input is empty");
            }

            // check the input for digits that exceed the allowable range
            foreach (int i in inNumber)
            {
                if (i >= fromBase)
                {
                    throw new Exception("Invalid digit encountered");
                }
            }

            // find how many digits the output needs
            int inLength = inNumber.Length;
            int outLength = (int)(inLength * (Math.Log(fromBase) / Math.Log(toBase))) + 1;
            int[] ts = new int[outLength + 10]; // assign accumulation array
            int[] resultTmp = new int[outLength + 10]; // assign the result array
            ts[0] = 1; // initialize array with number 1 

            for (int i = 0; i < inLength; i++) // for each input digit
            {
                for (int j = 0; j < outLength; j++) // add the input digit times (base:to from^i) to the output cumulator
                {
                    resultTmp[j] += ts[j] * inNumber[i];
                    int temp = resultTmp[j];
                    int ip = j;
                    do // fix up any remainders in base:to
                    {
                        int rem = temp / toBase;
                        resultTmp[ip] = temp - rem * toBase; ip++;
                        resultTmp[ip] += rem;
                        temp = resultTmp[ip];
                    }
                    while (temp >= toBase);
                }

                // calculate the next power from^i in base:to format
                for (int j = 0; j < outLength; j++)
                {
                    ts[j] = ts[j] * fromBase;
                }

                for (int j = 0; j < outLength; j++) // check for any remainders
                {
                    int temp = ts[j];
                    int ip = j;
                    do  // fix up any remainders
                    {
                        int rem = temp / toBase;
                        ts[ip] = temp - rem * toBase; ip++;
                        ts[ip] += rem;
                        temp = ts[ip];
                    }
                    while (temp >= toBase);
                }
            }

            int realLength = outLength - 1;
            for (; realLength >= 0; --realLength)
            {
                if (resultTmp[realLength] != 0)
                {
                    break;
                }
            }
            realLength++;

            byte[] result = new byte[realLength];
            for (int i = 0; i < realLength; ++i )
            {
                result[i] = (byte) resultTmp[i];
            }
            
            return result;
        }
    }
}
