using NUnit.Framework;
using mupdf;

namespace MuPDF.NET.Test
{
    public class BarcodeTest
    {
        [Test]
        public void TestBarcode()
        {
            //Document doc = new Document("../../../resources/test_barcode_out.pdf");
            string path = "test_barcode_out.pdf";

            string url = "http://artifex.com";
            string textIn = "012345678901";
            string textOut = "123456789012";

            {
                // Create empty document and add a qrcode image.
                Document document = new Document();
                var page = document.NewPage();

                // QR Code
                var qrPixmapNative = mupdf.mupdf.fz_new_barcode_pixmap(fz_barcode_type.FZ_BARCODE_QRCODE, url, 512, 4, 0, 1);
                var qrPixmap = new Pixmap(qrPixmapNative);
                page.InsertImage(new Rect(0, 0, 100, 100), pixmap: qrPixmap);

                // EAN13 Barcode
                var eanPixmapNative = mupdf.mupdf.fz_new_barcode_pixmap(fz_barcode_type.FZ_BARCODE_EAN13, textIn, 512, 4, 0, 1);
                var eanPixmap = new Pixmap(eanPixmapNative);
                page.InsertImage(new Rect(0, 200, 100, 300), pixmap: eanPixmap);

                document.Save(path);

                document.Close();
            }
        }
    }
}
