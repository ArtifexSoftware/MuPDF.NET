using mupdf;

namespace MuPDF.NET
{
    public class Span
    {
        public List<Char> Chars;

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
