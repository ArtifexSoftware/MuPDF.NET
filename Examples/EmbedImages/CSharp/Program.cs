// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document doc = new Document();
(float width, float height) = Utils.PageSize("a4");
Rect rect = new Rect(0, 0, width, height) + new Rect(36, 36, -36, -36);


string[] list = Directory.GetFiles("img");
int n = list.Length;

for (int i = 0; i < n; i++)
{
    if (!File.Exists(list[i]))
        continue;

    byte[] img = File.ReadAllBytes(list[i]);
    doc.AddEmbfile(list[i], img, filename: list[i], ufilename: list[i], desc: list[i]);
}

Page page = doc.NewPage();
doc.Save("output.pdf");
