namespace MuPDF.NET
{
    public class Font
    {
        public int Xref { get; set; }

        public string Ext { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public string RefName { get; set; }

        public string Encoding { get; set; }

        public int StreamXref { get; set; }

        public int Ordering { get; set; }

        public bool Simple { get; set; }

        public List<(int, double)> Glyphs { get; set; }

        public float Ascender { get; set; }

        public float Descender { get; set; }

        public byte[] Content { get; set; }

    }
}
