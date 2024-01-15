using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace CSharpMuPDF
{
    public struct CharStruct
    {
        public FzPoint ORIGIN;

        public FzRect BBOX;

        public char C;
    }
}
