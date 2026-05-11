using System;
using System.Collections;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a 3x3 transformation matrix [a, b, c, d, e, f].
    /// </summary>
    public class Matrix : IEnumerable<double>, IEquatable<Matrix>
    {
        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public double D { get; set; }
        public double E { get; set; }
        public double F { get; set; }

        /// <summary>
        /// The identity matrix.
        /// </summary>
        public static readonly Matrix Identity = new Matrix(1, 0, 0, 1, 0, 0);

        public Matrix() { A = B = C = D = E = F = 0.0; }
        public Matrix(double a, double b, double c, double d, double e, double f) { A = a; B = b; C = c; D = d; E = e; F = f; }

        /// <summary>Python <c>Matrix(zoom_x, zoom_y)</c>: axis-aligned scaling (a = zoom_x, d = zoom_y).</summary>
        public Matrix(double sx, double sy) { A = sx; B = 0; C = 0; D = sy; E = 0; F = 0; }

        /// <summary>
        /// Python <c>Matrix(zoom_x, zoom_y, 0)</c> (zoom) or <c>Matrix(shear_x, shear_y, 1)</c> (shear).
        /// The third value must be <c>0</c> (zoom) or <c>1</c> (shear), matching Python within <c>Constants.Epsilon</c>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="zoomOrShearMarker"/> is not 0 or 1.</exception>
        public Matrix(double p0, double p1, double zoomOrShearMarker)
        {
            if (Math.Abs(zoomOrShearMarker) < Constants.Epsilon)
            {
                A = p0; B = 0; C = 0; D = p1; E = 0; F = 0;
            }
            else if (Math.Abs(zoomOrShearMarker - 1.0) < Constants.Epsilon)
            {
                A = 1.0; B = p1; C = p0; D = 1.0; E = 0; F = 0;
            }
            else
                throw new ArgumentException("Matrix: third argument must be 0 (zoom) or 1 (shear).");
        }

        /// <summary>Python <c>Matrix(degrees)</c> where the argument is a rotation angle in degrees.</summary>
        public Matrix(double angleDegrees)
        {
            var r = Rotation(angleDegrees);
            A = r.A; B = r.B; C = r.C; D = r.D; E = r.E; F = r.F;
        }

        public Matrix(Matrix o) { A = o.A; B = o.B; C = o.C; D = o.D; E = o.E; F = o.F; }
        public Matrix(mupdf.FzMatrix m) { A = m.a; B = m.b; C = m.c; D = m.d; E = m.e; F = m.f; }

        /// <summary>
        /// Create a rotation matrix.
        /// </summary>
        public static Matrix Rotation(double degrees)
        {
            double t = degrees * Math.PI / 180.0;
            double cos = Math.Round(Math.Cos(t), 8);
            double sin = Math.Round(Math.Sin(t), 8);
            return new Matrix(cos, sin, -sin, cos, 0, 0);
        }

        /// <summary>
        /// Create a shearing matrix.
        /// </summary>
        public static Matrix Shear(double sx, double sy) => new Matrix(sx, sy, 1);

        public int Count => 6;
        public double this[int i]
        {
            get => i switch { 0 => A, 1 => B, 2 => C, 3 => D, 4 => E, 5 => F, _ => throw new IndexOutOfRangeException() };
            set { switch (i) { case 0: A = value; break; case 1: B = value; break; case 2: C = value; break; case 3: D = value; break; case 4: E = value; break; case 5: F = value; break; default: throw new IndexOutOfRangeException(); } }
        }

        /// <summary>
        /// Euclidean norm of the matrix.
        /// </summary>
        public double Norm => Math.Sqrt(A * A + B * B + C * C + D * D + E * E + F * F);

        /// <summary>
        /// True if the matrix maps rectangles to rectangles.
        /// </summary>
        public bool IsRectilinear =>
            (Math.Abs(B) < Constants.Epsilon && Math.Abs(C) < Constants.Epsilon) ||
            (Math.Abs(A) < Constants.Epsilon && Math.Abs(D) < Constants.Epsilon);

        /// <summary>
        /// Concatenate (multiply) with another matrix.
        /// </summary>
        public Matrix Concat(Matrix one, Matrix two)
        {
            A = one.A * two.A + one.B * two.C;
            B = one.A * two.B + one.B * two.D;
            C = one.C * two.A + one.D * two.C;
            D = one.C * two.B + one.D * two.D;
            E = one.E * two.A + one.F * two.C + two.E;
            F = one.E * two.B + one.F * two.D + two.F;
            return this;
        }

        /// <summary>
        /// Invert the matrix in place.
        /// </summary>
        public int Invert(Matrix? src = null)
        {
            var s = src ?? this;
            double det = s.A * s.D - s.B * s.C;
            if (Math.Abs(det) < 1e-14) return 1;
            double id = 1.0 / det;
            double a = s.D * id, b = -s.B * id, c = -s.C * id, d = s.A * id;
            double e = -(s.E * a + s.F * c), f = -(s.E * b + s.F * d);
            A = a; B = b; C = c; D = d; E = e; F = f;
            return 0;
        }

        /// <summary>
        /// Return the inverted matrix.
        /// </summary>
        public Matrix? Inverted() { var m = new Matrix(); return m.Invert(this) != 0 ? null : m; }

        /// <summary>
        /// Pre-concatenate a rotation.
        /// </summary>
        public Matrix Prerotate(double theta)
        {
            while (theta < 0) theta += 360;
            while (theta >= 360) theta -= 360;
            if (Math.Abs(theta) < Constants.Epsilon) { }
            else if (Math.Abs(90.0 - theta) < Constants.Epsilon) { double a = A, b = B; A = C; B = D; C = -a; D = -b; }
            else if (Math.Abs(180.0 - theta) < Constants.Epsilon) { A = -A; B = -B; C = -C; D = -D; }
            else if (Math.Abs(270.0 - theta) < Constants.Epsilon) { double a = A, b = B; A = -C; B = -D; C = a; D = b; }
            else
            {
                double rad = theta * Math.PI / 180.0;
                double s = Math.Sin(rad), co = Math.Cos(rad);
                double a = A, b = B;
                A = co * a + s * C; B = co * b + s * D;
                C = -s * a + co * C; D = -s * b + co * D;
            }
            return this;
        }

        /// <summary>
        /// Pre-concatenate a scaling.
        /// </summary>
        public Matrix Prescale(double sx, double sy) { A *= sx; B *= sx; C *= sy; D *= sy; return this; }

        /// <summary>
        /// Pre-concatenate a shearing.
        /// </summary>
        public Matrix Preshear(double h, double v)
        {
            double a = A, b = B;
            A += v * C; B += v * D;
            C += h * a; D += h * b;
            return this;
        }

        /// <summary>
        /// Pre-concatenate a translation.
        /// </summary>
        public Matrix Pretranslate(double tx, double ty) { E += tx * A + ty * C; F += tx * B + ty * D; return this; }

        public mupdf.FzMatrix ToFzMatrix() => mupdf.mupdf.fz_make_matrix((float)A, (float)B, (float)C, (float)D, (float)E, (float)F);

        public static Matrix operator +(Matrix a, Matrix b) => new Matrix(a.A + b.A, a.B + b.B, a.C + b.C, a.D + b.D, a.E + b.E, a.F + b.F);
        public static Matrix operator +(Matrix a, double s) => new Matrix(a.A + s, a.B + s, a.C + s, a.D + s, a.E + s, a.F + s);
        public static Matrix operator -(Matrix a, Matrix b) => new Matrix(a.A - b.A, a.B - b.B, a.C - b.C, a.D - b.D, a.E - b.E, a.F - b.F);
        public static Matrix operator -(Matrix a, double s) => new Matrix(a.A - s, a.B - s, a.C - s, a.D - s, a.E - s, a.F - s);
        public static Matrix operator -(Matrix a) => new Matrix(-a.A, -a.B, -a.C, -a.D, -a.E, -a.F);
        public static Matrix operator *(Matrix a, double s) => new Matrix(a.A * s, a.B * s, a.C * s, a.D * s, a.E * s, a.F * s);
        public static Matrix operator *(double s, Matrix a) => a * s;
        public static Matrix operator *(Matrix a, Matrix b) => new Matrix(1, 1).Concat(a, b);

        /// <summary>Scalar division (each component divided by <paramref name="s"/>).</summary>
        public static Matrix operator /(Matrix a, double s) { double i = 1.0 / s; return a * i; }

        /// <summary>
        /// Multiply by the inverse of <paramref name="b"/> (Python <c>Matrix / Matrix</c>).
        /// </summary>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="b"/> is singular.</exception>
        public static Matrix operator /(Matrix a, Matrix b)
        {
            var inv = b.Inverted();
            if (inv == null)
                throw new DivideByZeroException("matrix is not invertible");
            return a * inv;
        }

        public static bool operator ==(Matrix? a, Matrix? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            for (int i = 0; i < 6; i++) if (Math.Abs(a[i] - b[i]) >= Constants.Epsilon) return false;
            return true;
        }
        public static bool operator !=(Matrix? a, Matrix? b) => !(a == b);

        public bool Equals(Matrix? other) => this == other;
        public override bool Equals(object? obj) => Equals(obj as Matrix);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + A.GetHashCode();
                hash = hash * 31 + B.GetHashCode();
                hash = hash * 31 + C.GetHashCode();
                hash = hash * 31 + D.GetHashCode();
                hash = hash * 31 + E.GetHashCode();
                hash = hash * 31 + F.GetHashCode();
                return hash;
            }
        }
        public override string ToString() => $"Matrix({A}, {B}, {C}, {D}, {E}, {F})";

        public IEnumerator<double> GetEnumerator() { yield return A; yield return B; yield return C; yield return D; yield return E; yield return F; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Python/legacy compatibility aliases (mirrors _alias(Matrix, ...)).
        public bool is_rectilinear() => IsRectilinear;
        public Matrix prerotate(double theta) => Prerotate(theta);
        public Matrix preRotate(double theta) => prerotate(theta);
        public Matrix prescale(double sx, double sy) => Prescale(sx, sy);
        public Matrix preScale(double sx, double sy) => prescale(sx, sy);
        public Matrix preshear(double h, double v) => Preshear(h, v);
        public Matrix preShear(double h, double v) => preshear(h, v);
        public Matrix pretranslate(double tx, double ty) => Pretranslate(tx, ty);
        public Matrix preTranslate(double tx, double ty) => pretranslate(tx, ty);
    }
}
