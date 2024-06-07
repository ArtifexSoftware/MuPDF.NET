// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

internal class Program
{
    private static void Main(string[] args)
    {
        Document doc = new Document("../../../example.pdf");
        List<Toc> tocs = doc.GetToc(false);

        for (int i = 0; i < tocs.Count; i++)
        {
            Toc item = tocs[i];
            LinkInfo dest = item.Link;
            dest.Collapse = false;
            if (item.Level == 1)
            {
                dest.Color = new float[3] { 1, 0, 0 };
                dest.Bold = true;
                dest.Italic = false;
            }
            else if (item.Level == 2)
            {
                dest.Color = new float[3] { 0, 0, 1 };
                dest.Bold = false;
                dest.Italic = true;
            }
            else
            {
                dest.Color = new float[3] { 0, 1, 0 };
                dest.Bold = false;
                dest.Italic = false;
            }
            doc.SetTocItem(i, dest);
        }
        doc.Save("../../../new-toc.pdf");
    }
}
