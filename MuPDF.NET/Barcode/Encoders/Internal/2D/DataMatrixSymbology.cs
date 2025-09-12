/**************************************************
 *
 *
 *
 *
**************************************************/

/*
Sample online barcode generator:
http://invx.com/code/?code=A%C3%81BC%C4%8CD%C4%8EE%C3%89%C4%9AFGHChI%C3%8DJKLMN%C5%87O%C3%93PQR%C5%98S%C5%A0T%C5%A4U%C3%9A%C5%AEVWXY%C3%9DZ%C5%BD&fg=%23000000&bg=%23ffffff&height=&width=
*/

using System;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using DataMatrix symbology. A Data Matrix code is a 
    /// two-dimensional matrix barcode consisting of black and white square 
    /// modules arranged in either a square or rectangular pattern. The 
    /// information to be encoded can be text or raw data. Usual data size 
    /// is from a few bytes up to 2 kilobytes. The length of the encoded 
    /// data depends on the symbol dimension used.
    /// </summary>
    class DataMatrixSymbology : SymbologyDrawing2D
    {
        class Matrix
        {
            public Matrix(int totalHeight, int totalWidth, int regionHeight,
                int regionWidth, int totalDataCWCount, int regionDataCWCount,
                int regionReedSolomonCWCount)
            {
                m_totalHeight = totalHeight;
                m_totalWidth = totalWidth;
                m_regionHeight = regionHeight;
                m_regionWidth = regionWidth;
                m_totalDataCWCount = totalDataCWCount;
                m_regionDataCWCount = regionDataCWCount;
                m_regionReedSolomonCWCount = regionReedSolomonCWCount;
            }

            public int m_totalHeight;
            public int m_totalWidth;
            public int m_regionHeight;
            public int m_regionWidth;
            public int m_totalDataCWCount;
            public int m_regionDataCWCount;
            public int m_regionReedSolomonCWCount;
        };

        private static Matrix[] m_matrices = new Matrix[] 
        {
            new Matrix(10, 10, 10, 10, 3, 3, 5),
            new Matrix(12, 12, 12, 12, 5, 5, 7),
            new Matrix(8, 18, 8, 18, 5, 5, 7),
            new Matrix(14, 14, 14, 14, 8, 8, 10),
            new Matrix(8, 32, 8, 16, 10, 10, 11),
            new Matrix(16, 16, 16, 16, 12, 12, 12),
            new Matrix(12, 26, 12, 26, 16, 16, 14),
            new Matrix(18, 18, 18, 18, 18, 18, 14),
            new Matrix(20, 20, 20, 20, 22, 22, 18),
            new Matrix(12, 36, 12, 18, 22, 22, 18),
            new Matrix(22, 22, 22, 22, 30, 30, 20),
            new Matrix(16, 36, 16, 18, 32, 32, 24),
            new Matrix(24, 24, 24, 24, 36, 36, 24),
            new Matrix(26, 26, 26, 26, 44, 44, 28),
            new Matrix(16, 48, 16, 24, 49, 49, 28),
            new Matrix(32, 32, 16, 16, 62, 62, 36),
            new Matrix(36, 36, 18, 18, 86, 86, 42),
            new Matrix(40, 40, 20, 20, 114, 114, 48),
            new Matrix(44, 44, 22, 22, 144, 144, 56),
            new Matrix(48, 48, 24, 24, 174, 174, 68),
            new Matrix(52, 52, 26, 26, 204, 102, 42),
            new Matrix(64, 64, 16, 16, 280, 140, 56),
            new Matrix(72, 72, 18, 18, 368, 92, 36),
            new Matrix(80, 80, 20, 20, 456, 114, 48),
            new Matrix(88, 88, 22, 22, 576, 144, 56),
            new Matrix(96, 96, 24, 24, 696, 174, 68),
            new Matrix(104, 104, 26, 26, 816, 136, 56),
            new Matrix(120, 120, 20, 20, 1050, 175, 68),
            new Matrix(132, 132, 22, 22, 1304, 163, 62),
            new Matrix(144, 144, 24, 24, 1558, 156, 62)
        };

        private struct CompactionModesBlock
        {
            // number of bytes of source that can be encoded in a row 
            // at this point using this compaction mode
            public int source;

            // number of bytes of target generated encoding from 
            // this point to end if already in this encoding mode
            public int target;
        };

        private const int c_maxByteCount = 3116;

        protected byte[] m_data;
        private byte[,] m_encodedData;

        private int m_width;
        private int m_height;

        private DataMatrixCompactionMode m_compactionMode = DataMatrixCompactionMode.Auto;

        private bool m_useNonSquareMatrices = true;

        public override string Value
        {
            get { return base.Value; }
            set
            {
                if (ValueIsValid(value, false))
                {
                    base.Value = value;
                }
                else
                {
                    throw new BarcodeException("Provided value can't be encoded into DataMatrix of specified size and compaction mode.");
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMatrixSymbology"/> class.
        /// </summary>
        public DataMatrixSymbology()
            : base(TrueSymbologyType.DataMatrix)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMatrixSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public DataMatrixSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.DataMatrix)
        {
        }

        /// <summary>
        /// Validates the value using Data Matrix symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            parseSize(Options.DataMatrixSize, out m_width, out m_height);
            if (Options.DataMatrixSize == DataMatrixSize.AutoSquareSize)
                m_useNonSquareMatrices = false;

            m_compactionMode = Options.DataMatrixCompactionMode;
            AdjustCompactionMode();

            Encoding tmpEncoding = Options.Encoding;
            
            m_data = tmpEncoding.GetBytes(value);

            try
            {
                if (m_data.Length != 0)
                    encodeData();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void AdjustCompactionMode()
        {
            //auto switch to binary mode for multibytes encodings (Unicode, etc)

            byte[] bytes = Options.Encoding.GetBytes(Value);
            if (bytes.Length > Value.Length) 
                m_compactionMode = DataMatrixCompactionMode.Binary;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Data Matrix symbology allows a maximum data size of 2,335 alphanumeric characters.\n";
        }

        /// <summary>
        /// Gets the barcode value encoded using Data Matrix symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using Data Matrix symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            if (forCaption)
                return Value;

            return "";
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>The encoding pattern for given character.</returns>
        protected override string getCharPattern(char c)
        {
            return "";
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            parseSize(Options.DataMatrixSize, out m_width, out m_height);
            if (Options.DataMatrixSize == DataMatrixSize.AutoSquareSize)
                m_useNonSquareMatrices = false;

            m_compactionMode = Options.DataMatrixCompactionMode;
            AdjustCompactionMode();

            m_data = getDataForEncoding();
            if (m_data.Length != 0)
                encodeData();

            Size drawingSize = new Size();
            if (m_encodedData != null)
            {
                int width = m_encodedData.GetLength(0);
                int height = m_encodedData.GetLength(1);

                int cellWidth = NarrowBarWidth;
                drawingSize.Width = width * cellWidth;
                drawingSize.Height = height * cellWidth;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (m_encodedData[x, height - y - 1] != 0)
                            m_rects.Add(new Rectangle(x * cellWidth, y * cellWidth, cellWidth, cellWidth));
                    }
                }
            }

            return drawingSize;
        }

        protected virtual DataMatrixCompactionMode CompactionMode
        {
            get { return m_compactionMode; }
        }

        protected virtual byte[] getDataForEncoding()
        {
            // get encoding used in options
            Encoding tmpEncoding = Options.Encoding;

            /*             
            // detect Unicode and auto switch to UTF-8 ?            
            
            bool UnicodeSymbolFound = !Utils.IsLatinISOEncodingOnly(Value);

            // if we are not with encoding UTF8
            // then we are forcing to use UTF8
            if (UnicodeSymbolFound && tmpEncoding != Encoding.UTF8)
            {
                // force encoding to UTF8
                tmpEncoding = Encoding.UTF8;
            }
            */       
                
            return tmpEncoding.GetBytes(Value);

            //byte[] bbytes = new byte[Value.Length];

            //int index = 0;
            //foreach (char c in Value.ToCharArray())
            //{
            //    bbytes[index++] = (byte)c;
            //}
            //return bbytes;
        }        

        private static void parseSize(DataMatrixSize size, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (size != DataMatrixSize.AutoSize && size != DataMatrixSize.AutoSquareSize)
            {
                string[] sizes = size.ToString().Substring(4).Split(new char[] { 'x' });
                width = Convert.ToInt32(sizes[1]);
                height = Convert.ToInt32(sizes[0]);
            }
        }

        private Matrix getWorkingMatrix()
        {
            for (int i = 0; i < m_matrices.Length; i++)
            {
                if (m_matrices[i].m_totalWidth == m_width && m_matrices[i].m_totalHeight == m_height)
                    return m_matrices[i];
            }

            throw new BarcodeException("Too much data for " + m_width + "x" + m_height + " barcode");
        }

        private void encodeData()
        {
            Matrix matrix = null;
            DataMatrixCompactionMode[] compactionModes = getDefaultCompactionModes();
            if (m_width != 0)
                matrix = getMatrixAndCompactionForGivenSize(ref compactionModes);
            else
                matrix = findSuitableMatrixAndCompaction(ref compactionModes);

            if (matrix == null)
                throw new BarcodeException("Cannot encode this data. Possibly size is incorrect");

            byte[] codewords = produceCodewords(compactionModes, matrix.m_totalDataCWCount);
            if (codewords == null)
                throw new BarcodeException("Too much data for " + m_width + "x" + m_height + " barcode");

            // Last paramter controls if we should invert first 2 and last 2 bytes in RS codewords
            // for 144x144 see #159 (in Decoders) and #686 (in BarCode SDK)
            addReedSolomonCodewords(ref codewords, matrix, Options.DataMatrixAlternativeReedSolomonCorrectionFor144x144Size);

            produceEncodedData(matrix, codewords);
        }

        private void produceEncodedData(Matrix matrix, byte[] codewords)
        {
            int columnCount = m_width - 2 * (m_width / matrix.m_regionWidth);
            int rowCount = m_height - 2 * (m_height / matrix.m_regionHeight);
            int[] places = new int[columnCount * rowCount];
            calculatePlacement(places, rowCount, columnCount);

            m_encodedData = new byte[m_width, m_height];

            for (int row = 0; row < m_height; row += matrix.m_regionHeight)
            {
                for (int column = 0; column < m_width; column++)
                    m_encodedData[column, row] = 1;

                for (int column = 0; column < m_width; column += 2)
                    m_encodedData[column, row + matrix.m_regionHeight - 1] = 1;
            }

            for (int column = 0; column < m_width; column += matrix.m_regionWidth)
            {
                for (int row = 0; row < m_height; row++)
                    m_encodedData[column, row] = 1;

                for (int row = 0; row < m_height; row += 2)
                    m_encodedData[column + matrix.m_regionWidth - 1, row] = 1;
            }

            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    int v = places[(rowCount - row - 1) * columnCount + column];
                    if (v == 1 || v > 7 && (codewords[(v >> 3) - 1] & (1 << (v & 7))) != 0)
                        m_encodedData[1 + column + 2 * (column / (matrix.m_regionWidth - 2)), 1 + row + 2 * (row / (matrix.m_regionHeight - 2))] = 1;
                }
            }
        }

        private Matrix findSuitableMatrixAndCompaction(ref DataMatrixCompactionMode[] compactionModes)
        {
            int notUsedLength = 0;
            if (compactionModes == null)
                compactionModes = buildCompactionModes(ref notUsedLength, true);

            Matrix matrix = null;
            if (compactionModes != null)
            {
                matrix = findSuitableMatrixForGivenCompaction(compactionModes);
            }
            else
            {
                int len = 0;
                compactionModes = buildCompactionModes(ref len, true);
                for (int i = 0; i < m_matrices.Length; i++)
                {
                    if (m_matrices[i].m_totalDataCWCount == len)
                    {
                        matrix = m_matrices[i];
                        break;
                    }
                }

                if (compactionModes != null && matrix == null)
                {
                    // try once more and do not require an exact fit
                    compactionModes = buildCompactionModes(ref len, false);
                    for (int i = 0; i < m_matrices.Length; i++)
                    {
                        if (m_matrices[i].m_totalDataCWCount < len)
                        {
                            matrix = m_matrices[i];
                            break;
                        }
                    }
                }
            }

            if (matrix == null)
                throw new BarcodeException("Cannot find suitable size, too much data to encode");

            m_width = matrix.m_totalWidth;
            m_height = matrix.m_totalHeight;
            return matrix;
        }

        private Matrix findSuitableMatrixForGivenCompaction(DataMatrixCompactionMode[] compactionModes)
        {
            for (int i = 0; i < m_matrices.Length; i++)
            {
                if (!m_useNonSquareMatrices)
                {
                    if (m_matrices[i].m_totalHeight != m_matrices[i].m_totalWidth)
                        continue;
                }

                byte[] codewords = produceCodewords(compactionModes, m_matrices[i].m_totalDataCWCount);
                if (codewords != null)
                    return m_matrices[i];
            }

            return null;
        }

        private Matrix getMatrixAndCompactionForGivenSize(ref DataMatrixCompactionMode[] compactionModes)
        {
            Matrix matrix = getWorkingMatrix();
            if (compactionModes == null)
            {
                int len = 0;
                compactionModes = buildCompactionModes(ref len, true);
                if (compactionModes != null && len != matrix.m_totalDataCWCount)
                {
                    // try once more and do not require an exact fit
                    compactionModes = buildCompactionModes(ref len, false);
                    if (len > matrix.m_totalDataCWCount)
                        throw new BarcodeException("Too much data for " + m_width + "x" + m_height + " barcode");
                }
            }

            return matrix;
        }

        protected virtual DataMatrixCompactionMode[] getDefaultCompactionModes()
        {
            DataMatrixCompactionMode[] compactionModes = null;
            if (CompactionMode != DataMatrixCompactionMode.Auto)
            {
                compactionModes = new DataMatrixCompactionMode[m_data.Length];
                for (int i = 0; i < compactionModes.Length; i++)
                    compactionModes[i] = CompactionMode;
            }

            return compactionModes;
        }

        private DataMatrixCompactionMode[] buildCompactionModes(ref int lenp, bool exact)
        {
            if (m_data.Length == 0 || m_data.Length > c_maxByteCount)
                return null;

            CompactionModesBlock[,] blocks = new CompactionModesBlock[m_data.Length + 1, CompactionModeLength];
            int pos = m_data.Length;
            while ((pos--) > 0)
            {
                tryAsciiMode(pos, blocks);
                tryC40Mode(pos, blocks, exact);
                tryTextMode(pos, blocks, exact);
                tryX12Mode(pos, blocks, exact);
                tryEdifactMode(pos, blocks, exact);
                tryBinaryMode(pos, blocks);
            }

            return postprocessCompactionBlocks(blocks, ref lenp);
        }

        private DataMatrixCompactionMode[] postprocessCompactionBlocks(CompactionModesBlock[,] blocks, ref int lenp)
        {
            DataMatrixCompactionMode[] compactionModes = new DataMatrixCompactionMode[m_data.Length + 1];
            DataMatrixCompactionMode currentMode = DataMatrixCompactionMode.Ascii;
            int pos = 0;
            while (pos < m_data.Length)
            {
                int m = 0;
                DataMatrixCompactionMode mode = DataMatrixCompactionMode.Ascii;
                for (DataMatrixCompactionMode i = DataMatrixCompactionMode.Ascii; i < LastCompactionMode; i++)
                {
                    int t = blocks[pos, (int)i].target + switchCost(currentMode, i);
                    if (blocks[pos, (int)i].target != 0 && (t < m || t == m && i == currentMode || m == 0))
                    {
                        mode = i;
                        m = t;
                    }
                }

                currentMode = mode;
                m = blocks[pos, (int)mode].source;

                if (pos == 0 && lenp != 0)
                    lenp = blocks[pos, (int)mode].target;

                while (pos < m_data.Length && (m--) != 0)
                    compactionModes[pos++] = (DataMatrixCompactionMode)mode;
            }

            compactionModes[pos] = DataMatrixCompactionMode.Ascii;
            return compactionModes;
        }

        private static void tryBinaryMode(int pos, CompactionModesBlock[,] blocks)
        {
            int bl = 0;
            DataMatrixCompactionMode b = DataMatrixCompactionMode.Ascii;
            for (DataMatrixCompactionMode e = DataMatrixCompactionMode.Ascii; e < LastCompactionMode; e++)
            {
                int extra = 0;
                if (e == DataMatrixCompactionMode.Binary && blocks[pos + 1, (int)e].target == 249)
                    extra = 1;

                int t = blocks[pos + 1, (int)e].target + switchCost(DataMatrixCompactionMode.Binary, e) + extra;
                if (blocks[pos + 1, (int)e].target != 0 && (t < bl || bl == 0))
                {
                    bl = t;
                    b = e;
                }
            }

            blocks[pos, (int)DataMatrixCompactionMode.Binary].target = bl + 1;
            blocks[pos, (int)DataMatrixCompactionMode.Binary].source = 1;
            if (bl != 0 && b == DataMatrixCompactionMode.Binary)
                blocks[pos, (int)b].source += blocks[pos + 1, (int)b].source;
        }

        private void tryEdifactMode(int pos, CompactionModesBlock[,] enc, bool exact)
        {
            int bl = 0;
            if (m_data[pos] >= 32 && m_data[pos] <= 94)
            {
                // can encode 1
                int bs = 0;
                DataMatrixCompactionMode b = DataMatrixCompactionMode.Ascii;
                if (pos + 1 == m_data.Length && (bl == 0 || bl < 2))
                {
                    bl = 2;
                    bs = 1;
                }
                else
                {
                    for (DataMatrixCompactionMode e = DataMatrixCompactionMode.Ascii; e < LastCompactionMode; e++)
                    {
                        int t = 2 + enc[pos + 1, (int)e].target + switchCost(DataMatrixCompactionMode.Ascii, e);
                        if (e != DataMatrixCompactionMode.Edifact && enc[pos + 1, (int)e].target != 0 && (t < bl || bl == 0))
                        {
                            bs = 1;
                            bl = t;
                            b = e;
                        }
                    }
                }

                if (pos + 1 < m_data.Length && m_data[pos + 1] >= 32 && m_data[pos + 1] <= 94)
                {
                    // can encode 2
                    if (pos + 2 == m_data.Length && (bl == 0 || bl < 2))
                    {
                        bl = 3;
                        bs = 2;
                    }
                    else
                    {
                        for (DataMatrixCompactionMode e = DataMatrixCompactionMode.Ascii; e < LastCompactionMode; e++)
                        {
                            int t = 3 + enc[pos + 2, (int)e].target + switchCost(DataMatrixCompactionMode.Ascii, e);
                            if (e != DataMatrixCompactionMode.Edifact && enc[pos + 2, (int)e].target != 0 && (t < bl || bl == 0))
                            {
                                bs = 2;
                                bl = t;
                                b = e;
                            }
                        }
                    }

                    if (pos + 2 < m_data.Length && m_data[pos + 2] >= 32 && m_data[pos + 2] <= 94)
                    {
                        // can encode 3
                        if (pos + 3 == m_data.Length && (bl == 0 || bl < 3))
                        {
                            bl = 3;
                            bs = 3;
                        }
                        else
                        {
                            for (DataMatrixCompactionMode e = DataMatrixCompactionMode.Ascii; e < LastCompactionMode; e++)
                            {
                                int t = 3 + enc[pos + 3, (int)e].target + switchCost(DataMatrixCompactionMode.Ascii, e);
                                if (e != DataMatrixCompactionMode.Edifact && enc[pos + 3, (int)e].target != 0 && (t < bl || bl == 0))
                                {
                                    bs = 3;
                                    bl = t;
                                    b = e;
                                }
                            }
                        }

                        if (pos + 4 < m_data.Length && m_data[pos + 3] >= 32 && m_data[pos + 3] <= 94)
                        {
                            // can encode 4
                            if (pos + 4 == m_data.Length && (bl == 0 || bl < 3))
                            {
                                bl = 3;
                                bs = (char)4;
                            }
                            else
                            {
                                for (DataMatrixCompactionMode e = DataMatrixCompactionMode.Ascii; e < LastCompactionMode; e++)
                                {
                                    int t = 3 + enc[pos + 4, (int)e].target + switchCost(DataMatrixCompactionMode.Edifact, e);
                                    if (enc[pos + 4, (int)e].target != 0 && (t < bl || bl == 0))
                                    {
                                        bs = (char)4;
                                        bl = t;
                                        b = e;
                                    }
                                }

                                int temp = 3 + enc[pos + 4, (int)DataMatrixCompactionMode.Ascii].target;
                                if (exact && enc[pos + 4, (int)DataMatrixCompactionMode.Ascii].target != 0 && enc[pos + 4, (int)DataMatrixCompactionMode.Ascii].target <= 2 && temp < bl)
                                {
                                    // special case, switch to ASCII for last 1 of two bytes
                                    bs = 4;
                                    bl = temp;
                                    b = DataMatrixCompactionMode.Ascii;
                                }
                            }
                        }
                    }
                }

                enc[pos, (int)DataMatrixCompactionMode.Edifact].target = bl;
                enc[pos, (int)DataMatrixCompactionMode.Edifact].source = bs;
                if (bl != 0 && b == DataMatrixCompactionMode.Edifact)
                    enc[pos, (int)b].source += enc[pos + bs, (int)b].source;
            }
        }

        private void tryX12Mode(int pos, CompactionModesBlock[,] enc, bool exact)
        {
            int sub = 0;
            int tl = 0;
            int sl = 0;
            do
            {
                byte c = m_data[pos + sl++];
                if (c != 13 && c != '*' && c != '>' && c != ' ' && !isDigit(c) && !isUpper(c))
                {
                    sl = 0;
                    break;
                }

                sub++;
                while (sub >= 3)
                {
                    sub -= 3;
                    tl += 2;
                }

            } while (sub != 0 && pos + sl < m_data.Length);

            if (sub == 0 && sl != 0)
            {
                // can encode X12
                int bl = 0;
                DataMatrixCompactionMode b = DataMatrixCompactionMode.Ascii;
                if (pos + sl < m_data.Length)
                {
                    for (DataMatrixCompactionMode e = DataMatrixCompactionMode.Ascii; e < LastCompactionMode; e++)
                    {
                        int t = enc[pos + sl, (int)e].target + switchCost(DataMatrixCompactionMode.X12, e);
                        if (enc[pos + sl, (int)e].target != 0 && (t < bl || bl == 0))
                        {
                            bl = t;
                            b = e;
                        }
                    }
                }

                if (exact && enc[pos + sl, (int)DataMatrixCompactionMode.Ascii].target == 1 && 1 < bl)
                {
                    // special case, switch to ASCII for last bytes
                    bl = 1;
                    b = DataMatrixCompactionMode.Ascii;
                }

                enc[pos, (int)DataMatrixCompactionMode.X12].target = tl + bl;
                enc[pos, (int)DataMatrixCompactionMode.X12].source = sl;
                if (bl != 0 && b == DataMatrixCompactionMode.X12)
                    enc[pos, (int)b].source += enc[pos + sl, (int)b].source;
            }
        }

        private void tryTextMode(int pos, CompactionModesBlock[,] enc, bool exact)
        {
            int sub = 0;
            int tl = 0;
            int sl = 0;
            do
            {
                byte c = m_data[pos + sl++];
                if ((c & 0x80) != 0)
                {
                    // shift + upper
                    sub += 2;
                    c &= 0x7F;
                }

                if (c != ' ' && !isDigit(c) && !isLower(c))
                    sub++;	// shift

                sub++;
                while (sub >= 3)
                {
                    sub -= 3;
                    tl += 2;
                }

            } while (sub != 0 && (pos + sl < m_data.Length));

            if (exact && sub == 2 && pos + sl == m_data.Length)
            {
                // special case, can encode last block with shift 0 at end
                sub = 0;
                tl += 2;
            }

            if (sub == 0 && sl != 0)
            {
                // can encode Text
                int bl = 0;
                DataMatrixCompactionMode b = DataMatrixCompactionMode.Ascii;
                if (pos + sl < m_data.Length)
                {
                    for (DataMatrixCompactionMode e = DataMatrixCompactionMode.Ascii; e < LastCompactionMode; e++)
                    {
                        int t = enc[pos + sl, (int)e].target + switchCost(DataMatrixCompactionMode.Text, e);
                        if (enc[pos + sl, (int)e].target != 0 && (t < bl || bl == 0))
                        {
                            bl = t;
                            b = e;
                        }
                    }
                }

                if (exact && enc[pos + sl, (int)DataMatrixCompactionMode.Ascii].target == 1 && 1 < bl)
                {
                    // special case, switch to ASCII for last bytes
                    bl = 1;
                    b = DataMatrixCompactionMode.Ascii;
                }

                enc[pos, (int)DataMatrixCompactionMode.Text].target = (short)(tl + bl);
                enc[pos, (int)DataMatrixCompactionMode.Text].source = (short)sl;
                if (bl != 0 && b == DataMatrixCompactionMode.Text)
                    enc[pos, (int)b].source += enc[pos + sl, (int)b].source;
            }
        }

        private void tryC40Mode(int pos, CompactionModesBlock[,] enc, bool exact)
        {
            int sub = 0;
            int tl = 0;
            int sl = 0;

            do
            {
                byte c = m_data[pos + sl];
                sl++;

                if ((c & 0x80) != 0)
                {
                    // shift + upper
                    sub += 2;
                    c &= 0x7F;
                }

                if (c != ' ' && !isDigit(c) && !isUpper(c))
                {
                    // shift
                    sub++;
                }

                sub++;
                while (sub >= 3)
                {
                    sub -= 3;
                    tl += 2;
                }

            } while (sub != 0 && (pos + sl < m_data.Length));

            if (exact && sub == 2 && pos + sl == m_data.Length)
            {
                // special case, can encode last block with shift 0 at end
                sub = 0;
                tl += 2;
            }

            if (sub == 0)
            {
                // can encode C40
                int bl = 0;
                DataMatrixCompactionMode b = DataMatrixCompactionMode.Ascii;
                if (pos + sl < m_data.Length)
                {
                    for (DataMatrixCompactionMode e = DataMatrixCompactionMode.Ascii; e < LastCompactionMode; e++)
                    {
                        int t = enc[pos + sl, (int)e].target + switchCost(DataMatrixCompactionMode.C40, e);
                        if (enc[pos + sl, (int)e].target != 0 && (t < bl || bl == 0))
                        {
                            bl = t;
                            b = e;
                        }
                    }
                }

                if (exact && enc[pos + sl, (int)DataMatrixCompactionMode.Ascii].target == 1 && 1 < bl)
                {
                    // special case, switch to ASCII for last bytes
                    bl = 1;
                    b = DataMatrixCompactionMode.Ascii;
                }
                enc[pos, (int)DataMatrixCompactionMode.C40].target = (short)(tl + bl);
                enc[pos, (int)DataMatrixCompactionMode.C40].source = (short)sl;
                if (bl != 0 && b == DataMatrixCompactionMode.C40)
                    enc[pos, (int)b].source += enc[pos + sl, (int)b].source;
            }
        }

        private void tryAsciiMode(int pos, CompactionModesBlock[,] enc)
        {
            int sl = 1;
            int tl = 1;
            if (isDigit(m_data[pos]) && pos + 1 < m_data.Length && isDigit(m_data[pos + 1]))
            {
                // double digit
                sl = 2;
            }
            else if ((m_data[pos] & 0x80) != 0)
            {
                // upper shift
                tl = 2;
            }

            int bl = 0;
            DataMatrixCompactionMode b = DataMatrixCompactionMode.Ascii;
            if (pos + sl < m_data.Length)
            {
                for (DataMatrixCompactionMode e = DataMatrixCompactionMode.Ascii; e < LastCompactionMode; e++)
                {
                    int t = enc[pos + sl, (int)e].target + switchCost(DataMatrixCompactionMode.Ascii, e);
                    if (enc[pos + sl, (int)e].target != 0 && (t < bl || bl == 0))
                    {
                        bl = t;
                        b = e;
                    }
                }
            }

            enc[pos, (int)DataMatrixCompactionMode.Ascii].target = tl + bl;
            enc[pos, (int)DataMatrixCompactionMode.Ascii].source = sl;

            if (bl != 0 && b == DataMatrixCompactionMode.Ascii)
                enc[pos, (int)b].source += enc[pos + sl, (int)b].source;
        }

        protected virtual void initCodeWords(byte[] codewords, ref int codewordIndex)
        {
        }

        private byte[] produceCodewords(DataMatrixCompactionMode[] compactionModes, int codewordCountLimit)
        {
            byte[] codewords = new byte[codewordCountLimit];
            int codewordIndex = 0;
            initCodeWords(codewords, ref codewordIndex);

            int dataIndex = 0;
            DataMatrixCompactionMode mode = DataMatrixCompactionMode.Ascii;
            while (dataIndex < m_data.Length && codewordIndex < codewordCountLimit)
            {
                DataMatrixCompactionMode newMode = mode;

                // check if we have C30, Text or X12 mode 
                // and then if we need to force switch to ASCII:
                // if we are in C40 and Text modes and we have 1 single character to encode then we should use ASCII
                // if we are in x12 mode and we have 2 characters to encode then we should use ASCII mode to encode them
                if (codewordCountLimit - codewordIndex <= 1 &&
                    (mode == DataMatrixCompactionMode.C40 || mode == DataMatrixCompactionMode.Text) ||
                    codewordCountLimit - codewordIndex <= 2 && mode == DataMatrixCompactionMode.X12)
                {
                    // revert to ASCII
                    mode = DataMatrixCompactionMode.Ascii;
                }

                newMode = compactionModes[dataIndex];

                switch (newMode)
                {
                    case DataMatrixCompactionMode.C40:
                    case DataMatrixCompactionMode.Text:
                    case DataMatrixCompactionMode.X12:
                        produceTextBasedCodewords(ref mode, newMode, ref dataIndex, ref codewordIndex, codewordCountLimit, codewords);
                        break;

                    case DataMatrixCompactionMode.Edifact:
                        produceEdifactCodewords(ref mode, newMode, ref dataIndex, ref codewordIndex, compactionModes, codewordCountLimit, codewords);
                        break;

                    case DataMatrixCompactionMode.Ascii:
                        produceAsciiCodewords(ref mode, newMode, ref dataIndex, ref codewordIndex, codewordCountLimit, codewords);
                        break;

                    case DataMatrixCompactionMode.Binary:
                        produceBinaryCodewords(ref mode, newMode, ref dataIndex, ref codewordIndex, compactionModes, codewordCountLimit, codewords);
                        break;

                    default:
                        throw new BarcodeException("Unknown encoding: " + newMode);
                }
            }

            // close current chunk of codeword and add padding codewords at the end
            addEscapeAndPaddingCodewords(mode, ref codewordIndex, codewordCountLimit, codewords);

            if (codewordIndex > codewordCountLimit || dataIndex < m_data.Length)
            {
                // did not fit
                return null;
            }

            return codewords;
        }

        private static void addEscapeAndPaddingCodewords(DataMatrixCompactionMode mode, ref int codewordIndex, int codewordCountLimit, byte[] codewords)
        {
            if (codewordIndex < codewordCountLimit)
            {
                if (mode == DataMatrixCompactionMode.Ascii)
                    // ASCII mode padding
                    codewords[codewordIndex++] = 129;	// add padding character in ASCII mode
                else
                {
                    // non ASCII mode so we are adding the switch to ASCII mode 
                    if (mode == DataMatrixCompactionMode.C40 || mode == DataMatrixCompactionMode.X12 || mode == DataMatrixCompactionMode.Text)
                        codewords[codewordIndex++] = 254;	// escape X12/C40/Text
                    else
                        codewords[codewordIndex++] = 254;	// escape EDIFACT - we are adding 254 switch to ASCII as 0x7c switch causes some unpredictable response from decoders

                    // as we have switched to ANSI then we should add padding character 129
                    if (codewordIndex < codewordCountLimit)
                        codewords[codewordIndex++] = 129; // padding character in ASCII mode
                }
            }

            // if we still need to add more padding then we are adding according 
            // to the specification 129 + some random (see the spec)
            while (codewordIndex < codewordCountLimit)
            {
                // more padding
                int v = 129 + (((codewordIndex + 1) * 149) % 253) + 1;
                if (v > 254)
                    v -= 254;
                codewords[codewordIndex++] = (byte)v;
            }
        }

        private void produceBinaryCodewords(ref DataMatrixCompactionMode mode, DataMatrixCompactionMode newMode, ref int dataIndex, ref int codewordIndex, DataMatrixCompactionMode[] compactionModes, int codewordCountLimit, byte[] codewords)
        {

            // now perform check if we have enough codewords for this encoding method            
            // we are in Binary mode: we are encoding 1 byte into 1 codeword
            // and we need to add the starting marker + 2 bytes for the length
            bool enoughCW = (m_data.Length + 3) <= codewordCountLimit; // if we have enough codewords

            // return enlarged codewordindex so this matrix will wail and 
            // the calling code will try the next sizes matrix
            if (!enoughCW)
            {
                codewordIndex = codewordCountLimit + 1;
                return;
            }

            try
            {

                // close previous chunk of data if needed
                if (mode != newMode)
                {
                    // check if we are not at the beginning and 
                    // need to switch the mode
                    if (codewordIndex > 0)
                    {
                        if (
                            mode == DataMatrixCompactionMode.C40 || 
                            mode == DataMatrixCompactionMode.Text || 
                            mode == DataMatrixCompactionMode.X12
                            )
                            codewords[codewordIndex++] = 254;	// escape C40/text/X12
                        else if (mode == DataMatrixCompactionMode.Edifact)
                            codewords[codewordIndex++] = 254;	// escape EDIFACT
                    }

                    // set the codeword to open BINARY mode 
                    codewords[codewordIndex++] = 231;

                }

                // set mode to binary
                mode = DataMatrixCompactionMode.Binary;
                
                // length of the data to encode
                int lengthToEncode = 0;

                // calculate length of data to encode
                if (compactionModes != null)
                {
                    for (int p = dataIndex; p < m_data.Length && compactionModes[p] == DataMatrixCompactionMode.Binary; p++)
                        lengthToEncode++;
                }

                // write the length of the data to encode
                // see the specifcation
                if (lengthToEncode < 250)
                {
                    // if length < 250 then encoding using the length according the 255 algorithm 
                    codewords[codewordIndex] = (byte)(lengthToEncode + (((codewordIndex + 1) * 149) % 255) + 1);
                    codewordIndex++;
                }
                else
                {
                    // if length > 250 then encoding according to the rule described
                    // in the spec
                    codewords[codewordIndex++] = (byte)(249 + (lengthToEncode / 250));
                    codewords[codewordIndex++] = (byte)(lengthToEncode % 250);
                }

                while ((lengthToEncode--) != 0 && codewordIndex < codewordCountLimit)
                {
                    // encoding according to the specification of Base 256 encoding using The 255-state algorithm (see the spec)
                    codewords[codewordIndex] = (byte)(m_data[dataIndex++] + (((codewordIndex + 1) * 149) % 255) + 1);
                    codewordIndex++;
                }

            }
            catch { 
                // hide exception as it means the mode do not fit
            }
        }

        protected virtual void produceAsciiCodewords(ref DataMatrixCompactionMode mode, DataMatrixCompactionMode newMode, ref int dataIndex, ref int codewordIndex, int codewordCountLimit, byte[] codewords)
        {
            try
            {

                // switch to the new mode if needed
                if (mode != newMode)
                {
                    // close the previous chunk of data if needed                    
                    if (codewordIndex > 0)
                    {
                        if (mode == DataMatrixCompactionMode.C40 || mode == DataMatrixCompactionMode.Text || mode == DataMatrixCompactionMode.X12)
                            codewords[codewordIndex++] = 254;	// escape C40/text/X12
                        else
                            codewords[codewordIndex++] = 0x7C;	// escape EDIFACT
                    }
                }
                // set the mode to ASCII
                mode = DataMatrixCompactionMode.Ascii;
                if (m_data.Length - dataIndex >= 2 && isDigit(m_data[dataIndex]) && isDigit(m_data[dataIndex + 1]))
                {
                    codewords[codewordIndex++] = (byte)((m_data[dataIndex] - '0') * 10 + m_data[dataIndex + 1] - '0' + 130);
                    dataIndex += 2;
                }
                else if (m_data[dataIndex] > 127)
                {
                    codewords[codewordIndex++] = 235;
                    codewords[codewordIndex++] = (byte)(m_data[dataIndex++] - 127);
                }
                else
                    codewords[codewordIndex++] = (byte)(m_data[dataIndex++] + 1);
            }
            catch { 
             // hide exception as this will cause failing for this size and 
             // the code will try next size available
            }
        }

        /// <summary>
        /// checks if the string has lowercase characters
        /// we are using this for Edifact encoding as 
        /// </summary>
        /// <param name="str">input string to check</param>
        /// <returns></returns>
        private bool hasLowerCaseCharacters(ref byte[] str)
        {
            // lower case ASCII symbols are 97 to 122 (a-z)
            foreach (byte b in str)
            {
                if (b > 96 && b < 123)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Generates Edifact encoded 
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="newMode"></param>
        /// <param name="dataIndex"></param>
        /// <param name="codewordIndex"></param>
        /// <param name="compactionModes"></param>
        /// <param name="codewordCountLimit"></param>
        /// <param name="codewords"></param>
        private void produceEdifactCodewords(ref DataMatrixCompactionMode mode, DataMatrixCompactionMode newMode,
            ref int dataIndex, ref int codewordIndex, DataMatrixCompactionMode[] compactionModes, int codewordCountLimit, byte[] codewords)
        {

            // check if there are some lower case characters 
            // and throw exception if any as they are not allowed in Edifact mode
            if (hasLowerCaseCharacters(ref m_data))
                throw new BarcodeException("Cannot encode lowercase ASCII characters (a-z) with Edifact, use Auto, ANSI, Text or C40 method");

            // now perform check if we have enough codewords for this encoding method            
            // first we are checking if we have enough codewords
            // In Edifact mode 4 data characters are compacted in 3 CWs (1.33 compression rate)
            bool enoughCW = ((float)m_data.Length / codewordCountLimit) <= 1.4f; // or we have enough codewords for 1.5 compression more than codewords

            // return enlarged codewordindex so this matrix will wail and 
            // the calling code will try the next sizes matrix
            if (!enoughCW)
            {
                codewordIndex = codewordCountLimit + 1;
                return;
            }



            try
            {
                // add the mode switch if need to switch the mode
                if (mode != newMode)
                {
                    // close the previou chunk of data if needed                    
                    if (codewordIndex > 0)
                    {
                        // close the previous mode
                        if (mode == DataMatrixCompactionMode.C40 || mode == DataMatrixCompactionMode.Text || mode == DataMatrixCompactionMode.X12)
                            codewords[codewordIndex++] = 254;	// escape C40/text/X12
                        else
                            codewords[codewordIndex++] = 254; //0x7C;	// escape EDIFACT
                    }

                    // add adding the switch codeword to indicate beginning of Edifact mode
                    codewords[codewordIndex++] = 240;
                }

				// set the current mode to Edifact
                mode = DataMatrixCompactionMode.Edifact;

                // current position in the buffer
                int pos = 0;
                // buffer with 4 bytes which we are encode into codeword
                byte[] buffer = new byte[4];

                // run in loop to iterate through all input characters
                do
                {
                    // reset the buffer position

                    pos = 0;
                    // fill the bufer with input data (copying by 4 bytes or less if any)
                    while (dataIndex < m_data.Length && compactionModes[dataIndex] == DataMatrixCompactionMode.Edifact && pos < 4)
                        buffer[pos++] = m_data[dataIndex++];

                    // if we have empty slots in the buffer[]
                    // it means we finished with the source data
                    // and we should fill the reminding data
                    if (pos < 4)
                    {
                        // add "end of data" marker for Edifact, 31 (0x1F)
                        if (pos < 4)
                            buffer[pos++] = 31; // the same as 0x1F 

                        // fill the reminding slots with zero
                        while (pos < 4)
                            buffer[pos++] = 0;

                    }

                    // doing the encoding, each character is encoded by 6 bits
                    codewords[codewordIndex++] = (byte)((buffer[0] & 0x3F) << 2);
                    // here and below we are using ++ and -- to provide proper codewordIndex value when the code fails into the exception
                    codewordIndex--; 
                    // encoding next characters in the buffer
                    codewords[codewordIndex++] |= (byte)((buffer[1] & 0x30) >> 4);
                    codewords[codewordIndex++] = (byte)((buffer[1] & 0x0F) << 4);
                    codewordIndex--; 

                    if (pos == 2)
                        codewordIndex++;
                    else
                    {
                        codewords[codewordIndex++] |= (byte)((buffer[2] & 0x3C) >> 2);
                        codewords[codewordIndex++] = (byte)((buffer[2] & 0x03) << 6);
                        codewordIndex--;
                        codewords[codewordIndex++] |= (byte)(buffer[3] & 0x3F);
                    }
                }
                while (dataIndex < m_data.Length); // repeat until we have input data to encode

            }
            catch { 
                // suppress as current size do not fit
            }
        }

        /// <summary>
        /// Generates codewords from input data in C40, Text or x12 encoding modes
        /// </summary>
        /// <param name="mode">old mode</param>
        /// <param name="newMode">new mode (c40, text or x12)</param>
        /// <param name="dataIndex">index of the data to read from</param>
        /// <param name="codewordIndex">current index of codewords</param>
        /// <param name="codewordCountLimit">count of allowed codewords (for the current matrix)</param>
        /// <param name="codewords">codewords array to write into</param>
        private void produceTextBasedCodewords(ref DataMatrixCompactionMode mode, DataMatrixCompactionMode newMode,
            ref int dataIndex, ref int codewordIndex, int codewordCountLimit, byte[] codewords)
        {

            // x12 method is not able to encode lower case characters
            if (newMode == DataMatrixCompactionMode.X12 && hasLowerCaseCharacters(ref m_data))
                throw new BarcodeException("Cannot encode lowercase ASCII characters (a-z) with X12 method, use Auto, ANSI, Text or C40 method");

            // now perform check if we have enough codewords for this encoding method            
            // first we are checking if we have enough codewords
            // In Text, C40, x12 modes 3 data characters are compacted in 2 CWs (1.5 compression rate)
            bool enoughCW = (m_data.Length < 4 && codewordCountLimit <6) || // if we have 2-3 characters to encode and similar codewords then we may fit
                ((float)m_data.Length / codewordCountLimit) <= 1.5f; // or we have enough codewords for 1.5 compression more than codewords

            // return enlarged codewordindex so this matrix will wail and 
            // the calling code will try the next sizes matrix
            if (!enoughCW)
            {
                codewordIndex = codewordCountLimit + 1;
                return;
            }

            try
            {

                byte[] buffer = new byte[6];
                int pos = 0;

                string s2 = "!\"#$%&'()*+,-./:;<=>?@[\\]_";
                string s3 = "";
                string e = "";

                if (newMode == DataMatrixCompactionMode.C40)
                {
                    e = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    s3 = "`abcdefghijklmnopqrstuvwxyz{|}~\0177";
                }
                else if (newMode == DataMatrixCompactionMode.Text)
                {
                    e = " 0123456789abcdefghijklmnopqrstuvwxyz";
                    s3 = "`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~\0177";
                }
                else if (newMode == DataMatrixCompactionMode.X12)
                    e = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ\r*>";

                do
                {
                    char c = (char)m_data[dataIndex++];

                    //if (dataIndex < m_data.Length)
                    //{
                        if ((c & 0x80) != 0)
                        {
                            //if (newMode == DataMatrixCompactionMode.X12)
                              //  throw new BarcodeException("Cannot encode char 0x" + c + "X in X12");

                            c &= (char)0x7f;
                            buffer[pos++] = 1;
                            buffer[pos++] = 30;
                        }

                        int w_idx = e.IndexOf(c);
                        if (w_idx >= 0)
                            buffer[pos++] = (byte)((w_idx + 3) % 40); // +3 is because the specification uses first 3 indexes for control shift commands
                        else
                        {
                            //if (newMode == DataMatrixCompactionMode.X12)
                              //  throw new BarcodeException("Cannot encode char 0x" + c + " in X12");

                            if (c < 32)
                            {
                                // shift 1
                                buffer[pos++] = 0;
                                buffer[pos++] = (byte)c;
                            }
                            else
                            {
                                w_idx = s2.IndexOf(c);
                                if (w_idx >= 0)
                                {
                                    // shift 2
                                    buffer[pos++] = 1;
                                    buffer[pos++] = (byte)w_idx;
                                }
                                else
                                {
                                    w_idx = s3.IndexOf(c);

                                    if (w_idx >= 0)
                                    {
                                        buffer[pos++] = 2;
                                        buffer[pos++] = (byte)w_idx;
                                    }
                                    else
                                        throw new BarcodeException("Could not encode 0x" + c + "X, unsupported symbols passed");

                                }
                            }
                        }
                    //}


                    // as we are encoding every 3 characters into 1 (one) codeword
                    // and we may have 4 characters so need to properly encode 4th character 
                    // so we run the loop to make sure all characters were encoded into codewords
                    // this loop runs if we have 1 symbol left to encode!
                    do
                    {
                        // we are converting every 3 characters from the buffer[6] into codewords 
                        // so we are passing 2 times
                        // here we check if we are at the last character of the source string
                        // but but we are at the 4th or 5th position of the buffer: we have 2 bytes left
                        // so we have to pad these bytes with zero 
                        if (
                            (pos == 2 || pos == 5) && // if we encoded some characters already but left 1 the very last slot in buffer[]
                            dataIndex == m_data.Length
                            )
                            buffer[pos++] = 0; // shift 1 pad at end

                        // now encode into codewords

                        // 1. check if we have 1 byte left to encode (as we are running in the loop processing every 3 bytes from buffer[])
                        // so we check if we have
                        // so we should encode it as ASCII (see the specification on the remaingin bytes while using C40 mode)
                        if (
                                // if we are in C40 or Text mode and we have 1 character left
                                // then we should encode the very last character with ASCII
                                (
                                    (newMode == DataMatrixCompactionMode.C40 || newMode == DataMatrixCompactionMode.Text) &&
                                    dataIndex == m_data.Length &&  // we are working with the vert last character of the source string
                                    pos < 2 // we have 2 bytes left in the buffer[] (which we convert into codewords)
                                )
                                || // OR
                                // we are in x12 mode and we have 2 characters left (different from Text and C40 that C40/Text mode encodes the very last character with ANSI, x12 encodes 2 last characters)
                                // then we should encode the very last character with ASCII
                                (
                                    (newMode == DataMatrixCompactionMode.X12) && 
                                    dataIndex > m_data.Length-2 &&  // we are working with the vert last character of the source string
                                    pos < 2 // we have 2 bytes left in the buffer[] (which we convert into codewords)
                                )

                            ) 
                        {
                            // checking if we should add ANSI switch in C40 or Text mode
                            // we add ASCII switch if we have 2 codewords still left then we should add the ASCII mode switch codeword
                            // and we have 1 character to encode
                            if (
                                (newMode == DataMatrixCompactionMode.C40 || newMode == DataMatrixCompactionMode.Text) &&                                
                                (codewordIndex + 1 < codewordCountLimit)
                            )
                            {
                                    codewords[codewordIndex++] = 254;	// enter ASCII mode
                            }

                            // ELSE we if we are in x12 mode
                            // then we add ASCII switch if we have 3 codewords still left then we should add the ASCII mode switch codeword
                            // and we have 2 characters to encode 
                            else if (
                                (newMode == DataMatrixCompactionMode.X12) &&
                                (codewordIndex + 2 < codewordCountLimit)
                            )
                            {
                                codewords[codewordIndex++] = 254;	// enter ASCII mode
                            }

                            // otherwise we assume (according to the specification) that decoder treats 
                            // the vert last codeword as ASCII encoded (even without ASCII mode switch codeword)

                            // set the mode to ASCII
                            mode = DataMatrixCompactionMode.Ascii;

                            // "c" variable already stores the current character (i.e. c == m_data[dataIndex])
                            // encode this sigle character using the ASCII mode (see the spec for ASCII mode)
                            if ((byte)c > 127)
                            {
                                codewords[codewordIndex++] = 235;
                                codewords[codewordIndex++] = (byte)(c - 127);
                            }
                            else
                                codewords[codewordIndex++] = (byte)(c + 1);

                            // decrease the pos index as we just converted the single character into the codeword 
                            pos--;                            

                            // check if we have one more character to encode in ASCII mode 
                            // this may happen if we are in x12 mode

                            // "c" variable already stores the current character (i.e. c == m_data[dataIndex])
                            // encode this sigle character using the ASCII mode (see the spec for ASCII mode)
                            if (dataIndex < m_data.Length)
                            {
                                c = (char)m_data[dataIndex++];
                                if ((byte)c > 127)
                                {
                                    codewords[codewordIndex++] = 235;
                                    codewords[codewordIndex++] = (byte)(c - 127);
                                }
                                else
                                    codewords[codewordIndex++] = (byte)(c + 1);
                                
                                // decrease the pos index as we just converted the single character into the codeword 
                                pos--;
                            }
                        }
                        else // we have 3 or more bytes in the buffer[] so we are encoding the set of 3 symbols or more into 2 codewords pair using 
                        {
                            // process every 3 bytes from buffer[]
                            while (pos >= 3)
                            {
                                // encoding 3 bytes into 1 codewords (16 bit per each codeword) (see the spec)
                                int v = buffer[0] * 1600 + buffer[1] * 40 + buffer[2] + 1;

                                // adding the codeword to indicate the encoding mode switch if neccessary
                                if (mode != newMode)
                                {
                                    // check if we are not at the beginning and 
                                    // need to switch the mode
                                    if (codewordIndex > 0)
                                    {
                                        if (mode == DataMatrixCompactionMode.C40 || mode == DataMatrixCompactionMode.Text || mode == DataMatrixCompactionMode.X12)
                                            codewords[codewordIndex++] = 254;	// escape C40/text/X12
                                        else if (mode == DataMatrixCompactionMode.Edifact)
                                            codewords[codewordIndex++] = 254; //0x7C;	// escape EDIFACT
                                    }

                                    // and then indicate the beginning of the new mode
                                    if (newMode == DataMatrixCompactionMode.C40)
                                        codewords[codewordIndex++] = 230;
                                    else if (newMode == DataMatrixCompactionMode.Text)
                                        codewords[codewordIndex++] = 239;
                                    else if (newMode == DataMatrixCompactionMode.X12)
                                        codewords[codewordIndex++] = 238;

                                    mode = newMode;
                                }

                                // encoding codewords according to the specification (C40 mode)
                                codewords[codewordIndex++] = (byte)(v >> 8);
                                codewords[codewordIndex++] = (byte)(v & 0xFF);

                                // shift the pos index in the buffer[] as we just encoded 3 bytes from the buffer
                                pos -= 3;

                                // copy  next 3 bytes into the beginning of the buffer[]
                                buffer[0] = buffer[3];
                                buffer[1] = buffer[4];
                                buffer[2] = buffer[5];

                                // clear old 3 bytes (just in case although we have shifted the pos index so this should be ok)
                                buffer[3] = 0;
                                buffer[4] = 0;
                                buffer[5] = 0;


                            } // while still have 3 or more bytes to process in the buffer[]
                        } // else block (for the case when we have 3 or more bytes in the buffer[] to process)

                    }
                    while (pos == 1 && dataIndex > m_data.Length - 1); // repeat if we still have 1 byte left unprocessed in buffer[]

                }
                while (pos >-1 && dataIndex < m_data.Length); // repeat while we have source characters to process

            }
            catch { 
             // return nothing as exception means the barcode not fitting into current size
            }
        }

        private static void addReedSolomonCodewords(
            ref byte[] codewords, 
            Matrix matrix,
            bool exchangeFirstTwoAndLastBytesInRSCodes // used for 144x144 only in the special mode            
            )
        {
            // according to Datamatrix specification
            // codewords are located in the following way inside blocks:
            // exmple with 4 blocks:
            // DATA CODEWORDS: 1 2 3 4 5 6 ... 365 366 367 368 ERROR CORRECTION: 1 2 3 4 5 6 .. 141 142 143 144
            // BLOCK 1: data: 1 5 .. 361 365       error corr: 1 5 .. 137 141
            // BLOCK 2: data:   2 6 .. 362 366     error corr:   2 6 .. 138 142
            // BLOCK 3: data:     3 7 .. 363 367   error corr:     3 7 .. 139 143 
            // BLOCK 4: data:       4 8 .. 364 368 error corr:       4 8 .. 140 144

            int blockCount = (matrix.m_totalDataCWCount + 2) / matrix.m_regionDataCWCount;

            // increase codewords array to accommodate Reed-Solomon CWs
            byte[] newCodewords = new byte[codewords.Length + blockCount * matrix.m_regionReedSolomonCWCount];
            Array.Copy(codewords, newCodewords, codewords.Length);
            codewords = newCodewords;

            ReedSolomon rsObj = new ReedSolomon();
            rsObj.Init(0x12d);
            rsObj.InitCode(matrix.m_regionReedSolomonCWCount, 1);
            for (int b = 0; b < blockCount; b++)
            {
                byte[] buf = new byte[256];
                int position = 0;
                for (int n = b; n < matrix.m_totalDataCWCount; n += blockCount)
                    buf[position++] = codewords[n];

                byte[] ecc = new byte[256];
                rsObj.Encode(position, buf, out ecc);

                position = matrix.m_regionReedSolomonCWCount - 1;
                //position = 0;

                for (int n = b; n < matrix.m_regionReedSolomonCWCount * blockCount; n += blockCount)
                    codewords[matrix.m_totalDataCWCount + n] = ecc[position--];
            }

            //if (blockCount >= 10)
            if (exchangeFirstTwoAndLastBytesInRSCodes)
            {
                // rearrange RS data in the way that last 2 bytes of each block 
                // will be moved to the beginning of the block (insider RS data only!)

                int numOfIterations = (codewords.Length - matrix.m_totalDataCWCount)/ blockCount;

                for (int b = 0; b < numOfIterations; b++)
                {
                    // rearrange 
                    byte[] buf = new byte[blockCount];
                    Array.Copy(codewords, matrix.m_totalDataCWCount + b * blockCount, buf, 0, blockCount);

                    byte[] buf2 = new byte[blockCount];
                    // copy buf into buf2 except 2 last bytes + now start from pos2
                    Array.Copy(buf, 0, buf2, 2, blockCount - 2);
                    // copying 2 last bytes into the begining of the  buf2
                    Array.Copy(buf, blockCount - 2, buf2, 0, 2);
                    // finally replace the RS block with the modified block
                    Array.Copy(buf2, 0, codewords, matrix.m_totalDataCWCount + b * blockCount, blockCount);
                }
            }

        }

        private static int CompactionModeLength
        {
            get
            {
                return Enum.GetValues(typeof(DataMatrixCompactionMode)).Length;
            }
        }

        private static DataMatrixCompactionMode LastCompactionMode
        {
            get
            {
#if !PocketPC && !WindowsCE
                return (DataMatrixCompactionMode)Enum.GetValues(typeof(DataMatrixCompactionMode)).GetValue(CompactionModeLength - 1);
#else
                // less error-prone way
                return DataMatrixCompactionMode.Binary;
#endif
            }
        }

        protected static bool isDigit(byte b)
        {
            return (b >= '0' && b <= '9');
        }

        private static bool isUpper(byte b)
        {
            return (b >= 'A' && b <= 'Z');
        }

        private static bool isLower(byte b)
        {
            return (b >= 'a' && b <= 'z');
        }

        private static int switchCost(DataMatrixCompactionMode from, DataMatrixCompactionMode to)
        {
            int[,] switchCostTable = new int[6, 6] 
            {
                {0, 1, 1, 1, 1, 2},	// Ascii
                {1, 0, 2, 2, 2, 3},	// C40
                {1, 2, 0, 2, 2, 3},	// Text
                {1, 2, 2, 0, 2, 3},	// X12
                {1, 2, 2, 2, 0, 3},	// Edifact
                {0, 1, 1, 1, 1, 0}	// Binary
            };

            return switchCostTable[(int)from, (int)to];
        }

        private static void calculatePlacement(int[] places, int rowCount, int columnCount)
        {
            Array.Clear(places, 0, places.Length);

            int position = 1;
            int row = 4;
            int column = 0;

            do
            {
                // check corner
                if (row == rowCount && column == 0)
                    calculateCornerAPlacement(places, rowCount, columnCount, position++);

                if ((row == rowCount - 2) && column == 0 && (columnCount % 4) != 0)
                    calculateCornerBPlacement(places, rowCount, columnCount, position++);

                if (row == rowCount - 2 && column == 0 && (columnCount % 8) == 4)
                    calculateCornerCPlacement(places, rowCount, columnCount, position++);

                if (row == rowCount + 4 && column == 2 && (columnCount % 8) == 0)
                    calculateCornerDPlacement(places, rowCount, columnCount, position++);

                // up/right
                do
                {
                    if (row < rowCount && column >= 0 && places[row * columnCount + column] == 0)
                        calculateBlockPlacement(places, rowCount, columnCount, row, column, position++);

                    row -= 2;
                    column += 2;
                }
                while (row >= 0 && column < columnCount);

                row++;
                column += 3;

                // down/left
                do
                {
                    if (row >= 0 && column < columnCount && places[row * columnCount + column] == 0)
                        calculateBlockPlacement(places, rowCount, columnCount, row, column, position++);

                    row += 2;
                    column -= 2;
                }
                while (row < rowCount && column >= 0);

                row += 3;
                column++;
            }
            while (row < rowCount || column < columnCount);

            // unfilled corner
            if (places[rowCount * columnCount - 1] == 0)
            {
                places[rowCount * columnCount - 1] = 1;
                places[rowCount * columnCount - columnCount - 2] = 1;
            }
        }

        private static void calculateCornerAPlacement(int[] places, int rowCount, int columnCount, int position)
        {
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 1, 0, position, 7);
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 1, 1, position, 6);
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 1, 2, position, 5);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 2, position, 4);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 1, position, 3);
            calculatePlacementBit(places, rowCount, columnCount, 1, columnCount - 1, position, 2);
            calculatePlacementBit(places, rowCount, columnCount, 2, columnCount - 1, position, 1);
            calculatePlacementBit(places, rowCount, columnCount, 3, columnCount - 1, position, 0);
        }

        private static void calculateCornerBPlacement(int[] places, int rowCount, int columnCount, int position)
        {
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 3, 0, position, 7);
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 2, 0, position, 6);
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 1, 0, position, 5);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 4, position, 4);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 3, position, 3);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 2, position, 2);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 1, position, 1);
            calculatePlacementBit(places, rowCount, columnCount, 1, columnCount - 1, position, 0);
        }

        private static void calculateCornerCPlacement(int[] places, int rowCount, int columnCount, int position)
        {
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 3, 0, position, 7);
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 2, 0, position, 6);
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 1, 0, position, 5);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 2, position, 4);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 1, position, 3);
            calculatePlacementBit(places, rowCount, columnCount, 1, columnCount - 1, position, 2);
            calculatePlacementBit(places, rowCount, columnCount, 2, columnCount - 1, position, 1);
            calculatePlacementBit(places, rowCount, columnCount, 3, columnCount - 1, position, 0);
        }

        private static void calculateCornerDPlacement(int[] places, int rowCount, int columnCount, int position)
        {
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 1, 0, position, 7);
            calculatePlacementBit(places, rowCount, columnCount, rowCount - 1, columnCount - 1, position, 6);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 3, position, 5);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 2, position, 4);
            calculatePlacementBit(places, rowCount, columnCount, 0, columnCount - 1, position, 3);
            calculatePlacementBit(places, rowCount, columnCount, 1, columnCount - 3, position, 2);
            calculatePlacementBit(places, rowCount, columnCount, 1, columnCount - 2, position, 1);
            calculatePlacementBit(places, rowCount, columnCount, 1, columnCount - 1, position, 0);
        }

        private static void calculateBlockPlacement(int[] places, int rowCount, int columnCount, int row, int column, int position)
        {
            calculatePlacementBit(places, rowCount, columnCount, row - 2, column - 2, position, 7);
            calculatePlacementBit(places, rowCount, columnCount, row - 2, column - 1, position, 6);
            calculatePlacementBit(places, rowCount, columnCount, row - 1, column - 2, position, 5);
            calculatePlacementBit(places, rowCount, columnCount, row - 1, column - 1, position, 4);
            calculatePlacementBit(places, rowCount, columnCount, row - 1, column - 0, position, 3);
            calculatePlacementBit(places, rowCount, columnCount, row - 0, column - 2, position, 2);
            calculatePlacementBit(places, rowCount, columnCount, row - 0, column - 1, position, 1);
            calculatePlacementBit(places, rowCount, columnCount, row - 0, column - 0, position, 0);
        }

        private static void calculatePlacementBit(int[] places, int rowCount, int columnCount, int row, int column, int position, byte b)
        {
            if (row < 0)
            {
                row += rowCount;
                column += 4 - ((rowCount + 4) % 8);
            }

            if (column < 0)
            {
                column += columnCount;
                row += 4 - ((columnCount + 4) % 8);
            }

            places[row * columnCount + column] = (position << 3) + b;
        }
    }
}
