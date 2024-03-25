using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class LinkStruct
    {
        public Rect From;

        public LinkType Kind;

        public Point To;

        public int Page;

        public string Name;

        public string Uri;

        public float Zoom;

        public string File;

        public string Id;

        public int Xref;

        public bool Italic;

        public bool Bold;

        public bool Collapse;

        public float[] Color;
    }
}
