﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class Morph
    {
        public Point P;

        public Matrix M;

        public Morph()
        {
            P = new Point();
            M = new Matrix();
        }

        public Morph(Point p, Matrix m)
        {
            P = p;
            M = m;
        }

        public object this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return P;
                    case 1:
                        return M;
                }
                throw new NotImplementedException();
            }
        }

        public int Length
        {
            get
            {
                return 2;
            }
        }
    }
}