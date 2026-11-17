using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test5220
    {
        [Fact]
        public void test_5220_texttrace_bbox_uses_y_scale()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var origin = new Point(72, 72);
            const float fontSize = 10f;
            page.InsertText(
                origin,
                "H",
                fontSize: fontSize,
                morph: new Morph(origin, new Matrix(3, 0, 0, 1, 0, 0)));
            var span = page.GetTextTrace()[0];
            Assert.NotNull(span.Bbox);
            float height = span.Bbox.Y1 - span.Bbox.Y0;
            // X-scale is 3, Y-scale is 1. Height must follow Y (≈ fontSize), not X (≈ 3×fontSize).
            Assert.InRange(height, fontSize * 0.5f, fontSize * 1.6f);
            Assert.True(height < fontSize * 2f, $"bbox height {height} used X-scale instead of Y-scale");
        }
    }
}
