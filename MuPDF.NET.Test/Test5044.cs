using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Input: <c>TestDocuments/Test5044/test_5044.pdf</c>.
    /// Issue 5044: TOC / named dest parsing.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class Test5044
    {
        private const string TestClassName = nameof(Test5044);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        [Fact]
        public void test_5044()
        {
            string fileIn = Doc("test_5044.pdf");
            using var doc = new Document(fileIn);
            var toc = doc.GetToc(simple: false);
            var link = toc[0].link;
            var expectLink = new Dictionary<string, object>
            {
                ["kind"] = 1,
                ["xref"] = 11,
                ["page"] = 0,
                ["to"] = new Point(0.0f, 0.89001465f),
                ["zoom"] = 0.0f,
                ["color"] = (0.0f, 0.0f, 0.0f),
            };

            Assert.Equal(expectLink["kind"], link["kind"]);
            Assert.Equal(expectLink["xref"], link["xref"]);
            Assert.Equal(expectLink["page"], link["page"]);
            Assert.Equal(expectLink["to"], link["to"]);
            Assert.Equal(expectLink["zoom"], link["zoom"]);
            Assert.Equal(expectLink["color"], link["color"]);
        }
    }
}
