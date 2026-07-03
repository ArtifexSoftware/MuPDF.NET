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
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PYODIDE_ROOT")))
            {
                Console.WriteLine("test_import(): not running on Pyodide - cannot run child processes.");
                return;
            }

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
