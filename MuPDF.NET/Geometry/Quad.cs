using System;
using System.Collections;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Quadrilateral in the plane (four corners: upper-left, upper-right, lower-left, lower-right).
    /// </summary>
    /// <remarks>
    /// <para>Used for text-search hits, text-markup annotations, and drawing on pages. Applying
    /// rotation, scale, or translation to a rectangle yields a <see cref="IsRectangular"/> quad.</para>
    /// <para>Ports </para>
    /// </remarks>
    public class Quad : IEnumerable<Point>, IEquatable<Quad>
    {
        /// <summary>Upper-left corner.</summary>
        public Point UL { get; set; }

        /// <summary>Upper-right corner.</summary>
        public Point UR { get; set; }

        /// <summary>Lower-left corner.</summary>
        public Point LL { get; set; }

        /// <summary>Lower-right corner.</summary>
        public Point LR { get; set; }

        /// <summary>Legacy name for <see cref="UL"/>.</summary>
        public Point UpperLeft { get => UL; set => UL = value; }

        /// <summary>Legacy name for <see cref="UR"/>.</summary>
        public Point UpperRight { get => UR; set => UR = value; }

        /// <summary>Legacy name for <see cref="LL"/>.</summary>
        public Point LowerLeft { get => LL; set => LL = value; }

        /// <summary>Legacy name for <see cref="LR"/>.</summary>
        public Point LowerRight { get => LR; set => LR = value; }

        /// <summary>Sequence length (always 4).</summary>
        public int Length => Count;

        /// <summary>Creates a quad of four copies of <c>(0, 0)</c>.</summary>
        public Quad()
        {
            UL = new Point();
            UR = new Point();
            LL = new Point();
            LR = new Point();
        }

        /// <summary>Creates a quad from four corner points.</summary>
        /// <param name="ul">Upper-left corner.</param>
        /// <param name="ur">Upper-right corner.</param>
        /// <param name="ll">Lower-left corner.</param>
        /// <param name="lr">Lower-right corner.</param>
        public Quad(Point ul, Point ur, Point ll, Point lr)
        {
            UL = new Point(ul);
            UR = new Point(ur);
            LL = new Point(ll);
            LR = new Point(lr);
        }

        /// <summary>Creates a copy of another quad.</summary>
        /// <param name="o">Source quad.</param>
        public Quad(Quad o)
        {
            UL = new Point(o.UL);
            UR = new Point(o.UR);
            LL = new Point(o.LL);
            LR = new Point(o.LR);
        }

        /// <summary>Creates a quad from the four corners of a rectangle.</summary>
        /// <param name="r">Source rectangle.</param>
        public Quad(Rect r)
        {
            UL = r.TopLeft;
            UR = r.TopRight;
            LL = r.BottomLeft;
            LR = r.BottomRight;
        }

        /// <summary>Creates a quad from a native MuPDF <c>fz_quad</c>.</summary>
        /// <param name="q">Native quad.</param>
        public Quad(mupdf.FzQuad q)
        {
            UL = new Point(q.ul.x, q.ul.y);
            UR = new Point(q.ur.x, q.ur.y);
            LL = new Point(q.ll.x, q.ll.y);
            LR = new Point(q.lr.x, q.lr.y);
        }

        /// <summary>Infinite quad (wraps <see cref="Rect.Infinite"/>).</summary>
        public static Quad Infinite => new Quad(Rect.Infinite);

        /// <summary>Number of corners (always 4).</summary>
        public int Count => 4;

        /// <summary>Gets or sets corner 0–3 (ul, ur, ll, lr).</summary>
        /// <param name="i">Corner index.</param>
        public Point this[int i]
        {
            get => i switch
            {
                0 => UL,
                1 => UR,
                2 => LL,
                3 => LR,
                _ => throw new IndexOutOfRangeException(),
            };
            set
            {
                switch (i)
                {
                    case 0: UL = value; break;
                    case 1: UR = value; break;
                    case 2: LL = value; break;
                    case 3: LR = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        /// <summary>Enclosed area; zero when <see cref="IsEmpty"/>.</summary>
        public float Area => IsEmpty ? 0.0f : (UL - UR).Norm * (UL - LL).Norm;

        /// <summary>Same as <see cref="Area"/>.</summary>
        public float Abs() => Area;

        /// <summary>Maximum length of the top and bottom edges.</summary>
        public float Width => Math.Max((UL - UR).Norm, (LL - LR).Norm);

        /// <summary>Maximum length of the left and right edges.</summary>
        public float Height => Math.Max((UL - LL).Norm, (UR - LR).Norm);

        /// <summary>
        /// True when enclosed area is zero (at least three corners collinear); the quad may still be degenerate.
        /// </summary>
        public bool IsEmpty => Width < Constants.Epsilon || Height < Constants.Epsilon;

        /// <summary>
        /// True when every segment between two corners lies inside the quad (convexity test).
        /// </summary>
        public bool IsConvex
        {
            get
            {
                float Cross(Point a, Point b, Point c) =>
                    (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                float c1 = Cross(UL, UR, LR), c2 = Cross(UR, LR, LL), c3 = Cross(LR, LL, UL), c4 = Cross(LL, UL, UR);
                return (c1 >= 0 && c2 >= 0 && c3 >= 0 && c4 >= 0) || (c1 <= 0 && c2 <= 0 && c3 <= 0 && c4 <= 0);
            }
        }

        /// <summary>True when this is the infinite quad sentinel.</summary>
        public bool IsInfinite => Rect.IsInfinite;

        /// <summary>
        /// True when all corner angles are 90° (implies convex and not empty); typical after rotate/scale/translate of a rect.
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

        /// <summary>Smallest axis-aligned rectangle containing all four corners.</summary>
        public Rect Rect
        {
            get
            {
                float x0 = Math.Min(Math.Min(UL.X, UR.X), Math.Min(LL.X, LR.X));
                float y0 = Math.Min(Math.Min(UL.Y, UR.Y), Math.Min(LL.Y, LR.Y));
                float x1 = Math.Max(Math.Max(UL.X, UR.X), Math.Max(LL.X, LR.X));
                float y1 = Math.Max(Math.Max(UL.Y, UR.Y), Math.Max(LL.Y, LR.Y));
                return new Rect(x0, y0, x1, y1);
            }
        }

        /// <summary>Whether a point lies inside this quad (<c>fz_is_point_inside_quad</c>).</summary>
        /// <param name="p">Point to test.</param>
        public bool Contains(Point p)
        {
            if (IsEmpty) return false;
            return mupdf.mupdf.fz_is_point_inside_quad(p.ToFzPoint(), ToFzQuad()) != 0;
        }

        /// <summary>
        /// Whether both opposite corners of <paramref name="r"/> lie inside this quad; empty rects are contained.
        /// </summary>
        /// <param name="r">Rectangle to test.</param>
        public bool Contains(Rect r)
        {
            if (r.IsEmpty) return true;
            if (IsEmpty) return false;
            return Contains(r.TopLeft) && Contains(r.BottomRight);
        }

        /// <summary>Whether all corners of <paramref name="other"/> lie inside this quad.</summary>
        /// <param name="other">Quad to test.</param>
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
        /// Returns a new quad after applying <paramref name="m"/> about <paramref name="fixpoint"/>.
        /// </summary>
        /// <param name="fixpoint">Fixed point for the transformation.</param>
        /// <param name="m">Matrix to apply.</param>
        /// <returns>Morphed quad, or <see cref="Infinite"/> when this quad is infinite.</returns>
        public Quad Morph(Point fixpoint, Matrix m)
        {
            if (IsInfinite) return Infinite;
            var di = new Matrix(1, 0, 0, 1, -fixpoint.X, -fixpoint.Y);
            var d = new Matrix(1, 0, 0, 1, fixpoint.X, fixpoint.Y);
            var mat = di * m * d;
            var u = new Point(UL);
            u.Transform(mat);
            var r = new Point(UR);
            r.Transform(mat);
            var l = new Point(LL);
            l.Transform(mat);
            var lr = new Point(LR);
            lr.Transform(mat);
            return new Quad(u, r, l, lr);
        }

        /// <summary>Transforms each corner in place with <paramref name="m"/>.</summary>
        /// <param name="m">Transformation matrix.</param>
        /// <returns>This quad after transformation.</returns>
        public Quad Transform(Matrix m)
        {
            UL.Transform(m);
            UR.Transform(m);
            LL.Transform(m);
            LR.Transform(m);
            return this;
        }

        /// <summary>Creates a native MuPDF <c>fz_quad</c>.</summary>
        public mupdf.FzQuad ToFzQuad() =>
            mupdf.mupdf.fz_make_quad((float)UL.X, (float)UL.Y, (float)UR.X, (float)UR.Y, (float)LL.X, (float)LL.Y, (float)LR.X, (float)LR.Y);

        /// <summary>Adds a point to every corner.</summary>
        public static Quad operator +(Quad a, Point p) => new Quad(a.UL + p, a.UR + p, a.LL + p, a.LR + p);

        /// <summary>Adds a scalar to every coordinate.</summary>
        public static Quad operator +(Quad a, float s) => new Quad(a.UL + s, a.UR + s, a.LL + s, a.LR + s);

        /// <summary>Subtracts a point from every corner.</summary>
        public static Quad operator -(Quad a, Point p) => new Quad(a.UL - p, a.UR - p, a.LL - p, a.LR - p);

        /// <summary>Negates every corner.</summary>
        public static Quad operator -(Quad a) => new Quad(-a.UL, -a.UR, -a.LL, -a.LR);

        /// <summary>Scales every corner by a factor.</summary>
        public static Quad operator *(Quad q, float s) => new Quad(q.UL * s, q.UR * s, q.LL * s, q.LR * s);

        /// <summary>Scales every corner by a factor.</summary>
        public static Quad operator *(float s, Quad q) => q * s;

        /// <summary>Transforms every corner by a matrix (new quad).</summary>
        public static Quad operator *(Quad q, Matrix m) => new Quad(q).Transform(m);

        /// <summary>Divides every corner by a scalar.</summary>
        public static Quad operator /(Quad q, float s) => q * (1.0f / s);

        /// <summary>Transforms every corner by the inverse of <paramref name="m"/>.</summary>
        /// <exception cref="DivideByZeroException">When the matrix is not invertible.</exception>
        public static Quad operator /(Quad q, Matrix m)
        {
            var inv = m.Inverted();
            if (inv == null)
                throw new DivideByZeroException("matrix is not invertible");
            return q * inv;
        }

        /// <summary>Corner-wise equality.</summary>
        public static bool operator ==(Quad? a, Quad? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.UL == b.UL && a.UR == b.UR && a.LL == b.LL && a.LR == b.LR;
        }

        /// <summary>Corner-wise inequality.</summary>
        public static bool operator !=(Quad? a, Quad? b) => !(a == b);

        /// <inheritdoc />
        public bool Equals(Quad? other) => this == other;

        /// <summary>Epsilon-based equality against another quad.</summary>
        public bool EqualTo(Quad obj) => obj != null && this == obj;

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as Quad);

        /// <inheritdoc />
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

        /// <summary>Returns a string with all four corners.</summary>
        public override string ToString() => $"Quad({UL}, {UR}, {LL}, {LR})";

        /// <inheritdoc />
        public IEnumerator<Point> GetEnumerator()
        {
            yield return UL;
            yield return UR;
            yield return LL;
            yield return LR;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal Point ul => UL;
        internal Point ur => UR;
        internal Point ll => LL;
        internal Point lr => LR;
    }
}