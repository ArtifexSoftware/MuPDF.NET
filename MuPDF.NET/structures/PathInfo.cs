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

            Fill = new float[path.Fill.Length];
            Array.Copy(path.Fill, Fill, Fill.Length);

            Rect = new Rect(path.Rect);
            SeqNo = path.SeqNo;
            Layer = path.Layer;
            Width = path.Width;
            StrokeOpacity = path.StrokeOpacity;
            LineJoin = path.LineJoin;
            ClosePath = path.ClosePath;
            Dashes = path.Dashes;

            Color = new float[path.Color.Length];
            Array.Copy(path.Color, Color, Color.Length);

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

        public int Orientation { get; set; }

        public Point LastPoint { get; set; }

        public Point P1 { get; set; }

        public Point P2 { get; set; }

        public Point P3 { get; set; }

        public Quad Quad { get; set; }
    }
}
