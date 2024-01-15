using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMuPDF
{
    public struct PageStruct
    {
        public float WIDTH;

        public float HEIGHT;

        public List<BlockStruct> BLOCKS;
    }
}
