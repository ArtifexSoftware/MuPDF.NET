using MuPDF.NET;
using System.Globalization;

namespace Demo
{
    class Program
    {
        static void Main_(string[] args)
        {
            /*
            Document doc = new();
            Page page = doc.NewPage();

            MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(page.Rect);
            writer.FillTextbox(page.Rect, "text field I like you from Poland", new Font(fontName: "Kenpixel", fontFile: "kenpixel.ttf"), rtl: true);
            writer.WriteText(page);
            doc.Save("e:/res/test.pdf", pretty: 1);
            */

            // test for table
            Document doc = new Document(@"e:\__test.pdf");
            List<Table> tables = doc[0].GetTables();
        }

        static void Main(string[] args)
        {
            // test for bordered table
            Document doc = new Document(@"e:\Table\ (20).pdf");
            Rect clip = new Rect(47, 81, 960, 562);
            for (int i = 0; i < 0; i++)
            {
                List<Table> tables = doc[i].GetTables(clip:clip);
                foreach (var table in tables)
                {
                    List<List<string>> text = table.Extract();
                    foreach (var row in text)
                    {
                        foreach (var cell in row)
                        {
                            Console.Write(cell + ", ");
                        }
                        Console.WriteLine();
                    }
                    //string text = table.ToMarkdown();
                    //Console.WriteLine(text);
                }
            }

            Console.WriteLine("--------------------------------------------------");

            // test for non-bordered table
            for (int i = 0; i < 1; i++)
            {
                List<Table> tables = doc[i].GetTables(vertical_strategy: "text", horizontal_strategy: "text");
                foreach (var table in tables)
                {
                    List<List<string>> text = table.Extract();
                    foreach (var row in text)
                    {
                        foreach (var cell in row)
                        {
                            Console.Write(cell + ", ");
                        }
                        Console.WriteLine();
                    }
                    //string text = table.ToMarkdown();
                    //Console.WriteLine(text);
                }
            }

        }
    }
}
