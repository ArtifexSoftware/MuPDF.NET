/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes options for horizontal alignment of the barcode within the target rectangle.
    /// </summary>
    public enum BarcodeHorizontalAlignment
    {
        /// <summary>
        /// (0) Align the barcode to the left edge of the target rectangle.
        /// </summary>
        Left = 0,

        /// <summary>
        /// (1) Horizontally center the barcode in the target rectangle.
        /// </summary>
        Center  = 1,

        /// <summary>
        /// (2) Align the barcode to the right edge of the target rectangle.
        /// </summary>
        Right = 2
    }
}
