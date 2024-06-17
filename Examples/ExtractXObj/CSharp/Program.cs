// See https://aka.ms/new-console-template for more information
using MuPDF.NET;
using System.Text;

internal class Program
{
    static void Main(string[] args)
    {
        string infile = args[0];
        Document src = new Document(infile);
        string outfile = "output.pdf";

        Document doc = new Document();
        int total = 0;
        List<int> xrefs = new List<int>();

        for (int i = 0; i < src.PageCount; i ++)
        {
            int count = 0;
            List<Entry> xobjs = src.GetPageXObjects(i);
            foreach (Entry xobj in xobjs)
            {
                if (xobj.StreamXref != 0)
                    continue;
                Rect bbox = xobj.Bbox;
                if (bbox.IsInfinite)
                    continue;
                if (xrefs.Contains(xobj.Xref))
                    continue;
                xrefs.Add(xobj.Xref);

                doc.InsertPdf(src, fromPage: i, toPage: i, rotate: 0);
                string refName = xobj.RefName;
                byte[] refcmd = Encoding.UTF8.GetBytes($"/{refName} Do");
                Page page = doc[doc.PageCount - 1];
                page.SetMediaBox(bbox);
                page.CleanContetns();
                int xref = page.GetContents()[0];
                doc.UpdateStream(xref, refcmd);
                count++;
            }
            if (count > 0)
                Console.WriteLine(count);
            total += count;
        }

        if (total > 0)
        {
            doc.Save("output.pdf", garbage: 4, deflate: 1);
        }
    }
}
