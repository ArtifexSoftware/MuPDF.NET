using System.Threading.Tasks;

namespace Demo
{
    internal partial class Program
    {
        internal static void TestIssue234()
        {
            Console.WriteLine("\n=== TestIssue234 =======================");

            var pix = new Pixmap("../../../TestDocuments/Image/boxedpage.jpg"); // 629x1000 image
            var scaled = new Pixmap(pix, 943, 1500, null); // scale up
            byte[] jpeg = scaled.ToBytes("jpg", 65);

            using var doc = new Document();
            Page page = doc.NewPage(0, 943, 1500);
            page.InsertImage(page.Rect, stream: jpeg);
            page.Dispose();
            doc.Save("issue_234.pdf");
            doc.Close();

            Console.WriteLine("Saved issue_234.pdf");
        }

        internal static void TestRecompressJBIG2()
        {
            Console.WriteLine("\n=== TestJBIG2 =======================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/Jbig2.pdf");

            Document doc = new Document(testFilePath);

            PdfImageRewriterOptions opts = new PdfImageRewriterOptions();

            opts.bitonal_image_recompress_method = mupdf.mupdf.FZ_RECOMPRESS_FAX;
            opts.recompress_when = mupdf.mupdf.FZ_RECOMPRESS_WHEN_ALWAYS;

            doc.RewriteImage(options: opts);

            doc.Save(@"e:\TestRecompressJBIG2.pdf");
            doc.Close();

            Console.WriteLine("Saved e:\\TestRecompressJBIG2.pdf");
        }

        internal static void TestIssue1880()
        {
            Console.WriteLine("\n=== TestIssue1880 =======================");

            string testFilePath = Path.GetFullPath(@"../../../TestDocuments/issue_1880.pdf");

            Document doc = new Document(testFilePath);

            for (int i = 0; i < doc.PageCount; i++)
            {
                Page page = doc[i];

                List<Barcode> barcodes = page.ReadBarcodes(barcodeFormat: BarcodeFormat.DM, pureBarcode:true);
                foreach (Barcode barcode in barcodes)
                {
                    BarcodePoint[] points = barcode.ResultPoints;
                    Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
                }

                page.Dispose();
            }

            doc.Close();
        }

        internal static void TestIssue213()
        {
            Console.WriteLine("\n=== TestIssue213 =======================");

            string origfilename = @"../../../TestDocuments/issue_213.pdf";
            string outfilename = @"../../../TestDocuments/Blank.pdf";
            float newWidth = 0.5f;

            Document inputDoc = new Document(origfilename);
            Document outputDoc = new Document(outfilename);

            if (inputDoc.PageCount != outputDoc.PageCount)
            {
                return;
            }

            for (int pagNum = 0; pagNum < inputDoc.PageCount; pagNum++)
            {
                Page page = inputDoc.LoadPage(pagNum);

                Pixmap pxmp = page.GetPixmap();
                pxmp.Save(@"output.png");
                pxmp.Dispose();

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

            outputDoc.Save(@"output.pdf");
            outputDoc.Close();

            //writer.Close();
        }

        internal static void TestPixmapParallel()
        {
            const int iterations = 300;
            const int degreeOfParallelism = 10;

            var pdfPath = Path.Combine(@"..\..\..\TestDocuments\TestPdf1.pdf");
            var pdf = File.ReadAllBytes(pdfPath);

            Console.WriteLine($"MuPDF.NET parallel Pixmap.ToBytes repro");
            Console.WriteLine($"PDF: {pdfPath}");
            Console.WriteLine($"Iterations: {iterations}");
            Console.WriteLine($"Degree of parallelism: {degreeOfParallelism}");
            Console.WriteLine();

            Parallel.ForEach(
                Enumerable.Range(0, iterations),
                new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                iteration =>
                {
                    using var document = new Document(stream: pdf, fileType: "pdf");
                    using var page = document[0];
                    using var pixmap = page.GetPixmap(new Matrix(2, 2));

                    var png = pixmap.ToBytes("png");
                    Console.WriteLine($"Iteration {iteration + 1}: rendered {png.Length} bytes");
                });

            Console.WriteLine("Completed without crashing.");
        }
    }
}
