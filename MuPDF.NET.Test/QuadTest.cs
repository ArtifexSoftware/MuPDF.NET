using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class QuadTest
    {
        [Test]
        public void QuadCalc()
        {
            string text = " angle 327";
            Document doc = new Document("../../../resources/quad-calc-0.pdf");
            Page page = doc[0];

            Block block = (page.GetText("dict", flags: 0) as PageInfo).Blocks[0];
            Line line = block.Lines[0];

            Quad lineQuad = Utils.RecoverLineQuad(line, spans: line.Spans.Slice(line.Spans.Count - 1, 1));

            List<Quad> rl = page.SearchFor(text, quads: true);
            Quad searchQuad = rl[0];

            Assert.That((searchQuad.UpperLeft - lineQuad.UpperLeft).Abs(), Is.LessThan(1e4));
            Assert.That((searchQuad.UpperRight - lineQuad.UpperRight).Abs(), Is.LessThan(1e4));
            Assert.That((searchQuad.LowerLeft - lineQuad.LowerLeft).Abs(), Is.LessThan(1e4));
            Assert.That((searchQuad.LowerRight - lineQuad.LowerRight).Abs(), Is.LessThan(1e4));
        }

        [Test]
        public void Issue151_Circle()
        {
            Document doc = new Document("../../../resources/151/drawing.pdf");
            Page page = doc[0];
            List<PathInfo> paths = page.GetDrawings();

            doc.Close();

            Assert.That(paths[0].Items[0].Type, Is.EqualTo("c"));
            Assert.That(paths[0].Items[0].P1.X, Is.EqualTo(50));
            Assert.That(paths[0].Items[0].P1.Y, Is.EqualTo(100));
            Assert.That(paths[0].Items[0].LastPoint.X, Is.EqualTo(100));
            Assert.That(paths[0].Items[0].LastPoint.Y, Is.EqualTo(150));

            Document doc1 = new Document("../../../resources/151/drawing.pdf");
            Page page1 = doc1[0];
            List<PathInfo> paths1 = page1.GetDrawings();

            doc1.Close();

            Assert.That(paths1[0].Items.Count, Is.EqualTo(4));
        }
    }
}
