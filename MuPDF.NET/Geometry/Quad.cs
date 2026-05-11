using System;
using System.Collections;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a quadrilateral defined by four corner Points.
    /// </summary>
    public class Quad : IEnumerable<Point>, IEquatable<Quad>
    {
        /// <summary>
        /// Upper-left corner.
        /// </summary>
        public Point UL { get; set; }
        /// <summary>
        /// Upper-right corner.
        /// </summary>
        public Point UR { get; set; }
        /// <summary>
        /// Lower-left corner.
        /// </summary>
        public Point LL { get; set; }
        /// <summary>
        /// Lower-right corner.
        /// </summary>
        public Point LR { get; set; }

        public Quad() { UL = new Point(); UR = new Point(); LL = new Point(); LR = new Point(); }
        public Quad(Point ul, Point ur, Point ll, Point lr) { UL = new Point(ul); UR = new Point(ur); LL = new Point(ll); LR = new Point(lr); }
        public Quad(Quad o) { UL = new Point(o.UL); UR = new Point(o.UR); LL = new Point(o.LL); LR = new Point(o.LR); }
        public Quad(Rect r) { UL = r.TopLeft; UR = r.TopRight; LL = r.BottomLeft; LR = r.BottomRight; }
        public Quad(mupdf.FzQuad q) { UL = new Point(q.ul.x, q.ul.y); UR = new Point(q.ur.x, q.ur.y); LL = new Point(q.ll.x, q.ll.y); LR = new Point(q.lr.x, q.lr.y); }

        public static Quad Infinite => new Quad(Rect.Infinite);

        public int Count => 4;
        public Point this[int i]
        {
            get => i switch { 0 => UL, 1 => UR, 2 => LL, 3 => LR, _ => throw new IndexOutOfRangeException() };
            set { switch (i) { case 0: UL = value; break; case 1: UR = value; break; case 2: LL = value; break; case 3: LR = value; break; default: throw new IndexOutOfRangeException(); } }
        }

        /// <summary>
        /// Area of the quad.
        /// </summary>
        public double Area => IsEmpty ? 0.0 : (UL - UR).Norm * (UL - LL).Norm;
        /// <summary>
        /// Width of the quad (max of top and bottom edge lengths).
        /// </summary>
        public double Width => Math.Max((UL - UR).Norm, (LL - LR).Norm);
        /// <summary>
        /// Height of the quad (max of left and right edge lengths).
        /// </summary>
        public double Height => Math.Max((UL - LL).Norm, (UR - LR).Norm);
        /// <summary>
        /// Check whether all quad corners are on the same line.
        /// </summary>
        public bool IsEmpty => Width < Constants.Epsilon || Height < Constants.Epsilon;

        /// <summary>
        /// Check if the quad is convex.
        /// </summary>
        public bool IsConvex
        {
            get
            {
                double Cross(Point a, Point b, Point c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                double c1 = Cross(UL, UR, LR), c2 = Cross(UR, LR, LL), c3 = Cross(LR, LL, UL), c4 = Cross(LL, UL, UR);
                return (c1 >= 0 && c2 >= 0 && c3 >= 0 && c4 >= 0) || (c1 <= 0 && c2 <= 0 && c3 <= 0 && c4 <= 0);
            }
        }

        /// <summary>
        /// Check whether this is the infinite quad.
        /// </summary>
        public bool IsInfinite => Rect.IsInfinite;

        /// <summary>
        /// Check if the quad is a rectangle.
        /// </summary>
        public bool IsRectangular
        {
            get
            {
                var s = new Point(UR.X - UL.X, UR.Y - UL.Y);
                var t = new Point(LL.X - UL.X, LL.Y - UL.Y);
                if (Math.Abs(s * t) > Constants.Epsilon) return false;
                s = new Point(LR.X - UR.X, LR.Y - UR.Y);
                t = new Point(UL.X - UR.X, UL.Y - UR.Y);
                if (Math.Abs(s * t) > Constants.Epsilon) return false;
                s = new Point(LL.X - LR.X, LL.Y - LR.Y);
                t = new Point(UR.X - LR.X, UR.Y - LR.Y);
                if (Math.Abs(s * t) > Constants.Epsilon) return false;
                return true;
            }
        }

        /// <summary>
        /// Smallest enclosing rectangle.
        /// </summary>
        public Rect Rect
        {
            get
            {
                double x0 = Math.Min(Math.Min(UL.X, UR.X), Math.Min(LL.X, LR.X));
                double y0 = Math.Min(Math.Min(UL.Y, UR.Y), Math.Min(LL.Y, LR.Y));
                double x1 = Math.Max(Math.Max(UL.X, UR.X), Math.Max(LL.X, LR.X));
                double y1 = Math.Max(Math.Max(UL.Y, UR.Y), Math.Max(LL.Y, LR.Y));
                return new Rect(x0, y0, x1, y1);
            }
        }

        /// <summary>
        /// Whether <paramref name="p"/> lies inside this quad (Python <c>p in quad</c>; MuPDF <c>fz_is_point_inside_quad</c>).
        /// </summary>
        public bool Contains(Point p)
        {
            if (IsEmpty) return false;
            var fzP = p.ToFzPoint();
            var fzQ = ToFzQuad();
            return mupdf.mupdf.fz_is_point_inside_quad(fzP, fzQ) != 0;
        }

        /// <summary>
        /// Whether both opposite corners of <paramref name="r"/> lie inside this quad (Python <c>rect in quad</c>).
        /// Empty rectangles are considered contained, matching PyMuPDF.
        /// </summary>
        public bool Contains(Rect r)
        {
            if (r.IsEmpty) return true;
            if (IsEmpty) return false;
            return Contains(r.TopLeft) && Contains(r.BottomRight);
        }

        /// <summary>
        /// Whether all corners of <paramref name="other"/> lie inside this quad (Python <c>quad in quad</c>).
        /// </summary>
        public bool Contains(Quad other)
        {
            if (IsEmpty) return false;
            for (int i = 0; i < 4; i++)
            {
                if (!Contains(other[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Morph the quad using a fixed point and a matrix.
        /// </summary>
        public Quad Morph(Point fixpoint, Matrix m)
        {
            if (IsInfinite) return Quad.Infinite;
            var di = new Matrix(1, 0, 0, 1, -fixpoint.X, -fixpoint.Y);
            var d = new Matrix(1, 0, 0, 1, fixpoint.X, fixpoint.Y);
            var mat = di * m * d;
            var u = new Point(UL); u.Transform(mat);
            var r = new Point(UR); r.Transform(mat);
            var l = new Point(LL); l.Transform(mat);
            var lr = new Point(LR); lr.Transform(mat);
            return new Quad(u, r, l, lr);
        }

        /// <summary>
        /// Transform the quad by a matrix.
        /// </summary>
        public Quad Transform(Matrix m) { UL.Transform(m); UR.Transform(m); LL.Transform(m); LR.Transform(m); return this; }

        public mupdf.FzQuad ToFzQuad() => mupdf.mupdf.fz_make_quad((float)UL.X, (float)UL.Y, (float)UR.X, (float)UR.Y, (float)LL.X, (float)LL.Y, (float)LR.X, (float)LR.Y);

        public static Quad operator +(Quad a, Point p) => new Quad(a.UL + p, a.UR + p, a.LL + p, a.LR + p);
        public static Quad operator +(Quad a, double s) => new Quad(a.UL + s, a.UR + s, a.LL + s, a.LR + s);
        public static Quad operator -(Quad a, Point p) => new Quad(a.UL - p, a.UR - p, a.LL - p, a.LR - p);
        public static Quad operator -(Quad a) => new Quad(-a.UL, -a.UR, -a.LL, -a.LR);

        /// <summary>Python <c>Quad * scalar</c>: scale each corner.</summary>
        public static Quad operator *(Quad q, double s) =>
            new Quad(q.UL * s, q.UR * s, q.LL * s, q.LR * s);

        public static Quad operator *(double s, Quad q) => q * s;

        /// <summary>Python <c>Quad * Matrix</c>: transform each corner.</summary>
        public static Quad operator *(Quad q, Matrix m) => new Quad(q).Transform(m);

        /// <summary>Python <c>Quad / scalar</c>.</summary>
        public static Quad operator /(Quad q, double s) => q * (1.0 / s);

        /// <summary>Python <c>Quad / Matrix</c>: transform by inverse matrix.</summary>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="m"/> is singular.</exception>
        public static Quad operator /(Quad q, Matrix m)
        {
            var inv = m.Inverted();
            if (inv == null)
                throw new DivideByZeroException("matrix is not invertible");
            return q * inv;
        }

        public static bool operator ==(Quad? a, Quad? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.UL == b.UL && a.UR == b.UR && a.LL == b.LL && a.LR == b.LR;
        }
        public static bool operator !=(Quad? a, Quad? b) => !(a == b);

        public bool Equals(Quad? other) => this == other;
        public override bool Equals(object? obj) => Equals(obj as Quad);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + UL.GetHashCode();
                hash = hash * 31 + UR.GetHashCode();
                hash = hash * 31 + LL.GetHashCode();
                hash = hash * 31 + LR.GetHashCode();
                return hash;
            }
        }
        public override string ToString() => $"Quad({UL}, {UR}, {LL}, {LR})";

        public IEnumerator<Point> GetEnumerator() { yield return UL; yield return UR; yield return LL; yield return LR; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Python/legacy compatibility aliases (mirrors _alias(Quad, ...)).
        public bool is_convex() => IsConvex;
        public bool is_empty() => IsEmpty;
        public bool is_rectangular() => IsRectangular;
        /// <summary>Python <c>item in quad</c> alias for <see cref="Contains(Point)"/> / <see cref="Contains(Rect)"/> / <see cref="Contains(Quad)"/>.</summary>
        public bool contains(Point p) => Contains(p);
        public bool contains(Rect r) => Contains(r);
        public bool contains(Quad q) => Contains(q);
    }
}
