using System.Text.RegularExpressions;

namespace Demo
{
    internal partial class Program
    {
        /// <summary>
        /// Demo entry for removing Adobe Fill &amp; Sign (<c>/ADBE_FillSign</c>) Form XObjects
        /// embedded in page content (not <c>/Annots</c>).
        /// </summary>
        /// <remarks>
        /// Usage:
        /// <c>dotnet run -- remove-adbe-fillsign -- path\to\input.pdf [path\to\output.pdf]</c>
        /// Without paths, uses <c>TestDocuments/Demo/FillSign.pdf</c> if present.
        /// </remarks>
        internal static void TestRemoveAdbeFillSign(string[] args)
        {
            Console.WriteLine("\n=== TestRemoveAdbeFillSign =====================");

            string[] paths = SampleExtraArgs(args);
            string inputPath = paths.Length > 0
                ? Path.GetFullPath(paths[0])
                : DemoPaths.Input("FillSign.pdf");

            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"File not found: {inputPath}");
                Console.WriteLine("Pass an input PDF: dotnet run -- remove-adbe-fillsign -- <input.pdf> [output.pdf]");
                return;
            }

            string outputPdf = paths.Length > 1
                ? Path.GetFullPath(paths[1])
                : DemoPaths.Output(Path.GetFileNameWithoutExtension(inputPath) + "_no_fillsign.pdf");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPdf)!);
            Console.WriteLine($"Input:  {inputPath}");
            Console.WriteLine($"Output: {outputPdf}");

            RemoveAdbeFillSign(inputPath, outputPdf, savePagePngs: true);
            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Removes Adobe Fill &amp; Sign content stored as <c>/ADBE_FillSign</c> Form XObjects
        /// in page content (not <c>/Annots</c>). Saving with garbage cleans unused objects.
        /// </summary>
        internal static void RemoveAdbeFillSign(string inputPath, string outputPdf, bool savePagePngs = true)
        {
            using var doc = new Document(inputPath);

            int catalog = doc.PdfCatalog;
            var fillSignForms = FindAdbeFillSignFormXrefs(doc, catalog);
            Console.WriteLine($"ADBE_FillSign Form XObjects: {fillSignForms.Count}");

            // 1) Strip /ADBE_FillSign BMC ... EMC from content streams
            // 2) Detach FillSign Form XObjects from page Resources
            for (int i = 0; i < doc.PageCount; i++)
            {
                using Page page = doc[i];
                foreach (int contentsXref in page.GetContents())
                {
                    byte[] raw = doc.XrefStream(contentsXref);
                    if (raw == null || raw.Length == 0)
                        continue;

                    string text = Encoding.Latin1.GetString(raw);
                    string cleaned = Regex.Replace(
                        text,
                        @"/ADBE_FillSign\s+BMC[\s\S]*?EMC\s*",
                        "",
                        RegexOptions.CultureInvariant);
                    if (cleaned != text)
                        doc.UpdateStream(contentsXref, Encoding.Latin1.GetBytes(cleaned), compress: true);
                }

                foreach (Entry xo in page.GetXObjects())
                {
                    if (xo.Xref <= 0 || string.IsNullOrEmpty(xo.Name) || !fillSignForms.Contains(xo.Xref))
                        continue;
                    doc.XrefSetKey(page.Xref, $"Resources/XObject/{xo.Name}", "null");
                }
            }

            // 3) Clear catalog Fill & Sign metadata / OCG config
            if (catalog > 0)
            {
                var catalogKeys = doc.XrefGetKeys(catalog);
                if (catalogKeys.Contains("ADBE_FillSignInfo"))
                    doc.XrefSetKey(catalog, "ADBE_FillSignInfo", "null");
                if (catalogKeys.Contains("OCProperties"))
                    doc.XrefSetKey(catalog, "OCProperties", "null");
            }

            // 4) Empty leftover FillSign Form objects (removed by garbage on save)
            foreach (int xref in fillSignForms)
            {
                try { doc.UpdateObject(xref, "<<>>"); }
                catch { /* ignore unusable xrefs */ }
            }

            string outDir = Path.GetDirectoryName(outputPdf) ?? ".";
            string outBase = Path.GetFileNameWithoutExtension(outputPdf);
            Directory.CreateDirectory(outDir);

            for (int i = 0; i < doc.PageCount; i++)
            {
                using Page page = doc[i];
                page.CleanContents();

                if (savePagePngs)
                {
                    using Pixmap pix = page.GetPixmap();
                    string pngPath = Path.Combine(outDir, $"{outBase}_page{i}.png");
                    pix.Save(pngPath);
                    Console.WriteLine($"Saved: {pngPath}");
                }
            }

            // garbage > 0 is important so emptied objects are dropped
            doc.Save(outputPdf, garbage: 4, deflate: 1);
            Console.WriteLine($"Saved: {outputPdf}");
        }

        /// <summary>Form XObjects that carry <c>/ADBE_FillSign</c> (not catalog <c>/ADBE_FillSignInfo</c>).</summary>
        private static List<int> FindAdbeFillSignFormXrefs(Document doc, int catalogXref)
        {
            var list = new List<int>();
            for (int xref = 1; xref < doc.XrefLength; xref++)
            {
                if (xref == catalogXref)
                    continue;
                string s;
                try { s = doc.XrefObject(xref); }
                catch { continue; }
                if (s == null)
                    continue;
                // Match /ADBE_FillSign << ... >> — not catalog /ADBE_FillSignInfo
                if (Regex.IsMatch(s, @"/ADBE_FillSign\s*<<", RegexOptions.CultureInvariant))
                    list.Add(xref);
            }
            return list;
        }

        /// <summary>Args after the sample name (and optional <c>--</c> separator).</summary>
        private static string[] SampleExtraArgs(string[] args)
        {
            if (args == null || args.Length == 0)
                return Array.Empty<string>();

            int start = 0;
            if (args.Length > 0 && !args[0].Contains('.') && !Path.IsPathRooted(args[0])
                && !File.Exists(args[0]))
                start = 1; // skip sample name
            if (start < args.Length && args[start] == "--")
                start++;
            if (start >= args.Length)
                return Array.Empty<string>();
            return args.AsSpan(start).ToArray();
        }
    }
}
