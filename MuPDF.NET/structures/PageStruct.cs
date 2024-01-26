using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public struct PageStruct
    {
        public float WIDTH;

        public float HEIGHT;

        public List<BlockStruct> BLOCKS;
    }
}
