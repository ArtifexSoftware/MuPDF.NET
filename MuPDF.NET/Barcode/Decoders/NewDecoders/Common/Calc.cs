using System;

namespace BarcodeReader.Core.Common
{
    class Calc
    {
        public static float Modul(MyPoint a, MyPoint b) { return Modul(new MyPoint(a.X - b.X, a.Y - b.Y)); }
        public static float Modul(MyPoint a) { return (float) Math.Sqrt(a.X * a.X + a.Y * a.Y); }

        //middle point between a bresenham line from a to b. When the middle is 0.5 adjust to a.
        //IMPORTANT: Middle(a,b) can be != Middle(b,a), if division by 2 is not exact. In such
        //a case, adjust the middle closer to a.
        public static MyPoint Middle(MyPoint a, MyPoint b) { return new MyPoint(Middle(a.X, b.X), Middle(a.Y, b.Y)); }
        public static int Middle(int a, int b)
        {
            int c = a + b;
            if (c % 2 == 0) return c / 2;
            return c / 2 + (b > a ? 0 : 1);
        }
        public static bool Around(float f, float v) { return Around(f, v, 0.15F); }
        public static bool Around(float f, float v, float epsilon) { return f > v - epsilon && f < v + epsilon; }
        public static bool Around(int f, int v, int epsilon) { return f > v - epsilon && f < v + epsilon; }
    }
}
