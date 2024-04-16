using mupdf;

namespace MuPDF.NET
{
    public class Line
    {
        public List<Span> Spans { get; set; }

        public int WMode { get; set; }

        public FzPoint Dir { get; set; }

        public FzRect Bbox { get; set; }
    }
}
