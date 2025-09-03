/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Reed Solomon algorithm adapted for QR Code barcodes.
    /// </summary>
    class QRReedSolomon
    {
        private int m_bitsPerSymbol;
        private int m_symbolsPerBlock;

        /// <summary>
        /// Log lookup table.
        /// </summary>
        private byte[] m_alphaTo;

        /// <summary>
        /// Antilog lookup table.
        /// </summary>
        private byte[] m_indexOf;

        /// <summary>
        /// Generator polynomial.
        /// </summary>
        private byte[] m_genPoly;

        /// <summary>
        /// Number of generator roots (equal to number of produced ecc).
        /// </summary>
        private int m_generatorRootCount;

        private int m_padByteCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="QRReedSolomon"/> class.
        /// </summary>
        /// <param name="symbolSize">Size of the symbol in bits.</param>
        /// <param name="fgpoly">Field generator polynomial coefficients.</param>
        /// <param name="firstRoot">The first root.</param>
        /// <param name="primitive">The primitive element to generate polynomial roots.</param>
        /// <param name="rootCount">The root count.</param>
        /// <param name="padCount">The pad count.</param>
        public QRReedSolomon(int symbolSize, int fgpoly, int firstRoot, int primitive, int rootCount, int padCount)
        {
            if (symbolSize < 0 || symbolSize > 8)
                return;

            if (firstRoot < 0 || firstRoot >= (1 << symbolSize))
                return;

            if (primitive <= 0 || primitive >= (1 << symbolSize))
                return;

            if (rootCount < 0 || rootCount >= (1 << symbolSize))
                return; 

            if (padCount < 0 || padCount >= ((1 << symbolSize) - 1 - rootCount))
                return;

            m_bitsPerSymbol = symbolSize;
            m_symbolsPerBlock = (1 << symbolSize) - 1;
            m_padByteCount = padCount;

            m_alphaTo = new byte[m_symbolsPerBlock + 1];
            if (m_alphaTo == null)
                return;

            m_indexOf = new byte[m_symbolsPerBlock + 1];
            if (m_indexOf == null)
                return;

            // Generate Galois field lookup tables
            m_indexOf[0] = (byte)m_symbolsPerBlock;
            m_alphaTo[m_symbolsPerBlock] = 0;

            int sr = 1;
            for (int i = 0; i < m_symbolsPerBlock; i++)
            {
                m_indexOf[sr] = (byte)i;
                m_alphaTo[i] = (byte)sr;
                sr <<= 1;
                if ((sr & (1 << symbolSize)) != 0)
                    sr ^= fgpoly;
                sr &= m_symbolsPerBlock;
            }

            if (sr != 1)
            {
                // field generator polynomial is not primitive.
                return;
            }

            // form RS code generator polynomial from its roots.
            m_genPoly = new byte[rootCount + 1];
            if (m_genPoly == null)
                return;

            m_generatorRootCount = rootCount;

            m_genPoly[0] = 1;
            for (int i = 0, root = firstRoot * primitive; i < rootCount; i++, root += primitive)
            {
                m_genPoly[i + 1] = 1;

                for (int j = i; j > 0; j--)
                {
                    if (m_genPoly[j] != 0)
                        m_genPoly[j] = (byte)(m_genPoly[j - 1] ^ m_alphaTo[modnn(m_indexOf[m_genPoly[j]] + root)]);
                    else
                        m_genPoly[j] = m_genPoly[j - 1];
                }

                m_genPoly[0] = m_alphaTo[modnn(m_indexOf[m_genPoly[0]] + root)];
            }

            for (int i = 0; i <= rootCount; i++)
                m_genPoly[i] = m_indexOf[m_genPoly[i]];
        }

        /// <summary>
        /// Produces the error correction codes for the given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="rsData">The array to put error correction codes to.</param>
        public void ProduceCodes(byte[] data, byte[] rsData)
        {
            Array.Clear(rsData, 0, m_generatorRootCount);

            for (int i = 0; i < m_symbolsPerBlock - m_generatorRootCount - m_padByteCount; i++)
            {
                int feedback = m_indexOf[data[i] ^ rsData[0]];
                if (feedback != m_symbolsPerBlock)
                {
                    for (int j = 1; j < m_generatorRootCount; j++)
                        rsData[j] ^= m_alphaTo[modnn(feedback + m_genPoly[m_generatorRootCount - j])];
                }

                for (int pos = 0; pos < m_generatorRootCount - 1; pos++)
                    rsData[pos] = rsData[pos + 1];

                if (feedback != m_symbolsPerBlock)
                    rsData[m_generatorRootCount - 1] = m_alphaTo[modnn(feedback + m_genPoly[0])];
                else
                    rsData[m_generatorRootCount - 1] = 0;
            }
        }

        private int modnn(int x)
        {
            while (x >= m_symbolsPerBlock)
            {
                x -= m_symbolsPerBlock;
                x = (x >> m_bitsPerSymbol) + (x & m_symbolsPerBlock);
            }

            return x;
        }
    }
}
