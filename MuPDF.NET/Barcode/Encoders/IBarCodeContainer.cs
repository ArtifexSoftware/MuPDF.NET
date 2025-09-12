
using System;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Base interface for all barcode containers (controls and such).
    /// </summary>
    public interface IBarCodeContainer
    {
        /// <summary>
        /// Gets or sets the horizontal alignment of the barcode within the container.
        /// </summary>
        /// <value>The horizontal alignment of the barcode within the container.</value>
        BarcodeHorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the vertical alignment of the barcode within the container.
        /// </summary>
        /// <value>The vertical alignment of the barcode within the container.</value>
        BarcodeVerticalAlignment VerticalAlignment { get; set; }
    }
}
