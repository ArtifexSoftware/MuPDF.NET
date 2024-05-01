using mupdf;

namespace MuPDF.NET
{
    public class Line
    {
        public List<Span> Spans { get; set; }

        public int WMode { get; set; }

        public Point Dir { get; set; }

        public Rect Bbox { get; set; }
    }
}
