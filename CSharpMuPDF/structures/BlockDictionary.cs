using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace CSharpMuPDF
{
    public struct BlockDictionary
    {
        public int NUMBER;

        public int TYPE;

        public FzRect BBOX;

        public int WIDTH;

        public int HEIGHT;

        public string EXT;

        public int COLORSPACE;

        public int XRES;

        public int YRES;

        public byte BPC;

        public FzMatrix MATRIX;

        public uint SIZE;

        public FzBuffer IMAGE;

        public string CSNAME;

        public vectoruc DIGEST;

        public List<LineDictionary> lines;
    }
}
