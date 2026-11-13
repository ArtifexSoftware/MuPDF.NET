using System;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Input: <c>TestDocuments/Test4942/test_4942.pdf</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class Test4942
    {
        private const string TestClassName = nameof(Test4942);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        [Fact]
        public void test_4942()
        {
            string path = Doc("test_4942.pdf");
            using var document = new Document(path);

            var page = document[0];
            page.ClipToRect(page.Rect);
            var links = page.GetLinks();
            Assert.Equal(8, links.Count);
            foreach (var link in links)
            {
                Console.WriteLine(
                    $"kind={link.Kind} xref={link.Xref} page={link.Page} dest={link.Dest} nameddest={link.NamedDest} from={link.From} id={link.Id}");
                Assert.Equal(Constants.LinkNamed, (int)link.Kind);
                Assert.Equal(-1, link.Page);
                Assert.Equal("/XYZ null null null", link.Dest);
                Assert.False(string.IsNullOrEmpty(link.NamedDest));
                Assert.True(link.TryGetValue("dest", out var destVal));
                Assert.Equal("/XYZ null null null", destVal);
            }
        }
    }
}
