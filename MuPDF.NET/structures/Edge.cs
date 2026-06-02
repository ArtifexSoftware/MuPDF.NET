namespace MuPDF.NET
{
    // Legacy DTO used by MuPDF.NET Page.GetTables signatures.
    public class Edge
    {
        public float x0 { get; set; }
        public float x1 { get; set; }
        public float top { get; set; }
        public float bottom { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public string orientation { get; set; }
        public string object_type { get; set; }
        public float doctop { get; set; }
        public int page_number { get; set; }
        public float y0 { get; set; }
        public float y1 { get; set; }
    }
}
