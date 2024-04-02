using mupdf;

namespace MuPDF.NET
{
    public class Line
    {
        public List<Span> Spans;

        public int WMode;

        public FzPoint Dir;

        public FzRect Bbox;
    }
}
