using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test2907
    {
        private static readonly string testDocPath = _Path.ForTestClass("test_2907.pdf", nameof(Test2907));
        private static readonly string outDocPath = _Path.ForOutput("test_2907.pdf", nameof(Test2907));

        [Fact]
        public void test_2907()
        {
            // This test is for a bug in classic 'segfault trying to call clean_contents
            // on certain pdfs with python 3.12', which we are not going to fix.
            //     return;
            byte[] pdf_file = File.ReadAllBytes(testDocPath);
            using var fitz_document = new Document(pdf_file, fileType: "application/pdf");

            var pdf_pages = fitz_document.pages().ToList();
            Page page = Assert.Single(pdf_pages);
            page.CleanContents();
            fitz_document.Save(outDocPath);
        }
    }
}