using Xunit;
using mupdf;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class BarcodeTest
    {
        private const string TestClassName = nameof(BarcodeTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void ReadBarcodeFromImage()
        {
            string imageFilePath = Doc("rendered.bmp");

            // Code 128
            List<Barcode> code128s = Utils.ReadBarcodes(imageFilePath, barcodeFormat:BarcodeFormat.CODE128);
            Assert.True(code128s.Count == 3);
            Assert.True(code128s[0].Text == "Hello World!");
            Assert.True(code128s[1].Text == "0123456789");
            Assert.True(code128s[2].Text == "<FNC1>0101234567890128100123456789<FNC1>15041220");

            // I2OF5
            List<Barcode> i2of5s = Utils.ReadBarcodes(imageFilePath, clip: new Rect(1245, 690, 2200, 1235), barcodeFormat: BarcodeFormat.I2OF5);
            Assert.True(i2of5s.Count == 2);
            Assert.True(i2of5s[0].Text == "0123456789");
            Assert.True(i2of5s[1].Text == "15400141288763");

            // Code 39
            List<Barcode> code39s = Utils.ReadBarcodes(imageFilePath, clip: new Rect(1245, 1295, 2200, 1520), barcodeFormat: BarcodeFormat.CODE39);
            Assert.True(code39s.Count == 1);
            Assert.True(code39s[0].Text == "0123456789");

            // UPC-A
            List<Barcode> upcAs = Utils.ReadBarcodes(imageFilePath, clip: new Rect(1245, 2490, 2200, 2720), barcodeFormat: BarcodeFormat.UPC_A);
            Assert.True(upcAs.Count == 1);
            Assert.True(upcAs[0].Text == "001234567895");

            // UPC-E
            List<Barcode> upcEs = Utils.ReadBarcodes(imageFilePath, clip: new Rect(1245, 2780, 2200, 3020), barcodeFormat: BarcodeFormat.UPC_E);
            Assert.True(upcEs.Count == 1);
            Assert.True(upcEs[0].Text == "01234133");
        }

        [Fact]
        public void ReadBarcodeFromPdf()
        {
            string pdfFilePath = Doc("Samples.pdf");

            Document doc = new Document(pdfFilePath);

            Page page = doc[0];

            // EAN 8
            List<Barcode> ean8s = page.ReadBarcodes(barcodeFormat: BarcodeFormat.EAN8);
            Assert.True(ean8s.Count == 1);
            Assert.True(ean8s[0].Text == "40123455");

            // EAN 13
            List<Barcode> ean13s = page.ReadBarcodes(clip: new Rect(212, 243, 510, 575), barcodeFormat: BarcodeFormat.EAN13);
            Assert.True(ean13s.Count == 3);
            Assert.True(ean13s[0].Text == "4012345678901");
            Assert.True(ean13s[1].Text == "9783161484100");
            Assert.True(ean13s[2].Text == "9771234567058");

            // UPC-A
            List<Barcode> upcAs = page.ReadBarcodes(clip: new Rect(212, 575, 510, 690), barcodeFormat: BarcodeFormat.UPC_A);
            Assert.True(upcAs.Count == 1);
            Assert.True(upcAs[0].Text == "042287061527");

            // UPC-E
            List<Barcode> upcEs = page.ReadBarcodes(clip: new Rect(212, 690, 510, 790), barcodeFormat: BarcodeFormat.UPC_E);
            Assert.True(upcEs.Count == 1);
            Assert.True(upcEs[0].Text == "12345670");

            // 2/5 Interleaved
            List<Barcode> i2of5s = page.ReadBarcodes(clip: new Rect(800, 160, 1100, 370), barcodeFormat: BarcodeFormat.I2OF5);
            Assert.True(i2of5s.Count == 2);
            Assert.True(i2of5s[0].Text == "012345678905");
            Assert.True(i2of5s[1].Text == "40123456789010");

            // PHARMA
            List<Barcode> pharmas = page.ReadBarcodes(clip: new Rect(800, 230, 1100, 295), barcodeFormat: BarcodeFormat.PHARMA);
            Assert.True(pharmas.Count == 1);
            Assert.True(pharmas[0].Text == "-1043773788");

            // Code 39
            List<Barcode> code39s = page.ReadBarcodes(clip: new Rect(800, 370, 1100, 435), barcodeFormat: BarcodeFormat.CODE39);
            Assert.True(code39s.Count == 1);
            Assert.True(code39s[0].Text == "123ABC$");

            // Code 39 Extended
            List<Barcode> code39Exs = page.ReadBarcodes(clip: new Rect(800, 435, 1100, 505), barcodeFormat: BarcodeFormat.CODE39_EX);
            Assert.True(code39Exs.Count == 1);
            Assert.True(code39Exs[0].Text == "123+A+B+CX");

            // Code 128
            List<Barcode> code128s = page.ReadBarcodes(clip: new Rect(800, 505, 1100, 645), barcodeFormat: BarcodeFormat.CODE128);
            Assert.True(code128s.Count == 2);
            Assert.True(code128s[0].Text == "1234567890");
            Assert.True(code128s[1].Text == "<FNC1>014012345678901");

            // PHARMA
            pharmas = page.ReadBarcodes(clip: new Rect(800, 645, 1100, 775), barcodeFormat: BarcodeFormat.PHARMA);
            Assert.True(pharmas.Count == 2);
            Assert.True(pharmas[0].Text == "1319299");
            Assert.True(pharmas[1].Text == "12345");

            // TRIOPTIC
            List<Barcode> trioptics = page.ReadBarcodes(clip: new Rect(800, 775, 1100, 845), barcodeFormat: BarcodeFormat.TRIOPTIC);
            Assert.True(trioptics.Count == 1);
            Assert.True(trioptics[0].Text == "-1234567");

            // Datamatrix
            List<Barcode> datamatrixes = page.ReadBarcodes(clip: new Rect(1400, 150, 1660, 357), barcodeFormat: BarcodeFormat.DM);

            // QR
            List<Barcode> qrs = page.ReadBarcodes(clip: new Rect(1400, 357, 1660, 500), barcodeFormat: BarcodeFormat.QR);
            Assert.True(qrs.Count == 1);
            Assert.True(qrs[0].Text == "https://softmatic.com");

            // PDF417
            List<Barcode> pdf417s = page.ReadBarcodes(clip: new Rect(1400, 500, 1660, 580), barcodeFormat: BarcodeFormat.PDF417);
            Assert.True(pdf417s.Count == 1);
            Assert.True(pdf417s[0].Text == "1234567890ABCDEF");

            // Aztec
            List<Barcode> aztecs = page.ReadBarcodes(clip: new Rect(1400, 580, 1660, 720), barcodeFormat: BarcodeFormat.AZTEC);
            Assert.True(aztecs.Count == 1);
            Assert.True(aztecs[0].Text == "1234567890ABCDEF");

            page.Dispose();
            doc.Close();
        }

        [Fact]
        public void ReadDatamatrixFromPdf()
        {
            string pdfFilePath = Doc("datamatrix.pdf");

            Document doc = new Document(pdfFilePath);

            Page page = doc[0];

            List<Barcode> barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.DM);
            Assert.True(barcodes.Count == 2);
            Assert.True(barcodes[0].Text == "01020000110435177000");
            Assert.True(barcodes[1].Text == "1100630047057533");

            page.Dispose();
            doc.Close();
        }

        [Fact]
        public void ReadQrFromPdf()
        {
            string pdfFilePath = Doc("qr.pdf");

            Document doc = new Document(pdfFilePath);

            Page page = doc[0];

            List<Barcode> barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.QR);
            Assert.True(barcodes.Count == 1);
            Assert.True(barcodes[0].Text == "$001-52-20250520#MRC");

            page.Dispose();
            doc.Close();
        }

        [Fact]
        public void TestBarcode()
        {
            string testFileName = Out("TestBarcode.pdf");

            Document doc = new Document();
            Page page = doc.NewPage();

            MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(page.Rect);
            Font font = new Font("cour", isBold: 1);
            writer.FillTextbox(page.Rect, "QR_CODE", font, pos: new Point(0, 10));
            writer.FillTextbox(page.Rect, "EAN_8", font, pos: new Point(0, 110));
            writer.FillTextbox(page.Rect, "EAN_13", font, pos: new Point(0, 165));
            writer.FillTextbox(page.Rect, "UPC_A", font, pos: new Point(0, 220));
            writer.FillTextbox(page.Rect, "CODE_39", font, pos: new Point(0, 275));
            writer.FillTextbox(page.Rect, "CODE_128", font, pos: new Point(0, 330));
            writer.FillTextbox(page.Rect, "ITF", font, pos: new Point(0, 385));
            writer.FillTextbox(page.Rect, "PDF_417", font, pos: new Point(0, 440));
            writer.FillTextbox(page.Rect, "CODABAR", font, pos: new Point(0, 520));
            writer.FillTextbox(page.Rect, "DATA_MATRIX", font, pos: new Point(0, 620));
            writer.WriteText(page);

            // QR_CODE
            Rect rect = new Rect(100, 20, 300, 80);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.QR, forceFitToRect: false, pureBarcode: false, marginLeft: 0);

            // EAN_8
            rect = new Rect(100, 100, 300, 120);
            page.WriteBarcode(rect, "1234567", BarcodeFormat.EAN8, forceFitToRect: false, pureBarcode: false, marginBottom: 20);

            // EAN_13
            rect = new Rect(100, 155, 300, 200);
            page.WriteBarcode(rect, "123456789012", BarcodeFormat.EAN13, forceFitToRect: false, pureBarcode: true, marginBottom: 0);

            // UPC_A
            rect = new Rect(100, 210, 300, 255);
            page.WriteBarcode(rect, "123456789012", BarcodeFormat.UPC_A, forceFitToRect: false, pureBarcode: true, marginBottom: 0);

            // CODE_39
            rect = new Rect(100, 265, 600, 285);
            page.WriteBarcode(rect, "Hello!", BarcodeFormat.CODE39, forceFitToRect: false, pureBarcode: false, marginBottom: 0);

            // CODE_128
            rect = new Rect(100, 320, 400, 355);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.CODE128, forceFitToRect: true, pureBarcode: true, marginBottom: 0);

            // ITF
            rect = new Rect(100, 385, 300, 420);
            page.WriteBarcode(rect, "12345678901234567890", BarcodeFormat.I2OF5, forceFitToRect: false, pureBarcode: false, marginBottom: 0);

            // PDF_417
            rect = new Rect(100, 430, 400, 435);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.PDF417, forceFitToRect: false, pureBarcode: true, marginBottom: 0);

            // CODABAR
            rect = new Rect(100, 540, 400, 580);
            page.WriteBarcode(rect, "12345678901234567890", BarcodeFormat.CODABAR, forceFitToRect: true, pureBarcode: true, marginBottom: 0);

            // DATA_MATRIX
            rect = new Rect(100, 620, 140, 660);
            page.WriteBarcode(rect, "01100000110419257000", BarcodeFormat.DM, forceFitToRect: false, pureBarcode: false, marginBottom: 0);

            page.Dispose();
            doc.Save(testFileName);
            doc.Close();

            // read barcodes in the new pdf document
            doc = new Document(testFileName);
            page = doc[0];

            List<Barcode> barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.QR);
            Assert.True(barcodes.Count == 1 && barcodes[0].Text == "Hello World!");

            barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.EAN8);
            Assert.True(barcodes.Count == 1 && barcodes[0].Text == "12345670");

            barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.EAN13);
            Assert.True(barcodes.Count == 1 && barcodes[0].Text == "0123456789012");

            barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.UPC_A);
            Assert.True(barcodes.Count == 1 && barcodes[0].Text == "123456789012");

            barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.CODE39);
            Assert.True(barcodes.Count == 1 && barcodes[0].Text == "H+E+L+L+O/A");

            barcodes = page.ReadBarcodes(clip: new Rect(100, 320, 400, 355), barcodeFormat: BarcodeFormat.CODE128);
            Assert.True(barcodes.Count == 1 && barcodes[0].Text == "Hello World!");

            barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.I2OF5);
            Assert.True(barcodes.Count == 1 && barcodes[0].Text == "12345678901234567890");

            barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.PDF417);
            Assert.True(barcodes.Count == 1 && barcodes[0].Text == "Hello World!");

            barcodes = page.ReadBarcodes(clip: new Rect(80, 500, 450, 700), barcodeFormat: BarcodeFormat.CODABAR);

            barcodes = page.ReadBarcodes(clip: new Rect(100, 620, 140, 660), barcodeFormat: BarcodeFormat.DM);

            page.Dispose();
            doc.Close();
        }
    }
}
