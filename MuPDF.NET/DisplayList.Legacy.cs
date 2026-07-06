namespace MuPDF.NET
{
    /// <summary>
    /// Legacy MuPDF.NET API for <see cref="DisplayList"/>.
    /// </summary>
    /// <remarks>
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/classes/DisplayList.html"/>.
    /// </remarks>
    public partial class DisplayList
    {
        /// <summary>
        /// Legacy positional overload: <c>GetPixmap(matrix, colorSpace, alpha, clip)</c>.
        /// </summary>
        /// <param name="matrix">Transform matrix; <c>null</c> for identity.</param>
        /// <param name="colorSpace">Target colorspace; <c>null</c> for RGB.</param>
        /// <param name="alpha"><c>1</c> to include alpha; <c>0</c> for opaque (default).</param>
        /// <param name="clip">Clip rectangle intersected with <see cref="Rect"/>; <c>null</c> for full list.</param>
        /// <remarks>
        /// Parameterless and MuPDF-style calls use
        /// <see cref="GetPixmap(Matrix, Colorspace, bool, IRect)"/>.
        /// </remarks>
        public Pixmap GetPixmap(Matrix matrix, ColorSpace colorSpace, int alpha, Rect clip)
        {
            Colorspace cs = colorSpace;
            IRect irect = clip?.IRect;
            return GetPixmap(matrix, cs, alpha != 0, irect);
        }

        /// <summary>
        /// Legacy <c>Run(device, matrix, area)</c> with a <see cref="Rect"/> clip area.
        /// </summary>
        /// <param name="device">Device receiving drawing commands (<see cref="DeviceWrapper"/>).</param>
        /// <param name="matrix">Transformation matrix.</param>
        /// <param name="area">Visible region; only this part of the list is replayed.</param>
        public void Run(DeviceWrapper device, Matrix matrix, Rect area)
            => Run(device, matrix, (object)area);
    }
}