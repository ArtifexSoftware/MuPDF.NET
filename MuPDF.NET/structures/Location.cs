namespace MuPDF.NET
{
    public class Location
    {
        /// <summary>
        /// number of pages in chapter
        /// </summary>
        public int Chapter { get; set; }

        /// <summary>
        /// number of page
        /// </summary>
        public int Page { get; set; }

        // Compatibility bridge: allows assignment from modern location tuples.
        public static implicit operator Location((int chapter, int page) value)
            => new Location { Chapter = value.chapter, Page = value.page };
    }
}
