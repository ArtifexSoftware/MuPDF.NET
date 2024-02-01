using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace MuPDF.NET
{
    public struct SpanStruct
    {
        public List<CharStruct> Chars;

        public FzPoint Origin;

        public FzRect Bbox;

        public string Text;

        public float Size;

        public float Flags;

        public string Font;

        public int Color;

        public float Asc;

        public float Desc;
    }
}
