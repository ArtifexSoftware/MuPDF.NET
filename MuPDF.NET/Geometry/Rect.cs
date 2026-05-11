using System;
using System.Collections;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a rectangle in the plane, defined by four coordinates.
    /// </summary>
    public class Rect : IEnumerable<double>, IEquatable<Rect>
    {
        public double X0 { get; set; }
        public double Y0 { get; set; }
        public double X1 { get; set; }
        public double Y1 { get; set; }

        public Rect() { X0 = Y0 = X1 = Y1 = 0; }
        public Rect(double x0, double y0, double x1, double y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
        public Rect(Point tl, Point br) { X0 = tl.X; Y0 = tl.Y; X1 = br.X; Y1 = br.Y; }
        public Rect(Point tl, double x1, double y1) { X0 = tl.X; Y0 = tl.Y; X1 = x1; Y1 = y1; }
        public Rect(double x0, double y0, Point br) { X0 = x0; Y0 = y0; X1 = br.X; Y1 = br.Y; }
        public Rect(Rect o) { X0 = o.X0; Y0 = o.Y0; X1 = o.X1; Y1 = o.Y1; }
        public Rect(IRect o) { X0 = o.X0; Y0 = o.Y0; X1 = o.X1; Y1 = o.Y1; }
        public Rect(mupdf.FzRect r) { X0 = r.x0; Y0 = r.y0; X1 = r.x1; Y1 = r.y1; }
        public Rect(mupdf.fz_rect r) { X0 = r.x0; Y0 = r.y0; X1 = r.x1; Y1 = r.y1; }

        /// <summary>
        /// The infinite rectangle.
        /// </summary>
        public static Rect Infinite => new Rect(Constants.FzMinInfRect, Constants.FzMinInfRect, Constants.FzMaxInfRect, Constants.FzMaxInfRect);

        public int Count => 4;
        public double this[int i]
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
        public Point TL => TopLeft;
        public Point TR => TopRight;
        public Point BL => BottomLeft;
        public Point BR => BottomRight;
        /// <summary>
        /// Width of the rectangle.
        /// </summary>
        public double Width => Math.Max(0, X1 - X0);
        /// <summary>
        /// Height of the rectangle.
        /// </summary>
        public double Height => Math.Max(0, Y1 - Y0);
        /// <summary>
        /// Check if the rectangle is empty.
        /// </summary>
        public bool IsEmpty => X0 >= X1 || Y0 >= Y1;
        /// <summary>
        /// Check if the rectangle is infinite.
        /// </summary>
        public bool IsInfinite => X0 == Constants.FzMinInfRect && Y0 == Constants.FzMinInfRect && X1 == Constants.FzMaxInfRect && Y1 == Constants.FzMaxInfRect;
        /// <summary>
        /// Check if the rectangle is valid (non-degenerate).
        /// </summary>
        public bool IsValid => X0 <= X1 && Y0 <= Y1;

        /// <summary>
        /// Calculate area of the rectangle.
        /// </summary>
        public double GetArea(string unit = "px")
        {
            if (IsEmpty || IsInfinite) return 0.0;
            double a = Width * Height;
            return unit switch { "in" => a / 5184, "cm" => a * 6.4516 / 5184, "mm" => a * 645.16 / 5184, _ => a };
        }

        /// <summary>
        /// Extend rectangle to also contain a point.
        /// </summary>
        public Rect IncludePoint(Point p)
        {
            if (IsInfinite) return this;
            // Match PyMuPDF: util_include_point_in_rect -> mupdf.fz_include_point_in_rect
            // (pure C# path mishandled degenerate / empty rects, e.g. one-point then second point).
            using (var fr = ToFzRect())
            using (var fp = p.ToFzPoint())
            using (var merged = mupdf.mupdf.fz_include_point_in_rect(fr, fp))
            {
                X0 = merged.x0;
                Y0 = merged.y0;
                X1 = merged.x1;
                Y1 = merged.y1;
                return this;
            }
        }

        /// <summary>
        /// Extend rectangle to also contain another rectangle.
        /// </summary>
        public Rect IncludeRect(Rect r)
        {
            if (r.IsInfinite || IsInfinite) { X0 = Y0 = Constants.FzMinInfRect; X1 = Y1 = Constants.FzMaxInfRect; return this; }
            if (r.IsEmpty) return this;
            if (IsEmpty) { X0 = r.X0; Y0 = r.Y0; X1 = r.X1; Y1 = r.Y1; return this; }
            if (r.X0 < X0) X0 = r.X0; if (r.Y0 < Y0) Y0 = r.Y0;
            if (r.X1 > X1) X1 = r.X1; if (r.Y1 > Y1) Y1 = r.Y1;
            return this;
        }

        /// <summary>
        /// Compute intersection with another rectangle.
        /// </summary>
        public Rect Intersect(Rect r)
        {
            if (r.IsInfinite) return this;
            if (IsInfinite) { X0 = r.X0; Y0 = r.Y0; X1 = r.X1; Y1 = r.Y1; return this; }
            if (r.IsEmpty) { X0 = r.X0; Y0 = r.Y0; X1 = r.X1; Y1 = r.Y1; return this; }
            if (IsEmpty) return this;
            if (X0 < r.X0) X0 = r.X0; if (Y0 < r.Y0) Y0 = r.Y0;
            if (X1 > r.X1) X1 = r.X1; if (Y1 > r.Y1) Y1 = r.Y1;
            return this;
        }

        /// <summary>
        /// Check if rectangles have a non-empty intersection.
        /// </summary>
        public bool Intersects(Rect o) => !IsEmpty && !IsInfinite && !o.IsEmpty && !o.IsInfinite && X0 < o.X1 && o.X0 < X1 && Y0 < o.Y1 && o.Y0 < Y1;
        /// <summary>
        /// Check if the rectangle contains a point.
        /// </summary>
        public bool Contains(Point p) => X0 <= p.X && p.X <= X1 && Y0 <= p.Y && p.Y <= Y1;
        /// <summary>
        /// Check if the rectangle contains another rectangle.
        /// </summary>
        public bool Contains(Rect r) => X0 <= r.X0 && r.X1 <= X1 && Y0 <= r.Y0 && r.Y1 <= Y1;

        /// <summary>
        /// Return a normalized version of the rectangle.
        /// </summary>
        public Rect Normalize()
        {
            if (X1 < X0) { double t = X0; X0 = X1; X1 = t; }
            if (Y1 < Y0) { double t = Y0; Y0 = Y1; Y1 = t; }
            return this;
        }

        /// <summary>
        /// Return the smallest IRect containing this rectangle.
        /// </summary>
        public IRect Round()
        {
            if (IsInfinite) return IRect.Infinite;
            if (IsEmpty) return new IRect();
            return new IRect((int)Math.Floor(X0 + 0.001), (int)Math.Floor(Y0 + 0.001), (int)Math.Ceiling(X1 - 0.001), (int)Math.Ceiling(Y1 - 0.001));
        }

        /// <summary>
        /// Return the IRect.
        /// </summary>
        public IRect IRect => Round();
        /// <summary>
        /// Return Quad version of rectangle.
        /// </summary>
        public Quad Quad => new Quad(TL, TR, BL, BR);

        /// <summary>
        /// Morph the rectangle using a fixed point and a matrix.
        /// </summary>
        public Quad Morph(Point p, Matrix m) => IsInfinite ? Quad.Infinite : Quad.Morph(p, m);

        /// <summary>
        /// Transform rectangle by a matrix.
        /// </summary>
        public Rect Transform(Matrix m)
        {
            if (IsInfinite) return this;
            var q = Quad;
            var ul = new Point(q.UL); ul.Transform(m);
            var ur = new Point(q.UR); ur.Transform(m);
            var ll = new Point(q.LL); ll.Transform(m);
            var lr = new Point(q.LR); lr.Transform(m);
            X0 = Math.Min(Math.Min(ul.X, ur.X), Math.Min(ll.X, lr.X));
            Y0 = Math.Min(Math.Min(ul.Y, ur.Y), Math.Min(ll.Y, lr.Y));
            X1 = Math.Max(Math.Max(ul.X, ur.X), Math.Max(ll.X, lr.X));
            Y1 = Math.Max(Math.Max(ul.Y, ur.Y), Math.Max(ll.Y, lr.Y));
            return this;
        }

        /// <summary>
        /// Return matrix that converts to target rect.
        /// </summary>
        public Matrix ToRect(Rect r)
        {
            if (IsInfinite || IsEmpty || r.IsInfinite || r.IsEmpty)
                throw new InvalidOperationException("rectangles must be finite and not empty");
            return new Matrix(1, 0, 0, 1, -X0, -Y0) * new Matrix(r.Width / Width, r.Height / Height) * new Matrix(1, 0, 0, 1, r.X0, r.Y0);
        }

        public double Norm() => Math.Sqrt(X0 * X0 + Y0 * Y0 + X1 * X1 + Y1 * Y1);
        public mupdf.FzRect ToFzRect() => mupdf.mupdf.fz_make_rect((float)X0, (float)Y0, (float)X1, (float)Y1);

        public static Rect operator +(Rect a, Rect b) => new Rect(a.X0 + b.X0, a.Y0 + b.Y0, a.X1 + b.X1, a.Y1 + b.Y1);
        public static Rect operator +(Rect a, double s) => new Rect(a.X0 + s, a.Y0 + s, a.X1 + s, a.Y1 + s);
        public static Rect operator -(Rect a, Rect b) => new Rect(a.X0 - b.X0, a.Y0 - b.Y0, a.X1 - b.X1, a.Y1 - b.Y1);
        public static Rect operator -(Rect a, double s) => new Rect(a.X0 - s, a.Y0 - s, a.X1 - s, a.Y1 - s);
        public static Rect operator -(Rect a) => new Rect(-a.X0, -a.Y0, -a.X1, -a.Y1);
        public static Rect operator *(Rect a, double m) => new Rect(a.X0 * m, a.Y0 * m, a.X1 * m, a.Y1 * m);
        public static Rect operator *(double m, Rect a) => a * m;

        /// <summary>Python <c>Rect * Matrix</c>: transform corners and take the bounding rect.</summary>
        public static Rect operator *(Rect r, Matrix m) => new Rect(r).Transform(m);

        public static Rect operator /(Rect a, double m) { double i = 1.0 / m; return a * i; }

        /// <summary>Python <c>Rect / Matrix</c>: transform by inverse matrix.</summary>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="m"/> is singular.</exception>
        public static Rect operator /(Rect r, Matrix m)
        {
            var inv = m.Inverted();
            if (inv == null)
                throw new DivideByZeroException("matrix is not invertible");
            return r * inv;
        }
        public static Rect operator &(Rect a, Rect b) => new Rect(a).Intersect(b);
        public static Rect operator |(Rect a, Rect b) => new Rect(a).IncludeRect(b);
        public static Rect operator |(Rect a, Point p) => new Rect(a).IncludePoint(p);

        public static bool operator ==(Rect? a, Rect? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return Math.Abs(a.X0 - b.X0) < Constants.Epsilon && Math.Abs(a.Y0 - b.Y0) < Constants.Epsilon
                && Math.Abs(a.X1 - b.X1) < Constants.Epsilon && Math.Abs(a.Y1 - b.Y1) < Constants.Epsilon;
        }
        public static bool operator !=(Rect? a, Rect? b) => !(a == b);

        public bool Equals(Rect? other) => this == other;
        public override bool Equals(object? obj) => Equals(obj as Rect);
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
        public override string ToString() => $"Rect({X0}, {Y0}, {X1}, {Y1})";

        public IEnumerator<double> GetEnumerator() { yield return X0; yield return Y0; yield return X1; yield return Y1; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Python/legacy compatibility aliases (mirrors _alias(Rect, ...)).
        public double get_area(string unit = "px") => GetArea(unit);
        public double getRectArea(string unit = "px") => get_area(unit);
        public Rect include_point(Point p) => IncludePoint(p);
        public Rect include_rect(Rect r) => IncludeRect(r);
        public bool is_empty() => IsEmpty;
        public bool is_infinite() => IsInfinite;
    }
}
