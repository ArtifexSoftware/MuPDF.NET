// Demo - CLI tool ported from PyMuPDF's __main__.py
// Provides: show, clean, join, extract, gettext, embed-info/add/del/extract/copy commands.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MuPDF.NET;

namespace Demo
{
    internal static class Program
    {
        static void TestAnnot()
        {
            Document doc = new Document();
            for (int i = 0; i < 1; i++)
                doc.NewPage();

            Page page = doc[0];
            var annot = page.AddCaretAnnot(new Point(100, 100));
            annot.Update(rotate: 20);
            page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
            doc.Save1(@"E:\Pdf\Tmp\Test\Annot_AddCaretAnnot.pdf", annot);
            //doc.Save1(@"E:\Pdf\Tmp\Test\Annot_AddCaretAnnot.pdf", annot);
            //annot.GetApnMatrix();

            doc.Close();
        }
        static int Main(string[] args)
        {
            TestAnnot();
            return 0;
            /*
            if (args.Length == 0)
            {
                PrintHelp();
                return 0;
            }
            */
            string command = "show"; //args[0].ToLower();

            Console.WriteLine("\n========================================= Command: show =========================================\n");
            Console.WriteLine("Usage: show <input.pdf> [-password PW] [-catalog] [-trailer] [-metadata] [-xrefs 1,5-7] [-pages 1,5-7]");
            string[] cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\test.pdf", "-catalog", "-trailer", "-metadata-catalog", "-trailer", "-metadata" }; //args.Skip(1).ToArray();
            CommandShow(cmdArgs);

            Console.WriteLine("\n========================================= Command: clean =========================================\n");
            Console.WriteLine("Usage: clean <input.pdf> <output.pdf> [-password PW] [-pages 1,5-7] [-garbage 0-4] [-compress] [-linear] [-sanitize] [-pretty] [-ascii]");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\test.pdf", @"E:\Pdf\Tmp\6025432\2_.pdf", "-pages 1,5", "-compress", "-linear", "-sanitize", "-pretty", "-ascii" };
            CommandClean(cmdArgs);

            Console.WriteLine("\n========================================= Command: join =========================================\n");
            Console.WriteLine("Usage: join <input1.pdf[,password[,pages]]> [input2.pdf ...] -output <output.pdf>");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\test.pdf", @"E:\Pdf\Tmp\6025432\2_.pdf", "-output", @"E:\Pdf\Tmp\6025432\joined.pdf" };
            CommandJoin(cmdArgs);

            Console.WriteLine("\n========================================= Command: extract =========================================\n");
            Console.WriteLine("Usage: extract <input.pdf> [-images] [-fonts] [-output DIR] [-password PW] [-pages 1,5-7]");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\magazine.pdf", "-images", "-fonts", "-output", @"E:\Pdf\Tmp\6025432\extracted" };
            CommandExtract(cmdArgs);

            Console.WriteLine("\n========================================= Command: gettext =========================================\n");
            Console.WriteLine("Usage: gettext <input> [-password PW] [-mode simple|blocks|layout] [-pages 1,5-7,N] [-output FILE] [-grid N] [-fontsize N] [-noformfeed] [-skip-empty]");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\2.pdf", "-mode simple", "-pages 1", "-output", @"E:\Pdf\Tmp\6025432\output.txt", "-grid 4", "-fontsize 12", "-noformfeed", "-skip-empty" };
            CommandGetText(cmdArgs);

            Console.WriteLine("\n========================================= Command: embed-add =========================================\n");
            Console.WriteLine("Usage: embed-add <input.pdf> -name NAME -path FILE [-desc TEXT] [-output OUT.pdf] [-password PW]");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\2.pdf", "-name", "My_PDF", "-path", @"E:\Pdf\Tmp\6025432\image.png", "-desc Cover_Image", "-output", @"E:\Pdf\Tmp\6025432\output.pdf" };
            CommandEmbedAdd(cmdArgs);

            Console.WriteLine("\n========================================= Command: embed-copy =========================================\n");
            Console.WriteLine("Usage: embed-copy <target.pdf> -source <source.pdf> [-name NAME ...] [-output OUT.pdf] [-password PW] [-pwdsource PW]");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\test.pdf", "-source", @"E:\Pdf\Tmp\6025432\output.pdf", "-name", "My_PDF", "-output", @"E:\Pdf\Tmp\6025432\output3.pdf" };
            CommandEmbedCopy(cmdArgs);

            Console.WriteLine("\n========================================= Command: embed-info =========================================\n");
            Console.WriteLine("Usage: embed-info <input.pdf> [-name NAME] [-detail] [-password PW]");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\output.pdf", "-name", "My_PDF", "-detail" };
            CommandEmbedInfo(cmdArgs);

            Console.WriteLine("\n========================================= Command: embed-extract =========================================\n");
            Console.WriteLine("Usage: embed-extract <input.pdf> -name NAME [-output FILE] [-password PW]");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\output.pdf", "-name", "My_PDF", "-output", @"E:\Pdf\Tmp\6025432\output1.png" };
            CommandEmbedExtract(cmdArgs);

            Console.WriteLine("\n========================================= Command: embed-del =========================================\n");
            Console.WriteLine("Usage: embed-del <input.pdf> -name NAME [-output OUT.pdf] [-password PW]");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\output.pdf", "-name", "My_PDF", "-output", @"E:\Pdf\Tmp\6025432\output.pdf" };
            CommandEmbedDel(cmdArgs);

            Console.WriteLine("\n========================================= Command: embed-info =========================================\n");
            Console.WriteLine("Usage: embed-info <input.pdf> [-name NAME] [-detail] [-password PW]");
            cmdArgs = new[] { @"E:\Pdf\Tmp\6025432\output.pdf", "-name", "My_PDF", "-detail" };
            CommandEmbedInfo(cmdArgs);

            return 0;

            try
            {
                return command switch
                {
                    "show" => CommandShow(cmdArgs),
                    "clean" => CommandClean(cmdArgs),
                    "join" => CommandJoin(cmdArgs),
                    "extract" => CommandExtract(cmdArgs),
                    "gettext" => CommandGetText(cmdArgs),
                    "embed-info" => CommandEmbedInfo(cmdArgs),
                    "embed-add" => CommandEmbedAdd(cmdArgs),
                    "embed-del" => CommandEmbedDel(cmdArgs),
                    "embed-extract" => CommandEmbedExtract(cmdArgs),
                    "embed-copy" => CommandEmbedCopy(cmdArgs),
                    "-h" or "--help" or "help" => PrintHelp(),
                    _ => Error($"Unknown command: '{command}'. Use --help for usage.")
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────

        static string Center(string text, int width = 75, char pad = '-')
        {
            string s = $" {text} ";
            int total = Math.Max(width, s.Length);
            int left = (total - s.Length) / 2;
            int right = total - s.Length - left;
            return new string(pad, left) + s + new string(pad, right);
        }

        static int Error(string message)
        {
            Console.Error.WriteLine(message);
            return 1;
        }

        static int PrintHelp()
        {
            Console.WriteLine(Center("MuPDF.NET Demo CLI"));
            Console.WriteLine();
            Console.WriteLine("Usage: Demo <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  show           Display PDF document information");
            Console.WriteLine("  clean          Optimize PDF or create sub-PDF");
            Console.WriteLine("  join           Join pages from multiple PDFs");
            Console.WriteLine("  extract        Extract images and/or fonts to disk");
            Console.WriteLine("  gettext        Extract text in various formatting modes");
            Console.WriteLine("  embed-info     List embedded files");
            Console.WriteLine("  embed-add      Add an embedded file");
            Console.WriteLine("  embed-del      Delete an embedded file");
            Console.WriteLine("  embed-extract  Extract an embedded file to disk");
            Console.WriteLine("  embed-copy     Copy embedded files between PDFs");
            Console.WriteLine();
            Console.WriteLine("Use '<command> --help' for command-specific help.");
            return 0;
        }

        static Document OpenFile(string filename, string? password = null, bool requirePdf = true)
        {
            var doc = new Document(filename);
            if (!doc.IsPdf && requirePdf)
                throw new InvalidOperationException("This command supports PDF files only.");
            if (doc.NeedsPass)
            {
                if (string.IsNullOrEmpty(password))
                    throw new InvalidOperationException($"'{doc.Name}' requires a password.");
                if (!doc.Authenticate(password))
                    throw new InvalidOperationException("Authentication unsuccessful.");
            }
            return doc;
        }

        static List<int> ParsePageList(string spec, int pageCount)
        {
            int limit = pageCount + 1;
            string resolved = spec.Replace("N", (pageCount).ToString()).Replace(" ", "");
            var result = new List<int>();
            foreach (string item in resolved.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(item, out int single))
                {
                    if (single >= 1 && single < limit)
                        result.Add(single);
                    else
                        throw new ArgumentException($"Bad page specification: {item}");
                }
                else if (item.Contains('-'))
                {
                    var parts = item.Split('-', 2);
                    int i1 = int.Parse(parts[0]);
                    int i2 = int.Parse(parts[1]);
                    if (i1 < 1 || i1 >= limit || i2 < 1 || i2 >= limit)
                        throw new ArgumentException($"Bad page range: {item}");
                    if (i1 <= i2)
                        for (int i = i1; i <= i2; i++) result.Add(i);
                    else
                        for (int i = i1; i >= i2; i--) result.Add(i);
                }
                else
                    throw new ArgumentException($"Bad specification: {item}");
            }
            return result;
        }

        static (Dictionary<string, string?> named, List<string> positional) ParseArgs(
            string[] args, params string[] flags)
        {
            var named = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var positional = new List<string>();
            var flagSet = new HashSet<string>(flags, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    string key = args[i].TrimStart('-').ToLower();
                    if (flagSet.Contains(key))
                    {
                        named[key] = "true";
                    }
                    else if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        named[key] = args[++i];
                    }
                    else
                    {
                        named[key] = "true";
                    }
                }
                else
                {
                    positional.Add(args[i]);
                }
            }
            return (named, positional);
        }

        static string? Get(Dictionary<string, string?> d, string key, string? def = null)
            => d.TryGetValue(key, out var v) ? v : def;

        static bool Flag(Dictionary<string, string?> d, string key)
            => d.ContainsKey(key);

        // ─── show ───────────────────────────────────────────────────────

        static int CommandShow(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "catalog", "trailer", "metadata", "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count == 0)
            {
                Console.WriteLine("Usage: show <input.pdf> [-password PW] [-catalog] [-trailer] [-metadata] [-xrefs 1,5-7] [-pages 1,5-7]");
                return 0;
            }

            string input = pos[0];
            string? password = Get(opts, "password");
            using var doc = OpenFile(input, password, true);

            double size = new FileInfo(input).Length / 1024.0;
            string flag = "KB";
            if (size > 1000) { size /= 1024; flag = "MB"; }

            var meta = doc.GetMetadata();
            Console.WriteLine($"'{input}', pages: {doc.PageCount}, objects: {doc.XrefLength - 1}, {size:F1} {flag}, {meta.GetValueOrDefault("format", "")}, encryption: {meta.GetValueOrDefault("encryption", "None")}");

            if (Flag(opts, "metadata"))
            {
                Console.WriteLine(Center("PDF metadata"));
                foreach (var kv in doc.GetMetadata())
                    Console.WriteLine($"  {kv.Key}: {kv.Value}");
                Console.WriteLine();
            }

            if (Flag(opts, "catalog"))
            {
                Console.WriteLine(Center("PDF catalog"));
                int xref = doc.PdfCatalog;
                PrintXref(doc, xref);
                Console.WriteLine();
            }

            if (Flag(opts, "trailer"))
            {
                Console.WriteLine(Center("PDF trailer"));
                Console.WriteLine(doc.PdfTrailer());
                Console.WriteLine();
            }

            if (opts.ContainsKey("xrefs"))
            {
                Console.WriteLine(Center("object information"));
                var xrefs = ParsePageList(opts["xrefs"]!, doc.XrefLength);
                foreach (int xref in xrefs)
                {
                    PrintXref(doc, xref);
                    Console.WriteLine();
                }
            }

            if (opts.ContainsKey("pages"))
            {
                Console.WriteLine(Center("page information"));
                var pages = ParsePageList(opts["pages"]!, doc.PageCount);
                foreach (int pno in pages)
                {
                    int xref = doc.PageXref(pno - 1);
                    Console.WriteLine($"Page {pno}:");
                    PrintXref(doc, xref);
                    Console.WriteLine();
                }
            }

            return 0;
        }

        static void PrintXref(Document doc, int xref)
        {
            Console.WriteLine($"{xref} 0 obj");
            var keys = doc.XrefGetKeys(xref);
            string xrefStr = keys.Count > 0 ? string.Join("\n", keys.Select(k => $"  /{k} {doc.XrefGetKey(xref, k).value}")) : "";
            Console.WriteLine(xrefStr);
            if (doc.XrefIsStream(xref))
                Console.WriteLine("stream\n...bytes\nendstream");
            Console.WriteLine("endobj");
        }

        // ─── clean ──────────────────────────────────────────────────────

        static int CommandClean(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "compress", "ascii", "linear", "sanitize", "pretty", "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count < 2)
            {
                Console.WriteLine("Usage: clean <input.pdf> <output.pdf> [-password PW] [-pages 1,5-7] [-garbage 0-4] [-compress] [-linear] [-sanitize] [-pretty] [-ascii]");
                return 0;
            }

            string input = pos[0], output = pos[1];
            string? password = Get(opts, "password");
            using var doc = OpenFile(input, password, true);

            int garbage = int.Parse(Get(opts, "garbage", "0")!);
            bool compress = Flag(opts, "compress");
            bool clean = Flag(opts, "sanitize");
            bool linear = Flag(opts, "linear");

            string? pagesSpec = Get(opts, "pages");
            if (pagesSpec == null)
            {
                doc.Save(output, garbage: garbage>0?1:0, clean: clean?1:0, deflate: compress?1:0);
            }
            else
            {
                var pages = ParsePageList(pagesSpec, doc.PageCount);
                using var outDoc = new Document();
                foreach (int pno in pages)
                    outDoc.InsertPdf(doc, fromPage: pno - 1, toPage: pno - 1);
                outDoc.Save(output, garbage: garbage > 0 ? 1 : 0, clean: clean ? 1 : 0, deflate: compress ? 1 : 0);
            }

            Console.WriteLine($"Saved to '{output}'");
            return 0;
        }

        // ─── join ───────────────────────────────────────────────────────

        static int CommandJoin(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count == 0 || !opts.ContainsKey("output"))
            {
                Console.WriteLine("Usage: join <input1.pdf[,password[,pages]]> [input2.pdf ...] -output <output.pdf>");
                return 0;
            }

            string output = opts["output"]!;
            using var outDoc = new Document();

            foreach (string srcItem in pos)
            {
                var parts = srcItem.Split(',');
                string filename = parts[0];
                string? password = parts.Length > 1 ? parts[1] : null;
                using var src = OpenFile(filename, password, true);

                List<int> pageList;
                if (parts.Length > 2)
                    pageList = ParsePageList(string.Join(",", parts.Skip(2)), src.PageCount);
                else
                    pageList = Enumerable.Range(1, src.PageCount).ToList();

                foreach (int pno in pageList)
                    outDoc.InsertPdf(src, fromPage: pno - 1, toPage: pno - 1);
            }

            outDoc.Save(output, garbage: 1, deflate: 1);
            Console.WriteLine($"Joined to '{output}'");
            return 0;
        }

        // ─── extract ────────────────────────────────────────────────────

        static int CommandExtract(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "images", "fonts", "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count == 0)
            {
                Console.WriteLine("Usage: extract <input.pdf> [-images] [-fonts] [-output DIR] [-password PW] [-pages 1,5-7]");
                return 0;
            }

            bool doImages = Flag(opts, "images");
            bool doFonts = Flag(opts, "fonts");
            if (!doImages && !doFonts)
                return Error("Neither -images nor -fonts requested.");

            string input = pos[0];
            string? password = Get(opts, "password");
            using var doc = OpenFile(input, password, true);

            string outDir = Get(opts, "output") ?? Directory.GetCurrentDirectory();
            if (!Directory.Exists(outDir))
                return Error($"Output directory '{outDir}' does not exist.");

            var pagesSpec = Get(opts, "pages");
            var pages = pagesSpec != null
                ? ParsePageList(pagesSpec, doc.PageCount)
                : Enumerable.Range(1, doc.PageCount).ToList();

            var fontXrefs = new HashSet<int>();
            var imageXrefs = new HashSet<int>();

            foreach (int pno in pages)
            {
                if (doFonts)
                {
                    var fonts = doc.GetPageFonts(pno - 1);
                    foreach (var item in fonts)
                    {
                        int xref = item.Item1;
                        if (fontXrefs.Add(xref))
                        {
                            var (fontname, ext, _, buffer) = doc.ExtractFont(xref);
                            if (ext == "n/a" || buffer == null || buffer.Length == 0) continue;
                            string outname = Path.Combine(outDir, $"{fontname.Replace(' ', '-')}-{xref}.{ext}");
                            File.WriteAllBytes(outname, buffer);
                        }
                    }
                }

                if (doImages)
                {
                    var images = doc.GetPageImages(pno - 1);
                    foreach (var item in images)
                    {
                        int xref = item.Item1;
                        if (imageXrefs.Add(xref))
                        {
                            var imgData = doc.ExtractImage(xref);
                            if (imgData != null && imgData.ContainsKey("image"))
                            {
                                string ext = imgData.ContainsKey("ext") ? imgData["ext"].ToString()! : "png";
                                string outname = Path.Combine(outDir, $"img-{xref}.{ext}");
                                File.WriteAllBytes(outname, (byte[])imgData["image"]);
                            }
                        }
                    }
                }
            }

            if (doFonts) Console.WriteLine($"Saved {fontXrefs.Count} fonts to '{outDir}'");
            if (doImages) Console.WriteLine($"Saved {imageXrefs.Count} images to '{outDir}'");
            return 0;
        }

        // ─── gettext ────────────────────────────────────────────────────

        static int CommandGetText(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "noligatures", "convert-white", "extra-spaces",
                "noformfeed", "skip-empty", "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count == 0)
            {
                Console.WriteLine("Usage: gettext <input> [-password PW] [-mode simple|blocks|layout] [-pages 1,5-7,N] [-output FILE] [-grid N] [-fontsize N] [-noformfeed] [-skip-empty]");
                return 0;
            }

            string input = pos[0];
            string? password = Get(opts, "password");
            using var doc = OpenFile(input, password, requirePdf: false);

            string pagesSpec = Get(opts, "pages", "1-N")!;
            var pages = ParsePageList(pagesSpec, doc.PageCount);
            string mode = Get(opts, "mode", "layout")!;

            string? outputFile = Get(opts, "output");
            if (outputFile == null)
                outputFile = Path.ChangeExtension(input, ".txt");

            using var outStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            byte eop = Flag(opts, "noformfeed") ? (byte)'\n' : (byte)12;
            bool skipEmpty = Flag(opts, "skip-empty");

            foreach (int pno in pages)
            {
                var page = doc[pno - 1];
                string text;
                if (mode == "blocks")
                {
                    var blocks = page.GetTextBlocks();
                    if (blocks == null || blocks.Count == 0)
                    {
                        if (!skipEmpty) outStream.WriteByte(eop);
                        continue;
                    }
                    text = string.Join("", blocks.Select(b => b.Item5));
                }
                else
                {
                    text = page.GetText("text");
                }

                if (string.IsNullOrEmpty(text))
                {
                    if (!skipEmpty) outStream.WriteByte(eop);
                    continue;
                }
                var bytes = System.Text.Encoding.UTF8.GetBytes(text);
                outStream.Write(bytes, 0, bytes.Length);
                outStream.WriteByte(eop);
            }

            Console.WriteLine($"Text saved to '{outputFile}'");
            return 0;
        }

        // ─── embed-info ─────────────────────────────────────────────────

        static int CommandEmbedInfo(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "detail", "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count == 0)
            {
                Console.WriteLine("Usage: embed-info <input.pdf> [-name NAME] [-detail] [-password PW]");
                return 0;
            }

            using var doc = OpenFile(pos[0], Get(opts, "password"), true);
            var names = doc.EmbfileNames();
            if (names.Count == 0)
            {
                Console.WriteLine($"'{doc.Name}' contains no embedded files.");
                return 0;
            }

            string? filterName = Get(opts, "name");
            if (filterName != null)
            {
                if (!names.Contains(filterName))
                    return Error($"No such embedded file '{filterName}'");
                Console.WriteLine($"Printing 1 of {names.Count} embedded file(s):");
                Console.WriteLine($"  {filterName}");
                return 0;
            }

            Console.WriteLine($"'{doc.Name}' contains {names.Count} embedded file(s):");
            foreach (var name in names)
                Console.WriteLine($"  {name}");
            return 0;
        }

        // ─── embed-add ──────────────────────────────────────────────────

        static int CommandEmbedAdd(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count == 0
                || !opts.ContainsKey("name") || !opts.ContainsKey("path"))
            {
                Console.WriteLine("Usage: embed-add <input.pdf> -name NAME -path FILE [-desc TEXT] [-output OUT.pdf] [-password PW]");
                return 0;
            }

            string input = pos[0];
            using var doc = OpenFile(input, Get(opts, "password"), true);
            string name = opts["name"]!;
            string path = opts["path"]!;
            if (!File.Exists(path))
                return Error($"No such file '{path}'");

            byte[] data = File.ReadAllBytes(path);
            string desc = Get(opts, "desc", path)!;
            doc.EmbfileAdd(name, data, filename: path, desc: desc);

            string? output = Get(opts, "output");
            if (output != null && output != input)
                doc.Save(output);
            else
                doc.SaveIncr();

            Console.WriteLine($"Added embedded file '{name}'");
            return 0;
        }

        // ─── embed-del ──────────────────────────────────────────────────

        static int CommandEmbedDel(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count == 0 || !opts.ContainsKey("name"))
            {
                Console.WriteLine("Usage: embed-del <input.pdf> -name NAME [-output OUT.pdf] [-password PW]");
                return 0;
            }

            using var doc = OpenFile(pos[0], Get(opts, "password"), true);
            doc.EmbfileDel(opts["name"]!);

            string? output = Get(opts, "output");
            if (output != null && output != pos[0])
                doc.Save(output, garbage: 1);
            else
                doc.SaveIncr();

            Console.WriteLine($"Deleted embedded file '{opts["name"]}'");
            return 0;
        }

        // ─── embed-extract ──────────────────────────────────────────────

        static int CommandEmbedExtract(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count == 0 || !opts.ContainsKey("name"))
            {
                Console.WriteLine("Usage: embed-extract <input.pdf> -name NAME [-output FILE] [-password PW]");
                return 0;
            }

            using var doc = OpenFile(pos[0], Get(opts, "password"), true);
            string name = opts["name"]!;
            byte[] data = doc.EmbfileGet(name);
            string? outputFile = Get(opts, "output") ?? name;
            File.WriteAllBytes(outputFile, data);
            Console.WriteLine($"Saved embedded file '{name}' as '{outputFile}'");
            return 0;
        }

        // ─── embed-copy ─────────────────────────────────────────────────

        static int CommandEmbedCopy(string[] args)
        {
            var (opts, pos) = ParseArgs(args, "help", "h");
            if (Flag(opts, "help") || Flag(opts, "h") || pos.Count == 0 || !opts.ContainsKey("source"))
            {
                Console.WriteLine("Usage: embed-copy <target.pdf> -source <source.pdf> [-name NAME ...] [-output OUT.pdf] [-password PW] [-pwdsource PW]");
                return 0;
            }

            using var doc = OpenFile(pos[0], Get(opts, "password"), true);
            using var src = OpenFile(opts["source"]!, Get(opts, "pwdsource"), true);

            var srcNames = new HashSet<string>(src.EmbfileNames());
            var filterName = Get(opts, "name");
            var names = filterName != null ? new HashSet<string> { filterName } : srcNames;

            foreach (var name in names)
            {
                byte[] data = src.EmbfileGet(name);
                doc.EmbfileAdd(name, data);
                Console.WriteLine($"Copied entry '{name}'");
            }

            string? output = Get(opts, "output");
            if (output != null && output != pos[0])
                doc.Save(output, garbage: 1);
            else
                doc.SaveIncr();

            return 0;
        }
    }
}
