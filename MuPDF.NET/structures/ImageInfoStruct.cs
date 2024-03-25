using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class ImageInfoStruct
    {
        public string Ext;

        public int Smask;

        public float Width;

        public float Height;

        public int ColorSpace;

        public int Bpc;

        public float Xres;

        public float Yres;

        public string CsName;

        public byte[] Image;
    }
}
