using System;
using System.IO;
using MuPDF.NET;
using PDF4LLM;
using PDF4LLM.Llama;

namespace PDF4LLM.Test
{
    /// <summary>Port of pymupdf4llm/tests/test_376.py</summary>
    [TestFixture]
    public class Test376 : LLMTestBase
    {
        private const string TestClassName = nameof(Test376);
        private static string Doc(string fileName) => ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => ForOutput(fileName, TestClassName);

        [Test]
        public void test_376()
        {
            string path = Out("test_376_out.pdf");
            try
            {
                using (var document = new Document())
                {
                    document.NewPage();
                    document.Save(path);
                }

                var reader = PdfExtractor.LlamaMarkdownReader();
                var documents = reader.LoadData(path);
                Assert.That(documents, Is.Not.Null);
                Assert.That(documents.Count, Is.GreaterThan(0));
            }
            finally
            {
                //if (File.Exists(path))
                //    File.Delete(path);
            }
        }
    }
}
