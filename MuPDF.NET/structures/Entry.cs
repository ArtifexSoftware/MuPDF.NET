namespace MuPDF.NET
{
    public class Entry
    {
        // image info struct

        public string Ext { get; set; }

        public int Smask { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public int Bpc { get; set; }

        public string CsName { get; set; }

        public string AltCsName { get; set; }

        public string Filter { get; set; }

        // font struct

        public int Xref { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public string RefName { get; set; }

        public string Encoding { get; set; }

        public int StreamXref { get; set; }

        // form info struct

        public Rect Bbox = null { get; set; }
    }
}
