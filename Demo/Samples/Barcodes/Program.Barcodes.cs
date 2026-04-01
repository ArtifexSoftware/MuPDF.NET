namespace Demo
{
    internal partial class Program
    {
        internal static void TestWriteBarcode1()
        {
            Console.WriteLine("\n=== TestWriteBarcode1 =====================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/Blank.pdf");
            Document doc = new Document(testFilePath);

            Page page = doc[0];

            // CODE39
            Rect rect = new Rect(
                X0: Units.MmToPoints(50),
                X1: Units.MmToPoints(80), 
                Y0: Units.MmToPoints(70), 
                Y1: Units.MmToPoints(85));

            page.WriteBarcode(rect, "JJBEA6500", BarcodeFormat.CODE39, forceFitToRect: true, pureBarcode: true, narrowBarWidth:1);

            rect = new Rect(
                X0: Units.MmToPoints(50),
                X1: Units.MmToPoints(160),
                Y0: Units.MmToPoints(100),
                Y1: Units.MmToPoints(105));

            page.WriteBarcode(rect, "JJBEA6500", BarcodeFormat.CODE39, forceFitToRect: true, pureBarcode: true, narrowBarWidth: 2);

            // CODE128
            Rect rect1 = new Rect(
                X0: Units.MmToPoints(50),
                X1: Units.MmToPoints(100),
                Y0: Units.MmToPoints(50),
                Y1: Units.MmToPoints(60));

            page.WriteBarcode(rect1, "JJBEA6500063000000177922", BarcodeFormat.CODE128, forceFitToRect: false, pureBarcode: true, narrowBarWidth: 1);

            rect1 = new Rect(
                X0: Units.MmToPoints(50),
                X1: Units.MmToPoints(200),
                Y0: Units.MmToPoints(80),
                Y1: Units.MmToPoints(120));

            page.WriteBarcode(rect1, "JJBEA6500063000000177922", BarcodeFormat.CODE128, forceFitToRect: true, pureBarcode: true, narrowBarWidth: 1);

            Rect rect2 = new Rect(
                X0: Units.MmToPoints(100),
                X1: Units.MmToPoints(140),
                Y0: Units.MmToPoints(40),
                Y1: Units.MmToPoints(80));

            page.WriteBarcode(rect2, "01030000110444408000", BarcodeFormat.DM, forceFitToRect: false, pureBarcode: true, narrowBarWidth: 3);

            Pixmap pxmp = Utils.GetBarcodePixmap("JJBEA6500063000000177922", BarcodeFormat.CODE128, width: 500, pureBarcode: true, marginLeft:0, marginTop:0, marginRight:0, marginBottom:0, narrowBarWidth: 1);
            
            pxmp.Save(@"PxmpBarcode3.png");

            byte[] imageBytes = pxmp.ToBytes();

            using var stream = new SKMemoryStream(imageBytes);
            using var codec = SKCodec.Create(stream);
            var info = codec.Info;
            var bitmap = SKBitmap.Decode(codec);

            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100); // 100 = quality
            using var stream1 = File.OpenWrite(@"output.png");
            data.SaveTo(stream1);

            doc.Save(@"TestWriteBarcode1.pdf");

            page.Dispose();
            doc.Close();

            Console.WriteLine("TestWriteBarcode1 completed.");
        }

        internal static void TestReadDataMatrix()
        {
            int i = 0;

            Console.WriteLine("\n=== TestReadDataMatrix =======================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/Barcodes/datamatrix.pdf");
            Document doc = new Document(testFilePath);

            Page page = doc[0];

            List<Barcode> barcodes = page.ReadBarcodes(decodeEmbeddedOnly: false);

            foreach (Barcode barcode in barcodes)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
            /*
            List<Block> blocks = page.GetImageInfo();

            foreach (Block block in blocks)
            {
                Rect blockRect = block.Bbox;
                barcodes = page.ReadBarcodes(clip:blockRect);
                foreach (Barcode barcode in barcodes)
                {
                    BarcodePoint[] points = barcode.ResultPoints;
                    if (points.Length == 2)
                    {
                        Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
                    }
                    else if (points.Length == 4)
                    {
                        Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[2]}]");
                    }
                }
            }
            */
            /*
            List<Entry> imlist = page.GetImages();
            foreach (Entry im in imlist) 
            {
                ImageInfo img = doc.ExtractImage(im.Xref);
                File.WriteAllBytes(@"copy.png", img.Image);

                List<Barcode> barcodes = Utils.ReadBarcodes(@"copy.png", new Rect(0,0,img.Width,img.Height));

                foreach (Barcode barcode in barcodes)
                {
                    BarcodePoint[] points = barcode.ResultPoints;
                    Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
                }
            }
            */

            page.Dispose();
            doc.Close();
        }


        internal static void TestReadBarcode(string[] args)
        {
            int i = 0;

            Console.WriteLine("\n=== TestReadBarcode =======================");

            Console.WriteLine("--- Read from image file ----------");
            string testFilePath1 = Path.GetFullPath("../../../TestDocuments/Barcodes/rendered.bmp");

            Rect rect1 = new Rect(1260, 390, 1720, 580);
            List<Barcode> barcodes2 = Utils.ReadBarcodes(testFilePath1, clip:rect1);

            i = 0;
            foreach (Barcode barcode in barcodes2)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }

            Console.WriteLine("--- Read from pdf file ----------");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/Barcodes/Samples.pdf");
            Document doc = new Document(testFilePath);

            Page page = doc[0];
            //Rect rect = new Rect(290, 590, 420, 660);
            List<Barcode> barcodes = page.ReadBarcodes();

            foreach (Barcode barcode in barcodes)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
            doc.Close();
        }

        internal static void TestReadQrCode(string[] args)
        {
            Console.WriteLine("\n=== TestReadQrCode =======================");
            int i = 0;
            /*
            Console.WriteLine("=== Read from image file =====================");
            string testFilePath1 = Path.GetFullPath("../../../TestDocuments/Barcodes/2.png");

            List<Barcode> barcodes2 = Utils.ReadBarcodes(testFilePath1, autoRotate:true);

            i = 0;
            foreach (Barcode barcode in barcodes2)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
            */
            ///*
            Console.WriteLine("--- Read from pdf file ----------");

            string testImagePath = @"test.png";
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Barcodes/input.pdf");
            Document doc = new Document(testFilePath);

            Page page = doc[0];
            page.RemoveRotation(); // remove rotation to read barcodes correctly

            // Apply 2x scale (both X and Y)
            var matrix = new Matrix(3.0f, 3.0f);

            // Render the page using the scaled matrix
            var pixmap = page.GetPixmap(matrix);

            pixmap.GammaWith(3.2f); // apply gamma correction to improve barcode detection

            pixmap.Save(testImagePath);

            /*
            Rect rect = new Rect(400, 700, page.Rect.X1, page.Rect.Y1);
            List<Barcode> barcodes = page.ReadBarcodes(rect);

            foreach (Barcode barcode in barcodes)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
            */

            pixmap.Dispose();
            doc.Close();

            List<Barcode> barcodes2 = Utils.ReadBarcodes(testImagePath);

            i = 0;
            foreach (Barcode barcode in barcodes2)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
            //*/
        }

        internal static void TestWriteBarcode(string[] args)
        {
            Console.WriteLine("\n=== TestWriteBarcode =======================");
            Console.WriteLine("--- Write to pdf file ----------");
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Blank.pdf");
            Document doc = new Document(testFilePath);
            Page page = doc[0];

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
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.QR, forceFitToRect:false, pureBarcode:false, marginLeft:0);

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
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.CODE39, forceFitToRect: false, pureBarcode: false, marginBottom: 0);

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
            page.WriteBarcode(rect, "12345678901234567890", BarcodeFormat.CODABAR, forceFitToRect: false, pureBarcode: true, marginBottom: 0);
            
            // DATA_MATRIX
            rect = new Rect(100, 620, 140, 660);
            page.WriteBarcode(rect, "01100000110419257000", BarcodeFormat.DM, forceFitToRect: false, pureBarcode: false, marginBottom: 0);

            doc.Save("barcode.pdf");

            Console.WriteLine($"Barcodes written to 'barcode.pdf' in: {page.Rect}");
            doc.Close();

            Console.WriteLine("--- Write to image file ----------");

            // QR_CODE
            Utils.WriteBarcode("QR_CODE.png", "Hello World!", BarcodeFormat.QR, width: 600, height: 600, forceFitToRect: true, pureBarcode: false, marginBottom: 0);

            // EAN_8
            Utils.WriteBarcode("EAN_8.png", "1234567", BarcodeFormat.EAN8, width: 300, height: 20, forceFitToRect: false, pureBarcode: false, marginBottom: 4);

            // EAN_13
            Utils.WriteBarcode("EAN_13.png", "123456789012", BarcodeFormat.EAN13, width: 300, height: 0, forceFitToRect: false, pureBarcode: false, marginBottom: 10);

            // UPC_A
            Utils.WriteBarcode("UPC_A.png", "123456789012", BarcodeFormat.UPC_A, width: 300, height: 20, forceFitToRect: false, pureBarcode: false, marginBottom: 10);

            // CODE_39
            Utils.WriteBarcode("CODE_39.png", "Hello World!", BarcodeFormat.CODE39, width: 300, height: 70, forceFitToRect: false, pureBarcode: false, marginBottom: 20);

            // CODE_128
            Utils.WriteBarcode("CODE_128.png", "Hello World!", BarcodeFormat.CODE128, width: 300, height: 150, forceFitToRect: false, pureBarcode: false, marginBottom: 20);

            // ITF
            Utils.WriteBarcode("ITF.png", "12345678901234567890", BarcodeFormat.I2OF5, width: 300, height: 120, forceFitToRect: false, pureBarcode: false, marginBottom: 20);

            // PDF_417
            Utils.WriteBarcode("PDF_417.png", "Hello World!", BarcodeFormat.PDF417, width: 300, height: 10, forceFitToRect: false, pureBarcode: false, marginBottom: 0);

            // CODABAR
            Utils.WriteBarcode("CODABAR.png", "12345678901234567890", BarcodeFormat.CODABAR, width: 300, height: 150, forceFitToRect: false, pureBarcode: false, marginBottom: 20);

            // DATA_MATRIX
            Utils.WriteBarcode("DATA_MATRIX.png", "01100000110419257000", BarcodeFormat.DM, width: 300, height: 300, forceFitToRect: false, pureBarcode: true, marginBottom: 1);

            Console.WriteLine("Barcodes written to image files in the current directory.");
        }

    }
}
