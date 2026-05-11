using System;
using System.Collections;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a point in the plane, defined by coordinates x and y.
    /// </summary>
    public class Point : IEnumerable<double>, IEquatable<Point>
    {
        /// <summary>
        /// X coordinate.
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// Y coordinate.
        /// </summary>
        public double Y { get; set; }

        public Point() { X = 0.0; Y = 0.0; }
        public Point(double x, double y) { X = x; Y = y; }
        public Point(Point other) { X = other.X; Y = other.Y; }

        public Point(mupdf.FzPoint fp) { X = fp.x; Y = fp.y; }
        public Point(mupdf.fz_point fp) { X = fp.x; Y = fp.y; }

        public Point(IReadOnlyList<double> seq)
        {
            if (seq.Count != 2) throw new ArgumentException("Point: bad seq len");
            X = seq[0]; Y = seq[1];
        }

        public double this[int i]
        {
            get => i switch { 0 => X, 1 => Y, _ => throw new IndexOutOfRangeException() };
            set { switch (i) { case 0: X = value; break; case 1: Y = value; break; default: throw new IndexOutOfRangeException(); } }
        }

        public int Count => 2;
        /// <summary>
        /// Euclidean norm (distance to origin).
        /// </summary>
        public double Norm => Math.Sqrt(X * X + Y * Y);

        /// <summary>
        /// Unit vector of the point.
        /// </summary>
        public Point Unit
        {
            get
            {
                double s = X * X + Y * Y;
                if (s < Constants.Epsilon) return new Point(0, 0);
                s = Math.Sqrt(s);
                return new Point(X / s, Y / s);
            }
        }

        /// <summary>
        /// Unit vector with positive coordinates.
        /// </summary>
        public Point AbsUnit
        {
            get
            {
                double s = X * X + Y * Y;
                if (s < Constants.Epsilon) return new Point(0, 0);
                s = Math.Sqrt(s);
                return new Point(Math.Abs(X) / s, Math.Abs(Y) / s);
            }
        }

        /// <summary>
        /// Distance to another point.
        /// </summary>
        public double DistanceTo(Point other, string unit = "px")
        {
            var (num, den) = UnitFactor(unit);
            return new Point(X - other.X, Y - other.Y).Norm * num / den;
        }

        /// <summary>
        /// Distance to a rectangle.
        /// </summary>
        public double DistanceTo(Rect rect, string unit = "px")
        {
            var (num, den) = UnitFactor(unit);
            double f = num / den;
            var r = new Rect(rect);
            r.Normalize();
            if (r.Contains(this)) return 0.0;
            if (X > r.X1)
            {
                if (Y >= r.Y1) return DistanceTo(r.BottomRight, unit);
                if (Y <= r.Y0) return DistanceTo(r.TopRight, unit);
                return (X - r.X1) * f;
            }
            if (r.X0 <= X && X <= r.X1)
            {
                if (Y >= r.Y1) return (Y - r.Y1) * f;
                return (r.Y0 - Y) * f;
            }
            if (Y >= r.Y1) return DistanceTo(r.BottomLeft, unit);
            if (Y <= r.Y0) return DistanceTo(r.TopLeft, unit);
            return (r.X0 - X) * f;
        }

        /// <summary>
        /// Transform point by a matrix.
        /// </summary>
        public Point Transform(Matrix m)
        {
            double newX = X * m.A + Y * m.C + m.E;
            double newY = X * m.B + Y * m.D + m.F;
            X = newX; Y = newY;
            return this;
        }

        public mupdf.FzPoint ToFzPoint() => mupdf.mupdf.fz_make_point((float)X, (float)Y);

        private static (double num, double den) UnitFactor(string unit) => unit switch
        {
            "in" => (1.0, 72.0),
            "cm" => (2.54, 72.0),
            "mm" => (25.4, 72.0),
            _ => (1.0, 1.0)
        };

        public static Point operator +(Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
        public static Point operator +(Point a, double s) => new Point(a.X + s, a.Y + s);
        public static Point operator -(Point a, Point b) => new Point(a.X - b.X, a.Y - b.Y);
        public static Point operator -(Point a, double s) => new Point(a.X - s, a.Y - s);
        public static Point operator -(Point a) => new Point(-a.X, -a.Y);
        public static Point operator *(Point a, double m) => new Point(a.X * m, a.Y * m);
        public static Point operator *(double m, Point a) => new Point(a.X * m, a.Y * m);

        /// <summary>Python <c>Point * Matrix</c>: affine transform (returns a new point).</summary>
        public static Point operator *(Point p, Matrix m) =>
            new Point(p.X * m.A + p.Y * m.C + m.E, p.X * m.B + p.Y * m.D + m.F);

        public static double operator *(Point a, Point b) => a.X * b.X + a.Y * b.Y;

        public static Point operator /(Point a, double m) => new Point(a.X / m, a.Y / m);

        /// <summary>Python <c>Point / Matrix</c>: transform by inverse matrix.</summary>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="m"/> is singular.</exception>
        public static Point operator /(Point p, Matrix m)
        {
            var inv = m.Inverted();
            if (inv == null)
                throw new DivideByZeroException("matrix is not invertible");
            return p * inv;
        }

        public static bool operator ==(Point? a, Point? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return Math.Abs(a.X - b.X) < Constants.Epsilon && Math.Abs(a.Y - b.Y) < Constants.Epsilon;
        }
        public static bool operator !=(Point? a, Point? b) => !(a == b);

        public bool Equals(Point? other) => this == other;
        public override bool Equals(object? obj) => Equals(obj as Point);
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
        public override string ToString() => $"Point({X}, {Y})";

        public IEnumerator<double> GetEnumerator() { yield return X; yield return Y; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
