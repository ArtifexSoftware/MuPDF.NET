using System;
using System.Collections;
using System.Reflection.Metadata;

namespace CSharpMuPDF
{
    internal class Rect
    {
        public float X0 { get; set; }
        public float Y0 { get; set; }
        public float X1 { get; set; }
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

        public Rect(Rect r)
        {
            X0 = r[0]; Y0 = r[1];
            X1 = r[2]; Y1 = r[3];
        }
        public bool IsInfinite
        {
            get
            {
                // Assuming that FZ_MIN_INF_RECT and FZ_MAX_INF_RECT are constants
                return this.X0 == this.Y0 && this.X0 == Globals.FZ_MIN_INF_RECT && this.X1 == this.Y1 && this.X1 == Globals.FZ_MAX_INF_RECT;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return X0 >= X1 || Y0 >= Y1;
            }
        }
        public Quad Quad
        {
            get
            {
                return new Quad(TopLeft, TopRight, BottomLeft, BottomRight);
            }
        }
        public float Height
        {
            get
            {
                return Math.Abs(Y1 - Y0);
            }
        }

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

        public Rect()
        {
            X0 = X1 = Y0 = Y1 = 0.0f;
        }

        /*public Rect Intersect(Rect op)
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
                var result = Utils.UtilIntersectRect(this, op); // Replace this with actual method call

                this.X0 = result.X0;
                this.Y0 = result.Y0;
                this.X1 = result.X1;
                this.Y1 = result.Y1;
            }
            return this;
        }*/

        /*public static Rect operator &(Rect op1, Rect op2)
        {
            return op1.Intersect(op2);  // Assuming you have implemented the Intersect method
        }*/

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
            catch (Exception ex)
            {
                r = (new Quad(op)).Rect;
            }
            return (X0 <= r.X0 && r.X1 <= X1) && (Y0 <= r.Y0 && r.Y1 <= Y1);
        }

        public static Rect operator *(Rect op1, float op2)
        {
            return new Rect(op1.X0 * op2, op1.Y0 * op2, op1.X1 * op2, op1.Y1 * op2);
        }

        public static Rect operator -(Rect op) // negative
        {
            return new Rect(-op.X0, -op.Y0, -op.X1, -op.Y1);
        }

       /*public static bool operator |(Rect op1, Rect op2) // or
        {
            return op1.IncludeRect(op2);
        }*/

        public static Rect operator +(Rect op) // positive
        {
            return new Rect(+op.X0, +op.Y0, +op.X1, +op.Y1);
        }

        override
        public string ToString() => $"Rect({X0}, {Y0}, {X1}, {Y1})";

        public static Rect operator -(Rect p, float op)
        {
            return new Rect(p.X0 - op, p.Y0 - op, p.X1 - op, p.Y1 - op);
        }

        public static Rect operator -(Rect op1, Rect op2)
        {
            return new Rect(op1.X0 - op2.X0, op1.Y0 - op2.Y0, op1.X1 - op2.Y1, op1.Y1 - op2.Y1);
        }

        /*public Rect IncludeRect(Rect r)
        {
            if (r.IsInfinite || this.IsEmpty)
            {
                this.X0 = Globals.FZ_MIN_INF_RECT;
                this.Y0 = Globals.FZ_MIN_INF_RECT;
                this.X1 = Globals.FZ_MAX_INF_RECT;
                this.Y1 = Globals.FZ_MAX_INF_RECT;
            }
            else if (r.IsEmpty)
            {
                return this;
            } else if (this.IsEmpty)
            {
                this.X0 = r.X0;
                this.Y0 = r.Y0;
                this.X1 = r.X1;
                this.Y1 = r.Y1;
            } else
            {
                Rect r = Utils.UtilUnionRect(this, r);
                this.X0 = r.X0;
                this.Y0 = r.Y0;
                this.X1 = r.X1;
                this.Y1 = r.Y1;
            }
        }*/

        public bool IsValid()
        {
            return X0 <= X1 && Y0 <= Y1;
        }

        /*public Quad Morph(Point p, Matrix m)
        {
            if (this.IsInfinite)
            {
                return INFINITE_QUAD();
            }
            return this.quad.morph(p, m);
        }

        public float Norm()
        {
            return Math.Sqrt(sum([c * c for c in self]))
        }

        public Normalize()
        {
            if (X1 < X0)
            {
                float tmp = X0;
                X0 = X1;
                X1 = tmp;
            }
            if(Y1 < Y0)
            {
                float tmp = Y0;
                Y0 = Y1;
                Y1 = tmp;
            }
        }

        public IRect Round()
        {
            return new IRect(Utils.UtilRoundRect())
        }

        public Rect Transform(Matrix matrix)
        {
            (X0, Y0, X1, Y1) = UtilTransformRect(this, matrix);
            return this;
        }*/
    }
    
}
