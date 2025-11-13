using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace BarcodeReader.Core.Common
{
	internal class Utils
    {
        // changes the bit order per byte in the given array
        public static byte[] ReverseBitsPerByte(byte[] rawData)
        {
            byte[] data = new byte[rawData.Length];
            for (int i = 0; i < rawData.Length; ++i)
            {
                for (int b = 0; b < 8; ++b)
                {
                    bool isOne = (rawData[i] & (1 << b)) > 0;
                    if (isOne)
                    {
                        data[i] = (byte)(data[i] | (1 << (7 - b)));
                    }
                }
            }

            return data;
        }

        // Copies elements from the source to the destination array at the given indices
        public static void ReverseSelect(int[] sourceArray, int[] destinationArray, int[] indices)
        {
            for (int i = 0; i < indices.Length; ++i)
            {
                destinationArray[indices[i]] = sourceArray[i];
            }
        }

        // implements the missing Copy(from, index, length) for BitArrays
        public static BitArray BitArrayPart(BitArray srcArray, int from, int length)
        {
            BitArray result = new BitArray(length);
            for (int i = 0; i < length; ++i)
            {
                result[i] = srcArray[i + from];
            }

            return result;
        }

        public static bool[][] NewBoolArray(int cols, int rows)
        {
            bool[][] a=new bool[rows][];
            for (int i = 0; i < rows; i++)
            {
                a[i] = new bool[cols];
            }
            return a;
        }

        public static T[][] NewJaggedArray<T>(int cols, int rows)
        {
            var a = new T[rows][];
            for (int i = 0; i < rows; i++)
            {
                a[i] = new T[cols];
            }
            return a;
        }

        public static string BitArrayToString(BitArray arr)
        {
            var sb = new StringBuilder();
            foreach (bool d in arr)
            {
                sb.Append(d ? 1 : 0);
            }
            return sb.ToString();
        }

        public static bool[][] CloneJaggedArray(bool[][] arr)
        {
            var res = new bool[arr.Length][];
            for (int i = 0; i < arr.Length; i++)
            {
                res[i] = (bool[]) arr[i].Clone();
            }

            return res;
        }

        public static ulong GetArrayHash(int[] arr)
        {           
            unchecked
            {
                ulong hash = 17;

                // get hash code for all items in array
                for (int i = 0; i < arr.Length; i++)
                {
                    hash = hash * 23 + (ulong)arr[i];
                }

                return hash;
            }
        }

        public static int Sum(int[] arr)
        {
            var res = 0;
            for (int i = 0; i < arr.Length; i++)
                res += arr[i];

            return res;
        }

        public static ArrayComparer ArrayComparerInt = new ArrayComparer();

        public static bool IsMirrored(int[] arr1, int[] arr2)
        {
            if (arr1.Length != arr2.Length) return false;
            for(int i=0;i<arr1.Length;i++)
                if (arr1[i] != arr2[arr2.Length - 1 - i])
                    return false;

            return true;
        }

        public static int[] Reverse(int[] arr)
        {
            var res = (int[])arr.Clone();
            Array.Reverse(res);

            return res;
        }

        public static byte[] IntToByte(int[] arr)
        {
            var res = new byte[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                res[i] = (byte)arr[i];

            return res;
        }

        public static int[] ByteToInt(byte[] arr)
        {
            var res = new int[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                res[i] = arr[i];

            return res;
        }

        public static void Swap<T>(ref T v1, ref T v2)
        {
            T temp = v1;
            v1 = v2;
            v2 = temp;
        }

        public static SKRect DrawPath(SKPointI[] polygon)
        {
            // Create a path from the points
            using (var path = new SKPath())
            {
                path.MoveTo(polygon[0]);
                for (int i = 1; i < polygon.Length; i++)
                {
                    path.LineTo(polygon[i]);
                }
                path.Close(); // optional: close the shape

                // Get bounds
                SKRect bounds = path.Bounds;

                return bounds;
            }
        }

        public static void SaveSKBitmap(SKBitmap bitmap, string filePath)
        {
            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(filePath))
            {
                data.SaveTo(stream);
            }
        }

        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
    }

    /// <summary>
    /// Compares two arrays to see if the values inside of the array are the same. This is
    /// dependent on the type contained in the array having a valid Equals() override.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the array</typeparam>
    internal class ArrayComparer : IEqualityComparer<int[]>
    {
        /// <summary>
        /// Gets the hash code for the contents of the array since the default hash code
        /// for an array is unique even if the contents are the same.
        /// </summary>
        /// <remarks>
        /// See Jon Skeet (C# MVP) response in the StackOverflow thread 
        /// http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
        /// </remarks>
        /// <param name="array">The array to generate a hash code for.</param>
        /// <returns>The hash code for the values in the array.</returns>
        public int GetHashCode(int[] array)
        {
            // if non-null array then go into unchecked block to avoid overflow
            if (array != null)
            {
                unchecked
                {
                    int hash = 17;

                    // get hash code for all items in array
                    for (int i = 0; i < array.Length; i++)
                    {
                        hash = hash * 23 + array[i];
                    }

                    return hash;
                }
            }

            // if null, hash code is zero
            return 0;
        }

        /// <summary>
        /// Compares the contents of both arrays to see if they are equal. This depends on 
        /// typeparameter T having a valid override for Equals().
        /// </summary>
        /// <param name="firstArray">The first array to compare.</param>
        /// <param name="secondArray">The second array to compare.</param>
        /// <returns>True if firstArray and secondArray have equal contents.</returns>
        public bool Equals(int[] firstArray, int[] secondArray)
        {
            // if same reference or both null, then equality is true
            if (object.ReferenceEquals(firstArray, secondArray))
            {
                return true;
            }

            // otherwise, if both arrays have same length, compare all elements
            if (firstArray != null && secondArray != null &&
                (firstArray.Length == secondArray.Length))
            {
                for (int i = 0; i < firstArray.Length; i++)
                {
                    // if any mismatch, not equal
                    if (!object.Equals(firstArray[i], secondArray[i]))
                    {
                        return false;
                    }
                }

                // if no mismatches, equal
                return true;
            }

            // if we get here, they are not equal
            return false;
        }
    }
}
