using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class FontTest
    {
        [Test]
        public void Flags()
        {
            Font font = new Font("kenpixel", "../../../resources/kenpixel.ttf", isBold: 1);

            Assert.Pass();
        }

        [Test]
        public void TextLength()
        {
            Font font = new Font("kenpixel", "../../../resources/kenpixel.ttf", isBold: 1);

            float length = font.TextLength("hello world", 15);

            Assert.That(length, Is.Not.Zero);
        }

        [Test]
        public void SubsetFonts()
        {
            Document doc = new Document("../../../resources/subset.pdf");

            int n = doc.PageCount;

            mupdf.mupdf.pdf_subset_fonts2(Document.AsPdfDocument(doc), new mupdf.vectori(Enumerable.Range(0, n / 2).Select(i => i * 2)));
        }
    }
}
