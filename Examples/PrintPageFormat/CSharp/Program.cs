// See https://aka.ms/new-console-template for more information

string FindFit(float w, float h)
{
    Dictionary<string, (float, float)> PaperSizes = new Dictionary<string, (float, float)>()
    {
        { "A2", (1191, 1684) },
        { "A3", (842, 1191) },
        { "A4", (595, 842) },
        { "A5", (420, 595) },
        { "A6", (298, 420) },
        { "A7", (210, 298) },
        { "A8", (147, 210) },
        { "A9", (105, 147) },
        { "A10", (74, 105) },
        { "B0", (2835, 4008) },
        { "B1", (2004, 2835) },
        { "B2", (1417, 2004) },
        { "B3", (1001, 1417) },
        { "B4", (709, 1001) },
        { "B5", (499, 709) },
        { "B6", (354, 499) },
        { "B7", (249, 354) },
        { "B8", (176, 249) },
        { "B9", (125, 176) },
        { "B10", (88, 125) },
        { "C0", (2599, 3677) },
        { "C1", (1837, 2599) },
        { "C2", (1298, 1837) },
        { "C3", (918, 1298) },
        { "C4", (649, 918) },
        { "C5", (459, 649) },
        { "C6", (323, 459) },
        { "C7", (230, 323) },
        { "C8", (162, 230) },
        { "C9", (113, 162) },
        { "C10", (79, 113) },
        { "Tabloid Extra", (864, 1296) },
        { "Legal-13", (612, 936) },
        { "Commercial", (297, 684) },
        { "Monarch", (279, 540) },
        { "Card-5x7", (360, 504) },
        { "Card-4x6", (288, 432) },
        { "Invoice", (396, 612) },
        { "Executive", (522, 756) },
        { "Letter", (612, 792) },
        { "Legal", (612, 1008) },
        { "Ledger", (792, 1224) }
    };

    int wi = (int)(Math.Round(w, 0));
    int hi = (int)(Math.Round(h, 0));
    int w1, h1;
    if (w <= h)
    {
        w1 = wi;
        h1 = hi;
    }
    else
    {
        w1 = hi;
        h1 = wi;
    }
    string sw = $"{w1}";
    string sh = $"{h1}";
    List<float> stab = new List<float>();
    foreach (var size in PaperSizes.Values)
    {
        stab.Add(Math.Abs(w1 - size.Item1) + Math.Abs(h1 - size.Item2));
    }
    float small = stab.Min();
    int idx = stab.IndexOf(small);
    string f = new List<string>(PaperSizes.Keys)[idx];
    string ff, ss;  
    if (w <= h)
    {
        ff = f + "-P";
        ss = $"{PaperSizes[f].Item1}" + " x " + $"{PaperSizes[f].Item2}";
    }
    else
    {
        ff = f + "-L";
        ss = $"{PaperSizes[f].Item2}" + " x " + $"{PaperSizes[f].Item1}";
    }

    if (small < 2)
        return ff;
    return $"{sw} X {sh} (other), closest: {ff} = {ss}";
}

Console.WriteLine(FindFit(300, 300));