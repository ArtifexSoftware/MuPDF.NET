namespace Demo
{
    internal partial class Program
    {
        internal static void TestMoveFile()
        {
            string filePath = Path.GetFullPath("testmove.pdf");
            string movedPath = Path.GetFullPath("moved.pdf");

            if (File.Exists(filePath))
                File.Delete(filePath);
            if (File.Exists(movedPath))
                File.Delete(movedPath);

            try
            {
                File.Copy(Path.GetFullPath("../../../../TestDocuments/Demo/Blank.pdf"), filePath, true);
            }
            catch (FileNotFoundException)
            {
                using (Document seed = new Document())
                {
                    seed.NewPage();
                    seed.Save(filePath);
                    seed.Close();
                }
            }

            byte[] pdfBytes;
            using (Document d = new Document(filePath))
            {
                Page page = d[0];

                Point tl = new Point(100, 120);
                Point br = new Point(300, 150);

                Rect rect = new Rect(tl, br);

                TextWriter pw = new TextWriter(page.TrimBox);
                // Optional: fill the text box before saving (this sample only calls WriteText with default content).
                // Font font = new Font(fontName: "tiro");
                // pw.FillTextbox(rect, "This is a test to overwrite the original file and move it", font, fontSize: 24);
                pw.WriteText(page);

                page.Dispose();

                using MemoryStream tmp = new MemoryStream();
                d.Save(tmp, garbage: 3, deflateFonts: 1, deflate: 1);
                pdfBytes = tmp.ToArray();
            }

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(pdfBytes, 0, pdfBytes.Length);
                fs.Flush(true);
            }

            File.Move(filePath, movedPath, true);
            Console.WriteLine($"Moved {filePath} -> {movedPath}");
        }

        internal static void TestMetadata()
        {
            Console.WriteLine("\n=== TestMetadata =====================");

            string testFilePath = @"../../../../TestDocuments/Demo/Annot.pdf";

            Document doc = new Document(testFilePath);

            Dictionary<string, string>  metaDict = doc.MetaData;

            foreach (string key in metaDict.Keys)
            {
                Console.WriteLine(key + ": " + metaDict[key]);
            }

            doc.Close();

            Console.WriteLine("TestMetadata completed.");
        }

        internal static void TestMorph()
        {
            Console.WriteLine("\n=== TestMorph =====================");

            string testFilePath = @"../../../../TestDocuments/Demo/Morph.pdf";

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

            page.Dispose();
            doc.Save(@"morph.pdf");
            doc.Close();

            Console.WriteLine("Write to morph.pdf");
        }

        internal static void TestUnicodeDocument()
        {
            Console.WriteLine("\n=== TestUnicodeDocument =====================");

            string testFilePath = @"../../../../TestDocuments/Demo/你好.pdf";

            Document doc = new Document(testFilePath);

            doc.Save(@"你好_.pdf");
            doc.Close();

            Console.WriteLine("TestUnicodeDocument completed.");
        }

        internal static void TestMemoryLeak()
        {
            Console.WriteLine("\n=== [diag] memory-leak: document open/close loop ===");
            string testFilePath = DemoPaths.Input("Blank.pdf");

            for (int i = 0; i < 100; i++)
            {
                using var doc = new Document(testFilePath);
                using Page page = doc.NewPage();
            }

            Console.WriteLine("Completed 100 open/close iterations.");
        }

    }
}
