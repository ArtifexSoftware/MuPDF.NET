using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test4614
    {
        private static readonly string testDocPath = _Path.ForTestClass("test_4614.pdf", nameof(Test4614));
        private static readonly string outDocPath = _Path.ForOutput("test_4614.pdf", nameof(Test4614));

        [Fact]
        public void test_4614()
        {
            // script_dir = os.path.dirname(__file__)
            // filename = os.path.join(script_dir, "resources", "test_4614.pdf")
            using var src = new Document(testDocPath);
            using var doc = new Document();
            doc.InsertPdf(src);
            doc.Save(outDocPath);
        }
    }
}