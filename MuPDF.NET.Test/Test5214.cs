using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test5214
    {
        [Fact]
        public void test_5214_read()
        {
            using var src = new Document();
            for (int i = 0; i < 4; i++)
                src.NewPage();
            string nums =
                "[0<</S/D/P(Part 1/2-)>> 1<</S/D/P(Sec \\(1\\)-)>>" +
                " 2<</S/r/P(Anh\\344-)>> 3<</S/D/P<FEFF03A9002D>>>]";
            src.XrefSetKey(src.PdfCatalog, "PageLabels", $"<</Nums{nums}>>");
            using var doc = new Document(src.ToBytes(), "pdf");
            string[] expected = { "Part 1/2-1", "Sec (1)-1", "Anhä-i", "Ω-1" };
            var got = new List<string>();
            for (int i = 0; i < 4; i++)
                got.Add(doc[i].GetLabel());
            var prefixes = new List<string>();
            foreach (var d in doc.GetPageLabels())
                prefixes.Add(d["prefix"]?.ToString() ?? "");
            string[] expectedPrefixes =
            {
                "Part 1/2-",
                "Sec (1)-",
                "Anhä-",
                "Ω-",
            };
            Assert.Equal(expected, got);
            Assert.Equal(expectedPrefixes, prefixes);
        }

        [Fact]
        public void test_5214_write()
        {
            using var src = new Document();
            for (int i = 0; i < 2; i++)
                src.NewPage();
            src.SetPageLabels(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["startpage"] = 0,
                    ["prefix"] = "A/B-",
                    ["style"] = "D",
                },
                new Dictionary<string, object>
                {
                    ["startpage"] = 1,
                    ["prefix"] = "Anh. ä-",
                    ["style"] = "D",
                },
            });
            using var doc = new Document(src.ToBytes(), "pdf");
            Assert.Equal(new[] { "A/B-1", "Anh. ä-1" }, new[] { doc[0].GetLabel(), doc[1].GetLabel() });
        }
    }
}
