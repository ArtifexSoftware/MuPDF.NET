using System.IO;
using MuPDF.NET.PDF4LLM;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestMarkdownToPdf
    {
        private const string TestClassName = nameof(TestMarkdownToPdf);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_markdown_to_pdf()
        {
            //     """Use a pre-existing MD file and generate a PDF from it.
            //     Then convert the PDF back to MD and check that the content is the same.
            //     """
            string oldMdPath = Doc("test_markdown_to_pdf-expected.md");
            string oldMd = File.ReadAllText(oldMdPath);

            // new_pdf = layout package.markdown_to_pdf(old_md_path, output_path=...)
            string pdfPath = Out("test_markdown_to_pdf.pdf");
            MuPDF4LLM.MarkdownToPdf(oldMdPath, outputPath: pdfPath);
            Assert.True(File.Exists(pdfPath));

            // new_md = layout package.to_markdown(pdf_path, use_ocr=False)
            string newMd = MuPDF4LLM.ToMarkdown(pdfPath, useOcr: false);

            Assert.Equal(NormalizeMarkdown(oldMd), NormalizeMarkdown(newMd));
        }

        private static string NormalizeMarkdown(string md) =>
            md.Replace("\r\n", "\n").Trim();
    }
}