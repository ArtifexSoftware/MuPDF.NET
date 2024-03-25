using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace MuPDF.NET
{
    public class BlockStruct
    {
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

        public FzBuffer Image;

        public string CsName;

        public vectoruc Digest;

        public List<LineStruct> Lines;
    }
}
