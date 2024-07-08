// See https://aka.ms/new-console-template for more information

using MuPDF.NET;

Document doc = new Document();
Document src = new Document("input.pdf");

for (int i = 0; i < src.PageCount; i++)
{
    Rect srcRect = src[i].Rect;
    int srcRot = src[i].Rotation;
    Console.WriteLine(srcRot);
    src[i].SetRotation(0);
    Page page = doc.NewPage(-1, srcRect.Width, srcRect.Height);
    page.ShowPdfPage(page.Rect, src, src[i].Number, rotate: -srcRot);
}

doc.Save("output.pdf");