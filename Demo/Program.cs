using MuPDF.NET;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            AnnotationsFreeText1.Run(args);
            AnnotationsFreeText2.Run(args);
            TestHelloWorldToNewDocument(args);
            TestReadBarcode(args);
            TestWriteBarcode(args);
            TestHelloWorldToExistingDocument(args);
            TestExtractTextWithLayout(args);
            TestWidget(args);
            TestColor(args);
            TestReplaceImage(args);
            TestInsertImage(args);
            TestGetImageInfo(args);
            TestGetTextPageOcr(args);
            TestCreateImagePage(args);
            TestJoinPdfPages(args);
            TestFreeTextAnnot(args);
        }

        static void TestHelloWorldToNewDocument(string[] args)
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            //{ "helv", "Helvetica" },
            //{ "heit", "Helvetica-Oblique" },
            //{ "hebo", "Helvetica-Bold" },
            //{ "hebi", "Helvetica-BoldOblique" },
            //{ "cour", "Courier" },
            //{ "cobo", "Courier-Bold" },
            //{ "cobi", "Courier-BoldOblique" },
            //{ "tiro", "Times-Roman" },
            //{ "tibo", "Times-Bold" },
            //{ "tiit", "Times-Italic" },
            //{ "tibi", "Times-BoldItalic" },
            //{ "symb", "Symbol" },
            //{ "zadb", "ZapfDingbats" }
            MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(page.Rect);
            var ret = writer.FillTextbox(page.Rect, "Hello World!", new Font(fontName: "helv"), rtl: true);
            writer.WriteText(page);
            doc.Save("text.pdf", pretty: 1);
        }

        static void TestHelloWorldToExistingDocument(string[] args)
        {
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Blank.pdf");
            Document doc = new Document(testFilePath);
            
            Page page = doc[0];
            
            Rect rect = new Rect(100, 100, 510, 210);
            page.DrawRect(rect);
            
            MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(page.Rect);
            //Font font = new Font("kenpixel", "../../../kenpixel.ttf", isBold: 1);
            Font font = new Font("cobo", isBold: 0);
            var ret = writer.FillTextbox(page.Rect, "123456789012345678901234567890Peter Test- this is a string that is too long to fit into the TextBox", font, rtl: false);
            writer.WriteText(page);
            
            doc.Save("text1.pdf", pretty: 1);

            doc.Close();
        }

        static void TestReadBarcode(string[] args)
        {
            int i = 0;

            Console.WriteLine("=== Read from image file =====================");
            string testFilePath1 = Path.GetFullPath("../../../TestDocuments/Barcodes/rendered.bmp");

            Rect rect1 = new Rect(1260, 390, 1720, 580);
            List<Barcode> barcodes2 = Utils.ReadBarcodes(testFilePath1, rect1);

            i = 0;
            foreach (Barcode barcode in barcodes2)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
            
            Console.WriteLine("=== Read from pdf file =======================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/Barcodes/Sample1.pdf");
            Document doc = new Document(testFilePath);

            Page page = doc[0];
            Rect rect = new Rect(290, 590, 420, 660);
            List<Barcode> barcodes = page.ReadBarcodes(rect);

            foreach (Barcode barcode in barcodes)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
            doc.Close();
        }

        static void TestWriteBarcode(string[] args)
        {
            Console.WriteLine("=== Write to pdf file =====================");
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
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.QR_CODE, forceFitToRect:false, pureBarcode:false, margin:0);

            // EAN_8
            rect = new Rect(100, 100, 300, 145);
            page.WriteBarcode(rect, "1234567", BarcodeFormat.EAN_8, forceFitToRect: false, pureBarcode: false, margin: 0);

            // EAN_13
            rect = new Rect(100, 155, 300, 200);
            page.WriteBarcode(rect, "123456789012", BarcodeFormat.EAN_13, forceFitToRect: true, pureBarcode: true, margin: 0);

            // UPC_A
            rect = new Rect(100, 210, 300, 255);
            page.WriteBarcode(rect, "123456789012", BarcodeFormat.UPC_A, forceFitToRect: false, pureBarcode: true, margin: 0);

            // CODE_39
            rect = new Rect(100, 265, 300, 310);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.CODE_39, forceFitToRect: true, pureBarcode: false, margin: 0);

            // CODE_128
            rect = new Rect(100, 320, 300, 365);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.CODE_128, forceFitToRect: true, pureBarcode: true, margin: 0);

            // ITF
            rect = new Rect(100, 375, 300, 420);
            page.WriteBarcode(rect, "12345678901234567890", BarcodeFormat.ITF, forceFitToRect: false, pureBarcode: false, margin: 0);

            // PDF_417
            rect = new Rect(100, 430, 400, 520);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.PDF_417, forceFitToRect: false, pureBarcode: true, margin: 0);

            // CODABAR
            rect = new Rect(100, 540, 300, 600);
            page.WriteBarcode(rect, "12345678901234567890", BarcodeFormat.CODABAR, forceFitToRect: true, pureBarcode: true, margin: 0);
            
            // DATA_MATRIX
            rect = new Rect(100, 620, 140, 660);
            page.WriteBarcode(rect, "01100000110419257000", BarcodeFormat.DATA_MATRIX, forceFitToRect: false, pureBarcode: false, margin: 0);

            doc.Save("barcode.pdf");

            Console.WriteLine("Done");
            doc.Close();

            Console.WriteLine("=== Write to image file =====================");

            // QR_CODE
            Utils.WriteBarcode("QR_CODE.png", "Hello World!", BarcodeFormat.QR_CODE, width: 400, height: 300, forceFitToRect: false, pureBarcode: true, margin: 3);

            // EAN_8
            Utils.WriteBarcode("EAN_8.png", "1234567", BarcodeFormat.EAN_8, width:300, height:200, forceFitToRect: true, pureBarcode: true, margin: 3);

            // EAN_13
            Utils.WriteBarcode("EAN_13.png", "123456789012", BarcodeFormat.EAN_13, width: 300, height: 208, forceFitToRect: true, pureBarcode: true, margin: 3);

            // UPC_A
            Utils.WriteBarcode("UPC_A.png", "123456789012", BarcodeFormat.UPC_A, width: 300, height: 208, forceFitToRect: true, pureBarcode: true, margin: 3);

            // CODE_39
            Utils.WriteBarcode("CODE_39.png", "Hello World!", BarcodeFormat.CODE_39, width: 300, height: 150, forceFitToRect: false, pureBarcode: true, margin: 3);

            // CODE_128
            Utils.WriteBarcode("CODE_128.png", "Hello World!", BarcodeFormat.CODE_128, width: 300, height: 150, forceFitToRect: true, pureBarcode: false, margin: 3);

            // ITF
            Utils.WriteBarcode("ITF.png", "12345678901234567890", BarcodeFormat.ITF, width: 300, height: 120, forceFitToRect: true, pureBarcode: true, margin: 3);

            // PDF_417
            Utils.WriteBarcode("PDF_417.png", "Hello World!", BarcodeFormat.PDF_417, width: 300, height: 150, forceFitToRect: false, pureBarcode: true, margin: 3);

            // CODABAR
            Utils.WriteBarcode("CODABAR.png", "12345678901234567890", BarcodeFormat.CODABAR, width: 300, height: 150, forceFitToRect: true, pureBarcode: true, margin: 3);

            // DATA_MATRIX
            Utils.WriteBarcode("DATA_MATRIX.png", "01100000110419257000", BarcodeFormat.DATA_MATRIX, width: 300, height: 300, forceFitToRect: false, pureBarcode: true, margin: 1);

            Console.WriteLine("Done");
        }

        static void TestExtractTextWithLayout(string[] args)
        {
            Console.WriteLine("=== Extract text with layout =====================");
            string testFilePath = Path.GetFullPath("../../../TestDocuments/columns.pdf");
            Document doc = new Document(testFilePath);

            FileStream wstream = File.Create("columns.txt");

            for (int i = 0; i < 1/*doc.PageCount*/; i++)
            {
                Page page = doc[i];
                string textWithLayout = page.GetTextWithLayout(tolerance: 3);
                if (!string.IsNullOrEmpty(textWithLayout))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(textWithLayout);
                    wstream.Write(bytes, 0, bytes.Length);
                }
            }

            wstream.Close();

            doc.Close();

            Console.WriteLine("Created columns.txt file");
        }

        static void TestWidget(string[] args)
        {
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Widget.pdf");
            Document doc = new Document(testFilePath);
            for (int i = 0; i < 1; i++)
            {
                var page = doc[i];

                List<Entry> entries = page.GetXObjects();

                Widget fWidget = page.FirstWidget;
                while (fWidget != null)
                {
                    Console.WriteLine($"Widget: {fWidget}");
                    Console.WriteLine($"FieldName: {fWidget.FieldName}");
                    Console.WriteLine($"FieldType: {fWidget.FieldType}");
                    Console.WriteLine($"FieldValue: {fWidget.FieldValue}");
                    Console.WriteLine($"FieldFlags: {fWidget.FieldFlags}");
                    Console.WriteLine($"FieldLabel: {fWidget.FieldLabel}");
                    Console.WriteLine($"TextFont: {fWidget.TextFont}");
                    Console.WriteLine($"TextFontSize: {fWidget.TextFontSize}");
                    Console.WriteLine($"TextColor: {string.Join(",", fWidget.TextColor)}");
                    fWidget = (Widget)fWidget.Next;
                }

                foreach (var widget in page.GetWidgets())
                {
                    Console.WriteLine($"Widget: {widget}");
                    Console.WriteLine($"FieldName: {widget.FieldName}");
                    Console.WriteLine($"FieldType: {widget.FieldType}");
                    Console.WriteLine($"FieldValue: {widget.FieldValue}");
                    Console.WriteLine($"FieldFlags: {widget.FieldFlags}");
                    Console.WriteLine($"FieldLabel: {widget.FieldLabel}");
                    Console.WriteLine($"TextFont: {widget.TextFont}");
                    Console.WriteLine($"TextFontSize: {widget.TextFontSize}");
                    Console.WriteLine($"TextColor: {string.Join(",", widget.TextColor)}");

                }
            }

            doc.Close();
        }

        static void TestColor(string[] args)
        {
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Color.pdf");
            Document doc = new Document(testFilePath);
            List<Entry> images = doc.GetPageImages(0);
            Console.WriteLine($"CaName: {images[0].CsName}");
            doc.Recolor(0, 4);
            images = doc.GetPageImages(0);
            Console.WriteLine($"CaName: {images[0].AltCsName}");
            doc.Save("ReColor.pdf");
            doc.Close();
        }

        static void TestReplaceImage(string[] args)
        {
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Color.pdf");
            Document doc = new Document(testFilePath);
            Page page = doc[0];
            List<Entry> images = page.GetImages(true);
            List<Box> imgs = page.GetImageRects(images[0].Xref);

            List<Block> infos = page.GetImageInfo(xrefs: true);

            page.ReplaceImage(images[0].Xref, "../../../TestDocuments/Image/_apple.png");
            page.ReplaceImage(images[0].Xref, "../../../TestDocuments/Image/_bb-logo.png");

            infos = page.GetImageInfo(xrefs: true);
            //page.DeleteImage(images[0].Xref);

            //int newXref = page.InsertImage(imgs[0].Rect, "../../../TestDocuments/Sample.png");

            //images = page.GetImages(true);
            //imgs = page.GetImageRects(images[0].Xref);

            //page.ReplaceImage(infos[0].Xref, "../../../TestDocuments/Sample.png");
            //page.DeleteImage(images[0].Xref);

            //page.InsertImage(imgs[0].Rect, "../../../TestDocuments/Sample.jpg");

            doc.Save("ReplaceImage.pdf");
            doc.Close();
        }

        static void TestInsertImage(string[] args)
        {
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Image/test.pdf");
            Document doc = new Document(testFilePath);
            Page page = doc[0];

            var pixmap1 = new Pixmap("../../../TestDocuments/Image/_apple.png");
            //var pixmap1 = new Pixmap("../../../TestDocuments/Image/30mb.jpg");
            var pixmap2 = new Pixmap("../../../TestDocuments/Image/_bb-logo.png");
            var imageRect1 = new Rect(0, 0, 100, 100);
            var imageRect2 = new Rect(100, 100, 200, 200);
            var imageRect3 = new Rect(100, 200, 200, 300);
            var imageRect4 = new Rect(100, 300, 200, 400);
            var imageRect5 = new Rect(100, 400, 200, 500);
            var imageRect6 = new Rect(100, 500, 200, 600);

            var img_xref = page.InsertImage(imageRect1, pixmap: pixmap1);
            Console.WriteLine(img_xref);

            //img_xref = page.InsertImage(imageRect2, "../../../TestDocuments/Image/_apple.png");
            img_xref = page.InsertImage(imageRect2, pixmap: pixmap1);
            Console.WriteLine(img_xref);
            img_xref = page.InsertImage(imageRect3, pixmap: pixmap2);
            Console.WriteLine(img_xref);
            img_xref = page.InsertImage(imageRect4, "../../../TestDocuments/Image/_bb-logo.png");
            Console.WriteLine(img_xref);
            page.InsertImage(imageRect5, xref: img_xref);
            Console.WriteLine(img_xref);
            page.InsertImage(imageRect6, xref: img_xref);

            doc.Save("TestInsertImage.pdf");
            doc.Close();
        }

        static void TestGetImageInfo(string[] args)
        {
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Image/TestInsertImage.pdf");
            Document doc = new Document(testFilePath);
            Page page = doc[0];

            List<Block> infos = page.GetImageInfo(xrefs: true);

            doc.Close();
        }

        static void TestGetTextPageOcr(string[] args)
        {
            string testFilePath = Path.GetFullPath(@"../../../TestDocuments/Ocr.pdf");
            Document doc = new Document(testFilePath);
            Page page = doc[0];

            List<Block> blocks = page.GetText("dict", flags: (int)TextFlags.TEXT_PRESERVE_IMAGES)?.Blocks;
            foreach (Block block in blocks)
            {
                Console.WriteLine(block.Image.Length);
            }

            TextPage tp = page.GetTextPageOcr((int)TextFlags.TEXT_PRESERVE_SPANS, full: true);
            string txt = tp.ExtractText();
            Console.WriteLine(txt);

            doc.Close();
        }

        static void TestCreateImagePage(string[] args)
        {
            Pixmap pxmp = new Pixmap("../../../TestDocuments/Image/_bb-logo.png");

            Document doc = new Document();
            Page page = doc.NewPage(width:pxmp.W, height:pxmp.H);
            
            page.InsertImage(page.Rect, pixmap: pxmp);

            pxmp.Dispose();

            doc.Save("_bb-logo.pdf", pretty: 1);
            doc.Close();
        }

        static void TestJoinPdfPages(string[] args)
        {
            string testFilePath1 = Path.GetFullPath(@"../../../TestDocuments/Widget.pdf");
            Document doc1 = new Document(testFilePath1);
            string testFilePath2 = Path.GetFullPath(@"../../../TestDocuments/Color.pdf");
            Document doc2 = new Document(testFilePath2);

            doc1.InsertPdf(doc2, 0, 0, 2);

            doc1.Save("Joined.pdf", pretty: 1);

            doc2.Close();
            doc1.Close();
        }

        static void TestFreeTextAnnot(string[] args)
        {
            Rect r = new Rect(72, 72, 220, 100);
            string t1 = "têxt üsès Lätiñ charß,\nEUR: €, mu: µ, super scripts: ²³!";
            Rect rect = new Rect(100,100,200,200);
            float[] red = new float[] { 1, 0, 0 };
            float[] blue = new float[] { 0, 0, 1 };
            float[] gold = new float[] { 1, 1, 0 };
            float[] green = new float[] { 0, 1, 0 };
            float[] white = new float[] { 1, 1, 1 };

            Document doc = new Document();
            Page page = doc.NewPage();

            Annot annot = page.AddFreeTextAnnot(
                rect,
                t1,
                fontSize: 10,
                rotate: 90,
                textColor: red,
                fillColor: gold,
                align: (int)TextAlign.TEXT_ALIGN_CENTER,
                dashes: new int[] { 2 }
            );

            annot.SetBorder(border: null, width: 0.3f, dashes: new int[] { 2 });
            annot.Update(textColor: blue);
            //annot.Update(textColor: red, fillColor: blue);

            doc.Save("FreeTextAnnot.pdf");

            doc.Close();
        }
    }
}
