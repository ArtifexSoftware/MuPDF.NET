using mupdf;

namespace MuPDF.NET
{
    public class Matrix
    {
        public float A { get; set; }
        public float B { get; set; }
        public float C { get; set; }
        public float D { get; set; }
        public float E { get; set; }
        public float F { get; set; }
        public int Length { get; set; } = 6;

        /// <summary>
        /// Rectilinear means that no shearing is present and that any rotations are integer multiples of 90 degrees. Usually this is used to confirm that (axis-aligned) rectangles before the transformation are still axis-aligned rectangles afterwards.
        /// </summary>
        public bool IsRectilinear
        {
            get
            {
                return (Math.Abs(B) < float.Epsilon && Math.Abs(C) < float.Epsilon) ||
                (Math.Abs(A) < float.Epsilon && Math.Abs(D) < float.Epsilon);
            }
        }
        public Matrix(float a, float b, float c, float d, float e, float f)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
        }

        public Matrix(Rect arg)
        {
            double theta = Math.PI * arg.X0 / 180;
            double c = Math.Round(Math.Cos(theta), 8);
            double s = Math.Round(Math.Sin(theta), 8);
            A = D = (float)c;
            B = (float)s;
            C = (float)-s;
            E = F = 0;
        }

        public Matrix(int arg)
        {
            double theta = Math.PI * arg / 180;
            double c = Math.Round(Math.Cos(theta), 8);
            double s = Math.Round(Math.Sin(theta), 8);
            A = D = (float)c;
            B = (float)s;
            C = (float)-s;
            E = F = 0;
        }

        public Matrix(float arg0, float arg1, float arg2)
        {
            if (arg2 == 0)
                (A, B, C, D, E, F) = (arg0, 0, 0, arg1, 0, 0);
            else if (arg2 == 1)
                (A, B, C, D, E, F) = (1, 0, 0, arg1, 0, 0);
            else
                throw new ArgumentException("bad args");
        }

        public Matrix(float arg0, float arg1)
        {
            (A, B, C, D, E, F) = (arg0, 0, 0, arg1, 0, 0);
        }

        public float Abs()
        {
            float sum = 0;
            for (int i = 0; i < Length; i++)
                sum += this[i] * this[i];
            return (float)Math.Sqrt((double)sum);
        }

        /// <summary>
        /// the Euclidean norm
        /// </summary>
        /// <returns></returns>
        public float Norm()
        {
            return Abs();
        }

        public Matrix(FzMatrix m)
        {
            A = m.a;
            B = m.b;
            C = m.c;
            D = m.d;
            E = m.e;
            F = m.f;
        }

        public Matrix()
        {
            A = 0.0f;
            B = 0.0f;
            C = 0.0f;
            D = 0.0f;
            E = 0.0f;
            F = 0.0f;
        }

        public Matrix(Matrix m)
        {
            A = m.A;
            B = m.B;
            C = m.C;
            D = m.D;
            E = m.E;
            F = m.F;
        }

        public static Matrix operator +(Matrix op1, Matrix op2)
        {
            return new Matrix(op1.A + op2.A, op1.B + op2.B, op1.C + op2.C, op1.D + op2.D, op1.E + op2.E, op1.F + op2.F);
        }

        public FzMatrix ToFzMatrix()
        {
            return new FzMatrix(A, B, C, D, E, F);
        }

        public static Matrix operator +(Matrix op1, float op2)
        {
            return new Matrix(op1.A + op2, op1.B + op2, op1.C + op2, op1.D + op2, op1.E + op2, op1.F + op2);
        }

        public float this[int i]
        {
            get
            {
                return (new List<float> { A, B, C, D, E, F })[i];
            }
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
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public static Matrix operator *(Matrix op1, float op2)
        {
            return new Matrix(op1.A * op2, op1.B * op2, op1.C * op2, op1.D * op2, op1.E * op2, op1.F * op2);
        }

        public static Matrix operator *(Matrix op1, Matrix op2)
        {
            Matrix t = new Matrix(1, 1);
            return Matrix.Concat(op1, op2);
        }

        public static Matrix operator -(Matrix op1, Matrix op2)
        {
            return new Matrix(op1.A - op2.A, op1.B - op2.B, op1.C - op2.C, op1.D - op2.D, op1.E - op2.E, op1.F - op2.F);
        }

        public static Matrix operator -(Matrix op1, float m)
        {
            return new Matrix(op1.A - m, op1.B - m, op1.C - m, op1.D - m, op1.E - m, op1.F - m);
        }

        /// <summary>
        /// Calculate the matrix inverse of *m* and store the result in the current matrix.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public int Invert(Matrix src)
        {
            (int, Matrix) dst;
            if (src == null)
            {
                dst = Utils.InvertMatrix(this);
            }
            else
            {
                dst = Utils.InvertMatrix(src);
            }

            if (dst.Item1 == 1)
                return 1;

            (A, B, C, D, E, F) = (dst.Item2.A, dst.Item2.B, dst.Item2.C, dst.Item2.D, dst.Item2.E, dst.Item2.F);
            return 0;
        }

        /// <summary>
        /// Calculate the matrix product *m1 * m2* and store the result in the current matrix.
        /// </summary>
        /// <param name="op1"></param>
        /// <param name="op2"></param>
        /// <returns></returns>
        public static Matrix Concat(Matrix op1, Matrix op2)
        {
            Matrix ret = new Matrix(mupdf.mupdf.fz_concat(op1.ToFzMatrix(), op2.ToFzMatrix()));
            return ret;
        }

        /// <summary>
        /// Modify the matrix to perform a counter-clockwise rotation for positive *deg* degrees, else clockwise.
        /// </summary>
        /// <param name="theta"></param>
        /// <returns></returns>
        public Matrix Prerotate(float theta)
        {
            while (theta < 0)
            {
                theta += 360;
            }
            while (theta >= 360)
            {
                theta -= 360;
            }
            if (Math.Abs(0 - theta) < float.Epsilon)
            {
                // do nothing
            }
            else if (Math.Abs(90.0 - theta) < float.Epsilon)
            {
                float a = this.A;
                float b = this.B;
                this.A = this.C;
                this.B = this.D;
                this.C = -a;
                this.D = -b;
            }
            else if (Math.Abs(180.0 - theta) < float.Epsilon)
            {
                this.A = -this.A;
                this.B = -this.B;
                this.C = -this.C;
                this.D = -this.D;
            }
            else if (Math.Abs(270.0 - theta) < float.Epsilon)
            {
                float a = this.A;
                float b = this.B;
                this.A = -this.C;
                this.B = -this.D;
                this.C = a;
                this.D = b;
            }
            else
            {
                double rad = Math.PI * theta / 180.0;
                double s = Math.Sin(rad);
                double c = Math.Cos(rad);
                float a = this.A;
                float b = this.B;
                this.A = (float)(c * a + s * this.C);
                this.B = (float)(c * b + s * this.D);
                this.C = (float)(-s * a + c * this.C);
                this.D = (float)(-s * b + c * this.D);
            }
            return this;
        }

        /// <summary>
        /// Modify the matrix to scale by the zoom factors sx and sy.
        /// </summary>
        /// <param name="sx">Zoom factor in X direction.</param>
        /// <param name="sy">Zoom factor in Y direction.</param>
        public void Prescale(float sx, float sy)
        {
            this.A *= sx;
            this.B *= sx;
            this.C *= sy;
            this.D *= sy;
        }

        /// <summary>
        /// Modify the matrix to perform a shearing, i.e. transformation of rectangles into parallelograms (rhomboids).
        /// </summary>
        /// <param name="h">Shearing effect in X direction.</param>
        /// <param name="v">Shearing effect in Y direction.</param>
        /// <returns></returns>
        public Matrix Preshear(float h, float v)
        {
            float a = A;
            float b = B;
            A += v * C;
            B += v * D;
            C += h * a;
            D += h * b;
            return this;
        }

        /// <summary>
        /// Modify the matrix to perform a shifting / translation operation along the x and / or y axis.
        /// </summary>
        /// <param name="tx">Translation effect in X direction.</param>
        /// <param name="ty">Translation effect in Y direction.</param>
        /// <returns></returns>
        public Matrix Pretranslate(float tx, float ty)
        {
            E += tx * A + ty * C;
            F += tx * B + ty * D;
            return this;
        }

        public static Matrix operator ~(Matrix m)
        {
            Matrix m1 = new Matrix();
            int _ = m1.Invert(m);
            return m1;
        }

        public override string ToString()
        {
            return $"{A} {B} {C} {D} {E} {F}";
        }

    }
}
