/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Stores bits (not bytes) and allows accumulation of bits in groups 
    /// with length not equal to byte length (not equal to 8).
    /// </summary>
    class QRBitStream
    {
        private char[] m_bits;

        public QRBitStream()
        {
        }

        public QRBitStream(int bitCount, int value)
        {
            m_bits = new char[bitCount];
            int mask = 1 << (bitCount - 1);
            for (int i = 0; i < bitCount; i++)
            {
                if ((value & mask) != 0)
                    m_bits[i] = '1';
                else
                    m_bits[i] = '0';

                mask = mask >> 1;
            }
        }

        public void Append(QRBitStream stream)
        {
            if (stream == null || stream.m_bits == null)
                return;

            if (m_bits == null)
            {
                m_bits = new char[stream.m_bits.Length];
                Array.Copy(stream.m_bits, m_bits, m_bits.Length);
                return;
            }

            char[] newBits = new char[m_bits.Length + stream.m_bits.Length];
            Array.Copy(m_bits, newBits, m_bits.Length);
            Array.Copy(stream.m_bits, 0, newBits, m_bits.Length, stream.m_bits.Length);

            m_bits = newBits;
        }

        public void AppendNumber(int bitCount, int value)
        {
            Append(new QRBitStream(bitCount, value));
        }

        public int Length
        {
            get
            {
                if (m_bits == null)
                    return 0;

                return m_bits.Length;
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[(Length + 7) / 8];
            int byteCount = Length / 8;

            int dataIndex = 0;
            for (int i = 0; i < byteCount; i++)
            {
                byte v = 0;

                for (int j = 0; j < 8; j++)
                {
                    v = (byte)(v << 1);

                    if (m_bits[dataIndex] == '1')
                        v++;

                    dataIndex++;
                }

                bytes[i] = v;
            }

            if ((Length & 7) != 0)
            {
                byte v = 0;

                for (int j = 0; j < (Length & 7); j++)
                {
                    v = (byte)(v << 1);
                    if (m_bits[dataIndex] == '1')
                        v++;

                    dataIndex++;
                }

                bytes[byteCount] = v;
            }

            return bytes;
        }
    }
}
