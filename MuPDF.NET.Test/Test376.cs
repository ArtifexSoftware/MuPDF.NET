using System.IO;
using MuPDF.NET;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
    public class Test376
    {
        private const string TestClassName = nameof(Test376);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_376()
        {
            string path = Out("test_376_out.pdf");
            using (var document = new Document())
            {
                document.NewPage();
                document.Save(path);
            }

            // reader = LlamaMarkdownReader()
            PDFMarkdownReader reader = Utils.LlamaMarkdownReader();
            // documents = reader.load_data(path)
            var documents = reader.LoadData(path);
            Assert.NotNull(documents);
            Assert.NotEmpty(documents);
        }
    }
}