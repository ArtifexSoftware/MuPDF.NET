using MuPDF.NET;
using System.Globalization;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestHelloWorld(args);
            TestBarcode(args);
        }

        static void TestHelloWorld(string[] args)
        {
            Console.WriteLine("Hello World!");
            Document doc = new();
            Page page = doc.NewPage();

            MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(page.Rect);
            writer.FillTextbox(page.Rect, "Hello World!", new Font(fontName: "helv"), rtl: true);
            writer.WriteText(page);
            doc.Save("test.pdf", pretty: 1);
        }

        static void TestBarcode(string[] args)
        {
            string binaryDir = AppContext.BaseDirectory;
            int i = 0;

            Console.WriteLine("=== From pdf file =======================");

            string testFilePath = Path.GetFullPath("../../../TestDocuments/Barcodes/Sample1.pdf");
            Document doc = new(testFilePath);

            Page page = doc[0];
            Rect rect = new Rect(290, 590, 420, 660);
            List<Barcode> barcodes = page.ReadBarcodes(rect);

            foreach (Barcode barcode in barcodes)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
            doc.Close();

            Console.WriteLine("=== From image file =====================");
            testFilePath = Path.GetFullPath("../../../TestDocuments/Barcodes/rendered.bmp");

            rect = new Rect(1260, 390, 1720, 580);
            List<Barcode> barcodes2 = Utils.ReadBarcodes(testFilePath, rect);

            i = 0;
            foreach (Barcode barcode in barcodes2)
            {
                BarcodePoint[] points = barcode.ResultPoints;
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: [{points[0]},{points[1]}]");
            }
        }
    }
}
