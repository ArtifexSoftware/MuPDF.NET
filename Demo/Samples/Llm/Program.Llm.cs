namespace Demo
{
    internal partial class Program
    {
        internal static void TestMarkdownReader()
        {
            Console.WriteLine("\n=== TestMarkdownReader =======================");

            var reader = new PDFMarkdownReader();
            string testFilePath = Path.GetFullPath("../../../TestDocuments/columns.pdf");

            var docs = reader.LoadData(testFilePath);

            foreach (var doc in docs)
            {
                Console.WriteLine(doc.Text);
            }
        }

        internal static void TestGetText()
        {
            Console.WriteLine("\n=== TestGetText =======================");

            var reader = new PDFMarkdownReader();
            string testFilePath = Path.GetFullPath("../../../TestDocuments/columns.pdf");

            Document doc = new Document(testFilePath);

            for (int i = 0; i < doc.PageCount; i++)
            {
                Page page = doc[i];

                var text = Utils.GetText(page, option: "dict");

                Console.WriteLine(text);

                page.Dispose();
            }

            doc.Close();
        }

        internal static void TestTable()
        {
            Console.WriteLine("\n=== TestTable =======================");

            try
            {
                string testFilePath = Path.GetFullPath("../../../TestDocuments/err_table.pdf");
                
                if (!File.Exists(testFilePath))
                {
                    Console.WriteLine($"Error: Test file not found: {testFilePath}");
                    return;
                }

                Console.WriteLine($"Loading PDF: {testFilePath}");
                Document doc = new Document(testFilePath);
                Console.WriteLine($"Document loaded: {doc.PageCount} page(s)");

                // Test on first page
                Page page = doc[0];
                Console.WriteLine($"\nPage 0 - Rect: {page.Rect}");

                // Test 1: Get tables with default strategy
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
                        Console.WriteLine($"    Rows: {table.row_count}");
                        Console.WriteLine($"    Columns: {table.col_count}");
                        if (table.bbox != null)
                        {
                            Console.WriteLine($"    BBox: ({table.bbox.X0:F2}, {table.bbox.Y0:F2}, {table.bbox.X1:F2}, {table.bbox.Y1:F2})");
                        }

                        // Display header information
                        if (table.header != null)
                        {
                            Console.WriteLine($"    Header:");
                            Console.WriteLine($"      External: {table.header.external}");
                            if (table.header.names != null && table.header.names.Count > 0)
                            {
                                Console.WriteLine($"      Column names: {string.Join(", ", table.header.names)}");
                            }
                        }

                        // Extract table data
                        Console.WriteLine($"\n    Extracting table data...");
                        List<List<string>> tableData = table.Extract();
                        if (tableData != null && tableData.Count > 0)
                        {
                            Console.WriteLine($"    Extracted {tableData.Count} row(s) of data");
                            // Show first few rows as preview
                            int previewRows = Math.Min(3, tableData.Count);
                            for (int row = 0; row < previewRows; row++)
                            {
                                var rowData = tableData[row];
                                if (rowData != null)
                                {
                                    Console.WriteLine($"      Row {row + 1}: {string.Join(" | ", rowData.Take(5))}"); // Show first 5 columns
                                }
                            }
                            if (tableData.Count > previewRows)
                            {
                                Console.WriteLine($"      ... and {tableData.Count - previewRows} more row(s)");
                            }
                        }

                        // Convert to markdown
                        Console.WriteLine($"\n    Converting to Markdown...");
                        try
                        {
                            string markdown = table.ToMarkdown(clean: false, fillEmpty: true);
                            if (!string.IsNullOrEmpty(markdown))
                            {
                                Console.WriteLine($"    Markdown length: {markdown.Length} characters");
                                // Save markdown to file
                                string markdownFile = $"table_{i + 1}_page0.md";
                                File.WriteAllText(markdownFile, markdown, Encoding.UTF8);
                                Console.WriteLine($"    Markdown saved to: {markdownFile}");
                                
                                // Show preview
                                int previewLength = Math.Min(200, markdown.Length);
                                Console.WriteLine($"    Preview (first {previewLength} chars):");
                                Console.WriteLine($"    {markdown.Substring(0, previewLength)}...");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"    Error converting to markdown: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No tables found. Trying with 'lines' strategy...");
                    
                    // Test 2: Try with 'lines' strategy (less strict)
                    Console.WriteLine("\n--- Test 2: Get tables with 'lines' strategy ---");
                    tables = Utils.GetTables(
                        page, 
                        clip: page.Rect, 
                        vertical_strategy: "lines",
                        horizontal_strategy: "lines");

                    Console.WriteLine($"Found {tables.Count} table(s) with 'lines' strategy");
                }

                // Test 3: Try with 'text' strategy
                Console.WriteLine("\n--- Test 3: Get tables with 'text' strategy ---");
                List<Table> textTables = Utils.GetTables(
                    page, 
                    clip: page.Rect, 
                    vertical_strategy: "text",
                    horizontal_strategy: "text");

                Console.WriteLine($"Found {textTables.Count} table(s) with 'text' strategy");

                // Test 4: Get tables from all pages
                Console.WriteLine("\n--- Test 4: Get tables from all pages ---");
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

        internal static void TestPyMuPdfRagToMarkdown()
        {
            Console.WriteLine("\n=== TestPyMuPdfRagToMarkdown =======================");

            try
            {
                // Find a test PDF file
                //string testFilePath = Path.GetFullPath("../../../TestDocuments/national-capitals.pdf");
                string testFilePath = Path.GetFullPath("../../../TestDocuments/Magazine.pdf");

                Document doc = new Document(testFilePath);
                Console.WriteLine($"Document loaded: {doc.PageCount} page(s)");
                Console.WriteLine($"Document name: {doc.Name}");

                // Test 1: Basic ToMarkdown with default settings
                Console.WriteLine("\n--- Test 1: Basic ToMarkdown (default settings) ---");
                try
                {
                    List<int> pages = new List<int>();
                    pages.Add(0);
                    string markdown = MuPdfRag.ToMarkdown(
                        doc,
                        pages: pages, // All pages
                        hdrInfo: null, // Auto-detect headers
                        writeImages: false,
                        embedImages: false,
                        ignoreImages: false,
                        ignoreGraphics: false,
                        detectBgColor: true,
                        imagePath: "",
                        imageFormat: "png",
                        imageSizeLimit: 0.05f,
                        filename: testFilePath,
                        forceText: true,
                        pageChunks: false,
                        pageSeparators: false,
                        margins: null,
                        dpi: 150,
                        pageWidth: 612,
                        pageHeight: null,
                        tableStrategy: "lines_strict",
                        graphicsLimit: null,
                        fontsizeLimit: 3.0f,
                        ignoreCode: false,
                        extractWords: false,
                        showProgress: false,
                        useGlyphs: false,
                        ignoreAlpha: false
                    );

                    string markdownFile = "TestPyMuPdfRag_Output.md";
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
                /*
                // Test 2: ToMarkdown with IdentifyHeaders
                Console.WriteLine("\n--- Test 2: ToMarkdown with IdentifyHeaders ---");
                try
                {
                    var identifyHeaders = new IdentifyHeaders(doc, pages: null, bodyLimit: 12.0f, maxLevels: 6);
                    string markdown = MuPdfRag.ToMarkdown(
                        doc,
                        pages: new List<int> { 0 }, // First page only
                        hdrInfo: identifyHeaders,
                        writeImages: false,
                        embedImages: false,
                        ignoreImages: false,
                        filename: testFilePath,
                        forceText: true,
                        showProgress: false
                    );

                    string markdownFile = "TestPyMuPdfRag_WithHeaders.md";
                    File.WriteAllText(markdownFile, markdown, Encoding.UTF8);
                    Console.WriteLine($"Markdown with headers saved to: {markdownFile}");
                    Console.WriteLine($"Markdown length: {markdown.Length} characters");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ToMarkdown with IdentifyHeaders: {ex.Message}");
                }

                // Test 3: ToMarkdown with TocHeaders
                Console.WriteLine("\n--- Test 3: ToMarkdown with TocHeaders ---");
                try
                {
                    var tocHeaders = new TocHeaders(doc);
                    string markdown = MuPdfRag.ToMarkdown(
                        doc,
                        pages: new List<int> { 0 }, // First page only
                        hdrInfo: tocHeaders,
                        writeImages: false,
                        embedImages: false,
                        ignoreImages: false,
                        filename: testFilePath,
                        forceText: true,
                        showProgress: false
                    );

                    string markdownFile = "TestPyMuPdfRag_WithToc.md";
                    File.WriteAllText(markdownFile, markdown, Encoding.UTF8);
                    Console.WriteLine($"Markdown with TOC headers saved to: {markdownFile}");
                    Console.WriteLine($"Markdown length: {markdown.Length} characters");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ToMarkdown with TocHeaders: {ex.Message}");
                }

                // Test 4: ToMarkdown with page separators
                Console.WriteLine("\n--- Test 4: ToMarkdown with page separators ---");
                try
                {
                    string markdown = MuPdfRag.ToMarkdown(
                        doc,
                        pages: null, // All pages
                        hdrInfo: null,
                        writeImages: false,
                        embedImages: false,
                        ignoreImages: false,
                        filename: testFilePath,
                        forceText: true,
                        pageSeparators: true, // Add page separators
                        showProgress: false
                    );

                    string markdownFile = "TestPyMuPdfRag_WithSeparators.md";
                    File.WriteAllText(markdownFile, markdown, Encoding.UTF8);
                    Console.WriteLine($"Markdown with page separators saved to: {markdownFile}");
                    Console.WriteLine($"Markdown length: {markdown.Length} characters");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ToMarkdown with page separators: {ex.Message}");
                }

                // Test 5: ToMarkdown with progress bar
                Console.WriteLine("\n--- Test 5: ToMarkdown with progress bar ---");
                try
                {
                    string markdown = MuPdfRag.ToMarkdown(
                        doc,
                        pages: null, // All pages
                        hdrInfo: null,
                        writeImages: false,
                        embedImages: false,
                        ignoreImages: false,
                        filename: testFilePath,
                        forceText: true,
                        showProgress: true, // Show progress bar
                        pageSeparators: false
                    );

                    string markdownFile = "TestPyMuPdfRag_WithProgress.md";
                    File.WriteAllText(markdownFile, markdown, Encoding.UTF8);
                    Console.WriteLine($"\nMarkdown with progress saved to: {markdownFile}");
                    Console.WriteLine($"Markdown length: {markdown.Length} characters");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ToMarkdown with progress: {ex.Message}");
                }
                */
                doc.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during PyMuPdfRag test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\n=== TestPyMuPdfRagToMarkdown Completed =======================");
        }

        internal static void TestLLM()
        {
            Console.WriteLine("\n=== TestLLM =======================");

            try
            {
                // Display version information
                Console.WriteLine($"MuPDF.NET4LLM Version: {MuPDF4LLM.Version}");
                var versionTuple = MuPDF4LLM.VersionTuple;
                Console.WriteLine($"Version Tuple: ({versionTuple.major}, {versionTuple.minor}, {versionTuple.patch})");

                // Test with a sample PDF file
                string testFilePath = Path.GetFullPath("../../../TestDocuments/national-capitals.pdf");
                //string testFilePath = Path.GetFullPath("../../../TestDocuments/Magazine.pdf");

                // Try to find a PDF with actual content if Blank.pdf doesn't work well
                if (!File.Exists(testFilePath))
                {
                    testFilePath = Path.GetFullPath("../../../TestDocuments/Widget.pdf");
                }

                if (!File.Exists(testFilePath))
                {
                    Console.WriteLine($"Test PDF file not found. Skipping LLM test.");
                    return;
                }

                Console.WriteLine($"\nTesting with PDF: {testFilePath}");

                Document doc = new Document(testFilePath);
                Console.WriteLine($"Document loaded: {doc.PageCount} page(s)");

                string markdownStr = MuPDF4LLM.ToMarkdown(doc);

                doc.Close();

                string markdownFile = "TestLLM.md";
                File.WriteAllText(markdownFile, markdownStr, Encoding.UTF8);
                Console.WriteLine("\nLLM test completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TestLLM: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

    }
}
