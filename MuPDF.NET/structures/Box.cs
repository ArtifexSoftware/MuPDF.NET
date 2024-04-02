using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class Box
    {
        public Rect Rect;

        public Matrix Matrix;

        public dynamic this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Rect;
                    case 1: return Matrix;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
