using System.Collections.Generic;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
    public class TestTablulate
    {
        private const string TestClassName = nameof(TestTablulate);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        [Fact]
        public void test_tablulate_bug()
        {
            bool prior = Utils.LayoutEnabled;
            try
            {
                // layout package.use_layout(True) # default in Python when layout bridge is available
                Utils.UseLayout(true);
                string path = Doc("test_tablulate_bug.pdf");

                string pageList = Utils.ToText(
                    path,
                    pageChunks: true,
                    useOcr: false,
                    header: false,
                    footer: false,
                    pages: new List<int> { 0 },
                    showProgress: true);

                Assert.NotNull(pageList);
                Assert.NotEmpty(pageList);
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }
    }
}