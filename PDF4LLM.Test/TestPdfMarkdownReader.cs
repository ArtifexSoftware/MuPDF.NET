using System;
using System.Collections.Generic;
using System.IO;
using PDF4LLM;
using PDF4LLM.Llama;

namespace PDF4LLM.Test
{
    /// <summary>Port of pymupdf4llm/tests/pymupdf4llm/llama_index/_test_pdf_markdown_reader.py</summary>
    [TestFixture]
    public class TestPdfMarkdownReader : LLMTestBase
    {
        [Test]
        public void test_load_data()
        {
            string path = FixturePath("input.pdf");
            if (!File.Exists(path))
            {
                Assert.Ignore("Doing nothing because input.pdf does not exist.");
                return;
            }

            var pdfReader = PdfExtractor.LlamaMarkdownReader();
            var extraInfo = new Dictionary<string, object> { ["test_key"] = "test_value" };
            var documents = pdfReader.LoadData(path, extraInfo);
            Assert.That(documents, Is.Not.Null);
            Assert.That(documents.Count, Is.GreaterThan(0));
        }

        [Test]
        public void test_load_data_with_invalid_file_path()
        {
            var pdfReader = PdfExtractor.LlamaMarkdownReader();
            var extraInfo = new Dictionary<string, object> { ["test_key"] = "test_value" };
            Assert.Throws<FileNotFoundException>(() => pdfReader.LoadData("fake/path", extraInfo));
        }

        [Test]
        public void test_load_data_with_invalid_extra_info()
        {
            string path = FixturePath("input.pdf");
            if (!File.Exists(path))
            {
                Assert.Ignore("Doing nothing because input.pdf does not exist.");
                return;
            }

            // Python raises TypeError for extra_info=str; C# API requires Dictionary<string, object>.
            Assert.Ignore("LoadData extra_info is strongly typed in C# (no TypeError for string).");
        }

        [Test]
        public void test_aload_data_with_invalid_file_path()
        {
            Assert.Ignore("ALoadData is not implemented in PDF4LLM.Llama.PDFMarkdownReader.");
        }

        [Test]
        public void test_aload_data_with_invalid_extra_info()
        {
            Assert.Ignore("ALoadData is not implemented in PDF4LLM.Llama.PDFMarkdownReader.");
        }
    }
}
