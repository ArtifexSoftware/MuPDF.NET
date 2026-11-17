using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
    public class TestPdfMarkdownReader
    {
        // PDF = "input.pdf"
        private const string PDF = "input.pdf";

        private static string? _get_test_file_path(string fileName) =>
            _Path.TryForTestClass(fileName, nameof(TestPdfMarkdownReader))
            ?? null;

        [Fact]
        public void test_load_data()
        {
            string? path = _get_test_file_path(PDF);
            if (path == null || !File.Exists(path))
                return;

            var pdfReader = Utils.LlamaMarkdownReader();
            var extraInfo = new Dictionary<string, object> { ["test_key"] = "test_value" };
            var documents = pdfReader.LoadData(path, extraInfo);

            Assert.NotNull(documents);
            Assert.NotEmpty(documents);
        }

        [Fact]
        public void test_load_data_with_invalid_file_path()
        {
            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(false);
                var pdfReader = Utils.LlamaMarkdownReader();
                var extraInfo = new Dictionary<string, object> { ["test_key"] = "test_value" };
                Assert.ThrowsAny<Exception>(() => pdfReader.LoadData("fake/path", extraInfo));
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        [Fact]
        public void test_load_data_with_invalid_extra_info()
        {
            //     pdf_reader = PDFMarkdownReader()
            var pdfReader = Utils.LlamaMarkdownReader();
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