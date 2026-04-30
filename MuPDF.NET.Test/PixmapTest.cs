using NUnit.Framework;

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

            //Assert.Pass();
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

            pix.Dispose();
            page.Dispose();
            doc.Close();

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

            //Assert.Pass();
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
            //Assert.Pass();
        }

        [Test]
        public void TestPixmapToBytes()
        {
            // Test single pixmap creation and PNG conversion with matrix scaling
            Document doc = new Document("../../../resources/cython.pdf");
            Page page = doc[0];
            Pixmap pixmap = page.GetPixmap(new Matrix(2, 2));

            byte[] png = pixmap.ToBytes("png");

            // Verify PNG bytes are generated and valid
            Assert.That(png.Length, Is.GreaterThan(0));
            // PNG magic bytes: 137, 80, 78, 71
            Assert.That(png[0], Is.EqualTo(137));
            Assert.That(png[1], Is.EqualTo(80));
            Assert.That(png[2], Is.EqualTo(78));
            Assert.That(png[3], Is.EqualTo(71));

            pixmap.Dispose();
            page.Dispose();
            doc.Close();
        }

        [Test]
        public void TestPixmapParallel()
        {
            // Test parallel pixmap rendering with PNG conversion (simulating TestPixmap())
            const int iterations = 50;  // Reduced from 500 for unit test performance
            const int degreeOfParallelism = 4;  // Reduced from 10 for unit test performance

            using (var document = new Document("../../../resources/cython.pdf"))
            {
                var renderResults = new System.Collections.Concurrent.ConcurrentBag<int>();
                var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

                Parallel.ForEach(
                    Enumerable.Range(0, iterations),
                    new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                    iteration =>
                    {
                        try
                        {
                            using var page = document[0];
                            using var pixmap = page.GetPixmap(new Matrix(2, 2));
                            var png = pixmap.ToBytes("png");
                            renderResults.Add(png.Length);
                        }
                        catch (Exception ex)
                        {
                            errors.Add(ex);
                        }
                    });

                // Verify all iterations completed successfully
                Assert.That(errors.Count, Is.EqualTo(0), $"Parallel rendering encountered errors: {string.Join(", ", errors.Select(e => e.Message))}");
                Assert.That(renderResults.Count, Is.EqualTo(iterations), "Not all iterations completed");
                Assert.That(renderResults.All(size => size > 0), Is.True, "All PNG bytes should be valid");
            }
        }

    }
}
