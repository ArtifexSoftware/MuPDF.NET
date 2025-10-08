/**************************************************
 *
 *
 *
 *
**************************************************/

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes all supported MSI symbology checksum algorithms.
    /// </summary>
    public enum MSIChecksumAlgorithm
    {
        /// <summary>
        /// No check digit (least common)
        /// </summary>
        NoCheckDigit = 0,
        /// <summary>
        /// The Modulo 10 check digit algorithm (most common) uses the Luhn algorithm.
        /// </summary>
        Modulo10 = 1,
        /// <summary>
        /// The Modulo 11 check digit algorithm.
        /// </summary>
        Modulo11 = 2,
        /// <summary>
        /// The Modulo 1010 check digit algorithm.
        /// </summary>
        Modulo1010 = 3,
        /// <summary>
        /// The Modulo 1110 check digit algorithm.
        /// </summary>
        Modulo1110 = 4,
    }
}
