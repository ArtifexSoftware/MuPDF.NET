namespace MuPDF.NET
{
    public class WordBlock
    {
        public float X0 { get; set; }

        public float Y0 { get; set; }

        public float X1 { get; set; }

        public float Y1 { get; set; }

        public string Text { get; set; }

        public int BlockNum { get; set; }

        public int LineNum { get; set; }

        public int WordNum { get; set; }

        /// <summary>Legacy MuPDF.NET / PyMuPDF tuple field names.</summary>
        public float x0
        {
            get => X0;
            set => X0 = value;
        }

        public float y0
        {
            get => Y0;
            set => Y0 = value;
        }

        public float x1
        {
            get => X1;
            set => X1 = value;
        }

        public float y1
        {
            get => Y1;
            set => Y1 = value;
        }

        public string word
        {
            get => Text;
            set => Text = value;
        }

        public int blockNo
        {
            get => BlockNum;
            set => BlockNum = value;
        }

        public int lineNo
        {
            get => LineNum;
            set => LineNum = value;
        }

        public int wordNo
        {
            get => WordNum;
            set => WordNum = value;
        }
    }
}
