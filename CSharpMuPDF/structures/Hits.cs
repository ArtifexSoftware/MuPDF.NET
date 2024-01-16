using System;
using System.Collections.Generic;
using mupdf;

namespace CSharpMuPDF
{
    public class Hits
    {
        public int LEN;

        public List<Quad> QUADS;

        public float HFUZZ;

        public float VFUZZ;

        public override string ToString()
        {
            return $"Hits(len={LEN} quads={QUADS} hfuzz={HFUZZ} vfuzz={VFUZZ})";
        }
    }
}
