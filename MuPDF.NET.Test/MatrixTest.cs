using MuPDF.NET;
using NUnit.Framework;

namespace MuPDF.NET.Test
{
    public class MatrixTest
    {
        public Matrix m1;

        public Matrix m2;

        [SetUp]
        public void Setup()
        {
            m1 = new Matrix(1, 2, 3, 4, 5, 6);
            m2 = new Matrix(2, 3, 4, 5, 6, 7);
        }

        [Test]
        public void Constructor()
        {
            m1 = new Matrix();
            Assert.That(m1[0], Is.EqualTo(0.0f));

            m1 = new Matrix(m2);
            Assert.That(m1[1], Is.EqualTo(m2[1]));

            m1 = new Matrix(3.0f, 17.0f);
            Assert.That(m1[3], Is.EqualTo(17.0f));
            Assert.That(m1[1], Is.EqualTo(0.0f));
        }

        [Test]
        public void Addition()
        {
            Matrix m = m1 + m2;
            Assert.That(m[4], Is.EqualTo(11.0f));
        }

        [Test]
        public void PreScale()
        {
            m1.Prescale(1, 2);
            Assert.That(m1[3], Is.EqualTo(8.0f));
        }

        [Test]
        public void Invert()
        {
            m2.Invert(m1);
            Assert.That(m2[5], Is.EqualTo(-2.0f));
        }
    }
}
