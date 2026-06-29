using System;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_page_links.py</c>.
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestPageLinks/</c>; outputs: <c>TestDocuments/_Output/TestPageLinks/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestPageLinks
    {
        private const string TestClassName = nameof(TestPageLinks);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_page_links_generator()
        {
            using var doc = new Document(Doc("2.pdf"));

            // page = doc[-1]
            var page = doc[doc.PageCount - 1];

            // link_generator = page.links()
            var linkGenerator = page.links();
            // links = list(link_generator)
            var links = linkGenerator.ToList();
            Assert.Equal(7, links.Count);
        }
    }
}
