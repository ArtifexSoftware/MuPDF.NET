using mupdf;

namespace MuPDF.NET
{
    public class BoxLog
    {
        public string Type { get; set; }

        public Rect Box { get; set; }

        public string LayerName { get; set; }

        public BoxLog(string type = null, Rect box = null, string layername = null)
        {
            Type = type;
            Box = new Rect(box);
            LayerName = layername;
        }

        public BoxLog(string type = null, Rect box = null)
        {
            Type = type;
            Box = new Rect(box);
            LayerName = null;
        }

        public BoxLog(string type = null, fz_rect box = null, string layername = null)
        {
            Type = type;
            Box = new Rect(box);
            LayerName = layername;
        }
    }
}
