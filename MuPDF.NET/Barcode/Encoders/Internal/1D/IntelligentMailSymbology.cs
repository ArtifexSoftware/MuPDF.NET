using System;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using Intelligent Mail Barcode symbology. 
    /// This symbology is used in the USPS mailstream. It is also known as
    /// the USPS OneCode Solution or USPS 4-State Customer Barcode (abbreviated
    /// 4CB, 4-CB, or USPS4CB)
    /// </summary>
    class IntelligentMailSymbology : SymbologyDrawing
    {
        private static string m_alphabet = "0123456789";
        private static int[] m_Table1 = createCodewordToCharTable(true);
        private static int[] m_Table2 = createCodewordToCharTable(false);

        // bar position -> (descender char, bit), (ascender char, bit)
        int[] m_barPositions = new int[]
        {
            7, 2, 4, 3, 1, 10, 0, 0, 9, 12, 2, 8, 5, 5, 6, 11, 8, 9, 3, 1, // 5
            0, 1, 5, 12, 2, 5, 1, 8, 4, 4, 9, 11, 6, 3, 8, 10, 3, 9, 7, 6, // 10
            5, 11, 1, 4, 8, 5, 2, 12, 9, 10, 0, 2, 7, 1, 6, 7, 3, 6, 4, 9, // 15
            0, 3, 8, 6, 6, 4, 2, 7, 1, 1, 9, 9, 7, 10, 5, 2, 4, 0, 3, 8,   // 20
            6, 2, 0, 4, 8, 11, 1, 0, 9, 8, 3, 12, 2, 6, 7, 7, 5, 1, 4, 10, // 25
            1, 12, 6, 9, 7, 3, 8, 0, 5, 8, 9, 7, 4, 6, 2, 10, 3, 4, 0, 5,  // 30
            8, 4, 5, 7, 7, 11, 1, 9, 6, 0, 9, 6, 0, 6, 4, 8, 2, 1, 3, 2,   // 35
            5, 9, 8, 12, 4, 11, 6, 1, 9, 5, 7, 4, 3, 3, 1, 2, 0, 7, 2, 0,  // 40
            1, 3, 4, 1, 6, 10, 3, 5, 8, 7, 9, 4, 2, 11, 5, 6, 0, 8, 7, 12, // 45
            4, 2, 8, 1, 5, 10, 3, 0, 9, 3, 0, 9, 6, 5, 2, 4, 7, 8, 1, 7,   // 50
            5, 0, 4, 5, 2, 3, 0, 10, 6, 12, 9, 2, 3, 11, 1, 6, 8, 8, 7, 9, // 55
            5, 4, 0, 11, 1, 5, 2, 2, 9, 1, 4, 12, 8, 3, 6, 6, 7, 0, 3, 7,  // 60
            4, 7, 7, 5, 0, 12, 1, 11, 2, 9, 9, 0, 6, 8, 5, 3, 3, 10, 8, 2  // 65
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="IntelligentMailSymbology"/> class.
        /// </summary>
        public IntelligentMailSymbology()
            : base(TrueSymbologyType.IntelligentMail)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntelligentMailSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public IntelligentMailSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.IntelligentMail)
        {
        }

        /// <summary>
        /// Validates the value using Intelligent Mail symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            string clean = getCleanValue(value);
            if (clean.Length != 20 && clean.Length != 25 && clean.Length != 29 && clean.Length != 31)
                return false;

            foreach (char c in clean)
            {
                if (!char.IsDigit(c))
                    return false;
            }

            char secondDigit = clean[1];
            if ("01234".IndexOf(secondDigit) == -1)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Intelligent Mail symbology allows encoding of up to 31 digits of data. " +
                "Tracking code should be exactly 20 digits long and Routing code may be 0, 5, 9 or 11 digits long. " +
                "Spaces and dots are allowed as separators.";
        }

        /// <summary>
        /// Gets the barcode value encoded using Intelligent Mail symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            if (forCaption)
                return Value;

            return encodeValue(getCleanValue(Value));
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>
        /// The encoding pattern for given character.
        /// </returns>
        protected override string getCharPattern(char c)
        {
            throw new NotImplementedException();
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            SKSize drawingSize = new SKSize();
            int x = 0;
            int y = 0;

            string value = GetEncodedValue(false);
            int fullBarHeight = Math.Max((BarHeight / 3) * 3, 3);
            int ascenderHeight = (fullBarHeight / 3) * 2;
            int descenderHeight = ascenderHeight;
            int trackerHeight = fullBarHeight / 3;

            int gapWidth = Math.Max(NarrowBarWidth / 2, 1);
            int width = NarrowBarWidth;

            foreach (char c in value)
            {
                if (c == 'A')
                {
                    // ascender
                    m_rects.Add(new SKRect(x, y, width+x, ascenderHeight+y));
                }
                else if (c == 'D')
                {
                    // descender
                    m_rects.Add(new SKRect(x, y + trackerHeight, width+x, descenderHeight+ y + trackerHeight));
                }
                else if (c == 'T')
                {
                    // tracker
                    m_rects.Add(new SKRect(x, y + trackerHeight, width+x, trackerHeight+ y + trackerHeight));
                }
                else if (c == 'F')
                {
                    // full
                    m_rects.Add(new SKRect(x, y, width+x, fullBarHeight+y));
                }


                x += width + NarrowBarWidth;
            }

            drawingSize.Width = x;
            drawingSize.Height = fullBarHeight;
            return drawingSize;
        }

        private string getCleanValue(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c != ' ' && c != '.')
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private string encodeValue(string value)
        {
            //      Tracking Code        |                     Routing Code
            // [ 01234567890123456789 ]  |  [ ] or [ 012345 ] or [ 0123456789 ] or [ 012345678901 ]

            // Tracking Code consist of:
            //      Barcode Identifier
            //      Service Type Identifier
            //      Mailer Identifier
            //      Serial Number

            long routing = getRoutingCode(value);

            BigInteger binary = processTrackingCode(routing, value);
            byte[] binaryData = binary.getBytes();
            if (binaryData.Length < 13)
            {
                byte[] temp = new byte[13];
                Buffer.BlockCopy(binaryData, 0, temp, 13 - binaryData.Length, binaryData.Length);
                binaryData = temp;
            }

            ushort crc = calculateCrc(binaryData);

            BigInteger[] codewords = getCodewords(binary);
            postprocessCodewords(codewords, crc);

            int[] characters = getCharacters(codewords);
            postprocessCharacters(characters, crc);

            return charactersToBars(characters);
        }

        private long getRoutingCode(string value)
        {
            string routingStr = value.Substring(20);
            long routing = 0;
            switch (routingStr.Length)
            {
                case 0:
                    break;

                case 5:
                    routing = long.Parse(routingStr) + 1;
                    break;

                case 9:
                    routing = long.Parse(routingStr) + 100000 + 1;
                    break;

                case 11:
                    routing = long.Parse(routingStr) + 1000000000 + 100000 + 1;
                    break;
            }

            return routing;
        }

        private BigInteger processTrackingCode(long routingCode, string value)
        {
            BigInteger binary = new BigInteger(routingCode);

            binary *= 10;
            binary += m_alphabet.IndexOf(value[0]);

            binary *= 5;
            binary += m_alphabet.IndexOf(value[1]);

            for (int i = 0; i < 18; i++)
            {
                binary *= 10;
                binary += m_alphabet.IndexOf(value[2 + i]);
            }

            return binary;
        }

        /// <summary>
        /// Calculates the CRC.
        /// </summary>
        /// <param name="buffer">The byte array is a 13 byte array
        /// holding 102 bits which are right justified - ie: the leftmost 2 bits
        /// of the first byte do not hold data and must be set to zero.</param>
        /// <returns>11 bit Frame Check Sequence (right justified)</returns>
        private ushort calculateCrc(byte[] buffer)
        {
            ushort GeneratorPolynomial = 0x0F35;
            ushort FrameCheckSequence = 0x07FF;
            int bufIndex = 0;

            // Do most significant byte skipping the 2 most significant bits
            ushort Data = (ushort)(buffer[bufIndex++] << 5);

            for (int Bit = 2; Bit < 8; Bit++)
            {
                if (((FrameCheckSequence ^ Data) & 0x400) != 0)
                    FrameCheckSequence = (ushort)((FrameCheckSequence << 1) ^ GeneratorPolynomial);
                else
                    FrameCheckSequence = (ushort)(FrameCheckSequence << 1);

                FrameCheckSequence &= 0x7FF;
                Data <<= 1;
            }

            // Do rest of the bytes
            for (int ByteIndex = 1; ByteIndex < 13; ByteIndex++)
            {
                Data = (ushort)(buffer[bufIndex++] << 3);
                for (int Bit = 0; Bit < 8; Bit++)
                {
                    if (((FrameCheckSequence ^ Data) & 0x0400) != 0)
                        FrameCheckSequence = (ushort)((FrameCheckSequence << 1) ^ GeneratorPolynomial);
                    else
                        FrameCheckSequence = (ushort)(FrameCheckSequence << 1);

                    FrameCheckSequence &= 0x7FF;
                    Data <<= 1;
                }
            }

            return FrameCheckSequence;
        }

        private BigInteger[] getCodewords(BigInteger binary)
        {
            BigInteger[] codewords = new BigInteger[10];

            codewords[9] = binary % 636; // J
            binary /= 636;

            // I - B
            for (int i = 8; i >= 1; i--)
            {
                codewords[i] = binary % 1365;
                binary /= 1365;
            }

            Debug.Assert(binary >= 0 && binary <= 658);
            codewords[0] = binary; // A

            return codewords;
        }

        private void postprocessCodewords(BigInteger[] codewords, ushort crc)
        {
            codewords[9] *= 2;

            if ((crc & (1 << 9)) != 0)
            {
                // 10th bit is set
                codewords[0] += 659;
            }
        }

        private static ushort ReverseUnsignedShort(ushort Input)
        {
            ushort Reverse = 0;
            for (int Index = 0; Index < 16; Index++)
            {
                Reverse <<= 1;
                Reverse |= (ushort)(Input & 1);
                Input >>= 1;
            }

            return Reverse;
        }

        private static int[] createCodewordToCharTable(bool createTable1)
        {
            int[] table = null;
            int N;
            if (createTable1)
            {
                table = new int[1287];
                N = 5;
            }
            else
            {
                table = new int[78];
                N = 2;
            }

            // Count up to 2^13 - 1 and find all those values that have N bits on
            int LUT_LowerIndex = 0;
            int LUT_UpperIndex = table.Length - 1;
            for (int Count = 0; Count < 8192; Count++)
            {
                int BitCount = 0;
                for (int BitIndex = 0; BitIndex < 13; BitIndex++)
                {
                    if ((Count & (1 << BitIndex)) != 0)
                        BitCount++;
                }

                // If we don't have the right number of bits on, go on to the next value
                if (BitCount != N)
                    continue;
                
                // If the reverse is less than count, we have already visited this pair before
                int Reverse = ReverseUnsignedShort((ushort)Count) >> 3;
                if (Reverse < Count)
                    continue;

                // If Count is symmetric, place it at the first free slot from the end of the 
                // list. Otherwise, place it at the first free slot from the beginning of the
                // list AND place Reverse at the next free slot from the beginning of the list.
                if (Count == Reverse)
                {
                    table[LUT_UpperIndex] = Count;
                    LUT_UpperIndex -= 1;
                }
                else
                {
                    table[LUT_LowerIndex] = Count;
                    LUT_LowerIndex += 1;

                    table[LUT_LowerIndex] = Reverse;
                    LUT_LowerIndex += 1;
                }
            }

            // Make sure the lower and upper parts of the table meet properly
            if (LUT_LowerIndex != (LUT_UpperIndex + 1))
                return null;

            return table;
        }

        private int[] getCharacters(BigInteger[] codewords)
        {
            int[] chars = new int[codewords.Length];
            for (int i = 0; i < codewords.Length; i++)
            {
                int c = codewords[i].IntValue();
                if (c >= 0 && c <= 1286)
                    chars[i] = m_Table1[c];
                else if (c > 1286 && c <= 1364)
                    chars[i] = m_Table2[c - 1287];
                else
                    throw new InvalidOperationException();
            }

            return chars;
        }

        private void postprocessCharacters(int[] characters, ushort crc)
        {
            for (int bitPos = 0; bitPos < 10; bitPos++)
            {
                if ((crc & (1 << bitPos)) != 0)
                    characters[bitPos] = ~characters[bitPos] & 8191;
            }
        }

        private string charactersToBars(int[] characters)
        {
            StringBuilder bars = new StringBuilder(65);
            for (int i = 1; i <= 65; i++)
            {
                int index = (i - 1) * 4;
                int descenderChar = m_barPositions[index++];
                int descenderBit = m_barPositions[index++];
                int ascenderChar = m_barPositions[index++];
                int ascenderBit = m_barPositions[index++];

                bool haveDescender = ((characters[descenderChar] & (1 << (descenderBit ))) != 0);
                bool haveAscender = ((characters[ascenderChar] & (1 << (ascenderBit ))) != 0);

                if (haveAscender && haveDescender)
                    bars.Append('F');
                else if (haveAscender)
                    bars.Append('A');
                else if (haveDescender)
                    bars.Append('D');
                else
                    bars.Append('T');
            }

            return bars.ToString();
        }
    }
}

    