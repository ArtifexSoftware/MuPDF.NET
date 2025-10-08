using System;
using System.Text;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using GS1 DataBar Stacked symbology rules.
	/// This symbology used within the GS1 System for encode a GTIN.
    /// </summary>
    class GS1DataBarStackedSymbology : GS1DataBarOmnidirectionalBasic
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarStackedSymbology"/> class.
        /// </summary>
        public GS1DataBarStackedSymbology()
            : base(TrueSymbologyType.GS1_DataBar_Stacked)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarStackedSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public GS1DataBarStackedSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.GS1_DataBar_Stacked)
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
            return "GS1 DataBar Stacked symbology allows encoding of up to 14 digits of data. Last digit must be checksum and will be verified.";
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
                if (value != 13 * NarrowBarWidth)
                {
                    base.BarHeight = value;
                    base.NarrowBarWidth = (int)Math.Round(value / 13.0);
                }
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
                if (value * 13 != BarHeight)
                {
                    base.NarrowBarWidth = value;
                    base.BarHeight = value * 13;
                }
            }
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
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


            intList separatorModules = new intList();
            separatorModules.Add(0);
            for (int i = 4; i < upperModules.Count-4; i++) // where separator template starts and ends is not described
            {                                              // it may end with space 
                if (upperModules[i] == lowerModules[i])
                    separatorModules.Add((upperModules[i] == 1) ? 0 : 1);
                else
                    separatorModules.Add((separatorModules[separatorModules.Count-1] == 1) ? 0 : 1);

            }
            intList separatorLine = new intList();
            int moduleColor = 0;
            int counter = 1;
            for (int i = 1; i < separatorModules.Count; i++)
            {
                if (separatorModules[i] == moduleColor)
                    counter++;
                else
                {
                    separatorLine.Add(counter);
                    moduleColor = separatorModules[i];
                    counter = 1;
                }
            }
            separatorLine.Add(counter);

            SKSize drawingSize = new SKSize();
            int x = 0;
            int y = 0;

            int width = NarrowBarWidth;
            SKSize captionSize = calculateCaptionSize(canvas, font);

            int height = 5 * width;
            for (int i = 0; i < upperLine.Count; i++)
            {
                if (i % 2 != 0)
                    m_rects.Add(new SKRect(x, y, width * upperLine[i]+x, height+y));
                x += width * upperLine[i];
            }
            x = 3 * width; // where separator template starts and ends is not described in the specification
            y += height;
            height = width;
            for (int i = 0; i < separatorLine.Count; i++)
            {
                if (i % 2 != 0)
                    m_rects.Add(new SKRect(x, y, width * separatorLine[i]+x, height+y));
                x += width * separatorLine[i];
            }
            x = 0;
            y += height;
            height = 7 * width;
            for (int i = 0; i < lowerLine.Count; i++)
            {
                if (i % 2 == 0)
                    m_rects.Add(new SKRect(x, y, width * lowerLine[i]+x, height+y));
                x += width * lowerLine[i];
            }

            drawingSize.Width = x;
            drawingSize.Height = 13 * NarrowBarWidth + captionSize.Height / 2;
            return drawingSize;
        }
    }
}
