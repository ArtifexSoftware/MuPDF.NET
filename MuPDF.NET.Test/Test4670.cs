using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Input: <c>TestDocuments/Test4670/test_4670.pdf</c>.
    /// Port of PyMuPDF 1.28.2 <c>tests/test_4670.py</c> (#4670 scrub hidden text).
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class Test4670
    {
        private const string TestClassName = nameof(Test4670);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        [Fact]
        public void test_4670()
        {
            // Remove hidden text using redaction annotations.
            //
            // The page only contains hidden text, which should be removed
            // entirely after scrubbing the document.
            string filename = Doc("test_4670.pdf");
            using var doc = new Document(filename);
            var page = doc[0];
            string oldText = page.GetText();
            Assert.False(string.IsNullOrEmpty(oldText));
            doc.Scrub(hiddenText: true);
            // LoadPage always returns a new Page wrapper; re-index after Scrub like a fresh read.
            page = doc[0];
            string newText = page.GetText();
            Assert.True(string.IsNullOrEmpty(newText));
        }
    }
}
