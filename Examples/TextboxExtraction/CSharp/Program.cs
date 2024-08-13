// See https://aka.ms/new-console-template for more information
using mupdf;
using MuPDF.NET;

Dictionary<float, List<WordBlock>> lineDict = new Dictionary<float, List<WordBlock>>();

string MakeText(List<WordBlock> words)
{
    foreach (WordBlock word in words)
    {
        float y1 = (float)Math.Round(word.Y1);
        string w = word.Text;
        List<WordBlock> line = lineDict.GetValueOrDefault(y1, new List<WordBlock>());
        line.Add(word);
        lineDict[y1] = line;
    }
    List<List<WordBlock>> lines = new List<List<WordBlock>>(lineDict.Values);
    
    return string.Join("\n", lines.Select(x => string.Join(" ", x.Select(y => y.Text).ToArray())).ToArray());
}

Document doc = new Document("search.pdf");
Page page = doc[0];

Rect rect = page.FirstAnnot.Rect;
List<WordBlock> words = page.GetText("words");

words = words.Where(w => rect.Contains(new Rect(w.X0, w.Y0, w.X1, w.Y1))).ToList();

Console.WriteLine(MakeText(words));

words = words.Where(w => (new Rect(w.X0, w.Y0, w.X1, w.Y1)).Intersects(rect)).ToList();

Console.WriteLine(MakeText(words));