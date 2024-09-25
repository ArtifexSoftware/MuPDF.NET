namespace MuPDF.NET
{
    public class Position
    {
        /// <summary>
        /// depth of this element in the box
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// the header level, 0 if no header, 1-6 for h1 - h6
        /// </summary>
        public int Heading { get; set; }

        /// <summary>
        /// value of the `href` attribute, or null if not defined
        /// </summary>
        public string Href { get; set; }

        /// <summary>
        /// value of the `id` attribute, or null if not defined
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// element position on page
        /// </summary>
        public Rect Rect { get; set; }

        /// <summary>
        /// immediate text of the element
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// bit 0 set: opens element, bit 1 set: closes element
        /// </summary>
        public bool OpenClose { get; set; }

        /// <summary>
        /// count of rectangles filled by the story so far
        /// </summary>
        public int RectNum { get; set; }

        /// <summary>
        /// page number
        /// </summary>
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
