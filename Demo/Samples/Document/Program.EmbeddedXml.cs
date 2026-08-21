namespace Demo
{
    internal partial class Program
    {
        /// <summary>
        /// Extract and (re)embed ZUGFeRD / Factur-X XML using PDF EmbeddedFiles.
        /// Inputs: <c>zugferd-muster-rechnung.pdf</c>, <c>zugferd-muster-rechnung.xml</c>.
        /// </summary>
        internal static void TestZugferdEmbeddedXml(string[] args)
        {
            Console.WriteLine("\n=== TestZugferdEmbeddedXml =====================");

            string pdfPath = DemoPaths.Input("zugferd-muster-rechnung.pdf");
            string xmlPath = DemoPaths.Input("zugferd-muster-rechnung.xml");
            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"File not found: {pdfPath}");
                return;
            }
            if (!File.Exists(xmlPath))
            {
                Console.WriteLine($"File not found: {xmlPath}");
                return;
            }

            ExtractEmbeddedXml(pdfPath);
            AddEmbeddedXml(pdfPath, xmlPath);
        }

        /// <summary>List embedded files and extract the ZUGFeRD / Factur-X XML from a PDF.</summary>
        internal static void ExtractEmbeddedXml(string pdfPath)
        {
            Console.WriteLine("\n-- Extract embedded XML --");
            Console.WriteLine($"PDF: {pdfPath}");

            using var doc = new Document(pdfPath);
            Console.WriteLine($"EmbeddedFileCount: {doc.EmbeddedFileCount}");

            foreach (string name in doc.GetEmbeddedFileNames())
            {
                var info = doc.GetEmbeddedFileInfo(name);
                Console.WriteLine($"  name={info.GetValueOrDefault("name")}");
                Console.WriteLine($"  filename={info.GetValueOrDefault("filename")}");
                Console.WriteLine($"  description={info.GetValueOrDefault("description")}");
                Console.WriteLine($"  size={info.GetValueOrDefault("size")}");

                byte[] data = doc.GetEmbeddedFile(name);
                string outPath = DemoPaths.Output($"extracted-{SanitizeFileName(name)}");
                File.WriteAllBytes(outPath, data);
                Console.WriteLine($"  Saved: {outPath} ({data.Length} bytes)");
            }

            if (doc.EmbeddedFileCount == 0)
                Console.WriteLine("  (no embedded files)");
        }

        /// <summary>
        /// Embed <paramref name="xmlPath"/> into a copy of <paramref name="pdfPath"/> via <see cref="Document.AddEmbeddedFile"/>.
        /// </summary>
        internal static void AddEmbeddedXml(string pdfPath, string xmlPath)
        {
            Console.WriteLine("\n-- Add / replace embedded XML --");
            Console.WriteLine($"PDF: {pdfPath}");
            Console.WriteLine($"XML: {xmlPath}");

            byte[] xmlBytes = File.ReadAllBytes(xmlPath);
            string embName = "factur-x.xml";
            string outPdf = DemoPaths.Output("zugferd-with-embedded-xml.pdf");

            using var doc = new Document(pdfPath);

            // Replace any existing entry with the same logical name.
            if (doc.GetEmbeddedFileNames().Contains(embName))
            {
                Console.WriteLine($"Deleting existing embedded file '{embName}'");
                doc.DeleteEmbeddedFile(embName);
            }

            int xref = doc.AddEmbeddedFile(
                name: embName,
                buffer: xmlBytes,
                filename: embName,
                uFileName: embName,
                desc: "Factur-X / ZUGFeRD XML invoice");

            Console.WriteLine($"Added embedded file '{embName}' (xref={xref}, {xmlBytes.Length} bytes)");
            Console.WriteLine($"EmbeddedFileCount: {doc.EmbeddedFileCount}");

            doc.Save(outPdf, garbage: 4, deflate: 1);
            Console.WriteLine($"Saved: {outPdf}");

            // Round-trip check
            using var verify = new Document(outPdf);
            byte[] extracted = verify.GetEmbeddedFile(embName);
            bool match = extracted.AsSpan().SequenceEqual(xmlBytes);
            Console.WriteLine(match
                ? "Round-trip OK: extracted bytes match source XML."
                : $"Round-trip mismatch: extracted {extracted.Length} bytes, source {xmlBytes.Length} bytes.");
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
