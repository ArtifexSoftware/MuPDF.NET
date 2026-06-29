using Xunit;
using MuPDF.NET;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class UtilsTest
    {
        private const string TestClassName = nameof(UtilsTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void FloatToString_NoScientificNotation()
        {
            Assert.Equal("1.5", Utils.FloatToString(1.5f));
            Assert.Equal("0", Utils.FloatToString(0f));
            Assert.Equal("-123.456", Utils.FloatToString(-123.456f));
            Assert.Equal("1000000", Utils.FloatToString(1000000f));

            // Values that would use scientific notation with default ToString
            string small = Utils.FloatToString(0.0000123f);
            Assert.False(small.Contains("E") || small.Contains("e"), "Should not use scientific notation");
            Assert.True(small.Contains("0.00001") || small.Contains("0.000012"), "Should contain 0.00001 or 0.000012");
        }

        [Fact]
        public void FloatToString_InvariantCulture()
        {
            string result = Utils.FloatToString(1.5f);
            Assert.True(result.Contains("."), "Should contain .");
            Assert.False(result.Contains(","), "Should not contain ,");
        }

        [Fact]
        public void DoubleToString_NoScientificNotation()
        {
            Assert.Equal("1.5", Utils.DoubleToString(1.5));
            Assert.Equal("0", Utils.DoubleToString(0));
            Assert.Equal("-123.456", Utils.DoubleToString(-123.456));
            Assert.Equal("1000000", Utils.DoubleToString(1000000));

            string small = Utils.DoubleToString(1.23e-10);
            Assert.False(small.Contains("E") || small.Contains("e"), "Should not use scientific notation");
        }

        [Fact]
        public void DoubleToString_InvariantCulture()
        {
            string result = Utils.DoubleToString(1.5);
            Assert.True(result.Contains("."), "Should contain .");
            Assert.False(result.Contains(","), "Should not contain ,");
        }
    }
}
