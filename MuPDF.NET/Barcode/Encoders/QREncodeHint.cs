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
    /// Describes the library how non-alphanumerical characters should be encoded.
    /// </summary>
    public enum QREncodeHint
    {
        /// <summary>
        /// (0) All of non-alphanumerical characters will be encoded as is. This is default mode.
        /// </summary>
        Mode8 = 0,

        /// <summary>
        /// (1) Kanji/Kana characters will be encoded as Shif-JIS characters.
        /// </summary>
        Kanji,
    }
}
