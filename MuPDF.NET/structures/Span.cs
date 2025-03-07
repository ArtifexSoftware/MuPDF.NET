using mupdf;
using System.Collections.Generic;

namespace MuPDF.NET
{
    public class Span
    {

        public List<Char> Chars { get; set; }

        /// <summary>
        /// the first character's origin
        /// </summary>
        public Point Origin { get; set; }

        /// <summary>
        /// span rectangle
        /// </summary>
        public Rect Bbox { get; set; }

        /// <summary>
        /// text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// font size
        /// </summary>
        public float Size { get; set; }

        public float Flags { get; set; }

        /// <summary>
        /// font name
        /// </summary>
        public string Font { get; set; }

        /// <summary>
        /// font characteristics
        /// </summary>
        public int Color { get; set; }

        public float Asc { get; set; }

        public float Desc { get; set; }
    }
}
