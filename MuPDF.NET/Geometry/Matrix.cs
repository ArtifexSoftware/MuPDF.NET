using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace MuPDF.NET
{
    /// <summary>
    /// Row-major 3×3 affine transform used for PDF/image coordinate mapping .
    /// </summary>
    /// <remarks>
    /// <para>Only six values <c>[a, b, c, d, e, f]</c> are used; they map points as in the Adobe PDF
    /// specification. Convenience methods (<see cref="Prerotate"/>, <see cref="Prescale"/>, etc.) are
    /// equivalent to updating those six numbers directly.</para>
    /// <para>Legacy API reference:
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Matrix.html"/>.</para>
    /// <para>Constructors: <see cref="Matrix()"/> (zeros),
    /// <see cref="Matrix(float,float)"/> (zoom),
    /// <see cref="Matrix(float,float,float)"/> (zoom with third arg <c>0</c>, or shear with <c>1</c>),
    /// <see cref="Matrix(float,float,float,float,float,float)"/>,
    /// <see cref="Matrix(Matrix)"/> (copy),
    /// <see cref="Matrix(int)"/> / single float (rotation degrees, counter-clockwise for positive angles),
    /// six-element sequence, or <see cref="Matrix(Rect)"/> (page mediabox transform).</para>
    /// <para>For a non-transforming matrix constant, use <see cref="IdentityMatrix"/> or
    /// <see cref="Identity"/>; for a modifiable identity, use <c>new Matrix(1, 1)</c> or
    /// <c>new Matrix(new IdentityMatrix())</c>.</para>
    /// </remarks>
    public class Matrix : IEnumerable<float>, IEquatable<Matrix>
    {
        /// <summary>Zoom factor in the X direction (width). Negative values flip left-right.</summary>
        public virtual float A { get; set; }
        /// <summary>Shear: each point <c>(x, y)</c> becomes <c>(x, y - b*x)</c> (tilts horizontal lines).</summary>
        public virtual float B { get; set; }
        /// <summary>Shear: each point <c>(x, y)</c> becomes <c>(x - c*y, y)</c> (tilts vertical lines).</summary>
        public virtual float C { get; set; }
        /// <summary>Zoom factor in the Y direction (height). Negative values flip up-down.</summary>
        public virtual float D { get; set; }
        /// <summary>Horizontal shift: <c>(x, y) → (x + e, y)</c>; positive <c>e</c> moves right.</summary>
        public virtual float E { get; set; }
        /// <summary>Vertical shift: <c>(x, y) → (x, y - f)</c>; positive <c>f</c> moves down.</summary>
        public virtual float F { get; set; }

        /// <summary>Lowercase alias for <see cref="A"/>..</summary>
        public float a { get => A; set => A = value; }
        /// <summary>Lowercase alias for <see cref="B"/>..</summary>
        public float b { get => B; set => B = value; }
        /// <summary>Lowercase alias for <see cref="C"/>..</summary>
        public float c { get => C; set => C = value; }
        /// <summary>Lowercase alias for <see cref="D"/>..</summary>
        public float d { get => D; set => D = value; }
        /// <summary>Lowercase alias for <see cref="E"/>..</summary>
        public float e { get => E; set => E = value; }
        /// <summary>Lowercase alias for <see cref="F"/>..</summary>
        public float f { get => F; set => F = value; }

        /// <summary>Sequence length (always 6).</summary>
        public int Length => Count;

        /// <summary>Shared identity instance.</summary>
        public static readonly Matrix Identity = new IdentityMatrix();

        /// <summary>Creates the zero matrix <c>(0, 0, 0, 0, 0, 0)</c>.</summary>
        public Matrix() => A = B = C = D = E = F = 0.0f;

        /// <summary>Creates a matrix from six components <c>(a, b, c, d, e, f)</c>.</summary>
        public Matrix(float a, float b, float c, float d, float e, float f)
        {
            A = a; B = b; C = c; D = d; E = e; F = f;
        }

        /// <summary>Creates a zoom matrix <c>(zoomX, 0, 0, zoomY, 0, 0)</c>.</summary>
        public Matrix(float zoomX, float zoomY)
        {
            A = zoomX; B = 0; C = 0; D = zoomY; E = 0; F = 0;
        }

        /// <summary>
        /// Creates a counter-clockwise rotation matrix for positive <paramref name="angleDegrees"/>
        /// .
        /// </summary>
        public Matrix(float angleDegrees) => Assign(RotationMatrix(angleDegrees));

        /// <summary>
        /// Creates a counter-clockwise rotation matrix (legacy <c>Matrix(int degree)</c>).
        /// </summary>
        public Matrix(int angleDegrees) : this((float)angleDegrees) { }

        /// <summary>
        /// Zoom (<paramref name="zoomOrShear"/> = 0) or shear (<paramref name="zoomOrShear"/> = 1) constructor.
        /// </summary>
        /// <exception cref="ValueErrorException">Third argument is not 0 or 1.</exception>
        public Matrix(float p0, float p1, float zoomOrShear)
        {
            if (zoomOrShear == 0)
            {
                A = p0; B = 0; C = 0; D = p1; E = 0; F = 0;
            }
            else if (zoomOrShear == 1)
            {
                A = 1.0f; B = p1; C = p0; D = 1.0f; E = 0; F = 0;
            }
            else
                throw new ValueErrorException("Matrix: bad args");
        }

        /// <summary>Creates a copy of another matrix.</summary>
        public Matrix(Matrix o) => Assign(o);

        /// <summary>Creates a matrix from a MuPDF <c>fz_matrix</c> wrapper.</summary>
        public Matrix(mupdf.FzMatrix m) => Assign(m);

        /// <summary>
        /// Creates the default page transformation for a PDF mediabox
        /// (<c>fz_transform_page(mediabox, 72, 0)</c>).
        /// </summary>
        /// <remarks>
        /// Documented on legacy readthedocs as <c>Matrix(Rect)</c>. Older MuPDF.NET builds incorrectly
        /// treated <see cref="Rect.X0"/> as a rotation angle; this constructor uses MuPDF page semantics.
        /// MuPDF.NET adds a <c>Matrix(rect)</c> overload not present in MuPDF.
        /// </remarks>
        public Matrix(Rect mediabox)
        {
            if (mediabox == null)
                throw new ArgumentNullException(nameof(mediabox));
            using var fr = mediabox.ToFzRect();
            using var fm = mupdf.mupdf.fz_transform_page(fr, 72f, 0f);
            Assign(fm);
        }

        /// <summary>
        /// Factory with optional overrides for matrix elements <c>a</c>–<c>f</c>.
        /// </summary>
        public static Matrix Create(
            object[] args,
            float? a = null,
            float? b = null,
            float? c = null,
            float? d = null,
            float? e = null,
            float? f = null)
        {
            var m = FromArgs(args);
            if (a != null) m.A = a.Value;
            if (b != null) m.B = b.Value;
            if (c != null) m.C = c.Value;
            if (d != null) m.D = d.Value;
            if (e != null) m.E = e.Value;
            if (f != null) m.F = f.Value;
            return m;
        }

        /// <inheritdoc cref="Create(object[], float?, float?, float?, float?, float?, float?)"/>
        public static Matrix Create(params object[] args) => FromArgs(args);

        /// <summary>Number of components (6).</summary>
        public int Count => 6;

        /// <summary>Indexed access to <c>[a, b, c, d, e, f]</c>.</summary>
        public virtual float this[int i]
        {
            get => i switch
            {
                0 => A, 1 => B, 2 => C, 3 => D, 4 => E, 5 => F,
                _ => throw new IndexOutOfRangeException("index out of range"),
            };
            set
            {
                switch (i)
                {
                    case 0: A = value; break;
                    case 1: B = value; break;
                    case 2: C = value; break;
                    case 3: D = value; break;
                    case 4: E = value; break;
                    case 5: F = value; break;
                    default: throw new IndexOutOfRangeException("index out of range");
                }
            }
        }

        /// <summary>Euclidean norm of the six-element vector ( / <c>__abs__</c>).</summary>
        public float Norm =>
            (float)Math.Sqrt(A * A + B * B + C * C + D * D + E * E + F * F);

        /// <summary>Absolute-value alias for <see cref="Norm"/>.</summary>
        public float Abs() => Norm;

        /// <summary>True if not all components are zero.</summary>
        public bool IsTruthy => !(Math.Max(Math.Max(A, B), Math.Max(C, Math.Max(D, Math.Max(E, F)))) == 0
            && Math.Min(Math.Min(A, B), Math.Min(C, Math.Min(D, Math.Min(E, F)))) == 0);

        /// <summary>
        /// True when the transform maps axis-aligned rectangles to axis-aligned rectangles
        /// (no shear, rotation is a multiple of 90°).
        /// </summary>
        public bool IsRectilinear =>
            (Math.Abs(B) < Constants.Epsilon && Math.Abs(C) < Constants.Epsilon)
            || (Math.Abs(A) < Constants.Epsilon && Math.Abs(D) < Constants.Epsilon);

        /// <summary>
        /// Replaces this matrix with <paramref name="one"/> × <paramref name="two"/> .
        /// </summary>
        /// <remarks>Multiplication is not commutative; order matters.</remarks>
        public virtual Matrix ConcatInto(Matrix one, Matrix two)
        {
            if (one.Count != 6 || two.Count != 6)
                throw new ValueErrorException("Matrix: bad seq len");
            using var r = mupdf.mupdf.fz_concat(one.ToFzMatrix(), two.ToFzMatrix());
            Assign(r);
            return this;
        }

        /// <summary>
        /// Returns <paramref name="one"/> × <paramref name="two"/> as a new matrix .
        /// </summary>
        public static Matrix Concat(Matrix one, Matrix two) => ConcatCore(one, two);

        private static Matrix ConcatCore(Matrix one, Matrix two)
        {
            if (one.Count != 6 || two.Count != 6)
                throw new ValueErrorException("Matrix: bad seq len");
            using var r = mupdf.mupdf.fz_concat(one.ToFzMatrix(), two.ToFzMatrix());
            return new Matrix(r);
        }

        /// <summary>
        /// Inverts <paramref name="src"/> (or this matrix) into the current instance.
        /// </summary>
        /// <returns>0 if invertible (matrix updated); 1 if singular (unchanged).</returns>
        public virtual int Invert(Matrix? src = null)
        {
            var (code, a, b, c, d, e, f) = UtilInvertMatrix(src ?? this);
            if (code != 0)
                return 1;
            A = a; B = b; C = c; D = d; E = e; F = f;
            return 0;
        }

        /// <summary>Returns a new inverted matrix, or <c>null</c> if singular.</summary>
        public Matrix? Inverted()
        {
            var m = new Matrix();
            return m.Invert(this) != 0 ? null : m;
        }

        /// <summary>
        /// Pre-multiplies a counter-clockwise rotation (positive <paramref name="theta"/> in degrees).
        /// </summary>
        /// <remarks>On an identity matrix:
        /// <c>[1,0,0,1,0,0] → [cos θ, sin θ, -sin θ, cos θ, 0, 0]</c>.</remarks>
        public virtual Matrix Prerotate(float theta)
        {
            while (theta < 0) theta += 360;
            while (theta >= 360) theta -= 360;
            if (Math.Abs(theta) < Constants.Epsilon) { }
            else if (Math.Abs(90.0 - theta) < Constants.Epsilon)
            {
                float a0 = A, b0 = B;
                A = C; B = D; C = -a0; D = -b0;
            }
            else if (Math.Abs(180.0 - theta) < Constants.Epsilon)
            {
                A = -A; B = -B; C = -C; D = -D;
            }
            else if (Math.Abs(270.0 - theta) < Constants.Epsilon)
            {
                float a0 = A, b0 = B;
                A = -C; B = -D; C = a0; D = b0;
            }
            else
            {
                float rad = theta * (float)Math.PI / 180.0f;
                float s = (float)Math.Sin(rad), co = (float)Math.Cos(rad);
                float a0 = A, b0 = B;
                A = co * a0 + s * C;
                B = co * b0 + s * D;
                C = -s * a0 + co * C;
                D = -s * b0 + co * D;
            }
            return this;
        }

        /// <summary>
        /// Pre-multiplies scaling: <c>[a,b,c,d,e,f] → [a*sx, b*sx, c*sy, d*sy, e, f]</c>.
        /// </summary>
        public virtual Matrix Prescale(float sx, float sy)
        {
            A *= sx; B *= sx; C *= sy; D *= sy;
            return this;
        }

        /// <summary>Pre-multiplies shearing.</summary>
        public virtual Matrix Preshear(float h, float v)
        {
            float a0 = A, b0 = B;
            A += v * C; B += v * D;
            C += h * a0; D += h * b0;
            return this;
        }

        /// <summary>
        /// Pre-multiplies translation: <c>e,f</c> become <c>tx*a + ty*c, tx*b + ty*d</c> added to existing <c>e,f</c>.
        /// </summary>
        public virtual Matrix Pretranslate(float tx, float ty)
        {
            E += tx * A + ty * C;
            F += tx * B + ty * D;
            return this;
        }

        /// <summary>Creates a pure rotation matrix.</summary>
        public static Matrix Rotation(float degrees) => RotationMatrix(degrees);

        /// <summary>Creates a shear matrix <c>(sx, sy, 1)</c>.</summary>
        public static Matrix Shear(float sx, float sy) => new Matrix(sx, sy, 1);

        /// <summary>Exports this matrix as <c>fz_matrix</c>.</summary>
        public mupdf.FzMatrix ToFzMatrix() =>
            mupdf.mupdf.fz_make_matrix((float)A, (float)B, (float)C, (float)D, (float)E, (float)F);

        public static Matrix operator +(Matrix a, Matrix b) =>
            new Matrix(a.A + b.A, a.B + b.B, a.C + b.C, a.D + b.D, a.E + b.E, a.F + b.F);

        public static Matrix operator +(Matrix a, float s) =>
            new Matrix(a.A + s, a.B + s, a.C + s, a.D + s, a.E + s, a.F + s);

        public static Matrix operator +(Matrix a, int s) => a + (float)s;

        public static Matrix operator -(Matrix a, Matrix b) =>
            new Matrix(a.A - b.A, a.B - b.B, a.C - b.C, a.D - b.D, a.E - b.E, a.F - b.F);

        public static Matrix operator -(Matrix a, float s) =>
            new Matrix(a.A - s, a.B - s, a.C - s, a.D - s, a.E - s, a.F - s);

        public static Matrix operator -(Matrix a) =>
            new Matrix(-a.A, -a.B, -a.C, -a.D, -a.E, -a.F);

        public static Matrix operator *(Matrix a, float m) =>
            new Matrix(a.A * m, a.B * m, a.C * m, a.D * m, a.E * m, a.F * m);

        public static Matrix operator *(float m, Matrix a) => a * m;

        public static Matrix operator *(Matrix a, Matrix b) =>
            Concat(a, b);

        public static Matrix operator /(Matrix a, float m) => a * (1.0f / m);

        public static Matrix operator /(Matrix a, Matrix b)
        {
            var (code, ia, ib, ic, id, ie, iff) = UtilInvertMatrix(b);
            if (code != 0)
                throw new DivideByZeroException("matrix not invertible");
            return Concat(a, new Matrix(ia, ib, ic, id, ie, iff));
        }

        /// <summary>Matrix inversion; returns a zero matrix if singular.</summary>
        public static Matrix operator ~(Matrix m) => m.Inverted() ?? new Matrix();

        public static Matrix operator +(Matrix a) => new Matrix(a);

        public static bool operator ==(Matrix? a, Matrix? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            for (int i = 0; i < 6; i++)
                if (Math.Abs(a[i] - b[i]) >= Constants.Epsilon)
                    return false;
            return true;
        }

        public static bool operator !=(Matrix? a, Matrix? b) => !(a == b);

        /// <inheritdoc/>
        public bool Equals(Matrix? other) => this == other;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is Matrix m) return this == m;
            if (obj is IEnumerable seq && obj is not string)
            {
                var vals = new List<float>();
                foreach (var v in seq)
                {
                    if (v is float f) vals.Add(f);
                    else if (v is double d) vals.Add((float)d);
                    else if (v is int i) vals.Add(i);
                    else return false;
                }
                if (vals.Count != 6) return false;
                var diff = this - new Matrix(vals[0], vals[1], vals[2], vals[3], vals[4], vals[5]);
                for (int i = 0; i < 6; i++)
                    if (Math.Abs(diff[i]) >= Constants.Epsilon)
                        return false;
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override string ToString() => $"Matrix({A}, {B}, {C}, {D}, {E}, {F})";

        /// <inheritdoc/>
        public IEnumerator<float> GetEnumerator()
        {
            yield return A; yield return B; yield return C;
            yield return D; yield return E; yield return F;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal static Matrix FromArgs(object[]? args)
        {
            if (args == null || args.Length == 0)
                return new Matrix();
            if (args.Length > 6)
                throw new ValueErrorException("Matrix: bad seq len");
            if (args.Length == 6)
            {
                return new Matrix(
                    (float)Convert.ToDouble(args[0], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[1], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[2], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[3], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[4], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[5], CultureInfo.InvariantCulture));
            }
            if (args.Length == 1)
            {
                var arg = args[0];
                if (arg is Matrix m) return new Matrix(m);
                if (arg is IdentityMatrix) return new Matrix(1, 0, 0, 1, 0, 0);
                if (arg is mupdf.FzMatrix fm) return new Matrix(fm);
                if (arg is Rect r) return new Matrix(r);
                if (arg is float or double or int)
                    return new Matrix((float)Convert.ToDouble(arg, CultureInfo.InvariantCulture));
                if (arg is IEnumerable seq && arg is not string)
                {
                    var list = new List<float>();
                    foreach (var v in seq)
                        list.Add((float)Convert.ToDouble(v, CultureInfo.InvariantCulture));
                    if (list.Count != 6)
                        throw new ValueErrorException("Matrix: bad seq len");
                    return new Matrix(list[0], list[1], list[2], list[3], list[4], list[5]);
                }
                throw new ValueErrorException("Matrix: bad args");
            }
            if (args.Length == 2 || (args.Length == 3 && Convert.ToDouble(args[2], CultureInfo.InvariantCulture) == 0))
            {
                return new Matrix(
                    (float)Convert.ToDouble(args[0], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[1], CultureInfo.InvariantCulture));
            }
            if (args.Length == 3 && Convert.ToDouble(args[2], CultureInfo.InvariantCulture) == 1)
            {
                return new Matrix(
                    (float)Convert.ToDouble(args[0], CultureInfo.InvariantCulture),
                    (float)Convert.ToDouble(args[1], CultureInfo.InvariantCulture),
                    1);
            }
            throw new ValueErrorException("Matrix: bad args");
        }

        private static Matrix RotationMatrix(float degrees)
        {
            double theta = Math.PI * degrees / 180.0;
            double c = Math.Round(Math.Cos(theta), 8);
            double s = Math.Round(Math.Sin(theta), 8);
            return new Matrix((float)c, (float)s, (float)-s, (float)c, 0, 0);
        }

        private static (int code, float a, float b, float c, float d, float e, float f) UtilInvertMatrix(Matrix matrix)
        {
            float det = matrix.A * matrix.D - matrix.B * matrix.C;
            if (!(det < -float.Epsilon || det > float.Epsilon))
                return (1, 0, 0, 0, 0, 0, 0);
            float rdet = 1.0f / det;
            float a = matrix.D * rdet;
            float b = -matrix.B * rdet;
            float c = -matrix.C * rdet;
            float d = matrix.A * rdet;
            float e = -matrix.E * a - matrix.F * c;
            float f = -matrix.E * b - matrix.F * d;
            return (0, a, b, c, d, e, f);
        }

        private void Assign(Matrix o)
        {
            A = o.A; B = o.B; C = o.C; D = o.D; E = o.E; F = o.F;
        }

        private void Assign(mupdf.FzMatrix m)
        {
            A = m.a; B = m.b; C = m.c; D = m.d; E = m.e; F = m.f;
        }
    }

    /// <summary>
    /// Immutable identity matrix <c>[1, 0, 0, 1, 0, 0]</c> ( / <c>Identity</c>).
    /// </summary>
    /// <remarks>
    /// <para>Performs no transform. Component setters and mutating methods are disabled; values always
    /// remain identity.</para>
    /// <para>Legacy reference:
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/classes/IdentityMatrix.html"/>.</para>
    /// <para>For a mutable identity, use <c>new Matrix(1, 1)</c>, <c>new Matrix(1, 0, 0, 1, 0, 0)</c>,
    /// <c>new Matrix(0)</c>, or <c>new Matrix(new IdentityMatrix())</c>.</para>
    /// </remarks>
    public sealed class IdentityMatrix : Matrix
    {
        /// <summary>Creates <c>[1, 0, 0, 1, 0, 0]</c>.</summary>
        public IdentityMatrix() : base(1, 0, 0, 1, 0, 0) { }

        /// <inheritdoc/>
        public override string ToString() =>
            "IdentityMatrix(1.0, 0.0, 0.0, 1.0, 0.0, 0.0)";

        /// <inheritdoc/>
        public override float A { get => 1.0f; set { } }
        /// <inheritdoc/>
        public override float B { get => 0.0f; set { } }
        /// <inheritdoc/>
        public override float C { get => 0.0f; set { } }
        /// <inheritdoc/>
        public override float D { get => 1.0f; set { } }
        /// <inheritdoc/>
        public override float E { get => 0.0f; set { } }
        /// <inheritdoc/>
        public override float F { get => 0.0f; set { } }

        /// <inheritdoc/>
        public override float this[int i]
        {
            get => base[i];
            set { }
        }

        /// <inheritdoc/>
        public override Matrix ConcatInto(Matrix one, Matrix two) => this;

        /// <inheritdoc/>
        public override int Invert(Matrix? src = null)
        {
            var probe = new Matrix();
            return probe.Invert(src ?? this);
        }

        /// <inheritdoc/>
        public override Matrix Prerotate(float theta) => this;

        /// <inheritdoc/>
        public override Matrix Prescale(float sx, float sy) => this;

        /// <inheritdoc/>
        public override Matrix Preshear(float h, float v) => this;

        /// <inheritdoc/>
        public override Matrix Pretranslate(float tx, float ty) => this;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + 1;
                hash = hash * 31 + 0;
                hash = hash * 31 + 0;
                hash = hash * 31 + 1;
                return hash;
            }
        }
    }
}