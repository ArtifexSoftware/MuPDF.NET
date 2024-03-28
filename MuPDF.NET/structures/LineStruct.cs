using mupdf;

namespace MuPDF.NET
{
    public class LineStruct
    {
        public List<SpanStruct> Spans;

        public int WMode;

        public FzPoint Dir;

        public FzRect Bbox;
    }
}
