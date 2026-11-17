using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{


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


            var page = doc[doc.PageCount - 1];

            var linkGenerator = page.links();
            var links = linkGenerator.ToList();
            Assert.Equal(7, links.Count);
        }
    }
}