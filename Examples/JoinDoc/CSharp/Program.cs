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
int bis = 10;
int rot = 90;

pdfOut.InsertPdf(doc, fromPage: von, toPage: bis, rotate: rot);

totalToc.Add(new Toc() { Level = 1, Title = $"{von + 1}-{bis + 1}", Page = 7 });
List<Toc> toc = doc.GetToc(simple: false);
int lastLvl = 1;

foreach (Toc t in toc)
{
    try
    {
        LinkType lnkType = (t.Link as LinkInfo).Kind;
    }
    catch(Exception ex) { throw new Exception("invalid data format"); }
    
    if (t.Page - 1)
}