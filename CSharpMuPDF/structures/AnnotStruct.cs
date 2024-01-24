using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public struct AnnotStruct
    {
        public int XREF;

        public string TEXT;

        public int ALIGN;

        public Rect RECT;

        public List<float> TEXTCOLOR;

        public string FONTNAME;

        public float FONTSIZE;

    }
}
