using System.Diagnostics;

namespace Demo
{
    internal partial class Program
    {
        /// <summary>
        /// Issue #191 — wall time / CPU for parallel <see cref="Page.GetPixmap"/>
        /// (independent documents vs one shared document).
        /// </summary>
        internal static void TestIssue191(string[] args)
        {
            Console.WriteLine("\n=== issue-191: parallel GetPixmap (per-document native lock) ===");
            Console.WriteLine("https://github.com/ArtifexSoftware/MuPDF.NET/issues/191");

            string[] rest = args ?? Array.Empty<string>();
            if (rest.Length > 0 && string.Equals(rest[0], "issue-191", StringComparison.OrdinalIgnoreCase))
                rest = rest.Skip(1).ToArray();

            int jobs = Math.Max(Environment.ProcessorCount * 2, 16);
            int dop = Environment.ProcessorCount;
            string mode = "all";
            string pdfPath = null;
            float zoom = 2f;

            if (rest.Length > 0 && int.TryParse(rest[0], out int j))
                jobs = Math.Max(j, 1);
            if (rest.Length > 1 && int.TryParse(rest[1], out int d))
                dop = Math.Max(d, 1);
            if (rest.Length > 2)
                mode = rest[2];
            if (rest.Length > 3)
                pdfPath = rest[3];

            Console.WriteLine($"  processors={Environment.ProcessorCount}  jobs={jobs}  dop={dop}  zoom={zoom}");

            byte[] pdf;
            string source;
            if (!string.IsNullOrEmpty(pdfPath))
            {
                pdf = File.ReadAllBytes(pdfPath);
                source = Path.GetFullPath(pdfPath);
            }
            else
            {
                pdf = Issue191BuildVectorPdf(pageCount: 1, primitives: 8000);
                source = "synthetic-vector (1 page, 8000 rects)";
            }

            using (var probe = new Document(stream: pdf))
                Console.WriteLine($"  source={source}  bytes={pdf.Length}  pages={probe.PageCount}");

            Issue191Warmup(pdf, zoom);

            if (string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase))
            {
                var sequential = Issue191Run("sequential", pdf, jobs, dop: 1, zoom, shared: false);
                Issue191Run("independent", pdf, jobs, dop, zoom, shared: false, baselineMs: sequential);
                Issue191Run("shared", pdf, jobs, dop, zoom, shared: true, baselineMs: sequential);

                string magazine = DemoPaths.Input("Magazine.pdf");
                if (string.IsNullOrEmpty(pdfPath) && File.Exists(magazine))
                {
                    Console.WriteLine();
                    byte[] mag = File.ReadAllBytes(magazine);
                    using (var probe = new Document(stream: mag))
                        Console.WriteLine($"--- same modes on Magazine.pdf ({probe.PageCount} pages, mixed content) ---");
                    Issue191Warmup(mag, zoom);
                    sequential = Issue191Run("sequential", mag, jobs, dop: 1, zoom, shared: false);
                    Issue191Run("independent", mag, jobs, dop, zoom, shared: false, baselineMs: sequential);
                    Issue191Run("shared", mag, jobs, dop, zoom, shared: true, baselineMs: sequential);
                }

                Console.WriteLine();
                Console.WriteLine("  note: independent should use several cores on vector-heavy pages (separate documents).");
                Console.WriteLine("  shared stays serialized on LoadPage/GetDisplayList for that one document; raster can still overlap.");
                return;
            }

            bool shared = string.Equals(mode, "shared", StringComparison.OrdinalIgnoreCase);
            Issue191Run(mode, pdf, jobs, dop, zoom, shared);
        }

        private static byte[] Issue191BuildVectorPdf(int pageCount, int primitives)
        {
            using var doc = new Document();
            var black = new float[] { 0, 0, 0 };
            for (int p = 0; p < pageCount; p++)
            {
                using Page page = doc.NewPage(width: 612, height: 792);
                using var shape = page.NewShape();
                for (int i = 0; i < primitives; i++)
                {
                    float x = 8 + (i % 48) * 12.5f;
                    float y = 8 + (i / 48) * 9.5f;
                    shape.DrawRect(new Rect(x, y, x + 11, y + 8));
                }
                shape.Finish(color: black, width: 0.25f);
                shape.Commit();
            }
            return doc.Write();
        }

        private static void Issue191Warmup(byte[] pdf, float zoom)
        {
            using var doc = new Document(stream: pdf);
            using Page page = doc.LoadPage(0);
            using var pix = page.GetPixmap(new Matrix(zoom, zoom));
            _ = pix.Width;
        }

        private static double Issue191Run(
            string mode,
            byte[] pdf,
            int jobs,
            int dop,
            float zoom,
            bool shared,
            double baselineMs = 0)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var proc = Process.GetCurrentProcess();
            proc.Refresh();
            long startPrivate = proc.PrivateMemorySize64;
            TimeSpan cpu0 = proc.TotalProcessorTime;
            var sw = Stopwatch.StartNew();

            int pages;
            int pixels = 0;
            using (var probe = new Document(stream: pdf))
                pages = probe.PageCount;

            if (string.Equals(mode, "sequential", StringComparison.OrdinalIgnoreCase) || dop <= 1 && !shared)
            {
                for (int i = 0; i < jobs; i++)
                    pixels = Issue191RenderIndependent(pdf, i % pages, zoom);
            }
            else if (shared)
            {
                using var doc = new Document(stream: pdf);
                Parallel.For(0, jobs, new ParallelOptions { MaxDegreeOfParallelism = dop }, i =>
                {
                    int n = Issue191RenderShared(doc, i % pages, zoom);
                    Interlocked.Exchange(ref pixels, n);
                });
            }
            else
            {
                Parallel.For(0, jobs, new ParallelOptions { MaxDegreeOfParallelism = dop }, i =>
                {
                    int n = Issue191RenderIndependent(pdf, i % pages, zoom);
                    Interlocked.Exchange(ref pixels, n);
                });
            }

            sw.Stop();
            proc.Refresh();
            double wallMs = sw.Elapsed.TotalMilliseconds;
            double cpuMs = (proc.TotalProcessorTime - cpu0).TotalMilliseconds;
            double cpuCores = wallMs > 0 ? cpuMs / wallMs : 0;
            long peakPrivate = proc.PrivateMemorySize64;
            double speedup = baselineMs > 0 && wallMs > 0 ? baselineMs / wallMs : 0;

            Console.WriteLine(
                $"  {mode,-12} wallMs={wallMs,8:F0}  cpuMs={cpuMs,8:F0}  cpuCores={cpuCores,5:F2}  " +
                $"deltaPrivateMB={(peakPrivate - startPrivate) / (1024.0 * 1024.0),6:F1}  pixels={pixels}" +
                (speedup > 0 ? $"  speedup={speedup:F2}x" : ""));
            return wallMs;
        }

        private static int Issue191RenderIndependent(byte[] pdf, int pageNo, float zoom)
        {
            using var doc = new Document(stream: pdf);
            using Page page = doc.LoadPage(pageNo);
            using var pix = page.GetPixmap(new Matrix(zoom, zoom));
            return pix.Width * pix.Height;
        }

        private static int Issue191RenderShared(Document doc, int pageNo, float zoom)
        {
            using Page page = doc.LoadPage(pageNo);
            using var pix = page.GetPixmap(new Matrix(zoom, zoom));
            return pix.Width * pix.Height;
        }
    }
}
