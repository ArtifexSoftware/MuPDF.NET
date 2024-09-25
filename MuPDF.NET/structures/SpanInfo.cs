using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class SpanInfo
    {
        /// <summary>
        /// direction of the span
        /// </summary>
        public Point Dir { get; set; }

        /// <summary>
        /// font of the span
        /// </summary>
        public string Font { get; set; }

        /// <summary>
        /// write mode
        /// </summary>
        public uint WMode { get; set; }

        /// <summary>
        /// A dictionary with various font properties, each represented as bools
        /// </summary>
        public float Flags { get; set; }

        /// <summary>
        /// the bidirectional level
        /// </summary>
        public uint BidiLevel { get; set; }

        /// <summary>
        /// the bidirectional direction
        /// </summary>
        public uint BidiDir { get; set; }

        /// <summary>
        /// ascender of the font
        /// </summary>
        public float Ascender { get; set; }

        /// <summary>
        /// descender of the font
        /// </summary>
        public float Descender { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ColorSpace { get; set; }

        public float[] Color { get; set; }

        /// <summary>
        /// the font size
        /// </summary>
        public float Size { get; set; }

        /// <summary>
        /// span opacity
        /// </summary>
        public float Opacity { get; set; }

        /// <summary>
        /// line width
        /// </summary>
        public float LineWidth { get; set; }

        public float SpaceWidth { get; set; }
        /// <summary>
        /// the span bbox
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// the span bbox
        /// </summary>
        public Rect Bbox { get; set; }

        /// <summary>
        /// the layer name
        /// </summary>
        public string Layer { get; set; }

        /// <summary>
        /// no
        /// </summary>
        public int SeqNo { get; set; }

        /// <summary>
        /// a list of char in span
        /// </summary>
        public List<Char> Chars { get; set; }
    }
}
