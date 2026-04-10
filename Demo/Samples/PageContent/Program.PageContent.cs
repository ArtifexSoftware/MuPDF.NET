namespace Demo
{
    internal partial class Program
    {
        internal static void TestExtractTextWithLayout(string[] args)
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

        internal static void TestWidget(string[] args)
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

        internal static void TestColor(string[] args)
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

        internal static void TestCMYKRecolor(string[] args)
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

        internal static void TestSVGRecolor(string[] args)
        {
            Console.WriteLine("\n=== TestSVGRecolor =====================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/SvgTest.pdf");
            Document doc = new Document(testFilePath);
            doc.Recolor(0, "RGB");
            doc.Save("SVGRecolor.pdf");
            doc.Close();

            Console.WriteLine("SVG Recolor test completed.");
        }

        internal static void TestReplaceImage(string[] args)
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

        internal static void TestInsertImage(string[] args)
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

        internal static void TestGetImageInfo(string[] args)
        {
            Console.WriteLine("\n=== TestGetImageInfo =====================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/Image/TestInsertImage.pdf");
            Document doc = new Document(testFilePath);
            Page page = doc[0];

            List<Block> infos = page.GetImageInfo(xrefs: true);

            doc.Close();

            Console.WriteLine("Image info test completed.");
        }

        internal static void TestGetTextPageOcr(string[] args)
        {
            Console.WriteLine("\n=== TestGetTextPageOcr =====================");

            string testFilePath = Path.GetFullPath(@"../../../TestDocuments/Ocr.pdf");
            Document doc = new Document(testFilePath);
            Page page = doc[0];

            page.RemoveRotation();
            Pixmap pixmap = page.GetPixmap();

            List<Block> blocks = page.GetText("dict", flags: (int)TextFlags.TEXT_PRESERVE_IMAGES)?.Blocks;
            foreach (Block block in blocks)
            {
                Console.WriteLine(block.Image.Length);
            }

            // build the pipeline
            var pipeline = new ImageFilterPipeline();
            pipeline.Clear();
            //pipeline.AddDeskew(minAngle: 0.5);              // replaces any existing deskew step
            //pipeline.AddRemoveHorizontalLines();            // also replaces existing horizontal-removal step
            //pipeline.AddRemoveVerticalLines();
            //pipeline.AddGrayscale();
            //pipeline.AddMedian(blockSize: 2, replaceExisting: true);
            pipeline.AddGamma(gamma: 1.2);                  // brighten slightly
            //pipeline.AddScaleFit(100);
            pipeline.AddScale(scaleFactor: 3f, quality: SKFilterQuality.High);
            //pipeline.AddContrast(contrast: 100);
            //pipeline.AddDilation();
            //pipeline.AddInvert();

            TextPage tp = page.GetTextPageOcr((int)TextFlags.TEXT_PRESERVE_SPANS, full: true, imageFilters: pipeline);
            string txt = tp.ExtractText();
            Console.WriteLine(txt);

            doc.Close();

            Console.WriteLine("OCR text extraction test completed.");
        }

        internal static void TestCreateImagePage(string[] args)
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

        internal static void TestJoinPdfPages(string[] args)
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

    }
}
