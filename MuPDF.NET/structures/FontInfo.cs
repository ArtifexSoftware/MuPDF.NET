using System.Collections.Generic;

namespace MuPDF.NET
{
    public class FontInfo
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

        public List<(int, float)> Glyphs { get; set; }

        public float Ascender { get; set; }

        public float Descender { get; set; }

        public byte[] Content { get; set; }

        // Compatibility bridge: allows assignment from modern ExtractFont tuple results.
        public static implicit operator FontInfo((string name, string ext, string type, byte[] content) value)
        {
            return new FontInfo
            {
                Name = value.name ?? string.Empty,
                Ext = value.ext ?? string.Empty,
                Type = value.type ?? string.Empty,
                Content = value.content ?? System.Array.Empty<byte>(),
            };
        }

        // Compatibility bridge: allows assignment from modern Page.GetFonts tuple entries.
        public static implicit operator FontInfo((int xref, string ext, string type, string baseName, string name, string encoding, int? referencer) value)
        {
            return new FontInfo
            {
                Xref = value.xref,
                Ext = value.ext ?? string.Empty,
                Type = value.type ?? string.Empty,
                RefName = value.baseName ?? string.Empty,
                Name = value.name ?? string.Empty,
                Encoding = value.encoding ?? string.Empty,
                StreamXref = value.referencer ?? 0,
            };
        }

    }
}
