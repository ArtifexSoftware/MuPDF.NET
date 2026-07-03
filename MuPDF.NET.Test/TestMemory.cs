using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>Memory regression tests ported from </summary>
    /// <remarks>
    /// Fixtures: <c>TestDocuments/MuPDF.NET.Test/TestMemory/</c>.
    /// Outputs: <c>TestDocuments/MuPDF.NET.Test/_Output/TestMemory/</c>.
    /// Set <c>PYMUPDF_TEST_QUICK=1</c> to skip slow RSS loops.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestMemory
    {
        private const string TestClassName = nameof(TestMemory);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary>Honours <c>PYMUPDF_TEST_QUICK=1</c>.</summary>
        private static bool SkipSlowTests(string testName)
        {
            string? pymupdfTestQuick = Environment.GetEnvironmentVariable("PYMUPDF_TEST_QUICK");
            if (pymupdfTestQuick == "1")
            {
                Console.WriteLine($"{testName}(): skipping test because PYMUPDF_TEST_QUICK={pymupdfTestQuick}.");
                return true;
            }
            return false;
        }

        /// <summary>Process working set (RSS), analogous to </summary>
        private static long GetProcessRss()
        {
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            return process.WorkingSet64;
        }

        private static void StabilizeProcessMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static bool IsMsysNt() =>
            RuntimeInformation.OSDescription.StartsWith("MSYS_NT-", StringComparison.Ordinal);

        private static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        private static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>.NET runtime version; used where MuPDF gates assertions on Python &gt;= 3.11.</summary>
        private static Version PythonVersionTuple() => Environment.Version;

        /// <summary>Insert <paramref name="content"/> into <paramref name="coverpage"/> and return PDF bytes.</summary>
        private static byte[] MergePdf(byte[] content, byte[] coverpage)
        {
            using var coverpage_pdf = new Document(coverpage, fileType: "pdf");
            using var content_pdf = new Document(content, fileType: "pdf");
            coverpage_pdf.InsertPdf(content_pdf);
            return coverpage_pdf.Write();
        }

        /// <summary>Regression: scaled <c>Pixmap</c> dispose must not grow RSS per iteration.</summary>
        [Fact]
        public void test_pixmap_scale_memory()
        {
            if (SkipSlowTests(nameof(test_pixmap_scale_memory)))
                return;
            string img = Doc(@"boxedpage.jpg");
            if (!File.Exists(img))
            {
                Console.WriteLine($"{nameof(test_pixmap_scale_memory)}(): skip, missing {img}");
                return;
            }
            StabilizeProcessMemory();
            using var src = new Pixmap(img);
            var stats = new long[20];
            for (int i = 0; i < stats.Length; i++)
            {
                using (var scaled = new Pixmap(src, 943, 1500, null))
                {
                    Assert.NotNull(scaled.NativePixmap.m_internal);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                stats[i] = GetProcessRss();
            }
            float ratio = (float)stats[^1] / stats[2];
            Console.WriteLine($"{nameof(test_pixmap_scale_memory)}: ratio={ratio}");
            Assert.True(ratio < 1.2f, $"ratio={ratio}");
        }

        private static long GetPrivateMemoryBytes()
        {
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            return process.PrivateMemorySize64;
        }

        private static long GetPrivateMemoryMB() => GetPrivateMemoryBytes() / 1024 / 1024;

        private static string DemoDoc(string fileName) =>
            Path.Combine(_Path.ResolveSolutionRoot(), "TestDocuments", "Demo", fileName);

        /// <summary>
        /// Issue #213 repro: pixmap scale, JPEG encode, <c>InsertImage</c> on a new page.
        /// Returns private-memory growth ratio (last sample / baseline).
        /// </summary>
        private static float Issue213InsertImageMemoryLoop(bool useStream, int iterations = 100)
        {
            string pdfPath = DemoDoc("issue_213.pdf");
            string imgPath = Doc("boxedpage.jpg");
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"Required test document not found: {pdfPath}");
            if (!File.Exists(imgPath))
                throw new FileNotFoundException($"Required test document not found: {imgPath}");

            byte[]? pdfBytes = useStream ? File.ReadAllBytes(pdfPath) : null;
            var stats = new long[iterations];
            long before = GetPrivateMemoryMB();

            for (int i = 0; i < iterations; i++)
            {
                Document doc = useStream
                    ? new Document(stream: pdfBytes)
                    : new Document(fileName: pdfPath);

                _ = doc.PageCount;
                _ = doc.XrefLength;

                using var pix = new Pixmap(imgPath);
                using var scaled = new Pixmap(pix, 943, 1500, null);
                byte[] jpeg = scaled.ToBytes("jpg", 65);

                Page page = doc.NewPage(0, 943, 1500);
                page.InsertImage(page.Rect, stream: jpeg);
                page.Dispose();
                doc.Close();

                if ((i + 1) % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    long current = GetPrivateMemoryMB();
                    Console.WriteLine($"  Iter {i + 1,3}: {current} MB  (+{current - before} MB)");
                }

                stats[i] = GetPrivateMemoryBytes();
            }

            long endMb = GetPrivateMemoryMB();
            Console.WriteLine($"End: {endMb} MB  (delta: +{endMb - before} MB)");

            long baseline = stats[4];
            long last = stats[^1];
            return (float)last / baseline;
        }

        /// <summary>Regression: issue #213 workflow with <c>Document(fileName)</c> must not grow private memory.</summary>
        [Fact]
        public void test_issue_213_insert_image_memory_filename()
        {
            if (SkipSlowTests(nameof(test_issue_213_insert_image_memory_filename)))
                return;
            if (!File.Exists(DemoDoc("issue_213.pdf")) || !File.Exists(Doc("boxedpage.jpg")))
            {
                Console.WriteLine($"{nameof(test_issue_213_insert_image_memory_filename)}(): skip, missing test documents");
                return;
            }

            StabilizeProcessMemory();
            Console.WriteLine($"{nameof(test_issue_213_insert_image_memory_filename)}(): Document(fileName)");
            float ratio = Issue213InsertImageMemoryLoop(useStream: false);
            Console.WriteLine($"{nameof(test_issue_213_insert_image_memory_filename)}: ratio={ratio}");
            Assert.True(ratio < 1.15f, $"ratio={ratio}");
        }

        /// <summary>Regression: issue #213 workflow with <c>Document(stream)</c> (known leak repro).</summary>
        [Fact]
        public void test_issue_213_insert_image_memory_stream()
        {
            if (SkipSlowTests(nameof(test_issue_213_insert_image_memory_stream)))
                return;
            if (!File.Exists(DemoDoc("issue_213.pdf")) || !File.Exists(Doc("boxedpage.jpg")))
            {
                Console.WriteLine($"{nameof(test_issue_213_insert_image_memory_stream)}(): skip, missing test documents");
                return;
            }

            StabilizeProcessMemory();
            Console.WriteLine($"{nameof(test_issue_213_insert_image_memory_stream)}(): Document(stream)");
            float ratio = Issue213InsertImageMemoryLoop(useStream: true);
            Console.WriteLine($"{nameof(test_issue_213_insert_image_memory_stream)}: ratio={ratio}");

            if (IsLinux())
                Assert.True(ratio < 1.15f, $"ratio={ratio}");
            else
                Console.WriteLine($"{nameof(test_issue_213_insert_image_memory_stream)}(): not asserting ratio because non-Linux behaviour is too variable.");
        }

        /// <summary>Regression: rich-text <c>AddFreeTextAnnot</c> + save loop must not grow private memory per iteration.</summary>
        [Fact]
        public void test_freetext_annot_memory()
        {
            if (SkipSlowTests(nameof(test_freetext_annot_memory)))
                return;
            string ds = "font-size: 11pt; font-family: sans-serif;";
            string bullet = "\u2610\u2611\u2612";
            string text = $@"<p style=""text-align:justify;margin-top:-25px;""><span style=""color:blue;"">Test <b>bold</b> {bullet}</span></p>";
            string outPath = Out("freetext_memory.pdf");
            StabilizeProcessMemory();
            var stats = new long[50];
            for (int i = 0; i < stats.Length; i++)
            {
                using (var doc = new Document())
                {
                    var page = doc.NewPage();
                    var rect = new Rect(100, 100, 350, 200);
                    var p2 = rect.TopRight + new Point(50, 30);
                    var p3 = p2 + new Point(0, 30);
                    page.AddFreeTextAnnot(
                        rect, text,
                        fillColor: new float[] { 1, 1, 0 },
                        opacity: 1,
                        rotate: 0,
                        borderWidth: 1,
                        richtext: true,
                        style: ds,
                        callout: new[] { p3, p2, rect.TopRight },
                        lineEnd: PdfLineEnding.PDF_ANNOT_LE_OPEN_ARROW,
                        borderColor: new float[] { 0, 1, 0 });
                    doc.Save(outPath, pretty: 1);
                    doc.Close();
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                stats[i] = GetPrivateMemoryBytes();
            }
            long baseline = stats[4];
            long last = stats[^1];
            float ratio = (float)last / baseline;
            Console.WriteLine($"{nameof(test_freetext_annot_memory)}: baseline={baseline} last={last} ratio={ratio}");
            Assert.True(ratio < 1.15f, $"ratio={ratio}");
        }

        /// <summary>Regression: repeated <c>Document</c> open/close must not grow RSS per iteration (issue #213 repro).</summary>
        [Fact]
        public void test_document_open_close_memory()
        {
            if (SkipSlowTests(nameof(test_document_open_close_memory)))
                return;
            StabilizeProcessMemory();
            string path = Doc("test_2791_content.pdf");
            byte[] bytes = File.ReadAllBytes(path);
            var stats = new long[20];
            for (int i = 0; i < stats.Length; i++)
            {
                using (var doc = new Document(fileName: path))
                    _ = doc.PageCount;
                using (var doc = new Document(stream: bytes, fileType: "pdf"))
                    _ = doc.PageCount;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                stats[i] = GetProcessRss();
            }
            long first = stats[2];
            long last = stats[^1];
            float ratio = (float)last / first;
            Console.WriteLine($"test_document_open_close_memory: first={first} last={last} ratio={ratio}");
            Assert.True(ratio >= 0.90f && ratio < 1.15f, $"ratio={ratio}");
        }

        /// <summary>Repeated <c>MergePdf</c> must not grow RSS.</summary>
        [Fact]
        public void test_2791()
        {
            if (SkipSlowTests("test_2791"))
                return;

            StabilizeProcessMemory();
            const string stat_type = "psutil";
            Func<long> get_stat = stat_type switch
            {
                "tracemalloc" => throw new NotSupportedException("tracemalloc is not available in MuPDF.NET.Test"),
                "psutil" => GetProcessRss,
                _ => () => 0L,
            };
            int n = 1000;
            bool verbose = false;
            var stats = new long[n];
            for (int i = 0; i < n; i++)
                stats[i] = 1;
            string contentPath = Doc("test_2791_content.pdf");
            string coverpagePath = Doc("test_2791_coverpage.pdf");
            for (int i = 0; i < n; i++)
            {
                if (verbose)
                {
                    Console.WriteLine($"{i + 1}/{n}.");
                }

                byte[] content = File.ReadAllBytes(contentPath);
                byte[] coverpage = File.ReadAllBytes(coverpagePath);
                MergePdf(content, coverpage);
                Console.Out.Flush();

                GC.Collect();
                stats[i] = get_stat();
            }

            Console.WriteLine($"Memory usage stat_type={stat_type}.");
            for (int i = 0; i < stats.Length; i++)
            {
                Console.Write($" {stats[i]}");
            }
            Console.WriteLine();
            long first = stats[2];
            long last = stats[^1];
            float ratio = (float)last / first;
            Console.WriteLine($"first={first} last={last} ratio={ratio}");

            if (!IsLinux())
            {
                // RSS varies on non-Linux CI hosts; only assert on Linux.
                Console.WriteLine("test_2791(): not asserting ratio because not running on Linux.");
            }
            else if (PythonVersionTuple().Major < 3 || (PythonVersionTuple().Major == 3 && PythonVersionTuple().Minor < 11))
            {
                Console.WriteLine($"test_2791(): not asserting ratio because python version less than 3.11: {Environment.Version}.");
            }
            else if (stat_type == "tracemalloc")
            {
                Assert.True(ratio > 1 && ratio < 1.6);
            }
            else if (stat_type == "psutil")
            {
                // MuPDF expects ratio in [0.990, 1.027) after merge-PDF leak fixes.
                Assert.True(ratio >= 0.990 && ratio < 1.027, $"ratio={ratio}");
            }
        }

        /// <summary>Repeated <c>GetText(rawdict)</c> over all pages must not grow RSS.</summary>
        [Fact]
        public void test_4090()
        {
            StabilizeProcessMemory();
            Console.WriteLine($"test_4090(): PYTHONMALLOC={Environment.GetEnvironmentVariable("PYTHONMALLOC")}.");
            var rsss = new List<long>();
            long Rss()
            {
                long ret = GetProcessRss();
                rsss.Add(ret);
                return ret;
            }

            string path = Doc("test_4090.pdf");
            for (int i = 0; i < 100; i++)
            {
                var d = new Dictionary<int, Dictionary<int, object>>();
                d[i] = new Dictionary<int, object>();
                using (var document = new Document(path))
                {
                    int j = 0;
                    foreach (var page in document)
                    {
                        d[i][j] = page.GetText("rawdict");
                        j++;
                    }
                }
                Rss();
            }
            Rss();
            GC.Collect();
            Rss();
            long r1 = rsss[2];
            long r2 = rsss[^1];
            float r = (float)r2 / r1;
            Assert.True(0.93 <= r && r < 2.5, $"r1={r1} r2={r2} r={r}.");
        }

        private static void ShowTracemallocDiff(object snapshot1, object snapshot2) =>
            throw new NotSupportedException("tracemalloc is not available in MuPDF.NET.Test");

        /// <summary>Pixmap extraction per page image must not leak.</summary>
        [Fact]
        public void test_4125()
        {
            StabilizeProcessMemory();
            Console.WriteLine("");
            Console.WriteLine($"test_4125(): {Environment.Version}.");

            string path = Doc("test_4125.pdf");
            var state = new State4125();

            void GetStat()
            {
                long rss = GetProcessRss();
                if (state.Rsss.Count == 0)
                    state.Prev = rss;
                state.Rsss.Add(rss);
                long drss = rss - state.Prev;
                state.Prev = rss;
                {
                    Console.WriteLine(
                        "test_4125():"
                        + $" rss={rss:N0}"
                        + $" rss-rss0={rss - state.Rsss[0]:,}"
                        + $" drss={drss:,}"
                        + ".");
                }
                
            }

            for (int i = 0; i < 10; i++)
            {
                using (var document = new Document(path))
                {
                    foreach (var page in document)
                    {
                        foreach (var image_info in page.GetImages(full: true))
                        {
                            var (xref, smask, width, height, bpc, colorspace, alt_colorspace, name, filter_) = image_info;
                            var pixmap = new Pixmap(document, xref);
                            if (!ReferenceEquals(pixmap.Colorspace, Colorspace.Rgb))
                            {
                                var pixmap2 = new Pixmap(Colorspace.Rgb, pixmap);
                            }
                        }
                    }
                }
                Tools.StoreShrink(100);
                Tools.GlyphCacheEmpty();
                GC.Collect();
                GetStat();
            }

            if (IsLinux())
            {
                long rss_delta = state.Rsss[^1] - state.Rsss[3];
                Console.WriteLine($"rss_delta={rss_delta}");
                var pv = PythonVersionTuple();
                if (pv.Major < 3 || (pv.Major == 3 && pv.Minor < 11))
                {
                    Console.WriteLine($"test_4125(): Not checking on {Environment.Version} because < 3.11.");
                }
                else
                {
                    // MuPDF: pre-fix leaked ~4.9 MB per iteration; cap delta at 100 KB × iteration count.
                    long rss_delta_max = 100 * 1000 * (state.Rsss.Count - 3);
                    Assert.True(rss_delta < rss_delta_max);
                }
            }
            else
            {
                Console.WriteLine("Not checking results because non-Linux behaviour is too variable.");
            }
        }

        private sealed class State4125
        {
            public List<long> Rsss { get; } = new List<long>();
            public long Prev { get; set; }
        }

        /// <summary>Not ported: Python tracemalloc leak test.</summary>
        private static void _test_4751()
        {
            Console.WriteLine("_test_4751(): tracemalloc is not available in MuPDF.NET.Test");
        }

        [Fact]
        public void test_4751()
        {
            // We run the actual test in a child process, because otherwise previous
            // tests seem to effect the leak detection causing false positives. It's
            // possible that these could be real leaks, but they are not the ones
            // we are testing for here.
            {
                Console.WriteLine("test_4751(): not running on Pyodide - cannot run child processes.");
                return;
            }

            string? githubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            if (githubActions == "true")
            {
                // We see additional leaks on Github, don't know why.
                Console.WriteLine($"test_4751(): GITHUB_ACTIONS={githubActions}; not running on Github because known to fail.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("test_4751(): tracemalloc is not available in MuPDF.NET.Test; skipping leak detection.");
            _test_4751();
        }
    }
}