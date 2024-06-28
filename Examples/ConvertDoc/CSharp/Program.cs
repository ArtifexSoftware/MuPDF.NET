// See https://aka.ms/new-console-template for more information

using MuPDF.NET;

Document doc = new Document("../../../../input.epub");

if (doc.IsPDF)
    throw new Exception("document is PDF already");

byte[] b = doc.Convert2Pdf();
Document pdf = new Document("pdf", b);

List<Toc> toc = doc.GetToc();
Console.WriteLine(toc[0].ToString());
pdf.SetToc(toc);

Dictionary<string, string> meta = doc.MetaData;
if (meta.GetValueOrDefault("producer", null) != null)
    meta["producer"] = "MuPDF.NET v2.0.8-alpha";

if (meta.GetValueOrDefault("creator", null) != null)
    meta["creator"] = "MuPDF.NET PDF Converter";

pdf.SetMetadata(meta);

int linkCnt = 0;
int linkSkip = 0;
for (int i = 0; i < doc.PageCount; i++)
{
    Page page = doc[i];
    List<LinkInfo> links = page.GetLinks();
    linkCnt += links.Count;
    Page pOut = pdf[i];
    foreach (LinkInfo l in links)
    {
        if (l.Kind == LinkType.LINK_NAMED)
        {
            linkSkip += 1;
            continue;
        }
        pOut.InsertLink(l);
    }
}

pdf.Save("output.pdf", garbage: 4, deflate: 1);