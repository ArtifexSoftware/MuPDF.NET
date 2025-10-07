﻿using MuPDF.NET;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using File = System.IO.File;
using Font = MuPDF.NET.Font;
using TextWriter = MuPDF.NET.TextWriter;

namespace Demo
{
    public static class Units
    {
        // Constants
        public const float InchesPerMm = 1.0f / 25.4f;
        public const float PointsPerInch = 72.0f;

        // --- mm <-> points (PostScript points: 1 pt = 1/72 in) ---
        public static float MmToPoints(float mm) => mm * InchesPerMm * PointsPerInch;          // = mm * 72 / 25.4
        public static float PointsToMm(float points) => points / PointsPerInch / InchesPerMm;  // = points * 25.4 / 72

        // --- mm <-> pixels (requires device DPI) ---
        public static float MmToPixels(float mm, float dpi) => mm * InchesPerMm * dpi;
        public static float PixelsToMm(float px, float dpi) => px / dpi / InchesPerMm;
    }
    class Program
    {
        static void Main(string[] args)
        {
            //TestInsertHtmlbox();
            //TestLineAnnot();
            //AnnotationsFreeText1.Run(args);
            //AnnotationsFreeText2.Run(args);
            //NewAnnots.Run(args);
            //TestHelloWorldToNewDocument(args);
            //TestHelloWorldToExistingDocument(args);
            //TestReadBarcode(args);
            //TestReadDataMatrix();
            //TestWriteBarcode(args);
            //TestExtractTextWithLayout(args);
            //TestWidget(args);
            //TestColor(args);
            //TestCMYKRecolor(args);
            //TestSVGRecolor(args);
            //TestReplaceImage(args);
            //TestInsertImage(args);
            //TestGetImageInfo(args);
            //TestGetTextPageOcr(args);
            //TestCreateImagePage(args);
            //TestJoinPdfPages(args);
            //TestFreeTextAnnot(args);
            //TestTextFont(args);
            //TestMemoryLeak();
            //TestDrawLine();
            //TestReadBarcode1();
            //TestWriteBarcode1();
            //TestCMYKRecolor1(args);
            TestDocument();
            //TestMerph();

            return;
        }

        static void TestMerph()
        {
            string testFilePath = @"E:\MuPDF.NET\Tmp\Peter\morph\test.pdf";

            Document doc = new Document(testFilePath);
            Page page = doc[0];
            Rect printrect = new Rect(180, 30, 650, 60);
            int pagerot = page.Rotation;
            TextWriter pw = new TextWriter(page.TrimBox);
            string txt = "Origin 100.100";
            pw.Append(new Point(100, 100), txt, new Font("tiro"), fontSize: 24);
            pw.WriteText(page);

            txt = "rotated 270 - 100.100";
            Matrix matrix = new IdentityMatrix();
            matrix.Prerotate(270);
            Morph mo = new Morph(new Point(100, 100), matrix);
            pw = new TextWriter(page.TrimBox);
            pw.Append(new Point(100, 100), txt, new Font("tiro"), fontSize: 24);
            pw.WriteText(page, morph:mo);
            page.SetRotation(270);

            /*
            string text = "TextWriter:Rotate Me";
            Font font = new Font("tiro");
            TextWriter tw = new TextWriter(page.Rect);
            tw.Append(new(200, 200), text, font);
            Matrix matrix = new IdentityMatrix();
            matrix.Prerotate(90);
            Morph morph = new Morph(new(200, 200), matrix);
            tw.WriteText(page, morph: morph);
            */

            page.Dispose();
            doc.Save(@"E:\MuPDF.NET\Tmp\Peter\morph\output.pdf");
            doc.Close();
        }

        static void TestDocument()
        {
            Console.WriteLine("\n=== TestDocument =====================");

            string testFilePath = @"e:\你好.pdf";

            Document doc = new Document(testFilePath);
            //List<Entry> images = doc.GetPageImages(0);
            //Console.WriteLine($"CaName: {images[0].CsName}");
            //doc.Recolor(0, "CMYK");
            //images = doc.GetPageImages(0);
            //Console.WriteLine($"CaName: {images[0].AltCsName}");
            doc.Save(@"e:\CMYKRecolor.pdf");
            doc.Close();

            Console.WriteLine("TestDocument completed.");
        }

        static void TestCMYKRecolor1(string[] args)
        {
            Console.WriteLine("\n=== TestCMYKRecolor =====================");

            string testFilePath = "../../../TestDocuments/CMYK_Recolor1.pdf";
            Document doc = new Document(testFilePath);
            //List<Entry> images = doc.GetPageImages(0);
            //Console.WriteLine($"CaName: {images[0].CsName}");
            doc.Recolor(0, "CMYK");
            //images = doc.GetPageImages(0);
            //Console.WriteLine($"CaName: {images[0].AltCsName}");
            doc.Save(@"CMYKRecolor.pdf");
            doc.Close();

            Console.WriteLine("CMYK Recolor test completed.");
        }

        static void TestWriteBarcode1()
        {
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Blank.pdf");
            Document doc = new Document(testFilePath);

            Page page = doc[0];
            /*
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
            */
            /*
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
            */
            /*
            Rect rect2 = new Rect(
                X0: Units.MmToPoints(100),
                X1: Units.MmToPoints(140),
                Y0: Units.MmToPoints(40),
                Y1: Units.MmToPoints(80));

            page.WriteBarcode(rect2, "01030000110444408000", BarcodeFormat.DM, forceFitToRect: false, pureBarcode: true, narrowBarWidth: 3);
            */

            Pixmap pxmp = Utils.GetBarcodePixmap("JJBEA6500063000000177922", BarcodeFormat.CODE128, width: 500, pureBarcode: true, marginLeft:0, marginTop:0, marginRight:0, marginBottom:0, narrowBarWidth: 1);
            
            pxmp.Save(@"e:\PxmpBarcode3.png");

            /*
            byte[] imageBytes = pxmp.ToBytes();

            using var stream = new SKMemoryStream(imageBytes);
            using var codec = SKCodec.Create(stream);
            var info = codec.Info;
            var bitmap = SKBitmap.Decode(codec);

            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100); // 100 = quality
            using var stream1 = File.OpenWrite(@"output.png");
            data.SaveTo(stream1);

            doc.Save(@"TestWriteBarcode1.pdf");
            */

            page.Dispose();
            doc.Close();
        }

        static void TestReadBarcode1()
        {
            string testFilePath = @"../../../TestDocuments/Barcodes/Low/read-test-with-barcodes.pdf";
            Document doc = new Document(testFilePath);

            int i = 0;
            Page page = doc[0];

            List<Barcode> barcodes = page.ReadBarcodes(type:BarcodeFormat.ALL);

            foreach (Barcode barcode in barcodes)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"{ i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
        }

        static void TestReadDataMatrix()
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

        static void TestMemoryLeak()
        {
            Console.WriteLine("\n=== TestMemoryLeak =======================");
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Blank.pdf");

            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document(testFilePath);
                Page page = doc.NewPage();
                page.Dispose();
                doc.Close();
            }

            Console.WriteLine("Memory leak test completed. No leaks should be detected.");
        }

        static void DrawLine(Page page, float startX, float startY, float endX, float endY, Color lineColor = null, float lineWidth = 1, bool dashed = false)
        {
            Console.WriteLine("\n=== DrawLine =======================");

            if (lineColor == null)
            {
                lineColor = new Color(); // Default to black
                lineColor.Stroke = new float[] { 0, 0, 0 }; // RGB black
            }
            Shape img = page.NewShape();
            Point startPoint = new Point(startX, startY);
            Point endPoint = new Point(endX, endY);

            String dashString = "";
            if (dashed == true)
            {
                dashString = "[2] 0"; // Example dash pattern
            }

            img.DrawLine(startPoint, endPoint);
            img.Finish(width: lineWidth, color: lineColor.Stroke, dashes: dashString);
            img.Commit();

            Console.WriteLine($"Line drawn from ({startX}, {startY}) to ({endX}, {endY}) with color {lineColor.Stroke} and width {lineWidth}.");
        }

        static void TestDrawLine()
        {
            Console.WriteLine("\n=== TestDrawLine =======================");

            Document doc = new Document();

            Page page = doc.NewPage();

            string fontDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

            page.DrawLine(new Point(45, 50), new Point(80, 50), width: 0.5f, dashes: "[5] 0");
            page.DrawLine(new Point(90, 50), new Point(150, 50), width: 0.5f, dashes: "[5] 0");
            page.DrawLine(new Point(45, 80), new Point(180, 80), width: 0.5f, dashes: "[5] 0");
            page.DrawLine(new Point(45, 100), new Point(180, 100), width: 0.5f, dashes: "[5] 0");

            //DrawLine(page, 45, 50, 80, 50, lineWidth: 0.5f, dashed: true);
            //DrawLine(page, 90, 60, 150, 60, lineWidth: 0.5f, dashed: true);
            //DrawLine(page, 45, 80, 180, 80, lineWidth: 0.5f, dashed: true);
            //DrawLine(page, 45, 100, 180, 100, lineWidth: 0.5f, dashed: true);

            doc.Save(@"TestDrawLine.pdf");

            page.Dispose();
            doc.Close();

            Console.WriteLine("Write to TestDrawLine.pdf");
        }

        static void TestTextFont(string[] args)
        {
            Console.WriteLine("\n=== TestTextFont =======================");
            //for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();

                Page page0 = doc.NewPage();
                Page page1 = doc.NewPage(pno: -1, width: 595, height: 842);

                string fontDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

                float[] blue = new float[] { 0.0f, 0.0f, 1.0f };
                float[] red = new float[] { 1.0f, 0.0f, 0.0f };

                Rect rect1 = new Rect(100, 100, 510, 200);
                Rect rect2 = new Rect(100, 250, 300, 400);

                MuPDF.NET.Font font1 = new MuPDF.NET.Font("asdfasdf");
                //MuPDF.NET.Font font1 = new MuPDF.NET.Font("arial", fontDir+"\\arial_0.ttf");
                MuPDF.NET.Font font2 = new MuPDF.NET.Font("times", fontDir + "\\times.ttf");

                string text1 = "This is a test of the FillTextbox method with Arial font.";
                string text2 = "This is another test with Times New Roman font.";

                MuPDF.NET.TextWriter tw1 = new MuPDF.NET.TextWriter(page0.Rect);
                tw1.FillTextbox(rect: rect1, text: text1, font: font1, fontSize:20);
                font1.Dispose();
                tw1.WriteText(page0);

                MuPDF.NET.TextWriter tw2 = new MuPDF.NET.TextWriter(page0.Rect, color: red);
                tw2.FillTextbox(rect: rect2, text: text2, font: font2, fontSize: 10, align: (int)TextAlign.TEXT_ALIGN_LEFT);
                font2.Dispose();
                tw2.WriteText(page0);

                doc.Save(@"TestTextFont.pdf");

                page0.Dispose();
                doc.Close();

                Console.WriteLine("Write to TestTextFont.pdf");
            }

        }

        static void TestInsertHtmlbox()
        {
            Console.WriteLine("\n=== TestInsertHtmlbox =======================");

            Rect rect = new Rect(100, 100, 550, 2250);
            Document doc = new Document();
            Page page = doc.NewPage();

            string htmlString = "<html><body style=\"text-align:Left;font-family:Segoe UI;font-style:normal;font-weight:normal;font-size:12;color:#000000;\"><p><span>生产准备：</span></p><p><span>1. 每日生产进行维护保养，请参照并填写Philips 自动螺丝起点检表《WI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01》</span></p><p><span>2 .扭力计UNIT选择‘lbf.in’，‘P-P’模式，每四小时检查一次，每次检查5组数据，只有合格才可以生产；并填写</span></p><p><span>力扭矩记录表，表单号 ： F-EN-34 。</span></p><p><span>1.电动起子力矩： 5&#177;1 in-lbs，电动螺丝起编号：5.0。</span></p><p><span>2.电动起子力矩：10&#177;1 in-lbs，电动螺丝起编号：10.0。</span></p><p><span>3.电动起子力矩：12&#177;1 in-lbs，电动螺丝起编号：12.0。</span></p><p><span /></p><p><span>Colten - line break</span></p><p><span /></p><p><span>生产准备：</span></p><p><span>1. 每日生产进行维护保养，请参照并填写Philips 自动螺丝起点检表《WI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01》</span></p><p><span>2 .扭力计UNIT选择‘lbf.in’，‘P-P’模式，每四小时检查一次，每次检查5组数据，只有合格才可以生产；并填写</span></p><p><span>力扭矩记录表，表单号 ： F-EN-34 。</span></p><p><span>1.电动起子力矩： 5&#177;1 in-lbs，电动螺丝起编号：5.0。</span></p><p><span>2.电动起子力矩：10&#177;1 in-lbs，电动螺丝起编号：10.0。</span></p><p><span>3.电动起子力矩：12&#177;1 in-lbs，电动螺丝起编号：12.0。</span></p><p><span> </span></p><p><span>Colten - line break</span></p><p><span /></p><p><span>生产准备：</span></p><p><span>1. 每日生产进行维护保养，请参照并填写Philips 自动螺丝起点检表《WI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01》</span></p><p><span>2 .扭力计UNIT选择‘lbf.in’，‘P-P’模式，每四小时检查一次，每次检查5组数据，只有合格才可以生产；并填写</span></p><p><span>力扭矩记录表，表单号 ： F-EN-34 。</span></p><p><span>1.电动起子力矩： 5&#177;1 in-lbs，电动螺丝起编号：5.0。</span></p><p><span>2.电动起子力矩：10&#177;1 in-lbs，电动螺丝起编号：10.0。</span></p><p><span>3.电动起子力矩：12&#177;1 in-lbs，电动螺丝起编号：12.0。</span></p><p><span /></p><p><span>Colten - line break</span></p><p><span /></p><p><span>生产准备：</span></p><p><span>1. 每日生产进行维护保养，请参照并填写Philips 自动螺丝起点检表《WI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01》</span></p><p><span>2 .扭力计UNIT选择‘lbf.in’，‘P-P’模式，每四小时检查一次，每次检查5组数据，只有合格才可以生产；并填写</span></p><p><span>力扭矩记录表，表单号 ： F-EN-34 。</span></p><p><span>1.电动起子力矩： 5&#177;1 in-lbs，电动螺丝起编号：5.0。</span></p><p><span>2.电动起子力矩：10&#177;1 in-lbs，电动螺丝起编号：10.0。</span></p><p><span>3.电动起子力矩：12&#177;1 in-lbs，电动螺丝起编号：12.0。</span></p><p><span /></p></body></html>";
            (float s, float scale) = page.InsertHtmlBox(rect, htmlString, scaleLow: 0f);
            doc.Save(@"TestInsertHtmlbox.pdf");

            page.Dispose();
            doc.Close();

            Console.WriteLine($"Inserted HTML box with scale: {scale} and size: {s}");
        }

        static void TestLineAnnot()
        {
            Console.WriteLine("\n=== TestLineAnnot =======================");
            Document newDoc = new Document();
            Page newPage = newDoc.NewPage();

            newPage.AddLineAnnot(new Point(100, 100), new Point(300, 300));

            newDoc.Save(@"TestLineAnnot1.pdf");
            newDoc.Close();

            Document doc = new Document(@"TestLineAnnot1.pdf"); // open a document
            List<Annot> annotationsToUpdate = new List<Annot>();
            Page page = doc[0];
            // Fix: Correctly handle the IEnumerable<Annot> returned by GetAnnots()
            IEnumerable<Annot> annots = page.GetAnnots();
            foreach (Annot annot in annots)
            {
                Console.WriteLine("Annotation on page width before modified: " + annot.Border.Width);
                annot.SetBorder(width: 8);
                annot.Update();
                Console.WriteLine("Annotation on page width after modified: " + annot.Border.Width);
            }
            annotationsToUpdate.Clear();
            doc.Save(@"TestLineAnnot2.pdf"); // Save the modified document
            doc.Close(); // Close the document
        }

        static void TestHelloWorldToNewDocument(string[] args)
        {
            Console.WriteLine("\n=== TestHelloWorldToNewDocument =======================");
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
            var ret = writer.FillTextbox(page.Rect, "Hello World!", new MuPDF.NET.Font(fontName: "helv"), rtl: true);
            writer.WriteText(page);
            doc.Save("text.pdf", pretty: 1);
            doc.Close();

            Console.WriteLine($"Text written to 'text.pdf' in: {page.Rect}");
        }

        static void TestHelloWorldToExistingDocument(string[] args)
        {
            Console.WriteLine("\n=== TestHelloWorldToExistingDocument =======================");
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

            Console.WriteLine($"Text written to 'text1.pdf' in: {page.Rect}");
        }

        static void TestReadBarcode(string[] args)
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

        static void TestReadQrCode(string[] args)
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

        static void TestWriteBarcode(string[] args)
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
            page.WriteBarcode(rect, "Hello World!", BarcodeFormat.CODE39, forceFitToRect: true, pureBarcode: false, marginBottom: 0);

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

        static void TestExtractTextWithLayout(string[] args)
        {
            Console.WriteLine("\n=== TestExtractTextWithLayout =====================");
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
            Console.WriteLine("\n=== TestWidget =====================");

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
            Console.WriteLine("Widget test completed.");
        }

        static void TestColor(string[] args)
        {
            Console.WriteLine("\n=== TestColor =====================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/Color.pdf");
            Document doc = new Document(testFilePath);
            List<Entry> images = doc.GetPageImages(0);
            Console.WriteLine($"CaName: {images[0].CsName}");
            doc.Recolor(0, 4);
            images = doc.GetPageImages(0);
            Console.WriteLine($"CaName: {images[0].AltCsName}");
            doc.Save("ReColor.pdf");
            doc.Close();

            Console.WriteLine("Color test completed.");
        }

        static void TestCMYKRecolor(string[] args)
        {
            Console.WriteLine("\n=== TestCMYKRecolor =====================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/CMYK_Recolor.pdf");
            Document doc = new Document(testFilePath);
            //List<Entry> images = doc.GetPageImages(0);
            //Console.WriteLine($"CaName: {images[0].CsName}");
            doc.Recolor(0, "CMYK");
            //images = doc.GetPageImages(0);
            //Console.WriteLine($"CaName: {images[0].AltCsName}");
            doc.Save("CMYKRecolor.pdf");
            doc.Close();

            Console.WriteLine("CMYK Recolor test completed.");
        }

        static void TestSVGRecolor(string[] args)
        {
            Console.WriteLine("\n=== TestSVGRecolor =====================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/SvgTest.pdf");
            Document doc = new Document(testFilePath);
            doc.Recolor(0, "RGB");
            doc.Save("SVGRecolor.pdf");
            doc.Close();

            Console.WriteLine("SVG Recolor test completed.");
        }

        static void TestReplaceImage(string[] args)
        {
            Console.WriteLine("\n=== TestReplaceImage =====================");

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

            Console.WriteLine("Image replacement test completed.");
        }

        static void TestInsertImage(string[] args)
        {
            Console.WriteLine("\n=== TestInsertImage =====================");

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

            Console.WriteLine("Image insertion test completed.");
        }

        static void TestGetImageInfo(string[] args)
        {
            Console.WriteLine("\n=== TestGetImageInfo =====================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/Image/TestInsertImage.pdf");
            Document doc = new Document(testFilePath);
            Page page = doc[0];

            List<Block> infos = page.GetImageInfo(xrefs: true);

            doc.Close();

            Console.WriteLine("Image info test completed.");
        }

        static void TestGetTextPageOcr(string[] args)
        {
            Console.WriteLine("\n=== TestGetTextPageOcr =====================");

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

            Console.WriteLine("OCR text extraction test completed.");
        }

        static void TestCreateImagePage(string[] args)
        {
            Console.WriteLine("\n=== TestCreateImagePage =====================");

            Pixmap pxmp = new Pixmap("../../../TestDocuments/Image/_bb-logo.png");

            Document doc = new Document();
            Page page = doc.NewPage(width:pxmp.W, height:pxmp.H);
            
            page.InsertImage(page.Rect, pixmap: pxmp);

            pxmp.Dispose();

            doc.Save("_bb-logo.pdf", pretty: 1);
            doc.Close();

            Console.WriteLine("Image page creation test completed.");
        }

        static void TestJoinPdfPages(string[] args)
        {
            Console.WriteLine("\n=== TestJoinPdfPages =====================");

            string testFilePath1 = Path.GetFullPath(@"../../../TestDocuments/Widget.pdf");
            Document doc1 = new Document(testFilePath1);
            string testFilePath2 = Path.GetFullPath(@"../../../TestDocuments/Color.pdf");
            Document doc2 = new Document(testFilePath2);

            doc1.InsertPdf(doc2, 0, 0, 2);

            doc1.Save("Joined.pdf", pretty: 1);

            doc2.Close();
            doc1.Close();

            Console.WriteLine("PDF pages joined successfully into 'Joined.pdf'.");
        }

        static void TestFreeTextAnnot(string[] args)
        {
            Console.WriteLine("\n=== TestFreeTextAnnot =====================");

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

            Console.WriteLine("Free text annotation created and saved to 'FreeTextAnnot.pdf'.");
        }
    }
}
