// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

class Program
{
    static void Main(string[] args)
    {
        int lineCtr = 0;
        int totalCtr = 0;
        int outCtr = 0;
        string outBuf = "";

        Document doc = new Document();

        (float width, float height) = Utils.PageSize("a4");
        int fontsize = 10;
        float lineHeight = fontsize * 1.2f;
        int nlines = (int)((height - 108.0f) / lineHeight);

        int PageOut(string b)
        {
            Page page = doc.NewPage(width: width, height: height);
            return page.InsertText(new Point(50, 72), text: b, fontSize: fontsize, fontFile: "e://res/kenpixel.ttf");
        }

        foreach (string line in File.ReadLines("e://res/input.txt"))
        {
            outBuf += line;
            lineCtr++;
            totalCtr++;
            if (lineCtr == nlines)
            {
                outCtr += PageOut(outBuf);
                outBuf = "";
                lineCtr = 0;
            }
        }

        if (outBuf.Length > 0)
            outCtr += PageOut(outBuf);

        int hFontsz = 16;
        int fFontsz = 8;
        float[] blue = new float[3] { 0, 0, 1f };
        int pspace = 500;

        for (int i = 0; i < doc.PageCount; i++)
        {
            Page page = doc[i];
            string footer = $"{page.Number + 1} ({doc.PageCount})";
            float plenftr = Utils.GetTextLength(footer, fontsize: fFontsz, fontname: "Kenpixel");
            page.InsertText(new Point(50, 50), "input.txt", color: blue, fontSize: hFontsz, fontFile: "e://res/kenpixel.ttf");
            page.DrawLine(new Point(50, 60), new Point(50 + pspace, 60), color: blue, width: 0.5f);
            page.DrawLine(new Point(50, height - 33), new Point(50 + pspace, height - 33), color: blue, width: 0.5f);
            page.InsertText(new Point(50 + pspace - plenftr, height - 33 + fFontsz * 1.2f), footer, fontSize: fFontsz, color: blue, fontFile: "e://res/kenpixel.ttf");
            page.CleanContetns();
        }

        doc.SetMetadata(new Dictionary<string, string>()
            {
                {"creationDate", Utils.GetPdfNow() },
                {"modDate", Utils.GetPdfNow() },
                {"creator", "convert" },
                {"producer", "MuPDF.NET" }
            });

        doc.Save("e://res/output.pdf", garbage: 4, pretty: 1);
    }
}
