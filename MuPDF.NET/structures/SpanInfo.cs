using System.Collections.Generic;

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

        public int Type { get; set; }

        /// <summary>
        /// the span bbox
        /// </summary>
        public Rect Bbox { get; set; }

        /// <summary>
        /// the layer name
        /// </summary>
        public string Layer { get; set; }

        public int SeqNo { get; set; }

        /// <summary>
        /// a list of char in span
        /// </summary>
        public List<Char> Chars { get; set; }

        /// <summary>
        /// PyMuPDF <c>span["chars"]</c>: <c>List&lt;object&gt;</c> of
        /// <c>(ucs, gid, origin, bbox)</c> tuples from the text-trace device.
        /// </summary>
        internal List<object> CharsPy { get; set; }

        /// <summary>MuPDF.NET dictionary-style access (<c>span["font"]</c>).</summary>
        public object this[string key]
        {
            get
            {
                switch (key)
                {
                    case "dir": return Dir;
                    case "font": return Font;
                    case "wmode": return WMode;
                    case "flags": return Flags;
                    case "bidi_lvl": return BidiLevel;
                    case "bidi_dir": return BidiDir;
                    case "ascender": return Ascender;
                    case "descender": return Descender;
                    case "colorspace": return ColorSpace;
                    case "color": return Color;
                    case "size": return Size;
                    case "opacity": return Opacity;
                    case "linewidth": return LineWidth;
                    case "spacewidth": return SpaceWidth;
                    case "type": return Type;
                    case "bbox": return Bbox;
                    case "layer": return Layer;
                    case "seqno": return SeqNo;
                    case "chars": return CharsPy;
                    default: return null;
                }
            }
        }
    }
}
