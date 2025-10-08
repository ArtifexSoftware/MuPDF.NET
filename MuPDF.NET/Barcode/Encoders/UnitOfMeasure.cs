using System;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Specifies the unit of measure for the given data.
    /// </summary>
#if QRCODESDK
    internal enum UnitOfMeasure
#else
    public enum UnitOfMeasure
#endif
    {
        /// <summary>
        /// (0) Specifies a device pixel as the unit of measure.
        /// </summary>
        Pixel = 0,

        /// <summary>
        /// (1) Specifies a printer's point (1/72 inch) as the unit of measure. It is also used in PDF documents.
        /// </summary>
        Point = 1,

        /// <summary>
        /// (2) Specifies the inch as the unit of measure.
        /// </summary>
        Inch = 2,

        /// <summary>
        /// (3) Specifies the document unit (1/300 inch) as the unit of measure.
        /// </summary>
        Document = 3,

        /// <summary>
        /// (4) Specifies the millimeter as the unit of measure.
        /// </summary>
        Millimeter = 4,

        /// <summary>
        /// (5) Specifies the centimeter as the unit of measure.
        /// </summary>
        Centimeter = 5,

        /// <summary>
        /// (6) Specifies the twip unit (1/20 inch) as the unit of measure.
        /// </summary>
        Twip = 6,
    }
}
