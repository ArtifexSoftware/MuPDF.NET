using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public struct LinkStruct
    {
        public Rect From;

        public LinkType Kind;

        public Point To;

        public int Page;

        public string Name;

        public string Uri;

        public int Zoom;

        public string File;

        public string Id;

        public int Xref;
    }
}
