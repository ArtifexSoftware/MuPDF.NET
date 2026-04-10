namespace Demo
{
    /// <summary>
    /// PDF4LLM <see cref="PDF4LLM.Pdf4LLM.ToMarkdown"/> demos aligned with repository <c>tests/</c> fixtures (golden markdown, OCR behavior).
    /// PDFs live under repo <c>tests/</c>; samples skip if files are missing.
    /// </summary>
    internal partial class Program
    {
        private static string LlmRepositoryRootFromAppBase() =>
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static string LlmRepositoryTestsDirectory() =>
            Path.Combine(LlmRepositoryRootFromAppBase(), "tests");

        private static bool LlmOcrEnvironmentLikelyAvailable() =>
            !string.IsNullOrEmpty(Utils.TESSDATA_PREFIX);

        /// <summary>ToMarkdown with fixed flags vs <c>tests/test_370_expected.md</c> (fixture: <c>tests/test_370.pdf</c>).</summary>
        internal static void Test4LlmToMarkdownCompareExpected370(string[] args)
        {
            _ = args;
            Console.WriteLine("\n=== Test4LlmToMarkdownCompareExpected370 (PDF4LLM) =======================");

            string testsDir = LlmRepositoryTestsDirectory();
            string pdfPath = Path.Combine(testsDir, "test_370.pdf");
            string expectedPath = Path.Combine(testsDir, "test_370_expected.md");
            if (!File.Exists(pdfPath) || !File.Exists(expectedPath))
            {
                Console.WriteLine($"Skip: need test_370.pdf and test_370_expected.md in: {testsDir}");
                return;
            }

            string expected = File.ReadAllText(expectedPath, Encoding.UTF8);
            Document document = new Document(pdfPath);
            try
            {
                string actual = ToMarkdown(
                    document,
                    header: false,
                    footer: false,
                    writeImages: false,
                    embedImages: false,
                    imageFormat: "jpg",
                    showProgress: true,
                    forceText: true,
                    pageSeparators: true);

                string actualPath = Path.Combine(AppContext.BaseDirectory, "llm_fixture_370_actual.md");
                File.WriteAllText(actualPath, actual, Encoding.UTF8);
                Console.WriteLine($"Wrote actual markdown: {actualPath}");

                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                {
                    Console.WriteLine("Mismatch vs tests/test_370_expected.md (first differences):");
                    LlmPrintLineDiff(expected, actual, maxLines: 40);
                }
                else
                {
                    Console.WriteLine("OK: actual matches test_370_expected.md");
                }
            }
            finally
            {
                document.Close();
            }
        }

        /// <summary>Default ToMarkdown on FFFD fixture; U+FFFD vs <c>TESSDATA_PREFIX</c> (fixture: <c>tests/test_ocr_loremipsum_FFFD.pdf</c>).</summary>
        internal static void Test4LlmToMarkdownOcrFixture1(string[] args)
        {
            _ = args;
            Console.WriteLine("\n=== Test4LlmToMarkdownOcrFixture1 =======================");

            string pdfPath = Path.Combine(LlmRepositoryTestsDirectory(), "test_ocr_loremipsum_FFFD.pdf");
            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"Skip: missing {pdfPath}");
                return;
            }

            Document doc = new Document(pdfPath);
            string md;
            try
            {
                md = ToMarkdown(doc);
            }
            finally
            {
                doc.Close();
            }

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "llm_ocr_fixture_1.md"), md, Encoding.UTF8);
            bool ocr = LlmOcrEnvironmentLikelyAvailable();
            Console.WriteLine($"TESSDATA_PREFIX set: {ocr}");
            bool hasReplacement = md.Contains(TesseractApi.ReplacementUnicode);
            if (ocr && hasReplacement)
                Console.WriteLine("Note: U+FFFD still present—check tessdata / language / PDF.");
            else if (ocr && !hasReplacement)
                Console.WriteLine("OK: no U+FFFD when tessdata is configured.");
            else if (!ocr && hasReplacement)
                Console.WriteLine("OK: U+FFFD present without tessdata.");
            else
                Console.WriteLine("Note: no U+FFFD without tessdata—compare llm_ocr_fixture_1.md.");
        }

        /// <summary><c>ToMarkdown(..., useOcr: false)</c> on FFFD fixture.</summary>
        internal static void Test4LlmToMarkdownOcrFixture2(string[] args)
        {
            _ = args;
            Console.WriteLine("\n=== Test4LlmToMarkdownOcrFixture2 =======================");

            string pdfPath = Path.Combine(LlmRepositoryTestsDirectory(), "test_ocr_loremipsum_FFFD.pdf");
            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"Skip: missing {pdfPath}");
                return;
            }

            Document doc = new Document(pdfPath);
            string md;
            try
            {
                md = ToMarkdown(doc, useOcr: false);
            }
            finally
            {
                doc.Close();
            }

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "llm_ocr_fixture_2.md"), md, Encoding.UTF8);
            bool hasReplacement = md.Contains(TesseractApi.ReplacementUnicode);
            Console.WriteLine(hasReplacement
                ? "OK: U+FFFD present with useOcr=false."
                : "Note: no U+FFFD with OCR off—fixture-dependent.");
        }

        /// <summary>SVG text fixture: compare default vs <c>useOcr: false</c> output size (fixture: <c>tests/test_ocr_loremipsum_svg.pdf</c>).</summary>
        internal static void Test4LlmToMarkdownOcrFixture3(string[] args)
        {
            _ = args;
            Console.WriteLine("\n=== Test4LlmToMarkdownOcrFixture3 =======================");

            string pdfPath = Path.Combine(LlmRepositoryTestsDirectory(), "test_ocr_loremipsum_svg.pdf");
            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"Skip: missing {pdfPath}");
                return;
            }

            Document doc = new Document(pdfPath);
            string md;
            string mdNoOcr;
            try
            {
                md = ToMarkdown(doc);
                mdNoOcr = ToMarkdown(doc, useOcr: false);
            }
            finally
            {
                doc.Close();
            }

            string baseDir = AppContext.BaseDirectory;
            File.WriteAllText(Path.Combine(baseDir, "llm_ocr_fixture_3.md"), md, Encoding.UTF8);
            File.WriteAllText(Path.Combine(baseDir, "llm_ocr_fixture_3_no_ocr.md"), mdNoOcr, Encoding.UTF8);

            bool ocr = LlmOcrEnvironmentLikelyAvailable();
            if (ocr)
            {
                if (mdNoOcr.Length < md.Length)
                    Console.WriteLine($"OK: with tessdata, no-OCR shorter ({mdNoOcr.Length} < {md.Length}).");
                else
                    Console.WriteLine($"Note: lengths OCR={md.Length}, no-OCR={mdNoOcr.Length} (environment-dependent).");
            }
            else
            {
                Console.WriteLine(string.Equals(md, mdNoOcr, StringComparison.Ordinal)
                    ? "OK: without tessdata, OCR on/off often match."
                    : "Note: outputs differ; compare llm_ocr_fixture_3*.md.");
            }
        }

        private static void LlmPrintLineDiff(string expected, string actual, int maxLines)
        {
            string[] a = expected.Replace("\r\n", "\n").Split('\n');
            string[] b = actual.Replace("\r\n", "\n").Split('\n');
            int n = Math.Max(a.Length, b.Length);
            int printed = 0;
            for (int i = 0; i < n && printed < maxLines; i++)
            {
                string lineA = i < a.Length ? a[i] : "<eof>";
                string lineB = i < b.Length ? b[i] : "<eof>";
                if (lineA == lineB)
                    continue;
                Console.WriteLine($"  line {i + 1}:");
                Console.WriteLine($"    expected: {LlmTruncateForConsole(lineA)}");
                Console.WriteLine($"    actual:   {LlmTruncateForConsole(lineB)}");
                printed++;
            }
            if (printed >= maxLines)
                Console.WriteLine("  ... (truncated)");
        }

        private static string LlmTruncateForConsole(string s, int max = 200)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
