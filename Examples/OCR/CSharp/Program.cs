// See https://aka.ms/new-console-template for more information
using MuPDF.NET;
using System.Text;

string GetTessOCR(Page page, Rect bbox)
{
    IdentityMatrix mat = new IdentityMatrix(4, 4);
    Pixmap pix = page.GetPixmap(colorSpace: Utils.csGRAY.Name, matrix: mat, clip: bbox);
    byte[] text = pix.PdfOCR2Bytes();
    return Encoding.UTF8.GetString(text);
}

Document doc = new Document("v110-changes.pdf");
int ocrCount = 0;
for (int i = 0; i < doc.PageCount; i++)
{
    Page page = doc[i];
    List<Block> blocks = (page.GetText("dict", flags: 0) as PageInfo).Blocks;
    foreach (Block block in blocks)
    {
        foreach (Line line in block.Lines)
        {
            foreach (Span span in line.Spans)
            {
                string text = span.Text;
                Console.WriteLine(text);
                if (text.Contains((char)65533))
                {
                    ocrCount++;
                    string text1 = text.TrimStart();
                    string sb = new String(' ', text.Length - text1.Length);
                    text1 = text.TrimEnd();
                    string sa = new string(' ', text.Length - text1.Length);
                    string newText = sb + GetTessOCR(page, span.Bbox);
                }
            }
        }
    }
}
