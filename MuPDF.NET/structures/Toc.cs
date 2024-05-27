namespace MuPDF.NET
{
    public class Toc
    {
        public int Level { get; set; }

        public string Title { get; set; }

        public int Page { get; set; }

        public LinkInfo Link { get; set; } = null;

        public override string ToString()
        {
            return $"Level={Level}, Title={Title}, Page={Page}, Link={Link != null}";
        }
    }
}
