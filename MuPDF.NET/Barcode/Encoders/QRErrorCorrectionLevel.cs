/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Level of error correction in QR Code symbols
    /// </summary>
    public enum QRErrorCorrectionLevel
    {
        /// <summary>
        /// (0) Lowest error correction level (Approx. 7% of codewords can be restored).
        /// </summary>
        Low = 0,

        /// <summary>
        /// (1) Medium error correction level (Approx. 15% of codewords can be restored).
        /// </summary>
        Medium,

        /// <summary>
        /// (2) Quarter error correction level (Approx. 25% of codewords can be restored).
        /// </summary>
        Quarter,

        /// <summary>
        /// (3) Highest error correction level (Approx. 30% of codewords can be restored).
        /// </summary>
        High
    }
}
