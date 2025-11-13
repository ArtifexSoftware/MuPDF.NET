
using System;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes all possible compaction (encoding) modes for Aztec symbology.
    /// </summary>
    public enum AztecCompactionMode
    {
        /// <summary>
        /// (-1) Default. Library is mixing binary and ASCII encoding modes to get minimal size possible
        /// </summary>
        Auto = -1,

        /// <summary>
        /// (0) Library forces use of binary encoding
        /// For binary encoding you can set value from byte[] array like this:
        /// barcode.value = Encoding.Default.GetString(new byte[] { 0, 10, 11, 12, 13, 14, 15})
        /// </summary>
        Binary = 0
    }
}
