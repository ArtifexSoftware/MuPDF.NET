// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document doc = new Document();

string[] list = Directory.GetFiles("./img");

foreach (string file in list)
{
    if (!File.Exists(file))
    {
        continue;
    }

    Document img = new Document(file);
    Rect rect = img[0].Rect;
    Console.WriteLine(rect.ToString());
    byte[] pdfbytes = img.Convert2Pdf();
    img.Close();

    Document imgPdf = new Document("pdf", pdfbytes);
    Page page = doc.NewPage(width: rect.Width, height: rect.Height);
    page.ShowPdfPage(rect, imgPdf, 0);
}

doc.Save("ouput.pdf");