namespace MuPDF.NET
{
    /// <summary>RGB color in PDF range 0–1 (MuPDF.NET <c>GetColor</c>).</summary>
    public readonly struct ColorRgb
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }

        public float r => R;
        public float g => G;
        public float b => B;

        public ColorRgb(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public static implicit operator float[](ColorRgb c) => new[] { c.R, c.G, c.B };

        public void Deconstruct(out float r, out float g, out float b)
        {
            r = R;
            g = G;
            b = B;
        }
    }
}
