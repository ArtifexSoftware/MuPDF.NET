using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Link class.
    /// Ported from tests/test_page_links.py, tests/test_general.py.
    /// </summary>
    public class LinkTests
    {
        [Fact]
        public void Link_NullOnEmpty()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            Assert.Null(page.FirstLink);
        }

        [Fact]
        public void Link_GetLinksEmpty()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var links = page.GetLinks();
            Assert.Empty(links);
        }

        [Fact]
        public void Link_InsertAndRetrieve()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var linkDict = new System.Collections.Generic.Dictionary<string, object>
            {
                ["kind"] = 2, // LINK_URI
                ["from"] = new Rect(50, 50, 200, 70),
                ["uri"] = "https://example.com"
            };
            page.InsertLink(linkDict);
            var links = page.GetLinks();
            Assert.NotEmpty(links);
        }

        [Fact]
        public void Link_InsertLinkVoid_PythonParity()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var linkDict = new System.Collections.Generic.Dictionary<string, object>
            {
                ["kind"] = 2,
                ["from"] = new Rect(10, 10, 100, 30),
                ["uri"] = "https://example.org/test"
            };
            page.InsertLinkVoid(linkDict);
            var links = page.GetLinks();
            Assert.NotEmpty(links);
        }

        [Fact]
        public void Link_Enumeration()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            int count = 0;
            foreach (var link in page.Links())
                count++;
            Assert.Equal(0, count);
        }
    }
}
