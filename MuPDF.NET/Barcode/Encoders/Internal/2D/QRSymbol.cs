/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;
using System.Drawing;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Produces encoded QR Code symbols. Symbol data is represented as an 
    /// array that contains width * width bytes. If the less significant bit 
    /// of the data byte is 1, the corresponding module is black.
    /// </summary>
    class QRSymbol
    {
        private class ReedSolomonBlock
        {
            public byte[] m_data;
            public byte[] m_eccData;

            public void Init(byte[] data, int startIndex, int dataLength, int eccLength)
            {
                m_data = new byte[dataLength];
                Array.Copy(data, startIndex, m_data, 0, dataLength);

                QRReedSolomon rs = new QRReedSolomon(8, 0x11d, 0, 1, eccLength, 255 - dataLength - eccLength);
                m_eccData = new byte[eccLength];
                rs.ProduceCodes(m_data, m_eccData);
            }
        };

        private class EncodedData
        {
            public int m_symbolVersion;
            public ReedSolomonBlock[] m_rsBlocks;

            public int m_dataLength;
            public int m_eccDataLength;

            private byte[] m_data;
            private int m_count;
            private int m_blockNum1;

            public EncodedData(QRInputData input)
            {
                m_data = input.GetBytes();
                if (m_data == null)
                    return;

                int[] spec = QRSpec.GetEccSpec(input.m_symbolVersion, input.m_ecLevel);
                m_symbolVersion = input.m_symbolVersion;

                m_rsBlocks = new ReedSolomonBlock[QRSpec.rsBlockNum(spec)];
                for (int i = 0; i < m_rsBlocks.Length; i++)
                    m_rsBlocks[i] = new ReedSolomonBlock();

                int dataCodePos = 0;
                int rsBlockIndex = 0;
                for (int i = 0; i < QRSpec.rsBlockNum1(spec); i++)
                {
                    m_rsBlocks[rsBlockIndex].Init(m_data, dataCodePos, QRSpec.rsDataCodes1(spec), QRSpec.rsEccCodes1(spec));
                    dataCodePos += QRSpec.rsDataCodes1(spec);
                    rsBlockIndex++;
                }

                for (int i = 0; i < QRSpec.rsBlockNum2(spec); i++)
                {
                    m_rsBlocks[rsBlockIndex].Init(m_data, dataCodePos, QRSpec.rsDataCodes2(spec), QRSpec.rsEccCodes2(spec));
                    dataCodePos += QRSpec.rsDataCodes2(spec);
                    rsBlockIndex++;
                }

                m_blockNum1 = QRSpec.rsBlockNum1(spec);
                m_dataLength = QRSpec.rsBlockNum1(spec) * QRSpec.rsDataCodes1(spec) + QRSpec.rsBlockNum2(spec) * QRSpec.rsDataCodes2(spec);
                m_eccDataLength = QRSpec.rsBlockNum(spec) * QRSpec.rsEccCodes1(spec);
            }

            public byte GetCode()
            {
                if (m_count < m_dataLength)
                {
                    int row = m_count % m_rsBlocks.Length;
                    int col = m_count / m_rsBlocks.Length;
                    if (col >= m_rsBlocks[row].m_data.Length)
                        row += m_blockNum1;

                    m_count++;
                    return m_rsBlocks[row].m_data[col];
                }
                else if (m_count < m_dataLength + m_eccDataLength)
                {
                    int row = (m_count - m_dataLength) % m_rsBlocks.Length;
                    int col = (m_count - m_dataLength) / m_rsBlocks.Length;

                    m_count++;
                    return m_rsBlocks[row].m_eccData[col];
                }

                return 0;
            }
        };

        private class SymbolFrameDriver
        {
            private int m_symbolWidth;

            private byte[] m_frame;

            private int m_currentX;
            private int m_currentY;

            private int m_verticalShiftAmount;

            private bool m_moveRight;
            private bool m_firstPositionRequested;

            public byte this[int pos]
            {
                get
                {
                    return m_frame[pos];
                }
                set
                {
                    m_frame[pos] = value;
                }
            }

            public SymbolFrameDriver(int symbolWidth, byte[] frame)
            {
                m_symbolWidth = symbolWidth;

                m_frame = frame;

                m_currentX = symbolWidth - 1;
                m_currentY = symbolWidth - 1;
                m_verticalShiftAmount = -1;
            }

            public int GetNextFillPosition()
            {
                if (!m_firstPositionRequested)
                {
                    m_firstPositionRequested = true;
                    return m_currentY * m_symbolWidth + m_currentX;
                }

                if (!m_moveRight)
                    m_currentX--;
                else
                {
                    m_currentX++;
                    m_currentY += m_verticalShiftAmount;
                }

                m_moveRight = !m_moveRight;

                if (m_verticalShiftAmount < 0)
                {
                    if (m_currentY < 0)
                    {
                        m_currentY = 0;
                        m_currentX -= 2;
                        m_verticalShiftAmount = 1;
                        if (m_currentX == 6)
                        {
                            m_currentX--;
                            m_currentY = 9;
                        }
                    }
                }
                else
                {
                    if (m_currentY == m_symbolWidth)
                    {
                        m_currentY = m_symbolWidth - 1;
                        m_currentX -= 2;
                        m_verticalShiftAmount = -1;
                        if (m_currentX == 6)
                        {
                            m_currentX--;
                            m_currentY -= 8;
                        }
                    }
                }

                if (m_currentX < 0 || m_currentY < 0)
                    throw new BarcodeException("Incorrect encoded data.");

                if ((m_frame[m_currentY * m_symbolWidth + m_currentX] & 0x80) != 0)
                    return GetNextFillPosition();

                return m_currentY * m_symbolWidth + m_currentX;
            }
        };

        /// <summary>
        /// Demerit coefficients. See Section 8.8.2, pp.45, JIS X0510:2004.
        /// </summary>
        private const int N1 = 3;
        private const int N2 = 3;
        private const int N3 = 40;
        private const int N4 = 10;

        private int m_width;
        private byte[] m_data;

        /// <summary>
        /// Gets the width of the produced symbol.
        /// </summary>
        /// <value>The width of the produced symbol.</value>
        public int Width
        {
            get 
            { 
                return m_width; 
            }
        }

        /// <summary>
        /// Gets the <see cref="System.Byte"/> of produced symbol data with the specified index.
        /// </summary>
        /// <value></value>
        public byte this[int index]
        {
            get
            {
                return m_data[index];
            }
        }

        /// <summary>
        /// Encodes the input data and produces QR Code symbol.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <returns>The produced QR Code symbol.</returns>
        private static QRSymbol encodeInput(QRInputData input)
        {
            return encodeUsingMask(input, -1);
        }

        /// <summary>
        /// Encodes the string and produces QR Code symbol.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <param name="symbolVersion">The minimum symbol version.</param>
        /// <param name="errorCorrectionLevel">The error correction level.</param>
        /// <param name="encodeHint">The encode hint.</param>
        /// <param name="caseSensitive">if set to <c>false</c> then value will be converted to upper case before processing.</param>
        /// <param name="FNC1Mode">Need to insert FNC1 prefix?</param>
        /// <returns>The produced QR Code symbol.</returns>
        public static QRSymbol EncodeString(string value, int symbolVersion, QRErrorCorrectionLevel errorCorrectionLevel, QREncodeHint encodeHint, bool caseSensitive, bool FNC1Mode)
        {
            QREncodeMode internalHint = QREncodeMode.Mode8;
            if (encodeHint == QREncodeHint.Kanji)
                internalHint = QREncodeMode.Kanji;

            QRInputData input = new QRInputData(symbolVersion, errorCorrectionLevel);
            if (input == null)
                return null;

            if (FNC1Mode)
                input.FNC1Mode = true;

            bool ret = input.BuildFromString(value, internalHint, caseSensitive);
            if (!ret)
                return null;

            return encodeInput(input);
        }

        private static QRSymbol encodeUsingMask(QRInputData input, int maskVersion)
        {
            if (input.m_symbolVersion < 0 || input.m_symbolVersion > QRSpec.MaximumSymbolVersion)
                throw new BarcodeException("Incorrect symbol version");

            if (input.m_ecLevel < QRErrorCorrectionLevel.Low || input.m_ecLevel > QRErrorCorrectionLevel.High)
                throw new BarcodeException("Incorrect error correction level");

            EncodedData encoded = new EncodedData(input);
            if (encoded.m_rsBlocks == null)
                throw new BarcodeException("Failed to produce error correction codes");

            int version = encoded.m_symbolVersion;
            int width = QRSpec.GetWidth(version);
            byte[] frame = QRSpec.CreateNewFrame(version);
            SymbolFrameDriver frameDriver = new SymbolFrameDriver(width, frame);

            for (int i = 0; i < encoded.m_dataLength + encoded.m_eccDataLength; i++)
            {
                byte code = encoded.GetCode();
                byte bitMask = 0x80;
                for (int j = 0; j < 8; j++)
                {
                    int framePos = frameDriver.GetNextFillPosition();

                    if ((bitMask & code) != 0)
                        frameDriver[framePos] = 0x03;
                    else
                        frameDriver[framePos] = 0x02;

                    bitMask = (byte)(bitMask >> 1);
                }
            }

            int remainderLength = QRSpec.GetRemainderLength(version);
            for (int i = 0; i < remainderLength; i++)
                frameDriver[frameDriver.GetNextFillPosition()] = 0x02;

            byte[] masked = null;
            if (maskVersion < 0)
                masked = MakeMask(width, frame, input.m_ecLevel);
            else
            {
                masked = MakeMask(width, frame, maskVersion);
                addFormatInfo(width, masked, maskVersion, input.m_ecLevel);
            }

            return new QRSymbol(width, masked);
        }

        private static int addFormatInfo(int width, byte[] frame, int maskVersion, QRErrorCorrectionLevel level)
        {
            int format = QRSpec.GetFormatInfo(maskVersion, level);
            int blacks = 0;

            for (int i = 0; i < 8; i++)
            {
                byte v = 0x84;
                if ((format & 1) != 0)
                {
                    blacks += 2;
                    v = 0x85;
                }

                frame[width * 8 + width - 1 - i] = v;

                if (i < 6)
                    frame[width * i + 8] = v;
                else
                    frame[width * (i + 1) + 8] = v;

                format = format >> 1;
            }

            for (int i = 0; i < 7; i++)
            {
                byte v = 0x84;
                if ((format & 1) != 0)
                {
                    blacks += 2;
                    v = 0x85;
                }

                frame[width * (width - 7 + i) + 8] = v;

                if (i == 0)
                    frame[width * 8 + 7] = v;
                else
                    frame[width * 8 + 6 - i] = v;

                format = format >> 1;
            }

            return blacks;
        }

        private QRSymbol(int width, byte[] data)
        {
            m_width = width;
            m_data = data;
        }

        public static byte[] MakeMask(int symbolWidth, byte[] symbolFrame, int maskVersion)
        {
            byte[] masked = new byte[symbolWidth * symbolWidth];

            switch (maskVersion)
            {
                case 0:
                    makeMask0(symbolWidth, symbolFrame, masked);
                    break;
                case 1:
                    makeMask1(symbolWidth, symbolFrame, masked);
                    break;
                case 2:
                    makeMask2(symbolWidth, symbolFrame, masked);
                    break;
                case 3:
                    makeMask3(symbolWidth, symbolFrame, masked);
                    break;
                case 4:
                    makeMask4(symbolWidth, symbolFrame, masked);
                    break;
                case 5:
                    makeMask5(symbolWidth, symbolFrame, masked);
                    break;
                case 6:
                    makeMask6(symbolWidth, symbolFrame, masked);
                    break;
                case 7:
                    makeMask7(symbolWidth, symbolFrame, masked);
                    break;
            }

            return masked;
        }

        public static byte[] MakeMask(int symbolWidth, byte[] symbolFrame, QRErrorCorrectionLevel level)
        {
            byte[] bestMask = null;
            int minDemerit = int.MaxValue;

            for (int i = 0; i < 8; i++)
            {
                byte[] currentMask = new byte[symbolWidth * symbolWidth];

                int demerit = 0;
                int blacks = 0;

                switch (i)
                {
                    case 0:
                        blacks = makeMask0(symbolWidth, symbolFrame, currentMask);
                        break;
                    case 1:
                        blacks = makeMask1(symbolWidth, symbolFrame, currentMask);
                        break;
                    case 2:
                        blacks = makeMask2(symbolWidth, symbolFrame, currentMask);
                        break;
                    case 3:
                        blacks = makeMask3(symbolWidth, symbolFrame, currentMask);
                        break;
                    case 4:
                        blacks = makeMask4(symbolWidth, symbolFrame, currentMask);
                        break;
                    case 5:
                        blacks = makeMask5(symbolWidth, symbolFrame, currentMask);
                        break;
                    case 6:
                        blacks = makeMask6(symbolWidth, symbolFrame, currentMask);
                        break;
                    case 7:
                        blacks = makeMask7(symbolWidth, symbolFrame, currentMask);
                        break;
                }

                blacks += addFormatInfo(symbolWidth, currentMask, i, level);
                blacks = 100 * blacks / (symbolWidth * symbolWidth);
                demerit = (Math.Abs(blacks - 50) / 5) * N4;
                demerit += evaluateSymbol(symbolWidth, currentMask);

                if (demerit < minDemerit)
                {
                    minDemerit = demerit;
                    bestMask = currentMask;
                }
                else
                {
                    currentMask = null;
                }
            }

            return bestMask;
        }

        private static int evaluateSymbol(int symbolWidth, byte[] symbolFrame)
        {
            int demerit = 0;
            int frameIndex = 0;

            int[] runLength = new int[QRSpec.MaximumSymbolWidth + 1];

            for (int y = 0; y < symbolWidth; y++)
            {
                int head = 0;
                runLength[0] = 1;
                for (int x = 0; x < symbolWidth; x++)
                {
                    if (x > 0 && y > 0)
                    {
                        byte b22 = (byte)(symbolFrame[frameIndex] & symbolFrame[frameIndex - 1] & symbolFrame[frameIndex - symbolWidth] & symbolFrame[frameIndex - symbolWidth - 1]);
                        byte w22 = (byte)(symbolFrame[frameIndex] | symbolFrame[frameIndex - 1] | symbolFrame[frameIndex - symbolWidth] | symbolFrame[frameIndex - symbolWidth - 1]);
                        if (((b22 | (w22 ^ 1)) & 1) != 0)
                            demerit += N2;
                    }
                    if (x == 0 && (symbolFrame[frameIndex] & 1) != 0)
                    {
                        runLength[0] = -1;
                        head = 1;
                        runLength[head] = 1;
                    }
                    else if (x > 0)
                    {
                        if (((symbolFrame[frameIndex] ^ symbolFrame[frameIndex - 1]) & 1) != 0)
                        {
                            head++;
                            runLength[head] = 1;
                        }
                        else
                            runLength[head]++;
                    }

                    frameIndex++;
                }

                demerit += calcN1N3(head + 1, runLength);
            }

            for (int x = 0; x < symbolWidth; x++)
            {
                int head = 0;
                runLength[0] = 1;
                frameIndex = x;
                for (int y = 0; y < symbolWidth; y++)
                {
                    if (y == 0 && (symbolFrame[frameIndex] & 1) != 0)
                    {
                        runLength[0] = -1;
                        head = 1;
                        runLength[head] = 1;
                    }
                    else if (y > 0)
                    {
                        if (((symbolFrame[frameIndex] ^ symbolFrame[frameIndex - symbolWidth]) & 1) != 0)
                        {
                            head++;
                            runLength[head] = 1;
                        }
                        else
                            runLength[head]++;
                    }

                    frameIndex += symbolWidth;
                }

                demerit += calcN1N3(head + 1, runLength);
            }

            return demerit;
        }

        private static int calcN1N3(int length, int[] runLength)
        {
            int demerit = 0;
            for (int i = 0; i < length; i++)
            {
                if (runLength[i] >= 5)
                    demerit += N1 + (runLength[i] - 5);

                if ((i & 1) != 0)
                {
                    if (i >= 3 && i < length - 2 && (runLength[i] % 3) == 0)
                    {
                        int fact = runLength[i] / 3;
                        if (runLength[i - 2] == fact && runLength[i - 1] == fact && runLength[i + 1] == fact && runLength[i + 2] == fact)
                        {
                            if (runLength[i - 3] < 0 || runLength[i - 3] >= 4 * fact)
                                demerit += N3;
                            else if (i + 3 >= length || runLength[i + 3] >= 4 * fact)
                                demerit += N3;
                        }
                    }
                }
            }

            return demerit;
        }

        private static int makeMaskImpl(int symbolWidth, byte[] symbolFrame, byte[] symbolMask, int maskVersion)
        {
            int b = 0;
            int frameIndex = 0;
            int maskIndex = 0;

            for (int y = 0; y < symbolWidth; y++)
            {
                for (int x = 0; x < symbolWidth; x++)
                {
                    if ((symbolFrame[frameIndex] & 0x80) != 0)
                    {
                        symbolMask[maskIndex] = symbolFrame[frameIndex];
                    }
                    else
                    {
                        int expression = 0;
                        switch (maskVersion)
                        {
                            case 0:
                                expression = (x + y) & 1;
                                break;
                            case 1:
                                expression = y & 1;
                                break;
                            case 2:
                                expression = x % 3;
                                break;
                            case 3:
                                expression = (x + y) % 3;
                                break;
                            case 4:
                                expression = ((y / 2) + (x / 3)) & 1;
                                break;
                            case 5:
                                expression = ((x * y) & 1) + (x * y) % 3;
                                break;
                            case 6:
                                expression = (((x * y) & 1) + (x * y) % 3) & 1;
                                break;
                            case 7:
                                expression = (((x * y) % 3) + ((x + y) & 1)) & 1;
                                break;
                        }

                        if (expression == 0)
                            symbolMask[maskIndex] = (byte)(symbolFrame[frameIndex] ^ 1);
                        else
                            symbolMask[maskIndex] = (byte)(symbolFrame[frameIndex] ^ 0);
                    }

                    b += (symbolMask[maskIndex] & 1);
                    frameIndex++;
                    maskIndex++;
                }
            }

            return b;
        }

        private static int makeMask0(int symbolWidth, byte[] symbolFrame, byte[] symbolMask)
        {
            return makeMaskImpl(symbolWidth, symbolFrame, symbolMask, 0);
        }

        private static int makeMask1(int symbolWidth, byte[] symbolFrame, byte[] symbolMask)
        {
            return makeMaskImpl(symbolWidth, symbolFrame, symbolMask, 1);
        }

        private static int makeMask2(int symbolWidth, byte[] symbolFrame, byte[] symbolMask)
        {
            return makeMaskImpl(symbolWidth, symbolFrame, symbolMask, 2);
        }

        private static int makeMask3(int symbolWidth, byte[] symbolFrame, byte[] symbolMask)
        {
            return makeMaskImpl(symbolWidth, symbolFrame, symbolMask, 3);
        }

        private static int makeMask4(int symbolWidth, byte[] symbolFrame, byte[] symbolMask)
        {
            return makeMaskImpl(symbolWidth, symbolFrame, symbolMask, 4);
        }

        private static int makeMask5(int symbolWidth, byte[] symbolFrame, byte[] symbolMask)
        {
            return makeMaskImpl(symbolWidth, symbolFrame, symbolMask, 5);
        }

        private static int makeMask6(int symbolWidth, byte[] symbolFrame, byte[] symbolMask)
        {
            return makeMaskImpl(symbolWidth, symbolFrame, symbolMask, 6);
        }

        private static int makeMask7(int symbolWidth, byte[] symbolFrame, byte[] symbolMask)
        {
            return makeMaskImpl(symbolWidth, symbolFrame, symbolMask, 7);
        }
    }
}
