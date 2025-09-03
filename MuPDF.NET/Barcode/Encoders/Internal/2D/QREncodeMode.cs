/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// QR Input data chunk encoding mode.
    /// </summary>
    enum QREncodeMode
    {
        /// <summary>
        /// Incorrect/Undefined mode 
        /// </summary>
        Incorrect = -1,

        /// <summary>
        /// Numeric mode
        /// </summary>
        Numeric = 0,

        /// <summary>
        /// Alphabet-numeric mode
        /// </summary>
        AlphaNumeric,

        /// <summary>
        /// 8-bit data mode
        /// </summary>
        Mode8,

        /// <summary>
        /// Kanji/Kana (shift-jis) mode
        /// </summary>
        Kanji,
    }
}
