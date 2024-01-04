using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMuPDF
{
    public struct PageDictionary
    {
        public float WIDTH;

        public float HEIGHT;

        public List<BlockDictionary> BLOCKS;
    }
}
