// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

string fn = "input.pdf";
string pattern = "input";

Document src = new Document(fn);
for (int i = 0; i < src.PageCount; i++)
{
    Document doc = new Document();
    doc.InsertPdf(src, fromPage: i, toPage: i);
    doc.Save($"./output/{pattern}-{i}.pdf");
    doc.Close();
}