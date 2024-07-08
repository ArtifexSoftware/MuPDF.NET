// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document pdfOut = new Document();
string cdate = Utils.GetPdfNow();

Dictionary<string, string> pdfDict = new Dictionary<string, string>()
{
    {"creator", "PDF Joiner" },
    {"producer", "PyMuPDF" },
    {"creationDate", cdate },
    {"modDate", cdate },
    {"title", "Pdf Joiner" },
    {"author", "Green" },
    {"subject", "pdf joiner" },
    {"keywords", "mupdf doc join" }
};

pdfOut.SetMetadata(pdfDict);
List<Toc> totalToc = new List<Toc>();

Document doc = new Document("thinkpython2.pdf");
int von = 2;
int bis = 100;
int rot = 90;
int ausNR = 0;

pdfOut.InsertPdf(doc, fromPage: von, toPage: bis, rotate: rot);

totalToc.Add(new Toc() { Level = 1, Title = $"{von + 1}-{bis + 1}", Page = 7 });
List<Toc> toc = doc.GetToc(simple: false);
int lastLvl = 1;

List<int> pageRange = new List<int>();
for (int i = von; i < bis + 1; i++)
    pageRange.Add(i);

foreach (Toc t in toc)
{
    int pno = 0;
    LinkType lnkType = 0;
    try
    {
        lnkType = (t.Link as LinkInfo).Kind;
    }
    catch(Exception ex)
    {
        throw new Exception("invalid data format");
    }

    if (!pageRange.Contains(t.Page - 1) && lnkType == LinkType.LINK_GOTO)
        continue;
    if (lnkType == LinkType.LINK_GOTO)
         pno = pageRange.IndexOf(t.Page - 1) + ausNR + 1;
    while (t.Level > lastLvl + 1)
    {
        totalToc.Add(new Toc() { Level = lastLvl + 1, Title = "<>", Page = pno, Link = t.Link });
        lastLvl += 1;
    }
    lastLvl = t.Level;
    t.Page = pno;
    totalToc.Add(t);
}

ausNR += pageRange.Count;
doc.Close();

if (totalToc.Count != 0)
    pdfOut.SetToc(totalToc);
pdfOut.Save("output1.pdf");