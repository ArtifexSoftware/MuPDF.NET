using System.Collections.Generic;

namespace MuPDF.NET
{
    public class Hits
    {
        public int Len { get; set; }

        public List<Quad> Quads { get; set; }

        public float HFuzz { get; set; }

        public float VFuzz { get; set; }

        public override string ToString()
        {
            return $"Hits(len={Len} quads={Quads} hfuzz={HFuzz} vfuzz={VFuzz})";
        }
    }
}
