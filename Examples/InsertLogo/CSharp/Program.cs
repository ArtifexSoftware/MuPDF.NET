// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document src = new Document("logo.png");

if (!src.IsPDF)
{
    byte[] pdfbytes = src.Convert2Pdf();
    src.Close();
    src = new Document("pdf", pdfbytes);
}

Rect rect = src[0].Rect;
Console.WriteLine(rect.ToString());
float factor = 25.0f / rect.Height;
rect *= factor;

Document doc = new Document("input.pdf");
int xref = 0;
for (int i = 0; i < doc.PageCount; i++)
{
    xref = doc[i].ShowPdfPage(rect, src, 0, overlay: false);
}

doc.Save("ouput.pdf", garbage: 4);
