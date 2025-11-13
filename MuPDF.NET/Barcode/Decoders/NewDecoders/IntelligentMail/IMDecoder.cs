using System;
using System.Globalization;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.IntelligentMail
{
    //Class to decode IM barcodes. We recieve an boolean array of ascendent and descendent bars and
    //returns the decoded message
	internal class IMDecoder: IDecoderAscDescBars
    {
        static int[] codeWordToChar = null;

        //each bar correspond to a bit of the byte string, but they are interleaved. Here are the indexs:
        static readonly int[] ascendantCharIndex = new int[] { 4, 0, 2, 6, 3, 5, 1, 9, 8, 7, 1, 2, 0, 6, 4, 8, 2, 9, 5, 3, 0, 1, 3, 7, 4, 6, 8, 9, 2, 0, 5, 1, 9, 4, 3, 8, 6, 7, 1, 2, 4, 3, 9, 5, 7, 8, 3, 0, 2, 1, 4, 0, 9, 1, 7, 0, 2, 4, 6, 3, 7, 1, 9, 5, 8 };
        static readonly int[] ascendantCharBit = new int[] { 3, 0, 8, 11, 1, 12, 8, 11, 10, 6, 4, 12, 2, 7, 9, 6, 7, 9, 2, 8, 4, 0, 12, 7, 10, 9, 0, 7, 10, 5, 7, 9, 6, 8, 2, 12, 1, 4, 2, 0, 1, 5, 4, 6, 12, 1, 0, 9, 4, 7, 5, 10, 2, 6, 9, 11, 2, 12, 6, 7, 5, 11, 0, 3, 2 };
        static readonly int[] descendantCharIndex = new int[] { 7, 1, 9, 5, 8, 0, 2, 4, 6, 3, 5, 8, 9, 7, 3, 0, 6, 1, 7, 4, 6, 8, 9, 2, 5, 1, 7, 5, 4, 3, 8, 7, 6, 0, 2, 5, 4, 9, 3, 0, 1, 6, 8, 2, 0, 4, 5, 9, 6, 7, 5, 2, 6, 3, 8, 5, 1, 9, 8, 7, 4, 0, 2, 6, 3 };
        static readonly int[] descendantCharBit = new int[] { 2, 10, 12, 5, 9, 1, 5, 4, 3, 9, 11, 5, 10, 1, 6, 3, 4, 1, 10, 0, 2, 11, 8, 6, 1, 12, 3, 8, 6, 4, 4, 11, 0, 6, 1, 9, 11, 5, 3, 7, 3, 10, 7, 11, 8, 2, 10, 3, 5, 8, 0, 3, 12, 11, 8, 4, 5, 1, 3, 0, 7, 12, 9, 8, 10 };

        public IMDecoder()
        {
            if (codeWordToChar == null)
            {
                codeWordToChar = new int[1365];
                int[] t5of13 = InitializeNof13Table(5, 1287);
                int[] t2of13 = InitializeNof13Table(2, 78);
                t5of13.CopyTo(codeWordToChar, 0);
                t2of13.CopyTo(codeWordToChar, 1287);
            }
        }


        //convert samples to code words. Not all combinations are allowed, thus FindChar can return null.
        //Some CRC bits are encoded in the code words, one bit per word. If crc bit is 1 then code word is reversed.
        int[] BarsToCodeWords(bool[][] samples, bool leftToRight, out int crc)
        {
            //Bars to chars
            int[] chars = new int[10]; //A(0), B(1),...J(9)
            for (int i = 0; i < 65; i++)
                if (leftToRight)
                {
                    if (samples[i][0]) chars[ascendantCharIndex[i]] |= 1 << ascendantCharBit[i];
                    if (samples[i][1]) chars[descendantCharIndex[i]] |= 1 << descendantCharBit[i];
                }
                else
                {
                    if (samples[64-i][1]) chars[ascendantCharIndex[i]] |= 1 << ascendantCharBit[i];
                    if (samples[64-i][0]) chars[descendantCharIndex[i]] |= 1 << descendantCharBit[i];
                }

            //Chars to CodeWords
            int[] codeWords = new int[10];
            crc = 0;
            int p2 = 1;
            for (int i = 0; i < 10; i++)
            {
                int pos = FindChar(chars[i]);
                if (pos >= 0) codeWords[i] = pos;
                else
                { //if fails try in the reversed direction and set crc bit
                    int inverted = ~chars[i] & 8191;
                    pos = FindChar(inverted);
                    if (pos >= 0) codeWords[i] = pos;
                    else return null; //wrong code
                    crc += p2;
                }
                p2 *= 2;
            }
            return codeWords;
        }

        //Decode two boolean array (for ascendent and descendent bars) and check crc.
        public string Decode(bool[][] samples, out float confidence)
        {
            confidence=1.0f;
            int crc;
            int[] codeWords = BarsToCodeWords(samples, true, out crc);
            if (codeWords == null) codeWords = BarsToCodeWords(samples, false, out crc);
            if (codeWords == null) return null;

            //check orientation
            if ((codeWords[9] & 1) == 1)
            {
                for (int i = 0; i < 5; i++) { int c = codeWords[i]; codeWords[i] = codeWords[9 - i]; codeWords[9 - i] = c; }
                if ((codeWords[9] & 1) == 1) return null; //wrong code
            }
            codeWords[9] /= 2;

            //remove FCS of A
            if (codeWords[0] >= 659)
            {
                codeWords[0] -= 659;
                crc += (1 << 10); //p2
            }

            //To binary
            BigInt binary = new BigInt(140, 0);
            for (int i = 0; i < 10; i++)
            {
                binary = binary * new BigInt(i < 9 ? 1365 : 636);
                binary = binary + new BigInt(codeWords[i]);
            }

            //Check CRC
            int crc2 = this.USPS_MSB_Math_CRC11GenerateFrameCheckSequence(binary);
            if (crc != crc2) confidence = 0f;

            //binary to string message
            BigInt serviceTypeMailerIdSerial, id1, id0;
            Decimal routingCode=0;
            binary.Divide(new BigInt((decimal)1e18), out binary, out serviceTypeMailerIdSerial);
            binary.Divide(new BigInt(5), out binary, out id1);
            binary.Divide(new BigInt(10), out binary, out id0);
			Decimal d = binary.ToDecimal();
			int digits = 0;
			if (d >= (decimal) (1e9 + 1e5 + 1)) { routingCode = d - (decimal) (1e9 + 1e5 + 1); digits = 11; }
			else if (d >= (decimal) (1e5 + 1)) { routingCode = d - (decimal) (1e5 + 1); digits = 9; }
			else if (d >= (decimal) (1)) { routingCode = d - (decimal) (1); digits = 5; }

			// pad serviceTypeMailerIdSerial to 18 digits
			string serviceTypeMailerIdSerialStr = serviceTypeMailerIdSerial.ToDecimal().ToString(CultureInfo.InvariantCulture).PadLeft(18, '0');
			// pad routine code to 0, 5, 9, or 11 digits
			string routingCodeStr = digits == 0 ? "" : "-" + routingCode.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');

			return "" + id0.ToInt() + id1.ToInt() + "-" + serviceTypeMailerIdSerialStr + routingCodeStr;
        }


        private int FindChar(int ch)
        {
            for (int i = 0; i < codeWordToChar.Length; i++)
                if (codeWordToChar[i] == ch) return i;
            return -1;
        }





        /******************************************************************************
        ** InitializeNof13Table
        **
        ** Inputs:
        ** N is the type of table (i.e. 5 for 5of13 table, 2 for 2of13 table
        ** TableLength is the length of the table requested (i.e. 78 for 2of13 table)
        ** Output:
        ** TableNof13 is a pointer to the resulting table
        ******************************************************************************/
        private int[] InitializeNof13Table(int N, int TableLength)
        {
            int LUT_LowerIndex, LUT_UpperIndex;
            int[] TableNof13 = new int[TableLength];

            /* Count up to 2^13 - 1 and find all those values that have N bits on */
            LUT_LowerIndex = 0;
            LUT_UpperIndex = TableLength - 1;
            for (int Count = 0; Count < 8192; Count++)
            {
                int BitCount = 0;
                for (int BitIndex = 0; BitIndex < 13; BitIndex++)
                    BitCount += ((Count & (1 << BitIndex)) != 0 ? 1 : 0);

                /* If we don't have the right number of bits on, go on to the next value */
                if (BitCount != N) continue;
                /* If the reverse is less than count, we have already visited this pair before */
                int Reverse = ReverseUnsignedShort(Count) >> 3;
                if (Reverse < Count) continue;
                /* If Count is symmetric, place it at the first free slot from the end of the */
                /* list. Otherwise, place it at the first free slot from the beginning of the */
                /* list AND place Reverse at the next free slot from the beginning of the list.*/
                if (Count == Reverse)
                {
                    TableNof13[LUT_UpperIndex] = Count;
                    LUT_UpperIndex -= 1;
                }
                else
                {
                    TableNof13[LUT_LowerIndex] = Count;
                    LUT_LowerIndex += 1;
                    TableNof13[LUT_LowerIndex] = Reverse;
                    LUT_LowerIndex += 1;
                }
            }

            /* Make sure the lower and upper parts of the table meet properly */
            if (LUT_LowerIndex != (LUT_UpperIndex + 1)) return null;
            return TableNof13;
        }

        private int ReverseUnsignedShort(int Input)
        {
            int Reverse = 0;
            for (int i = 0; i < 16; i++)
            {
                Reverse <<= 1;
                Reverse |= Input & 1;
                Input >>= 1;
            }
            return Reverse;
        }

        /***************************************************************************
        ** USPS_MSB_Math_CRC11GenerateFrameCheckSequence
        **
        ** Inputs:
        ** ByteArrayPtr is the address of a 13 byte array holding 102 bits which
        ** are right justified - ie: the leftmost 2 bits of the first byte do not
        ** hold data and must be set to zero.
        **
        ** Outputs:
        ** return unsigned short - 11 bit Frame Check Sequence (right justified)
        ***************************************************************************/
        int USPS_MSB_Math_CRC11GenerateFrameCheckSequence(BigInt n)
        {
            int GeneratorPolynomial = 0x0F35;
            int FrameCheckSequence = 0x07FF;

            /* Do most significant byte skipping the 2 most significant bits */
            int Data = n.GetByte(12);
            Data <<= 5;
            for (int Bit = 2; Bit < 8; Bit++)
            {
                if (((FrameCheckSequence ^ Data) & 0x0400) != 0)
                    FrameCheckSequence = (FrameCheckSequence << 1) ^ GeneratorPolynomial;
                else
                    FrameCheckSequence = (FrameCheckSequence << 1);
                FrameCheckSequence &= 0x7FF;
                Data <<= 1;
            }
            /* Do rest of the bytes */
            for (int ByteIndex = 1; ByteIndex < 13; ByteIndex++)
            {
                Data = n.GetByte(12 - ByteIndex);
                Data <<= 3;
                for (int Bit = 0; Bit < 8; Bit++)
                {
                    if (((FrameCheckSequence ^ Data) & 0x0400) != 0)
                        FrameCheckSequence = (FrameCheckSequence << 1) ^ GeneratorPolynomial;
                    else
                        FrameCheckSequence = (FrameCheckSequence << 1);
                    FrameCheckSequence &= 0x7FF;
                    Data <<= 1;
                }
            }
            return FrameCheckSequence;
        }
    }
}
