namespace MuPDF.NET
{
    public class Annot
    {
        public int Xref { get; set; }

        public string Text { get; set; } = null;

        public int Align { get; set; } = 0;

        public Rect Rect { get; set; }

        public float[] TextColor { get; set; }

        public string FontName { get; set; }

        public float FontSize { get; set; }

        public float[] Fill { get; set; }
    }
}
