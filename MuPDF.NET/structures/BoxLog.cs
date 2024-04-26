using mupdf;

namespace MuPDF.NET
{
    public class BoxLog
    {
        public string Code { get; set; }

        public Rect Box { get; set; }

        public string LayerName { get; set; }

        public BoxLog(string code = null, Rect box = null, string layername = null)
        {
            Code = code;
            Box = new Rect(box);
            LayerName = layername;
        }

        public BoxLog(string code = null, Rect box = null)
        {
            Code = code;
            Box = new Rect(box);
            LayerName = null;
        }

        public BoxLog(string code = null, fz_rect box = null, string layername = null)
        {
            Code = code;
            Box = new Rect(box);
            LayerName = layername;
        }
    }
}
