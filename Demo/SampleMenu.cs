namespace Demo
{
    /// <summary>
    /// Demo samples grouped by MuPDF.NET / PDF4LLM feature areas. With no arguments, runs every sample.
    /// Use <c>dotnet run -- help</c> for the list, or <c>dotnet run -- &lt;name&gt;</c> for one sample.
    /// </summary>
    public static class SampleMenu
    {
        /// <summary>Library-facing group (matches folders under <c>Samples/</c> and major API surfaces).</summary>
        private sealed record Sample(string Category, string Name, string Description, Action<string[]> Run);

        /// <summary>Order matches <c>Samples/</c> layout; PDF4LLM extras live in <c>Samples/Llm/Program.Llm.*.Fixtures.cs</c>.</summary>
        private static readonly Sample[] Samples =
        {
            // —— Document & I/O (MuPDF.NET Document, open/save, streams) —— Samples/Document
            new("Document & I/O", "hello-new-pdf", "Hello World on a new PDF", a => Program.TestHelloWorldToNewDocument(a)),
            new("Document & I/O", "hello-existing-pdf", "Hello World on existing Blank.pdf", a => Program.TestHelloWorldToExistingDocument(a)),
            new("Document & I/O", "join-pdf", "Insert pages from another PDF", a => Program.TestJoinPdfPages(a)),
            new("Document & I/O", "metadata", "Print document metadata", _ => Program.TestMetadata()),
            new("Document & I/O", "move-file", "Save through MemoryStream and move output", _ => Program.TestMoveFile()),
            new("Document & I/O", "unicode-doc", "Save PDF with unicode filename", _ => Program.TestUnicodeDocument()),
            new("Document & I/O", "memory-leak", "Open/close documents in a loop", _ => Program.TestMemoryLeak()),

            // —— Text, story & vector drawing (Page, Story, TextWriter, Shape) —— Samples/TextDrawing
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
            new("Barcodes", "read-barcode", "Read barcodes from image and PDF", a => Program.TestReadBarcode(a)),
            new("Barcodes", "read-datamatrix", "Read Data Matrix from PDF", _ => Program.TestReadDataMatrix()),
            //new("Barcodes", "read-qrcode", "Render PDF page and read QR from PNG", a => Program.TestReadQrCode(a)),
            new("Barcodes", "write-barcode", "Write many barcode types to PDF and PNG", a => Program.TestWriteBarcode(a)),
            new("Barcodes", "write-barcode1", "Write CODE39/CODE128/DM with Units rects", _ => Program.TestWriteBarcode1()),

            // —— PDF4LLM —— Samples/Llm
            new("PDF4LLM", "rag-markdown", "Legacy RAG: PDF4LLM.ToMarkdown with UseLayout=false (Magazine.pdf)", _ => Program.TestMuPdfRagToMarkdown()),
            new("PDF4LLM", "table", "Detect tables and export markdown", _ => Program.TestTable()),
            new("PDF4LLM", "table-extract-1", "Dump detected tables by page to console", _ => Program.TestTableExtract1()),
            new("PDF4LLM", "table-extract-2", "Export detected tables to tables.csv", _ => Program.TestTableExtract2()),
            new("PDF4LLM", "table-extract-3", "Merge continued table pages by column count", _ => Program.TestTableExtract3()),
            new("PDF4LLM", "table-ocr", "Extract OCR text from Ocr.pdf", _ => Program.TestOcr()),
            new("PDF4LLM", "llm-reader-save-pages", "Load markdown chunks and save per-page .md files", _ => Program.TestLLM2()),
            new("PDF4LLM", "markdown-reader", "LlamaIndex PDFMarkdownReader", _ => Program.TestMarkdownReader()),
            new("PDF4LLM", "llm-to-markdown-fixture-370", "ToMarkdown vs tests/test_370_expected.md (needs tests/test_370.pdf)", a => Program.Test4LlmToMarkdownCompareExpected370(a)),
            new("PDF4LLM", "llm-to-markdown-ocr-1", "ToMarkdown + U+FFFD fixture (tests/test_ocr_loremipsum_FFFD.pdf)", a => Program.Test4LlmToMarkdownOcrFixture1(a)),
            new("PDF4LLM", "llm-to-markdown-ocr-2", "ToMarkdown useOcr=false on FFFD fixture", a => Program.Test4LlmToMarkdownOcrFixture2(a)),
            new("PDF4LLM", "llm-to-markdown-ocr-3", "ToMarkdown OCR on/off on SVG fixture", a => Program.Test4LlmToMarkdownOcrFixture3(a)),
            new("PDF4LLM", "llm-pdf-reader-empty", "PDFMarkdownReader: new PDF, one blank page", a => Program.Test4LlmPdfMarkdownReaderEmptyPage(a)),
            new("PDF4LLM", "llm-pdf-reader-missing-file", "PDFMarkdownReader: missing path → FileNotFoundException", a => Program.Test4LlmPdfMarkdownReaderMissingFile(a)),

            // —— Regression & diagnostics —— Samples/Regression
            new("Regression & diagnostics", "issue-213", "Repro: drawing paths / line width", _ => Program.TestIssue213()),
            new("Regression & diagnostics", "issue-1880", "Repro: read Data Matrix barcodes", _ => Program.TestIssue1880()),
            new("Regression & diagnostics", "issue-234", "Repro: pixmap scale + insert image", _ => Program.TestIssue234()),
            new("Regression & diagnostics", "jbig2", "Rewrite images with FAX recompression", _ => Program.TestRecompressJBIG2()),
        };

        private static readonly Dictionary<string, Sample> ByName = BuildIndex();

        private static Dictionary<string, Sample> BuildIndex()
        {
            var d = new Dictionary<string, Sample>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in Samples)
            {
                d[s.Name] = s;
            }
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
                RunAll();
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
            sample.Run(args);
        }

        private static bool IsHelp(string a) =>
            a is "-h" or "-?" or "/?" or "help" or "--help";

        private static bool IsRunAllSwitch(string a) =>
            string.Equals(a, "all", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "-all", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "--all", StringComparison.OrdinalIgnoreCase);

        private static void RunAll()
        {
            var sampleArgs = Array.Empty<string>();
            foreach (var s in Samples)
            {
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
            Console.WriteLine("MuPDF.NET Demo — samples mirror library areas under Demo/Samples/. Default: run all.");
            Console.WriteLine();
            Console.WriteLine("  dotnet run                 (or: dotnet run -- -all)");
            Console.WriteLine("  dotnet run -- <sample-name>");
            Console.WriteLine("  dotnet run -- help");
            Console.WriteLine();
            Console.WriteLine("Samples by category:");
            var lastCat = "";
            foreach (var s in Samples)
            {
                if (s.Category != lastCat)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  [{s.Category}]");
                    lastCat = s.Category;
                }

                Console.WriteLine($"    {s.Name,-22} {s.Description}");
            }

            Console.WriteLine();
        }
    }
}
