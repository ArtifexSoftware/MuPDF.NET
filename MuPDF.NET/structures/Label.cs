namespace MuPDF.NET
{
    public class Label
    {
        /// <summary>
        /// the first page number (0-based) to apply the label rule
        /// </summary>
        public int StartPage { get; set; }

        /// <summary>
        /// an arbitrary string to start the label with, e.g. "A-". Default is "".
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// start numbering with this value.
        /// </summary>
        public int FirstPageNum { get; set; }

        /// <summary>
        /// the numbering style
        /// </summary>
        public string Style { get; set; }
    }
}
