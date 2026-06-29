using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class QuadTest
    {
        private const string TestClassName = nameof(QuadTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void QuadCalc()
        {
            string text = " angle 327";
            Document doc = new Document(Doc("quad-calc-0.pdf"));
            Page page = doc[0];

            Block block = (page.GetText("dict", flags: 0) as PageInfo).Blocks[0];
            Line line = block.Lines[0];

            Quad lineQuad = Utils.RecoverLineQuad(line, spans: line.Spans.Slice(line.Spans.Count - 1, 1));

            List<Quad> rl = page.SearchFor(text, quads: true);
            Quad searchQuad = rl[0];

            Assert.True((searchQuad.UpperLeft - lineQuad.UpperLeft).Abs() < 1e4);
            Assert.True((searchQuad.UpperRight - lineQuad.UpperRight).Abs() < 1e4);
            Assert.True((searchQuad.LowerLeft - lineQuad.LowerLeft).Abs() < 1e4);
            Assert.True((searchQuad.LowerRight - lineQuad.LowerRight).Abs() < 1e4);
        }

        [Fact]
        public void Issue151_Circle()
        {
            Document doc = new Document(Doc("drawing.pdf"));
            Page page = doc[0];
            List<PathInfo> paths = page.GetDrawings();

            doc.Close();

            Assert.Equal("c", paths[0].Items[0].Type);
            Assert.Equal(50, paths[0].Items[0].P1.X);
            Assert.Equal(100, paths[0].Items[0].P1.Y);
            Assert.Equal(100, paths[0].Items[0].LastPoint.X);
            Assert.Equal(150, paths[0].Items[0].LastPoint.Y);

            Document doc1 = new Document(Doc("drawing.pdf"));
            Page page1 = doc1[0];
            List<PathInfo> paths1 = page1.GetDrawings();

            doc1.Close();

            Assert.Equal(4, paths1[0].Items.Count);
        }
    }
}
