using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace MuPDF.NET
{
    /// <summary>
    /// A point in the plane defined by <see cref="X"/> and <see cref="Y"/> .
    /// </summary>
    /// <remarks>
    /// <para>Supports sequence-style indexing (<c>[0]</c> = x, <c>[1]</c> = y) and arithmetic with
    /// <see cref="Point"/>, <see cref="Matrix"/>, and scalars, matching MuPDF behavior.</para>
    /// <para>Constructors: parameterless <c>(0, 0)</c>, coordinates, copy of another point,
    /// <see cref="mupdf.FzPoint"/>, or a two-element sequence.</para>
    /// </remarks>
    public class Point : IEnumerable<float>, IEquatable<Point>
    {
        /// <summary>The X coordinate.</summary>
        public float X { get; set; }

        /// <summary>The Y coordinate.</summary>
        public float Y { get; set; }

        /// <summary>Sequence length (always 2).</summary>
        public int Length => Count;

        /// <summary>Creates <c>(0, 0)</c>.</summary>
        public Point() => X = Y = 0.0f;

        /// <summary>Creates a point at (<paramref name="x"/>, <paramref name="y"/>).</summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Creates a copy of another point.</summary>
        /// <param name="o">Source point.</param>
        public Point(Point o) : this(o.X, o.Y) { }

        /// <summary>Creates a point from a MuPDF <c>fz_point</c>.</summary>
        /// <param name="fp">Native point.</param>
        public Point(mupdf.FzPoint fp) : this(fp.x, fp.y) { }

        /// <summary>Creates a point from a MuPDF <c>fz_point</c> struct.</summary>
        /// <param name="fp">Native point.</param>
        public Point(mupdf.fz_point fp) : this(fp.x, fp.y) { }

        /// <summary>Creates a point from a two-element sequence <c>[x, y]</c>.</summary>
        /// <param name="seq">Exactly two numeric values.</param>
        public Point(IReadOnlyList<float> seq)
        {
            if (seq.Count != 2)
                throw new ValueErrorException("Point: bad seq len");
            X = seq[0];
            Y = seq[1];
        }

        /// <summary>
        /// Builds a point from positional arguments with optional keyword overrides.
        /// </summary>
        /// <param name="args">Zero, one, or two positional arguments (point, native point, or x/y pair).</param>
        /// <param name="x">If set, overrides the X coordinate after parsing <paramref name="args"/>.</param>
        /// <param name="y">If set, overrides the Y coordinate after parsing <paramref name="args"/>.</param>
        public static Point Create(object[] args, float? x = null, float? y = null)
        {
            var p = FromArgs(args);
            if (x != null) p.X = x.Value;
            if (y != null) p.Y = y.Value;
            return p;
        }

        /// <summary>Builds a point from positional arguments.</summary>
        public static Point Create(params object[] args) => FromArgs(args);

        /// <summary>Number of components (always 2).</summary>
        public int Count => 2;

        /// <summary>Gets or sets component <c>0</c> (<see cref="X"/>) or <c>1</c> (<see cref="Y"/>).</summary>
        /// <param name="i">Index 0 or 1.</param>
        public float this[int i]
        {
            get => i switch { 0 => X, 1 => Y, _ => throw new IndexOutOfRangeException("index out of range") };
            set
            {
                var v = (float)Convert.ToDouble(value, CultureInfo.InvariantCulture);
                switch (i)
                {
                    case 0: X = v; break;
                    case 1: Y = v; break;
                    default: throw new IndexOutOfRangeException("index out of range");
                }
            }
        }

        /// <summary>
        /// Euclidean length of the vector from the origin to this point ( / <c>abs</c>).
        /// </summary>
        public float Norm => (float)Math.Sqrt(X * X + Y * Y);

        /// <summary>True if both coordinates are within <see cref="Constants.Epsilon"/> of zero.</summary>
        public bool IsZero() => Math.Abs(X) < Constants.Epsilon && Math.Abs(Y) < Constants.Epsilon;

        /// <summary>Returns a copy of this point.</summary>
        public Point Position() => new Point(X, Y);

        /// <summary>Epsilon-based equality test against another point.</summary>
        /// <param name="obj">Point to compare.</param>
        public bool EqualTo(Point obj) => obj != null && this == obj;

        /// <summary>
        /// Unit vector in the same direction: each coordinate divided by <see cref="Norm"/>.
        /// X and Y are cosine and sine of the angle with the positive X axis.
        /// </summary>
        public Point Unit
        {
            get
            {
                float s = X * X + Y * Y;
                if (s < Constants.Epsilon)
                    return new Point(0, 0);
                s = (float)Math.Sqrt(s);
                return new Point(X / s, Y / s);
            }
        }

        /// <summary>
        /// Euclidean length of this vector (published MuPDF.NET <c>Abs()</c>; / <c>norm</c>).
        /// </summary>
        public float Abs() => Norm;

        /// <summary>
        /// Like <see cref="Unit"/>, but each coordinate is replaced by its absolute value .
        /// </summary>
        public Point AbsUnit
        {
            get
            {
                float s = X * X + Y * Y;
                if (s < Constants.Epsilon)
                    return new Point(0, 0);
                s = (float)Math.Sqrt(s);
                return new Point(Math.Abs(X) / s, Math.Abs(Y) / s);
            }
        }

        /// <summary>
        /// Distance to a point or rectangle in pixels, inches, centimeters, or millimeters.
        /// </summary>
        /// <param name="arg">A <see cref="Point"/>, <see cref="Rect"/>, or four- or two-element sequence.</param>
        /// <param name="unit">One of <c>px</c> (default), <c>in</c>, <c>cm</c>, or <c>mm</c>.</param>
        /// <returns>
        /// For a point, the straight-line distance. For a rectangle, the shortest distance to a side;
        /// zero if the rectangle contains this point (finite rect semantics).
        /// </returns>
        public float DistanceTo(object arg, string unit = "px")
        {
            if (arg == null)
                throw new ValueErrorException("at least one parameter must be given");

            var x = CoerceDistanceTarget(arg);
            float f = UnitFactor(unit);

            if (x is Point pt)
                return new Point(X - pt.X, Y - pt.Y).Norm * f;

            var rect = (Rect)x;
            var r = new Rect(rect.TopLeft, rect.TopLeft);
            r = r | rect.BottomRight;
            if (r.Contains(this))
                return 0.0f;
            if (X > r.X1)
            {
                if (Y >= r.Y1)
                    return DistanceTo(rect.BottomRight, unit);
                if (Y <= r.Y0)
                    return DistanceTo(rect.TopRight, unit);
                return (X - r.X1) * f;
            }
            if (r.X0 <= X && X <= r.X1)
            {
                if (Y >= r.Y1)
                    return (Y - r.Y1) * f;
                return (r.Y0 - Y) * f;
            }
            if (Y >= r.Y1)
                return DistanceTo(rect.BottomLeft, unit);
            if (Y <= r.Y0)
                return DistanceTo(rect.TopLeft, unit);
            return (r.X0 - X) * f;
        }

        /// <summary>Distance to another point.</summary>
        /// <param name="other">Target point.</param>
        /// <param name="unit">Length unit (<c>px</c>, <c>in</c>, <c>cm</c>, <c>mm</c>).</param>
        public float DistanceTo(Point other, string unit = "px") => DistanceTo((object)other, unit);

        /// <summary>Distance to a rectangle.</summary>
        /// <param name="rect">Target rectangle.</param>
        /// <param name="unit">Length unit (<c>px</c>, <c>in</c>, <c>cm</c>, <c>mm</c>).</param>
        public float DistanceTo(Rect rect, string unit = "px") => DistanceTo((object)rect, unit);

        /// <summary>
        /// Applies <paramref name="m"/> to this point in place via <c>fz_transform_point</c> and returns <c>this</c>.
        /// </summary>
        /// <param name="m">Transformation matrix (six components).</param>
        /// <returns>This point after transformation.</returns>
        public Point Transform(Matrix m)
        {
            if (m.Count != 6)
                throw new ValueErrorException("Matrix: bad seq len");
            using var fp = ToFzPoint();
            using var fm = m.ToFzMatrix();
            using var tr = mupdf.mupdf.fz_transform_point(fp, fm);
            X = tr.x;
            Y = tr.y;
            return this;
        }

        /// <summary>Creates a MuPDF <c>FzPoint</c> for native calls.</summary>
        public mupdf.FzPoint ToFzPoint() =>
            mupdf.mupdf.fz_make_point((float)X, (float)Y);

        /// <summary>Creates a MuPDF <c>FzPoint</c> from a managed point.</summary>
        public static mupdf.FzPoint toFzPoint(Point p) => p?.ToFzPoint();

        // ─── Operators ───

        /// <summary>Component-wise addition of two points.</summary>
        public static Point operator +(Point a, Point b) =>
            new Point(a.X + b.X, a.Y + b.Y);

        /// <summary>Adds a scalar to both coordinates.</summary>
        public static Point operator +(Point a, float s) =>
            new Point(a.X + s, a.Y + s);

        /// <summary>Adds an integer scalar to both coordinates.</summary>
        public static Point operator +(Point a, int s) => a + (float)s;

        /// <summary>Component-wise subtraction.</summary>
        public static Point operator -(Point a, Point b) =>
            new Point(a.X - b.X, a.Y - b.Y);

        /// <summary>Subtracts a scalar from both coordinates.</summary>
        public static Point operator -(Point a, float s) =>
            new Point(a.X - s, a.Y - s);

        /// <summary>Subtracts an integer scalar from both coordinates.</summary>
        public static Point operator -(Point a, int s) => a - (float)s;

        /// <summary>Negates both coordinates.</summary>
        public static Point operator -(Point a) => new Point(-a.X, -a.Y);

        /// <summary>Unary plus (copy).</summary>
        public static Point operator +(Point a) => new Point(a);

        /// <summary>Scales both coordinates by a factor.</summary>
        public static Point operator *(Point a, float m) =>
            new Point(a.X * m, a.Y * m);

        /// <summary>Scales both coordinates by a factor.</summary>
        public static Point operator *(float m, Point a) => a * m;

        /// <summary>Scales both coordinates by an integer factor.</summary>
        public static Point operator *(Point a, int m) => a * (float)m;

        /// <summary>
        /// Dot product with a two-element sequence, scalar multiply, or transform by <see cref="Matrix"/>.
        /// </summary>
        public static object operator *(Point a, object b)
        {
            if (b is float f) return a * f;
            if (b is int i) return a * (float)i;
            if (b is Matrix m) return new Point(a).Transform(m);
            if (b is Point p) return a.X * p.X + a.Y * p.Y;
            if (b is IEnumerable seq && b is not string)
            {
                var list = new List<float>();
                foreach (var v in seq)
                    list.Add((float)Convert.ToDouble(v, CultureInfo.InvariantCulture));
                if (list.Count == 2)
                    return a.X * list[0] + a.Y * list[1];
            }
            throw new ValueErrorException("bad operand 2");
        }

        /// <summary>Transforms a point by a matrix (returns a new point).</summary>
        public static Point operator *(Point p, Matrix m) =>
            new Point(p).Transform(m);

        /// <summary>Dot product of two points.</summary>
        public static float operator *(Point a, Point b) => a.X * b.X + a.Y * b.Y;

        /// <summary>Divides both coordinates by a scalar.</summary>
        public static Point operator /(Point a, float m) =>
            new Point(a.X / m, a.Y / m);

        /// <summary>Divides both coordinates by a scalar.</summary>
        public Point TrueDivide(float m) => this / m;

        /// <summary>Divides by the inverse of a matrix.</summary>
        public static Point operator /(Point p, Matrix m)
        {
            var inv = m.Inverted();
            if (inv == null)
                throw new DivideByZeroException("matrix not invertible");
            return new Point(p).Transform(inv);
        }

        /// <summary>Divides by the inverse of a matrix.</summary>
        public Point TrueDivide(Matrix m) => this / m;

        /// <summary>Epsilon-based equality.</summary>
        public static bool operator ==(Point? a, Point? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return Math.Abs(a.X - b.X) < Constants.Epsilon
                && Math.Abs(a.Y - b.Y) < Constants.Epsilon;
        }

        /// <summary>Epsilon-based inequality.</summary>
        public static bool operator !=(Point? a, Point? b) => !(a == b);

        /// <inheritdoc />
        public bool Equals(Point? other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is Point p) return this == p;
            if (obj is IEnumerable seq && obj is not string)
            {
                var vals = new List<float>();
                foreach (var v in seq)
                {
                    if (v is float d) vals.Add(d);
                    else if (v is int i) vals.Add(i);
                    else return false;
                }
                if (vals.Count != 2) return false;
                var diff = this - new Point(vals[0], vals[1]);
                return Math.Abs(diff.X) < Constants.Epsilon && Math.Abs(diff.Y) < Constants.Epsilon;
            }
            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X.GetHashCode();
                hash = hash * 31 + Y.GetHashCode();
                return hash;
            }
        }

        /// <summary>Returns <c>Point(x, y)</c> for display.</summary>
        public override string ToString() => $"Point({X}, {Y})";

        /// <inheritdoc />
        public IEnumerator<float> GetEnumerator()
        {
            yield return X;
            yield return Y;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal Point unit => Unit;
        internal Point abs_unit => AbsUnit;
        internal float distance_to(object arg, string unit = "px") => DistanceTo(arg, unit);
        internal Point transform(Matrix m) => Transform(m);
        internal float norm() => Norm;

        private static Point FromArgs(object[] args)
        {
            if (args == null || args.Length == 0)
                return new Point();
            if (args.Length > 2)
                throw new ValueErrorException("Point: bad seq len");
            if (args.Length == 2)
            {
                return new Point(
                    (float)Convert.ToDouble(args[0], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[1], CultureInfo.InvariantCulture));
            }
            var l = args[0];
            if (l is Point p)
                return new Point(p);
            if (l is mupdf.FzPoint fp)
                return new Point(fp);
            if (l is mupdf.fz_point fzp)
                return new Point(fzp);
            if (l is IEnumerable seq && l is not string)
            {
                var list = new List<float>();
                foreach (var v in seq)
                    list.Add((float)Convert.ToDouble(v, CultureInfo.InvariantCulture));
                if (list.Count != 2)
                    throw new ValueErrorException("Point: bad seq len");
                return new Point(list[0], list[1]);
            }
            throw new ValueErrorException("Point: bad args");
        }

        private static object CoerceDistanceTarget(object arg)
        {
            if (arg is Point || arg is Rect)
                return arg;
            if (arg is IEnumerable seq && arg is not string)
            {
                var list = new List<float>();
                foreach (var v in seq)
                    list.Add((float)Convert.ToDouble(v, CultureInfo.InvariantCulture));
                if (list.Count == 2)
                    return new Point(list[0], list[1]);
                if (list.Count == 4)
                    return new Rect(list[0], list[1], list[2], list[3]);
            }
            throw new ValueErrorException("arg1 must be point-like or rect-like");
        }

        private static float UnitFactor(string unit)
        {
            var (num, den) = unit switch
            {
                "in" => (1.0f, 72.0f),
                "cm" => (2.54f, 72.0f),
                "mm" => (25.4f, 72.0f),
                _ => (1.0f, 1.0f),
            };
            return (float)(num / den);
        }
    }
}