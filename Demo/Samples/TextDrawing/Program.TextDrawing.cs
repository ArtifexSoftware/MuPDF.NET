namespace Demo
{
    internal partial class Program
    {
        internal static void CreateAnnotDocument()
        {
            Console.WriteLine("\n=== CreateAnnotDocument =======================");
            Rect r = Constants.r;  // use the rectangle defined in Constants.cs

            Document doc = new Document();
            Page page = doc.NewPage();

            page.SetRotation(0);  // no rotation

            TextWriter pw = new TextWriter(page.TrimBox);
            string txt = "Origin 100.100";
            pw.Append(new Point(100, 500), txt, new Font("tiro"), fontSize: 24);
            pw.WriteText(page, new float[]{0,0.4f,1}, oc: 0);



            Annot annot = page.AddRectAnnot(r);  // 'Square'
            annot.SetBorder(width: 1f, dashes: new int[] { 1, 2 });
            annot.SetColors(stroke: Constants.blue, fill: Constants.gold);
            annot.Update(opacity: 0.5f);

            doc.Save(@"CreateAnnotDocument.pdf");

            doc.Close();
        }

        internal static void TestDrawShape()
        {
            string origfilename = @"../../../TestDocuments/NewAnnots.pdf";
            string outfilename = @"../../../TestDocuments/Blank.pdf";
            float newWidth = 0.5f;

            Document inputDoc = new Document(origfilename);
            Document outputDoc = new Document(outfilename);

            //string filePath = @"D:\\Vectorlab\\Jobs\\2025\\PACE\\pdf_fix\\assets\\exported_paths_net.txt";
            //StreamWriter writer = new StreamWriter(filePath);

            if (inputDoc.PageCount != outputDoc.PageCount)
            {
                return;
            }

            for (int pagNum = 0; pagNum < inputDoc.PageCount; pagNum++)
            {
                Page page = inputDoc.LoadPage(pagNum);
                Page outPage = outputDoc.LoadPage(pagNum);
                List<PathInfo> paths = page.GetDrawings(extended: false);
                int totalPaths = paths.Count;

                int i = 0;
                foreach (PathInfo pathInfo in paths)
                {
                    Shape shape = outPage.NewShape();
                    foreach (Item item in pathInfo.Items)
                    {
                        if (item != null)
                        {
                            if (item.Type == "l")
                            {
                                shape.DrawLine(item.P1, item.LastPoint);
                                //writer.Write($"{i:000}\\] line: {item.Type} >>> {item.P1}, {item.LastPoint}\\n");
                            }
                            else if (item.Type == "re")
                            {
                                shape.DrawRect(item.Rect, item.Orientation);
                                //writer.Write($"{i:000}\\] rect: {item.Type} >>> {item.Rect}, {item.Orientation}\\n");
                            }
                            else if (item.Type == "qu")
                            {
                                shape.DrawQuad(item.Quad);
                                //writer.Write($"{i:000}\\] quad: {item.Type} >>> {item.Quad}\\n");
                            }
                            else if (item.Type == "c")
                            {
                                shape.DrawBezier(item.P1, item.P2, item.P3, item.LastPoint);
                                //writer.Write($"{i:000}\\] curve: {item.Type} >>> {item.P1},  {item.P2}, {item.P3}, {item.LastPoint}\\n");
                            }
                            else
                            {
                                throw new Exception("unhandled drawing. Aborting...");
                            }
                        }
                    }

                    //pathInfo.Items.get
                    float newLineWidth = pathInfo.Width;
                    if (pathInfo.Width <= newWidth)
                    {
                        newLineWidth = newWidth;
                    }

                    int lineCap = 0;
                    if (pathInfo.LineCap != null && pathInfo.LineCap.Count > 0)
                        lineCap = (int)pathInfo.LineCap[0];
                    shape.Finish(
                        fill: pathInfo.Fill,
                        color: pathInfo.Color, //this.\_m_DEFAULT_COLOR,
                        evenOdd: pathInfo.EvenOdd,
                        closePath: pathInfo.ClosePath,
                        lineJoin: (int)pathInfo.LineJoin,
                        lineCap: lineCap,
                        width: newLineWidth,
                        strokeOpacity: pathInfo.StrokeOpacity,
                        fillOpacity: pathInfo.FillOpacity,
                        dashes: pathInfo.Dashes
                     );

                    // file_export.write(f'Path {i:03}\] width: {lwidth}, dashes: {path\["dashes"\]}, closePath: {path\["closePath"\]}\\n')
                    //writer.Write($"Path {i:000}\\] with: {newLineWidth}, dashes: {pathInfo.Dashes}, closePath: {pathInfo.ClosePath}\\n");

                    i++;
                    shape.Commit();
                }
            }

            inputDoc.Close();

            outputDoc.Save(@"TestDrawShape.pdf");
            outputDoc.Close();

            //writer.Close();
        }

        internal static void DrawLine(Page page, float startX, float startY, float endX, float endY, Color lineColor = null, float lineWidth = 1, bool dashed = false)
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

        internal static void TestDrawLine()
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

        internal static void TestTextFont(string[] args)
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

        internal static void TestInsertHtmlbox()
        {
            Console.WriteLine("\n=== TestInsertHtmlbox =======================");

            Rect rect = new Rect(100, 100, 550, 2250);
            Document doc = new Document();
            Page page = doc.NewPage();

            string htmlString = "<html><body style=\"text-align:Left;font-family:Segoe UI;font-style:normal;font-weight:normal;font-size:12;color:#000000;\"><p><span>τöƒΣ║ºσçåσñç∩╝Ü</span></p><p><span>1. µ»ÅµùÑτöƒΣ║ºΦ┐¢Φíîτ╗┤µèñΣ┐¥σà╗∩╝îΦ»╖σÅéτàºσ╣╢σí½σåÖPhilips Φç¬σè¿Φ₧║Σ╕¥Φ╡╖τé╣µúÇΦí¿πÇèWI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01πÇï</span></p><p><span>2 .µë¡σè¢Φ«íUNITΘÇëµï⌐ΓÇÿlbf.inΓÇÖ∩╝îΓÇÿP-PΓÇÖµ¿íσ╝Å∩╝îµ»Åσ¢¢σ░Åµù╢µúÇµƒÑΣ╕Çµ¼í∩╝îµ»Åµ¼íµúÇµƒÑ5τ╗äµò░µì«∩╝îσÅ¬µ£ëσÉêµá╝µëìσÅ»Σ╗ÑτöƒΣ║º∩╝¢σ╣╢σí½σåÖ</span></p><p><span>σè¢µë¡τƒ⌐Φ«░σ╜òΦí¿∩╝îΦí¿σìòσÅ╖ ∩╝Ü F-EN-34 πÇé</span></p><p><span>1.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü 5&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü5.0πÇé</span></p><p><span>2.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü10&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü10.0πÇé</span></p><p><span>3.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü12&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü12.0πÇé</span></p><p><span /></p><p><span>Colten - line break</span></p><p><span /></p><p><span>τöƒΣ║ºσçåσñç∩╝Ü</span></p><p><span>1. µ»ÅµùÑτöƒΣ║ºΦ┐¢Φíîτ╗┤µèñΣ┐¥σà╗∩╝îΦ»╖σÅéτàºσ╣╢σí½σåÖPhilips Φç¬σè¿Φ₧║Σ╕¥Φ╡╖τé╣µúÇΦí¿πÇèWI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01πÇï</span></p><p><span>2 .µë¡σè¢Φ«íUNITΘÇëµï⌐ΓÇÿlbf.inΓÇÖ∩╝îΓÇÿP-PΓÇÖµ¿íσ╝Å∩╝îµ»Åσ¢¢σ░Åµù╢µúÇµƒÑΣ╕Çµ¼í∩╝îµ»Åµ¼íµúÇµƒÑ5τ╗äµò░µì«∩╝îσÅ¬µ£ëσÉêµá╝µëìσÅ»Σ╗ÑτöƒΣ║º∩╝¢σ╣╢σí½σåÖ</span></p><p><span>σè¢µë¡τƒ⌐Φ«░σ╜òΦí¿∩╝îΦí¿σìòσÅ╖ ∩╝Ü F-EN-34 πÇé</span></p><p><span>1.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü 5&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü5.0πÇé</span></p><p><span>2.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü10&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü10.0πÇé</span></p><p><span>3.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü12&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü12.0πÇé</span></p><p><span> </span></p><p><span>Colten - line break</span></p><p><span /></p><p><span>τöƒΣ║ºσçåσñç∩╝Ü</span></p><p><span>1. µ»ÅµùÑτöƒΣ║ºΦ┐¢Φíîτ╗┤µèñΣ┐¥σà╗∩╝îΦ»╖σÅéτàºσ╣╢σí½σåÖPhilips Φç¬σè¿Φ₧║Σ╕¥Φ╡╖τé╣µúÇΦí¿πÇèWI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01πÇï</span></p><p><span>2 .µë¡σè¢Φ«íUNITΘÇëµï⌐ΓÇÿlbf.inΓÇÖ∩╝îΓÇÿP-PΓÇÖµ¿íσ╝Å∩╝îµ»Åσ¢¢σ░Åµù╢µúÇµƒÑΣ╕Çµ¼í∩╝îµ»Åµ¼íµúÇµƒÑ5τ╗äµò░µì«∩╝îσÅ¬µ£ëσÉêµá╝µëìσÅ»Σ╗ÑτöƒΣ║º∩╝¢σ╣╢σí½σåÖ</span></p><p><span>σè¢µë¡τƒ⌐Φ«░σ╜òΦí¿∩╝îΦí¿σìòσÅ╖ ∩╝Ü F-EN-34 πÇé</span></p><p><span>1.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü 5&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü5.0πÇé</span></p><p><span>2.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü10&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü10.0πÇé</span></p><p><span>3.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü12&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü12.0πÇé</span></p><p><span /></p><p><span>Colten - line break</span></p><p><span /></p><p><span>τöƒΣ║ºσçåσñç∩╝Ü</span></p><p><span>1. µ»ÅµùÑτöƒΣ║ºΦ┐¢Φíîτ╗┤µèñΣ┐¥σà╗∩╝îΦ»╖σÅéτàºσ╣╢σí½σåÖPhilips Φç¬σè¿Φ₧║Σ╕¥Φ╡╖τé╣µúÇΦí¿πÇèWI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01πÇï</span></p><p><span>2 .µë¡σè¢Φ«íUNITΘÇëµï⌐ΓÇÿlbf.inΓÇÖ∩╝îΓÇÿP-PΓÇÖµ¿íσ╝Å∩╝îµ»Åσ¢¢σ░Åµù╢µúÇµƒÑΣ╕Çµ¼í∩╝îµ»Åµ¼íµúÇµƒÑ5τ╗äµò░µì«∩╝îσÅ¬µ£ëσÉêµá╝µëìσÅ»Σ╗ÑτöƒΣ║º∩╝¢σ╣╢σí½σåÖ</span></p><p><span>σè¢µë¡τƒ⌐Φ«░σ╜òΦí¿∩╝îΦí¿σìòσÅ╖ ∩╝Ü F-EN-34 πÇé</span></p><p><span>1.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü 5&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü5.0πÇé</span></p><p><span>2.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü10&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü10.0πÇé</span></p><p><span>3.τö╡σè¿Φ╡╖σ¡Éσè¢τƒ⌐∩╝Ü12&#177;1 in-lbs∩╝îτö╡σè¿Φ₧║Σ╕¥Φ╡╖τ╝ûσÅ╖∩╝Ü12.0πÇé</span></p><p><span /></p></body></html>";
            (float s, float scale) = page.InsertHtmlBox(rect, htmlString, scaleLow: 0f);
            doc.Save(@"TestInsertHtmlbox.pdf");

            page.Dispose();
            doc.Close();

            Console.WriteLine($"Inserted HTML box with scale: {scale} and size: {s}");
        }

        internal static void TestLineAnnot()
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

        internal static void TestHelloWorldToNewDocument(string[] args)
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

        internal static void TestHelloWorldToExistingDocument(string[] args)
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

        internal static void TestFreeTextAnnot(string[] args)
        {
            Console.WriteLine("\n=== TestFreeTextAnnot =====================");

            Rect r = new Rect(72, 72, 220, 100);
            string t1 = "t├¬xt ├╝s├¿s L├ñti├▒ char├ƒ,\nEUR: Γé¼, mu: ┬╡, super scripts: ┬▓┬│!";
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
