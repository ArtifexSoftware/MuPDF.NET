using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace MuPDF.NET
{
    /// <summary>
    /// Integer axis-aligned rectangle for pixel regions (same semantics as <see cref="Rect"/>).
    /// </summary>
    /// <remarks>
    /// <para>Used for pixmap clips and raster bounds. Coordinates are truncated/rounded from floats
    /// where noted. See <see cref="Rect"/> for validity, emptiness, and infinite-rect rules.</para>
    /// <para>Ports PyMuPDF <c>IRect</c>.</para>
    /// </remarks>
    public class IRect : IEnumerable<int>, IEquatable<IRect>
    {
        /// <summary>Left edge x coordinate.</summary>
        public int X0 { get; set; }

        /// <summary>Top edge y coordinate.</summary>
        public int Y0 { get; set; }

        /// <summary>Right edge x coordinate.</summary>
        public int X1 { get; set; }

        /// <summary>Bottom edge y coordinate.</summary>
        public int Y1 { get; set; }

        /// <summary>Creates the empty integer rectangle <c>(0, 0, 0, 0)</c>.</summary>
        public IRect() => X0 = Y0 = X1 = Y1 = 0;

        /// <summary>Creates an integer rectangle from four coordinates.</summary>
        public IRect(int x0, int y0, int x1, int y1)
        {
            X0 = x0; Y0 = y0; X1 = x1; Y1 = y1;
        }

        /// <summary>Creates an integer rect from top-left and bottom-right points (floored/ceiled).</summary>
        public IRect(Point topLeft, Point bottomRight) : this(
            (int)Math.Floor(topLeft.X), (int)Math.Floor(topLeft.Y),
            (int)Math.Ceiling(bottomRight.X), (int)Math.Ceiling(bottomRight.Y)) { }

        /// <summary>Creates an integer rect from top-left point and opposite corner coordinates.</summary>
        public IRect(Point topLeft, int x1, int y1) : this(
            (int)Math.Floor(topLeft.X), (int)Math.Floor(topLeft.Y), x1, y1) { }

        /// <summary>Creates an integer rect from top-left coordinates and bottom-right point.</summary>
        public IRect(int x0, int y0, Point bottomRight) : this(
            x0, y0, (int)Math.Ceiling(bottomRight.X), (int)Math.Ceiling(bottomRight.Y)) { }

        /// <summary>Creates a copy of another <see cref="IRect"/>.</summary>
        public IRect(IRect o) : this(o.X0, o.Y0, o.X1, o.Y1) { }

        /// <summary>Creates an integer rect from a float <see cref="Rect"/> (floor/ceil).</summary>
        public IRect(Rect r) : this(
            (int)Math.Floor(r.X0), (int)Math.Floor(r.Y0),
            (int)Math.Ceiling(r.X1), (int)Math.Ceiling(r.Y1)) { }

        /// <summary>Implicit conversion from <see cref="Rect"/>.</summary>
        public static implicit operator IRect(Rect r) => r == null ? null : new IRect(r);

        /// <summary>Creates an integer rect from a native <c>fz_irect</c>.</summary>
        public IRect(mupdf.FzIrect r) : this(r.x0, r.y0, r.x1, r.y1) { }

        /// <summary>Creates an integer rect from a native <c>fz_irect</c> struct.</summary>
        public IRect(mupdf.fz_irect r) : this(r.x0, r.y0, r.x1, r.y1) { }

        /// <summary>Builds an integer rect from positional args with optional keyword overrides.</summary>
        public static IRect Create(
            object[] args,
            Point? p0 = null,
            Point? p1 = null,
            float? x0 = null,
            float? y0 = null,
            float? x1 = null,
            float? y1 = null)
        {
            var t = MakeIrectInts(args, p0, p1, x0, y0, x1, y1);
            return new IRect(t.x0, t.y0, t.x1, t.y1);
        }

        /// <summary>Builds an integer rect from positional args.</summary>
        public static IRect Create(params object[] args)
        {
            var t = MakeIrectInts(args, null, null, null, null, null, null);
            return new IRect(t.x0, t.y0, t.x1, t.y1);
        }

        /// <summary>The unique infinite integer rectangle.</summary>
        public static IRect Infinite => new IRect(
            Constants.FzMinInfRect, Constants.FzMinInfRect,
            Constants.FzMaxInfRect, Constants.FzMaxInfRect);

        /// <summary>Number of components (always 4).</summary>
        public int Count => 4;

        /// <summary>Gets or sets <c>x0, y0, x1, y1</c> by index 0–3.</summary>
        public int this[int i]
        {
            get => i switch { 0 => X0, 1 => Y0, 2 => X1, 3 => Y1, _ => throw new IndexOutOfRangeException("index out of range") };
            set
            {
                var v = (int)value;
                switch (i)
                {
                    case 0: X0 = v; break;
                    case 1: Y0 = v; break;
                    case 2: X1 = v; break;
                    case 3: Y1 = v; break;
                    default: throw new IndexOutOfRangeException("index out of range");
                }
            }
        }

        /// <summary>Top-left corner <c>(x0, y0)</c>.</summary>
        public Point TopLeft => new Point(X0, Y0);

        /// <summary>Top-right corner <c>(x1, y0)</c>.</summary>
        public Point TopRight => new Point(X1, Y0);

        /// <summary>Bottom-left corner <c>(x0, y1)</c>.</summary>
        public Point BottomLeft => new Point(X0, Y1);

        /// <summary>Bottom-right corner <c>(x1, y1)</c>.</summary>
        public Point BottomRight => new Point(X1, Y1);

        /// <summary>Alias for <see cref="TopLeft"/>.</summary>
        public Point TL => TopLeft;

        /// <summary>Alias for <see cref="TopRight"/>.</summary>
        public Point TR => TopRight;

        /// <summary>Alias for <see cref="BottomLeft"/>.</summary>
        public Point BL => BottomLeft;

        /// <summary>Alias for <see cref="BottomRight"/>.</summary>
        public Point BR => BottomRight;

        /// <summary>Width <c>max(x1 - x0, 0)</c>.</summary>
        public int Width => Math.Max(0, X1 - X0);

        /// <summary>Height <c>max(y1 - y0, 0)</c>.</summary>
        public int Height => Math.Max(0, Y1 - Y0);

        /// <summary>True when <c>x0 &gt;= x1</c> or <c>y0 &gt;= y1</c>.</summary>
        public bool IsEmpty => X0 >= X1 || Y0 >= Y1;

        /// <summary>True for the unique infinite integer rectangle.</summary>
        public bool IsInfinite =>
            (X0 == Y0 && X0 == Constants.FzMinInfRect && X1 == Y1 && X1 == Constants.FzMaxInfRect)
            || float.IsNegativeInfinity(X0) || float.IsNegativeInfinity(Y0)
            || float.IsPositiveInfinity(X1) || float.IsPositiveInfinity(Y1);

        /// <summary>True when <c>x0 &lt;= x1</c> and <c>y0 &lt;= y1</c>.</summary>
        public bool IsValid => X0 <= X1 && Y0 <= Y1;

        /// <summary>Float rectangle with the same coordinates.</summary>
        public Rect Rect => new Rect(this);

        /// <summary>Quadrilateral <c>Quad(tl, tr, bl, br)</c>.</summary>
        public Quad Quad => new Quad(TL, TR, BL, BR);

        /// <summary>
        /// Area in square pixels; optional unit converts to in/cm/mm.
        /// </summary>
        /// <param name="unit">One of <c>px</c> (default), <c>in</c>, <c>cm</c>, or <c>mm</c>.</param>
        public float GetArea(string unit = "px") => RectArea(Width, Height, unit);

        /// <summary>Returns a new integer rect that also contains <paramref name="p"/>.</summary>
        public IRect IncludePoint(Point p)
        {
            var r = new Rect(this);
            r.IncludePoint(p);
            return Round(r);
        }

        /// <summary>Returns a new integer rect that also contains <paramref name="r"/>.</summary>
        public IRect IncludeRect(object r)
        {
            var rect = new Rect(this);
            rect.IncludeRect(CoerceRect(r));
            return Round(rect);
        }

        /// <summary>Replaces coordinates with intersection with <paramref name="r"/> (returns new <see cref="IRect"/>).</summary>
        public IRect Intersect(object r)
        {
            var rect = new Rect(this);
            rect.Intersect(CoerceRect(r));
            return Round(rect);
        }

        /// <summary>True when intersection with <paramref name="x"/> is a non-empty integer rect.</summary>
        public bool Intersects(object x) => new Rect(this).Intersects(CoerceRect(x));

        /// <summary>
        /// Whether <paramref name="x"/> is contained (point, rect, quad, or coordinate value).
        /// </summary>
        public bool Contains(object x)
        {
            if (x is float or double or int or long)
            {
                var v = Convert.ToDouble(x, CultureInfo.InvariantCulture);
                return v == X0 || v == Y0 || v == X1 || v == Y1;
            }
            if (Helpers.TryCoercePoint(x, out var p))
                return mupdf.mupdf.fz_is_point_inside_rect(p.ToFzPoint(), ToFzRect()) != 0;
            if (Helpers.TryCoerceRect(x, out var r))
                return X0 <= r.X0 && r.X1 <= X1 && Y0 <= r.Y0 && r.Y1 <= Y1;
            if (x is Quad q)
                return Contains(q.Rect);
            return false;
        }

        /// <summary>Makes the rectangle valid by swapping corners (finite rect).</summary>
        public IRect Normalize()
        {
            if (X1 < X0) { int t = X0; X0 = X1; X1 = t; }
            if (Y1 < Y0) { int t = Y0; Y0 = Y1; Y1 = t; }
            return this;
        }

        /// <summary>Rounds this rect via <c>fz_round_rect</c> (may yield empty while float rect is not).</summary>
        public IRect Round()
        {
            if (IsInfinite) return Infinite;
            if (IsEmpty) return new IRect();
            using var fr = ToFzRect();
            return new IRect(fr.fz_round_rect());
        }

        /// <summary>Returns a morphed quad about <paramref name="p"/> with matrix <paramref name="m"/>.</summary>
        public Quad Morph(Point p, Matrix m) =>
            IsInfinite ? Quad.Infinite : Quad.Morph(p, m);

        /// <summary>Euclidean norm of the four coordinates as a vector.</summary>
        public float Norm() => (float)Math.Sqrt((float)X0 * X0 + Y0 * Y0 + X1 * X1 + Y1 * Y1);

        /// <summary>
        /// Matrix mapping this rect to <paramref name="r"/> (delegates to <see cref="Rect.ToRect"/>).
        /// </summary>
        public Matrix ToRect(Rect r) => new Rect(this).ToRect(r);

        /// <summary>Transforms by <paramref name="m"/> and returns a rounded integer rect.</summary>
        public IRect Transform(Matrix m)
        {
            var rect = new Rect(this);
            rect.Transform(m);
            return Round(rect);
        }

        /// <summary>Native <c>fz_rect</c> built from integer coordinates.</summary>
        public mupdf.FzRect ToFzRect() =>
            mupdf.mupdf.fz_make_rect(X0, Y0, X1, Y1);

        /// <summary>Native <c>fz_irect</c> structure.</summary>
        public mupdf.FzIrect ToFzIRect()
        {
            var r = new mupdf.FzIrect();
            r.x0 = X0; r.y0 = Y0; r.x1 = X1; r.y1 = Y1;
            return r;
        }

        // ─── Operators (delegate to Rect, then round) ───

        public static IRect operator +(IRect a, IRect b) => Round(new Rect(a) + new Rect(b));
        public static IRect operator +(IRect a, float s) => Round(new Rect(a) + s);
        public static IRect operator +(IRect a, int s) => Round(new Rect(a) + s);

        public static IRect operator -(IRect a, IRect b) => Round(new Rect(a) - new Rect(b));
        public static IRect operator -(IRect a, float s) => Round(new Rect(a) - s);
        public static IRect operator -(IRect a) => new IRect(-a.X0, -a.Y0, -a.X1, -a.Y1);

        public static IRect operator *(IRect a, float m) => Round(new Rect(a) * m);
        public static IRect operator *(float m, IRect a) => Round(new Rect(a) * m);
        public static IRect operator *(IRect a, Matrix m) => Round(new Rect(a) * m);

        public static IRect operator /(IRect a, float m) => Round(new Rect(a) / m);
        public static IRect operator /(IRect a, Matrix m) => Round(new Rect(a) / m);

        public static IRect operator &(IRect a, IRect b) => Round(new Rect(a) & new Rect(b));
        public static IRect operator |(IRect a, IRect b) => Round(new Rect(a) | new Rect(b));
        public static IRect operator |(IRect a, Point p) => Round(new Rect(a) | p);

        public static IRect operator +(IRect a) => new IRect(a);

        public static bool operator ==(IRect? a, IRect? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.X0 == b.X0 && a.Y0 == b.Y0 && a.X1 == b.X1 && a.Y1 == b.Y1;
        }

        public static bool operator !=(IRect? a, IRect? b) => !(a == b);

        /// <inheritdoc />
        public bool Equals(IRect? other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is IRect ir) return this == ir;
            if (obj is IEnumerable seq && obj is not string)
            {
                var vals = new List<int>();
                foreach (var v in seq)
                {
                    if (v is int i) vals.Add(i);
                    else if (v is float d) vals.Add((int)d);
                    else if (v is double f) vals.Add((int)f);
                    else return false;
                }
                return vals.Count == 4 && X0 == vals[0] && Y0 == vals[1] && X1 == vals[2] && Y1 == vals[3];
            }
            return false;
        }

        /// <inheritdoc />
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

        /// <summary>Returns <c>IRect(x0, y0, x1, y1)</c>.</summary>
        public override string ToString() => $"IRect({X0}, {Y0}, {X1}, {Y1})";

        /// <inheritdoc />
        public IEnumerator<int> GetEnumerator()
        {
            yield return X0; yield return Y0; yield return X1; yield return Y1;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static (int x0, int y0, int x1, int y1) MakeIrectInts(
            object[] args,
            Point? p0,
            Point? p1,
            float? x0,
            float? y0,
            float? x1,
            float? y1) =>
            ToIrectInts(Rect.MakeRectCoords(args, p0, p1, x0, y0, x1, y1));

        private static (int x0, int y0, int x1, int y1) ToIrectInts(
            float x0, float y0, float x1, float y1) =>
            ((int)Math.Floor(x0), (int)Math.Floor(y0), (int)Math.Ceiling(x1), (int)Math.Ceiling(y1));

        private static (int x0, int y0, int x1, int y1) ToIrectInts(
            (float x0, float y0, float x1, float y1) c) =>
            ToIrectInts(c.x0, c.y0, c.x1, c.y1);

        private static IRect Round(Rect r)
        {
            if (r.IsInfinite) return Infinite;
            if (r.IsEmpty) return new IRect();
            using var fr = r.ToFzRect();
            return new IRect(fr.fz_round_rect());
        }

        private static Rect CoerceRect(object r) => Rect.CoerceRect(r);

        private static float RectArea(int width, int height, string unit)
        {
            if (width <= 0 || height <= 0) return 0;
            float a = (float)width * height;
            return unit switch
            {
                "in" => a / 5184,
                "cm" => a * 6.4516f / 5184,
                "mm" => a * 645.16f / 5184,
                _ => a,
            };
        }
    }
}
