using SkiaSharp;
using System;
using System.Drawing;

namespace BarcodeReader.Core
{
    // Represents a vector with integer coordinates
#if CORE_DEV
    public
#else
    internal
#endif
    struct MyVector
    {
        public int X;

        public int Y;

        public static readonly MyVector Zero = new MyVector(0, 0);

        public MyVector(int x, int y)
        {
            X = x;
            Y = y;
        }

        // length^2
        public float LengthSq
        {
            get
            {
                return (float)X*X + Y*Y;
            }
        }

        // length
        public float Length
        {
            get
            {
                return (float)Math.Sqrt(LengthSq);
            }
        }

        public float Angle
        {
            get
            {
                return (float)Math.Atan2(Y, X);
            }
        }

        // returns another vector which is perpendicular to this one
        public MyVector Perpendicular
        {
            get
            {
                return new MyVector(Y, -X);
            }
        }

        public MyVector NormInt
        {
            get
            {
                int x = X > 0 ? X : -X;
                int y = Y > 0 ? Y : -Y;
                if (x > y) return new MyVector(X>0?1:-1, 0);
                return new MyVector(0, Y>0?1:-1);
            }
        }

        public void swap(ref MyVector v)
        {
            int x = X, y = Y;
            X = v.X; Y = v.Y;
            v.X = x; v.Y = y;
        }

        public bool isHorizontal()
        {
            if (X > 0)
                if (Y > 0) return X > Y;
                else return X>-Y;
            else
                if (Y > 0) return -X > Y;
                else return -X>-Y;
        }

        public static bool operator ==(MyVector a, MyVector b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(MyVector a, MyVector b)
        {
            return !(a.X == b.X && a.Y == b.Y);
        }

        public static MyVector operator + (MyVector a, MyVector b)
        {
            return new MyVector(a.X + b.X, a.Y + b.Y);
        }

        public static MyVector operator - (MyVector a, MyVector b)
        {
            return new MyVector(a.X - b.X, a.Y - b.Y);
        }

        // scalar multiply
        public static MyVector operator *(int i, MyVector a)
        {
            return new MyVector(a.X * i, a.Y * i);
        }

        public static MyPoint operator +(MyPoint p, MyVector a)
        {
            return new MyPoint(p.X + a.X, p.Y + a.Y);
        }

        public static MyPoint operator -(MyPoint p, MyVector a)
        {
            return new MyPoint(p.X - a.X, p.Y - a.Y);
        }

        public static MyVectorF operator /(MyVector a, float d)
        {
            return new MyVectorF(a.X / d, a.Y / d);
        }

        public static float operator *(MyVector a, MyVector b)
        {
            return (float)(a.X*b.X+a.Y*b.Y);
        }

        public static float crossProduct(MyVector a, MyVector b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public static implicit operator MyVectorF(MyVector vector)
        {
            return new MyVectorF(vector.X, vector.Y);
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }

    	public bool Equals(MyVector other)
    	{
    		return other.X == X && other.Y == Y;
    	}

    	public override bool Equals(object obj)
    	{
    		if (ReferenceEquals(null, obj)) return false;
    		if (obj.GetType() != typeof(MyVector)) return false;
    		return Equals((MyVector) obj);
    	}

    	public override int GetHashCode()
    	{
    		unchecked
    		{
    			return (X * 397) ^ Y;
    		}
    	}
    }

    // Represents a vector with fractional coordinates
#if CORE_DEV
    public
#else
    internal
#endif
    struct MyVectorF
    {
        public float X;

        public float Y;

        public MyVectorF(float x, float y)
        {
            X = x;
            Y = y;
        }

        // length^2
        public float LengthSq
        {
            get
            {
                return X * X + Y * Y;
            }
        }

        // length
        public float Length
        {
            get
            {
                return (float)Math.Sqrt(LengthSq);
            }
        }

        /// <summary>
        /// Angle from (-PI ; PI]
        /// </summary>
        public float Angle
        {
            get
            {
                return (float)Math.Atan2(Y, X);
            }
        }

        private const float PI = 3.14159f;

        /// <summary>
        /// Minimal angle between vectors
        /// </summary>
        public float AngleWith(MyVectorF c2)
        {
            var a = Angle - c2.Angle;
            a += (a > PI) ? -2 * PI : (a < -PI) ? 2 * PI : 0;

            return a;
        }

        /// <summary>
        /// Normal from the line to the point (the line is defined by normalized direction)
        /// </summary>
        public MyVectorF NormalFromLine(MyVectorF normalizedLineDirection)
        {
            var cosAngle = X * normalizedLineDirection.X + Y * normalizedLineDirection.Y;
            var x = normalizedLineDirection.X * cosAngle;
            var y = normalizedLineDirection.Y * cosAngle;
            return new MyVectorF(X - x, Y - y);
        }

        // returns a vector which points the same direction but has a length of 1
        public MyVectorF Normalized
        {
            get
            {
                return this / Length;
            }
        }

        public MyVectorF Perpendicular
        {
            get
            {
                return new MyVectorF(Y, -X);
            }
        }

        public MyVectorF Rotate(float angle)
        {
            float ca = (float)Math.Cos(angle);
            float sa = (float)Math.Sin(angle);
            return new MyVectorF(X * ca - Y * sa, X * sa + Y * ca);
        }

        public static MyVectorF operator +(MyVectorF a, MyVectorF b)
        {
            return new MyVectorF(a.X + b.X, a.Y + b.Y);
        }

        public static MyVectorF operator -(MyVectorF a, MyVectorF b)
        {
            return new MyVectorF(a.X - b.X, a.Y - b.Y);
        }

        public static MyVectorF operator -(MyVectorF v)
        {
            return new MyVectorF(-v.X, -v.Y);
        }

        // scalar multiply
        public static MyVectorF operator *(MyVectorF a, float d)
        {
            return new MyVectorF(a.X * d, a.Y * d);
        }

        public static MyPointF operator +(MyPointF p, MyVectorF a)
        {
            return new MyPointF(p.X + a.X, p.Y + a.Y);
        }

        public static MyPointF operator -(MyPointF p, MyVectorF a)
        {
            return new MyPointF(p.X - a.X, p.Y - a.Y);
        }

        public static MyVectorF operator /(MyVectorF a, float d)
        {
            return new MyVectorF(a.X / d, a.Y / d);
        }

        // dot product
        public static float operator * (MyVectorF a, MyVectorF b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static float crossProduct(MyVectorF a, MyVectorF b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public static explicit operator MyVector(MyVectorF vectorF)
        {
            return new MyVector((int)Math.Round(vectorF.X), (int)Math.Round(vectorF.Y));
        }

        public bool isNaN() { return float.IsNaN(X) || float.IsNaN(Y); }

        public override string ToString()
        {
            return string.Format("({0}:{1})", X.ToString("F2"), Y.ToString("F2"));
        }
    }

    // Represents a point with fractional coordinates
#if CORE_DEV
    public
#else
    internal
#endif
    struct MyPointF
    {
        private bool empty;

        public float X;

        public float Y;

        public bool IsEmpty
        {
            get
            {
                return empty || float.IsNaN(X) || float.IsNaN(Y);
            }
        }

        public bool IsInfinity
        {
            get
            {

                if (float.IsNaN(X) || float.IsInfinity(X) || float.IsNaN(Y) || float.IsInfinity(Y))
                    return true;
                return false;
            }
        }

        public static MyPointF Empty
        {
            get
            {
                MyPointF e = new MyPointF();
                e.empty = true;
                return e;
            }
        }

        public MyPointF(float x, float y)
        {
            X = x;
            Y = y;
            empty = false;
        }

        public void Add(MyVectorF vector)
        {
            X += vector.X;
            Y += vector.Y;
        }

        public void swap(ref MyPointF v)
        {
            float x = X, y = Y;
            X = v.X; Y = v.Y;
            v.X = x; v.Y = y;
        }

        public static MyPointF Lerp(MyPointF a, MyPointF b, float k)
        {
            return a * (1 - k) + b * k;
        }

        public static MyVectorF operator -(MyPointF a, MyPointF b)
        {
            return new MyVectorF(a.X - b.X, a.Y - b.Y);
        }

        public static MyPointF operator +(MyPointF a, MyPointF b)
        {
            return new MyPointF(a.X + b.X, a.Y + b.Y);
        }

        public static MyPointF operator *(MyPointF a, float b)
        {
            return new MyPointF(a.X * b, a.Y * b);
        }

        public static MyPointF operator /(MyPointF a, float b)
        {
            return new MyPointF(a.X / b, a.Y / b);
        }

        public static implicit operator MyPoint(MyPointF pointF)
        {
            return new MyPoint((int)Math.Round(pointF.X), (int)Math.Round(pointF.Y));
        }

        public MyPoint Truncate()
        {
            return new MyPoint((int)Math.Truncate(X), (int)Math.Truncate(Y));
        }

        public static implicit operator MyPointF(MyVectorF p)
        {
            return new MyPointF(p.X, p.Y);
        }

        public static implicit operator MyPointF(SKPointI p)
        {
            return new MyPointF((float)p.X, (float)p.Y);
        }

        public static implicit operator SKPointI(MyPointF p)
        {
            return new SKPointI((int)Math.Round(p.X), (int)Math.Round(p.Y));
        }

        public override string ToString()
        {
            return string.Format("({0}:{1})", X.ToString("F2"), Y.ToString("F2"));
        }
    }

    // Represents a point with integer coordinates
#if CORE_DEV
    public
#else
    internal
#endif
    struct MyPoint : IEquatable<MyPoint>
    {
        private bool empty;

        public int X;

        public int Y;

        public bool IsEmpty
        {
            get
            {
                return empty;
            }
        }

        public static MyPoint Empty
        {
            get
            {
                MyPoint e = new MyPoint();
                e.empty = true;
                return e;
            }
        }

        public MyPoint(int x, int y)
        {
            X = x;
            Y = y;
            empty = false;
        }

        public void swap(ref MyPoint v)
        {
            int x = X, y = Y;
            X = v.X; Y = v.Y;
            v.X = x; v.Y = y;
        }

        public static MyPoint operator +(MyPoint a, MyPoint b)
        {
            return new MyPoint(a.X + b.X, a.Y + b.Y);
        }

        public static MyVector operator -(MyPoint a, MyPoint b)
        {
            return new MyVector(a.X - b.X, a.Y - b.Y);
        }

        public static MyPoint operator *(MyPoint a, int d)
        {
            return new MyPoint(a.X * d, a.Y * d);
        }

        public static MyPoint operator /(MyPoint a, int d)
        {
            return new MyPoint(a.X / d, a.Y / d);
        }

        public static implicit operator MyPoint(SKPointI p)
        {
            return new MyPoint(p.X, p.Y);
        }

        public static implicit operator SKPointI(MyPoint p)
        {
            return new SKPointI(p.X, p.Y);
        }

        public static implicit operator MyPointF(MyPoint point)
        {
            if (point.IsEmpty)
            {
                return MyPointF.Empty;
            }
            return new MyPointF(point.X, point.Y);
        }

        public static bool operator ==(MyPoint a, MyPoint b)
        {
            if (a.IsEmpty != b.IsEmpty)
            {
                return false;
            }

            return (a.IsEmpty && b.IsEmpty) || a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(MyPoint a, MyPoint b)
        {
            if (a.IsEmpty && b.IsEmpty)
            {
                return false;
            }

            return (a.IsEmpty != b.IsEmpty) || a.X != b.X || a.Y != b.Y;
        }

        public override string ToString()
        {
            return empty ? "empty" : "(" + X + "," + Y + ")";
        }

        public bool Equals(MyPoint other)
        {
            return empty == other.empty && X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MyPoint && Equals((MyPoint) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = empty.GetHashCode();
                hashCode = (hashCode * 397) ^ X;
                hashCode = (hashCode * 397) ^ Y;
                return hashCode;
            }
        }
    }

}