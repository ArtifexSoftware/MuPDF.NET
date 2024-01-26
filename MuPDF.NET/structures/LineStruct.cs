using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace MuPDF.NET
{
    public struct LineStruct
    {
        public List<SpanStruct> SPANS;

        public int WMODE;

        public FzPoint DIR;

        public FzRect BBOX;
    }
}
