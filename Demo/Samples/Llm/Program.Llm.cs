namespace Demo
{
    internal partial class Program
    {
        internal static void TestMarkdownReader()
        {
            Console.WriteLine("\n=== TestMarkdownReader =======================");

            var reader = new PDFMarkdownReader();
            string testFilePath = DemoPaths.Input("columns.pdf");

            var docs = reader.LoadData(testFilePath);

            foreach (var doc in docs)
            {
                Console.WriteLine(doc.Text);
            }
        }

        internal static void TestTable()
        {
            Console.WriteLine("\n=== TestTable =======================");

            try
            {
                string testFilePath = DemoPaths.Input("err_table.pdf");

                if (!File.Exists(testFilePath))
                {
                    Console.WriteLine($"Error: Test file not found: {testFilePath}");
                    return;
                }

                Console.WriteLine($"Loading PDF: {testFilePath}");
                Document doc = new Document(testFilePath);
                Console.WriteLine($"Document loaded: {doc.PageCount} page(s)");

                Page page = doc[0];
                Console.WriteLine($"\nPage 0 - Rect: {page.Rect}");

                Console.WriteLine("\n--- Test 1: Get tables with 'lines_strict' strategy ---");
                List<Table> tables = Utils.GetTables(
                    page,
                    clip: page.Rect,
                    vertical_strategy: "lines_strict",
                    horizontal_strategy: "lines_strict");

                Console.WriteLine($"Found {tables.Count} table(s) on page 0");

                if (tables.Count > 0)
                {
                    for (int i = 0; i < tables.Count; i++)
                    {
                        Table table = tables[i];
                        Console.WriteLine($"\n  Table {i + 1}:");
                        Console.WriteLine($"    Rows: {table.RowCount}, Cols: {table.ColCount}");
                        try
                        {
                            string md = table.ToMarkdown();
                            Console.WriteLine($"    Markdown preview:\n{md.Substring(0, Math.Min(500, md.Length))}...");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"    ToMarkdown failed: {ex.Message}");
                        }
                    }
                }

                int totalTables = 0;
                for (int pageNum = 0; pageNum < doc.PageCount; pageNum++)
                {
                    Page currentPage = doc[pageNum];
                    List<Table> pageTables = Utils.GetTables(
                        currentPage,
                        clip: currentPage.Rect,
                        vertical_strategy: "lines_strict",
                        horizontal_strategy: "lines_strict");

                    if (pageTables.Count > 0)
                    {
                        Console.WriteLine($"  Page {pageNum}: {pageTables.Count} table(s)");
                        totalTables += pageTables.Count;
                    }
                    currentPage.Dispose();
                }
                Console.WriteLine($"Total tables found across all pages: {totalTables}");

                page.Dispose();
                doc.Close();

                Console.WriteLine("\n=== TestTable completed successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TestTable: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        internal static void TestMuPdfRagToMarkdown()
        {
            Console.WriteLine("\n=== TestMuPdfRagToMarkdown (legacy RAG via MuPDF4LLM.UseLayout=false) =======================");

            try
            {
                string testFilePath = DemoPaths.Input("Magazine.pdf");

                Document doc = new Document(testFilePath);
                Console.WriteLine($"Document loaded: {doc.PageCount} page(s)");
                Console.WriteLine($"Document name: {doc.Name}");

                Console.WriteLine("\n--- Test 1: MuPDF4LLM.ToMarkdown with MuPDF4LLM.UseLayout =false ---");
                try
                {
                    List<int> pages = new List<int> { 0 };
                    bool prev = MuPDF4LLM.UseLayout;
                    string markdown;
                    try
                    {
                        MuPDF4LLM.UseLayout = false;
                        markdown = MuPDF4LLM.ToMarkdown(
                            doc,
                            pages: pages,
                            writeImages: false,
                            embedImages: false,
                            imagePath: "",
                            imageFormat: "png",
                            filename: testFilePath,
                            forceText: true,
                            pageChunks: false,
                            pageSeparators: false,
                            dpi: 150,
                            pageWidth: 612,
                            pageHeight: null,
                            ignoreCode: false,
                            showProgress: false);
                    }
                    finally
                    {
                        MuPDF4LLM.UseLayout = prev;
                    }

                    string markdownFile = DemoPaths.Output("TestMuPdfRag_Output.md");
                    File.WriteAllText(markdownFile, markdown, Encoding.UTF8);
                    Console.WriteLine($"Markdown output saved to: {markdownFile}");
                    Console.WriteLine($"Markdown length: {markdown.Length} characters");
                    if (markdown.Length > 0)
                    {
                        int previewLength = Math.Min(300, markdown.Length);
                        Console.WriteLine($"Preview (first {previewLength} chars):\n{markdown.Substring(0, previewLength)}...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in basic ToMarkdown: {ex.Message}");
                }

                doc.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during MuPdfRag test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\n=== TestMuPdfRagToMarkdown Completed =======================");
        }
    }
}
