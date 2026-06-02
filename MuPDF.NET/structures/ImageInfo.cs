using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    public class ImageInfo
    {
        public string Ext { get; set; }

        public int Smask { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public int ColorSpace { get; set; }

        public int Bpc { get; set; }

        public float Xres { get; set; }

        public float Yres { get; set; }

        public string CsName { get; set; }

        public byte[] Image { get; set; }

        public byte Orientation { get; set; }

        public Matrix Matrix { get; set; }

        /// <summary>PyMuPDF dictionary-style access (<c>image["ext"]</c>).</summary>
        public object this[string key]
        {
            get
            {
                switch (key)
                {
                    case "ext": return Ext;
                    case "smask": return Smask;
                    case "width": return Width;
                    case "height": return Height;
                    case "colorspace": return ColorSpace;
                    case "bpc": return Bpc;
                    case "xres": return Xres;
                    case "yres": return Yres;
                    case "cs-name": return CsName;
                    case "image": return Image;
                    default: return null;
                }
            }
        }

        // Compatibility bridge: allows assignment from modern ExtractImage dictionary results.
        public static implicit operator ImageInfo(Dictionary<string, object> data)
        {
            if (data == null)
                return null;
            float F(string key) => data.TryGetValue(key, out var v) ? Convert.ToSingle(v) : 0f;
            int I(string key) => data.TryGetValue(key, out var v) ? Convert.ToInt32(v) : 0;
            string S(string key) => data.TryGetValue(key, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
            byte[] B(string key) => data.TryGetValue(key, out var v) && v is byte[] bytes ? bytes : Array.Empty<byte>();
            return new ImageInfo
            {
                Ext = S("ext"),
                Smask = I("smask"),
                Width = F("width"),
                Height = F("height"),
                ColorSpace = I("colorspace"),
                Bpc = I("bpc"),
                Xres = F("xres"),
                Yres = F("yres"),
                CsName = S("cs-name"),
                Image = B("image"),
            };
        }

        // Compatibility bridge: allows assignment from modern Page.GetImages tuple entries.
        public static implicit operator ImageInfo((int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter) value)
        {
            _ = value.xref;
            _ = value.altCs;
            _ = value.name;
            _ = value.filter;
            int smask = 0;
            if (!string.IsNullOrEmpty(value.smask))
                int.TryParse(value.smask, out smask);
            return new ImageInfo
            {
                Smask = smask,
                Width = value.width,
                Height = value.height,
                Bpc = value.bpc,
                CsName = value.colorspace ?? string.Empty,
            };
        }
    }
}
