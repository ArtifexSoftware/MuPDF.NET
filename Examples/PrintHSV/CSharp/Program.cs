// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

string SortKey(float[] x)
{
    float r = x[1] / 255f;
    float g = x[2] / 255f;
    float b = x[3] / 255f;

    float cmax = Math.Max(Math.Max(r, g), b);
    string v = ((int)Math.Round(cmax * 100)).ToString("D3");
    
    float cmin = Math.Min(Math.Min(r, g), b);
    float delta = cmax - cmin;
    float hue = 0;
    float sat = 0;

    if (delta == 0)
        hue = 0;
    else if (cmax == r)
        hue = 60f * (((g - b) / delta) % 6);
    else if (cmax == g)
        hue = 60f * (((b - r) / delta) + 2);
    else
        hue = 60f * (((r -g) / delta) + 4);
    string h = ((int)Math.Round(hue)).ToString("D3");

    if (cmax == 0)
        sat = 0;
    else
        sat = delta / cmax;
    string s = ((int)Math.Round(sat * 100)).ToString("D3");
    return h + s + v;
}

List<(string, int, int, int)> myList = Utils.GetColorInfoList();

int w = 800;
int h = 600;
int rw = 80;
int rh = 60;
int numColors = myList.Count;

float[] black = { 0f, 0f, 0f };
float[] white = { 1f, 1f, 1f };
float fsize = 8;
float lheight = fsize * 1.2f;
int idx = 0;
Document doc = new Document();

while (idx < numColors)
{
    doc.InsertPage(-1, width: w, height: h);
    Page page = doc[-1];
    for (int i = 0; i < 10; i++)
    {
        if (idx >= numColors)
            break;
        for (int j = 0; j < 10; j++)
        {
            Rect rect = new Rect(rw * j, rh * i, rw * j + rw, rh * i + rh);
            string cname = myList[idx].Item1.ToLower();
            float[] col = new float[3] { myList[idx].Item2 / 255f, myList[idx].Item3 / 255f, myList[idx].Item4 / 255f };

            page.DrawRect(rect, color: col, fill: col);
            Point pnt1 = rect.TopLeft + new Point(0, rh * 0.3f);
            Point pnt2 = pnt1 + new Point(0, lheight);
            page.InsertText(pnt1, cname, fontSize: fsize, color: white, fontName: "Atop", fontFile: "e://res/apo.ttf");
            page.InsertText(pnt2, cname, fontSize: fsize, color: black, fontName: "Atop", fontFile: "e://res/apo.ttf");
            idx += 1;
            if (idx >= numColors)
                break;
        }
    }
}

Dictionary<string, string> m = new Dictionary<string, string>()
{
    { "author", "Green" },
    { "producer", "MuPDF.NET" },
    { "creator", "Examples/PrintHSV" },
    { "creationDate", Utils.GetPdfNow() },
    { "modDate", Utils.GetPdfNow() },
    { "title", "MuPDF.NET Color Database" },
    { "subject", "Sorted down by HSV values" }
};

doc.SetMetadata(m);
doc.Save("e://res/output.pdf");