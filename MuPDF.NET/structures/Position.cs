namespace MuPDF.NET
{
    public class Position
    {
        public int Depth { get; set; }

        public int Heading { get; set; }

        public string Href { get; set; }

        public string Id { get; set; }

        public Rect Rect { get; set; }

        public string Text { get; set; }

        public bool OpenClose { get; set; }

        public int RectNum { get; set; }

        public int PageNum { get; set; }

        public Position() { }

        public Position(Position arg)
        {
            Depth = arg.Depth;
            Heading = arg.Heading;
            Href = arg.Href;
            Id = arg.Id;
            Rect = arg.Rect;
            Text = arg.Text;
            OpenClose = arg.OpenClose;
            RectNum = arg.RectNum;
            PageNum = arg.PageNum;
        }
    }
}
