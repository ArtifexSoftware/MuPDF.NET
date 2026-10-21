using System.Text;
using MuPDF.NET.PDF4LLM;

namespace Demo
{
    /// <summary>
    /// Extract <c>national-capitals.pdf</c> to Markdown with <c>MuPDF4LLM.ToMarkdown</c>.
    /// </summary>
    internal partial class Program
    {
        internal static void TestExtractNationalCapitals()
        {
            Console.WriteLine("\n=== TestExtractNationalCapitals (MuPDF.NET.PDF4LLM.ToMarkdown) =======================");

            string path = DemoPaths.Input("Llm/national-capitals.pdf");
            if (!File.Exists(path))
            {
                Console.WriteLine("Skip: need national-capitals.pdf under TestDocuments/Demo/Llm/.");
                return;
            }

            bool priorLayout = MuPDF4LLM.UseLayout;
            try
            {
                // Same path as LoadAiAsync / ollama: markdown tables without pymupdf_layout.
                MuPDF4LLM.SetUseLayout(true);
                string markdown = MuPDF4LLM.ToMarkdown(path, header: true, footer: true, showProgress: false);

                string output = DemoPaths.Output("national-capitals.md");
                File.WriteAllText(output, markdown, Encoding.UTF8);

                Console.WriteLine($"Source: {path}");
                Console.WriteLine($"Markdown characters: {markdown.Length}");
                Console.WriteLine($"Wrote: {output}");
                Console.WriteLine();
                int preview = Math.Min(800, markdown.Length);
                Console.WriteLine(markdown.Substring(0, preview));
                if (markdown.Length > preview)
                    Console.WriteLine("…");
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(priorLayout);
            }
        }
    }
}
