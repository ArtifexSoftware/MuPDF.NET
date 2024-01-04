using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace CSharpMuPDF
{
    public struct LineDictionary
    {
        public List<SpanDictionary> SPANS;

        public int WMODE;

        public FzPoint DIR;

        public FzRect BBOX;
    }
}
