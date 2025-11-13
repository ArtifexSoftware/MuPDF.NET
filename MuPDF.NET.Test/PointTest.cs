using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MuPDF.NET.Test
{
    public class PointTest
    {
        public Point op1;

        public Point op2;

        [SetUp]
        public void Setup()
        {
            op1 = new Point(0, 0);
            op2 = new Point(3, 5);
        }

        [Test]
        public void Contructor()
        {
            Point t1 = new Point(0, 0);
            t1 = new Point(op1);
            t1 = new Point();

            FzPoint p = new FzPoint();
            Point t2 = new Point(p);
            Assert.That(t1.X, Is.EqualTo(t2.X));
        }

        [Test]
        public void Addition()
        {
            Point t = op1 + op2;
            Assert.IsTrue(t.X.Equals(3.0f));
            Assert.IsTrue(t.Y.Equals(5.0f));

            t = op1 + 3.0f;
            Assert.IsTrue(t.X.Equals(t.Y));
        }

        [Test]
        public void Subtraction()
        {
            Point t = op1 - op2;
            Assert.IsTrue(t.X.Equals(-3f));
            Assert.IsTrue(t.Y.Equals(-5f));
        }

        [Test]
        public void Transform()
        {
            Matrix m = new Matrix(0, 0, 0, 1, 1, 1);
            Point t = op2.Transform(m);
            Assert.IsTrue(t[0].Equals(1f));
            Assert.IsTrue(t.Y.Equals(6f));
        }

        [Test]
        public void Abs()
        {
            float abs = new Point(3.0f, 4.0f).Abs();
            Assert.That(abs, Is.EqualTo(5.0f));
        }

        [Test]
        public void TrueDivide()
        {
            Point t = op1.TrueDivide(3.0f);
            //Assert.Pass();
        }
    }
}
