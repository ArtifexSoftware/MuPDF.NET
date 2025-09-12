/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;
using System.Drawing;
using System.Collections;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    class PDF417Symbology : SymbologyDrawing2D
    {
        /// <summary>
        /// Encoder compaction mode (some modes are more suitable for some data)
        /// </summary>
        protected enum EncoderMode
        {
            /// <summary>
            /// Mode undefined
            /// </summary>
            Undefined = 0,

            /// <summary>
            /// Text compaction mode (allows optimized text encoding)
            /// </summary>
            Text = 900,

            /// <summary>
            /// Byte compaction mode (allows optimized bytes encoding)
            /// </summary>
            Byte = 901,

            /// <summary>
            /// Numeric compaction mode (allows optimized encoding of numbers)
            /// </summary>
            Numeric = 902
        }

        /// <summary>
        /// Contains information about one chunk of a string being encoded
        /// </summary>
        protected class EncoderChunk
        {
            /// <summary>
            /// Encoder mode to use for chunk
            /// </summary>
            public EncoderMode mode;

            /// <summary>
            /// Number of symbols to encode
            /// </summary>
            public int length;

            /// <summary>
            /// Initializes a new instance of the <see cref="EncoderChunk"/> class.
            /// </summary>
            /// <param name="mode">The encoder mode to use for chunk</param>
            /// <param name="length">The number of symbols to encode.</param>
            public EncoderChunk(EncoderMode mode, int length)
            {
                this.mode = mode;
                this.length = length;
            }
        }

        /// <summary>
        /// Encoder chunks.
        /// First chunk start from first symbol of a string being encoded.
        /// Next chunk start immediately after first and so on.
        /// </summary>
        protected ArrayList m_chunks = new ArrayList();

        protected intList m_codewords;

        protected bool m_compact;
        public int m_errorCorrectionLevel = -1;
        public int m_dataColumnCount;

        private const int c_maxCodewordsCount = 928;
        public string[] m_encodedData;
        protected string m_text;

        /// <summary>
        /// Initializes a new instance of the <see cref="PDF417Symbology"/> class.
        /// </summary>
        public PDF417Symbology()
            : base(TrueSymbologyType.PDF417)
        {
        }

        public PDF417Symbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.PDF417)
        {
        }

        /// <summary>
        /// Validates the value using PDF417 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
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
            return "PDF417 symbology allows a maximum data size of 1850 text characters, or 2710 digits.\n";
        }

        /// <summary>
        /// Gets the barcode value encoded using PDF417 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using PDF417 symbology rules.
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
            Size drawingSize = new Size();

            // a bit weird cycle goes here, but we need it,
            // or we can end up with 2-byte chars which is unsupported
            StringBuilder sb = new StringBuilder();

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

            
            byte[] bytes = tmpEncoding.GetBytes(Value);

            // after inserting this code RussianText UnitTest does not pass (#320 Commit: 334 and #352 Commit: 368)
            /*
            byte[] bytes = new byte[Value.Length];

            int index = 0;
            foreach (char c in Value.ToCharArray())
            {
                bytes[index++] = (byte)c;
            }
            */
            // after inserting this code RussianText UnitTest does not pass (#320 Commit: 334 and #352 Commit: 368)

            for (int i = 0; i < bytes.Length; i++)
                sb.Append((char)bytes[i]);

            m_text = sb.ToString();

            if (Options.PDF417UseManualSize)
                m_dataColumnCount = Options.PDF417ColumnCount;
            else
                m_dataColumnCount = Options.PDF417MinimumColumnCount;

            m_errorCorrectionLevel = (int)Options.PDF417ErrorCorrectionLevel;

            if (Options.PDF417CompactionMode != PDF417CompactionMode.Auto)
                validateCompactionMode(Options.PDF417CompactionMode);

            encodeData();

            int symbolwidth = 0;
            if (m_encodedData.Length != 0)
                symbolwidth = m_encodedData[0].Length;

            drawingSize.Width = symbolwidth * NarrowBarWidth;
            drawingSize.Height = m_encodedData.Length * BarHeight;

            for (int x = 0; x < symbolwidth; x++)
            {
                for (int y = 0; y < m_encodedData.Length; y++)
                {
                    if (m_encodedData[y][x] == '1')
                        m_rects.Add(new Rectangle(x * NarrowBarWidth, y * BarHeight, NarrowBarWidth, BarHeight));
                }
            }

            return drawingSize;
        }

        protected virtual void encodeData()
        {
            if (Options.PDF417CompactionMode == PDF417CompactionMode.Auto)
            {
                splitTextIntoChunks();
                optimizeChunks();
            }

            produceCodewords();
            adjustErrorCorrectionLevel();

            int checkCodewordsCount = calculateCheckCodewordsCount();

            if (!Options.PDF417UseManualSize)
            {
                adjustDataColumnCount(checkCodewordsCount);
                checkBounds(checkCodewordsCount, -1);
                addPaddingIfNeeded(checkCodewordsCount);
            }
            else
            {
                checkBounds(checkCodewordsCount, Options.PDF417RowCount);
                addPaddingIfNeededForManualSize(checkCodewordsCount, Options.PDF417RowCount);
            }

            // we should add the length descriptor
            m_codewords.Insert(0, m_codewords.Count + 1);

            addCheckCodewords(checkCodewordsCount);

            // following integers (c1, c2, c3) are precalculated 
            // values for left|right codewords.
            // which value to use depends on current row encoding table

            int c1 = (m_codewords.Count / m_dataColumnCount - 1) / 3;
            int c2 = m_errorCorrectionLevel * 3 + (m_codewords.Count / m_dataColumnCount - 1) % 3;
            int c3 = m_dataColumnCount - 1;

            int rows = (m_codewords.Count / m_dataColumnCount);
            m_encodedData = new string[rows];
            encodeRows(c1, c2, c3);
        }

        private void encodeRows(int c1, int c2, int c3)
        {
            for (int row = 0; row < (m_codewords.Count / m_dataColumnCount); row++)
            {
                int[] rowCodewords = fillRowCodewords(row, c1, c2, c3);
                string alphaPattern = buildAlphaPattern(row, rowCodewords);

                StringBuilder pattern = new StringBuilder();
                for (int i = 0; i < alphaPattern.Length; i++)
                {
                    for (int j = 0; j < m_patternAlphabet.Length; j++)
                    {
                        if (alphaPattern[i] == m_patternAlphabet[j])
                        {
                            pattern.Append(m_barsForAlpha[j]);
                            break;
                        }
                    }
                }

                m_encodedData[row] = pattern.ToString();
            }
        }

        private string buildAlphaPattern(int row, int[] rowCodewords)
        {
            // Start with a start char and a separator
            StringBuilder alphaPattern = new StringBuilder();
            alphaPattern.Append("+*");

            // truncated symbol differs from ordinary PDF417 symbol by last 5 chars
            int stopIndex = m_dataColumnCount + 1;
            if (m_compact)
                stopIndex--;

            for (int i = 0; i <= stopIndex; i++)
            {
                int offset = 0; // cluster 0
                switch (row % 3)
                {
                    case 1:
                        // cluster 3
                        offset = 929;
                        break;

                    case 2:
                        // cluster 6
                        offset = 1858;
                        break;
                }

                alphaPattern.Append(m_alphaPatternTable[offset + rowCodewords[i]]);
                alphaPattern.Append("*");
            }

            if (!m_compact)
                alphaPattern.Append("-");

            return alphaPattern.ToString();
        }

        private int[] fillRowCodewords(int row, int c1, int c2, int c3)
        {
            int[] rowCodewords = new int[m_dataColumnCount + 2];
            for (int i = 0; i < m_dataColumnCount; i++)
                rowCodewords[i + 1] = m_codewords[row * m_dataColumnCount + i];

            int rowCheckCodewordsCount = (row / 3) * 30;
            switch (row % 3)
            {
                /* follows this pattern from US Patent 5,243,655: 
                    Row 0: L0 (row #, # of rows)         R0 (row #, # of columns)
                    Row 1: L1 (row #, security level)    R1 (row #, # of rows)
                    Row 2: L2 (row #, # of columns)      R2 (row #, security level)
                    Row 3: L3 (row #, # of rows)         R3 (row #, # of columns)
                    etc. 
                 */

                case 0:
                    rowCodewords[0] = rowCheckCodewordsCount + c1;
                    rowCodewords[m_dataColumnCount + 1] = rowCheckCodewordsCount + c3;
                    break;

                case 1:
                    rowCodewords[0] = rowCheckCodewordsCount + c2;
                    rowCodewords[m_dataColumnCount + 1] = rowCheckCodewordsCount + c1;
                    break;

                case 2:
                    rowCodewords[0] = rowCheckCodewordsCount + c3;
                    rowCodewords[m_dataColumnCount + 1] = rowCheckCodewordsCount + c2;
                    break;
            }

            return rowCodewords;
        }

        protected void addCheckCodewords(int checkCodewordsCount)
        {
            int[] correctionCodewords = calculateReedSolomonCodes(checkCodewordsCount);
            for (int i = checkCodewordsCount - 1; i >= 0; i--)
                m_codewords.Add(correctionCodewords[i]);
        }

        private int[] calculateReedSolomonCodes(int checkCodewordsCount)
        {
            int offset = 0;
            switch (m_errorCorrectionLevel)
            {
                case 1:
                    offset = 2;
                    break;

                case 2:
                    offset = 6;
                    break;

                case 3:
                    offset = 14;
                    break;

                case 4:
                    offset = 30;
                    break;

                case 5:
                    offset = 62;
                    break;

                case 6:
                    offset = 126;
                    break;

                case 7:
                    offset = 254;
                    break;

                case 8:
                    offset = 510;
                    break;
            }

            int[] correctionCodewords = new int[checkCodewordsCount];
            Array.Clear(correctionCodewords, 0, correctionCodewords.Length);

            for (int i = 0; i < m_codewords.Count; i++)
            {
                int total = (m_codewords[i] + correctionCodewords[checkCodewordsCount - 1]) % 929;
                for (int j = checkCodewordsCount - 1; j >= 0; j--)
                {
                    if (j == 0)
                        correctionCodewords[j] = (929 - (total * m_errorCorrectionCoefficients[offset + j]) % 929) % 929;
                    else
                        correctionCodewords[j] = (correctionCodewords[j - 1] + 929 - (total * m_errorCorrectionCoefficients[offset + j]) % 929) % 929;
                }
            }

            for (int i = 0; i < checkCodewordsCount; i++)
            {
                if (correctionCodewords[i] != 0)
                    correctionCodewords[i] = 929 - correctionCodewords[i];
            }

            return correctionCodewords;
        }

        private void addPaddingIfNeeded(int checkCodewordsCount)
        {
            int totalCodewordsCount = m_codewords.Count + 1 + checkCodewordsCount;

            int paddingLength = 0;
            if ((totalCodewordsCount / m_dataColumnCount) < 3)
            {
                // a barcode must have at least three rows
                paddingLength = (m_dataColumnCount * 3) - totalCodewordsCount;
            }
            else
            {
                if ((totalCodewordsCount % m_dataColumnCount) > 0)
                    paddingLength = m_dataColumnCount - (totalCodewordsCount % m_dataColumnCount);
            }

            while (paddingLength > 0)
            {
                m_codewords.Add(900);
                paddingLength--;
            }
        }

        private void addPaddingIfNeededForManualSize(int checkCodewordsCount, int rowCount)
        {
            // if row count is zero or less
            // auto calculate row count
            if (rowCount <= 0 && m_dataColumnCount > 0)
            {
                rowCount = (m_codewords.Count + checkCodewordsCount) / m_dataColumnCount + 1;
            }


            int totalCodewordsCount = m_codewords.Count + 1 + checkCodewordsCount;
            int paddingLength = m_dataColumnCount * rowCount - totalCodewordsCount;
            if (paddingLength < 0)
                throw new BarcodeException(String.Format("Input string is too long for specified number of data columns and rows. Row count provided = {0}, data columns count = {1}", rowCount, m_dataColumnCount));

            for (int i = 0; i < paddingLength; i++)
                m_codewords.Add(900);
        }

        private void checkBounds(int checkCodewordsCount, int rowCount)
        {
            // if row count is zero or less
            // auto calculate row count
            if (rowCount <= 0 && m_dataColumnCount > 0)
            {
                rowCount = (m_codewords.Count + checkCodewordsCount) / m_dataColumnCount + 1;
            }

            if (m_codewords.Count + checkCodewordsCount > c_maxCodewordsCount)
                throw new BarcodeException("Input string is too long for PDF 417 barcode.");

            // check if row count or column count exceeds allowed
            if (m_dataColumnCount == 0 || rowCount == 0 || m_dataColumnCount > 30 || rowCount > 90) 
                throw new BarcodeException(String.Format("Incorrect size. PDF417 barcode should contain 1..90 data rows and 1..20 data columns. Row count provided = {0}, data columns count = {1}", rowCount, m_dataColumnCount));

            // check if row count exceeds required 
            //if (((m_codewords.Count + checkCodewordsCount) / m_dataColumnCount) > 90)
            //    throw new BarcodeException("Input string is too long for specified number of data columns.");
        }

        private void adjustDataColumnCount(int checkCodewordsCount)
        {
            if (m_dataColumnCount > 30)
                m_dataColumnCount = 30;

            if (m_dataColumnCount < 1)
                m_dataColumnCount = (int)(0.5f + Math.Sqrt((m_codewords.Count + checkCodewordsCount) / 3.0f));

            if (((m_codewords.Count + checkCodewordsCount) / m_dataColumnCount) > 90)
            {
                // prevent too tall columns
                m_dataColumnCount++;
            }
        }

        private int calculateCheckCodewordsCount()
        {
            int checkCodewordsCount = 1;
            for (int i = 1; i <= (m_errorCorrectionLevel + 1); i++)
                checkCodewordsCount *= 2;

            return checkCodewordsCount;
        }

        private void adjustErrorCorrectionLevel()
        {
            if (m_errorCorrectionLevel < 0)
            {
                // error correction level 8 is never used automatically
                m_errorCorrectionLevel = 7;

                if (m_codewords.Count <= 1280)
                    m_errorCorrectionLevel = 6;

                if (m_codewords.Count <= 640)
                    m_errorCorrectionLevel = 5;

                if (m_codewords.Count <= 320)
                    m_errorCorrectionLevel = 4;

                if (m_codewords.Count <= 160)
                    m_errorCorrectionLevel = 3;

                if (m_codewords.Count <= 40)
                    m_errorCorrectionLevel = 2;
            }
        }

        protected void produceCodewords()
        {
            int textPos = 0;
            m_codewords = new intList();
            for (int i = 0; i < m_chunks.Count; i++)
            {
                switch ((m_chunks[i] as EncoderChunk).mode)
                {
                    case EncoderMode.Text:
                        processTextChunk(textPos, i);
                        break;

                    case EncoderMode.Byte:
                        processByteChunk(textPos, i);
                        break;

                    case EncoderMode.Numeric:
                        processNumberChunk(textPos, i);
                        break;
                }

                textPos += (m_chunks[i] as EncoderChunk).length;
            }

            if (Options.PDF417CreateMacro)
            {
                m_codewords.Add(928);
                //segment index
                var t = 100000 + Options.PDF417SegmentIndex;
                var d1 = t % 900;
                t = t / 900;
                var d2 = t % 900;
                m_codewords.Add(d2);
                m_codewords.Add(d1);
                //file ID
                m_codewords.Add((Options.PDF417FileID / 1000) % 900);
                m_codewords.Add((Options.PDF417FileID % 1000) % 900);

                if (Options.PDF417LastSegment)
                    m_codewords.Add(922);
            }
        }

        private static EncoderMode detectMode(char codeascii)
        {
            EncoderMode mode = EncoderMode.Byte;

            if ((codeascii == '\t') || (codeascii == '\n') || (codeascii >= ' ' && codeascii <= '~') || (codeascii == '\r'))
                mode = EncoderMode.Text;
            
            if ((codeascii >= '0') && (codeascii <= '9'))
                mode = EncoderMode.Numeric;

            return mode;
        }

        /// <summary>
        /// Optimizes the chunks. Tries to remove unnecessary mode switches and
        /// re-groups chunks after that.
        /// </summary>
        protected virtual void optimizeChunks()
        {
            for (int i = 0; i < m_chunks.Count; i++)
            {
                EncoderMode prevMode = EncoderMode.Undefined;
                EncoderMode nextMode = EncoderMode.Undefined;
                getPrevAndNextMode(out prevMode, out nextMode, i);

                EncoderChunk chunk = (m_chunks[i] as EncoderChunk);
                if (chunk.mode == EncoderMode.Numeric)
                {
                    int currentChunkLength = chunk.length;

                    if (i == 0 && m_chunks.Count > 1)
                    {
                        // first block and there are other blocks
                        if ((nextMode == EncoderMode.Text) && (currentChunkLength < 8))
                            chunk.mode = EncoderMode.Text;

                        if ((nextMode == EncoderMode.Byte) && (currentChunkLength == 1))
                            chunk.mode = EncoderMode.Byte;
                    }
                    else
                    {
                        if (i == m_chunks.Count - 1)
                        {
                            // last block
                            if ((prevMode == EncoderMode.Text) && (currentChunkLength < 7))
                                chunk.mode = EncoderMode.Text;

                            if ((prevMode == EncoderMode.Byte) && (currentChunkLength == 1))
                                chunk.mode = EncoderMode.Byte;
                        }
                        else
                        {
                            // not first or last block
                            if (((prevMode == EncoderMode.Byte) && (nextMode == EncoderMode.Byte)) && (currentChunkLength < 4))
                                chunk.mode = EncoderMode.Byte;

                            if (((prevMode == EncoderMode.Byte) && (nextMode == EncoderMode.Text)) && (currentChunkLength < 4))
                                chunk.mode = EncoderMode.Text;

                            if (((prevMode == EncoderMode.Text) && (nextMode == EncoderMode.Byte)) && (currentChunkLength < 5))
                                chunk.mode = EncoderMode.Text;

                            if (((prevMode == EncoderMode.Text) && (nextMode == EncoderMode.Text)) && (currentChunkLength < 8))
                                chunk.mode = EncoderMode.Text;
                        }
                    }
                }
            }

            condenseChunks();

            for (int i = 0; i < m_chunks.Count; i++)
            {
                EncoderChunk chunk = (m_chunks[i] as EncoderChunk);
                int currentChunkLength = chunk.length;

                EncoderMode prevMode = EncoderMode.Undefined;
                EncoderMode nextMode = EncoderMode.Undefined;
                getPrevAndNextMode(out prevMode, out nextMode, i);

                if ((chunk.mode == EncoderMode.Text) && (i > 0))
                {
                    if (i == m_chunks.Count - 1)
                    {
                        if ((prevMode == EncoderMode.Byte) && (currentChunkLength == 1))
                            chunk.mode = EncoderMode.Byte;
                    }
                    else
                    {
                        if (((prevMode == EncoderMode.Byte) && (nextMode == EncoderMode.Byte)) && (currentChunkLength < 5))
                            chunk.mode = EncoderMode.Byte;

                        if ((((prevMode == EncoderMode.Byte) && (nextMode != EncoderMode.Byte)) || ((prevMode != EncoderMode.Byte) && (nextMode == EncoderMode.Byte))) && (currentChunkLength < 3))
                            chunk.mode = EncoderMode.Byte;
                    }
                }
            }

            condenseChunks();
        }

        void getPrevAndNextMode(out EncoderMode prevMode, out EncoderMode nextMode, int currentChunk)
        {
            prevMode = EncoderMode.Undefined;
            if (currentChunk != 0)
                prevMode = (m_chunks[currentChunk - 1] as EncoderChunk).mode;

            nextMode = EncoderMode.Undefined;
            if (currentChunk != m_chunks.Count - 1)
                nextMode = (m_chunks[currentChunk + 1] as EncoderChunk).mode;
        }

        void processTextChunk(int startTextPos, int currentChunk)
        {
            EncoderChunk chunk = m_chunks[currentChunk] as EncoderChunk;
            int[] tableNumber = new int[chunk.length];
            int[] charCodePos = new int[chunk.length];
            buildTextModeArrays(currentChunk, startTextPos, tableNumber, charCodePos);

            // default table is Uppercase
            int currentTable = 1;

            // charCodePos is array of character positions
            // charCodePosFinal is array of character positions with all required table switches
            intList charCodePosFinal = new intList();

            for (int i = 0; i < chunk.length; i++)
            {
                if ((tableNumber[i] & currentTable) != 0)
                {
                    // the character is in the current table
                    charCodePosFinal.Add(charCodePos[i]);
                }
                else
                {
                    //character does not contained in current table. we need to change table
                    bool changedTableForOneCharacter = false;
                    if (shouldChangeTableForOneCharacter(currentChunk, i, tableNumber))
                        changedTableForOneCharacter = tryChangeTableForOneCharacter(tableNumber, charCodePos, charCodePosFinal, i, currentTable);

                    if (!changedTableForOneCharacter)
                    {
                        currentTable = changeTable(tableNumber, charCodePosFinal, currentTable, currentChunk, i);
                        charCodePosFinal.Add(charCodePos[i]);
                    }
                }
            }

            if ((charCodePosFinal.Count % 2) != 0)
                charCodePosFinal.Add(29);

            if (currentChunk > 0)
                m_codewords.Add(900);

            for (int i = 0; i < charCodePosFinal.Count; i += 2)
            {
                int cwNumber = (30 * charCodePosFinal[i]) + charCodePosFinal[i + 1];
                m_codewords.Add(cwNumber);
            }
        }

        private int changeTable(int[] tableNumber, intList charCodePosFinal, int currentTable, int currentChunk, int i)
        {
            int newTable;
            if (i == ((m_chunks[currentChunk] as EncoderChunk).length - 1))
            {
                newTable = tableNumber[i];
            }
            else
            {
                if ((tableNumber[i] & tableNumber[i + 1]) == 0)
                    newTable = tableNumber[i];
                else
                    newTable = tableNumber[i] & tableNumber[i + 1];
            }

            newTable = findSingleNewTable(newTable);
            addTableSwitches(currentTable, newTable, charCodePosFinal);
            return newTable;
        }

        private static void addTableSwitches(int currentTable, int newTable, intList charCodePosFinal)
        {
            switch (currentTable)
            {
                case 1:
                    switch (newTable)
                    {
                        case 2:
                            charCodePosFinal.Add(27);
                            break;

                        case 4:
                            charCodePosFinal.Add(28);
                            break;

                        case 8:
                            charCodePosFinal.Add(28);
                            charCodePosFinal.Add(25);
                            break;
                    }
                    break;

                case 2:
                    switch (newTable)
                    {
                        case 1:
                            charCodePosFinal.Add(28);
                            charCodePosFinal.Add(28);
                            break;

                        case 4:
                            charCodePosFinal.Add(28);
                            break;

                        case 8:
                            charCodePosFinal.Add(28);
                            charCodePosFinal.Add(25);
                            break;
                    }
                    break;

                case 4:
                    switch (newTable)
                    {
                        case 1:
                            charCodePosFinal.Add(28);
                            break;

                        case 2:
                            charCodePosFinal.Add(27);
                            break;

                        case 8:
                            charCodePosFinal.Add(25);
                            break;
                    }
                    break;

                case 8:
                    switch (newTable)
                    {
                        case 1:
                            charCodePosFinal.Add(29);
                            break;

                        case 2:
                            charCodePosFinal.Add(29);
                            charCodePosFinal.Add(27);
                            break;

                        case 4:
                            charCodePosFinal.Add(29);
                            charCodePosFinal.Add(28);
                            break;
                    }
                    break;
            }
        }

        private static int findSingleNewTable(int newTableCandidate)
        {
            // Use the first eligible table
            int newTable = newTableCandidate;
            switch (newTableCandidate)
            {
                case 3:
                case 5:
                case 7:
                case 9:
                case 11:
                case 13:
                case 15:
                    newTable = 1;
                    break;

                case 6:
                case 10:
                case 14:
                    newTable = 2;
                    break;

                case 12:
                    newTable = 4;
                    break;
            }

            return newTable;
        }

        private static bool tryChangeTableForOneCharacter(int[] tableNumber, int[] charCodePos, intList charCodePosFinal, int currentChunkPos, int currentTable)
        {
            bool changedTableForOneCharacter = true;

            if (((tableNumber[currentChunkPos] & 1) != 0) && (currentTable == 2))
            {
                charCodePosFinal.Add(27); // T_UPP
                charCodePosFinal.Add(charCodePos[currentChunkPos]);
            }

            if ((tableNumber[currentChunkPos] & 8) != 0)
            {
                charCodePosFinal.Add(29); // T_PUN
                charCodePosFinal.Add(charCodePos[currentChunkPos]);
            }

            if (!((((tableNumber[currentChunkPos] & 1) != 0) && (currentTable == 2)) || ((tableNumber[currentChunkPos] & 8) != 0)))
            {
                // can't change for one character
                changedTableForOneCharacter = false;
            }

            return changedTableForOneCharacter;
        }

        private bool shouldChangeTableForOneCharacter(int currentChunk, int currentChunkPos, int[] tableNumber)
        {
            bool shouldChange = false;

            if (currentChunkPos == ((m_chunks[currentChunk] as EncoderChunk).length - 1))
                shouldChange = true;
            else
            {
                if ((tableNumber[currentChunkPos] & tableNumber[currentChunkPos + 1]) == 0)
                    shouldChange = true;
            }

            return shouldChange;
        }

        /// <summary>
        /// Builds the text mode arrays.
        /// </summary>
        /// <param name="currentChunk">The current chunk.</param>
        /// <param name="startTextPos">The start text pos.</param>
        /// <param name="tableNumber">The table numbers array.</param>
        /// <param name="charCodePos">The charcode positions array.</param>
        private void buildTextModeArrays(int currentChunk, int startTextPos, int[] tableNumber, int[] charCodePos)
        {
            // Charcode positions array and table numbers array are synchronized.
            // It means that charCodePos[i] and tableNumber[i] are for symbol m_text[startTextPos + i].

            // Table numbers array contains encoded values.
            // Each encoded value indicates tables where a symbol can be found.

            // Please take a look at "Papers/The PDF417 code.rtf"

            // For example: 
            //      m_text[startTextPos + i] == '\n' (LF)
            //      tableNumber[i] is 8. 
            //      8 is 1000 in binary representation.
            //      tables are [Punctuation, Mixed, Lowercase, Uppercase]
            //      so, LF symbol can be found in Punctuation table.
            //      charCodePos[i] is 15
            //      so, LF symbol can be found in Punctuation table at line with number 15

            int chunkLength = (m_chunks[currentChunk] as EncoderChunk).length;
            for (int i = 0; i < chunkLength; i++)
            {
                char asciiCode = m_text[startTextPos + i];
                switch (asciiCode)
                {
                    case '\t':
                        tableNumber[i] = 12;
                        charCodePos[i] = 12;
                        break;

                    case '\n':
                        tableNumber[i] = 8;
                        charCodePos[i] = 15;
                        break;

                    case (char)13:
                        tableNumber[i] = 12;
                        charCodePos[i] = 11;
                        break;

                    default:
                        tableNumber[i] = m_textModeTableNumbers[asciiCode - 32];
                        charCodePos[i] = m_textModeCharPos[asciiCode - 32];
                        break;
                }
            }
        }

        /// <summary>
        /// readonly array with precalculated values of 256 in different pows from 0 to 5
        /// </summary>
        private readonly long[] arrPow256 = {
            1, // Math.Pow(256, 0);
    		256, // Math.Pow(256, 1)
		    65536, // Math.Pow(256, 2)	
		    16777216, // Math.Pow(256, 3)	
		    4294967296, // Math.Pow(256, 4)		
            1099511627776, // Math.Pow(256, 5)
        };

        void processByteChunk(int startTextPos, int currentChunk)
        {
            EncoderChunk chunk = m_chunks[currentChunk] as EncoderChunk;
            if (chunk.length == 1)
            {
                // single byte mode
                // see http://www.geocities.ws/ooosawaddee3pdf417/high.htm#Byte Compaction Mode
                // and http://grandzebu.net/informatique/codbar-en/pdf417.htm
                m_codewords.Add(913);
                m_codewords.Add(m_text[startTextPos]);
            }
            else
            {
                // mode latch 924 when multiple 6
                if (chunk.length % 6 == 0)
                    m_codewords.Add(924);
                else // mode latch 901 
                    m_codewords.Add(901);

                for (int i = 0; i < chunk.length; )
                {
                    /* from old implementation (see #452, 26 July 2013)
                    short[] accum = new short[112];
                    short[] xReg = new short[112];
                    short[] yReg = new short[112];
                    */
                    long sumAll = 0;
                    int chunkLength = chunk.length - i;
                    if (chunkLength >= 6)
                    {
                        #region old 256 to 900 base conversion, old implementation see #452, 26 July 2013
                        /*
                        for (int j = 0; j < 112; j++)
                        {
                            accum[j] = 0;
                            xReg[j] = 0;
                            yReg[j] = 0;
                        }

                        chunkLength = 6;
                        for (int j = 0; j < chunkLength; j++)
                        {
                            for (int k = 0; k < 8; k++)
                                shiftUp(yReg);

                            if ((m_text[startTextPos + i + j] & 0x80) != 0)
                                yReg[7] = 1;

                            if ((m_text[startTextPos + i + j] & 0x40) != 0)
                                yReg[6] = 1;

                            if ((m_text[startTextPos + i + j] & 0x20) != 0)
                                yReg[5] = 1;

                            if ((m_text[startTextPos + i + j] & 0x10) != 0)
                                yReg[4] = 1;

                            if ((m_text[startTextPos + i + j] & 0x08) != 0)
                                yReg[3] = 1;

                            if ((m_text[startTextPos + i + j] & 0x04) != 0)
                                yReg[2] = 1;

                            if ((m_text[startTextPos + i + j] & 0x02) != 0)
                                yReg[1] = 1;

                            if ((m_text[startTextPos + i + j] & 0x01) != 0)
                                yReg[0] = 1;

                        }

                        int[] chunkCodewords = new int[5];
                        for (int j = 0; j < 4; j++)
                        {
                            for (int k = 0; k < 112; k++)
                            {
                                accum[k] = yReg[k];
                                yReg[k] = 0;
                                xReg[k] = 0;
                            }

                            xReg[101] = 1;
                            xReg[100] = 1;
                            xReg[99] = 1;
                            xReg[94] = 1;

                            for (int k = 92; k >= 0; k--)
                            {
                                yReg[k] = isLarger(accum, xReg);
                                if (yReg[k] == 1)
                                    binarySubtract(accum, xReg);

                                shiftdown(xReg);
                            }

                            chunkCodewords[j] = (accum[9] * 512) + (accum[8] * 256) + (accum[7] * 128) + (accum[6] * 64) + (accum[5] * 32) + (accum[4] * 16) + (accum[3] * 8) + (accum[2] * 4) + (accum[1] * 2) + accum[0];
                        }

                        chunkCodewords[4] = (yReg[9] * 512) + (yReg[8] * 256) + (yReg[7] * 128) + (yReg[6] * 64) + (yReg[5] * 32) + (yReg[4] * 16) + (yReg[3] * 8) + (yReg[2] * 4) + (yReg[1] * 2) + yReg[0];

                        for (int j = 0; j < 5; j++)
                            m_codewords.Add(chunkCodewords[4 - j]);
                    */

                        #endregion 

                        int alreadyProcessed = 0;
                        int loopCount = chunkLength / 6;
                        // iterate through each 6 
                        for (int ilooper = 0; ilooper < loopCount; ilooper++)
                        {
                            for (int j = 0; j < 6; j++)
                                sumAll += m_text[startTextPos + i + alreadyProcessed + j] * arrPow256[5 - j];
                            long[] newValues = new long[5];

                            
                            for(int k=0; k<5; k++)
                            {
                                // save reminder
                                newValues[k] = sumAll % 900;
                                // save divided value
                                sumAll = sumAll / 900;
                            }

                            // adding inversed newValues array 
                            for (int k = 4; k >= 0; k-- )
                                m_codewords.Add((int)newValues[k]);

                            // shift by 6 bytes (source), not 5 as we are converting 6 bytes into 5 (in base of 900, see papers/pdf417..rtf)
                            alreadyProcessed += 6;
                        }
                        // shift by number of processed symbols (with 6 symbols per each loop)
                        i += loopCount * 6;
                    }
                    else
                    {
                        // less than 6 bytes remains
                        for (int j = 0; j < chunkLength; j++)
                        {
                            m_codewords.Add(m_text[startTextPos + i + j]);                            
                        }
                        // exit the loop as we processed the remaining less than 6 bytes
                        break; 
                    }

                }
            }
        }

        void processNumberChunk(int startTextPos, int currentChunk)
        {
            m_codewords.Add(902);

            EncoderChunk chunk = m_chunks[currentChunk] as EncoderChunk;
            for (int i = 0; i < chunk.length; )
            {
                int blockLength = chunk.length - i;
                if (blockLength > 44)
                    blockLength = 44;

                string blockToEncode = "1" + m_text.Substring(startTextPos + i, blockLength);
                string encodedBlock = "";
                intList blockCodewords = new intList();
                do
                {
                    int divider = 900;
                    encodedBlock = "";

                    int number = 0;
                    while (blockToEncode.Length != 0)
                    {
                        number *= 10;
                        number += ctoi(blockToEncode[0]);

                        blockToEncode = blockToEncode.Remove(0, 1);

                        if (number < divider)
                        {
                            if (encodedBlock.Length != 0)
                                encodedBlock += "0";
                        }
                        else
                        {
                            char temp = (char)((number / divider) + '0');
                            encodedBlock += temp;
                        }

                        number = number % divider;
                    }

                    divider = number;
                    blockCodewords.Insert(0, divider);
                    blockToEncode = encodedBlock;

                } while (encodedBlock.Length != 0);

                for (int j = 0; j < blockCodewords.Count; j++)
                    m_codewords.Add(blockCodewords[j]);

                i += blockLength;
            }
        }

        /// <summary>
        /// Condenses the chunks. Brings together blocks with same mode.
        /// </summary>
        void condenseChunks()
        {
            if (m_chunks.Count > 1)
            {
                int i = 1;
                while (i < m_chunks.Count)
                {
                    if ((m_chunks[i - 1] as EncoderChunk).mode == (m_chunks[i] as EncoderChunk).mode)
                    {
                        // sum lengths of adjustment chunks with same mode
                        (m_chunks[i - 1] as EncoderChunk).length += (m_chunks[i] as EncoderChunk).length;
                        m_chunks.RemoveAt(i);
                    }

                    i++;
                }
            }
        }

        private static void shiftUp(short[] buffer)
        {
            for (int i = 102; i > 0; i--)
                buffer[i] = buffer[i - 1];

            buffer[0] = 0;
        }

        private static short isLarger(short[] accum, short[] reg)
        {
            bool found = false;
            short larger = 0;

            int i = 103;
            do
            {
                if ((accum[i] == 1) && (reg[i] == 0))
                {
                    found = true;
                    larger = 1;
                }

                if ((accum[i] == 0) && (reg[i] == 1))
                    found = true;

                i--;
            } while (!found && (i > -1));

            return larger;
        }

        private static void binarySubtract(short[] accumulator, short[] inputBuffer)
        {
            // 2's compliment subtraction
            // take inputBuffer from accumulator and put answer in accumulator

            short[] subBuffer = new short[112];
            for (int i = 0; i < 112; i++)
            {
                if (inputBuffer[i] == 0)
                    subBuffer[i] = 1;
                else
                    subBuffer[i] = 0;
            }

            binaryAdd(accumulator, subBuffer);

            subBuffer[0] = 1;
            for (int i = 1; i < 112; i++)
                subBuffer[i] = 0;

            binaryAdd(accumulator, subBuffer);
        }

        private static void binaryAdd(short[] accumulator, short[] inputBuffer)
        {
            int carry = 0;
            for (int i = 0; i < 112; i++)
            {
                bool done = false;
                if (((inputBuffer[i] == 0) && (accumulator[i] == 0)) && ((carry == 0) && !done))
                {
                    accumulator[i] = 0;
                    carry = 0;
                    done = true;
                }
                if (((inputBuffer[i] == 0) && (accumulator[i] == 0)) && ((carry == 1) && !done))
                {
                    accumulator[i] = 1;
                    carry = 0;
                    done = true;
                }
                if (((inputBuffer[i] == 0) && (accumulator[i] == 1)) && ((carry == 0) && !done))
                {
                    accumulator[i] = 1;
                    carry = 0;
                    done = true;
                }
                if (((inputBuffer[i] == 0) && (accumulator[i] == 1)) && ((carry == 1) && !done))
                {
                    accumulator[i] = 0;
                    carry = 1;
                    done = true;
                }
                if (((inputBuffer[i] == 1) && (accumulator[i] == 0)) && ((carry == 0) && !done))
                {
                    accumulator[i] = 1;
                    carry = 0;
                    done = true;
                }
                if (((inputBuffer[i] == 1) && (accumulator[i] == 0)) && ((carry == 1) && !done))
                {
                    accumulator[i] = 0;
                    carry = 1;
                    done = true;
                }
                if (((inputBuffer[i] == 1) && (accumulator[i] == 1)) && ((carry == 0) && !done))
                {
                    accumulator[i] = 0;
                    carry = 1;
                    done = true;
                }
                if (((inputBuffer[i] == 1) && (accumulator[i] == 1)) && ((carry == 1) && !done))
                {
                    accumulator[i] = 1;
                    carry = 1;
                    done = true;
                }
            }
        }

        // Converts a character 0-9 to its equivalent integer value
        protected static int ctoi(char source)
        {
            if ((source >= '0') && (source <= '9'))
                return (source - '0');

            return (source - 'A' + 10);
        }

        private static void shiftdown(short[] buffer)
        {
            buffer[102] = 0;
            buffer[103] = 0;

            for (int i = 0; i < 102; i++)
                buffer[i] = buffer[i + 1];
        }

        protected void validateCompactionMode(PDF417CompactionMode mode)
        {
            m_chunks.Clear();
            bool cantEncode = false;

            if (mode != PDF417CompactionMode.Binary)
            {
                if (mode == PDF417CompactionMode.Numeric)
                {
                    foreach (char c in m_text)
                    {
                        if (c < '0' || c > '9')
                        {
                            cantEncode = true;
                            break;
                        }
                    }
                }
                else if (mode == PDF417CompactionMode.Text)
                {
                    foreach (char c in m_text)
                    {
                        if ((c < ' ' || c > '~') && (c != '\t') && (c != '\n') && (c != 13))
                        {
                            cantEncode = true;
                            break;
                        }
                    }
                }
            }

            if (cantEncode)
                throw new BarcodeException(string.Format("Input string can not be encoded with {0} compaction mode", mode));

            EncoderMode em = EncoderMode.Byte;
            if (mode == PDF417CompactionMode.Numeric)
                em = EncoderMode.Numeric;
            else if (mode == PDF417CompactionMode.Text)
                em = EncoderMode.Text;

            m_chunks.Add(new EncoderChunk(em, m_text.Length));
        }

        protected virtual void splitTextIntoChunks()
        {
            m_chunks.Clear();

            for (int textPos = 0; textPos < m_text.Length; )
            {
                EncoderMode mode = detectMode(m_text[textPos]);
                int chunkLength = 0;
                for (; textPos < m_text.Length; textPos++)
                {
                    EncoderMode newMode = detectMode(m_text[textPos]);
                    if (newMode != mode)
                        break;

                    chunkLength++;
                }

                m_chunks.Add(new EncoderChunk(mode, chunkLength));
            }
        }


        //////////////////////////////////////////////////////////////////////////
        //
        //  Data part
        //
        //////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Alphabet of symbols that may be found in a alpha pattern.
        /// </summary>
        protected static string m_patternAlphabet = "ABCDEFabcdefghijklmnopqrstuvwxyz*+-";

        /// <summary>
        /// Bar patterns for symbols of alpha pattern alphabet
        /// </summary>
        protected static string[] m_barsForAlpha = 
        { 
            "00000", "00001", "00010", "00011", "00100", "00101", "00110", "00111",
            "01000", "01001", "01010", "01011", "01100", "01101", "01110", "01111", "10000", "10001",
            "10010", "10011", "10100", "10101", "10110", "10111", "11000", "11001", "11010",
            "11011", "11100", "11101", "11110", "11111", "01", "1111111101010100", "11111101000101001"
        };

        static int[] m_textModeTableNumbers = 
        { 
            7, 8, 8, 4, 12, 4, 4, 8, 8, 8, 12, 4, 12, 12, 12, 12, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 12, 8, 8, 4, 8, 8, 8, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 8, 8, 8, 4, 8, 8, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 8, 8, 8, 8 
        };

        static int[] m_textModeCharPos = 
        { 
            26, 10, 20, 15, 18, 21, 10, 28, 23, 24, 22, 20, 13, 16, 17, 19, 0, 1, 2, 3,
            4, 5, 6, 7, 8, 9, 14, 0, 1, 23, 2, 25, 3, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 4, 5, 6, 24, 7, 8, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 21, 27, 9 
        };

        /* PDF417 error correction coefficients*/
        static int[] m_errorCorrectionCoefficients = 
        {
            /* k = 2 */
            27, 917,
        	
            /* k = 4 */
            522, 568, 723, 809,
        	
            /* k = 8 */
            237, 308, 436, 284, 646, 653, 428, 379,
        	
            /* k = 16 */
            274, 562, 232, 755, 599, 524, 801, 132, 295, 116, 442, 428, 295, 42, 176, 65,
        	
            /* k = 32 */
            361, 575, 922, 525, 176, 586, 640, 321, 536, 742, 677, 742, 687, 284, 193, 517,
            273, 494, 263, 147, 593, 800, 571, 320, 803, 133, 231, 390, 685, 330, 63, 410,
        	
            /* k = 64 */
            539, 422, 6, 93, 862, 771, 453, 106, 610, 287, 107, 505, 733, 877, 381, 612,
            723, 476, 462, 172, 430, 609, 858, 822, 543, 376, 511, 400, 672, 762, 283, 184,
            440, 35, 519, 31, 460, 594, 225, 535, 517, 352, 605, 158, 651, 201, 488, 502,
            648, 733, 717, 83, 404, 97, 280, 771, 840, 629, 4, 381, 843, 623, 264, 543,
        	
            /* k = 128 */
            521, 310, 864, 547, 858, 580, 296, 379, 53, 779, 897, 444, 400, 925, 749, 415,
            822, 93, 217, 208, 928, 244, 583, 620, 246, 148, 447, 631, 292, 908, 490, 704,
            516, 258, 457, 907, 594, 723, 674, 292, 272, 96, 684, 432, 686, 606, 860, 569,
            193, 219, 129, 186, 236, 287, 192, 775, 278, 173, 40, 379, 712, 463, 646, 776,
            171, 491, 297, 763, 156, 732, 95, 270, 447, 90, 507, 48, 228, 821, 808, 898,
            784, 663, 627, 378, 382, 262, 380, 602, 754, 336, 89, 614, 87, 432, 670, 616,
            157, 374, 242, 726, 600, 269, 375, 898, 845, 454, 354, 130, 814, 587, 804, 34,
            211, 330, 539, 297, 827, 865, 37, 517, 834, 315, 550, 86, 801, 4, 108, 539,
        	
            /* k = 256 */
            524, 894, 75, 766, 882, 857, 74, 204, 82, 586, 708, 250, 905, 786, 138, 720,
            858, 194, 311, 913, 275, 190, 375, 850, 438, 733, 194, 280, 201, 280, 828, 757,
            710, 814, 919, 89, 68, 569, 11, 204, 796, 605, 540, 913, 801, 700, 799, 137,
            439, 418, 592, 668, 353, 859, 370, 694, 325, 240, 216, 257, 284, 549, 209, 884,
            315, 70, 329, 793, 490, 274, 877, 162, 749, 812, 684, 461, 334, 376, 849, 521,
            307, 291, 803, 712, 19, 358, 399, 908, 103, 511, 51, 8, 517, 225, 289, 470,
            637, 731, 66, 255, 917, 269, 463, 830, 730, 433, 848, 585, 136, 538, 906, 90,
            2, 290, 743, 199, 655, 903, 329, 49, 802, 580, 355, 588, 188, 462, 10, 134,
            628, 320, 479, 130, 739, 71, 263, 318, 374, 601, 192, 605, 142, 673, 687, 234,
            722, 384, 177, 752, 607, 640, 455, 193, 689, 707, 805, 641, 48, 60, 732, 621,
            895, 544, 261, 852, 655, 309, 697, 755, 756, 60, 231, 773, 434, 421, 726, 528,
            503, 118, 49, 795, 32, 144, 500, 238, 836, 394, 280, 566, 319, 9, 647, 550,
            73, 914, 342, 126, 32, 681, 331, 792, 620, 60, 609, 441, 180, 791, 893, 754,
            605, 383, 228, 749, 760, 213, 54, 297, 134, 54, 834, 299, 922, 191, 910, 532,
            609, 829, 189, 20, 167, 29, 872, 449, 83, 402, 41, 656, 505, 579, 481, 173,
            404, 251, 688, 95, 497, 555, 642, 543, 307, 159, 924, 558, 648, 55, 497, 10,
        	
            /* k = 512 */
            352, 77, 373, 504, 35, 599, 428, 207, 409, 574, 118, 498, 285, 380, 350, 492,
            197, 265, 920, 155, 914, 299, 229, 643, 294, 871, 306, 88, 87, 193, 352, 781,
            846, 75, 327, 520, 435, 543, 203, 666, 249, 346, 781, 621, 640, 268, 794, 534,
            539, 781, 408, 390, 644, 102, 476, 499, 290, 632, 545, 37, 858, 916, 552, 41,
            542, 289, 122, 272, 383, 800, 485, 98, 752, 472, 761, 107, 784, 860, 658, 741,
            290, 204, 681, 407, 855, 85, 99, 62, 482, 180, 20, 297, 451, 593, 913, 142,
            808, 684, 287, 536, 561, 76, 653, 899, 729, 567, 744, 390, 513, 192, 516, 258,
            240, 518, 794, 395, 768, 848, 51, 610, 384, 168, 190, 826, 328, 596, 786, 303,
            570, 381, 415, 641, 156, 237, 151, 429, 531, 207, 676, 710, 89, 168, 304, 402,
            40, 708, 575, 162, 864, 229, 65, 861, 841, 512, 164, 477, 221, 92, 358, 785,
            288, 357, 850, 836, 827, 736, 707, 94, 8, 494, 114, 521, 2, 499, 851, 543,
            152, 729, 771, 95, 248, 361, 578, 323, 856, 797, 289, 51, 684, 466, 533, 820,
            669, 45, 902, 452, 167, 342, 244, 173, 35, 463, 651, 51, 699, 591, 452, 578,
            37, 124, 298, 332, 552, 43, 427, 119, 662, 777, 475, 850, 764, 364, 578, 911,
            283, 711, 472, 420, 245, 288, 594, 394, 511, 327, 589, 777, 699, 688, 43, 408,
            842, 383, 721, 521, 560, 644, 714, 559, 62, 145, 873, 663, 713, 159, 672, 729,
            624, 59, 193, 417, 158, 209, 563, 564, 343, 693, 109, 608, 563, 365, 181, 772,
            677, 310, 248, 353, 708, 410, 579, 870, 617, 841, 632, 860, 289, 536, 35, 777,
            618, 586, 424, 833, 77, 597, 346, 269, 757, 632, 695, 751, 331, 247, 184, 45,
            787, 680, 18, 66, 407, 369, 54, 492, 228, 613, 830, 922, 437, 519, 644, 905,
            789, 420, 305, 441, 207, 300, 892, 827, 141, 537, 381, 662, 513, 56, 252, 341,
            242, 797, 838, 837, 720, 224, 307, 631, 61, 87, 560, 310, 756, 665, 397, 808,
            851, 309, 473, 795, 378, 31, 647, 915, 459, 806, 590, 731, 425, 216, 548, 249,
            321, 881, 699, 535, 673, 782, 210, 815, 905, 303, 843, 922, 281, 73, 469, 791,
            660, 162, 498, 308, 155, 422, 907, 817, 187, 62, 16, 425, 535, 336, 286, 437,
            375, 273, 610, 296, 183, 923, 116, 667, 751, 353, 62, 366, 691, 379, 687, 842,
            37, 357, 720, 742, 330, 5, 39, 923, 311, 424, 242, 749, 321, 54, 669, 316,
            342, 299, 534, 105, 667, 488, 640, 672, 576, 540, 316, 486, 721, 610, 46, 656,
            447, 171, 616, 464, 190, 531, 297, 321, 762, 752, 533, 175, 134, 14, 381, 433,
            717, 45, 111, 20, 596, 284, 736, 138, 646, 411, 877, 669, 141, 919, 45, 780,
            407, 164, 332, 899, 165, 726, 600, 325, 498, 655, 357, 752, 768, 223, 849, 647,
            63, 310, 863, 251, 366, 304, 282, 738, 675, 410, 389, 244, 31, 121, 303, 263 
        };

        protected static string[] m_alphaPatternTable = 
        { 
            "urA", "xfs", "ypy", "unk", "xdw", "yoz", "pDA", "uls", "pBk", "eBA",
            "pAs", "eAk", "prA", "uvs", "xhy", "pnk", "utw", "xgz", "fDA", "pls", "fBk", "frA", "pvs",
            "uxy", "fnk", "ptw", "uwz", "fls", "psy", "fvs", "pxy", "ftw", "pwz", "fxy", "yrx", "ufk",
            "xFw", "ymz", "onA", "uds", "xEy", "olk", "ucw", "dBA", "oks", "uci", "dAk", "okg", "dAc",
            "ovk", "uhw", "xaz", "dnA", "ots", "ugy", "dlk", "osw", "ugj", "dks", "osi", "dvk", "oxw",
            "uiz", "dts", "owy", "dsw", "owj", "dxw", "oyz", "dwy", "dwj", "ofA", "uFs", "xCy", "odk",
            "uEw", "xCj", "clA", "ocs", "uEi", "ckk", "ocg", "ckc", "ckE", "cvA", "ohs", "uay", "ctk",
            "ogw", "uaj", "css", "ogi", "csg", "csa", "cxs", "oiy", "cww", "oij", "cwi", "cyy", "oFk",
            "uCw", "xBj", "cdA", "oEs", "uCi", "cck", "oEg", "uCb", "ccc", "oEa", "ccE", "oED", "chk",
            "oaw", "uDj", "cgs", "oai", "cgg", "oab", "cga", "cgD", "obj", "cib", "cFA", "oCs", "uBi",
            "cEk", "oCg", "uBb", "cEc", "oCa", "cEE", "oCD", "cEC", "cas", "cag", "caa", "cCk", "uAr",
            "oBa", "oBD", "cCB", "tfk", "wpw", "yez", "mnA", "tds", "woy", "mlk", "tcw", "woj", "FBA",
            "mks", "FAk", "mvk", "thw", "wqz", "FnA", "mts", "tgy", "Flk", "msw", "Fks", "Fkg", "Fvk",
            "mxw", "tiz", "Fts", "mwy", "Fsw", "Fsi", "Fxw", "myz", "Fwy", "Fyz", "vfA", "xps", "yuy",
            "vdk", "xow", "yuj", "qlA", "vcs", "xoi", "qkk", "vcg", "xob", "qkc", "vca", "mfA", "tFs",
            "wmy", "qvA", "mdk", "tEw", "wmj", "qtk", "vgw", "xqj", "hlA", "Ekk", "mcg", "tEb", "hkk",
            "qsg", "hkc", "EvA", "mhs", "tay", "hvA", "Etk", "mgw", "taj", "htk", "qww", "vij", "hss",
            "Esg", "hsg", "Exs", "miy", "hxs", "Eww", "mij", "hww", "qyj", "hwi", "Eyy", "hyy", "Eyj",
            "hyj", "vFk", "xmw", "ytj", "qdA", "vEs", "xmi", "qck", "vEg", "xmb", "qcc", "vEa", "qcE",
            "qcC", "mFk", "tCw", "wlj", "qhk", "mEs", "tCi", "gtA", "Eck", "vai", "tCb", "gsk", "Ecc",
            "mEa", "gsc", "qga", "mED", "EcC", "Ehk", "maw", "tDj", "gxk", "Egs", "mai", "gws", "qii",
            "mab", "gwg", "Ega", "EgD", "Eiw", "mbj", "gyw", "Eii", "gyi", "Eib", "gyb", "gzj", "qFA",
            "vCs", "xli", "qEk", "vCg", "xlb", "qEc", "vCa", "qEE", "vCD", "qEC", "qEB", "EFA", "mCs",
            "tBi", "ghA", "EEk", "mCg", "tBb", "ggk", "qag", "vDb", "ggc", "EEE", "mCD", "ggE", "qaD",
            "ggC", "Eas", "mDi", "gis", "Eag", "mDb", "gig", "qbb", "gia", "EaD", "giD", "gji", "gjb",
            "qCk", "vBg", "xkr", "qCc", "vBa", "qCE", "vBD", "qCC", "qCB", "ECk", "mBg", "tAr", "gak",
            "ECc", "mBa", "gac", "qDa", "mBD", "gaE", "ECC", "gaC", "ECB", "EDg", "gbg", "gba", "gbD",
            "vAq", "vAn", "qBB", "mAq", "EBE", "gDE", "gDC", "gDB", "lfA", "sps", "wey", "ldk", "sow",
            "ClA", "lcs", "soi", "Ckk", "lcg", "Ckc", "CkE", "CvA", "lhs", "sqy", "Ctk", "lgw", "sqj",
            "Css", "lgi", "Csg", "Csa", "Cxs", "liy", "Cww", "lij", "Cwi", "Cyy", "Cyj", "tpk", "wuw",
            "yhj", "ndA", "tos", "wui", "nck", "tog", "wub", "ncc", "toa", "ncE", "toD", "lFk", "smw",
            "wdj", "nhk", "lEs", "smi", "atA", "Cck", "tqi", "smb", "ask", "ngg", "lEa", "asc", "CcE",
            "asE", "Chk", "law", "snj", "axk", "Cgs", "trj", "aws", "nii", "lab", "awg", "Cga", "awa",
            "Ciw", "lbj", "ayw", "Cii", "ayi", "Cib", "Cjj", "azj", "vpA", "xus", "yxi", "vok", "xug",
            "yxb", "voc", "xua", "voE", "xuD", "voC", "nFA", "tms", "wti", "rhA", "nEk", "xvi", "wtb",
            "rgk", "vqg", "xvb", "rgc", "nEE", "tmD", "rgE", "vqD", "nEB", "CFA", "lCs", "sli", "ahA",
            "CEk", "lCg", "slb", "ixA", "agk", "nag", "tnb", "iwk", "rig", "vrb", "lCD", "iwc", "agE",
            "naD", "iwE", "CEB", "Cas", "lDi", "ais", "Cag", "lDb", "iys", "aig", "nbb", "iyg", "rjb",
            "CaD", "aiD", "Cbi", "aji", "Cbb", "izi", "ajb", "vmk", "xtg", "ywr", "vmc", "xta", "vmE",
            "xtD", "vmC", "vmB", "nCk", "tlg", "wsr", "rak", "nCc", "xtr", "rac", "vna", "tlD", "raE",
            "nCC", "raC", "nCB", "raB", "CCk", "lBg", "skr", "aak", "CCc", "lBa", "iik", "aac", "nDa",
            "lBD", "iic", "rba", "CCC", "iiE", "aaC", "CCB", "aaB", "CDg", "lBr", "abg", "CDa", "ijg",
            "aba", "CDD", "ija", "abD", "CDr", "ijr", "vlc", "xsq", "vlE", "xsn", "vlC", "vlB", "nBc",
            "tkq", "rDc", "nBE", "tkn", "rDE", "vln", "rDC", "nBB", "rDB", "CBc", "lAq", "aDc", "CBE",
            "lAn", "ibc", "aDE", "nBn", "ibE", "rDn", "CBB", "ibC", "aDB", "ibB", "aDq", "ibq", "ibn",
            "xsf", "vkl", "tkf", "nAm", "nAl", "CAo", "aBo", "iDo", "CAl", "aBl", "kpk", "BdA", "kos",
            "Bck", "kog", "seb", "Bcc", "koa", "BcE", "koD", "Bhk", "kqw", "sfj", "Bgs", "kqi", "Bgg",
            "kqb", "Bga", "BgD", "Biw", "krj", "Bii", "Bib", "Bjj", "lpA", "sus", "whi", "lok", "sug",
            "loc", "sua", "loE", "suD", "loC", "BFA", "kms", "sdi", "DhA", "BEk", "svi", "sdb", "Dgk",
            "lqg", "svb", "Dgc", "BEE", "kmD", "DgE", "lqD", "BEB", "Bas", "kni", "Dis", "Bag", "knb",
            "Dig", "lrb", "Dia", "BaD", "Bbi", "Dji", "Bbb", "Djb", "tuk", "wxg", "yir", "tuc", "wxa",
            "tuE", "wxD", "tuC", "tuB", "lmk", "stg", "nqk", "lmc", "sta", "nqc", "tva", "stD", "nqE",
            "lmC", "nqC", "lmB", "nqB", "BCk", "klg", "Dak", "BCc", "str", "bik", "Dac", "lna", "klD",
            "bic", "nra", "BCC", "biE", "DaC", "BCB", "DaB", "BDg", "klr", "Dbg", "BDa", "bjg", "Dba",
            "BDD", "bja", "DbD", "BDr", "Dbr", "bjr", "xxc", "yyq", "xxE", "yyn", "xxC", "xxB", "ttc",
            "wwq", "vvc", "xxq", "wwn", "vvE", "xxn", "vvC", "ttB", "vvB", "llc", "ssq", "nnc", "llE",
            "ssn", "rrc", "nnE", "ttn", "rrE", "vvn", "llB", "rrC", "nnB", "rrB", "BBc", "kkq", "DDc",
            "BBE", "kkn", "bbc", "DDE", "lln", "jjc", "bbE", "nnn", "BBB", "jjE", "rrn", "DDB", "jjC",
            "BBq", "DDq", "BBn", "bbq", "DDn", "jjq", "bbn", "jjn", "xwo", "yyf", "xwm", "xwl", "tso",
            "wwf", "vto", "xwv", "vtm", "tsl", "vtl", "lko", "ssf", "nlo", "lkm", "rno", "nlm", "lkl",
            "rnm", "nll", "rnl", "BAo", "kkf", "DBo", "lkv", "bDo", "DBm", "BAl", "jbo", "bDm", "DBl",
            "jbm", "bDl", "jbl", "DBv", "jbv", "xwd", "vsu", "vst", "nku", "rlu", "rlt", "DAu", "bBu",
            "jDu", "jDt", "ApA", "Aok", "keg", "Aoc", "AoE", "AoC", "Aqs", "Aqg", "Aqa", "AqD", "Ari",
            "Arb", "kuk", "kuc", "sha", "kuE", "shD", "kuC", "kuB", "Amk", "kdg", "Bqk", "kvg", "kda",
            "Bqc", "kva", "BqE", "kvD", "BqC", "AmB", "BqB", "Ang", "kdr", "Brg", "kvr", "Bra", "AnD",
            "BrD", "Anr", "Brr", "sxc", "sxE", "sxC", "sxB", "ktc", "lvc", "sxq", "sgn", "lvE", "sxn",
            "lvC", "ktB", "lvB", "Alc", "Bnc", "AlE", "kcn", "Drc", "BnE", "AlC", "DrE", "BnC", "AlB",
            "DrC", "BnB", "Alq", "Bnq", "Aln", "Drq", "Bnn", "Drn", "wyo", "wym", "wyl", "swo", "txo",
            "wyv", "txm", "swl", "txl", "kso", "sgf", "lto", "swv", "nvo", "ltm", "ksl", "nvm", "ltl",
            "nvl", "Ako", "kcf", "Blo", "ksv", "Dno", "Blm", "Akl", "bro", "Dnm", "Bll", "brm", "Dnl",
            "Akv", "Blv", "Dnv", "brv", "yze", "yzd", "wye", "xyu", "wyd", "xyt", "swe", "twu", "swd",
            "vxu", "twt", "vxt", "kse", "lsu", "ksd", "ntu", "lst", "rvu", "ypk", "zew", "xdA", "yos",
            "zei", "xck", "yog", "zeb", "xcc", "yoa", "xcE", "yoD", "xcC", "xhk", "yqw", "zfj", "utA",
            "xgs", "yqi", "usk", "xgg", "yqb", "usc", "xga", "usE", "xgD", "usC", "uxk", "xiw", "yrj",
            "ptA", "uws", "xii", "psk", "uwg", "xib", "psc", "uwa", "psE", "uwD", "psC", "pxk", "uyw",
            "xjj", "ftA", "pws", "uyi", "fsk", "pwg", "uyb", "fsc", "pwa", "fsE", "pwD", "fxk", "pyw",
            "uzj", "fws", "pyi", "fwg", "pyb", "fwa", "fyw", "pzj", "fyi", "fyb", "xFA", "yms", "zdi",
            "xEk", "ymg", "zdb", "xEc", "yma", "xEE", "ymD", "xEC", "xEB", "uhA", "xas", "yni", "ugk",
            "xag", "ynb", "ugc", "xaa", "ugE", "xaD", "ugC", "ugB", "oxA", "uis", "xbi", "owk", "uig",
            "xbb", "owc", "uia", "owE", "uiD", "owC", "owB", "dxA", "oys", "uji", "dwk", "oyg", "ujb",
            "dwc", "oya", "dwE", "oyD", "dwC", "dys", "ozi", "dyg", "ozb", "dya", "dyD", "dzi", "dzb",
            "xCk", "ylg", "zcr", "xCc", "yla", "xCE", "ylD", "xCC", "xCB", "uak", "xDg", "ylr", "uac",
            "xDa", "uaE", "xDD", "uaC", "uaB", "oik", "ubg", "xDr", "oic", "uba", "oiE", "ubD", "oiC",
            "oiB", "cyk", "ojg", "ubr", "cyc", "oja", "cyE", "ojD", "cyC", "cyB", "czg", "ojr", "cza",
            "czD", "czr", "xBc", "ykq", "xBE", "ykn", "xBC", "xBB", "uDc", "xBq", "uDE", "xBn", "uDC",
            "uDB", "obc", "uDq", "obE", "uDn", "obC", "obB", "cjc", "obq", "cjE", "obn", "cjC", "cjB",
            "cjq", "cjn", "xAo", "ykf", "xAm", "xAl", "uBo", "xAv", "uBm", "uBl", "oDo", "uBv", "oDm",
            "oDl", "cbo", "oDv", "cbm", "cbl", "xAe", "xAd", "uAu", "uAt", "oBu", "oBt", "wpA", "yes",
            "zFi", "wok", "yeg", "zFb", "woc", "yea", "woE", "yeD", "woC", "woB", "thA", "wqs", "yfi",
            "tgk", "wqg", "yfb", "tgc", "wqa", "tgE", "wqD", "tgC", "tgB", "mxA", "tis", "wri", "mwk",
            "tig", "wrb", "mwc", "tia", "mwE", "tiD", "mwC", "mwB", "FxA", "mys", "tji", "Fwk", "myg",
            "tjb", "Fwc", "mya", "FwE", "myD", "FwC", "Fys", "mzi", "Fyg", "mzb", "Fya", "FyD", "Fzi",
            "Fzb", "yuk", "zhg", "hjs", "yuc", "zha", "hbw", "yuE", "zhD", "hDy", "yuC", "yuB", "wmk",
            "ydg", "zEr", "xqk", "wmc", "zhr", "xqc", "yva", "ydD", "xqE", "wmC", "xqC", "wmB", "xqB",
            "tak", "wng", "ydr", "vik", "tac", "wna", "vic", "xra", "wnD", "viE", "taC", "viC", "taB",
            "viB", "mik", "tbg", "wnr", "qyk", "mic", "tba", "qyc", "vja", "tbD", "qyE", "miC", "qyC",
            "miB", "qyB", "Eyk", "mjg", "tbr", "hyk", "Eyc", "mja", "hyc", "qza", "mjD", "hyE", "EyC",
            "hyC", "EyB", "Ezg", "mjr", "hzg", "Eza", "hza", "EzD", "hzD", "Ezr", "ytc", "zgq", "grw",
            "ytE", "zgn", "gny", "ytC", "glz", "ytB", "wlc", "ycq", "xnc", "wlE", "ycn", "xnE", "ytn",
            "xnC", "wlB", "xnB", "tDc", "wlq", "vbc", "tDE", "wln", "vbE", "xnn", "vbC", "tDB", "vbB",
            "mbc", "tDq", "qjc", "mbE", "tDn", "qjE", "vbn", "qjC", "mbB", "qjB", "Ejc", "mbq", "gzc",
            "EjE", "mbn", "gzE", "qjn", "gzC", "EjB", "gzB", "Ejq", "gzq", "Ejn", "gzn", "yso", "zgf",
            "gfy", "ysm", "gdz", "ysl", "wko", "ycf", "xlo", "ysv", "xlm", "wkl", "xll", "tBo", "wkv",
            "vDo", "tBm", "vDm", "tBl", "vDl", "mDo", "tBv", "qbo", "vDv", "qbm", "mDl", "qbl", "Ebo",
            "mDv", "gjo", "Ebm", "gjm", "Ebl", "gjl", "Ebv", "gjv", "yse", "gFz", "ysd", "wke", "xku",
            "wkd", "xkt", "tAu", "vBu", "tAt", "vBt", "mBu", "qDu", "mBt", "qDt", "EDu", "gbu", "EDt",
            "gbt", "ysF", "wkF", "xkh", "tAh", "vAx", "mAx", "qBx", "wek", "yFg", "zCr", "wec", "yFa",
            "weE", "yFD", "weC", "weB", "sqk", "wfg", "yFr", "sqc", "wfa", "sqE", "wfD", "sqC", "sqB",
            "lik", "srg", "wfr", "lic", "sra", "liE", "srD", "liC", "liB", "Cyk", "ljg", "srr", "Cyc",
            "lja", "CyE", "ljD", "CyC", "CyB", "Czg", "ljr", "Cza", "CzD", "Czr", "yhc", "zaq", "arw",
            "yhE", "zan", "any", "yhC", "alz", "yhB", "wdc", "yEq", "wvc", "wdE", "yEn", "wvE", "yhn",
            "wvC", "wdB", "wvB", "snc", "wdq", "trc", "snE", "wdn", "trE", "wvn", "trC", "snB", "trB",
            "lbc", "snq", "njc", "lbE", "snn", "njE", "trn", "njC", "lbB", "njB", "Cjc", "lbq", "azc",
            "CjE", "lbn", "azE", "njn", "azC", "CjB", "azB", "Cjq", "azq", "Cjn", "azn", "zio", "irs",
            "rfy", "zim", "inw", "rdz", "zil", "ily", "ikz", "ygo", "zaf", "afy", "yxo", "ziv", "ivy",
            "adz", "yxm", "ygl", "itz", "yxl", "wco", "yEf", "wto", "wcm", "xvo", "yxv", "wcl", "xvm",
            "wtl", "xvl", "slo", "wcv", "tno", "slm", "vro", "tnm", "sll", "vrm", "tnl", "vrl", "lDo",
            "slv", "nbo", "lDm", "rjo", "nbm", "lDl", "rjm", "nbl", "rjl", "Cbo", "lDv", "ajo", "Cbm",
            "izo", "ajm", "Cbl", "izm", "ajl", "izl", "Cbv", "ajv", "zie", "ifw", "rFz", "zid", "idy",
            "icz", "yge", "aFz", "ywu", "ygd", "ihz", "ywt", "wce", "wsu", "wcd", "xtu", "wst", "xtt",
            "sku", "tlu", "skt", "vnu", "tlt", "vnt", "lBu", "nDu", "lBt", "rbu", "nDt", "rbt", "CDu",
            "abu", "CDt", "iju", "abt", "ijt", "ziF", "iFy", "iEz", "ygF", "ywh", "wcF", "wsh", "xsx",
            "skh", "tkx", "vlx", "lAx", "nBx", "rDx", "CBx", "aDx", "ibx", "iCz", "wFc", "yCq", "wFE",
            "yCn", "wFC", "wFB", "sfc", "wFq", "sfE", "wFn", "sfC", "sfB", "krc", "sfq", "krE", "sfn",
            "krC", "krB", "Bjc", "krq", "BjE", "krn", "BjC", "BjB", "Bjq", "Bjn", "yao", "zDf", "Dfy",
            "yam", "Ddz", "yal", "wEo", "yCf", "who", "wEm", "whm", "wEl", "whl", "sdo", "wEv", "svo",
            "sdm", "svm", "sdl", "svl", "kno", "sdv", "lro", "knm", "lrm", "knl", "lrl", "Bbo", "knv",
            "Djo", "Bbm", "Djm", "Bbl", "Djl", "Bbv", "Djv", "zbe", "bfw", "npz", "zbd", "bdy", "bcz",
            "yae", "DFz", "yiu", "yad", "bhz", "yit", "wEe", "wgu", "wEd", "wxu", "wgt", "wxt", "scu",
            "stu", "sct", "tvu", "stt", "tvt", "klu", "lnu", "klt", "nru", "lnt", "nrt", "BDu", "Dbu",
            "BDt", "bju", "Dbt", "bjt", "jfs", "rpy", "jdw", "roz", "jcy", "jcj", "zbF", "bFy", "zjh",
            "jhy", "bEz", "jgz", "yaF", "yih", "yyx", "wEF", "wgh", "wwx", "xxx", "sch", "ssx", "ttx",
            "vvx", "kkx", "llx", "nnx", "rrx", "BBx", "DDx", "bbx", "jFw", "rmz", "jEy", "jEj", "bCz",
            "jaz", "jCy", "jCj", "jBj", "wCo", "wCm", "wCl", "sFo", "wCv", "sFm", "sFl", "kfo", "sFv",
            "kfm", "kfl", "Aro", "kfv", "Arm", "Arl", "Arv", "yDe", "Bpz", "yDd", "wCe", "wau", "wCd",
            "wat", "sEu", "shu", "sEt", "sht", "kdu", "kvu", "kdt", "kvt", "Anu", "Bru", "Ant", "Brt",
            "zDp", "Dpy", "Doz", "yDF", "ybh", "wCF", "wah", "wix", "sEh", "sgx", "sxx", "kcx", "ktx",
            "lvx", "Alx", "Bnx", "Drx", "bpw", "nuz", "boy", "boj", "Dmz", "bqz", "jps", "ruy", "jow",
            "ruj", "joi", "job", "bmy", "jqy", "bmj", "jqj", "jmw", "rtj", "jmi", "jmb", "blj", "jnj",
            "jli", "jlb", "jkr", "sCu", "sCt", "kFu", "kFt", "Afu", "Aft", "wDh", "sCh", "sax", "kEx",
            "khx", "Adx", "Avx", "Buz", "Duy", "Duj", "buw", "nxj", "bui", "bub", "Dtj", "bvj", "jus",
            "rxi", "jug", "rxb", "jua", "juD", "bti", "jvi", "btb", "jvb", "jtg", "rwr", "jta", "jtD",
            "bsr", "jtr", "jsq", "jsn", "Bxj", "Dxi", "Dxb", "bxg", "nyr", "bxa", "bxD", "Dwr", "bxr",
            "bwq", "bwn", "pjk", "urw", "ejA", "pbs", "uny", "ebk", "pDw", "ulz", "eDs", "pBy", "eBw",
            "zfc", "fjk", "prw", "zfE", "fbs", "pny", "zfC", "fDw", "plz", "zfB", "fBy", "yrc", "zfq",
            "frw", "yrE", "zfn", "fny", "yrC", "flz", "yrB", "xjc", "yrq", "xjE", "yrn", "xjC", "xjB",
            "uzc", "xjq", "uzE", "xjn", "uzC", "uzB", "pzc", "uzq", "pzE", "uzn", "pzC", "djA", "ors",
            "ufy", "dbk", "onw", "udz", "dDs", "oly", "dBw", "okz", "dAy", "zdo", "drs", "ovy", "zdm",
            "dnw", "otz", "zdl", "dly", "dkz", "yno", "zdv", "dvy", "ynm", "dtz", "ynl", "xbo", "ynv",
            "xbm", "xbl", "ujo", "xbv", "ujm", "ujl", "ozo", "ujv", "ozm", "ozl", "crk", "ofw", "uFz",
            "cns", "ody", "clw", "ocz", "cky", "ckj", "zcu", "cvw", "ohz", "zct", "cty", "csz", "ylu",
            "cxz", "ylt", "xDu", "xDt", "ubu", "ubt", "oju", "ojt", "cfs", "oFy", "cdw", "oEz", "ccy",
            "ccj", "zch", "chy", "cgz", "ykx", "xBx", "uDx", "cFw", "oCz", "cEy", "cEj", "caz", "cCy",
            "cCj", "FjA", "mrs", "tfy", "Fbk", "mnw", "tdz", "FDs", "mly", "FBw", "mkz", "FAy", "zFo",
            "Frs", "mvy", "zFm", "Fnw", "mtz", "zFl", "Fly", "Fkz", "yfo", "zFv", "Fvy", "yfm", "Ftz",
            "yfl", "wro", "yfv", "wrm", "wrl", "tjo", "wrv", "tjm", "tjl", "mzo", "tjv", "mzm", "mzl",
            "qrk", "vfw", "xpz", "hbA", "qns", "vdy", "hDk", "qlw", "vcz", "hBs", "qky", "hAw", "qkj",
            "hAi", "Erk", "mfw", "tFz", "hrk", "Ens", "mdy", "hns", "qty", "mcz", "hlw", "Eky", "hky",
            "Ekj", "hkj", "zEu", "Evw", "mhz", "zhu", "zEt", "hvw", "Ety", "zht", "hty", "Esz", "hsz",
            "ydu", "Exz", "yvu", "ydt", "hxz", "yvt", "wnu", "xru", "wnt", "xrt", "tbu", "vju", "tbt",
            "vjt", "mju", "mjt", "grA", "qfs", "vFy", "gnk", "qdw", "vEz", "gls", "qcy", "gkw", "qcj",
            "gki", "gkb", "Efs", "mFy", "gvs", "Edw", "mEz", "gtw", "qgz", "gsy", "Ecj", "gsj", "zEh",
            "Ehy", "zgx", "gxy", "Egz", "gwz", "ycx", "ytx", "wlx", "xnx", "tDx", "vbx", "mbx", "gfk",
            "qFw", "vCz", "gds", "qEy", "gcw", "qEj", "gci", "gcb", "EFw", "mCz", "ghw", "EEy", "ggy",
            "EEj", "ggj", "Eaz", "giz", "gFs", "qCy", "gEw", "qCj", "gEi", "gEb", "ECy", "gay", "ECj",
            "gaj", "gCw", "qBj", "gCi", "gCb", "EBj", "gDj", "gBi", "gBb", "Crk", "lfw", "spz", "Cns",
            "ldy", "Clw", "lcz", "Cky", "Ckj", "zCu", "Cvw", "lhz", "zCt", "Cty", "Csz", "yFu", "Cxz",
            "yFt", "wfu", "wft", "sru", "srt", "lju", "ljt", "arA", "nfs", "tpy", "ank", "ndw", "toz",
            "als", "ncy", "akw", "ncj", "aki", "akb", "Cfs", "lFy", "avs", "Cdw", "lEz", "atw", "ngz",
            "asy", "Ccj", "asj", "zCh", "Chy", "zax", "axy", "Cgz", "awz", "yEx", "yhx", "wdx", "wvx",
            "snx", "trx", "lbx", "rfk", "vpw", "xuz", "inA", "rds", "voy", "ilk", "rcw", "voj", "iks",
            "rci", "ikg", "rcb", "ika", "afk", "nFw", "tmz", "ivk", "ads", "nEy", "its", "rgy", "nEj",
            "isw", "aci", "isi", "acb", "isb", "CFw", "lCz", "ahw", "CEy", "ixw", "agy", "CEj", "iwy",
            "agj", "iwj", "Caz", "aiz", "iyz", "ifA", "rFs", "vmy", "idk", "rEw", "vmj", "ics", "rEi",
            "icg", "rEb", "ica", "icD", "aFs", "nCy", "ihs", "aEw", "nCj", "igw", "raj", "igi", "aEb",
            "igb", "CCy", "aay", "CCj", "iiy", "aaj", "iij", "iFk", "rCw", "vlj", "iEs", "rCi", "iEg",
            "rCb", "iEa", "iED", "aCw", "nBj", "iaw", "aCi", "iai", "aCb", "iab", "CBj", "aDj", "ibj",
            "iCs", "rBi", "iCg", "rBb", "iCa", "iCD", "aBi", "iDi", "aBb", "iDb", "iBg", "rAr", "iBa",
            "iBD", "aAr", "iBr", "iAq", "iAn", "Bfs", "kpy", "Bdw", "koz", "Bcy", "Bcj", "Bhy", "Bgz",
            "yCx", "wFx", "sfx", "krx", "Dfk", "lpw", "suz", "Dds", "loy", "Dcw", "loj", "Dci", "Dcb",
            "BFw", "kmz", "Dhw", "BEy", "Dgy", "BEj", "Dgj", "Baz", "Diz", "bfA", "nps", "tuy", "bdk",
            "now", "tuj", "bcs", "noi", "bcg", "nob", "bca", "bcD", "DFs", "lmy", "bhs", "DEw", "lmj",
            "bgw", "DEi", "bgi", "DEb", "bgb", "BCy", "Day", "BCj", "biy", "Daj", "bij", "rpk", "vuw",
            "xxj", "jdA", "ros", "vui", "jck", "rog", "vub", "jcc", "roa", "jcE", "roD", "jcC", "bFk",
            "nmw", "ttj", "jhk", "bEs", "nmi", "jgs", "rqi", "nmb", "jgg", "bEa", "jga", "bED", "jgD",
            "DCw", "llj", "baw", "DCi", "jiw", "bai", "DCb", "jii", "bab", "jib", "BBj", "DDj", "bbj",
            "jjj", "jFA", "rms", "vti", "jEk", "rmg", "vtb", "jEc", "rma", "jEE", "rmD", "jEC", "jEB",
            "bCs", "nli", "jas", "bCg", "nlb", "jag", "rnb", "jaa", "bCD", "jaD", "DBi", "bDi", "DBb",
            "jbi", "bDb", "jbb", "jCk", "rlg", "vsr", "jCc", "rla", "jCE", "rlD", "jCC", "jCB", "bBg",
            "nkr", "jDg", "bBa", "jDa", "bBD", "jDD", "DAr", "bBr", "jDr", "jBc", "rkq", "jBE", "rkn",
            "jBC", "jBB", "bAq", "jBq", "bAn", "jBn", "jAo", "rkf", "jAm", "jAl", "bAf", "jAv", "Apw",
            "kez", "Aoy", "Aoj", "Aqz", "Bps", "kuy", "Bow", "kuj", "Boi", "Bob", "Amy", "Bqy", "Amj",
            "Bqj", "Dpk", "luw", "sxj", "Dos", "lui", "Dog", "lub", "Doa", "DoD", "Bmw", "ktj", "Dqw",
            "Bmi", "Dqi", "Bmb", "Dqb", "Alj", "Bnj", "Drj", "bpA", "nus", "txi", "bok", "nug", "txb",
            "boc", "nua", "boE", "nuD", "boC", "boB", "Dms", "lti", "bqs", "Dmg", "ltb", "bqg", "nvb",
            "bqa", "DmD", "bqD", "Bli", "Dni", "Blb", "bri", "Dnb", "brb", "ruk", "vxg", "xyr", "ruc",
            "vxa", "ruE", "vxD", "ruC", "ruB", "bmk", "ntg", "twr", "jqk", "bmc", "nta", "jqc", "rva",
            "ntD", "jqE", "bmC", "jqC", "bmB", "jqB", "Dlg", "lsr", "bng", "Dla", "jrg", "bna", "DlD",
            "jra", "bnD", "jrD", "Bkr", "Dlr", "bnr", "jrr", "rtc", "vwq", "rtE", "vwn", "rtC", "rtB",
            "blc", "nsq", "jnc", "blE", "nsn", "jnE", "rtn", "jnC", "blB", "jnB", "Dkq", "blq", "Dkn",
            "jnq", "bln", "jnn", "rso", "vwf", "rsm", "rsl", "bko", "nsf", "jlo", "bkm", "jlm", "bkl",
            "jll", "Dkf", "bkv", "jlv", "rse", "rsd", "bke", "jku", "bkd", "jkt", "Aey", "Aej", "Auw",
            "khj", "Aui", "Aub", "Adj", "Avj", "Bus", "kxi", "Bug", "kxb", "Bua", "BuD", "Ati", "Bvi",
            "Atb", "Bvb", "Duk", "lxg", "syr", "Duc", "lxa", "DuE", "lxD", "DuC", "DuB", "Btg", "kwr",
            "Dvg", "lxr", "Dva", "BtD", "DvD", "Asr", "Btr", "Dvr", "nxc", "tyq", "nxE", "tyn", "nxC",
            "nxB", "Dtc", "lwq", "bvc", "nxq", "lwn", "bvE", "DtC", "bvC", "DtB", "bvB", "Bsq", "Dtq",
            "Bsn", "bvq", "Dtn", "bvn", "vyo", "xzf", "vym", "vyl", "nwo", "tyf", "rxo", "nwm", "rxm",
            "nwl", "rxl", "Dso", "lwf", "bto", "Dsm", "jvo", "btm", "Dsl", "jvm", "btl", "jvl", "Bsf",
            "Dsv", "btv", "jvv", "vye", "vyd", "nwe", "rwu", "nwd", "rwt", "Dse", "bsu", "Dsd", "jtu",
            "bst", "jtt", "vyF", "nwF", "rwh", "DsF", "bsh", "jsx", "Ahi", "Ahb", "Axg", "kir", "Axa",
            "AxD", "Agr", "Axr", "Bxc", "kyq", "BxE", "kyn", "BxC", "BxB", "Awq", "Bxq", "Awn", "Bxn",
            "lyo", "szf", "lym", "lyl", "Bwo", "kyf", "Dxo", "lyv", "Dxm", "Bwl", "Dxl", "Awf", "Bwv",
            "Dxv", "tze", "tzd", "lye", "nyu", "lyd", "nyt", "Bwe", "Dwu", "Bwd", "bxu", "Dwt", "bxt",
            "tzF", "lyF", "nyh", "BwF", "Dwh", "bwx", "Aiq", "Ain", "Ayo", "kjf", "Aym", "Ayl", "Aif",
            "Ayv", "kze", "kzd", "Aye", "Byu", "Ayd", "Byt", "szp" 
        };
    }
}
