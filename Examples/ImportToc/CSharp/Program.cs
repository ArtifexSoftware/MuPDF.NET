// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document doc = new Document("input.pdf");

string[] lines = File.ReadAllLines("input.csv");
List<Toc> toc = new List<Toc>();

foreach (string line in lines)
{
    string[] row = line.Split(';');
    float p4 = float.Parse(row[3]);
    Toc t = new Toc() { Level = int.Parse(row[0]), Title = row[1], Page = int.Parse(row[2]), Link = p4 };
    toc.Add(t);
}

doc.SetToc(toc);
doc.SaveIncremental();