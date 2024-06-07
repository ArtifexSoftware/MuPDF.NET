// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document src = new Document("input.pdf");
Document doc = new Document();

(int width, int height) = Utils.PageSize("a4");
Rect r = new Rect(0, 0, width, height);

Rect r1 = r * 0.5f;
Console.WriteLine(r.BottomRight.ToString());
Console.WriteLine(r1.BottomRight.ToString());
Rect r2 = r1 + new Rect(r1.Width, 0, r1.Width, 0);
Rect r3 = r1 + new Rect(0, r1.Height, 0, r1.Height);
Rect r4 = new Rect(r1.BottomRight, r.BottomRight);

Rect[] rTab = { r1, r2, r3, r4 };
Page page = null;
for (int i = 0; i < src.PageCount; i++)
{
    Page spage = src[i];
    if (spage.Number % 4 == 0)
    {
        page = doc.NewPage(width: width, height: height);
    }
    Console.WriteLine($"{i}  " + rTab[spage.Number % 4].ToString());
    page.ShowPdfPage(
           rTab[spage.Number % 4],
           src,
           spage.Number);
}

doc.Save("output.pdf", garbage: 4, deflate: 1);