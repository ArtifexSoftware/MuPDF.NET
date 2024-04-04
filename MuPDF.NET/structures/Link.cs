namespace MuPDF.NET
{
    public class Link
    {
        public Rect From;

        public LinkType Kind;

        public Point To = new Point(0, 0);

        public string ToStr; //used page number is less than 0

        public int Page;

        public string Name;

        public string Uri;

        public float Zoom = 0;

        public string File;

        public string Id;

        public int Xref;

        public bool Italic = false;

        public bool Bold = false;

        public bool Collapse;

        public float[] Color = null;
    }
}
