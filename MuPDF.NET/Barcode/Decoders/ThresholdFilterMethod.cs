namespace BarcodeReader.Core
{
    /// <summary>
    /// Image threshold filter types.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
	enum ThresholdFilterMethod
	{
		/// <summary>
		/// (0) WholeImage method
		/// </summary>
		WholeImage,

        /// <summary>
        /// (1) Block method
        /// </summary>
        Block,

        BlockOld,

        /// <summary>
        /// Block method + dilate morphology
        /// </summary>
	    BlockSmoothed,

        /// <summary>
        /// Block method + median filtration (removes thin lines and single pixels)
        /// </summary>
        BlockMedian,

        /// <summary>
        /// Block method + grid filtration (removes thin single lines and single pixels)
        /// </summary>
        BlockGrid,

        /// <summary>
        /// Simple cutoff binarization with predefined threshold level
        /// </summary>
        Threshold,

        /// <summary>
        /// For internal use only.
        /// </summary>
        None,

		/// <summary>
        /// For internal use only.
		/// </summary>
		Rotated,
		
		/// <summary>
		/// New enhancing BW filter.
		/// </summary>
		Enhancing,

        /// <summary>
        /// Simple cutoff binarization with predefined threshold level
        /// </summary>
        ThresholdEx,
    }
}
