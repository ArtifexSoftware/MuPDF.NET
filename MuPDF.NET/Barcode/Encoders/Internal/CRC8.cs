/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Class for calculating CRC8 checksums
    /// </summary>
    internal class CRC8
    {
        private byte[] m_table;

        /// <summary>
        /// Initializes a new instance of the <see cref="CRC8"/> class.
        /// </summary>
        /// <param name="polynomial">The generator polinomial.</param>
        public CRC8(int polynomial)
        {
            this.m_table = this.GenerateTable(polynomial);
        }


        private byte[] GenerateTable(int polynomial)
        {
            byte[] table = new byte[256];

            for (int i = 0; i < 256; ++i)
            {
                int r = i;
                for (int j = 0; j < 8; ++j)
                {
                    if ((r & 0x80) != 0)
                        r = (r << 1) ^ polynomial;
                    else
                        r <<= 1;
                }
                table[i] = (byte)r;
            }

            return table;
        }

        public byte Checksum(byte[] value)
        {
            if (value == null)
                throw new BarcodeException("CRC8.Checksum argument is null");

            byte crc = 0;
            for (int i = 0; i < value.Length; i++)
            {
                crc = m_table[crc ^ value[i]];
            }
            return crc;
        }
    }
}
