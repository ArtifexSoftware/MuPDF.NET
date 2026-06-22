using System.IO;
using MuPDF.NET;
using PDF4LLM;
using PDF4LLM.Llama;
using Xunit;

namespace PDF4LLM.Test
{
    /// <summary>Port of <c>tests/test_376.py</c>.</summary>
    [Collection("PDF4LLM")]
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
            PDFMarkdownReader reader = PdfExtractor.LlamaMarkdownReader();
            // documents = reader.load_data(path)
            var documents = reader.LoadData(path);
            Assert.NotNull(documents);
            Assert.NotEmpty(documents);
        }
    }
}
