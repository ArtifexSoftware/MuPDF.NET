// See https://aka.ms/new-console-template for more information

using MuPDF.NET;

class Program
{
    static void Main(string[] args)
    {
        Document doc = new Document(args[0]);
        byte[] content = doc.GetEmbfile(0);
        File.WriteAllBytes("../../../../output.jpg", content);
    }
}