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
        public Point Dir { get; set; }

        public string Font { get; set; }

        public uint WMode { get; set; }

        public float Flags { get; set; }

        public uint BidiLevel { get; set; }

        public uint BidiDir { get; set; }

        public float Ascender { get; set; }

        public float Descender { get; set; }

        public int ColorSpace { get; set; }

        public float[] Color { get; set; }

        public float Size { get; set; }

        public float Opacity { get; set; }

        public float LineWidth { get; set; }

        public float SpaceWidth { get; set; }

        public int Type { get; set; }

        public Rect Bbox { get; set; }

        public string Layer { get; set; }

        public int SeqNo { get; set; }

        public List<Char> Chars { get; set; }
    }
}
