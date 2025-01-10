using MuPDF.NET;
using System.Globalization;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
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
            Document doc = new Document(@"e:\test.pdf");
            Page page = doc[0];
            List<Table> tables = page.GetTables();
        }
    }
}
