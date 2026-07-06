namespace Demo
{
    /// <summary>
    /// Demo samples grouped by MuPDF.NET / PDF4LLM feature areas.
    /// Default run executes every entry in <see cref="Samples"/> (including <c>[diag]</c> samples).
    /// Use <c>user</c> to skip diagnostics, or <c>diagnostics</c> to run only <c>[diag]</c> samples.
    /// </summary>
    public static class SampleMenu
    {
        private sealed record Sample(
            string Category,
            string Name,
            string Description,
            Action<string[]> Run,
            bool Diagnostic = false);

        private static readonly Sample[] Samples =
        {
            // —— Document & I/O —— Samples/Document
            new("Document & I/O", "hello-new-pdf", "Hello World on a new PDF", a => Program.TestHelloWorldToNewDocument(a)),
            new("Document & I/O", "hello-existing-pdf", "Hello World on existing Blank.pdf", a => Program.TestHelloWorldToExistingDocument(a)),
            new("Document & I/O", "join-pdf", "Insert pages from another PDF", a => Program.TestJoinPdfPages(a)),
            new("Document & I/O", "metadata", "Print document metadata", _ => Program.TestMetadata()),
            new("Document & I/O", "move-file", "Save through MemoryStream and move output", _ => Program.TestMoveFile()),
            new("Document & I/O", "unicode-doc", "Save PDF with unicode filename", _ => Program.TestUnicodeDocument()),
            new("Document & I/O", "memory-leak", "[diag] Open/close documents in a loop", _ => Program.TestMemoryLeak(), Diagnostic: true),
            
            // —— Text, story & vector drawing —— Samples/TextDrawing
            new("Text, story & drawing", "insert-htmlbox", "Insert HTML story box into a new page", _ => Program.TestInsertHtmlbox()),
            new("Text, story & drawing", "text-font", "FillTextbox with fonts", a => Program.TestTextFont(a)),
            new("Text, story & drawing", "morph", "TextWriter with morph / rotation", _ => Program.TestMorph()),
            new("Text, story & drawing", "gettext", "GetText dict dump per page", _ => Program.TestGetText()),
            new("Text, story & drawing", "extract-text-layout", "Extract text with reading order (columns.pdf)", a => Program.TestExtractTextWithLayout(a)),
            new("Text, story & drawing", "draw-line", "Draw dashed lines on a page", _ => Program.TestDrawLine()),
            new("Text, story & drawing", "draw-shape", "Copy vector paths between PDFs", _ => Program.TestDrawShape()),
            
            // —— Annotations —— Samples/Annotations
            new("Annotations", "line-annot", "Create and modify line annotations", _ => Program.TestLineAnnot()),
            new("Annotations", "annot-freetext1", "Free-text annotation sample (1)", a => Program.TestAnnotationsFreeText1(a)),
            new("Annotations", "annot-freetext2", "Free-text annotation sample (2)", a => Program.TestAnnotationsFreeText2(a)),
            new("Annotations", "new-annots", "Caret, markers, shapes, stamp, redaction, etc.", a => NewAnnots.Run(a)),
            new("Annotations", "annot-doc", "Rectangle annotation + text", _ => Program.CreateAnnotDocument()),
            new("Annotations", "freetext-annot", "Add free-text annotation (unicode)", a => Program.TestFreeTextAnnot(a)),
            
            // —— Pages, widgets, images & color —— Samples/PageContent
            new("Pages, widgets, images & color", "widget", "Inspect form widgets", a => Program.TestWidget(a)),
            new("Pages, widgets, images & color", "color", "Recolor page images", a => Program.TestColor(a)),
            new("Pages, widgets, images & color", "cmyk-recolor", "CMYK recolor", a => Program.TestCMYKRecolor(a)),
            new("Pages, widgets, images & color", "svg-recolor", "SVG / RGB recolor", a => Program.TestSVGRecolor(a)),
            new("Pages, widgets, images & color", "replace-image", "Replace embedded images", a => Program.TestReplaceImage(a)),
            new("Pages, widgets, images & color", "insert-image", "Insert images from pixmaps and files", a => Program.TestInsertImage(a)),
            new("Pages, widgets, images & color", "get-image-info", "Dump image xref info", a => Program.TestGetImageInfo(a)),
            new("Pages, widgets, images & color", "page-ocr", "OCR text page with image filter pipeline", a => Program.TestGetTextPageOcr(a)),
            new("Pages, widgets, images & color", "create-image-page", "New PDF page from PNG pixmap", a => Program.TestCreateImagePage(a)),
            
            // —— Image filters (Skia) —— Samples/ImageFilters
            new("Image filters (Skia)", "image-filter", "Skia pipeline on table.jpg → output.png", _ => Program.TestImageFilter()),
            new("Image filters (Skia)", "image-filter-ocr", "Pixmap OCR with filter pipeline", _ => Program.TestImageFilterOcr()),
            
            // —— Barcodes —— Samples/Barcodes
            new("Barcodes", "write-barcode", "Write many barcode types to PDF and PNG", a => Program.TestWriteBarcode(a)),
            new("Barcodes", "write-barcode1", "Write CODE39/CODE128/DM with Units rects", _ => Program.TestWriteBarcode1()),
            
            // —— PDF4LLM —— Samples/Llm
            new("PDF4LLM", "rag-markdown", "PDF to Markdown (Magazine.pdf)", _ => Program.TestMuPdfRagToMarkdown()),
            new("PDF4LLM", "table", "Detect tables and export markdown", _ => Program.TestTable()),
            new("PDF4LLM", "table-extract-1", "Dump detected tables by page to console", _ => Program.TestTableExtract1()),
            new("PDF4LLM", "table-extract-2", "Export detected tables to tables.csv", _ => Program.TestTableExtract2()),
            new("PDF4LLM", "table-extract-3", "Merge continued table pages by column count", _ => Program.TestTableExtract3()),
            new("PDF4LLM", "table-ocr", "Extract OCR text from Ocr.pdf", _ => Program.TestOcr()),
            new("PDF4LLM", "llm-reader-save-pages", "Load markdown chunks and save per-page .md files", _ => Program.TestLLM2()),
            new("PDF4LLM", "markdown-reader", "LlamaIndex PDFMarkdownReader", _ => Program.TestMarkdownReader()),
            new("PDF4LLM", "ai-connector", "LoadAiAsync: Ask / Summarize / Search", a => Program.TestMicrosoftAiConnector(a).GetAwaiter().GetResult()),
            new("PDF4LLM", "llm-to-markdown-fixture-370", "[diag] ToMarkdown vs test_370 expected output", a => Program.Test4LlmToMarkdownCompareExpected370(a), Diagnostic: true),
            new("PDF4LLM", "llm-to-markdown-ocr-1", "[diag] ToMarkdown OCR fixture (FFFD)", a => Program.Test4LlmToMarkdownOcrFixture1(a), Diagnostic: true),
            new("PDF4LLM", "llm-to-markdown-ocr-2", "[diag] ToMarkdown without OCR on FFFD fixture", a => Program.Test4LlmToMarkdownOcrFixture2(a), Diagnostic: true),
            new("PDF4LLM", "llm-to-markdown-ocr-3", "[diag] ToMarkdown OCR on/off SVG fixture", a => Program.Test4LlmToMarkdownOcrFixture3(a), Diagnostic: true),
            new("PDF4LLM", "llm-pdf-reader-empty", "[diag] PDFMarkdownReader empty page", a => Program.Test4LlmPdfMarkdownReaderEmptyPage(a), Diagnostic: true),
            new("PDF4LLM", "llm-pdf-reader-missing-file", "[diag] PDFMarkdownReader missing file", a => Program.Test4LlmPdfMarkdownReaderMissingFile(a), Diagnostic: true),
            
            // —— Regression & diagnostics —— Samples/Regression
            new("Regression & diagnostics", "issue-213", "[diag] Drawing paths / line width", _ => Program.TestIssue213(), Diagnostic: true),
            new("Regression & diagnostics", "issue-1880", "[diag] Read Data Matrix barcodes", _ => Program.TestIssue1880(), Diagnostic: true),
            new("Regression & diagnostics", "issue-234", "[diag] Pixmap scale + insert image", _ => Program.TestIssue234(), Diagnostic: true),
            new("Regression & diagnostics", "pixmap-parallel", "[diag] Parallel Pixmap.ToBytes", _ => Program.TestPixmapParallel(), Diagnostic: true),
            new("Regression & diagnostics", "gettables-parallel", "[diag] Parallel Utils.GetTables", _ => Program.TestGetTablesParallel(), Diagnostic: true),
            new("Regression & diagnostics", "jbig2", "[diag] JBIG2 image recompression", _ => Program.TestRecompressJBIG2(), Diagnostic: true),
        };

        private static readonly Dictionary<string, Sample> ByName = BuildIndex();

        private static Dictionary<string, Sample> BuildIndex()
        {
            var d = new Dictionary<string, Sample>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in Samples)
                d[s.Name] = s;
            return d;
        }

        public static void Run(string[] args)
        {
            if (args.Length > 0 && IsHelp(args[0]))
            {
                PrintUsage();
                return;
            }

            if (args.Length == 0 || IsRunAllSwitch(args[0]))
            {
                RunSamples(_ => true);
                return;
            }

            if (string.Equals(args[0], "user", StringComparison.OrdinalIgnoreCase))
            {
                RunSamples(s => !s.Diagnostic);
                return;
            }

            if (string.Equals(args[0], "diagnostics", StringComparison.OrdinalIgnoreCase))
            {
                RunSamples(s => s.Diagnostic);
                return;
            }

            if (!ByName.TryGetValue(args[0], out var sample))
            {
                Console.Error.WriteLine($"Unknown sample: {args[0]}");
                PrintUsage();
                Environment.ExitCode = 1;
                return;
            }

            Console.WriteLine($"--- Sample: {sample.Name} ({sample.Category}) ---");
            try
            {
                sample.Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FAILED {sample.Name}: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }

        private static bool IsHelp(string a) =>
            a is "-h" or "-?" or "/?" or "help" or "--help";

        private static bool IsRunAllSwitch(string a) =>
            string.Equals(a, "all", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "-all", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "--all", StringComparison.OrdinalIgnoreCase);

        private static void RunSamples(Func<Sample, bool> include)
        {
            var sampleArgs = Array.Empty<string>();
            foreach (var s in Samples)
            {
                if (!include(s))
                    continue;
                Console.WriteLine();
                Console.WriteLine($"========== {s.Category} / {s.Name} ==========");
                try
                {
                    s.Run(sampleArgs);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"FAILED {s.Name}: {ex.Message}");
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("MuPDF.NET Demo — see Demo/README.md for details.");
            Console.WriteLine();
            Console.WriteLine("  dotnet run                 all samples in SampleMenu (default, includes [diag])");
            Console.WriteLine("  dotnet run -- user           user-facing samples only (skips [diag])");
            Console.WriteLine("  dotnet run -- all            same as default");
            Console.WriteLine("  dotnet run -- diagnostics    [diag] samples only");
            Console.WriteLine("  dotnet run -- <sample-name>");
            Console.WriteLine("  dotnet run -- help");
            Console.WriteLine();
            Console.WriteLine("Samples by category ([diag] = regression / diagnostic fixtures):");
            var lastCat = "";
            foreach (var s in Samples)
            {
                if (s.Category != lastCat)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  [{s.Category}]");
                    lastCat = s.Category;
                }
                Console.WriteLine($"    {s.Name,-28} {s.Description}");
            }
            Console.WriteLine();
        }
    }
}
