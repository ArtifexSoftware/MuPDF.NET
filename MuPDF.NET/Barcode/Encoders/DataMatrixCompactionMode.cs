
using System;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes all possible compaction (encoding) modes for DataMatrix symbology.
    /// </summary>
    public enum DataMatrixCompactionMode
    {
        /// <summary>
        /// (-1) Library will choose best compaction mode (aim is to produce shortest encoded byte string).
        /// </summary>
        Auto = -1,

        /// <summary>
        /// (0) ASCII compaction mode. Sort of 'general purpose' mode. One encoded symbol 
        /// produced from: 1/2 of source symbol for ASCII characters with code from 128 to 255,
        /// 1 source symbol for ASCII characters with code from 0 to 127,
        /// 2 source symbols for ASCII digits.
        /// </summary>
        Ascii = 0,

        /// <summary>
        /// (1) C40 compaction mode. Best for upper-case alphanumeric strings. One encoded
        /// symbol produced from 1 and 1/2 of source symbols.
        /// </summary>
        C40 = 1,

        /// <summary>
        /// (2) TEXT compaction mode. Best for lower-case alphanumeric strings. One encoded
        /// symbol produced from 1 and 1/2 of source symbols.
        /// </summary>
        Text = 2,

        /// <summary>
        /// (3) X12 compaction mode. Useful for encoding symbols from 
        /// " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ\r*>" range. One encoded
        /// symbol produced from 1 and 1/2 of source symbols.
        /// </summary>
        X12 = 3,

        /// <summary>
        /// (4) EDIFACT compaction mode. Best for ASCII charcters with codes
        /// from 32 to 94. One encoded symbol produced from 1 and 1/3 of
        /// source symbols.
        /// </summary>
        Edifact = 4,

        /// <summary>
        /// (5) BINARY (or BASE256) compaction mode. Can encoded all ASCII symbols.
        /// One encoded symbol produced from one source symbol.
        /// </summary>
        Binary = 5,
    }
}
