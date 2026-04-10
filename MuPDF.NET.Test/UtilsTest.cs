using NUnit.Framework;
using MuPDF.NET;

namespace MuPDF.NET.Test
{
    public class UtilsTest
    {
        [Test]
        public void FloatToString_NoScientificNotation()
        {
            Assert.That(Utils.FloatToString(1.5f), Is.EqualTo("1.5"));
            Assert.That(Utils.FloatToString(0f), Is.EqualTo("0"));
            Assert.That(Utils.FloatToString(-123.456f), Is.EqualTo("-123.456"));
            Assert.That(Utils.FloatToString(1000000f), Is.EqualTo("1000000"));

            // Values that would use scientific notation with default ToString
            string small = Utils.FloatToString(0.0000123f);
            Assert.That(small.Contains("E") || small.Contains("e"), Is.False, "Should not use scientific notation");
            Assert.That(small.Contains("0.00001") || small.Contains("0.000012"), Is.True);
        }

        [Test]
        public void FloatToString_InvariantCulture()
        {
            string result = Utils.FloatToString(1.5f);
            Assert.That(result.Contains("."), Is.True);
            Assert.That(result.Contains(","), Is.False);
        }

        [Test]
        public void DoubleToString_NoScientificNotation()
        {
            Assert.That(Utils.DoubleToString(1.5), Is.EqualTo("1.5"));
            Assert.That(Utils.DoubleToString(0), Is.EqualTo("0"));
            Assert.That(Utils.DoubleToString(-123.456), Is.EqualTo("-123.456"));
            Assert.That(Utils.DoubleToString(1000000), Is.EqualTo("1000000"));

            string small = Utils.DoubleToString(1.23e-10);
            Assert.That(small.Contains("E") || small.Contains("e"), Is.False, "Should not use scientific notation");
        }

        [Test]
        public void DoubleToString_InvariantCulture()
        {
            string result = Utils.DoubleToString(1.5);
            Assert.That(result.Contains("."), Is.True);
            Assert.That(result.Contains(","), Is.False);
        }
    }
}
