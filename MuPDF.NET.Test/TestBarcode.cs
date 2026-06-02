using System.IO;
using System.Runtime.InteropServices;
using mupdf;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_barcode.py</c> — generate QR and EAN-13 barcodes,
    /// embed them in a PDF, reload, and decode via MuPDF's barcode reader.
    /// </summary>
    public class TestBarcode
    {
        /// <summary>PyMuPDF <c>tests/test_barcode.py::test_barcode</c>.</summary>
        [Fact]
        public void test_barcode()
        {
            const string url = "http://artifex.com";
            const string textIn = "012345678901";
            const string textOut = "123456789012";

            string outDocPath = _Path.ForOutput("test_barcode.pdf", nameof(TestBarcode));
            using (var document = new Document())
            {
                var page = document.NewPage();

                {
                    using var pixmap = new Pixmap(new FzPixmap(fz_barcode_type.FZ_BARCODE_QRCODE, url, 512, 4, 0, 1));
                    page.InsertImage(new Rect(0, 0, 100, 100), pixmap: pixmap);
                }

                {
                    using var pixmap = new Pixmap(new FzPixmap(fz_barcode_type.FZ_BARCODE_EAN13, textIn, 512, 4, 0, 1));
                    page.InsertImage(new Rect(0, 200, 100, 300), pixmap: pixmap);
                }

                document.Save(outDocPath);
            }

            using (var document = new Document(outDocPath))
            {
                var page = document[0];
                var images = page.GetImages();
                Assert.Equal(2, images.Count);
                for (int i = 0; i < images.Count; i++)
                {
                    int xref = images[i].xref;
                    using var pixmap = new Pixmap(document, xref);
                    // Python: pymupdf.mupdf.fz_decode_barcode_from_pixmap2(pixmap.this, 0, outparams)
                    string hrt;
                    IntPtr pType = Marshal.AllocHGlobal(sizeof(int));
                    try
                    {
                        Marshal.WriteInt32(pType, 0);
                        var typePtr = new SWIGTYPE_p_fz_barcode_type(pType, false);
                        hrt = mupdf.mupdf.fz_decode_barcode_from_pixmap2(typePtr, pixmap.NativePixmap, 0);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pType);
                    }
                    if (i == 0)
                        Assert.Equal(url, hrt);
                    else if (i == 1)
                        Assert.Equal(textOut, hrt);
                    else
                        Assert.Fail("unexpected image index");
                }
            }
        }
    }
}
