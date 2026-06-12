using System;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents the color space of a <see cref="Pixmap"/> (device gray, RGB, or CMYK).
    /// </summary>
    /// <remarks>
    /// <para>
    /// MuPDF.NET colorspace type (PyMuPDF <c>fitz.Colorspace</c> equivalent). Legacy MuPDF.NET used the spelling
    /// <see cref="ColorSpace"/> with the same constructors and properties; see
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/classes/ColorSpace.html"/>.
    /// </para>
    /// <para>
    /// Use <see cref="Rgb"/>, <see cref="Gray"/>, and <see cref="Cmyk"/> instead of creating
    /// new instances when possible. Legacy code may use <see cref="ColorSpace.csRGB"/>,
    /// <see cref="ColorSpace.csGRAY"/>, and <see cref="ColorSpace.csCMYK"/>.
    /// </para>
    /// <para>
    /// Type identifiers match <see cref="Utils.CS_RGB"/> (1), <see cref="Utils.CS_GRAY"/> (2),
    /// and <see cref="Utils.CS_CMYK"/> (3), or <see cref="ColorspaceType"/>.
    /// </para>
    /// </remarks>
    public partial class Colorspace
    {
        internal mupdf.FzColorspace _native;

        /// <summary>
        /// Predefined RGB device colorspace (PyMuPDF <c>csRGB</c> / <c>CS_RGB</c>).
        /// </summary>
        public static readonly Colorspace Rgb = new Colorspace(ColorspaceType.Rgb);

        /// <summary>
        /// Predefined GRAY device colorspace (PyMuPDF <c>csGRAY</c> / <c>CS_GRAY</c>).
        /// </summary>
        public static readonly Colorspace Gray = new Colorspace(ColorspaceType.Gray);

        /// <summary>
        /// Predefined CMYK device colorspace (PyMuPDF <c>csCMYK</c> / <c>CS_CMYK</c>).
        /// </summary>
        public static readonly Colorspace Cmyk = new Colorspace(ColorspaceType.Cmyk);

        /// <summary>
        /// Creates a device colorspace from a <see cref="ColorspaceType"/> value.
        /// </summary>
        /// <param name="type">RGB, Gray, or CMYK. Unknown values map to RGB.</param>
        public Colorspace(ColorspaceType type)
        {
            _native = type switch
            {
                ColorspaceType.Gray => Helpers.DeviceColorspace(1),
                ColorspaceType.Cmyk => Helpers.DeviceColorspace(4),
                _ => Helpers.DeviceColorspace(3),
            };
        }

        /// <summary>
        /// Creates a device colorspace from a legacy or PyMuPDF numeric type id.
        /// </summary>
        /// <param name="n">
        /// Colorspace id: <see cref="Utils.CS_RGB"/> (1), <see cref="Utils.CS_GRAY"/> (2),
        /// or <see cref="Utils.CS_CMYK"/> (3). Other values default to RGB.
        /// </param>
        /// <remarks>
        /// Same parameter as legacy <see cref="ColorSpace.ColorSpace(int)"/>.
        /// Not to be confused with <see cref="N"/>, which is the component count (1, 3, or 4).
        /// </remarks>
        public Colorspace(int n) : this(LegacyTypeToEnum(n)) { }

        /// <summary>
        /// Uses the same underlying device colorspace as <paramref name="cs"/>.
        /// </summary>
        /// <param name="cs">Source colorspace.</param>
        /// <exception cref="ArgumentNullException"><paramref name="cs"/> is null.</exception>
        /// <remarks>Legacy <c>ColorSpace(ColorSpace)</c> copy constructor.</remarks>
        public Colorspace(Colorspace cs)
        {
            if (cs == null)
                throw new ArgumentNullException(nameof(cs));
            _native = cs._native;
        }

        internal Colorspace(mupdf.FzColorspace native) => _native = native;

        /// <summary>
        /// Number of color components required for one pixel (not including alpha).
        /// </summary>
        /// <value>1 for gray, 3 for RGB, 4 for CMYK.</value>
        /// <remarks>PyMuPDF <c>Colorspace.n</c>; legacy <see cref="ColorSpace.N"/>.</remarks>
        public int N => mupdf.mupdf.fz_colorspace_n(_native);

        /// <summary>
        /// Name identifying this colorspace (e.g. <c>DeviceRGB</c>, <c>DeviceGray</c>).
        /// </summary>
        /// <remarks>PyMuPDF <c>Colorspace.name</c>; legacy <see cref="ColorSpace.Name"/>.</remarks>
        public string Name => mupdf.mupdf.fz_colorspace_name(_native);

        internal mupdf.FzColorspace Native => _native;

        /// <summary>Returns the native <c>fz_colorspace</c> handle for MuPDF calls.</summary>
        public mupdf.FzColorspace ToFzColorspace() => _native;

        /// <summary>Debug string (PyMuPDF <c>Colorspace.__repr__</c>).</summary>
        public override string ToString()
        {
            string csLabel = N switch
            {
                1 => "GRAY",
                3 => "RGB",
                4 => "CMYK",
                _ => "",
            };
            return $"Colorspace(CS_{csLabel}) - {Name}";
        }

        /// <summary>Implicit conversion to <see cref="mupdf.FzColorspace"/>.</summary>
        public static implicit operator mupdf.FzColorspace(Colorspace cs) => cs._native;

        internal int n() => N;

        internal string name() => Name;

        private static ColorspaceType LegacyTypeToEnum(int type)
        {
            if (type == Utils.CS_GRAY || type == (int)ColorspaceType.Gray)
                return ColorspaceType.Gray;
            if (type == Utils.CS_CMYK || type == (int)ColorspaceType.Cmyk)
                return ColorspaceType.Cmyk;
            return ColorspaceType.Rgb;
        }
    }
}
