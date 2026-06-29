using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace MuPDF.NET
{
    /// <summary>
    /// Axis-aligned rectangle (<see cref="X0"/>, <see cref="Y0"/>, <see cref="X1"/>, <see cref="Y1"/>).
    /// </summary>
    /// <remarks>
    /// <para>PDF coordinates in points (72 per inch). Valid when <c>x0 &lt;= x1</c> and <c>y0 &lt;= y1</c>;
    /// empty when <c>x0 &gt;= x1</c> or <c>y0 &gt;= y1</c>. One infinite rect exists (<see cref="Infinite"/>).
    /// Right and bottom edges are semi-open (not included in containment).</para>
    /// <para>Ports PyMuPDF <c>Rect</c>.</para>
    /// </remarks>
    public class Rect : IEnumerable<float>, IEquatable<Rect>
    {
        /// <summary>Left edge x coordinate (top-left and bottom-left).</summary>
        public float X0 { get; set; }

        /// <summary>Top edge y coordinate.</summary>
        public float Y0 { get; set; }

        /// <summary>Right edge x coordinate (excluded from containment).</summary>
        public float X1 { get; set; }

        /// <summary>Bottom edge y coordinate (excluded from containment).</summary>
        public float Y1 { get; set; }

        /// <summary>Sequence length (always 4).</summary>
        public int Length => Count;

        /// <summary>Creates the empty rectangle <c>(0, 0, 0, 0)</c>.</summary>
        public Rect() => X0 = Y0 = X1 = Y1 = 0;

        /// <summary>Creates a rectangle from four coordinates.</summary>
        public Rect(float X0, float Y0, float X1, float Y1)
        {
            this.X0 = X0; this.Y0 = Y0; this.X1 = X1; this.Y1 = Y1;
        }

        /// <summary>Creates a rectangle from top-left and bottom-right points.</summary>
        public Rect(Point topLeft, Point bottomRight)
            : this(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y) { }

        /// <summary>Creates a rectangle from top-left point and opposite corner coordinates.</summary>
        public Rect(Point topLeft, float x1, float y1)
            : this(topLeft.X, topLeft.Y, x1, y1) { }

        /// <summary>Creates a rectangle from top-left coordinates and bottom-right point.</summary>
        public Rect(float x0, float y0, Point bottomRight)
            : this(x0, y0, bottomRight.X, bottomRight.Y) { }

        /// <summary>Creates a copy of another rectangle.</summary>
        public Rect(Rect o) : this(o.X0, o.Y0, o.X1, o.Y1) { }

        /// <summary>Creates a float rectangle from an <see cref="IRect"/>.</summary>
        public Rect(IRect o) : this(o.X0, o.Y0, o.X1, o.Y1) { }

        /// <summary>Creates a rectangle from a native <c>fz_rect</c>.</summary>
        public Rect(mupdf.FzRect r) : this(r.x0, r.y0, r.x1, r.y1) { }

        /// <summary>Creates a rectangle from a native <c>fz_rect</c> struct.</summary>
        public Rect(mupdf.fz_rect r) : this(r.x0, r.y0, r.x1, r.y1) { }

        /// <summary>Builds a rectangle from positional args with optional keyword overrides.</summary>
        public static Rect Create(
            object[] args,
            Point? p0 = null,
            Point? p1 = null,
            float? x0 = null,
            float? y0 = null,
            float? x1 = null,
            float? y1 = null)
        {
            var c = MakeRectCoords(args, p0, p1, x0, y0, x1, y1);
            return new Rect(c.x0, c.y0, c.x1, c.y1);
        }

        /// <summary>Builds a rectangle from positional args.</summary>
        public static Rect Create(params object[] args)
        {
            var c = MakeRectCoords(args, null, null, null, null, null, null);
            return new Rect(c.x0, c.y0, c.x1, c.y1);
        }

        /// <summary>The unique infinite rectangle (contains every other rect).</summary>
        public static Rect Infinite => new Rect(
            Constants.FzMinInfRect, Constants.FzMinInfRect,
            Constants.FzMaxInfRect, Constants.FzMaxInfRect);

        /// <summary>Number of components (always 4).</summary>
        public int Count => 4;

        /// <summary>Gets or sets <c>x0, y0, x1, y1</c> by index 0–3.</summary>
        public float this[int i]
        {
            get => i switch { 0 => X0, 1 => Y0, 2 => X1, 3 => Y1, _ => throw new IndexOutOfRangeException("index out of range") };
            set
            {
                var v = (float)value;
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
        public float Width => Math.Max(0, X1 - X0);

        /// <summary>Height <c>max(y1 - y0, 0)</c>.</summary>
        public float Height => Math.Max(0, Y1 - Y0);

        /// <summary>True when <c>x0 &gt;= x1</c> or <c>y0 &gt;= y1</c> (includes invalid rects).</summary>
        public bool IsEmpty => X0 >= X1 || Y0 >= Y1;

        /// <summary>True for the unique infinite rectangle.</summary>
        public bool IsInfinite =>
            (X0 == Y0 && X0 == Constants.FzMinInfRect && X1 == Y1 && X1 == Constants.FzMaxInfRect)
            || float.IsNegativeInfinity(X0) || float.IsNegativeInfinity(Y0)
            || float.IsPositiveInfinity(X1) || float.IsPositiveInfinity(Y1);

        /// <summary>True when <c>x0 &lt;= x1</c> and <c>y0 &lt;= y1</c>.</summary>
        public bool IsValid => X0 <= X1 && Y0 <= Y1;

        /// <summary>Area in square points; zero if empty or infinite.</summary>
        public float Abs()
        {
            if (IsEmpty || IsInfinite) return 0.0f;
            return Width * Height;
        }

        /// <summary>Epsilon-based equality against another rectangle.</summary>
        public bool EqualTo(Rect obj) => obj != null && this == obj;

        /// <summary>True when not all coordinates are zero.</summary>
        public bool IsTruthy => !(Math.Max(Math.Max(X0, Y0), Math.Max(X1, Y1)) == 0
            && Math.Min(Math.Min(X0, Y0), Math.Min(X1, Y1)) == 0);

        /// <summary>Quadrilateral <c>Quad(tl, tr, bl, br)</c>.</summary>
        public Quad Quad => new Quad(TL, TR, BL, BR);

        /// <summary>Smallest containing integer rect (<see cref="Round"/>).</summary>
        public IRect IRect => Round();

        /// <summary>
        /// Rectangle area; optional unit converts square pixels to in/cm/mm.
        /// </summary>
        /// <param name="unit">One of <c>px</c> (default), <c>in</c>, <c>cm</c>, or <c>mm</c>.</param>
        public float GetArea(string unit = "px") => RectArea(Width, Height, unit);

        /// <summary>Enlarges this rect to include a point (in place).</summary>
        public Rect IncludePoint(Point p) => IncludePoint((object)p);

        /// <summary>Enlarges this rect to include a point-like value (in place).</summary>
        public Rect IncludePoint(object p)
        {
            if (IsInfinite) return this;
            var pt = RequirePoint(p);
            using var fr = ToFzRect();
            using var fp = pt.ToFzPoint();
            using var merged = mupdf.mupdf.fz_include_point_in_rect(fr, fp);
            Assign(merged);
            return this;
        }

        /// <summary>Enlarges this rect to include another rectangle (in place).</summary>
        public Rect IncludeRect(Rect r) => IncludeRect((object)r);

        /// <summary>Enlarges this rect to include a rect-like value (in place).</summary>
        public Rect IncludeRect(object r)
        {
            var rect = CoerceRect(r);
            if (rect.IsInfinite || IsInfinite)
            {
                X0 = Y0 = Constants.FzMinInfRect;
                X1 = Y1 = Constants.FzMaxInfRect;
                return this;
            }
            if (rect.IsEmpty) return this;
            if (IsEmpty)
            {
                Assign(rect);
                return this;
            }
            using var a = ToFzRect();
            using var b = rect.ToFzRect();
            using var u = mupdf.mupdf.fz_union_rect(a, b);
            Assign(u);
            return this;
        }

        /// <summary>Replaces this rect with its intersection with <paramref name="r"/> (in place).</summary>
        public Rect Intersect(Rect r) => Intersect((object)r);

        /// <summary>Replaces this rect with its intersection with a rect-like value (in place).</summary>
        public Rect Intersect(object r)
        {
            var rect = CoerceRect(r);
            if (rect.IsInfinite) return this;
            if (IsInfinite)
            {
                Assign(rect);
                return this;
            }
            if (rect.IsEmpty)
            {
                Assign(rect);
                return this;
            }
            if (IsEmpty) return this;
            using var a = ToFzRect();
            using var b = rect.ToFzRect();
            using var ir = mupdf.mupdf.fz_intersect_rect(a, b);
            Assign(ir);
            return this;
        }

        /// <summary>True when intersection with <paramref name="x"/> is non-empty.</summary>
        public bool Intersects(object x) => Intersects(CoerceRect(x));

        /// <summary>True when this rect and <paramref name="o"/> overlap with positive area.</summary>
        public bool Intersects(Rect o) =>
            !IsEmpty && !IsInfinite && !o.IsEmpty && !o.IsInfinite
            && X0 < o.X1 && o.X0 < X1 && Y0 < o.Y1 && o.Y0 < Y1;

        /// <summary>
        /// Whether <paramref name="x"/> is contained (point, rect, or coordinate value).
        /// </summary>
        public bool Contains(object x)
        {
            if (x is float or double or int or long)
            {
                var v = Convert.ToDouble(x, CultureInfo.InvariantCulture);
                return v == X0 || v == Y0 || v == X1 || v == Y1;
            }
            if (TryCoercePoint(x, out var p))
                return mupdf.mupdf.fz_is_point_inside_rect(p.ToFzPoint(), ToFzRect()) != 0;
            if (TryCoerceRectLike(x, out var r))
                return X0 <= r.X0 && r.X1 <= X1 && Y0 <= r.Y0 && r.Y1 <= Y1;
            return false;
        }

        /// <summary>Makes the rectangle valid by swapping corners so <c>x0 &lt;= x1</c> and <c>y0 &lt;= y1</c>.</summary>
        public Rect Normalize()
        {
            if (X1 < X0) { float t = X0; X0 = X1; X1 = t; }
            if (Y1 < Y0) { float t = Y0; Y0 = Y1; Y1 = t; }
            return this;
        }

        /// <summary>
        /// Smallest <see cref="IRect"/> containing this rect (tl rounded up/out, br rounded down/out).
        /// </summary>
        public IRect Round()
        {
            if (IsInfinite) return IRect.Infinite;
            if (IsEmpty) return new IRect();
            using var fr = ToFzRect();
            return new IRect(fr.fz_round_rect());
        }

        /// <summary>Returns a new quad after morphing this rect about <paramref name="p"/> with <paramref name="m"/>.</summary>
        public Quad Morph(Point p, Matrix m) =>
            IsInfinite ? Quad.Infinite : Quad.Morph(p, m);

        /// <summary>Euclidean norm of the four coordinates as a vector.</summary>
        public float Norm() => (float)Math.Sqrt(X0 * X0 + Y0 * Y0 + X1 * X1 + Y1 * Y1);

        /// <summary>
        /// Matrix <c>mat</c> such that transforming this rect yields <paramref name="r"/> (<c>self * mat = r</c>).
        /// </summary>
        /// <param name="r">Target rectangle; must be finite and non-empty.</param>
        public Matrix ToRect(Rect r)
        {
            if (IsInfinite || IsEmpty || r.IsInfinite || r.IsEmpty)
                throw new ValueErrorException("rectangles must be finite and not empty");
            return new Matrix(1, 0, 0, 1, -X0, -Y0)
                * new Matrix(r.Width / Width, r.Height / Height)
                * new Matrix(1, 0, 0, 1, r.X0, r.Y0);
        }

        /// <summary>
        /// Replaces this rect with the smallest axis-aligned rect containing the transformed corners.
        /// </summary>
        /// <param name="m">Transformation matrix.</param>
        /// <returns>This rect after transformation (no-op when empty or infinite).</returns>
        public Rect Transform(Matrix m)
        {
            using var tr = mupdf.mupdf.fz_transform_rect(ToFzRect(), m.ToFzMatrix());
            Assign(tr);
            return this;
        }

        /// <summary>Native MuPDF <c>fz_rect</c> for interop.</summary>
        public mupdf.FzRect ToFzRect() =>
            mupdf.mupdf.fz_make_rect((float)X0, (float)Y0, (float)X1, (float)Y1);

        private void Assign(mupdf.FzRect r)
        {
            X0 = r.x0; Y0 = r.y0; X1 = r.x1; Y1 = r.y1;
        }

        private void Assign(Rect r)
        {
            X0 = r.X0; Y0 = r.Y0; X1 = r.X1; Y1 = r.Y1;
        }

        // ─── Operators (PyMuPDF) ───

        public static Rect operator +(Rect a, Rect b) =>
            new Rect(a.X0 + b.X0, a.Y0 + b.Y0, a.X1 + b.X1, a.Y1 + b.Y1);

        public static Rect operator +(Rect a, float s) =>
            new Rect(a.X0 + s, a.Y0 + s, a.X1 + s, a.Y1 + s);

        public static Rect operator +(Rect a, int s) => a + (float)s;

        public static Rect operator -(Rect a, Rect b) =>
            new Rect(a.X0 - b.X0, a.Y0 - b.Y0, a.X1 - b.X1, a.Y1 - b.Y1);

        public static Rect operator -(Rect a, float s) =>
            new Rect(a.X0 - s, a.Y0 - s, a.X1 - s, a.Y1 - s);

        public static Rect operator -(Rect a) => new Rect(-a.X0, -a.Y0, -a.X1, -a.Y1);

        public static Rect operator *(Rect a, float m) =>
            new Rect(a.X0 * m, a.Y0 * m, a.X1 * m, a.Y1 * m);

        public static Rect operator *(float m, Rect a) => a * m;

        public static Rect operator *(Rect r, Matrix m) => new Rect(r).Transform(m);

        public static Rect operator /(Rect a, float m) => a * (1.0f / m);

        public static Rect operator /(Rect r, Matrix m)
        {
            var inv = m.Inverted();
            if (inv == null)
                throw new DivideByZeroException($"Matrix not invertible: {m}");
            return new Rect(r).Transform(inv);
        }

        public static Rect operator &(Rect a, object x)
        {
            if (x == null) throw new ValueErrorException("bad operand 2");
            return new Rect(a).Intersect(x);
        }

        public static Rect operator &(Rect a, Rect b) => a & (object)b;

        public static Rect operator |(Rect a, object x)
        {
            if (x == null) throw new ValueErrorException("bad operand 2");
            var r = new Rect(a);
            if (TryCoercePoint(x, out var p))
                return r.IncludePoint(p);
            if (TryCoerceRectLike(x, out var rect))
                return r.IncludeRect(rect);
            throw new ValueErrorException("bad operand 2");
        }

        public static Rect operator |(Rect a, Rect b) => a | (object)b;

        public static Rect operator |(Rect a, Point p) => a | (object)p;

        public static Rect operator +(Rect a) => new Rect(a);

        public static bool operator ==(Rect? a, Rect? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return Math.Abs(a.X0 - b.X0) < Constants.Epsilon
                && Math.Abs(a.Y0 - b.Y0) < Constants.Epsilon
                && Math.Abs(a.X1 - b.X1) < Constants.Epsilon
                && Math.Abs(a.Y1 - b.Y1) < Constants.Epsilon;
        }

        public static bool operator !=(Rect? a, Rect? b) => !(a == b);

        public bool Equals(Rect? other) => this == other;

        public override bool Equals(object? obj)
        {
            if (obj is Rect r) return this == r;
            if (obj is IEnumerable seq && obj is not string)
            {
                var vals = new List<float>();
                foreach (var v in seq)
                {
                    if (v is float d) vals.Add(d);
                    else if (v is float f) vals.Add(f);
                    else if (v is int i) vals.Add(i);
                    else return false;
                }
                if (vals.Count != 4) return false;
                var diff = this - new Rect(vals[0], vals[1], vals[2], vals[3]);
                return Math.Abs(diff.X0) < Constants.Epsilon && Math.Abs(diff.Y0) < Constants.Epsilon
                    && Math.Abs(diff.X1) < Constants.Epsilon && Math.Abs(diff.Y1) < Constants.Epsilon;
            }
            return false;
        }

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

        public IEnumerator<float> GetEnumerator()
        {
            yield return X0; yield return Y0; yield return X1; yield return Y1;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // ─── util_make_rect (shared with <see cref="IRect"/>) ───

        internal static (float x0, float y0, float x1, float y1) MakeRectCoords(
            object[] args,
            Point? p0,
            Point? p1,
            float? x0,
            float? y0,
            float? x1,
            float? y1)
        {
            float rx0, ry0, rx1, ry1;
            if (args == null || args.Length == 0)
                (rx0, ry0, rx1, ry1) = (0, 0, 0, 0);
            else if (args.Length == 1)
                (rx0, ry0, rx1, ry1) = ResolveSingleArg(args[0]);
            else if (args.Length == 2)
            {
                var pta = RequirePoint(args[0]);
                var ptb = RequirePoint(args[1]);
                (rx0, ry0, rx1, ry1) = (pta.X, pta.Y, ptb.X, ptb.Y);
            }
            else if (args.Length == 3)
            {
                var (ax, ay) = GetXYNullable(args[0]);
                if (ax is not null && ay is not null)
                    (rx0, ry0, rx1, ry1) = (ax.Value, ay.Value,
                        (float)Convert.ToDouble(args[1], CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble(args[2], CultureInfo.InvariantCulture));
                else
                {
                    var (bx, by) = GetXYNullable(args[2]);
                    if (bx is null || by is null)
                        throw new ArgumentException($"Unrecognised args: ({args[0]}, {args[1]}, {args[2]})");
                    (rx0, ry0, rx1, ry1) = (
                        (float)Convert.ToDouble(args[0], CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble(args[1], CultureInfo.InvariantCulture),
                        bx.Value, by.Value);
                }
            }
            else if (args.Length == 4)
                (rx0, ry0, rx1, ry1) = (
                    (float)Convert.ToDouble(args[0], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[1], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[2], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[3], CultureInfo.InvariantCulture));
            else
                throw new ArgumentException($"Unrecognised args: {string.Join(", ", args)}");

            if (p0 != null) { var (px, py) = GetXYNullable(p0); rx0 = px ?? rx0; ry0 = py ?? ry0; }
            if (p1 != null) { var (px, py) = GetXYNullable(p1); rx1 = px ?? rx1; ry1 = py ?? ry1; }
            if (x0 != null) rx0 = x0.Value;
            if (y0 != null) ry0 = y0.Value;
            if (x1 != null) rx1 = x1.Value;
            if (y1 != null) ry1 = y1.Value;
            return (rx0, ry0, rx1, ry1);
        }

        internal static Rect CoerceRect(object r)
        {
            if (r is Rect rect) return rect;
            if (r is IRect ir) return new Rect(ir);
            return Helpers.RectFromPy(r);
        }

        private static bool TryCoerceRectLike(object x, out Rect r)
        {
            r = default;
            try
            {
                r = CoerceRect(x);
                return true;
            }
            catch
            {
                if (x is Quad q)
                {
                    r = q.Rect;
                    return true;
                }
                return false;
            }
        }

        private static bool TryCoercePoint(object x, out Point p)
        {
            p = default;
            try
            {
                p = RequirePoint(x);
                return true;
            }
            catch { return false; }
        }

        private static (float x0, float y0, float x1, float y1) ResolveSingleArg(object arg)
        {
            if (arg is Rect r) return (r.X0, r.Y0, r.X1, r.Y1);
            if (arg is IRect ir) return (ir.X0, ir.Y0, ir.X1, ir.Y1);
            if (arg is mupdf.FzRect fr) return (fr.x0, fr.y0, fr.x1, fr.y1);
            if (arg is mupdf.FzIrect fir) return (fir.x0, fir.y0, fir.x1, fir.y1);
            if (arg is Point p) return (p.X, p.Y, p.X, p.Y);
            if (arg is IEnumerable seq && arg is not string)
            {
                var list = new List<object>();
                foreach (var item in seq) list.Add(item);
                if (list.Count == 2)
                {
                    var pta = RequirePoint(list[0]);
                    var ptb = RequirePoint(list[1]);
                    return (pta.X, pta.Y, ptb.X, ptb.Y);
                }
                if (list.Count == 3)
                {
                    var flat = new List<float>();
                    foreach (var part in list)
                        flat.AddRange(ExpandTuple(part));
                    if (flat.Count != 4)
                        throw new ArgumentException("invalid rect-like sequence");
                    return (flat[0], flat[1], flat[2], flat[3]);
                }
                if (list.Count == 4)
                    return (
                        (float)Convert.ToDouble(list[0], CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble(list[1], CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble(list[2], CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble(list[3], CultureInfo.InvariantCulture));
                throw new ArgumentException($"invalid rect-like sequence: len={list.Count}");
            }
            throw new ArgumentException($"Unrecognised arg: {arg}");
        }

        private static float[] ExpandTuple(object a)
        {
            if (a is Point p) return new[] { p.X, p.Y };
            if (a is IRect ir) return new[] { (float)ir.X0, ir.Y0, ir.X1, ir.Y1 };
            if (a is Rect r) return new[] { r.X0, r.Y0, r.X1, r.Y1 };
            if (a is IEnumerable seq && a is not string)
            {
                var list = new List<float>();
                foreach (var v in seq)
                    list.Add((float)Convert.ToDouble(v, CultureInfo.InvariantCulture));
                return list.ToArray();
            }
            throw new ArgumentException("invalid sequence for rect");
        }

        internal static Point RequirePoint(object arg)
        {
            var (x, y) = GetXYNullable(arg);
            if (x is null || y is null)
                throw new ValueErrorException("Point: bad seq len");
            return new Point(x.Value, y.Value);
        }

        private static (float? x, float? y) GetXYNullable(object arg)
        {
            if (arg is Point p) return (p.X, p.Y);
            if (arg is IEnumerable seq && arg is not string)
            {
                var list = new List<float>();
                foreach (var v in seq)
                    list.Add((float)Convert.ToDouble(v, CultureInfo.InvariantCulture));
                if (list.Count == 2) return (list[0], list[1]);
                if (list.Count != 2 && list.Count > 0)
                    throw new ValueErrorException("Point: bad seq len");
            }
            if (arg is float d) return (d, null);
            if (arg is float f) return (f, null);
            if (arg is int i) return (i, null);
            return (null, null);
        }

        private static float RectArea(float width, float height, string unit)
        {
            if (width <= 0 || height <= 0) return 0;
            float a = width * height;
            return unit switch
            {
                "in" => a / 5184,
                "cm" => a * 6.4516f / 5184f,
                "mm" => a * 645.16f / 5184f,
                _ => a,
            };
        }
    }
}
