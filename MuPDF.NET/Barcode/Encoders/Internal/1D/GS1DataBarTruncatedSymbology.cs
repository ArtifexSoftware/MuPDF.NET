/**************************************************
 *
 Copyright (c) 2008 - 2012 Bytescout
 *
 *
**************************************************/
using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using GS1 DataBar Truncated symbology rules.
    /// This symbology used within the GS1 System for encode a GTIN.
    /// </summary>
    class GS1DataBarTruncatedSymbology : GS1DataBarOmnidirectionalSymbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarTruncatedSymbology"/> class.
        /// </summary>
        public GS1DataBarTruncatedSymbology()
            : base(TrueSymbologyType.GS1_DataBar_Truncated)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarTruncatedSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public GS1DataBarTruncatedSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.GS1_DataBar_Truncated)
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
            return "GS1 DataBar Truncated symbology allows encoding of up to 14 digits of data. Last digit must be checksum and will be verified.";
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
                if (value > 33 * NarrowBarWidth)
                    base.BaseBarHeight = 33 * NarrowBarWidth;
                else if (value < 13 * NarrowBarWidth)
                    base.BaseBarHeight = 13 * NarrowBarWidth;
                else
                    base.BaseBarHeight = value;
            }
        }
    }
}
