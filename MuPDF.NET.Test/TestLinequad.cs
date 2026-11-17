using System;
using System.Collections.Generic;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{


    /// <remarks>
    /// Check approx. equality of search quads versus quads recovered from text extractions.
    /// Inputs: <c>TestDocuments/TestLinequad/</c>; outputs: <c>TestDocuments/_Output/TestLinequad/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestLinequad
    {
        private const string TestClassName = nameof(TestLinequad);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary>Regression test: quadcalc.</summary>
        [Fact]
        public void test_quadcalc()
        {
            string text = " angle 327";
            bool oldSkip = Tools.UnsetQuadCorrections(true);
            try
            {
                using var doc = new Document(Doc("quad-calc-0.pdf"));
                var page = doc[0];
                // This special page has one block with one line, and
                // its last span contains the searched text.
                var pageDict = (Dictionary<string, object>)page.GetText("dict", flags: 0);
                var blocks = (List<Dictionary<string, object>>)pageDict["blocks"];
                var block = blocks[0];
                var lines = (List<Dictionary<string, object>>)block["lines"];
                var line = lines[0];
                // compute quad of last span in line
                var spans = (List<Dictionary<string, object>>)line["spans"];
                var lineq = Utils.RecoverLineQuad(line, new List<Dictionary<string, object>> { spans[spans.Count - 1] });

                // let text search find the text returning quad coordinates
                var rl = page.SearchFor(text, quads: true);
                var searchq = rl[0];
                Console.WriteLine($"searchq={searchq}");
                Console.WriteLine($"lineq={lineq}");
                // 4llm changes skip_quad_corrections.
                Assert.True((searchq.UL - lineq.UL).Norm <= 4);
                Assert.True((searchq.UR - lineq.UR).Norm <= 4);
                Assert.True((searchq.LL - lineq.LL).Norm <= 4);
                Assert.True((searchq.LR - lineq.LR).Norm <= 4);
            }
            finally
            {
                Tools.UnsetQuadCorrections(oldSkip);
            }
        }
    }
}
