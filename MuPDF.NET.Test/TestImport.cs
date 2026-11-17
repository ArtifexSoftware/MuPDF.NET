using System;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>C# equivalent of MuPDF import smoke test.</summary>
    [Collection("MuPDF.NET native")]
    public class TestImport
    {
        [Fact]
        public void test_import()
        {
            Assert.NotNull(typeof(Document).Assembly);
            Assert.NotNull(typeof(Document));
            Assert.NotNull(typeof(Page));
            Assert.NotNull(typeof(Font));
            Assert.NotNull(typeof(Utils));

            using var doc = new Document();
            _ = doc.NewPage();
        }
    }
}
