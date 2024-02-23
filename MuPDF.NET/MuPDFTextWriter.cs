using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace MuPDF.NET
{
    public class MuPDFTextWriter
    {

        private FzText _nativeText;

        public float Opacity;
        public MuPDFTextWriter(Rect pageRect, float opacity = 1, ColorStruct color)
        {
            _nativeText = mupdf.mupdf.fz_new_text();
            Opacity = opacity;
        }
    }
}