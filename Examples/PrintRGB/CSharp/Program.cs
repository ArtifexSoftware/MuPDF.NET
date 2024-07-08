// See https://aka.ms/new-console-template for more information

using MuPDF.NET;

List<(string, int, int, int)> myList = Utils.GetColorInfoList();

float w = 800;
float h = 600;
float rw = 80;
float rh = 60;

int numColors = myList.Count;
float[] black = Utils.GetColor("black");
float[] white = Utils.GetColor("white");
float fsize = 8;
float lheight = fsize * 1.2f;
int idx = 0;

Document doc = new Document();

while (idx < numColors)
{
    Page page = doc.NewPage(-1, width: w, height: h);
    for (int i = 0; i < 10; i ++)
    {
        if (idx >= numColors)
            break;
        for (int j = 0; j < 10; j ++)
        {
            Rect rect = new Rect(rw * j, rh * i, rw * j + rw, rh * i + rh);
            string cname = myList[idx].Item1.ToLower();
            float[] col = new float[3] { myList[idx].Item2 / 255f, myList[idx].Item3 / 255f, myList[idx].Item4 / 255f };
            page.DrawRect(rect, color: col, fill: col);
            Point pnt1 = rect.TopLeft + new Point(0, rh * 0.3f);
            Point pnt2 = pnt1 + new Point(0, lheight);
            page.InsertText(pnt1, cname, fontSize: fsize, color: white, fontName: "Atop", fontFile: "../apo.ttf");
            page.InsertText(pnt2, cname, fontSize: fsize, color: black, fontName: "Atop", fontFile: "../apo.ttf");
            idx++;

            if (idx >= numColors)
                break;
        }
    }
}

Dictionary<string, string> m = new Dictionary<string, string>()
{
    {"author", "Green" },
    {"producer", "MuPDF.NET" },
    {"creator", "PrintRGB" },
    {"creationDate", Utils.GetPdfNow() },
    {"modDate", Utils.GetPdfNow()},
    {"title", "MuPDF.NET Color Database" },
    {"subject", "RGB values" }
};

doc.SetMetadata(m);
doc.Save("output.pdf");
