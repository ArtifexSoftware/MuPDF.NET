using mupdf;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static mupdf.FzBandWriter;

namespace CSharpMuPDF
{
    public class IRect
    {
        public float X0 { get; set; }

        public float Y0 { get; set; }

        public float X1 { get; set; }

        public float Y1 { get; set; }

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

        public int LENGTH
        {
            get { return 4; }
        }

        public IRect()
        {
            X0 = Y0 = X1 = Y1 = 0;
        }

        public IRect(Point ul, float x1, float y1)
        {
            X0 = ul.X;
            Y0 = ul.Y;
            X1 = x1;
            Y1 = y1;
        }

        public IRect(float x0, float y0, Point br)
        {
            X0 = x0;
            Y0 = y0;
            X1 = br.X;
            Y1 = br.Y;
        }

        public bool IsInfinite
        {
            get
            {
                return (X0 == Utils.FZ_MIN_INF_RECT) && (Y0 == Utils.FZ_MIN_INF_RECT) && (X1 == Utils.FZ_MAX_INF_RECT ) && (Y1 == Utils.FZ_MAX_INF_RECT);
            }
        }

        public bool IsValid
        {
            get
            {
                return X0 <= X1 && Y0 <= Y1;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return X0 >= X1 && Y0 >= Y1;
            }
        }

        public Quad QUAD
        {
            get
            {
                return new Quad(TopLeft, TopRight, BottomLeft, BottomRight);
            }
        }

        public Rect RECT
        {
            get
            {
                return new Rect(this);
            }
        }

        public IRect(Point ul, Point br)
        {
            X0 = ul.X;
            Y0 = ul.Y;
            X1 = br.X;
            Y1 = br.Y;
        }

        public IRect(float x0, float y0, float x1, float y1)
        {
            X0 = x0;
            Y0 = y0;
            X1 = x1;
            Y1 = y1;
        }

        public IRect(Rect r)
        {
            X0 = r.X0;
            Y0 = r.Y0;
            X1 = r.X1;
            Y1 = r.Y1;
        }

        public IRect(FzIrect ir)
        {
            X0 = ir.x0;
            Y0 = ir.y0;
            X1 = ir.x1;
            Y1 = ir.y1;
        }

        public Rect ToRect()
        {
            return new Rect(X0, Y0, X1, Y1);
        }

        public IRect Round()
        {
            return new IRect((new Rect(this)).ToFzRect().fz_round_rect());
        }

        public static IRect operator +(IRect left, IRect right)
        {
            Rect r = new Rect(left) + new Rect(right);
            return (new IRect(r)).Round();
        }

        public static IRect operator &(IRect left, IRect right)
        {
            Rect left_rect = new Rect(left);
            Rect right_rect = new Rect(right);

            return new IRect(left_rect & right_rect);
        }

        public static bool Contains(IRect left, IRect right)
        {
            Rect left_rect = new Rect(left);
            Rect right_rect = new Rect(right);

            return left_rect.Contains(right_rect);
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

        public static IRect operator *(IRect left, float right)
        {
            return new IRect(left.ToRect() * right).Round();
        }

        public static IRect operator -(IRect op)
        {
            return new IRect(-op.X0, -op.Y0, -op.X1, -op.Y1);
        }

        public static IRect operator |(IRect left, IRect right)
        {
            return new IRect(left.ToRect() | right.ToRect());
        }

        public static IRect operator +(IRect op)
        {
            return op;
        }

        public override string ToString()
        {
            return "IRect";
        }

        public static IRect operator -(IRect left, IRect right)
        {
            return new IRect(left.ToRect() - right.ToRect());
        }

        public IRect IncludePoint(Point p)
        {
            return new IRect(new Rect(this).IncludePoint(p));
        }

        public IRect IncludeRect(Rect r)
        {
            return new IRect(new Rect(this).IncludeRect(r));
        }

        public Rect Intersect(Rect r)
        {
            return ToRect().Intersect(r);
        }

        public bool Intersects(Rect r)
        {
            return ToRect().Intersects(r);
        }

        public Quad Morph(Point p, Matrix m)
        {
            if (IsInfinite)
                return Utils.INFINITE_RECT().QUAD;
            return QUAD.Morph(p, m);
        }

        public float Norm()
        {
            float ret = 0.0f;
            for (int i = 0; i < this.LENGTH; i++)
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

        public Matrix ToRect(Rect r)
        {
            if (IsInfinite || IsEmpty || r.IsInfinite || r.IsEmpty)
                throw new Exception("rectangles must be finite and not empty");
            return new Matrix(1, 0, 0, 1, -X0, Y0) * new Matrix(r.Width / Width, r.Height / Height) * new Matrix(1, 0, 0, 1, r.X0, r.Y0);
        }

        public Rect Trnasform(Matrix m)
        {
            return new Rect(this).Transform(m);
        }
    }
}
