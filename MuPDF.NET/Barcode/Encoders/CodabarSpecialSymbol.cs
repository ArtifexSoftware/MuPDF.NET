/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes special symbols of the Codabar symbology.
    /// This symbols can be set as Codabar start or termination symbols.
    /// Additional data can be encoded by the choice of start and termination characters.
    /// </summary>
    public enum CodabarSpecialSymbol
    {
        /// <summary>
        /// (0) The A symbol.
        /// </summary>
        A = 0,

        /// <summary>
        /// (1) The B symbol.
        /// </summary>
        B = 1,

        /// <summary>
        /// (2) The C symbol.
        /// </summary>
        C = 2,

        /// <summary>
        /// (3) The D symbol
        /// </summary>
        D = 3
    }
}
