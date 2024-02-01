using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class ColorSpace
    {
        private readonly FzColorspace _nativeColorSpace;

        public int N
        {
            get
            {
                return _nativeColorSpace.fz_colorspace_n();
            }
        }

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
            string x = (new List<string>(){"", "GRAY", "RGB", "CMYK"})[N];
            return $"ColorSpace(CS_{x}) - {Name}";
        } 
    }
}
