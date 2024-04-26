using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class MuPDFFontTest
    {
        [Test]
        public void Flags()
        {
            MuPDFFont font = new MuPDFFont("kenpixel", "kenpixel.ttf", isBold: 1);

            Assert.Pass();
        }

        [Test]
        public void TextLength()
        {
            MuPDFFont font = new MuPDFFont("kenpixel", "kenpixel.ttf", isBold: 1);

            float length = font.TextLength("hello world", 15);

            Assert.That(length, Is.Not.Zero);
        }

        [Test]
        public void SubsetFonts()
        {
            MuPDFDocument doc = new MuPDFDocument("resources/2.pdf");

            int n = doc.Len;

            mupdf.mupdf.pdf_subset_fonts2(MuPDFDocument.AsPdfDocument(doc), new mupdf.vectori(Enumerable.Range(0, n / 2).Select(i => i * 2)));
        }
    }
}
