using mupdf;
using static System.Net.Mime.MediaTypeNames;

namespace MuPDF.NET
{

    public class Point
    {

        static Point()
        {
            Utils.InitApp();
        }

        public float X { get; set; }
        public float Y { get; set; }
        public int Length { get; } = 2;

        public Point()
        {
            X = 0.0f;
            Y = 0.0f;
        }
        public Point(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public Point(FzPoint p)
        {
            this.X = p.x;
            this.Y = p.y;
        }

        public Point(Point p)
        {
            X = p.X;
            Y = p.Y;
        }

        public FzPoint ToFzPoint()
        {
            return new FzPoint(X, Y);
        }

        public float Abs()
        {
            return (float)Math.Sqrt(X * X + Y * Y);
        }

        public static Point operator +(Point p, float t)
        {
            return new Point(p.X + t, p.Y + t);
        }

        public static Point operator +(Point p, double t)
        {
            return new Point((float)(p.X + t), (float)(p.Y + (float)t));
        }

        public static Point operator +(Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }

        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return this.X;
                    case 1: return this.Y;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (i)
                {
                    case 0: this.X = value; break;
                    case 1: this.Y = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public static Point operator *(Point p, float m)
        {
            return new Point(p.X * m, p.Y * m);
        }

        public static Point operator *(Point p, double m)
        {
            return new Point((float)(p.X * m), (float)(p.Y * m));
        }

        public static Point operator *(Point op1, Matrix m)
        {
            Point op = new Point(op1);
            return op.Transform(m);
        }

        public static Point operator /(Point p, float m)
        {
            return new Point(p.X / m, p.Y / m);
        }

        public static Point operator -(Point p)
        {
            return new Point(-p.X, -p.Y);
        }

        public bool IsZero()
        {
            return X == 0 && Y == 0;
        }

        // set time

        public static Point operator -(Point p, float t)
        {
            return new Point(p.X - t, p.Y - t);
        }

        public static Point operator -(Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        public Point Position()
        {
            return new Point(X, Y);
        }

        public static FzPoint toFzPoint(Point p)
        {
            return new FzPoint(p.X, p.Y);
        }

        public Point TrueDivide(float m)
        {
            return new Point((float)(this.X * 1.0 / m), (float)(this.Y * 1.0 / m));
        }

        public Point TrueDivide(Matrix m)
        {
            (int, Matrix) result = Utils.InvertMatrix(m); // util

            if (result.Item1 == 0)
            {
                throw new DivideByZeroException("Matrix not invertible");
            }

            Point p = new Point(this.X, this.Y);
            return p.Transform(result.Item2);
        }

        public Point Transform(Matrix m)
        {
            FzPoint p = mupdf.mupdf.fz_transform_point(ToFzPoint(), m.ToFzMatrix());
            return new Point(p);
        }

        public float DistanceTo(Point p, string unit = "px")
        {
            Dictionary<string, (float, float)> u = new Dictionary<string, (float, float)>
            {
                {"px", (1.0f, 1.0f) },
                {"in", (1.0f, 72.0f) },
                {"cm", (2.54f, 72.0f) },
                {"mm", (25.4f, 72.0f) }
            };
            float f = u[unit].Item1 / u[unit].Item2;
            return (this - p).Abs() * f;
        }

        public float DistanceTo(Rect rect, string unit = "px")
        {
            Dictionary<string, (float, float)> u = new Dictionary<string, (float, float)>
            {
                {"px", (1.0f, 1.0f) },
                {"in", (1.0f, 72.0f) },
                {"cm", (2.54f, 72.0f) },
                {"mm", (25.4f, 72.0f) }
            };
            float f = u[unit].Item1 / u[unit].Item2;

            Rect r = new Rect(rect.TopLeft, rect.TopLeft);
            r = r | rect.BottomRight;
            if ((r.X0 < X && r.X1 > X) && (r.Y0 < Y && r.Y1 > Y))
                return 0.0f;
            if (X > r.X1)
            {
                if (Y >= r.Y1)
                    return DistanceTo(r.BottomRight, unit);
                else if (Y <= r.Y0)
                    return DistanceTo(r.TopRight, unit);
                else
                    return (X - r.X1) * f;
            }
            else if (r.X0 <= X && X <= r.X1)
            {
                if (Y >= r.Y1)
                    return (Y - r.Y1) * f;
                else
                    return (r.Y0 - Y) * f;
            }
            else
            {
                if (Y >= r.Y1)
                    return DistanceTo(r.BottomLeft, unit);
                else if (Y <= r.Y0)
                    return DistanceTo(r.TopLeft, unit);
                else
                    return (r.X0- X) * f;
            }
        }

        /// <summary>
        /// Unit vector of the point.
        /// </summary>
        public Point Unit
        {
            get
            {
                float s = X * X + Y * Y;
                if (s < Utils.FLT_EPSILON)
                    return new Point(0, 0);
                s = (float)Math.Sqrt(s);
                return new Point(X / s, Y / s);
            }
        }

        public override string ToString()
        {
            return $"Point({X}, {Y})";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool EqualTo(Point obj)
        {
            if (obj == null)
                throw new NullReferenceException("is null object.");
            return X == obj.X && Y == obj.Y;
        }
    }
}
