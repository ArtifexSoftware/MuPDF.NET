using System;
using System.Text;
using System.Collections;

namespace BarcodeWriter.Core.Internal
{
    class PDF417MicroSymbology : PDF417Symbology
    {
        private int m_symbolVariant;

        /// <summary>
        /// Initializes a new instance of the <see cref="PDF417MicroSymbology"/> class.
        /// </summary>
        public PDF417MicroSymbology()
            : base()
        {
            m_type = TrueSymbologyType.MicroPDF417;
            Options.PDF417CreateMacro = false;
        }

        public PDF417MicroSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.MicroPDF417;
            Options.PDF417CreateMacro = false;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Micro PDF417 symbology allows a maximum data size of 250 characters (if all characters are uppercase), or 150 ASCII characters, or 366 digits.\n";
        }

        protected override void encodeData()
        {
            if (Options.PDF417CompactionMode == PDF417CompactionMode.Auto)
            {
                splitTextIntoChunks();
                optimizeChunks();
            }

            produceCodewords();

            if (m_codewords.Count > 126)
                throw new BarcodeException("Input string is too long for Micro PDF 417 barcode.");

            if (!Options.PDF417UseManualSize)
                findSymbolVariant();
            else
                findSymbolVariantForManualSize(Options.PDF417RowCount);

            m_dataColumnCount = m_microVariants[m_symbolVariant];
            int rows = m_microVariants[m_symbolVariant + 34];
            int checkCodewordsCount = m_microVariants[m_symbolVariant + 68];
            int paddingLength = (m_dataColumnCount * rows) - checkCodewordsCount - m_codewords.Count;
            if (paddingLength < 0)
                throw new BarcodeException("Input string is too long for specified number of data columns and rows.");

            int coeffOffset = m_microVariants[m_symbolVariant + 102];

            addPadding(paddingLength);
            addCheckCodewords(checkCodewordsCount, coeffOffset);

            encodeRows(rows);
        }

        protected override void splitTextIntoChunks()
        {
            // see MicroPDF spec, Annex N, Algorithm to minimise the number of codewords
            // for some hints about algorithm below
            int numThreshold = 13;
            int textTreshold = 5;

            m_chunks.Clear();

            for (int textPos = 0; textPos < m_text.Length; )
            {
                int numLength = 0;
                for (int i = textPos; i < m_text.Length; i++, numLength++)
                {
                    char c = m_text[i];
                    if (c < '0' || c > '9')
                        break;
                }

                if (numLength >= numThreshold)
                {
                    m_chunks.Add(new EncoderChunk(EncoderMode.Numeric, numLength));
                    textPos += numLength;
                    continue;
                }
                else
                {
                    int textLength = numLength;
                    numLength = 0;

                    for (int i = textPos + textLength; i < m_text.Length; i++, textLength++)
                    {
                        char c = m_text[i];
                        if (c >= '0' && c <= '9')
                            numLength++;
                        else
                            numLength = 0;

                        if (numLength >= numThreshold)
                        {
                            textLength -= numLength - 1;
                            break;
                        }

                        if ((c < ' ' || c > '~') && c != '\t' && c != '\n' && c != '\r')
                            break;
                    }

                    if (textLength >= textTreshold)
                    {
                        m_chunks.Add(new EncoderChunk(EncoderMode.Text, textLength));
                        textPos += textLength;
                        continue;
                    }
                    else
                    {
                        int byteLength = textLength;
                        textLength = 0;

                        for (int i = textPos + byteLength; i < m_text.Length; i++, byteLength++)
                        {
                            char c = m_text[i];
                            if (c >= ' ' && c <= '~')
                                textLength++;
                            else
                                textLength = 0;

                            if (textLength >= textTreshold)
                            {
                                byteLength -= textLength - 1;
                                break;
                            }
                        }

                        m_chunks.Add(new EncoderChunk(EncoderMode.Byte, byteLength));
                        textPos += byteLength;
                    }
                }
            }
        }

        private void findSymbolVariant()
        {
            if (m_dataColumnCount > 4)
                m_dataColumnCount = 4;

            m_symbolVariant = 0;

            // check if data fits specified column count.
            // if it doesn't, then reset data column count. 
            // it will be auto-detected later.

            if ((m_dataColumnCount == 1) && (m_codewords.Count > 20))
                m_dataColumnCount = 0;
            else if ((m_dataColumnCount == 2) && (m_codewords.Count > 37))
                m_dataColumnCount = 0;
            else if ((m_dataColumnCount == 3) && (m_codewords.Count > 82))
                m_dataColumnCount = 0;

            if (m_dataColumnCount == 1)
            {
                m_symbolVariant = 6;

                if (m_codewords.Count <= 16)
                    m_symbolVariant = 5;
                if (m_codewords.Count <= 12)
                    m_symbolVariant = 4;
                if (m_codewords.Count <= 10)
                    m_symbolVariant = 3;
                if (m_codewords.Count <= 7)
                    m_symbolVariant = 2;
                if (m_codewords.Count <= 4)
                    m_symbolVariant = 1;
            }
            else if (m_dataColumnCount == 2)
            {
                m_symbolVariant = 13;

                if (m_codewords.Count <= 33)
                    m_symbolVariant = 12;
                if (m_codewords.Count <= 29)
                    m_symbolVariant = 11;
                if (m_codewords.Count <= 24)
                    m_symbolVariant = 10;
                if (m_codewords.Count <= 19)
                    m_symbolVariant = 9;
                if (m_codewords.Count <= 13)
                    m_symbolVariant = 8;
                if (m_codewords.Count <= 8)
                    m_symbolVariant = 7;
            }
            else if (m_dataColumnCount == 3)
            {
                m_symbolVariant = 23;

                if (m_codewords.Count <= 70)
                    m_symbolVariant = 22;
                if (m_codewords.Count <= 58)
                    m_symbolVariant = 21;
                if (m_codewords.Count <= 46)
                    m_symbolVariant = 20;
                if (m_codewords.Count <= 34)
                    m_symbolVariant = 19;
                if (m_codewords.Count <= 24)
                    m_symbolVariant = 18;
                if (m_codewords.Count <= 18)
                    m_symbolVariant = 17;
                if (m_codewords.Count <= 14)
                    m_symbolVariant = 16;
                if (m_codewords.Count <= 10)
                    m_symbolVariant = 15;
                if (m_codewords.Count <= 6)
                    m_symbolVariant = 14;
            }
            else if (m_dataColumnCount == 4)
            {
                m_symbolVariant = 34;

                if (m_codewords.Count <= 108)
                    m_symbolVariant = 33;
                if (m_codewords.Count <= 90)
                    m_symbolVariant = 32;
                if (m_codewords.Count <= 72)
                    m_symbolVariant = 31;
                if (m_codewords.Count <= 54)
                    m_symbolVariant = 30;
                if (m_codewords.Count <= 39)
                    m_symbolVariant = 29;
                if (m_codewords.Count <= 30)
                    m_symbolVariant = 28;
                if (m_codewords.Count <= 24)
                    m_symbolVariant = 27;
                if (m_codewords.Count <= 18)
                    m_symbolVariant = 26;
                if (m_codewords.Count <= 12)
                    m_symbolVariant = 25;
                if (m_codewords.Count <= 8)
                    m_symbolVariant = 24;
            }

            if (m_symbolVariant == 0)
            {
                // no selection was made until this point.
                // so, we should auto-detect symbol variant
                for (int i = 27; i >= 0; i--)
                {
                    if (m_microAutosize[i] >= m_codewords.Count)
                        m_symbolVariant = m_microAutosize[i + 28];
                }
            }

            m_symbolVariant--;
        }

        private void findSymbolVariantForManualSize(int rowCount)
        {
            if (m_dataColumnCount > 4)
                throw new BarcodeException("Micro PDF417 barcode allows no more than 4 data columns.");

            if (m_dataColumnCount == 0 || rowCount == 0)
                throw new BarcodeException("Incorrect size for Micro PDF417 barcode.");

            m_symbolVariant = 0;

            if (m_dataColumnCount == 1)
            {
                if (rowCount == 11)
                    m_symbolVariant = 1;
                else if (rowCount == 14)
                    m_symbolVariant = 2;
                else if (rowCount == 17)
                    m_symbolVariant = 3;
                else if (rowCount == 20)
                    m_symbolVariant = 4;
                else if (rowCount == 24)
                    m_symbolVariant = 5;
                else if (rowCount == 28)
                    m_symbolVariant = 6;
                else
                    throw new BarcodeException("Invalid row count. Should be 11, 14, 17, 20, 24 or 28 when column count is 1.");
            }
            else if (m_dataColumnCount == 2)
            {
                if (rowCount == 8)
                    m_symbolVariant = 7;
                else if (rowCount == 11)
                    m_symbolVariant = 8;
                else if (rowCount == 14)
                    m_symbolVariant = 9;
                else if (rowCount == 17)
                    m_symbolVariant = 10;
                else if (rowCount == 20)
                    m_symbolVariant = 11;
                else if (rowCount == 23)
                    m_symbolVariant = 12;
                else if (rowCount == 26)
                    m_symbolVariant = 13;
                else
                    throw new BarcodeException("Invalid row count. Should be 8, 11, 14, 17, 20, 23 or 26 when column count is 2.");
            }
            else if (m_dataColumnCount == 3)
            {
                if (rowCount == 6)
                    m_symbolVariant = 14;
                else if (rowCount == 8)
                    m_symbolVariant = 15;
                else if (rowCount == 10)
                    m_symbolVariant = 16;
                else if (rowCount == 12)
                    m_symbolVariant = 17;
                else if (rowCount == 15)
                    m_symbolVariant = 18;
                else if (rowCount == 20)
                    m_symbolVariant = 19;
                else if (rowCount == 26)
                    m_symbolVariant = 20;
                else if (rowCount == 32)
                    m_symbolVariant = 21;
                else if (rowCount == 38)
                    m_symbolVariant = 22;
                else if (rowCount == 44)
                    m_symbolVariant = 23;
                else
                    throw new BarcodeException("Invalid row count. Should be 6, 8, 10, 12, 15, 20, 26, 32, 38 or 44 when column count is 3.");
            }
            else if (m_dataColumnCount == 4)
            {
                if (rowCount == 4)
                    m_symbolVariant = 24;
                else if (rowCount == 6)
                    m_symbolVariant = 25;
                else if (rowCount == 8)
                    m_symbolVariant = 26;
                else if (rowCount == 10)
                    m_symbolVariant = 27;
                else if (rowCount == 12)
                    m_symbolVariant = 28;
                else if (rowCount == 15)
                    m_symbolVariant = 29;
                else if (rowCount == 20)
                    m_symbolVariant = 30;
                else if (rowCount == 26)
                    m_symbolVariant = 31;
                else if (rowCount == 32)
                    m_symbolVariant = 32;
                else if (rowCount == 38)
                    m_symbolVariant = 33;
                else if (rowCount == 44)
                    m_symbolVariant = 34;
                else
                    throw new BarcodeException("Invalid row count. Should be 4, 6, 8, 10, 12, 15, 20, 26, 32, 38 or 44 when column count is 4.");
            }

            m_symbolVariant--;
        }

        private void addPadding(int paddingLength)
        {
            while (paddingLength > 0)
            {
                m_codewords.Add(900);
                paddingLength--;
            }
        }

        protected void addCheckCodewords(int checkCodewordsCount, int coeffOffset)
        {
            int[] rsSymbols = new int[50];
            for (int i = 0; i < 50; i++)
                rsSymbols[i] = 0;

            for (int i = 0; i < m_codewords.Count; i++)
            {
                int total = (m_codewords[i] + rsSymbols[checkCodewordsCount - 1]) % 929;
                for (int j = checkCodewordsCount - 1; j >= 0; j--)
                {
                    if (j == 0)
                        rsSymbols[j] = (929 - (total * m_microCoeffs[coeffOffset + j]) % 929) % 929;
                    else
                        rsSymbols[j] = (rsSymbols[j - 1] + 929 - (total * m_microCoeffs[coeffOffset + j]) % 929) % 929;
                }
            }

            for (int j = 0; j < checkCodewordsCount; j++)
            {
                if (rsSymbols[j] != 0)
                    rsSymbols[j] = 929 - rsSymbols[j];
            }

            for (int i = checkCodewordsCount - 1; i >= 0; i--)
                m_codewords.Add(rsSymbols[i]);
        }

        private void encodeRows(int rows)
        {
            m_encodedData = new string[rows];

            int leftRAP = m_rapTable[m_symbolVariant];
            int centerRAP = m_rapTable[m_symbolVariant + 34];
            int rightRAP = m_rapTable[m_symbolVariant + 68];

            // cluster can be 0, 1 or 2 for cluster(0), cluster(3) and cluster(6)
            int cluster = m_rapTable[m_symbolVariant + 102] / 3;

            for (int row = 0; row < rows; row++)
            {
                string alphaPattern = buildAlphaPattern(row, leftRAP, centerRAP, rightRAP, cluster);
                m_encodedData[row] = buildPattern(alphaPattern);

                leftRAP++;
                if (leftRAP == 53)
                    leftRAP = 1;

                centerRAP++;
                if (centerRAP == 53)
                    centerRAP = 1;

                rightRAP++;
                if (rightRAP == 53)
                    rightRAP = 1;

                cluster++;
                if (cluster == 3)
                    cluster = 0;
            }
        }

        private string buildAlphaPattern(int row, int leftRAP, int centerRAP, int rightRAP, int cluster)
        {
            int offset = 929 * cluster;

            int[] dummy = new int[5];
            for (int i = 0; i < m_dataColumnCount; i++)
                dummy[i + 1] = m_codewords[row * m_dataColumnCount + i];

            StringBuilder pattern = new StringBuilder();
            pattern.Append(m_leftRightRAP[leftRAP]);
            pattern.Append("1");
            pattern.Append(m_alphaPatternTable[offset + dummy[1]]);
            pattern.Append("1");

            if (m_dataColumnCount == 3)
                pattern.Append(m_centerRAP[centerRAP]);

            if (m_dataColumnCount >= 2)
            {
                pattern.Append("1");
                pattern.Append(m_alphaPatternTable[offset + dummy[2]]);
                pattern.Append("1");
            }

            if (m_dataColumnCount == 4)
                pattern.Append(m_centerRAP[centerRAP]);

            if (m_dataColumnCount >= 3)
            {
                pattern.Append("1");
                pattern.Append(m_alphaPatternTable[offset + dummy[3]]);
                pattern.Append("1");
            }

            if (m_dataColumnCount == 4)
            {
                pattern.Append("1");
                pattern.Append(m_alphaPatternTable[offset + dummy[4]]);
                pattern.Append("1");
            }

            pattern.Append(m_leftRightRAP[rightRAP]);
            pattern.Append("1");
            return pattern.ToString();
        }

        private static string buildPattern(string alphaPattern)
        {
            bool setOne = true;
            StringBuilder pattern = new StringBuilder();
            for (int loop = 0; loop < alphaPattern.Length; loop++)
            {
                if ((alphaPattern[loop] >= '0') && (alphaPattern[loop] <= '9'))
                {
                    for (int k = 0; k < ctoi(alphaPattern[loop]); k++)
                    {
                        if (setOne)
                            pattern.Append('1');
                        else
                            pattern.Append('0');
                    }

                    setOne = !setOne;
                }
                else
                {
                    for (int i = 0; i < m_patternAlphabet.Length; i++)
                    {
                        if (alphaPattern[loop] == m_patternAlphabet[i])
                            pattern.Append(m_barsForAlpha[i]);
                    }
                }
            }

            return pattern.ToString();
        }

        //////////////////////////////////////////////////////////////////////////
        //
        //  Data part
        //
        //////////////////////////////////////////////////////////////////////////

        // Automatic sizing table
        static int[] m_microAutosize =
        {	
            4, 6, 7, 8, 10, 12, 13, 14, 16, 18, 19, 20, 24, 29, 30, 33, 34, 37, 
            39, 46, 54, 58, 70, 72, 82, 90, 108, 126, 1, 14, 2, 7, 3, 25, 8, 16, 
            5, 17, 9, 6, 10, 11, 28, 12, 19, 13, 29, 20, 30, 21, 22, 31, 23, 32,
            33, 34
        };

        // rows, columns, check codewords, coefficiet offset of valid MicroPDF417
        // sizes from ISO/IEC 24728:2006
        static int[] m_microVariants =
        {
            1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 11, 14, 17, 20, 24, 28, 8, 11, 14,
            17, 20, 23, 26, 6, 8, 10, 12, 15, 20, 26, 32, 38, 44, 4, 6, 8, 10,
            12, 15, 20, 26, 32, 38, 44, 7, 7, 7, 8, 8, 8, 8, 9, 9, 10, 11, 13,
            15, 12, 14, 16, 18, 21, 26, 32, 38, 44, 50, 8, 12, 14, 16, 18, 21,
            26, 32, 38, 44, 50, 0, 0, 0, 7, 7, 7, 7, 15, 15, 24, 34, 57, 84, 45,
            70, 99, 115, 133, 154, 180, 212, 250, 294, 7, 45, 70, 99, 115, 133,
            154, 180, 212, 250, 294
        };

        // MicroPDF417 coefficients from ISO/IEC 24728:2006 Annex F
        static int[] m_microCoeffs =
        {
	        /* k = 7 */
	        76, 925, 537, 597, 784, 691, 437,
	        /* k = 8 */
	        237, 308, 436, 284, 646, 653, 428, 379,
	        /* k = 9 */
	        567, 527, 622, 257, 289, 362, 501, 441, 205,
	        /* k = 10 */
	        377, 457, 64, 244, 826, 841, 818, 691, 266, 612,
	        /* k = 11 */
	        462, 45, 565, 708, 825, 213, 15, 68, 327, 602, 904,
	        /* k = 12 */
	        597, 864, 757, 201, 646, 684, 347, 127, 388, 7, 69, 851,
	        /* k = 13 */
	        764, 713, 342, 384, 606, 583, 322, 592, 678, 204, 184, 394, 692,
	        /* k = 14 */
	        669, 677, 154, 187, 241, 286, 274, 354, 478, 915, 691, 833, 105, 215,
	        /* k = 15 */
	        460, 829, 476, 109, 904, 664, 230, 5, 80, 74, 550, 575, 147, 868, 642,
	        /* k = 16 */
	        274, 562, 232, 755, 599, 524, 801, 132, 295, 116, 442, 428, 295, 42, 
            176, 65,
	        /* k = 18 */
	        279, 577, 315, 624, 37, 855, 275, 739, 120, 297, 312, 202, 560, 321, 
            233, 756, 760, 573,
	        /* k = 21 */
	        108, 519, 781, 534, 129, 425, 681, 553, 422, 716, 763, 693, 624, 610,
            310, 691, 347, 165, 193, 259, 568,
	        /* k = 26 */
	        443, 284, 887, 544, 788, 93, 477, 760, 331, 608, 269, 121, 159, 830,
            446, 893, 699, 245, 441, 454, 325, 858, 131, 847, 764, 169,
	        /* k = 32 */
	        361, 575, 922, 525, 176, 586, 640, 321, 536, 742, 677, 742, 687, 284,
            193, 517, 273, 494, 263, 147, 593, 800, 571, 320, 803, 133, 231, 390,
            685, 330, 63, 410,
	        /* k = 38 */
	        234, 228, 438, 848, 133, 703, 529, 721, 788, 322, 280, 159, 738, 586,
            388, 684, 445, 680, 245, 595, 614, 233, 812, 32, 284, 658, 745, 229,
            95, 689, 920, 771, 554, 289, 231, 125, 117, 518,
	        /* k = 44 */
	        476, 36, 659, 848, 678, 64, 764, 840, 157, 915, 470, 876, 109, 25,
            632, 405, 417, 436, 714, 60, 376, 97, 413, 706, 446, 21, 3, 773, 569,
            267, 272, 213, 31, 560, 231, 758, 103, 271, 572, 436, 339, 730, 82, 285,
	        /* k = 50 */
	        923, 797, 576, 875, 156, 706, 63, 81, 257, 874, 411, 416, 778, 50,
            205, 303, 188, 535, 909, 155, 637, 230, 534, 96, 575, 102, 264, 233,
            919, 593, 865, 26, 579, 623, 766, 146, 10, 739, 246, 127, 71, 244,
            211, 477, 920, 876, 427, 820, 718, 435
        };

        // Left RAP (Row Address Pattern), Center RAP, Right RAP and Start Cluster
        // from ISO/IEC 24728:2006 tables 10, 11 and 12
        static int[] m_rapTable =
        {
            1, 8, 36, 19, 9, 25, 1, 1, 8, 36, 19, 9, 27, 1, 7, 15, 25, 37, 1, 1,
            21, 15, 1, 47, 1, 7, 15, 25, 37, 1, 1, 21, 15, 1, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 1, 7, 15, 25, 37, 17, 9, 29, 31, 25, 19, 1, 7,
            15, 25, 37, 17, 9, 29, 31, 25, 9, 8, 36, 19, 17, 33, 1, 9, 8, 36, 19,
            17, 35, 1, 7, 15, 25, 37, 33, 17, 37, 47, 49, 43, 1, 7, 15, 25, 37,
            33, 17, 37, 47, 49, 0, 3, 6, 0, 6, 0, 0, 0, 3, 6, 0, 6, 6, 0, 0, 6,
            0, 0, 0, 0, 6, 6, 0, 3, 0, 0, 6, 0, 0, 0, 0, 6, 6, 0
        };

        // Left and Right Row Address Pattern from Table 2
        static string[] m_leftRightRAP =
        {
            "", "221311", "311311", "312211", "222211", "213211", "214111",
            "223111", "313111", "322111", "412111", "421111", "331111", "241111",
            "232111", "231211", "321211", "411211", "411121", "411112", "321112",
            "312112", "311212", "311221", "311131", "311122", "311113", "221113",
            "221122", "221131", "221221", "222121", "312121", "321121", "231121",
            "231112", "222112", "213112", "212212", "212221", "212131", "212122",
            "212113", "211213", "211123", "211132", "211141", "211231", "211222",
            "211312", "211321", "211411", "212311"
        };

        // Center Row Address Pattern from Table 2
        static string[] m_centerRAP =
        {
            "", "112231", "121231", "122131", "131131", "131221", "132121",
            "141121", "141211", "142111", "133111", "132211", "131311", "122311",
            "123211", "124111", "115111", "114211", "114121", "123121", "123112",
            "122212", "122221", "121321", "121411", "112411", "113311", "113221",
            "113212", "113122", "122122", "131122", "131113", "122113", "113113",
            "112213", "112222", "112312", "112321", "111421", "111331", "111322",
            "111232", "111223", "111133", "111124", "111214", "112114", "121114",
            "121123", "121132", "112132", "112141"
        };
    }
}
