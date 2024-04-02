using mupdf;

namespace MuPDF.NET
{
    public class Block
    {
        public int Xref;

        public int Number;

        public int Type;

        public FzRect Bbox;

        public int Width;

        public int Height;

        public string Ext;

        public int ColorSpace;

        public int Xres;

        public int Yres;

        public byte Bpc;

        public FzMatrix Matrix;

        public uint Size;

        public byte[] Image;

        public string CsName;

        public vectoruc Digest;

        public List<Line> Lines;
    }
}
