using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Extract images from a PDF file, confirm number of images found.
    /// Inputs: <c>TestDocuments/TestExtractImage/</c>;
    /// outputs: <c>TestDocuments/_Output/TestExtractImage/</c>.
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class TestExtractImage
    {
        private const int KnownImageCount = 21;
        private const string TestClassName = nameof(TestExtractImage);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary>Regression test: extract image.</summary>
        [Fact]
        public void test_extract_image()
        {
            // filename = os.path.join(scriptdir, "resources", "joined.pdf")
            using var doc = new Document(Doc("joined.pdf"));
            int imageCount = 1;
            for (int xref = 1; xref < doc.XrefLength - 1; xref++)
            {
                if (doc.XrefGetKey(xref, "Subtype").value != "/Image")
                    continue;
                var img = doc.ExtractImage(xref);
                if (img != null)
                    imageCount += 1;
            }

            Assert.Equal(KnownImageCount, imageCount);
        }

        /// <summary>Regression test: 2348.</summary>
        [Fact]
        public void test_2348()
        {
            string pdfPath = Out("test_2348.pdf");
            using (var document = new Document())
            {
                var rect = new Rect(20, 20, 480, 820);
                var page = document.NewPage(width: 500, height: 842);
                page.InsertImage(rect, filename: Doc("nur-ruhig.jpg"));
                page = document.NewPage(width: 500, height: 842);
                page.InsertImage(rect, filename: Doc("img-transparent.png"));
                document.EzSave(pdfPath);
            }

            using (var document = new Document(pdfPath))
            {
                var page = document[0];
                var imlist = page.GetImages();
                var image = document.ExtractImage(imlist[0].xref);
                string jpegExtension = (string)image["ext"];

                page = document[1];
                imlist = page.GetImages();
                image = document.ExtractImage(imlist[0].xref);
                string pngExtension = (string)image["ext"];

                Assert.Equal("jpeg", jpegExtension);
                Assert.Equal("png", pngExtension);
            }
        }

        /// <summary>smoke test; no assert in Python.</summary>
        [Fact]
        public void test_delete_image()
        {
            using var doc = new Document(Doc("test_delete_image.pdf"));
            var page = doc[0];
            int xref = page.GetImages()[0].xref;
            page.DeleteImage(xref);
            doc.Save(Out("test_delete_image.pdf"));
        }
    }
}