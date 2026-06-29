using System;
using System.Collections.Generic;
using System.IO;
using PDF4LLM;
using PDF4LLM.Llama;
using Xunit;

namespace PDF4LLM.Test
{
    /// <summary>Port of <c>tests/llama_index/_test_pdf_markdown_reader.py</c>.</summary>
    [Collection("PDF4LLM")]
    public class TestPdfMarkdownReader
    {
        // PDF = "input.pdf"
        private const string PDF = "input.pdf";

        private static string _get_test_file_path(string fileName)
        {
            // def _get_test_file_path(file_name: str, __file__: str = __file__) -> str:
            //     return os.path.normpath(f'{__file__}/../../source/4llm/helpers/{file_name}')
            string fromTestDocuments = _Path.ResolveTestDocument(fileName, nameof(TestPdfMarkdownReader));
            if (File.Exists(fromTestDocuments))
                return fromTestDocuments;
            string fromUpstream = Path.Combine(_Path.ResolveSolutionRoot(), "pymupdf4llm", "tests", fileName);
            return fromUpstream;
        }

        [Fact]
        public void test_load_data()
        {
            string path = _get_test_file_path(PDF);
            if (!File.Exists(path))
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
            // # We need to disable layout.
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
            // def test_load_data_with_invalid_extra_info():
            //     pdf_reader = PDFMarkdownReader()
            var pdfReader = PdfExtractor.LlamaMarkdownReader();
            //     path = _get_test_file_path(PDF)
            string path = _get_test_file_path(PDF);
            if (!File.Exists(path))
                return;
            //     extra_info = "not a dict"
            //     with pytest.raises(TypeError):
            //         pdf_reader.load_data(path, extra_info)
            Assert.Throws<ArgumentException>(() => pdfReader.LoadData(path, "not a dict"));
        }

        [Fact(Skip = "ALoadData is not implemented in PDF4LLM.Llama.PDFMarkdownReader.")]
        public void test_aload_data_with_invalid_file_path()
        {
            // async def test_aload_data_with_invalid_file_path():
        }

        [Fact(Skip = "ALoadData is not implemented in PDF4LLM.Llama.PDFMarkdownReader.")]
        public void test_aload_data_with_invalid_extra_info()
        {
            // async def test_aload_data_with_invalid_extra_info():
        }
    }
}
