using System;
using System.Collections.Generic;
using System.Linq;

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

        // Compatibility bridge: allows assignment from modern GetDrawings dictionaries.
        public static implicit operator PathInfo(Dictionary<string, object> data)
        {
            if (data == null)
                return null;
            float F(string key) => data.TryGetValue(key, out var v) ? Convert.ToSingle(v) : 0f;
            int I(string key) => data.TryGetValue(key, out var v) ? Convert.ToInt32(v) : 0;
            bool Z(string key) => data.TryGetValue(key, out var v) && Convert.ToBoolean(v);
            string S(string key) => data.TryGetValue(key, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
            float[] A(string key) => data.TryGetValue(key, out var v) && v is float[] arr ? arr : Array.Empty<float>();
            List<LineCapType> LineCaps(string key)
            {
                if (!data.TryGetValue(key, out var v) || v is not object[] caps)
                    return null;
                return caps.Select(c => (LineCapType)Convert.ToInt32(c)).ToList();
            }
            return new PathInfo
            {
                Type = S("type"),
                EvenOdd = Z("even_odd"),
                FillOpacity = F("fill_opacity"),
                Fill = A("fill"),
                Rect = data.TryGetValue("rect", out var rect) ? rect as Rect : null,
                SeqNo = I("seqno"),
                Layer = S("layer"),
                Width = F("width"),
                StrokeOpacity = F("stroke_opacity"),
                LineJoin = F("lineJoin"),
                ClosePath = Z("closePath"),
                Dashes = S("dashes"),
                Color = A("color"),
                LineCap = LineCaps("lineCap"),
                Scissor = data.TryGetValue("scissor", out var sc) ? sc as Rect : null,
                Level = I("level"),
                Isolated = Z("isolated"),
                Knockout = Z("knockout"),
                BlendMode = S("blendmode"),
                Opacity = F("opacity"),
                Items = ItemsFromDict(data),
            };
        }

        private static List<Item> ItemsFromDict(Dictionary<string, object> data)
        {
            if (!data.TryGetValue("items", out var itemsObj) || itemsObj is not List<object> raw)
                return null;

            var items = new List<Item>(raw.Count);
            foreach (var entry in raw)
            {
                Item? item = ItemFromDictEntry(entry);
                if (item != null)
                    items.Add(item);
            }
            return items;
        }

        private static Item? ItemFromDictEntry(object entry)
        {
            if (entry is not object[] oa || oa.Length == 0)
                return null;

            string cmd = oa[0]?.ToString() ?? string.Empty;
            var item = new Item { Type = cmd };

            switch (cmd)
            {
                case "l":
                    if (oa.Length >= 3)
                    {
                        item.P1 = CoercePoint(oa[1]);
                        item.LastPoint = CoercePoint(oa[2]);
                    }
                    break;
                case "c":
                    if (oa.Length >= 5)
                    {
                        item.P1 = CoercePoint(oa[1]);
                        item.P2 = CoercePoint(oa[2]);
                        item.P3 = CoercePoint(oa[3]);
                        item.LastPoint = CoercePoint(oa[4]);
                    }
                    break;
                case "re":
                    if (oa.Length >= 2)
                    {
                        item.Rect = CoerceRect(oa[1]);
                        if (oa.Length >= 3)
                            item.Orientation = Convert.ToInt32(oa[2]);
                    }
                    break;
                case "qu":
                    if (oa.Length >= 2 && oa[1] is Quad q)
                        item.Quad = q;
                    break;
            }

            return item;
        }

        private static Point CoercePoint(object? o)
        {
            if (o is Point p)
                return new Point(p);
            if (o is mupdf.FzPoint fp)
                return Helpers.PointFromFz(fp);
            if (o != null && Helpers.TryCoercePoint(o, out var pt))
                return pt;
            return null;
        }

        private static Rect CoerceRect(object? o)
        {
            if (o is Rect r)
                return r;
            if (o is mupdf.FzRect fr)
                return new Rect(fr);
            if (o != null && Helpers.TryCoerceRect(o, out var rect))
                return rect;
            return null;
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
    }
}
