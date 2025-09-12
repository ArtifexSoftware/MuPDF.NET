/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;
using System.Collections;

namespace BarcodeWriter.Core.Internal
{
    class QRInputData
    {
        private class InputDataChunk
        {
            public QREncodeMode m_encodeMode;
            public byte[] m_data;
            public QRBitStream m_stream;

            public InputDataChunk(QREncodeMode mode, byte[] data, int dataLength)
            {
                m_encodeMode = mode;
                m_data = new byte[dataLength];
                Array.Copy(data, m_data, m_data.Length);
            }
        };

        private static int[] alphaNumericTable = 
        {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            36, -1, -1, -1, 37, 38, -1, -1, -1, -1, 39, 40, -1, 41, 42, 43,
             0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 44, -1, -1, -1, -1, -1,
            -1, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
            25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };

        public int m_symbolVersion;
        public QRErrorCorrectionLevel m_ecLevel;
        private ArrayList m_chunks;

        /// <summary>
        /// Need to insert FNC1 prefix?
        /// </summary>
        public bool FNC1Mode = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="QRInputData"/> class.
        /// </summary>
        public QRInputData()
        {
            init(0, QRErrorCorrectionLevel.Low);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QRInputData"/> class.
        /// </summary>
        /// <param name="version">The minimum version (may be increased if neccessary).</param>
        /// <param name="level">The error correction level.</param>
        public QRInputData(int version, QRErrorCorrectionLevel level)
        {
            init(version, level);
        }

        /// <summary>
        /// Appends the data.
        /// </summary>
        /// <param name="mode">The encoded mode.</param>
        /// <param name="data">The data to append.</param>
        /// <returns></returns>
        public bool AppendData(QREncodeMode mode, byte[] data)
        {
            if (!validateData(mode, data.Length, data))
                return false;

            InputDataChunk chunk = new InputDataChunk(mode, data, data.Length);
            if (chunk == null)
                return false;

            m_chunks.Add(chunk);
            return true;
        }

        private static bool validateData(QREncodeMode mode, int dataLength, byte[] data)
        {
            if (dataLength <= 0)
                return false;

            switch (mode)
            {
                case QREncodeMode.Numeric:
                    return validateNumericData(dataLength, data);

                case QREncodeMode.AlphaNumeric:
                    return validateAlphaNumericData(dataLength, data);

                case QREncodeMode.Kanji:
                    return validateKanjiData(dataLength, data);

                case QREncodeMode.Mode8:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the bytes of all bit streams. Bits get padded if needed.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            QRBitStream bstream = getCompoundBitStream();
            if (bstream == null)
                return null;

            return bstream.GetBytes();
        }

        private int estimateBitStreamSize(int version)
        {
            int bits = 0;

            if (FNC1Mode)
                bits += 4; //FNC1 prefix = 4 bits

            foreach (InputDataChunk chunk in m_chunks)
                bits += estimateBitStreamSize(chunk, version);

            return bits;
        }

        /// <summary>
        /// Merges all bit streams.
        /// </summary>
        /// <returns></returns>
        private QRBitStream mergeBitStreams()
        {
            if (!convertData())
                return null;

            QRBitStream bstream = new QRBitStream();
            if (bstream == null)
                return null;

            if (FNC1Mode)
                bstream.AppendNumber(4, 5);//FNC1 = 0101

            foreach (InputDataChunk chunk in m_chunks)
                bstream.Append(chunk.m_stream);

            return bstream;
        }

        /// <summary>
        /// Gets the compound bit stream (merged from all bit streams and padded).
        /// </summary>
        /// <returns></returns>
        private QRBitStream getCompoundBitStream()
        {
            QRBitStream bstream = mergeBitStreams();
            if (bstream == null)
                return null;

            QRBitStream padding = createPaddingBitStream();
            if (padding != null)
                bstream.Append(padding);

            return bstream;
        }

        /// <summary>
        /// Estimates the numeric chunk bit count.
        /// </summary>
        private static int estimateNumericBitCount(int numericByteCount)
        {
            int w = numericByteCount / 3;
            int bitCount = w * 10;
            switch (numericByteCount - w * 3)
            {
                case 1:
                    bitCount += 4;
                    break;

                case 2:
                    bitCount += 7;
                    break;
            }

            return bitCount;
        }

        /// <summary>
        /// Estimates the alpha numeric chunk bit count.
        /// </summary>
        /// <returns></returns>
        private static int estimateAlphaNumericBitCount(int alphaNumericByteCount)
        {
            int w = alphaNumericByteCount / 2;
            int bitCount = w * 11;
            if ((alphaNumericByteCount & 1) != 0)
                bitCount += 6;


            return bitCount;
        }

        /// <summary>
        /// Estimates the mode8 chunk bit count.
        /// </summary>
        /// <returns></returns>
        private static int estimateMode8BitCount(int byteCount)
        {
            return byteCount * 8;
        }

        /// <summary>
        /// Estimates the kanji chunk bit count.
        /// </summary>
        /// <returns></returns>
        private static int estimateKanjiBitCount(int size)
        {
            return (size / 2) * 13;
        }

        private static int getValueForAlphaNumeric(int c)
        {
            if ((c & 0x80) != 0)
                return -1;

            return alphaNumericTable[c];
        }

        private void init(int version, QRErrorCorrectionLevel level)
        {
            if (version < 0 || version > QRSpec.MaximumSymbolVersion || level < QRErrorCorrectionLevel.Low || level > QRErrorCorrectionLevel.High)
                throw new BarcodeException("Incorrect version or error correction level");

            m_chunks = new ArrayList();
            m_symbolVersion = version;
            m_ecLevel = level;
        }

        /// <summary>
        /// Creates the bit stream from input data chunks.
        /// </summary>
        /// <returns></returns>
        private int createBitStream()
        {
            int bits = 0;

            foreach (InputDataChunk chunk in m_chunks)
                bits += encodeBitStream(chunk, m_symbolVersion);

            return bits;
        }

        /// <summary>
        /// Converts the data to a bit stream.
        /// </summary>
        /// <returns></returns>
        private bool convertData()
        {
            int ver = estimateVersion();
            if (ver > m_symbolVersion)
                m_symbolVersion = ver;

            for ( ; ; )
            {
                int bits = createBitStream();
                ver = QRSpec.GetMinimumVersion((bits + 7) / 8, m_ecLevel);
                if (ver < 0)
                    return false;
                else if (ver > m_symbolVersion)
                    m_symbolVersion = ver;
                else
                    break;
            }

            return true;
        }

        /// <summary>
        /// Encodes input data chunk into a bit stream.
        /// </summary>
        /// <returns></returns>
        private static int encodeBitStream(InputDataChunk chunk, int version)
        {
            chunk.m_stream = null;

            int words = QRSpec.GetMaximumWords(chunk.m_encodeMode, version);
            if (chunk.m_data.Length > words)
            {
                byte[] temp = new byte[chunk.m_data.Length - words];
                Array.Copy(chunk.m_data, words, temp, 0, chunk.m_data.Length - words);

                InputDataChunk st1 = new InputDataChunk(chunk.m_encodeMode, chunk.m_data, words);
                InputDataChunk st2 = new InputDataChunk(chunk.m_encodeMode, temp, chunk.m_data.Length - words);

                encodeBitStream(st1, version);
                encodeBitStream(st2, version);

                chunk.m_stream = new QRBitStream();
                chunk.m_stream.Append(st1.m_stream);
                chunk.m_stream.Append(st2.m_stream);
            }
            else
            {
                switch (chunk.m_encodeMode)
                {
                    case QREncodeMode.Numeric:
                        encodeNumeric(chunk, version);
                        break;

                    case QREncodeMode.AlphaNumeric:
                        encodeAlphaNumeric(chunk, version);
                        break;

                    case QREncodeMode.Mode8:
                        encodeMode8(chunk, version);
                        break;

                    case QREncodeMode.Kanji:
                        encodeModeKanji(chunk, version);
                        break;
                }
            }

            return chunk.m_stream.Length;
        }

        private static bool validateAlphaNumericData(int size, byte[] data)
        {
            for (int i = 0; i < size; i++)
            {
                if (getValueForAlphaNumeric(data[i]) < 0)
                    return false;
            }

            return true;
        }

        private static void encodeAlphaNumeric(InputDataChunk chunk, int version)
        {
            int words = chunk.m_data.Length / 2;
            chunk.m_stream = new QRBitStream();

            int val = 0x2;
            chunk.m_stream.AppendNumber(4, val);

            val = chunk.m_data.Length;
            chunk.m_stream.AppendNumber(QRSpec.GetLengthIndicator(QREncodeMode.AlphaNumeric, version), val);

            for (int i = 0; i < words; i++)
            {
                val = getValueForAlphaNumeric(chunk.m_data[i * 2]) * 45;
                val += getValueForAlphaNumeric(chunk.m_data[i * 2 + 1]);
                chunk.m_stream.AppendNumber(11, val);
            }

            if ((chunk.m_data.Length & 1) != 0)
            {
                val = getValueForAlphaNumeric(chunk.m_data[words * 2]);
                chunk.m_stream.AppendNumber(6, val);
            }
        }

        private static bool validateNumericData(int size, byte[] data)
        {
            for (int i = 0; i < size; i++)
            {
                if (data[i] < '0' || data[i] > '9')
                    return false;
            }

            return true;
        }

        private static bool validateKanjiData(int size, byte[] data)
        {
            if ((size & 1) != 0)
                return false;

            for (int i = 0; i < size; i += 2)
            {
                uint val = ((uint)data[i] << 8) | data[i + 1];
                if (val < 0x8140 || (val > 0x9ffc && val < 0xe040) || val > 0xebbf)
                    return false;
            }

            return true;
        }

        private static int estimateBitStreamSize(InputDataChunk chunk, int version)
        {
            if (version == 0)
                version = 1;

            int bits = 0;
            switch (chunk.m_encodeMode)
            {
                case QREncodeMode.Numeric:
                    bits = estimateNumericBitCount(chunk.m_data.Length);
                    break;

                case QREncodeMode.AlphaNumeric:
                    bits = estimateAlphaNumericBitCount(chunk.m_data.Length);
                    break;

                case QREncodeMode.Mode8:
                    bits = estimateMode8BitCount(chunk.m_data.Length);
                    break;

                case QREncodeMode.Kanji:
                    bits = estimateKanjiBitCount(chunk.m_data.Length);
                    break;

                default:
                    return 0;
            }

            int l = QRSpec.GetLengthIndicator(chunk.m_encodeMode, version);
            int m = 1 << l;
            int num = (chunk.m_data.Length + m - 1) / m;

            // mode indicator (4bits) + length indicator
            bits += num * (4 + l);
            return bits;
        }

        private QRBitStream createPaddingBitStream()
        {
            int bits = 0;
            foreach (InputDataChunk chunk in m_chunks)
                bits += chunk.m_stream.Length;

            int maxwords = QRSpec.GetDataLength(m_symbolVersion, m_ecLevel);
            int maxbits = maxwords * 8;
            QRBitStream bstream = null;

            if (maxbits - bits < 5)
            {
                if (maxbits == bits)
                    return null;
                else
                {
                    bstream = new QRBitStream();
                    bstream.AppendNumber(maxbits - bits, 0);
                    return bstream;
                }
            }

            bits += 4;
            int words = (bits + 7) / 8;

            bstream = new QRBitStream();
            bstream.AppendNumber(words * 8 - bits + 4, 0);

            for (int i = 0; i < maxwords - words; i++)
            {
                if ((i & 1) != 0)
                    bstream.AppendNumber(8, 0x11);
                else
                    bstream.AppendNumber(8, 0xec);
            }

            return bstream;
        }

        /// <summary>
        /// Estimates the minimum version required in order to encode data.
        /// </summary>
        /// <returns></returns>
        private int estimateVersion()
        {
            int version = 0;
            int prevVersion;

            do
            {
                prevVersion = version;

                int bits = estimateBitStreamSize(prevVersion);
                version = QRSpec.GetMinimumVersion((bits + 7) / 8, m_ecLevel);
                if (version < 0)
                    return -1;

            } while (version > prevVersion);

            return version;
        }

        private static void encodeNumeric(InputDataChunk chunk, int version)
        {
            int words = chunk.m_data.Length / 3;
            chunk.m_stream = new QRBitStream();

            int val = 0x1;
            chunk.m_stream.AppendNumber(4, val);

            val = chunk.m_data.Length;
            chunk.m_stream.AppendNumber(QRSpec.GetLengthIndicator(QREncodeMode.Numeric, version), val);

            for (int i = 0; i < words; i++)
            {
                val = (chunk.m_data[i * 3] - '0') * 100;
                val += (chunk.m_data[i * 3 + 1] - '0') * 10;
                val += (chunk.m_data[i * 3 + 2] - '0');

                chunk.m_stream.AppendNumber(10, val);
            }

            if (chunk.m_data.Length - words * 3 == 1)
            {
                val = chunk.m_data[words * 3] - '0';
                chunk.m_stream.AppendNumber(4, val);
            }
            else if (chunk.m_data.Length - words * 3 == 2)
            {
                val = (chunk.m_data[words * 3] - '0') * 10;
                val += (chunk.m_data[words * 3 + 1] - '0');
                chunk.m_stream.AppendNumber(7, val);
            }
        }

        private static void encodeMode8(InputDataChunk chunk, int version)
        {
            chunk.m_stream = new QRBitStream();

            int val = 0x4;
            chunk.m_stream.AppendNumber(4, val);

            val = chunk.m_data.Length;
            chunk.m_stream.AppendNumber(QRSpec.GetLengthIndicator(QREncodeMode.Mode8, version), val);

            for (int i = 0; i < chunk.m_data.Length; i++)
                chunk.m_stream.AppendNumber(8, chunk.m_data[i]);
        }

        private static void encodeModeKanji(InputDataChunk chunk, int version)
        {
            chunk.m_stream = new QRBitStream();

            int val = 0x8;
            chunk.m_stream.AppendNumber(4, val);

            val = chunk.m_data.Length / 2;
            chunk.m_stream.AppendNumber(QRSpec.GetLengthIndicator(QREncodeMode.Kanji, version), val);

            for (int i = 0; i < chunk.m_data.Length; i += 2)
            {
                val = (int)(((uint)chunk.m_data[i] << 8) | chunk.m_data[i + 1]);
                if (val <= 0x9ffc)
                    val -= 0x8140;
                else
                    val -= 0xc140;

                int h = (val >> 8) * 0xc0;
                val = (val & 0xff) + h;

                chunk.m_stream.AppendNumber(13, val);
            }
        }

        public bool BuildFromString(string s, QREncodeMode hint, bool caseSensitive)
        {
            if (!caseSensitive)
            {
                string newstr = toUpper(s, hint);
                if (newstr == null)
                    return false;

                return splitString(newstr, hint);
            }

            return splitString(s, hint);
        }

        private static QREncodeMode identifyMode(string s, QREncodeMode hint)
        {
            if (s.Length == 0)
                return QREncodeMode.Incorrect;

            if (isDigit((byte)s[0]))
                return QREncodeMode.Numeric;
            else if (isAlphaNumeric((byte)s[0]))
                return QREncodeMode.AlphaNumeric;
            else if (hint == QREncodeMode.Kanji && s.Length > 1)
            {
                uint word = ((uint)s[0] << 8) | s[1];
                if ((word >= 0x8140 && word <= 0x9ffc) || (word >= 0xe040 && word <= 0xebbf))
                    return QREncodeMode.Kanji;
            }

            return QREncodeMode.Mode8;
        }

        private int eatNumericChunk(string s, QREncodeMode hint)
        {
            int chunkLength = 0;
            for (int i = 0; i < s.Length && isDigit((byte)s[chunkLength]); i++)
                chunkLength++;

            int li = QRSpec.GetLengthIndicator(QREncodeMode.Numeric, m_symbolVersion);

            QREncodeMode mode = identifyMode(s.Substring(chunkLength), hint);
            if (mode == QREncodeMode.Mode8)
            {
                int dif = estimateNumericBitCount(chunkLength) + 4 + li + estimateMode8BitCount(1) - estimateMode8BitCount(chunkLength + 1);
                if (dif > 0)
                    return eatMode8Chunk(s, hint);
            }
            else if (mode == QREncodeMode.AlphaNumeric)
            {
                int dif = estimateNumericBitCount(chunkLength) + 4 + li + estimateAlphaNumericBitCount(1) - estimateAlphaNumericBitCount(chunkLength + 1);
                if (dif > 0)
                    return eatAlphaNumericChunk(s, hint);
            }

            bool ret = AppendData(QREncodeMode.Numeric, Encoding.UTF8.GetBytes(s.Substring(0, chunkLength)));
            if (!ret)
                return -1;

            return chunkLength;
        }

        private int eatAlphaNumericChunk(string s, QREncodeMode hint)
        {
            int liNumeric = QRSpec.GetLengthIndicator(QREncodeMode.Numeric, m_symbolVersion);

            int pos = 0;
            while (pos < s.Length && isAlphaNumeric((byte)s[pos]))
            {
                if (isDigit((byte)s[pos]))
                {
                    int numericPartPos = pos;
                    while (numericPartPos < s.Length && isDigit((byte)s[numericPartPos]))
                        numericPartPos++;

                    int dif = estimateAlphaNumericBitCount(pos) + estimateNumericBitCount(numericPartPos - pos) + 4 + liNumeric - estimateAlphaNumericBitCount(numericPartPos);
                    if (dif < 0)
                        break;
                    else
                        pos = numericPartPos;
                }
                else
                    pos++;
            }

            int liAlphaNumeric = QRSpec.GetLengthIndicator(QREncodeMode.AlphaNumeric, m_symbolVersion);
            int chunkLength = pos;
            if (pos != s.Length && !isAlphaNumeric((byte)s[pos]))
            {
                int dif = estimateAlphaNumericBitCount(chunkLength) + 4 + liAlphaNumeric + estimateMode8BitCount(1) - estimateMode8BitCount(chunkLength + 1);
                if (dif > 0)
                    return eatMode8Chunk(s, hint);
            }

            bool ret = AppendData(QREncodeMode.AlphaNumeric, Encoding.UTF8.GetBytes(s.Substring(0, chunkLength)));
            if (!ret)
                return -1;

            return chunkLength;
        }

        private int eatMode8Chunk(string s, QREncodeMode hint)
        {
            int pos = 1;
            while (pos < s.Length && s[pos] != '\0')
            {
                QREncodeMode mode = identifyMode(s.Substring(pos), hint);
                if (mode == QREncodeMode.Kanji)
                {
                    break;
                }
                if (mode == QREncodeMode.Numeric)
                {
                    int numericChunkPos = pos;
                    while (numericChunkPos < s.Length && isDigit((byte)s[numericChunkPos]))
                        numericChunkPos++;

                    int liNumeric = QRSpec.GetLengthIndicator(QREncodeMode.Numeric, m_symbolVersion);
                    int dif = estimateMode8BitCount(pos) + estimateNumericBitCount(numericChunkPos - pos) + 4 + liNumeric - estimateMode8BitCount(numericChunkPos);
                    if (dif < 0)
                        break;
                    else
                        pos = numericChunkPos;
                }
                else if (mode == QREncodeMode.AlphaNumeric)
                {
                    int alphaNumericChunkPos = pos;
                    while (alphaNumericChunkPos < s.Length && isAlphaNumeric((byte)s[alphaNumericChunkPos]))
                        alphaNumericChunkPos++;

                    int liAlphaNumeric = QRSpec.GetLengthIndicator(QREncodeMode.AlphaNumeric, m_symbolVersion);
                    int dif = estimateMode8BitCount(pos) + estimateAlphaNumericBitCount(alphaNumericChunkPos - pos) + 4 + liAlphaNumeric - estimateMode8BitCount(alphaNumericChunkPos);
                    if (dif < 0)
                        break;
                    else
                        pos = alphaNumericChunkPos;
                }
                else
                    pos++;
            }

            //byte[] bytes = Encoding.ASCII.GetBytes(s.Substring(0, pos));

            string ss = s.Substring(0, pos);

            byte[] bbytes = new byte[ss.Length];

            int index = 0;
            foreach (char c in ss.ToCharArray()){
                bbytes[index++] = (byte)c; 
            }

            bool ret = AppendData(QREncodeMode.Mode8, bbytes);
            if (!ret)
                return -1;

            return pos;
        }

        private int eatKanjiChunk(string s, QREncodeMode hint)
        {
            int chunkLength = 0;
            for (int i = 0; i < s.Length && identifyMode(s.Substring(i), hint) == QREncodeMode.Kanji; i++)
                chunkLength += 2;

            bool ret = AppendData(QREncodeMode.Kanji, Encoding.UTF8.GetBytes(s.Substring(0, chunkLength)));
            if (!ret)
                return -1;

            return chunkLength;
        }

        private bool splitString(string s, QREncodeMode hint)
        {
            if (s.Length == 0)
                return true;

            QREncodeMode mode = identifyMode(s, hint);
            int length = 0;

            if (mode == QREncodeMode.Numeric)
                length = eatNumericChunk(s, hint);
            else if (mode == QREncodeMode.AlphaNumeric)
                length = eatAlphaNumericChunk(s, hint);
            else if (mode == QREncodeMode.Kanji && hint == QREncodeMode.Kanji)
                length = eatKanjiChunk(s, hint);
            else
                length = eatMode8Chunk(s, hint);

            if (length == 0)
                return true;

            if (length < 0)
                return false;

            return splitString(s.Substring(length), hint);
        }

        private static string toUpper(string str, QREncodeMode hint)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < str.Length; )
            {
                QREncodeMode mode = identifyMode(str.Substring(i), hint);
                if (mode == QREncodeMode.Kanji)
                {
                    sb.Append(str[i]);
                    sb.Append(str[i + 1]);
                    i += 2;
                }
                else
                {
                    if (str[i] >= 'a' && str[i] <= 'z')
                        sb.Append((char)((int)str[i] - 32));
                    else
                        sb.Append(str[i]);

                    i++;
                }
            }

            return sb.ToString();
        }

        private static bool isDigit(byte c)
        {
            return ((byte)(c - '0') < 10);
        }

        private static bool isAlphaNumeric(byte c)
        {
            return (getValueForAlphaNumeric(c) >= 0);
        }
    }
}
