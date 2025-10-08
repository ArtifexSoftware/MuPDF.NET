using System;
using System.ComponentModel;

namespace BarcodeWriter.Core
{
	/// <summary>
	/// Interface that describes barcode margins.
	/// </summary>
	interface IMargins
	{
		/// <summary>
		/// Occurs when margins get changed.
		/// </summary>
		event EventHandler Changed;

        /// <summary>
        /// Gets or sets the left margin in pixels.
        /// </summary>
        /// <value>The left margin in pixels.</value>
		int Left { get; set; }

        /// <summary>
        /// Gets or sets the top margin in pixels.
        /// </summary>
        /// <value>The top margin in pixels.</value>
		int Top { get; set; }

        /// <summary>
        /// Gets or sets the right margin in pixels.
        /// </summary>
        /// <value>The right margin in pixels.</value>
		int Right { get; set; }

        /// <summary>
        /// Gets or sets the bottom margin in pixels.
        /// </summary>
        /// <value>The bottom margin in pixels.</value>
		int Bottom { get; set; }
	}
}