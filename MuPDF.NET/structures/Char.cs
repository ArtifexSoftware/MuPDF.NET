using mupdf;

namespace MuPDF.NET
{
    public class Char
    {
        /// <summary>
        /// character's left baseline point
        /// </summary>
        public FzPoint Origin { get; set; }

        /// <summary>
        /// character rectangle
        /// </summary>
        public FzRect Bbox { get; set; }

        /// <summary>
        /// the character
        /// </summary>
        public char C { get; set; }

        public int UCS { get; set; }

        public int GID { get; set; }
    }
}
