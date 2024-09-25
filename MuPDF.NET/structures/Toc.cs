namespace MuPDF.NET
{
    public class Toc
    {
        /// <summary>
        /// hierarchy level
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 1-based source page number
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// included only if simpleis False. Contains details of the TOC item
        /// </summary>
        public dynamic Link { get; set; } = null;

        public override string ToString()
        {
            return $"Level={Level}, Title={Title}, Page={Page}, Link={Link != null}";
        }
    }
}
