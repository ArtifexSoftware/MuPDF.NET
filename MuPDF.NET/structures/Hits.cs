using System;
using System.Collections.Generic;
using mupdf;

namespace MuPDF.NET
{
    public class Hits
    {
        public int Len;

        public List<Quad> Quads;

        public float HFuzz;

        public float VFuzz;

        public override string ToString()
        {
            return $"Hits(len={Len} quads={Quads} hfuzz={HFuzz} vfuzz={VFuzz})";
        }
    }
}
