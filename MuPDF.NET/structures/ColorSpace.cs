using mupdf;
using System.Runtime.InteropServices;

namespace MuPDF.NET
{
    public class ColorSpace
    {
        static ColorSpace()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Utils.LoadEmbeddedDllForWindows();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Utils.LoadEmbeddedDllForLinux();
            }
        }

        private readonly FzColorspace _nativeColorSpace;

        /// <summary>
        /// The number of bytes required to define the color of one pixel.
        /// </summary>
        public int N
        {
            get
            {
                return _nativeColorSpace.fz_colorspace_n();
            }
        }

        /// <summary>
        /// The name identifying the colorspace.
        /// </summary>
        public string Name
        {
            get
            {
                return _nativeColorSpace.fz_colorspace_name();
            }
        }

        public ColorSpace(int type)
        {
            if (type == Utils.CS_GRAY)
                _nativeColorSpace = new FzColorspace(FzColorspace.Fixed.Fixed_GRAY);
            else if (type == Utils.CS_CMYK)
                _nativeColorSpace = new FzColorspace(FzColorspace.Fixed.Fixed_CMYK);
            else if (type == Utils.CS_RGB)
                _nativeColorSpace = new FzColorspace(FzColorspace.Fixed.Fixed_RGB);
            else
                _nativeColorSpace = new FzColorspace(FzColorspace.Fixed.Fixed_RGB);
        }

        public ColorSpace(ColorSpace cs) : this(cs.N)
        {

        }

        public ColorSpace(FzColorspace nativeColorSpace)
        {
            _nativeColorSpace = nativeColorSpace;
        }

        public FzColorspace ToFzColorspace()
        {
            return _nativeColorSpace;
        }

        public override string ToString()
        {
            string x = (new List<string>() { "", "GRAY", "", "RGB", "CMYK" })[N];
            return $"ColorSpace(CS_{x}) - {Name}";
        }
    }
}
