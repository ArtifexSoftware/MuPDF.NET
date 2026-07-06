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
            // page = document[0]
            var page = document[0];
            // page.clip_to_rect(page.rect)
            page.ClipToRect(page.Rect);
            // page.get_links()
            var links = page.GetLinks();
            Assert.Equal(8, links.Count);
        }
    }
}
