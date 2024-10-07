namespace MuPDF.NET
{
    public class LinkInfo
    {
        /// <summary>
        /// describing the "hot spot" location on the page's visible representation
        /// </summary>
        public Rect From { get; set; }

        /// <summary>
        /// an integer indicating the kind of link
        /// </summary>
        public LinkType Kind { get; set; }

        public Point To { get; set; } = null;

        public string ToStr { get; set; } //used page number is less than 0

        /// <summary>
        /// page number
        /// </summary>
        public int Page { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// a string specifying the destination internet resource
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// zoom value
        /// </summary>
        public float Zoom { get; set; } = 0;

        /// <summary>
        /// a string specifying the destination file
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// xref of the item
        /// </summary>
        public int Xref { get; set; }

        /// <summary>
        /// true if italic item text, or omitted. PDF only
        /// </summary>
        public bool Italic { get; set; } = false;

        /// <summary>
        /// true if bold item text or omitted. PDF only
        /// </summary>
        public bool Bold { get; set; } = false;

        /// <summary>
        /// true if sub-items are folded, or omitted in toc. PDF only
        /// </summary>
        public bool Collapse { get; set; }

        /// <summary>
        /// item color in PDF RGB format
        /// </summary>
        public float[] Color { get; set; }

        public override string ToString()
        {
            return $"Kind = {(int)Kind}, Xref = {Xref}, Page = {Page}, To = {To.ToString()}, Zoom = {Zoom}, Collapse = {Collapse}";
        }
    }
}
