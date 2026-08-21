using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Input: <c>TestDocuments/Test5044/test_5044.pdf</c>.
    /// Port of PyMuPDF 1.28.2 <c>tests/test_5044.py</c> (#5044 TOC / named dest parsing).
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
                ["to"] = new Point(0.0f, 0.89001467f),
                ["zoom"] = 0.0f,
                ["color"] = new float[] { 0.0f, 0.0f, 0.0f },
            };

            Assert.Equal(expectLink["kind"], link["kind"]);
            Assert.Equal(expectLink["xref"], link["xref"]);
            Assert.Equal(expectLink["page"], link["page"]);
            Assert.Equal(expectLink["zoom"], Convert.ToSingle(link["zoom"]));

            Point to = link["to"] switch
            {
                Point pt => pt,
                ValueTuple<float, float> vt => new Point(vt.Item1, vt.Item2),
                _ => throw new InvalidCastException($"Unexpected 'to' type: {link["to"]?.GetType().FullName}"),
            };
            Assert.Equal(0.0f, to.X, 5);
            Assert.Equal(0.89001467f, to.Y, 5);

            float[] color = link["color"] switch
            {
                float[] fa => fa,
                ValueTuple<float, float, float> vt3 => new[] { vt3.Item1, vt3.Item2, vt3.Item3 },
                IEnumerable<object> objs => objs.Select(Convert.ToSingle).ToArray(),
                _ => throw new InvalidCastException($"Unexpected 'color' type: {link["color"]?.GetType().FullName}"),
            };
            Assert.Equal(new float[] { 0f, 0f, 0f }, color);
        }
    }
}
