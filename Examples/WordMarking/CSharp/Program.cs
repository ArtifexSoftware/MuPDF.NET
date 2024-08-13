// See https://aka.ms/new-console-template for more information
using System.Text;
using MuPDF.NET;

List<(Rect, string)> FindWords(Page page, WordBlock words, string prefix = "", string suffix = "", bool lower = true)
{
    bool TakeThis(string checkword, string prefix, string suffix, bool lower)
    {
        if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
            return true;
        if (lower)
            checkword = checkword.ToLower();
        if (!string.IsNullOrEmpty(prefix) && checkword.StartsWith(prefix))
            return true;
        if (!string.IsNullOrEmpty(suffix) && checkword.EndsWith(suffix))
            return true;
        return false;
    }

    List<(Rect, string)> rList = new List<(Rect, string)> ();
    Rect rect = new Rect(words.X0, words.Y0, words.X1, words.Y1);
    List<Block> blocks = (page.GetText("rawdict", clip: rect, flags: 0) as PageInfo).Blocks;
    
    foreach (Block block in blocks)
    {
        foreach (Line line in block.Lines)
        {
            if (line.Spans == null || line.Spans.Count == 0)
                continue;
            Rect r = new Rect();
            string checkword = "";
            foreach (Span span in line.Spans)
            {
                foreach (MuPDF.NET.Char ch in span.Chars)
                {
                    if (System.Char.IsLetter(ch.C))
                    {
                        r = r | new Rect(ch.Bbox);
                        Console.WriteLine(r.ToString());
                        checkword += ch.C;
                    }
                    else
                    {
                        if (TakeThis(checkword, prefix, suffix, lower))
                            rList.Add((r, checkword));
                        r = new Rect();
                        checkword = "";
                    }
                }
            }
            if (TakeThis(checkword, prefix, suffix, lower))
                rList.Add((r, checkword));
        }
    }
    return rList;
}

Document doc = new Document("search.pdf");
Page page = doc[0];
List<WordBlock> wordlist = page.GetText("words");

foreach (WordBlock word in wordlist)
{
    string text = word.Text.ToLower();
    
    if (!(text.StartsWith("seife") || text.StartsWith("wissenschaft")))
        continue;
    List<(Rect, string)> items = FindWords(page, word, prefix: "", suffix: "", lower: true);
    foreach ((Rect, string) item in items)
    {
        if (item.Item1.IsEmpty)
            continue;
        if (!(item.Item2.ToLower() == "seife" || item.Item2.ToLower() == "wissenschaft"))
            continue;
        Annot annot = page.AddRectAnnot(item.Item1);
        annot.SetBorder(null, width: 0.3f);
        annot.Update();
    }
}

doc.Save("makeword.pdf", garbage: 3, deflate: 1);