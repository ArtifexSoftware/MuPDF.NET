using mupdf;

namespace MuPDF.NET
{
    public class Block
    {
        public int Xref { get; set; }

        public int Number { get; set; }

        public int Type { get; set; }

        public FzRect Bbox { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string Ext { get; set; }

        public int ColorSpace { get; set; }

        public int Xres { get; set; }

        public int Yres { get; set; }

        public byte Bpc { get; set; }

        public FzMatrix Transform { get; set; }

        public uint Size { get; set; }

        public byte[] Image { get; set; }

        public string CsName { get; set; }

        public vectoruc Digest { get; set; }

        public List<Line> Lines { get; set; }
    }
}
