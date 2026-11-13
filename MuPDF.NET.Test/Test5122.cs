using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Input: <c>TestDocuments/Test5122/test_5122.pdf</c>.
    /// First drawing's rect top-left equals the following line start.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class Test5122
    {
        private const string TestClassName = nameof(Test5122);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        [Fact]
        public void test_5122()
        {
            using var doc = new Document(Doc("test_5122.pdf"));
            var page = doc[0];
            var path = page.GetDrawings()[0];
            var rectTl = path.Items[0].Rect.TL;
            var lineP1 = path.Items[1].P1;
            Assert.Equal(rectTl, lineP1);
        }
    }
}
