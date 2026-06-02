using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    public class MuPDFCharStyle
    {
        public float Size { get; set; }
        public float Flags { get; set; }
        public string Font { get; set; }
        public uint Argb { get; set; }
        public uint CharFlags { get; set; }
        public ushort Bidi { get; set; }
        public float Asc { get; set; }
        public float Desc { get; set; }

        public MuPDFCharStyle(Dictionary<string, object> rhs)
        {
            Size = rhs.TryGetValue("Size", out var size) ? Convert.ToSingle(size) : 0;
            Flags = rhs.TryGetValue("Flags", out var flags) ? Convert.ToSingle(flags) : 0;
            Font = rhs.TryGetValue("Font", out var font) ? font?.ToString() ?? string.Empty : string.Empty;
            Asc = rhs.TryGetValue("Asc", out var asc) ? Convert.ToSingle(asc) : 0;
            Desc = rhs.TryGetValue("Desc", out var desc) ? Convert.ToSingle(desc) : 0;
            Argb = rhs.ContainsKey("Argb")
                ? Convert.ToUInt32(rhs["Argb"])
                : (rhs.ContainsKey("Color") ? unchecked((uint)(int)rhs["Color"]) : 0u);
            CharFlags = rhs.ContainsKey("CharFlags") ? Convert.ToUInt32(rhs["CharFlags"]) : 0;
            Bidi = rhs.ContainsKey("Bidi") ? Convert.ToUInt16(rhs["Bidi"]) : (ushort)0;
        }

        public MuPDFCharStyle(MuPDFCharStyle rhs)
        {
            Size = rhs.Size;
            Flags = rhs.Flags;
            Font = rhs.Font;
            Argb = rhs.Argb;
            CharFlags = rhs.CharFlags;
            Bidi = rhs.Bidi;
            Asc = rhs.Asc;
            Desc = rhs.Desc;
        }

        public MuPDFCharStyle()
        {
            Size = -1;
            Flags = -1;
            Font = "";
            Argb = 0;
            CharFlags = 0;
            Bidi = 0;
            Asc = 0;
            Desc = 0;
        }

        public override string ToString() =>
            $"{Size} {Flags} {Font} {Argb:x8} {CharFlags} {Bidi} {Asc} {Desc}";
    }
}
