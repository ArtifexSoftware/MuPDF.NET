// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document doc = new Document();
(int width, int height) = Utils.PageSize("a6-l");
Page page = doc.NewPage(width: width, height: height);
Rect rect = new Rect(36, 36, width - 36, height - 36);

string[] imgList = Directory.GetFiles("input");
int imgCount = imgList.Length;

int perPage = (((width - 72) / 25) * ((height - 36 - 56) / 35));

int pages = (int)Math.Round(imgCount / (float)perPage + 0.5);

string text = $"Contains the following {imgCount} files from img:\n\n";

int pno = 1;

page.InsertText(rect.TopLeft, text, fontFile: "kenpixel.ttf");
page.InsertText(rect.BottomLeft, $"Page {pno} of {pages}", fontFile: "kenpixel.ttf");

Point point = rect.TopLeft + new Point(0, 20);
for (int i = 0; i < imgList.Length; i++)
{
    string path = imgList[i];
    Console.WriteLine(path);
    if (!File.Exists(path))
    {
        Console.WriteLine("skipping non-file");
        continue;
    }
    byte[] img = File.ReadAllBytes(path);
    page.AddFileAnnot(point, img, filename: imgList[i]);

    point += new Point(25, 0);
    if (point.X >= rect.Width)
        point = new Point(rect.X0, point.Y + 35);
    if (point.Y >= rect.Height && i < imgCount - 1)
    {
        page = doc.NewPage(width: width, height: height);
        pno += 1;
        page.InsertText(rect.TopLeft, text, fontFile: "kenpixel.ttf");
        page.InsertText(rect.BottomLeft, $"Page {pno} of {pages}", fontFile: "kenpixel.ttf");
        point = rect.TopLeft + new Point(0, 20);
    }
}
doc.Save("output.pdf");
