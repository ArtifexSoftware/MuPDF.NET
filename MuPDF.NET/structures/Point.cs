using mupdf;

namespace MuPDF.NET
{

    public class Point
    {
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

        public static Point operator *(Point op1, Matrix m)
        {
            return op1.Transform(m);
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

        public bool EqualTo(Point obj)
        {
            return X == ((Point)obj).X && Y == ((Point)obj).Y;
        }
    }
}
