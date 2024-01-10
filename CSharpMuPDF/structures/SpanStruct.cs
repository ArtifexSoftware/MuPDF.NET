using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace CSharpMuPDF
{
    public struct SpanStruct
    {
        public List<CharStruct> CHARS;

        public FzPoint ORIGIN;

        public FzRect BBOX;

        public string TEXT;

        public float SIZE;

        public float FLAGS;

        public string FONT;

        public int COLOR;

        public float ASC;

        public float DESC;
    }
}
