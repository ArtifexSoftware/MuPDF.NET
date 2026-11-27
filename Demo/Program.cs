using MuPDF.NET;

namespace Demo;

internal static class Program
{
    static void Main()
    {
        string outputPath = Path.Combine(ProjectDir(), "Output", "hello.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using (Document doc = new Document())
        {
            Page page = doc.NewPage();
            string text = "Made with MuPDF.NET";
            MuPDF.NET.Font font = new MuPDF.NET.Font("helv");
            MuPDF.NET.TextWriter tw = new MuPDF.NET.TextWriter(page.Rect);
            tw.Append(new Point(50, 100), text, font);
            tw.WriteText(page);
            doc.Save(outputPath);
        }

        using (Document doc = Document.Open(outputPath))
        using (Page page = doc[0])
        {
            Console.WriteLine($"Saved: {outputPath}");
            Console.WriteLine($"pageCount={doc.PageCount}");
            Console.WriteLine($"text={page.GetText()?.ToString()?.Trim()}");
        }
        MuPDF4LLM.SetLayout(true);
    }

    static string ProjectDir()
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
        {
            if (File.Exists(Path.Combine(dir, "Demo.csproj")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Could not find Demo.csproj. Run from the MuPDF.NET repository.");
    }
}
