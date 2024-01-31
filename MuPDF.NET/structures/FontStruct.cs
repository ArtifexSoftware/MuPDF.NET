using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class FontStruct
    {
        public int XREF;

        public string EXT;

        public string TYPE;

        public string NAME;

        public string REFNAME;

        public string ENCODING;

        public int STREAM_XREF;

        public int ORDERING;

        public bool SIMPLE;

        public List<(int, double)> GLYPHS;

        public float ASCENDER;
        
        public float DESCENDER;

    }
}
