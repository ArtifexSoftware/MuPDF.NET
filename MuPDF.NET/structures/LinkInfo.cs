namespace MuPDF.NET
{
    public class LinkInfo
    {
        public Rect From { get; set; }

        public LinkType Kind { get; set; }

        public Point To { get; set; } = null;

        public string ToStr { get; set; } //used page number is less than 0

        public int Page { get; set; }

        public string Name { get; set; }

        public string Uri { get; set; }

        public float Zoom { get; set; } = 0;

        public string File { get; set; }

        public string Id { get; set; }

        public int Xref { get; set; }

        public bool Italic { get; set; } = false;

        public bool Bold { get; set; } = false;

        public bool Collapse { get; set; }

        public float[] Color { get; set; }
    }
}
