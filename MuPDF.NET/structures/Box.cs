using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class Box
    {
        /// <summary>
        /// boundary box
        /// </summary>
        public Rect Rect { get; set; }

        /// <summary>
        /// respective transformation matricx
        /// </summary>
        public Matrix Matrix { get; set; }

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
