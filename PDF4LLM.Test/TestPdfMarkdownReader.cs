using System;
using System.Collections.Generic;
using System.IO;
using PDF4LLM;
using PDF4LLM.Llama;
using Xunit;

namespace PDF4LLM.Test
{
    [Collection("PDF4LLM")]
    public class TestPdfMarkdownReader
    {
        // PDF = "input.pdf"
        private const string PDF = "input.pdf";

        private static string? _get_test_file_path(string fileName) =>
            _Path.TryForTestClass(fileName, nameof(TestPdfMarkdownReader))
            ?? _Path.TryPymupdf4llmTests(fileName);

        [Fact]
        public void test_load_data()
        {
            string? path = _get_test_file_path(PDF);
            if (path == null || !File.Exists(path))
                return;

            var pdfReader = PdfExtractor.LlamaMarkdownReader();
            var extraInfo = new Dictionary<string, object> { ["test_key"] = "test_value" };
            var documents = pdfReader.LoadData(path, extraInfo);

            Assert.NotNull(documents);
            Assert.NotEmpty(documents);
        }

        [Fact]
        public void test_load_data_with_invalid_file_path()
        {
            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(false);
                var pdfReader = PdfExtractor.LlamaMarkdownReader();
                var extraInfo = new Dictionary<string, object> { ["test_key"] = "test_value" };
                Assert.ThrowsAny<Exception>(() => pdfReader.LoadData("fake/path", extraInfo));
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_load_data_with_invalid_extra_info()
        {
            //     pdf_reader = PDFMarkdownReader()
            var pdfReader = PdfExtractor.LlamaMarkdownReader();
            //     path = _get_test_file_path(PDF)
            string? path = _get_test_file_path(PDF);
            if (path == null || !File.Exists(path))
                return;
            //     extra_info = "not a dict"
            //         pdf_reader.load_data(path, extra_info)
            Assert.Throws<ArgumentException>(() => pdfReader.LoadData(path, "not a dict"));
        }
    }
}