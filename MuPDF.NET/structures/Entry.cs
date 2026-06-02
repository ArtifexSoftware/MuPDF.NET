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
        /// <summary>
        /// the image, font and form object number
        /// </summary>
        public int Xref { get; set; }

        public string Type { get; set; }

        /// <summary>
        /// name under which this entry is stored or basefont name of font entry
        /// </summary>
        public string Name { get; set; }

        public string RefName { get; set; }

        public string Encoding { get; set; }

        public int StreamXref { get; set; }

        // form info struct

        public Rect Bbox { get; set; } = null;

        // PyMuPDF get_images() tuple aliases (legacy tests / ports).
        public int xref => Xref;
        public string smask => Smask.ToString();
        public int width => (int)Width;
        public int height => (int)Height;
        public int bpc => Bpc;
        public string colorspace => CsName ?? "";
        public string altCs => AltCsName ?? "";
        public string name => Name ?? "";
        public string filter => Filter ?? "";

        /// <summary>Deconstruct like PyMuPDF <c>get_images()</c> row tuples.</summary>
        public void Deconstruct(
            out int xref,
            out string smask,
            out int width,
            out int height,
            out int bpc,
            out string colorspace,
            out string altCs,
            out string name,
            out string filter)
        {
            xref = Xref;
            smask = Smask.ToString();
            width = (int)Width;
            height = (int)Height;
            bpc = Bpc;
            colorspace = CsName ?? "";
            altCs = AltCsName ?? "";
            name = Name ?? "";
            filter = Filter ?? "";
        }
    }
}
