/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes alphabet options for Telepen symbology.
    /// </summary>
    public enum TelepenAlphabet
    {
        /// <summary>
        /// (0) The alphabet to use ASCII chars (encoded as string)
        /// All characters are encoded as ASCII characters.
        /// </summary>
        ASCII = 0,

        /// <summary>
        /// (1) The Numeric alphabet. Works when input value contains numbers only and makes smaller size barcodes by packing each number
        /// Input string should contain numbers only.
        /// </summary>
        Numeric = 1,

        /// <summary>
        /// (2) Mixed alphabet. Automatically selects appropriate alphabet (ASCII or Numeric) for different parts of barcode.
        /// WARNING: Not supported by some hardware barcode readers manufacturers 
        /// </summary>
        Auto = 2
    }
}
