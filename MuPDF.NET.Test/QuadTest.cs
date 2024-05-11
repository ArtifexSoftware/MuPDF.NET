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
            MuPDFDocument doc = new MuPDFDocument("../../../resources/quad-calc-0.pdf");
            MuPDFPage page = doc[0];

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
    }
}
