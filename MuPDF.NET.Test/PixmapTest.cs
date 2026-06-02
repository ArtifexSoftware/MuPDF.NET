using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class PixmapTest
    {
        private const string TestClassName = nameof(PixmapTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);
        
        [Fact]
        public void GetPixamp_Save()
        {
            Document doc = new Document(Doc("cython.pdf"));

            Page page = doc[0];

            Pixmap source = page.GetPixmap();
            
            Pixmap p = new Pixmap(source.ColorSpace, source.W, source.H, source.SAMPLES, 0);

            p.Save(Out("GetPixamp_Save.jpg"), "JPEG");

            //Assert.Pass();
        }

        [Fact]
        public void GetPixel()
        {
            Document doc = new Document(Doc("cython.pdf"));

            Page page = doc[0];

            Pixmap pix = page.GetPixmap();

            byte[] bPixel = pix.GetPixel(10, 10);

            Assert.Equal(3, bPixel.Length);
            Assert.Equal(255, bPixel[0]);
            Assert.Equal(255, bPixel[1]);
            Assert.Equal(255, bPixel[2]);
        }
        
        [Fact]
        public void ToBytes()
        {
            Document doc = new Document(Doc("cython.pdf"));

            Page page = doc[0];

            Pixmap pix = page.GetPixmap();

            byte[] bytes = pix.ToBytes();

            Assert.Equal(21908, bytes.Length);
        }
        
        [Fact]
        public void ColorToUsage()
        {
            Document doc = new Document(Doc("cython.pdf"));

            Page page = doc[0];

            Pixmap pix = page.GetPixmap();

            (float f, byte[] max) = pix.ColorTopUsage();

            pix.Dispose();
            page.Dispose();
            doc.Close();

            Assert.Equal(255, max[0]);
            Assert.Equal(255, max[1]);
            Assert.Equal(255, max[2]);


        }
        
        [Fact]
        public void InvertIrect()
        {
            Document doc = new Document(Doc("cython.pdf"));

            Page page = doc[0];

            Pixmap pix = page.GetPixmap();

            bool result = pix.InvertIrect(new IRect(100, 100, 900, 900));

            Assert.True(result);
            doc.Save(Out("InvertIrect.pdf"));
        }
        
        [Fact]
        public void PdfOCR()
        {
            Document doc = new Document(Doc("cython.pdf"));

            Page page = doc[0];

            Pixmap pix = page.GetPixmap();

            byte[] byts = pix.PdfOCR2Bytes();

            Assert.Equal(23813, byts.Length);

            //Assert.Pass();
        }
        
        [Fact]
        public void PdfPixmap()
        {
            Document doc = new Document(Doc("toc.pdf"));
            Entry img = doc.GetPageImages(0)[0];
            Pixmap pix = new Pixmap(doc, img.Xref);
            Assert.Equal(img.Width, pix.W);
            Assert.Equal(img.Height, pix.H);

            ImageInfo ex = doc.ExtractImage(img.Xref);
            Assert.Equal(pix.W, ex.Width);
            Assert.Equal(pix.H, ex.Height);
        }
        
        [Fact]
        public void InvertIrect1()
        {
            Pixmap pix = new Pixmap(Doc("img-transparent.png"));
            Rect rect = new Rect(0, 0, 100, 100);
            pix.InvertIrect(new IRect(rect));
            pix.Save(Out("InvertIrect1.png"));
            //Assert.Pass();
        }
        
        [Fact]
        public void TestPixmapToBytes()
        {
            // Test single pixmap creation and PNG conversion with matrix scaling
            Document doc = new Document(Doc("cython.pdf"));
            Page page = doc[0];
            Pixmap pixmap = page.GetPixmap(new Matrix(2, 2));

            byte[] png = pixmap.ToBytes("png");

            // Verify PNG bytes are generated and valid
            Assert.True(png.Length > 0);
            // PNG magic bytes: 137, 80, 78, 71
            Assert.Equal(137, png[0]);
            Assert.Equal(80, png[1]);
            Assert.Equal(78, png[2]);
            Assert.Equal(71, png[3]);

            pixmap.Dispose();
            page.Dispose();
            doc.Close();
        }
        
        [Fact]
        public void TestPixmapParallel()
        {
            // Test parallel pixmap rendering with PNG conversion (simulating TestPixmap())
            const int iterations = 50;  // Reduced from 500 for unit test performance
            const int degreeOfParallelism = 4;  // Reduced from 10 for unit test performance

            using (var document = new Document(Doc("cython.pdf")))
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
                Assert.True(
                    errors.Count == 0,
                    $"Parallel rendering encountered errors: {string.Join(", ", errors.Select(e => e.Message))}");
                Assert.True(renderResults.Count == iterations, "Not all iterations completed");
                Assert.True(renderResults.All(size => size > 0), "All PNG bytes should be valid");
            }
        }
    }
}
