using MuPDF.NET;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class MatrixTest
    {
        private const string TestClassName = nameof(MatrixTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        public Matrix m1;

        public Matrix m2;

        public MatrixTest()
        {
            m1 = new Matrix(1, 2, 3, 4, 5, 6);
            m2 = new Matrix(2, 3, 4, 5, 6, 7);
        }

        [Fact]
        public void Constructor()
        {
            m1 = new Matrix();
            Assert.Equal(0.0f, m1[0]);

            m1 = new Matrix(m2);
            Assert.Equal(m2[1], m1[1]);

            m1 = new Matrix(3.0f, 17.0f);
            Assert.Equal(17.0f, m1[3]);
            Assert.Equal(0.0f, m1[1]);
        }

        [Fact]
        public void Addition()
        {
            Matrix m = m1 + m2;
            Assert.Equal(11.0f, m[4]);
        }

        [Fact]
        public void PreScale()
        {
            m1.Prescale(1, 2);
            Assert.Equal(8.0f, m1[3]);
        }

        [Fact]
        public void Invert()
        {
            m2.Invert(m1);
            Assert.Equal(-2.0f, m2[5]);
        }
    }
}
