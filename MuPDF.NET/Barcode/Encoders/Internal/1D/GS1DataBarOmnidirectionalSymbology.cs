/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2012 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using GS1 DataBar Omnidirectional (aka RSS-14) symbology rules.
	/// This symbology used within the GS1 System for encode a GTIN.
    /// </summary>
    class GS1DataBarOmnidirectionalSymbology : GS1DataBarOmnidirectionalBasic
    {
        public GS1DataBarOmnidirectionalSymbology(TrueSymbologyType type)
            : base(type)
        {
        }

        public GS1DataBarOmnidirectionalSymbology(SymbologyDrawing prototype, TrueSymbologyType type)
            : base(prototype, type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarOmnidirectionalSymbology"/> class.
        /// </summary>
        public GS1DataBarOmnidirectionalSymbology()
            : base(TrueSymbologyType.GS1_DataBar_Omnidirectional)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarOmnidirectionalSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public GS1DataBarOmnidirectionalSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.GS1_DataBar_Omnidirectional)
        {
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
                if (value < 33 * NarrowBarWidth)
                    base.BarHeight = 33 * NarrowBarWidth;
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
                // preservring the aspect ration and proportions between BarHeight and NarrowBarWidth
                double ratio = BarHeight / base.NarrowBarWidth;
                base.NarrowBarWidth = value;
                BarHeight = (int)Math.Round(ratio * base.NarrowBarWidth);
            }
        }

        protected int BaseBarHeight
        {
            set
            {
                    base.BarHeight = value;
            }
        }

        protected int BaseNarrowBarWidth
        {
            set
            {
                base.NarrowBarWidth = value;
            }
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            string sValue = GetEncodedValue(false);
            createSegments(sValue);

            intList symbol = new intList();
            GS1Utils.addArray(symbol, m_guardPattern, false);
            GS1Utils.addArray(symbol, m_sign1, false);
            GS1Utils.addArray(symbol, m_leftPatternSign, false);
            GS1Utils.addArray(symbol, m_sign2, true);
            GS1Utils.addArray(symbol, m_sign4, false);
            GS1Utils.addArray(symbol, m_rightPatternSign, true);
            GS1Utils.addArray(symbol, m_sign3, true);
            GS1Utils.addArray(symbol, m_guardPattern, false);
            if (symbol.Count != 46)
                throw new BarcodeException("GS1 DataBar Omnidirectional symbology error");

            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            int height = BarHeight;
            int width = NarrowBarWidth;

            Size captionSize = calculateCaptionSize(canvas, font);
            int guardHeight = height + captionSize.Height / 2;

            for (int i = 0; i < symbol.Count; i++)
            {
                if (i % 2 != 0)
                    m_rects.Add(new Rectangle(x, y, width * symbol[i], guardHeight));
                x += width * symbol[i];
            }

            drawingSize.Width = x;
            drawingSize.Height = BarHeight + captionSize.Height / 2;
            return drawingSize;
        }

    }
}
