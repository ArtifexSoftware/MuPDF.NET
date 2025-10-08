namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes options for barcode caption alignment.
    /// </summary>
    public enum CaptionAlignment
    {
        /// <summary>
        /// (0) Automatic alignment. 
        /// </summary>
        Auto = 0,

        /// <summary>
        /// (1) Align caption text to the left.
        /// </summary>
        Left = 1,
		
		/// <summary>
		/// (2) Align caption text to the center.
        /// </summary>
        Center = 2,
		
		/// <summary>
        /// (3) Align caption text to the right.
        /// </summary>
        Right = 3
    }
}
