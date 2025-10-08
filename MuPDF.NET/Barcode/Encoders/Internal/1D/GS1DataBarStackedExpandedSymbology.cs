/**************************************************
 *
 Copyright (c) 2008 - 2012 Bytescout
 *
 *
**************************************************/

using System;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    class GS1DataBarStackedExpandedSymbology : GS1DataBarExpandedSymbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarStackedExpandedSymbology"/> class.
        /// </summary>
        public GS1DataBarStackedExpandedSymbology()
            : base(TrueSymbologyType.GS1_DataBar_Expanded_Stacked)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarStackedExpandedSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public GS1DataBarStackedExpandedSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.GS1_DataBar_Expanded_Stacked)
        {
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "GS1 DataBar Expanded Stacked symbology allows encoding up to 74 numeric or 41 alphabetic characters of AI Element String data.";
        }

        /// <summary>
        /// Gets or sets the number of segments in line.
        /// </summary>
        /// <value>The number of segments in line.</value>
        public int SegmentsNumber
        {
            get { return Options.GS1ExpandedStackedSegmentsNumber; }
            set { Options.GS1ExpandedStackedSegmentsNumber = value; }
        }

        private void addModulesForSign(intList modules, int[] array, bool firstSpace, bool flip)
        {
            if (flip)
                for (int i = array.Length - 1; i >= 0; i--)
                {
                    int module;
                    if (firstSpace)
                        module = ((array.Length - 1 - i) % 2 == 0) ? 0 : 1;
                    else
                        module = ((array.Length - 1 - i) % 2 == 0) ? 1 : 0;
                    for (int j = 0; j < array[i]; j++)
                        modules.Add(module);
                }
            else
                for (int i = 0; i < array.Length; i++)
                {
                    int module;
                    if (firstSpace)
                        module = (i % 2 == 0) ? 0 : 1;
                    else
                        module = (i % 2 == 0) ? 1 : 0;
                    for (int j = 0; j < array[i]; j++)
                        modules.Add(module);
                }
        }

        private void addModulesForFinderPattern(intList modules, int[] array, bool firstSpace, bool flip)
        {
            if (flip)
                for (int i = array.Length - 1; i >= 0; i--)
                {
                    int module;
                    if (firstSpace)
                        module = ((array.Length - 1 - i) % 2 == 0) ? 0 : 1;
                    else
                        module = ((array.Length - 1 - i) % 2 == 0) ? 1 : 0;

                    if (module == 0)
                    {
                        for (int j = 0; j < array[i]; j++)
                            modules.Add(module);
                    }
                    else
                    {
                        for (int j = 0; j < array[i]; j++)
                            modules.Add((modules[modules.Count - 1] == 1) ? 0 : 1);
                    }
                }
            else
                for (int i = 0; i < array.Length; i++)
                {
                    int module;
                    if (firstSpace)
                        module = (i % 2 == 0) ? 0 : 1;
                    else
                        module = (i % 2 == 0) ? 1 : 0;  
                    if (module == 0)
                    {
                        for (int j = 0; j < array[i]; j++)
                            modules.Add(module);
                    }
                    else
                    {
                        for (int j = 0; j < array[i]; j++)
                            modules.Add((modules[modules.Count - 1] == 1) ? 0 : 1);
                    }
                }
        }

        private void drowSymbolLine(intList symbol, int x, int y, int width, int height, bool firstSpace, bool flip)
        {
            if (flip)
                for (int i = symbol.Count - 1; i >= 0; i--)
                {
                    if (((symbol.Count - 1 - i) % 2 != 0) && firstSpace)
                        m_rects.Add(new SKRect(x, y, width * symbol[i]+x, height+y));
                    else if (((symbol.Count - 1 - i) % 2 == 0) && !firstSpace)
                        m_rects.Add(new SKRect(x, y, width * symbol[i]+x, height+y));
                    x += width * symbol[i];
                }
            else
                for (int i = 0; i < symbol.Count; i++)
                {
                    if ((i % 2 != 0) && firstSpace)
                        m_rects.Add(new SKRect(x, y, width * symbol[i]+x, height+y));
                    else if ((i % 2 == 0) && !firstSpace)
                        m_rects.Add(new SKRect(x, y, width * symbol[i]+x, height+y));
                    x += width * symbol[i];
                }
        }

        private void drowSeparatorLine(intList symbol, int x, int y, int width, int height, bool flip)
        {
            if (flip)
                for (int i = symbol.Count - 1; i >= 0; i--)
                {
                    if (symbol[i] == 1)
                        m_rects.Add(new SKRect(x, y, width+x, height+y));
                    x += width;
                }
            else
                for (int i = 0; i < symbol.Count; i++)
                {
                    if (symbol[i] == 1)
                        m_rects.Add(new SKRect(x, y, width+x, height+y));
                    x += width;
                }
        }

       // drawing the middle row of the separator pattern
        private void drowSeparatorLine(int length, int x, int y, int width, int height)
        {
            for (int i = 0; i < length; i++)
            {
                if ((i % 2 != 0) && (i > 3) && (i < length - 4))
                    m_rects.Add(new SKRect(x, y, width+x, height+y));
                x += width;
            }
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            int maxSignNumber = 21;
            string sValue = GetEncodedValue(false);

            // Find binary value of symbol
            string binaryValue = getBinaryValue(SegmentsNumber);
            if ((binaryValue.Length > maxSignNumber * 12) || (binaryValue.Length % 12 != 0))
                throw new BarcodeException("Incorrect value for Databar Expanded symbology");

            // Split into signs of 12 bits and find their decimal value
            intList dataSigns = new intList();
            for (int i = 0; i < binaryValue.Length / 12; i++)
            {
                string binarySign = binaryValue.Substring(i * 12, 12);
                dataSigns.Add(Convert.ToInt16(binarySign, 2));
            }

            // Find widths of sign elements
            int[][] signs_temp = new int[dataSigns.Count][];
            for (int i = 0; i < dataSigns.Count; i++)
            {
                signs_temp[i] = signValue(dataSigns[i]);
            }

            // Find check sign of symbol
            int checkSignValue = 211 * (dataSigns.Count - 3) + checkSum(signs_temp);
            int[][] signs = new int[dataSigns.Count + 1][];
            signs[0] = signValue(checkSignValue);
            for (int i = 0; i < dataSigns.Count; i++)
            {
                signs[i + 1] = signs_temp[i];
            }

            // find index for table of finder patterns sequences
            int paternIndex = (signs.Length - 1) % 2 == 0 ? (signs.Length - 2) / 2 : (signs.Length - 3) / 2;

            intList symbol = new intList();
            int rowCount = (int)Math.Ceiling((double)signs.Length / SegmentsNumber); // number of rows
            int index = 0;
            bool inversion = (SegmentsNumber > 2) && ((SegmentsNumber / 2) % 2 == 0);

            SKSize drawingSize = new SKSize();
            int x = 0;
            int y = 0;
            int height = BarHeight;
            int width = NarrowBarWidth;
            SKSize captionSize = calculateCaptionSize(canvas, font);

            for (int k = 0; k < rowCount; k++)
            {
                bool oddRow = k % 2 != 0;
                bool rowFlip = (oddRow && inversion);

                symbol = new intList();
                int finderPaternCount = 0;
                intList separatorModules = new intList();
                GS1Utils.addArray(symbol, GuardPattern, oddRow);
                addModulesForSign(separatorModules, GuardPattern, !oddRow, false);
                for (int j = 0; j < SegmentsNumber; j++)
                {
                    if (index < signs.Length)
                    {
                        if (index % 2 == 0)
                        {
                            // symbol is on the left side of finder pattern
                            addModulesForSign(separatorModules,
                                              signs[index],
                                              inversion ? (symbol.Count % 2 != 0) : (symbol.Count % 2 != 0) ^ oddRow,
                                              false);
                            GS1Utils.addArray(symbol, signs[index], false);
                            int patern = FinderPaternOrder[paternIndex][(index + 1) / 2 + 1]; // code of finder pattern
                            if (patern > 0)
                            {
                                addModulesForFinderPattern(separatorModules,
                                                           FinderPaternValues[patern - 1],
                                                           inversion ? (symbol.Count % 2 != 0) : (symbol.Count % 2 != 0) ^ oddRow,
                                                           false);
                                GS1Utils.addArray(symbol, FinderPaternValues[patern - 1], false);
                                finderPaternCount++;
                            }
                            else
                            {
                                addModulesForFinderPattern(separatorModules,
                                                           FinderPaternValues[-patern - 1],
                                                           inversion ? (symbol.Count % 2 != 0) : (symbol.Count % 2 != 0) ^ oddRow,
                                                           true);
                                GS1Utils.addArray(symbol, FinderPaternValues[-patern - 1], true);
                                finderPaternCount++;
                            }
                        }
                        else
                        {
                            // symbol is on the right side of finder pattern
                            addModulesForSign(separatorModules,
                                              signs[index],
                                              inversion ? (symbol.Count % 2 != 0) : (symbol.Count % 2 != 0) ^ oddRow,
                                              true);
                            GS1Utils.addArray(symbol, signs[index], true);
                        }

                        index++;
                    }
                    else
                        break;
                }
                GS1Utils.addArray(symbol, GuardPattern, oddRow);
                addModulesForSign(separatorModules, GuardPattern, !oddRow, false);
                for (int i = 0; i < 4; i++)
                {
                    separatorModules[i] = 0;
                    separatorModules[separatorModules.Count - 1 - i] = 0;
                }

                if ((k == rowCount - 1) && (finderPaternCount % 2 != 0) && rowFlip)
                {
                    // if the last row should be displayed mirrored
                    // but also has an odd number of finder patterns, then ...
                    separatorModules.Insert(0,0);
                    symbol[0]++;
                    oddRow = false;
                    rowFlip = false;
                }

                if (k > 0)
                {
                    // draw the bottom row of the separator pattern
                    x = 0;
                    drowSeparatorLine(separatorModules, x, y, width, width, rowFlip);
                    y += width;
                }

                // draw the k-th row of the symbol
                x = 0;
                drowSymbolLine(symbol, x, y, width, height, !oddRow, rowFlip);
                y += height;

                if (k < rowCount - 1)
                {
                    // draw top and middle rows of the separator pattern
                    x = 0;
                    drowSeparatorLine(separatorModules, x, y, width, width, rowFlip);
                    y += width;

                    x = 0;
                    drowSeparatorLine(separatorModules.Count, x, y, width, width);
                    y += width;
                }
            }
            int segmentsNumber = SegmentsNumber > dataSigns.Count + 1 ? dataSigns.Count + 1 : SegmentsNumber;
            drawingSize.Width = segmentsNumber * 17 * NarrowBarWidth
                                + (int)Math.Ceiling((double)segmentsNumber / 2) * 15 * NarrowBarWidth
                                + NarrowBarWidth * 4;
            drawingSize.Height = y + captionSize.Height / 2;
            return drawingSize;
        }
    }
}
