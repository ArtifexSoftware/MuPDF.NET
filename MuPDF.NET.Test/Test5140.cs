using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test5140
    {
        [Fact]
        public void test_5140()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.NewPage();
            doc.SetPageLabels(
                new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["startpage"] = 1,
                        ["prefix"] = "",
                        ["firstpagenum"] = 1,
                        ["style"] = "D",
                    },
                });
            Assert.Equal("", doc[0].GetLabel()); // IndexError: list index out of range
            Assert.Equal("1", doc[1].GetLabel());
        }
    }
}
