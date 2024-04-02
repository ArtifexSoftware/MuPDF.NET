namespace MuPDF.NET
{
    public class Font
    {
        public int Xref;

        public string Ext;

        public string Type;

        public string Name;

        public string RefName;

        public string Encoding;

        public int StreamXref;

        public int Ordering;

        public bool Simple;

        public List<(int, double)> Glyphs;

        public float Ascender;

        public float Descender;

        public byte[] Content;

    }
}
