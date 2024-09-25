namespace MuPDF.NET
{
    public class PageInfo
    {
        /// <summary>
        /// width of the clip rectangle
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// height of the clip rectangle
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// list of Block
        /// </summary>
        public List<Block> Blocks { get; set; }
    }
}
