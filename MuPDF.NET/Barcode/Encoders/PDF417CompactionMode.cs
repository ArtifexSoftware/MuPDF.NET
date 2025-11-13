/**************************************************
 *
 *
 *
 *
**************************************************/

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes all possible compaction (encoding) modes for PDF417 symbology.
    /// </summary>
    public enum PDF417CompactionMode
    {
        /// <summary>
        /// (-1) Library will choose best compaction mode (aim is to produce shortest encoded byte string).
        /// </summary>
        Auto = -1,

        /// <summary>
        /// (0) Binary compaction mode. Can encode any ASCII symbol.
        /// </summary>
        Binary = 0,

        /// <summary>
        /// (1) Text compaction mode (allows optimized text encoding)
        /// </summary>
        Text = 1,

        /// <summary>
        /// (2) Numeric compaction mode (allows optimized encoding of numbers)
        /// </summary>
        Numeric = 2
    }
}
