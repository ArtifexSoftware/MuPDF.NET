namespace MuPDF.NET
{
    public class AnnotStruct
    {
        public int Xref { get; set; }

        public string Text { get; set; }

        public int Align { get; set; }

        public Rect Rect { get; set; }

        public List<float> TextColor { get; set; }

        public string FontName { get; set; }

        public float FontSize { get; set; }

        public float[] Fill { get; set; }
    }
}
