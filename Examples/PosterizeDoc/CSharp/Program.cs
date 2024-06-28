// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document src = new Document("input.pdf");
Document doc = new Document();

for (int i = 0; i < src.PageCount; i ++)
{
    Page spage = src[i];
    int xref = 0;
    Rect r = spage.Rect;
    Rect d = new Rect(spage.CropBoxPosition, spage.CropBoxPosition);

    Rect r1 = r * 0.5f;
    Rect r2 = r1 + new Rect(r1.Width, 0, r1.Width, 0);
    Rect r3 = r1 + new Rect(0, r1.Height, 0, r1.Height);
    Rect r4 = new Rect(r1.BottomRight, r.BottomRight);
    Rect[] rectList = { r1, r2, r3, r4 };

    foreach (Rect rr in rectList)
    {
        Rect rx = rr + d;
        Page page = doc.NewPage(-1, width: rx.Width, height: rx.Height);
        xref = page.ShowPdfPage(page.Rect, src, spage.Number, clip: rx);
    }
}

doc.Save("output.pdf", garbage: 4, deflate: 1);
