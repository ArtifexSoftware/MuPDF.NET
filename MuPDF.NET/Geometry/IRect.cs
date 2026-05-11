using System;
using System.Collections;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a rectangle with integer coordinates.
    /// </summary>
    public class IRect : IEnumerable<int>, IEquatable<IRect>
    {
        public int X0 { get; set; }
        public int Y0 { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }

        public IRect() { X0 = Y0 = X1 = Y1 = 0; }
        public IRect(int x0, int y0, int x1, int y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
        public IRect(IRect o) { X0 = o.X0; Y0 = o.Y0; X1 = o.X1; Y1 = o.Y1; }
        public IRect(Rect r) { X0 = (int)Math.Floor(r.X0 + 0.001); Y0 = (int)Math.Floor(r.Y0 + 0.001); X1 = (int)Math.Ceiling(r.X1 - 0.001); Y1 = (int)Math.Ceiling(r.Y1 - 0.001); }
        public IRect(mupdf.FzIrect r) { X0 = r.x0; Y0 = r.y0; X1 = r.x1; Y1 = r.y1; }

        /// <summary>
        /// The infinite integer rectangle.
        /// </summary>
        public static IRect Infinite => new IRect(Constants.FzMinInfRect, Constants.FzMinInfRect, Constants.FzMaxInfRect, Constants.FzMaxInfRect);

        public int Count => 4;
        public int this[int i]
        {
            get => i switch { 0 => X0, 1 => Y0, 2 => X1, 3 => Y1, _ => throw new IndexOutOfRangeException() };
            set { switch (i) { case 0: X0 = value; break; case 1: Y0 = value; break; case 2: X1 = value; break; case 3: Y1 = value; break; default: throw new IndexOutOfRangeException(); } }
        }

        /// <summary>
        /// Top-left corner.
        /// </summary>
        public Point TopLeft => new Point(X0, Y0);
        /// <summary>
        /// Top-right corner.
        /// </summary>
        public Point TopRight => new Point(X1, Y0);
        /// <summary>
        /// Bottom-left corner.
        /// </summary>
        public Point BottomLeft => new Point(X0, Y1);
        /// <summary>
        /// Bottom-right corner.
        /// </summary>
        public Point BottomRight => new Point(X1, Y1);
        public Point TL => TopLeft; public Point TR => TopRight;
        public Point BL => BottomLeft; public Point BR => BottomRight;
        /// <summary>
        /// Width.
        /// </summary>
        public int Width => Math.Max(0, X1 - X0);
        /// <summary>
        /// Height.
        /// </summary>
        public int Height => Math.Max(0, Y1 - Y0);
        /// <summary>
        /// Check if empty.
        /// </summary>
        public bool IsEmpty => X0 >= X1 || Y0 >= Y1;
        /// <summary>
        /// Check if infinite.
        /// </summary>
        public bool IsInfinite => X0 == Constants.FzMinInfRect && Y0 == Constants.FzMinInfRect && X1 == Constants.FzMaxInfRect && Y1 == Constants.FzMaxInfRect;
        /// <summary>
        /// Check if valid.
        /// </summary>
        public bool IsValid => X0 <= X1 && Y0 <= Y1;

        /// <summary>
        /// Calculate area.
        /// </summary>
        public double GetArea(string unit = "px")
        {
            if (IsEmpty || IsInfinite) return 0;
            double a = (double)Width * Height;
            return unit switch { "in" => a / 5184, "cm" => a * 6.4516 / 5184, "mm" => a * 645.16 / 5184, _ => a };
        }

        /// <summary>
        /// Extend rectangle to include point p.
        /// </summary>
        public IRect IncludePoint(Point p)
        {
            var r = new Rect(X0, Y0, X1, Y1); r.IncludePoint(p);
            X0 = (int)Math.Floor(r.X0); Y0 = (int)Math.Floor(r.Y0); X1 = (int)Math.Ceiling(r.X1); Y1 = (int)Math.Ceiling(r.Y1);
            return this;
        }

        /// <summary>
        /// Extend rectangle to include rectangle r.
        /// </summary>
        public IRect IncludeRect(IRect o)
        {
            if (o.IsInfinite || IsInfinite) { X0 = Y0 = Constants.FzMinInfRect; X1 = Y1 = Constants.FzMaxInfRect; return this; }
            if (o.IsEmpty) return this;
            if (IsEmpty) { X0 = o.X0; Y0 = o.Y0; X1 = o.X1; Y1 = o.Y1; return this; }
            if (o.X0 < X0) X0 = o.X0; if (o.Y0 < Y0) Y0 = o.Y0;
            if (o.X1 > X1) X1 = o.X1; if (o.Y1 > Y1) Y1 = o.Y1;
            return this;
        }

        /// <summary>
        /// Restrict rectangle to intersection with rectangle r.
        /// </summary>
        public IRect Intersect(IRect o)
        {
            if (o.IsInfinite) return this;
            if (IsInfinite) { X0 = o.X0; Y0 = o.Y0; X1 = o.X1; Y1 = o.Y1; return this; }
            if (o.IsEmpty) { X0 = o.X0; Y0 = o.Y0; X1 = o.X1; Y1 = o.Y1; return this; }
            if (IsEmpty) return this;
            if (X0 < o.X0) X0 = o.X0; if (Y0 < o.Y0) Y0 = o.Y0;
            if (X1 > o.X1) X1 = o.X1; if (Y1 > o.Y1) Y1 = o.Y1;
            return this;
        }

        /// <summary>
        /// Check if a point is in the rectangle.
        /// </summary>
        public bool Contains(Point p) => X0 <= p.X && p.X <= X1 && Y0 <= p.Y && p.Y <= Y1;
        /// <summary>
        /// Check if a rectangle is contained in this rectangle.
        /// </summary>
        public bool Contains(IRect r) => X0 <= r.X0 && r.X1 <= X1 && Y0 <= r.Y0 && r.Y1 <= Y1;

        /// <summary>
        /// Normalize the rectangle.
        /// </summary>
        public IRect Normalize()
        {
            if (X1 < X0) { int t = X0; X0 = X1; X1 = t; }
            if (Y1 < Y0) { int t = Y0; Y0 = Y1; Y1 = t; }
            return this;
        }

        /// <summary>
        /// Convert to Rect.
        /// </summary>
        public Rect ToRect() => new Rect(X0, Y0, X1, Y1);
        /// <summary>
        /// Return Quad version of rectangle.
        /// </summary>
        public Quad Quad => new Quad(TL, TR, BL, BR);
        public mupdf.FzIrect ToFzIRect() { var r = new mupdf.FzIrect(); r.x0 = X0; r.y0 = Y0; r.x1 = X1; r.y1 = Y1; return r; }

        public static IRect operator +(IRect a, IRect b) => new IRect(a.X0 + b.X0, a.Y0 + b.Y0, a.X1 + b.X1, a.Y1 + b.Y1);
        public static IRect operator -(IRect a, IRect b) => new IRect(a.X0 - b.X0, a.Y0 - b.Y0, a.X1 - b.X1, a.Y1 - b.Y1);
        public static IRect operator -(IRect a) => new IRect(-a.X0, -a.Y0, -a.X1, -a.Y1);
        public static IRect operator &(IRect a, IRect b) => new IRect(a).Intersect(b);
        public static IRect operator |(IRect a, IRect b) => new IRect(a).IncludeRect(b);

        public static bool operator ==(IRect? a, IRect? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.X0 == b.X0 && a.Y0 == b.Y0 && a.X1 == b.X1 && a.Y1 == b.Y1;
        }
        public static bool operator !=(IRect? a, IRect? b) => !(a == b);

        public bool Equals(IRect? other) => this == other;
        public override bool Equals(object? obj) => Equals(obj as IRect);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X0.GetHashCode();
                hash = hash * 31 + Y0.GetHashCode();
                hash = hash * 31 + X1.GetHashCode();
                hash = hash * 31 + Y1.GetHashCode();
                return hash;
            }
        }
        public override string ToString() => $"IRect({X0}, {Y0}, {X1}, {Y1})";

        public IEnumerator<int> GetEnumerator() { yield return X0; yield return Y0; yield return X1; yield return Y1; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Python/legacy compatibility aliases (mirrors _alias(IRect, ...)).
        public double get_area(string unit = "px") => GetArea(unit);
        public double getRectArea(string unit = "px") => get_area(unit);
        public IRect include_point(Point p) => IncludePoint(p);
        public IRect include_rect(IRect r) => IncludeRect(r);
        public bool is_empty() => IsEmpty;
        public bool is_infinite() => IsInfinite;
    }
}
