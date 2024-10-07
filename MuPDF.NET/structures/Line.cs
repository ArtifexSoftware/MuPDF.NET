using mupdf;

namespace MuPDF.NET
{
    public class Line
    {
        /// <summary>
        /// list of span dictionaries
        /// </summary>
        public List<Span> Spans { get; set; }

        /// <summary>
        /// writing mode *(int)*: 0 = horizontal, 1 = vertical
        /// </summary>
        public int WMode { get; set; }

        /// <summary>
        /// writing direction
        /// </summary>
        public Point Dir { get; set; }

        /// <summary>
        /// line rectangle
        /// </summary>
        public Rect Bbox { get; set; }
    }
}
