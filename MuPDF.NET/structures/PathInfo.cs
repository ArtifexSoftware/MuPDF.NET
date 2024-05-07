namespace MuPDF.NET
{
    public class PathInfo
    {
        public List<Item> Items { get; set; }

        public string Type { get; set; }

        public bool EvenOdd { get; set; }

        public float FillOpacity { get; set; }

        public float[] Fill { get; set; }

        public Rect Rect { get; set; }

        public int SeqNo { get; set; }

        public string Layer { get; set; }

        public float Width { get; set; }

        public float StrokeOpacity { get; set; }

        public float LineJoin { get; set; }

        public bool ClosePath { get; set; }

        public string Dashes { get; set; }

        public float[] Color { get; set; }

        public List<LineCapType> LineCap { get; set; }

        public Rect Scissor { get; set; }

        public int Level { get; set; }

        public bool Isolated { get; set; }

        public bool Knockout { get; set; }

        public string BlendMode { get; set; }

        public float Opacity { get; set; }

        public PathInfo()
        {

        }

        public PathInfo(PathInfo path)
        {
            Items = new List<Item>(path.Items);
            Type = path.Type;
            EvenOdd = path.EvenOdd;
            FillOpacity = path.FillOpacity;

            if (path.Fill != null)
            {
                Fill = new float[path.Fill.Length];
                Array.Copy(path.Fill, Fill, Fill.Length);
            }

            Rect = new Rect(path.Rect);
            SeqNo = path.SeqNo;
            Layer = path.Layer;
            Width = path.Width;
            StrokeOpacity = path.StrokeOpacity;
            LineJoin = path.LineJoin;
            ClosePath = path.ClosePath;
            Dashes = path.Dashes;

            if (path.Color != null)
            {
                Color = new float[path.Color.Length];
                Array.Copy(path.Color, Color, Color.Length);
            }

            LineCap = new List<LineCapType>(path.LineCap);
            Scissor = path.Scissor;
            Level = path.Level;
            Isolated = path.Isolated;
            Knockout = path.Knockout;
            BlendMode = path.BlendMode;
            Opacity = path.Opacity;
        }
    }

    public class Item
    {
        public string Type { get; set; }

        public Rect Rect { get; set; }

        public int Orientation { get; set; } = -1;

        public Point LastPoint { get; set; }

        public Point P1 { get; set; }

        public Point P2 { get; set; }

        public Point P3 { get; set; }

        public Quad Quad { get; set; }

        public bool Equal(Item other)
        {
            if (Type != other.Type) return false;

            bool ret = true;
            ret = other != null && (Rect == null ? other.Rect == null : Rect.Equals(other.Rect));
            ret = other != null && (LastPoint == null ? other.LastPoint == null : LastPoint.Equals(other.LastPoint));
            ret = other != null && (P1 == null ? other.P1 == null : P1.Equals(other.P1));
            ret = other != null && (P2 == null ? other.P2 == null : P2.Equals(other.P2));
            ret = other != null && (P3 == null ? other.P3 == null : P3.Equals(other.P3));
            ret = other != null && (Quad == null ? other.Quad == null : Quad.Equals(other.Quad));

            return true;
        }
    }
}
