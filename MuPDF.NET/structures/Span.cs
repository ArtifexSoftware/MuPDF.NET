using mupdf;

namespace MuPDF.NET
{
    public class Span
    {
        public List<Char> Chars { get; set; }

        public FzPoint Origin { get; set; }

        public FzRect Bbox { get; set; }

        public string Text { get; set; }

        public float Size { get; set; }

        public float Flags { get; set; }

        public string Font { get; set; }

        public int Color { get; set; }

        public float Asc { get; set; }

        public float Desc { get; set; }
    }
}
