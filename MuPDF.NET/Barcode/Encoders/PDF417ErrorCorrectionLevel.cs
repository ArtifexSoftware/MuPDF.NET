/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes all possible error correction levels for PDF417 symbology.
    /// </summary>
    /// <remarks>
    /// When error correction level set to Auto, library will try to 
    /// use best possible error correction level. Error correction is done
    /// by adding extra symbols to barcode. Higher error correction level
    /// adds more extra symbols and thus shortens possible length of string 
    /// to encode.
    /// </remarks>
    public enum PDF417ErrorCorrectionLevel
    {
        /// <summary>
        /// (-1) Library will choose best possible error correction level.
        /// </summary>
        Auto = -1,

        /// <summary>
        /// (0) Level 0 error correction. 2 extra symbols will be added to
        /// encoded data.
        /// </summary>
        Level0 = 0,

        /// <summary>
        /// (1) Level 1 error correction. 4 extra symbols will be added to 
        /// encoded data.
        /// </summary>
        Level1 = 1,

        /// <summary>
        /// (2) Level 2 error correction. 8 extra symbols will be added to 
        /// encoded data.
        /// </summary>
        Level2 = 2,

        /// <summary>
        /// (3) Level 3 error correction. 16 extra symbols will be added to 
        /// encoded data.
        /// </summary>
        Level3 = 3,

        /// <summary>
        /// (4) Level 4 error correction. 32 extra symbols will be added to 
        /// encoded data.
        /// </summary>
        Level4 = 4,

        /// <summary>
        /// (5) Level 5 error correction. 64 extra symbols will be added to 
        /// encoded data.
        /// </summary>
        Level5 = 5,

        /// <summary>
        /// (6) Level 6 error correction. 128 extra symbols will be added to 
        /// encoded data.
        /// </summary>
        Level6 = 6,

        /// <summary>
        /// (7) Level 7 error correction. 256 extra symbols will be added to 
        /// encoded data.
        /// </summary>
        Level7 = 7,

        /// <summary>
        /// (8) Level 8 error correction. 512 extra symbols will be added to 
        /// encoded data.
        /// </summary>
        Level8 = 8
    }
}
