namespace MuPDF.NET
{
    public class Toc
    {
        public int Level { get; set; }

        public string Title { get; set; }

        public int Page { get; set; }

        public Link Link { get; set; } = null;
    }
}
