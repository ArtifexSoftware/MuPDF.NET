using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class PixmapTest
    {
        [Test]
        public void GetPixamp_Save()
        {
            Document doc = new Document("../../../resources/cython.pdf");

            Page page = doc[0];

            Pixmap source = page.GetPixmap();
            
            Pixmap p = new Pixmap(source.ColorSpace, source.W, source.H, source.SAMPLES, 0);

            p.Save("1.jpg", "JPEG");

            Assert.Pass();
        }

        [Test]
        public void GetPixel()
        {
            Document doc = new Document("../../../resources/cython.pdf");

            Page page = doc[0];

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
            Document doc = new Document("../../../resources/cython.pdf");

            Page page = doc[0];

            Pixmap pix = page.GetPixmap();

            byte[] bytes = pix.ToBytes();

            Assert.That(bytes.Length, Is.EqualTo(21908));
        }

        [Test]
        public void ColorToUsage()
        {
            Document doc = new Document("../../../resources/cython.pdf");

            Page page = doc[0];

            Pixmap pix = page.GetPixmap();

            (float f, byte[] max) = pix.ColorTopUsage();

            Assert.That(max[0], Is.EqualTo(255));
            Assert.That(max[1], Is.EqualTo(255));
            Assert.That(max[2], Is.EqualTo(255));
        }

        [Test]
        public void InvertIrect()
        {
            Document doc = new Document("../../../resources/cython.pdf");

            Page page = doc[0];

            Pixmap pix = page.GetPixmap();

            bool result = pix.InvertIrect(new IRect(100, 100, 900, 900));

            Assert.That(result, Is.True);
        }

        [Test]
        public void PdfOCR()
        {
            Document doc = new Document("../../../resources/cython.pdf");

            Page page = doc[0];

            Pixmap pix = page.GetPixmap();

            pix.PdfOCR2Bytes();

            Assert.Pass();
        }

        [Test]
        public void PdfPixmap()
        {
            Document doc = new Document("../../../resources/toc.pdf");
            Entry img = doc.GetPageImages(0)[0];
            Pixmap pix = new Pixmap(doc, img.Xref);
            Assert.That(pix.W, Is.EqualTo(img.Width));
            Assert.That(pix.H, Is.EqualTo(img.Height));

            ImageInfo ex = doc.ExtractImage(img.Xref);
            Assert.That(ex.Width, Is.EqualTo(pix.W));
            Assert.That(ex.Height, Is.EqualTo(pix.H));
        }

        [Test]
        public void InvertIrect1()
        {
            Pixmap pix = new Pixmap("../../../resources/img-transparent.png");
            Rect rect = new Rect(0, 0, 100, 100);
            pix.InvertIrect(new IRect(rect));
            Assert.Pass();
        }
    }
}
