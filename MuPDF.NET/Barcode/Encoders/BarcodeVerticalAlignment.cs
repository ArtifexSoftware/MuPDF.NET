/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes options for vertical alignment of the barcode within the target rectangle.
    /// </summary>
    public enum BarcodeVerticalAlignment
    {
        /// <summary>
        /// (0) Align the barcode to the top edge of the target rectangle.
        /// </summary>
        Top = 0, 

        /// <summary>
        /// (1) Vertically center the barcode in the target rectangle.
        /// </summary>
        Middle  = 1,

        /// <summary>
        /// (2) Align the barcode to the bottom edge of the target rectangle.
        /// </summary>
        Bottom = 2
    }
}
