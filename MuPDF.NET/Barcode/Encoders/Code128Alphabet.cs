/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes alphabet options for Code 128 symbology.
    /// </summary>
    public enum Code128Alphabet
    {
        /// <summary>
        /// (0) The alphabet to use selected automatically.
        /// Different parts of a barcode can be encoded with different alphabets.
        /// This alphabet type allows all ASCII table to be encoded with
        /// minimum output barcode width.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// (1) The A alphabet. Allows only character from NUL (ASCII 0) to 
        /// '_' (ASCII 95) to be encoded.
        /// </summary>
        A = 1,

        /// <summary>
        /// (2) The B alphabet. Allows only character from SPACE (ASCII 32) to 
        /// DEL (ASCII 127) to be encoded.
        /// </summary>
        B = 2,

        /// <summary>
        /// (3) The C alphabet. Allows only numeric values having even length to
        /// be encoded. This alphabet allows most efficient encoding
        /// of numeric values having even length.
        /// </summary>
        C = 3
    }
}
