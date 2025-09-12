
namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes options for barcode caption position.
    /// </summary>
    public enum CaptionPosition
    {
        /// <summary>
        /// (0) Caption above the barcode.
        /// </summary>
        Above = 0,

        /// <summary>
        /// (1) Caption below the barcode.
        /// </summary>
        Below = 1,
		
		/// <summary>
		/// (2) Caption before the barcode (at left).
        /// </summary>
        Before = 2,
		
		/// <summary>
        /// (3) Caption after the barcode (at right).
        /// </summary>
        After= 3,
    }
}
