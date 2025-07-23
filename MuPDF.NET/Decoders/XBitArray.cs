using System;
using System.Text;

namespace BarcodeReader.Core
{
    /// <summary>
    /// Array of bits.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class XBitArray
    {
        public int[] _bits;
        public int _size;

        public XBitArray(int size)
        {
            if (size < 1)
                throw new ArgumentException("Size must be greater then 0");

            _size = size;

            //to avoid out of range
            size = (int)(size * 2f);

            //
            var arraySize = size >> 5;
            if ((size & 0x1F) != 0)
                arraySize++;

            _bits = new int[arraySize];
        }

        private XBitArray()
        {
        }

        public XBitArray Clone()
        {
            XBitArray copy = new XBitArray();

            int[] newBits = new int[_bits.Length];
            Array.Copy(_bits, newBits, _bits.Length);
            copy._size = _size;
            copy._bits = newBits;

            return copy;
        }

        public int Size
        {
            get
            {
                return _size;
            }
        }

        /// <summary>
        /// Gets or sets the value of the bit at a specific position.
        /// </summary>
        /// <value>The zero-based index of the value to get or set</value>
        public bool this[int index]
        {
            get
            {
                //var i = index >> 5;
                //if (i >= arraySize)
                //    return false;
                //return (_bits[i] & (1 << (index & 0x1F))) != 0;

                try
                {
                    return (_bits[index >> 5] & (1 << (index & 0x1F))) != 0;
                }
                catch (IndexOutOfRangeException)
                {
                    return false;
                }
            }
            set
            {
                if (value)
                    _bits[index >> 5] |= 1 << (index & 0x1F);
                else
                    _bits[index >> 5] &= ~(1 << (index & 0x1F));
            }
        }

        /// <summary>
        /// Sets a block of 32 bits, starting at bit i.
        /// </summary>
        /// <param name="i">The index of a first bit to set.</param>
        /// <param name="newBits">The new value for the next 32 bits.</param>
        public void Set32(int i, int newBits)
        {
            _bits[i >> 5] = newBits;
        }

        /// <summary>
        /// Clears all bits (sets to false).
        /// </summary>
        public void Clear()
        {
            Array.Clear(_bits, 0, _bits.Length);
        }

        /// <summary>
        /// Checks all bits are 0.
        /// </summary>
        public bool IsNil()
        {
            for (int i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] != 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a range of bits [start, end) is set or not set.
        /// </summary>
        /// <param name="start">The start index.</param>
        /// <param name="end">The end index.</param>
        /// <param name="value">if set to <c>true</c> then checks that bits
        /// are set; otherwise checks that bits are not set.</param>
        /// <returns>
        ///     <c>true</c> if all bits in range are set or not set (according
        /// to value argument); otherwise, <c>false</c>.
        /// </returns>
        public bool IsRange(int start, int end, bool value)
        {
            if (end < start)
                throw new ArgumentException("end index should be greater or equal to start index", "end");

            if (end == start)
                return true;

            end--;
            int firstInt = start >> 5;
            int lastInt = end >> 5;
            for (int i = firstInt; i <= lastInt; i++)
            {
                int firstBit = i > firstInt ? 0 : start & 0x1F;
                int lastBit = i < lastInt ? 31 : end & 0x1F;
                int mask;
                if (firstBit == 0 && lastBit == 31)
                {
                    mask = -1;
                }
                else
                {
                    mask = 0;
                    for (int j = firstBit; j <= lastBit; j++)
                        mask |= 1 << j;
                }

                if ((_bits[i] & mask) != (value ? mask : 0))
                {
                    // Return false if we're looking for 1s and the masked
                    // bits[i] isn't all 1s (that is, equals the mask, or we're
                    // looking for 0s and the masked portion is not all 0s
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reverses all bits in the array.
        /// </summary>
        public XBitArray Reverse()
        {
            int[] newBits = new int[_bits.Length];
            int size = _size;
            for (int i = 0; i < size; i++)
            {
                if (this[size - i - 1])
                    newBits[i >> 5] |= 1 << (i & 0x1F);
            }

            XBitArray array = new XBitArray();
            array._size = _size;
            array._bits = newBits;
            return array;
        }

        /// <summary>
        /// Reverses all bits in the array.
        /// </summary>
        public void ReverseMe()
        {
            int[] newBits = new int[_bits.Length];
            int size = _size;
            for (int i = 0; i < size; i++)
            {
                if (this[size - i - 1])
                    newBits[i >> 5] |= 1 << (i & 0x1F);
            }

            _bits = newBits;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < _size; i++)
            {
                if ((i & 0x07) == 0)
                    result.Append(' ');

                result.Append(this[i] ? '|' : '.');
            }

            return result.ToString();
        }
    }
}