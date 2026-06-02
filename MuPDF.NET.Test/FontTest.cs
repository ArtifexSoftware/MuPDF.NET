using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class FontTest
    {
        private const string TestClassName = nameof(FontTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void Flags()
        {
            Font font = new Font("kenpixel", Doc("kenpixel.ttf"), isBold: 1);

            //Assert.Pass();
        }

        [Fact]
        public void TextLength()
        {
            Font font = new Font("kenpixel", Doc("kenpixel.ttf"), isBold: 1);

            float length = font.TextLength("hello world", 15);

            Assert.True(length != 0);
        }

        [Fact]
        public void SubsetFonts()
        {
            Document doc = new Document(Doc("subset.pdf"));

            int n = doc.PageCount;

            mupdf.mupdf.pdf_subset_fonts2(Document.AsPdfDocument(doc), new mupdf.vectori(Enumerable.Range(0, n / 2).Select(i => i * 2)));
        }
    }
}
