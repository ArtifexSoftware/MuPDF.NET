﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public struct PageStruct
    {
        public float Width;

        public float Height;

        public List<BlockStruct> Blocks;
    }
}