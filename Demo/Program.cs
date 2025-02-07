using MuPDF.NET;
using System.Globalization;

namespace Demo
{
    class Program
    {
        static void Main_(string[] args)
        {
            Console.WriteLine("Hello World!");
            Document doc = new();
            Page page = doc.NewPage();

            MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(page.Rect);
            writer.FillTextbox(page.Rect, "Hello World!", new Font(fontName: "helv"), rtl: true);
            writer.WriteText(page);
            doc.Save("test.pdf", pretty: 1);
        }

        static void Main(string[] args)
        {
            string binaryDir = AppContext.BaseDirectory;
            int i = 0;
            /*
            Document doc = new(binaryDir + @"..\..\..\TestDocuments\Barcodes\Sample1.pdf");

            Page page = doc[0];
            List<Barcode> barcodes = page.ReadBarcodes(tryHarder: true, tryInverted: true, pureBarcode: true, multi: true, autoRotate: true);

            foreach (Barcode barcode in barcodes)
            {
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: {barcode.addResultPoints}");
            }
            doc.Close();
            */
            Console.WriteLine("====================================");
            string testFilePath = Path.GetFullPath("../../../TestDocuments/Barcodes/rendered.bmp");
            List<Barcode> barcodes2 = Utils.ReadBarcodes(testFilePath, tryHarder: false, tryInverted: false, pureBarcode: true, multi: true, autoRotate: false);

            i = 0;
            foreach (Barcode barcode in barcodes2)
            {
                Console.WriteLine($"Page {i++} - Type: {barcode.BarcodeFormat} - Value: {barcode.Text} - Rect: {barcode.addResultPoints}");
            }
        }
    }
}
