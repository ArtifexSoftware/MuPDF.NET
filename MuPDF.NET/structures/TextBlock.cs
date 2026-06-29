namespace MuPDF.NET
{
    public class TextBlock
    {
        public float X0 { get; set; }

        public float Y0 { get; set; }

        public float X1 { get; set; }

        public float Y1 { get; set; }

        public string Text { get; set; }

        public int BlockNum { get; set; }

        public int Type { get; set; }

        /// <summary>Legacy tuple deconstruction for <c>foreach (var (x0, y0, ...) in ExtractBlocks())</c>.</summary>
        public void Deconstruct(
            out float x0,
            out float y0,
            out float x1,
            out float y1,
            out string text,
            out int blockNo,
            out int blockType)
        {
            x0 = X0;
            y0 = Y0;
            x1 = X1;
            y1 = Y1;
            text = Text;
            blockNo = BlockNum;
            blockType = Type;
        }
    }
}
