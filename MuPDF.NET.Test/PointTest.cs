using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class PointTest
    {
        private const string TestClassName = nameof(PixmapTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        public Point op1;

        public Point op2;

        public PointTest()
        {
            op1 = new Point(0, 0);
            op2 = new Point(3, 5);
        }

        [Fact]
        public void Contructor()
        {
            Point t1 = new Point(0, 0);
            t1 = new Point(op1);
            t1 = new Point();

            FzPoint p = new FzPoint();
            Point t2 = new Point(p);
            Assert.Equal(t1.X, t2.X);
        }

        [Fact]
        public void Addition()
        {
            Point t = op1 + op2;
            Assert.Equal(3.0f, t.X);
            Assert.Equal(5.0f, t.Y);

            t = op1 + 3.0f;
            Assert.Equal(t.X, t.Y);
        }

        [Fact]
        public void Subtraction()
        {
            Point t = op1 - op2;
            Assert.Equal(-3f, t.X);
            Assert.Equal(-5f, t.Y);
        }

        [Fact]
        public void Transform()
        {
            Matrix m = new Matrix(0, 0, 0, 1, 1, 1);
            Point t = op2.Transform(m);
            Assert.Equal(1f, t[0]);
            Assert.Equal(6f, t.Y);
        }

        [Fact]
        public void Abs()
        {
            float abs = new Point(3.0f, 4.0f).Abs();
            Assert.Equal(5.0f, abs);
        }

        [Fact]
        public void TrueDivide()
        {
            Point t = op1.TrueDivide(3.0f);
            //Assert.Pass();
        }
    }
}
