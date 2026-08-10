using System.Collections.Generic;
using MuPDF.NET.PDF4LLM;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestTablulate
    {
        private const string TestClassName = nameof(TestTablulate);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        [Fact]
        public void test_tablulate_bug()
        {
            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                // layout package.use_layout(True) # default in Python when layout bridge is available
                MuPDF4LLM.SetUseLayout(true);
                string path = Doc("test_tablulate_bug.pdf");

                string pageList = MuPDF4LLM.ToText(
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
                MuPDF4LLM.SetUseLayout(prior);
            }
        }
    }
}