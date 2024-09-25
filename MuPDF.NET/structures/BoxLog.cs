using mupdf;

namespace MuPDF.NET
{
    public class BoxLog
    {
        /// <summary>
        /// a type of rectangle
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// rectangle coordinates
        /// </summary>
        public Rect Box { get; set; }

        /// <summary>
        /// optional. layer name
        /// </summary>
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
