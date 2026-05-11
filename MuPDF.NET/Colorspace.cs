using System;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a PDF colorspace.
    /// </summary>
    public class Colorspace
    {
        internal mupdf.FzColorspace _native;

        /// <summary>
        /// Predefined RGB colorspace.
        /// </summary>
        public static readonly Colorspace CsRGB = new Colorspace(ColorspaceType.RGB);
        /// <summary>
        /// Predefined GRAY colorspace.
        /// </summary>
        public static readonly Colorspace CsGRAY = new Colorspace(ColorspaceType.GRAY);
        /// <summary>
        /// Predefined CMYK colorspace.
        /// </summary>
        public static readonly Colorspace CsCMYK = new Colorspace(ColorspaceType.CMYK);

        /// <summary>
        /// Initializes a new colorspace. Supported are GRAY, RGB and CMYK.
        /// </summary>
        public Colorspace(ColorspaceType type)
        {
            _native = type switch
            {
                ColorspaceType.RGB => mupdf.mupdf.fz_device_rgb(),
                ColorspaceType.GRAY => mupdf.mupdf.fz_device_gray(),
                ColorspaceType.CMYK => mupdf.mupdf.fz_device_cmyk(),
                _ => throw new ArgumentException($"Unknown colorspace type: {type}")
            };
        }

        internal Colorspace(mupdf.FzColorspace native) { _native = native; }

        /// <summary>
        /// Number of components per pixel.
        /// </summary>
        public int N => mupdf.mupdf.fz_colorspace_n(_native);
        /// <summary>
        /// Name of the colorspace.
        /// </summary>
        public string Name => mupdf.mupdf.fz_colorspace_name(_native);
        internal mupdf.FzColorspace Native => _native;

        internal mupdf.FzColorspace ToFzColorspace() => _native;

        /// <summary>
        /// Returns a string that represents the current colorspace.
        /// </summary>
        public override string ToString() => $"Colorspace({Name}, n={N})";
        public static implicit operator mupdf.FzColorspace(Colorspace cs) => cs._native;
    }
}
