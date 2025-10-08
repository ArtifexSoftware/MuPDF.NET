/**************************************************
 *
 *
 *
 *
**************************************************/

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes all supported barcode output rotation angles.
    /// </summary>
#if QRCODESDK
    internal enum RotationAngle
#else
    public enum RotationAngle
#endif
    {
        /// <summary>
        /// (0) 0 degrees clockwise
        /// </summary>
        Degrees0 = 0,

#if !PocketPC && !WindowsCE
        /// <summary>
        /// (1) 90 degrees clockwise
        /// </summary>
        Degrees90 = 1,

        /// <summary>
        /// (2) 180 degrees clockwise
        /// </summary>
        Degrees180 = 2,

        /// <summary>
        /// (3) 270 degrees clockwise
        /// </summary>
        Degrees270 = 3,
#endif
    }
}
