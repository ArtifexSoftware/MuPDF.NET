using MuPDF.NET;
namespace Demo
{
    /// <summary>
    /// <see cref="Utils.LlamaMarkdownReader"/> demos aligned with MuPDF.NET repository reader tests.
    /// </summary>
    internal partial class Program
    {
        /// <summary>Empty in-memory PDF, save, then <see cref="Utils.LlamaMarkdownReader"/> LoadData (one page → one Llama document).</summary>
        internal static void Test4LlmPdfMarkdownReaderEmptyPage(string[] args)
        {
            _ = args;
            Console.WriteLine("\n=== Test4LlmPdfMarkdownReaderEmptyPage =======================");

            string path = Path.Combine(AppContext.BaseDirectory, "llm_reader_empty_page.pdf");
            Document document = new Document();
            try
            {
                document.NewPage();
                document.Save(path);
            }
            finally
            {
                document.Close();
            }

            var reader = Utils.LlamaMarkdownReader();
            var documents = reader.LoadData(path);
            Console.WriteLine($"Loaded {documents.Count} document(s).");
        }

        /// <summary><see cref="Utils.LlamaMarkdownReader"/> LoadData with a non-existent path → <see cref="FileNotFoundException"/>.</summary>
        internal static void Test4LlmPdfMarkdownReaderMissingFile(string[] args)
        {
            _ = args;
            Console.WriteLine("\n=== Test4LlmPdfMarkdownReaderMissingFile =======================");

            var reader = Utils.LlamaMarkdownReader();
            try
            {
                reader.LoadData(DemoPaths.Input("Llm/__missing_fixture__.pdf"));
                Console.WriteLine("Unexpected: LoadData should throw for missing file.");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"OK: FileNotFoundException — {ex.Message}");
            }
        }
    }
}
