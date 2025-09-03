using System;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using GS1 DataBar Stacked Omnidirectional symbology rules.
    /// This symbology used within the GS1 System for encode a GTIN.
    /// </summary>
    class GS1DataBarStackedOmnidirectionalSymbology : GS1DataBarOmnidirectionalBasic
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarStackedOmnidirectionalSymbology"/> class.
        /// </summary>
        public GS1DataBarStackedOmnidirectionalSymbology()
            : base(TrueSymbologyType.GS1_DataBar_Stacked_Omnidirectional)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarStackedOmnidirectionalSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public GS1DataBarStackedOmnidirectionalSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.GS1_DataBar_Stacked_Omnidirectional)
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
            return "GS1 DataBar Stacked Omnidirectional symbology allows encoding of up to 14 digits of data. Last digit must be checksum and will be verified.";
        }

        /// <summary>
        /// Gets or sets the height of the barcode bars in pixels.
        /// </summary>
        /// <value>The height of the barcode bars in pixels.</value>
        public override int BarHeight
        {
            get
            {
                return base.BarHeight;
            }
            set
            {
                if (value < 69 * NarrowBarWidth)
                    base.BarHeight = 69 * NarrowBarWidth;
                else
                    base.BarHeight = value;
            }
        }

        /// <summary>
        /// Gets or sets the width of the narrow bar in pixels.
        /// </summary>
        /// <value>The width of the narrow bar in pixels.</value>
        public override int NarrowBarWidth
        {
            get
            {
                return base.NarrowBarWidth;
            }
            set
            {
                // Preserving the proportions between BarHeight and NarrowBarWidth
                double ratio = BarHeight / base.NarrowBarWidth;
                base.NarrowBarWidth = value;
                BarHeight = (int)Math.Round(ratio * base.NarrowBarWidth);
            }
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            string sValue = GetEncodedValue(false);
            createSegments(sValue);

            intList upperLine = new intList();
            intList lowerLine = new intList();
            GS1Utils.addArray(upperLine, m_guardPattern, false);
            GS1Utils.addArray(upperLine, m_sign1, false);
            GS1Utils.addArray(upperLine, m_leftPatternSign, false);
            GS1Utils.addArray(upperLine, m_sign2, true);
            GS1Utils.addArray(upperLine, m_guardPattern, false);
            GS1Utils.addArray(lowerLine, m_guardPattern, false);
            GS1Utils.addArray(lowerLine, m_sign4, false);
            GS1Utils.addArray(lowerLine, m_rightPatternSign, true);
            GS1Utils.addArray(lowerLine, m_sign3, true);
            GS1Utils.addArray(lowerLine, m_guardPattern, false);
            if (upperLine.Count != 25 || lowerLine.Count != 25)
                throw new BarcodeException("GS1 DataBar Stacked symbology error");
            
            intList upperModules = new intList();
            for (int i = 0; i < upperLine.Count; i++)
            {
                int module = 0;
                if (i % 2 != 0)
                    module = 1;
                for (int j = 0; j < upperLine[i]; j++)
                    upperModules.Add(module);
            }
            intList lowerModules = new intList();
            for (int i = 0; i < lowerLine.Count; i++)
            {
                int module = 1;
                if (i % 2 != 0)
                    module = 0;
                for (int j = 0; j < lowerLine[i]; j++)
                    lowerModules.Add(module);
            }
            if (upperModules.Count != 50 || lowerModules.Count != 50)
                throw new BarcodeException("GS1 DataBar Stacked symbology error");

            // Forming the upper row of the template - the separator lines
            intList upperSeparatorModules = new intList();
            for (int i = 4; i < 18; i++)
            {
                upperSeparatorModules.Add((upperModules[i] == 1) ? 0 : 1);
            }
            for (int i = 18; i < 31; i++)
            {
                if (upperModules[i] == 1)
                    upperSeparatorModules.Add(0);
                else
                    upperSeparatorModules.Add((upperSeparatorModules[upperSeparatorModules.Count - 1] == 1) ? 0 : 1);
            }
            for (int i = 31; i < upperModules.Count - 4; i++)
            {
                upperSeparatorModules.Add((upperModules[i] == 1) ? 0 : 1);
            }

            // forming the lower row of the template - the separator lines
            intList lowerSeparatorModules = new intList();
            for (int i = 4; i < 18; i++)
            {
                lowerSeparatorModules.Add((lowerModules[i] == 1) ? 0 : 1);
            }
            if ((m_rightPatternSign[0] == 3) && (m_rightPatternSign[1] == 1) && (m_rightPatternSign[2] == 9))
            {
                // if the search pattern == 3, then when i == 32, we insert a dark module, the rest of the modules are light
                for (int i = 18; i < 31; i++)
                {
                    if (i == 32)
                        lowerSeparatorModules.Add(1);
                    else
                        lowerSeparatorModules.Add(0);
                }
            }
            else
            {
                for (int i = 18; i < 31; i++)
                {
                    if (lowerModules[i] == 1)
                        lowerSeparatorModules.Add(0);
                    else
                        lowerSeparatorModules.Add((lowerSeparatorModules[lowerSeparatorModules.Count - 1] == 1) ? 0 : 1);
                }
            }
            for (int i = 31; i < lowerModules.Count - 4; i++)
            {
                lowerSeparatorModules.Add((lowerModules[i] == 1) ? 0 : 1);
            }

            intList upperSeparatorLine = new intList();
            int moduleColor = upperSeparatorModules[0];
            int counter = 1;
            for (int i = 1; i < upperSeparatorModules.Count; i++)
            {
                if (upperSeparatorModules[i] == moduleColor)
                    counter++;
                else
                {
                    upperSeparatorLine.Add(counter);
                    moduleColor = upperSeparatorModules[i];
                    counter = 1;
                }
            }
            upperSeparatorLine.Add(counter);
            intList lowerSeparatorLine = new intList();
            moduleColor = lowerSeparatorModules[0];
            counter = 1;
            for (int i = 1; i < lowerSeparatorModules.Count; i++)
            {
                if (lowerSeparatorModules[i] == moduleColor)
                    counter++;
                else
                {
                    lowerSeparatorLine.Add(counter);
                    moduleColor = lowerSeparatorModules[i];
                    counter = 1;
                }
            }
            lowerSeparatorLine.Add(counter);

            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            int width = NarrowBarWidth;
            Size captionSize = calculateCaptionSize(canvas, font);

            int height = (int)Math.Round((BarHeight - NarrowBarWidth * 3) / 2.0);
            for (int i = 0; i < upperLine.Count; i++)
            {
                if (i % 2 != 0)
                    m_rects.Add(new Rectangle(x, y, width * upperLine[i], height));
                x += width * upperLine[i];
            }
            x = 4 * width;
            y += height;
            height = width;
            for (int i = 0; i < upperSeparatorLine.Count; i++)
            {
                if (upperSeparatorModules[0] == 1)
                {
                    if (i % 2 == 0)
                        m_rects.Add(new Rectangle(x, y, width * upperSeparatorLine[i], height));
                }
                else
                {
                    if (i % 2 != 0)
                        m_rects.Add(new Rectangle(x, y, width * upperSeparatorLine[i], height));
                }

                x += width * upperSeparatorLine[i];
            }
            x = 4 * width;
            y += height;
            for (int i = 0; i < 42; i++)
            {
                // Middle row of the template - the separator lines consists of alternating light and dark modules
                if (i % 2 != 0)
                    m_rects.Add(new Rectangle(x, y, width , height));
                x += width;
            }
            x = 4 * width;
            y += height;
            for (int i = 0; i < lowerSeparatorLine.Count; i++)
            {
                if (lowerSeparatorModules[0] == 1)
                {
                    if (i % 2 == 0)
                        m_rects.Add(new Rectangle(x, y, width * lowerSeparatorLine[i], height));
                }
                else
                {
                    if (i % 2 != 0)
                        m_rects.Add(new Rectangle(x, y, width * lowerSeparatorLine[i], height));
                }
                x += width * lowerSeparatorLine[i];
            }
            x = 0;
            y += height;
            height = (int)Math.Round((BarHeight - NarrowBarWidth * 3) / 2.0);
            for (int i = 0; i < lowerLine.Count; i++)
            {
                if (i % 2 == 0)
                    m_rects.Add(new Rectangle(x, y, width * lowerLine[i], height));
                x += width * lowerLine[i];
            }

            drawingSize.Width = x;
            drawingSize.Height = BarHeight + captionSize.Height / 2;
            return drawingSize;
        }
    }
}
