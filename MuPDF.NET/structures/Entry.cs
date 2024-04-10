using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class Entry
    {
        // image info struct

        public string Ext;

        public int Smask;

        public float Width;

        public float Height;

        public int Bpc;

        public string CsName;

        public string AltCsName;

        public string Filter;

        // font struct

        public int Xref;

        public string Type;

        public string Name;

        public string RefName;

        public string Encoding;

        public int StreamXref;

        // form info struct

        public Rect Bbox = null;
    }
}
