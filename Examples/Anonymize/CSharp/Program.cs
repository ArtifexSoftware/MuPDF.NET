// See https://aka.ms/new-console-template for more information
using MuPDF.NET;
using System.Text;

string RemoveTxt(string cont)
{
    string cont1 = cont.Replace("\n", " ");
    string[] ct = cont1.Split(' ');
    bool intext = false;
    List<string> nct = new List<string>();
    foreach (string word in ct)
    {
        if (word == "ET")
        {
            intext = false;
            continue;
        }
        if (word == "BT")
        {
            intext = true;
            continue;
        }
        if (intext)
            continue;
        nct.Add(word);
    }
    string ncont = string.Join(" ", nct);
    return ncont;
}

Document doc = new Document("input.pdf");
doc.SetMetadata();
doc.DeleteXmlMetadata();

for (int i = 0; i < doc.PageCount; i++)
{
    Page page = doc[i];
    List<int> xrefList = page.GetContents();
    foreach (int xref in xrefList)
    {
        byte[] cont = doc.GetXrefStream(xref);
        string ncont = RemoveTxt(Encoding.UTF8.GetString(cont));
        doc.UpdateStream(xref, Encoding.UTF8.GetBytes(ncont));
    }
}

doc.Save("ouput.pdf", clean: 1, garbage: 4);
