namespace Demo
{
    /// <summary>
    /// <see cref="PDFMarkdownReader"/> demos aligned with MuPDF.NET4LLM / repository reader tests.
    /// </summary>
    internal partial class Program
    {
        /// <summary>Empty in-memory PDF, save, then <see cref="PDFMarkdownReader.LoadData"/> (one page → one Llama document).</summary>
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

            var reader = new PDFMarkdownReader();
            var documents = reader.LoadData(path);
            Console.WriteLine($"Loaded {documents.Count} document(s).");
        }

        /// <summary><see cref="PDFMarkdownReader.LoadData"/> with a non-existent path → <see cref="FileNotFoundException"/>.</summary>
        internal static void Test4LlmPdfMarkdownReaderMissingFile(string[] args)
        {
            _ = args;
            Console.WriteLine("\n=== Test4LlmPdfMarkdownReaderMissingFile =======================");

            var reader = new PDFMarkdownReader();
            try
            {
                reader.LoadData(Path.Combine(LlmRepositoryTestsDirectory(), "fake", "path", "nope.pdf"));
                Console.WriteLine("Unexpected: LoadData should throw for missing file.");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"OK: FileNotFoundException — {ex.Message}");
            }
        }
    }
}
