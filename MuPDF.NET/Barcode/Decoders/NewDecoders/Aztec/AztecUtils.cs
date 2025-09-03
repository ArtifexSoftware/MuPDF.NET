using System;
using System.Collections;

namespace BarcodeReader.Core.Aztec
{
    class AztecUtils
    {
        #region Reference data

        public static readonly int[] Polynoms = new int[] { 0, 0, 0, 0, 19, 0, 67, 0, 301, 0, 1033, 0, 4201 };

        public static readonly MyVector[] RotationMap = new MyVector[]
                                             {
                                                 new MyVector(1, 0), new MyVector(0, -1), new MyVector(-1, 0),
                                                 new MyVector(0, 1)
                                             };

        public static readonly int[] FullRangeSizes = new int[]
                                                          {
                                                              0, 19, 23, 27, 31, 37, 41, 45, 49, 53, 57, 61, 67, 71, 75, 79,
                                                              83, 87, 91, 95, 101, 105, 109, 113, 117, 121, 125, 131, 135,
                                                              139, 143, 147, 151
                                                          };

        public static readonly int[] FullRangeCapacities = new int[]
                                                               {
                                                                  0, 128, 288, 480, 704, 960, 1248, 1568, 1920, 2304, 2720, 
                                                                   3168, 3648, 4160, 4704, 5280, 5888, 6528, 7200, 7904,
                                                                   8640, 9408, 10208, 11040, 11904, 12800, 13728, 14688, 15680,
                                                                   16704, 17760, 18848, 19968
/* Old table : wrong because of offset missing bits                0, 126, 288, 480, 704, 960, 1248, 1568, 1920, 2300, 2720,
                                                                   3160, 3640, 4160, 4700, 5280, 5880, 6520, 7200, 7900,
                                                                   8640, 9400, 10200, 11040, 11904, 12792, 13728, 14688, 15672, 
                                                                   16704, 17760, 18840, 19968
*/
                                                               };

        public static readonly int[] FullRangeBitCount = new int[]
                                                             {
                                                                 0, 6, 6, 8, 8, 8, 8, 8, 8, 10, 10, 10, 10, 10, 10, 10, 10, 10
                                                                 , 10, 10, 10, 10, 10, 12, 12, 12, 12, 12, 12, 12, 12, 12,
                                                                 12
                                                             };

        public static readonly int[] FullRangeGridCount = new int[]
                                                              {
                                                                  0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 4, 4, 4, 4, 4, 4, 4, 4,
                                                                  6, 6, 6, 6, 6, 6, 6, 8, 8, 8, 8, 8, 8
                                                              };

        public static readonly int[] CompactSizes = new int[] { 0, 15, 19, 23, 27 };

        public static readonly int[] CompactCapacities = new int[] { 0, 104, 240, 408, 608 };

        public static readonly int[] CompactBitCount = new int[] { 0, 6, 6, 8, 8 };
        #endregion       


        // rotates an axis vector clockwise
        public static MyVector RotateClockwise(MyVector vector)
        {
            for (int i = 0; i < 4; ++i)
            {
                if (RotationMap[i] == vector)
                {
                    int n = (i + 1) % 4;
                    return RotationMap[n];
                }
            }

            throw new Exception("Invalid direction vector");
        }

        // rotates an axis vector counter-clockwise
        public static MyVector RotateCounterClockwise(MyVector vector)
        {
            for (int i = 0; i < 4; ++i)
            {
                if (RotationMap[i] == vector)
                {
                    int n = (i + 3) % 4;
                    return RotationMap[n];
                }
            }

            throw new Exception("Invalid direction vector");
        }

        // cuts up a bitstream into fixed size chunks
   	public static int[] SliceBitStream(BitArray bitStream, int sliceSize, int offset)
        {
            int[] result = new int[(bitStream.Length-offset) / sliceSize];
            for (int i = result.Length - 1, index = bitStream.Length - 1; i >= 0; --i)
            {
                for (int b = 0; b < sliceSize; ++b)
                {
                    if (bitStream[index--])
                    {
                        result[i] |= (1 << b);
                    }
                }
            }

            return result;
        }

        // put together a bitstream from slices, and take care of removing the padding 0's and 1's
        public static BitArray AssembleBitStream(int[] codeWords, int sliceSize)
        {
            int streamLength = codeWords.Length * sliceSize;
            BitArray result = new BitArray(streamLength);
            int magicWord = (1 << sliceSize) - 2;

            int index = 0;
            for (int i = 0; i < codeWords.Length; ++i)
            {
                for (int b = sliceSize - 1; b >= 0; --b)
                {
                    result[index++] = (codeWords[i] & (1 << b)) != 0;
                }

                if (codeWords[i] == 1 || codeWords[i] == magicWord)
                {
                    index--;
                }
            }
            result.Length = index;

            return result;
        }

        // take one piece of the given size from the bitstream, and report its value
        public static int BitSliceValue(BitArray dataStream, ref int index, int length)
        {
            int value = 0;
            for (int i = length - 1; i >= 0; --i, ++index)
            {
                if (index < dataStream.Count)
                {

                    if (dataStream[index])
                    {
                        value |= (1 << i);
                    }
                }

            }

            return value;
        }

    }
}
