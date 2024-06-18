// See https://aka.ms/new-console-template for more information
using MuPDF.NET;
using System.Text;

Document src = new Document("input.pdf");
Document dst = new Document("output.pdf");

for (int i = 0; i < src.GetEmbfileCount(); i++)
{
    EmbfileInfo d = src.GetEmbfileInfo(i);
    byte[] b = src.GetEmbfile(i);
    Console.WriteLine(Encoding.UTF8.GetString(b));
    dst.AddEmbfile(Encoding.UTF8.GetString(b), Encoding.UTF8.GetBytes(d.FileName), d.UFileName, d.Desc);
}

dst.SaveIncremental();
