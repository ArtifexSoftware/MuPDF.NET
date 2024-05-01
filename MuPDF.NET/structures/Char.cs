using mupdf;

namespace MuPDF.NET
{
    public class Char
    {
        public FzPoint Origin { get; set; }

        public FzRect Bbox { get; set; }

        public char C { get; set; }

        public int UCS { get; set; }

        public int GID { get; set; }
    }
}
