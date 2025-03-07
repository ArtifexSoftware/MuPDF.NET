using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    public class PathInfo
    {
        /// <summary>
        /// List of draw commands: lines, rectangles, quads or curves
        /// </summary>
        public List<Item> Items { get; set; }

        /// <summary>
        /// Type of this path
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Fill colors of area overlaps
        /// </summary>
        public bool EvenOdd { get; set; }

        /// <summary>
        /// Fill color transparency
        /// </summary>
        public float FillOpacity { get; set; }

        /// <summary>
        /// Fill color
        /// </summary>
        public float[] Fill { get; set; }

        /// <summary>
        /// Page area covered by this path
        /// </summary>
        public Rect Rect { get; set; }

        /// <summary>
        /// Command number when building page appearance
        /// </summary>
        public int SeqNo { get; set; }

        /// <summary>
        /// Name of applicable Optional Content Group
        /// </summary>
        public string Layer { get; set; }

        /// <summary>
        /// Stroke line width
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// Stroke color transparency
        /// </summary>
        public float StrokeOpacity { get; set; }

        public float LineJoin { get; set; }

        /// <summary>
        /// Same as the parameter
        /// </summary>
        public bool ClosePath { get; set; }

        /// <summary>
        /// Dashed line specification
        /// </summary>
        public string Dashes { get; set; }

        /// <summary>
        /// Stroke color
        /// </summary>
        public float[] Color { get; set; }

        /// <summary>
        /// Number 3-tuple, use its max value on output with Shape
        /// </summary>
        public List<LineCapType> LineCap { get; set; }

        public Rect Scissor { get; set; }

        /// <summary>
        /// the hierarchy level
        /// </summary>
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
            ret = other != null && (Rect == null ? other.Rect == null : Rect.EqualTo(other.Rect));
            ret = other != null && (LastPoint == null ? other.LastPoint == null : LastPoint.EqualTo(other.LastPoint));
            ret = other != null && (P1 == null ? other.P1 == null : P1.EqualTo(other.P1));
            ret = other != null && (P2 == null ? other.P2 == null : P2.EqualTo(other.P2));
            ret = other != null && (P3 == null ? other.P3 == null : P3.EqualTo(other.P3));
            ret = other != null && (Quad == null ? other.Quad == null : Quad.EqualTo(other.Quad));

            return true;
        }
    }
}
