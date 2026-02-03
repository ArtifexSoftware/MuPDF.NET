using FzRect = mupdf.FzRect;
using mupdf;
using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    public class Rect
    {
        static Rect()
        {
            Utils.InitApp();
        }

        /// <summary>
        /// Left corners' x coordinate
        /// </summary>
        public float X0 { get; set; }

        /// <summary>
        /// Top corners' y coordinate
        /// </summary>
        public float Y0 { get; set; }

        /// <summary>
        /// Right corners' x -coordinate
        /// </summary>
        public float X1 { get; set; }

        /// <summary>
        /// Bottom corners' y coordinate
        /// </summary>
        public float Y1 { get; set; }
        public int Length { get; set; } = 4;

        public Point BottomLeft
        {
            get
            {
                return new Point(X0, Y1);
            }
        }

        public Point BottomRight
        {
            get
            {
                return new Point(X1, Y1);
            }
        }

        public Point TopLeft
        {
            get
            {
                return new Point(X0, Y0);
            }
        }

        public Point TopRight
        {
            get
            {
                return new Point(X1, Y0);
            }
        }

        public Rect(float X0, float Y0, float X1, float Y1)
        {
            this.X0 = X0; this.Y0 = Y0;
            this.X1 = X1; this.Y1 = Y1;
        }

        public Rect(Point tl, float x1, float y1)
        {
            this.X0 = tl.X;
            this.Y0 = tl.Y;
            X1 = x1;
            Y1 = y1;
        }

        public Rect(Rect r)
        {
            X0 = r[0]; Y0 = r[1];
            X1 = r[2]; Y1 = r[3];
        }

        public Rect(fz_rect r)
        {
            X0 = r.x0; Y0 = r.y0;
            X1 = r.x1; Y1 = r.y1;
        }

        public Rect(Point p1, Point p2)
        {
            X0 = p1.X;
            Y0 = p1.Y;
            X1 = p2.X;
            Y1 = p2.Y;
        }

        public bool IsInfinite
        {
            get
            {
                // Assuming that FZ_MIN_INF_RECT and FZ_MAX_INF_RECT are constants
                return this.X0 == this.Y0 && this.X0 == Utils.FZ_MIN_INF_RECT && this.X1 == this.Y1 && this.X1 == Utils.FZ_MAX_INF_RECT;
            }
        }

        /// <summary>
        /// Whether rectangle is empty
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return X0 >= X1 || Y0 >= Y1;
            }
        }

        /// <summary>
        /// Quad made from rectangle corners
        /// </summary>
        public Quad Quad
        {
            get
            {
                return new Quad(TopLeft, TopRight, BottomLeft, BottomRight);
            }
        }

        /// <summary>
        /// Rectangle height
        /// </summary>
        public float Height
        {
            get
            {
                return Math.Abs(Y1 - Y0);
            }
        }

        /// <summary>
        /// Rectangle width
        /// </summary>
        public float Width
        {
            get
            {
                return Math.Abs(X1 - X0);
            }
        }

        public float Abs()
        {
            if (IsEmpty || IsInfinite)
                return 0.0f;
            return (X1 - X0) * (Y1 - Y0);
        }

        public Rect(FzRect rect)
        {
            X0 = rect.x0;
            Y0 = rect.y0;
            X1 = rect.x1;
            Y1 = rect.y1;
        }

        public Rect(IRect rect) : this(rect.X0, rect.Y0, rect.X1, rect.Y1)
        {

        }

        public static Rect operator +(Rect p, float op)
        {
            return new Rect(p.X0 + op, p.Y0 + op, p.X1 + op, p.Y1 + op);
        }

        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return this.X0;
                    case 1: return this.Y0;
                    case 2: return this.X1;
                    case 3: return this.Y1;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (i)
                {
                    case 0: this.X0 = value; break;
                    case 1: this.Y0 = value; break;
                    case 2: this.X1 = value; break;
                    case 3: this.Y1 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public static Rect operator +(Rect op1, Rect op2)
        {
            return new Rect(op1.X0 + op2.X0, op1.Y0 + op2.Y0, op1.X1 + op2.X1, op1.Y1 + op2.Y1);
        }

        public FzRect ToFzRect()
        {
            return new FzRect(X0, Y0, X1, Y1);
        }

        public Rect()
        {
            X0 = X1 = Y0 = Y1 = 0.0f;
        }

        public Rect Intersect(Rect op)
        {
            if (this.IsInfinite)
            {
                return this;
            }
            else if (op.IsInfinite)
            {
                this.X0 = op.X0;
                this.Y0 = op.Y0;
                this.X1 = op.X1;
                this.Y1 = op.Y1;
            }
            else if (op.IsEmpty)
            {
                this.X0 = op.X0;
                this.Y0 = op.Y0;
                this.X1 = op.X1;
                this.Y1 = op.Y1;
            }
            else if (this.IsEmpty)
            {
                return this;
            }
            else
            {
                FzRect result = FzRect.fz_intersect_rect(ToFzRect(), op.ToFzRect()); // Replace this with actual method call

                this.X0 = result.x0;
                this.Y0 = result.y0;
                this.X1 = result.x1;
                this.Y1 = result.y1;
            }
            return this;
        }

        public static Rect operator &(Rect op1, Rect op2)
        {
            Rect t1 = new Rect(op1);
            Rect t2 = new Rect(op2);

            return t1.Intersect(t2);  // Assuming you have implemented the Intersect method
        }

        public bool Contains(float op)
        {
            // Assuming tuple equivalent is a list in this C# class
            return (new List<float> { X0, Y0, X1, Y1 }).Contains(op);
        }

        public bool Contains(Rect op)
        {
            Rect r = null;
            try
            {
                r = new Rect(op);
            }
            catch// (Exception ex)
            {
                r = (new Quad(op)).Rect;
            }
            return (X0 <= r.X0 && r.X1 <= X1) && (Y0 <= r.Y0 && r.Y1 <= Y1);
        }

        public static Rect operator *(Rect op1, float op2)
        {
            return new Rect(op1.X0 * op2, op1.Y0 * op2, op1.X1 * op2, op1.Y1 * op2);
        }

        public static Rect operator *(Rect op, Matrix m)
        {
            return op.Transform(m);
        }

        public static Rect operator -(Rect op) // negative
        {
            return new Rect(-op.X0, -op.Y0, -op.X1, -op.Y1);
        }

        public static Rect operator |(Rect op1, Rect op2) // |
        {
            return op1.IncludeRect(op2);
        }

        public static Rect operator |(Rect op1, Point op2)
        {
            return op1.IncludePoint(op2);
        }

        public static Rect operator +(Rect op) // positive
        {
            return new Rect(+op.X0, +op.Y0, +op.X1, +op.Y1);
        }

        override
        public string ToString() => $"Rect({Utils.FloatToString(X0)}, {Utils.FloatToString(Y0)}, {Utils.FloatToString(X1)}, {Utils.FloatToString(Y1)})";

        public static Rect operator -(Rect p, float op)
        {
            return new Rect(p.X0 - op, p.Y0 - op, p.X1 - op, p.Y1 - op);
        }

        public static Rect operator -(Rect op1, Rect op2)
        {
            return new Rect(op1.X0 - op2.X0, op1.Y0 - op2.Y0, op1.X1 - op2.Y1, op1.Y1 - op2.Y1);
        }

        /// <summary>
        /// Enlarge rectangle to also contain another one
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public Rect IncludeRect(Rect r)
        {
            if (r.IsInfinite || this.IsInfinite)
            {
                this.X0 = Utils.FZ_MIN_INF_RECT;
                this.Y0 = Utils.FZ_MIN_INF_RECT;
                this.X1 = Utils.FZ_MAX_INF_RECT;
                this.Y1 = Utils.FZ_MAX_INF_RECT;
            }
            else if (r.IsEmpty)
            {
                return this;
            }
            else if (this.IsEmpty)
            {
                this.X0 = r.X0;
                this.Y0 = r.Y0;
                this.X1 = r.X1;
                this.Y1 = r.Y1;
            }
            else
            {
                FzRect ret = FzRect.fz_union_rect(ToFzRect(), r.ToFzRect());
                this.X0 = ret.x0;
                this.Y0 = ret.y0;
                this.X1 = ret.x1;
                this.Y1 = ret.y1;
            }
            return this;
        }

        /// <summary>
        /// Whether rectangle is valid
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return X0 <= X1 && Y0 <= Y1;
        }

        public Quad Morph(Point p, Matrix m)
        {
            if (this.IsInfinite)
            {
                return Utils.INFINITE_RECT().Quad;
            }
            return this.Quad.Morph(p, m);
        }

        public float Norm()
        {
            float ret = 0.0f;
            for (int i = 0; i < this.Length; i++)
            {
                ret += this[i] * this[i];
            }

            return (float)Math.Sqrt(ret);
        }

        public void Normalize()
        {
            if (X1 < X0)
            {
                float tmp = X0;
                X0 = X1;
                X1 = tmp;
            }
            if (Y1 < Y0)
            {
                float tmp = Y0;
                Y0 = Y1;
                Y1 = tmp;
            }
        }

        /// <summary>
        /// Enlarge rectangle to also contain a point
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Rect IncludePoint(Point p)
        {
            return new Rect(ToFzRect().fz_include_point_in_rect(p.ToFzPoint()));
        }

        /// <summary>
        /// Returns true if this rectangle and <paramref name="r"/> overlap (without mutating either).
        /// </summary>
        public bool Intersects(Rect r)
        {
            if (IsEmpty || IsInfinite || r.IsEmpty || r.IsInfinite)
                return false;
            return (X0 < r.X1 && r.X0 < X1) && (Y0 < r.Y1 && r.Y0 < Y1);
        }

        /// <summary>
        /// Create smallest IRect containing rectangle
        /// </summary>
        /// <returns></returns>
        public IRect Round()
        {
            return new IRect(ToFzRect().fz_round_rect());
        }

        /// <summary>
        /// Return matrix that converts to target rect.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Matrix ToRect(Rect rect)
        {
            if (IsInfinite || IsEmpty || rect.IsInfinite || rect.IsEmpty)
                throw new Exception("rectangles must be finite and not empty.");
            return new Matrix(1, 0, 0, 1, -X0, -Y0) * new Matrix(rect.Width / Width, rect.Height / Height) * new Matrix(1, 0, 0, 1, rect.X0, rect.Y0);
        }

        public Rect Transform(Matrix matrix)
        {
            FzRect r = mupdf.mupdf.fz_transform_rect(ToFzRect(), matrix.ToFzMatrix());
            
            return new Rect(r);
        }

        /// <summary>
        /// Calculate area of rectangle.\nparameter is one of 'px' (default), 'in', 'cm', or 'mm'.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public float GetArea(string unit = null)
        {
            string unit_ = "px";
            if (!string.IsNullOrEmpty(unit))
                unit_ = unit;
            Dictionary<string, (float, float)> u = new Dictionary<string, (float, float)>
            {
                { "px", (1, 1) },
                { "in", (1.0f, 72.0f) },
                { "cm", (2.54f, 72.0f) },
                { "mm", (25.4f, 72.0f) }
            };

            float f = (float)Math.Pow(u[unit].Item1 / u[unit].Item2, 2);
            return f * Width * Height;
        }

        public bool EqualTo(Rect obj)
        {
            return (X0 == ((Rect)obj).X0 && Y0 == ((Rect)obj).Y0 && X1 == ((Rect)obj).X1 && Y1 == ((Rect)obj).Y1);
        }
    }

}
