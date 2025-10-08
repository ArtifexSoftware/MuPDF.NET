
namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes all supported Codabar checksum algorithms.
    /// </summary>
    /// <remarks>
    /// Note that there is no checksum defined as part of the Codabar standard, but some industries 
    /// (libraries, for example) have adopted their own checksum standards. BarcodeWriter.Core
    /// implements two such standards.
    /// </remarks>
    public enum CodabarChecksumAlgorithm
    {
        /// <summary>
        /// (0) Modulo 9 checksum algorithm.
        /// <para>
        /// Many libraries use the following system which includes 13 digits 
        /// plus a checksum;
        /// </para>
        /// <para>
        /// Digit 1 indicates the type of barcode: 2 = patron, 3 = item (book)
        /// </para>
        /// <para>
        /// Digits 2-5 identify the institution
        /// </para>
        /// <para>
        /// The next 6 digits (00010 586) identify the individual patron or item
        /// </para>
        /// <para>
        /// Digit 14 is the checksum
        /// </para>
        /// </summary>
        Modulo9 = 0,

        /// <summary>
        /// (1) AIIM check digit calculation algorithm. This standard is recommended by AIIM.
        /// </summary>
        AiimCheckDigit = 1,
    }
}
