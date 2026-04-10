namespace Demo
{
    internal partial class Program
    {
        internal static void TestMoveFile()
        {
            string origfilename = @"../../../TestDocuments/Blank.pdf";

            string filePath = @"testmove.pdf";

            File.Copy(origfilename, filePath, true);

            Document d = new Document(filePath);

            Page page = d[0];
            
            Point tl = new Point(100, 120);
            Point br = new Point(300, 150);

            Rect rect = new Rect(tl, br);
            
            TextWriter pw = new TextWriter(page.TrimBox);
            /*
            Font font = new Font(fontName: "tiro");

            List<(string, float)> ret = pw.FillTextbox(rect, "This is a test to overwrite the original file and move it", font, fontSize: 24);
            */
            pw.WriteText(page);
            
            page.Dispose();

            MemoryStream tmp = new MemoryStream();

            d.Save(tmp, garbage: 3, deflateFonts: 1, deflate: 1);

            d.Close();

            File.WriteAllBytes(filePath, tmp.ToArray());

            tmp.Dispose();

            File.Move(filePath, @"moved.pdf", true);
        }

        internal static void TestMetadata()
        {
            Console.WriteLine("\n=== TestMetadata =====================");

            string testFilePath = @"../../../TestDocuments/Annot.pdf";

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

            string testFilePath = @"../../../TestDocuments/Morph.pdf";

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
        }

        internal static void TestUnicodeDocument()
        {
            Console.WriteLine("\n=== TestUnicodeDocument =====================");

            string testFilePath = @"../../../TestDocuments/Σ╜áσÑ╜.pdf";

            Document doc = new Document(testFilePath);

            doc.Save(@"Σ╜áσÑ╜_.pdf");
            doc.Close();

            Console.WriteLine("TestUnicodeDocument completed.");
        }

        internal static void TestMemoryLeak()
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

    }
}
