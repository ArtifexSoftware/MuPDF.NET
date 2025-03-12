using MuPDF.NET;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            TestHelloWorldToNewDocument(args);
            TestReadBarcode(args);
            TestWriteBarcode(args);
            TestHelloWorldToExistingDocument(args);
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
            //{ "coit", "Courier-Obliqu" },
            //{ "cobo", "Courier-Bold" },
            //{ "cobi", "Courier-BoldOblique" },
            //{ "tiro", "Times-Roman" },
            //{ "tibo", "Times-Bold" },
            //{ "tiit", "Times-Italic" },
            //{ "tibi", "Times-BoldItalic" },
            //{ "symb", "Symbol" },
            //{ "zadb", "ZapfDingbats" }
            MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(page.Rect);
            writer.FillTextbox(page.Rect, "Hello World!", new Font(fontName: "helv"), rtl: true);
            writer.WriteText(page);
            doc.Save("text.pdf", pretty: 1);
        }

        static void TestHelloWorldToExistingDocument(string[] args)
        {
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Barcodes/Blank.pdf");
            Document doc = new Document(testFilePath);
            
            Page page = doc[0];
            
            Rect rect = new Rect(100, 100, 200, 200);
            page.DrawRect(rect);
            
            MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(page.Rect);
            //Font font = new Font("kenpixel", "../../../kenpixel.ttf", isBold: 1);
            Font font = new Font("tiro", isBold: 0);
            writer.FillTextbox(rect, "Hello World!", font, rtl: false);
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
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Barcodes/Blank.pdf");
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
            writer.WriteText(page);

            // QR_CODE
            Rect rect = new Rect(100, 0, 190, 90);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.QR_CODE);

            // EAN_8
            rect = new Rect(100, 100, 190, 145);
            page.WriteBarcode(rect, "1234567", BarcodeFormat.EAN_8);

            // EAN_13
            rect = new Rect(100, 155, 220, 200);
            page.WriteBarcode(rect, "123456789012", BarcodeFormat.EAN_13);

            // UPC_A
            rect = new Rect(100, 210, 220, 255);
            page.WriteBarcode(rect, "123456789012", BarcodeFormat.UPC_A);

            // CODE_39
            rect = new Rect(100, 265, 300, 310);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.CODE_39);

            // CODE_128
            rect = new Rect(100, 320, 220, 365);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.CODE_128);

            // ITF
            rect = new Rect(100, 375, 220, 420);
            page.WriteBarcode(rect, "12345678901234567890", BarcodeFormat.ITF);

            // PDF_417
            rect = new Rect(100, 430, 310, 500);
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.PDF_417);

            // CODABAR
            rect = new Rect(100, 510, 310, 600);
            page.WriteBarcode(rect, "12345678901234567890", BarcodeFormat.CODABAR);

            doc.Save("barcode.pdf");

            Console.WriteLine("Done");
            doc.Close();

            Console.WriteLine("=== Write to image file =====================");

            // QR_CODE
            Utils.WriteBarcode("QR_CODE.png", "Hello World!", BarcodeFormat.QR_CODE, width: 300, height: 300);

            // EAN_8
            Utils.WriteBarcode("EAN_8.png", "1234567", BarcodeFormat.EAN_8, width:300, height:200);

            // EAN_13
            Utils.WriteBarcode("EAN_13.png", "123456789012", BarcodeFormat.EAN_13, width: 300, height: 208);

            // UPC_A
            Utils.WriteBarcode("UPC_A.png", "123456789012", BarcodeFormat.UPC_A, width: 300, height: 208);

            // CODE_39
            Utils.WriteBarcode("CODE_39.png", "Hello World!", BarcodeFormat.CODE_39, width: 300, height: 150);

            // CODE_128
            Utils.WriteBarcode("CODE_128.png", "Hello World!", BarcodeFormat.CODE_128, width: 300, height: 150);

            // ITF
            Utils.WriteBarcode("ITF.png", "12345678901234567890", BarcodeFormat.ITF, width: 300, height: 120);

            // PDF_417
            Utils.WriteBarcode("PDF_417.png", "Hello World!", BarcodeFormat.PDF_417, width: 300, height: 150);

            // CODABAR
            Utils.WriteBarcode("CODABAR.png", "12345678901234567890", BarcodeFormat.CODABAR, width: 300, height: 150);

            Console.WriteLine("Done");
        }
    }
}
