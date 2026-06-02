using mupdf;
using System;

namespace MuPDF.NET
{
    /// <summary>
    /// Legacy MuPDF.NET colorspace type (readthedocs <c>ColorSpace</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Thin wrapper over <see cref="Colorspace"/> for code written against the original API.
    /// New code should prefer <see cref="Colorspace"/> and <see cref="Colorspace.Rgb"/> /
    /// <see cref="Colorspace.Gray"/> / <see cref="Colorspace.Cmyk"/>.
    /// </para>
    /// <para>
    /// See <see href="https://mupdfnet.readthedocs.io/en/latest/classes/ColorSpace.html"/>.
    /// </para>
    /// </remarks>
    public sealed class ColorSpace
    {
        private readonly Colorspace _inner;

        /// <summary>
        /// Predefined RGB colorspace (<c>new ColorSpace(Utils.CS_RGB)</c>).
        /// </summary>
        public static readonly ColorSpace csRGB = new ColorSpace(Utils.CS_RGB);

        /// <summary>
        /// Predefined GRAY colorspace (<c>new ColorSpace(Utils.CS_GRAY)</c>).
        /// </summary>
        public static readonly ColorSpace csGRAY = new ColorSpace(Utils.CS_GRAY);

        /// <summary>
        /// Predefined CMYK colorspace (<c>new ColorSpace(Utils.CS_CMYK)</c>).
        /// </summary>
        public static readonly ColorSpace csCMYK = new ColorSpace(Utils.CS_CMYK);

        /// <summary>
        /// Creates a device colorspace from a legacy type id.
        /// </summary>
        /// <param name="n">
        /// One of <see cref="Utils.CS_RGB"/> (1), <see cref="Utils.CS_GRAY"/> (2),
        /// or <see cref="Utils.CS_CMYK"/> (3). Other values default to RGB.
        /// </param>
        public ColorSpace(int n) => _inner = new Colorspace(n);

        /// <summary>
        /// Shares the same underlying device colorspace as <paramref name="cs"/>.
        /// </summary>
        /// <param name="cs">Source colorspace.</param>
        /// <exception cref="ArgumentNullException"><paramref name="cs"/> is null.</exception>
        public ColorSpace(ColorSpace cs)
        {
            if (cs == null)
                throw new ArgumentNullException(nameof(cs));
            _inner = cs._inner;
        }

        internal ColorSpace(Colorspace cs) =>
            _inner = cs ?? throw new ArgumentNullException(nameof(cs));

        internal ColorSpace(FzColorspace native) => _inner = new Colorspace(native);

        /// <summary>
        /// Number of color components per pixel (1 = gray, 3 = RGB, 4 = CMYK).
        /// </summary>
        /// <remarks>Legacy docs: bytes required to define the color of one pixel (component count).</remarks>
        public int N => _inner.N;

        /// <summary>
        /// Name identifying the colorspace (e.g. <c>DeviceRGB</c>).
        /// </summary>
        public string Name => _inner.Name;

        /// <summary>Returns the native <c>fz_colorspace</c> handle.</summary>
        public FzColorspace ToFzColorspace() => _inner.ToFzColorspace();

        /// <inheritdoc />
        public override string ToString()
        {
            string label = N switch
            {
                1 => "GRAY",
                3 => "RGB",
                4 => "CMYK",
                _ => "",
            };
            return $"ColorSpace(CS_{label}) - {Name}";
        }

        internal Colorspace Inner => _inner;

        /// <summary>Implicit conversion to <see cref="Colorspace"/>.</summary>
        public static implicit operator Colorspace(ColorSpace cs) => cs._inner;
    }
}
