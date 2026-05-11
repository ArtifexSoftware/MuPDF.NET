using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Colorspace class.
    /// </summary>
    public class ColorspaceTests
    {
        [Fact]
        public void Colorspace_RGB()
        {
            var cs = Colorspace.CsRGB;
            Assert.Equal(3, cs.N);
            Assert.Contains("RGB", cs.Name, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Colorspace_Gray()
        {
            var cs = Colorspace.CsGRAY;
            Assert.Equal(1, cs.N);
        }

        [Fact]
        public void Colorspace_CMYK()
        {
            var cs = Colorspace.CsCMYK;
            Assert.Equal(4, cs.N);
        }

        [Fact]
        public void Colorspace_NameNotEmpty()
        {
            var cs = Colorspace.CsRGB;
            Assert.False(string.IsNullOrEmpty(cs.Name));
        }
    }
}
