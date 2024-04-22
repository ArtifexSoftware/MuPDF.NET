using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class PixmapTest
    {
        [Test]
        public void GetPixamp_Save()
        {
            MuPDFDocument doc = new MuPDFDocument("input.pdf");

            MuPDFPage page = doc[0];

            Pixmap source = page.GetPixmap();

            Pixmap p = new Pixmap(source.ColorSpace, source.W, source.H, source.SAMPLES, 0);

            p.Save("1.jpg", "JPEG");

            Assert.Pass();
        }

        [Test]
        public void GetPixel()
        {
            MuPDFDocument doc = new MuPDFDocument("input.pdf");

            MuPDFPage page = doc[0];

            Pixmap pix = page.GetPixmap();

            byte[] bPixel = pix.GetPixel(10, 10);

            Assert.That(bPixel.Length, Is.EqualTo(3));
            Assert.That(bPixel[0], Is.EqualTo(255));
            Assert.That(bPixel[1], Is.EqualTo(255));
            Assert.That(bPixel[2], Is.EqualTo(255));
        }

        [Test]
        public void ToBytes()
        {
            MuPDFDocument doc = new MuPDFDocument("input.pdf");

            MuPDFPage page = doc[0];

            Pixmap pix = page.GetPixmap();

            byte[] bytes = pix.ToBytes();

            Assert.That(bytes.Length, Is.EqualTo(167863));
        }
    }
}
