// Pixmap tests
// * make pixmap from a PDF xref and compare with extracted image
// * pixmap from file and from binary image and compare
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using mupdf;

namespace MuPDF.NET.Test
{
    [CollectionDefinition("MuPDF.NET pixmap", DisableParallelization = true)]
    public class MuPDFPixmapTestCollection
    {
    }

    [Collection("MuPDF.NET pixmap")]
    public class TestPixmap
    {
        private const string TestClassName = nameof(TestPixmap);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static readonly string epub = Doc("Bezier.epub");
        private static readonly string pdf = Doc("001003ED.pdf");
        private static readonly string imgfile = Doc("nur-ruhig.jpg");

        private static string Resource(string name) => Doc(name);

        private static string TestsPath(string name) =>
            name.StartsWith("resources/", StringComparison.Ordinal)
                ? Doc(name["resources/".Length..])
                : Out(name);

        private static bool HasFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required test document not found: {path}");
            return true;
        }
        private static bool SkipSlowTests(string testName)
        {
            return false;
        }

        private static IEnumerable<(int xx, int yy)> product(int xCount, int yCount)
        {
            for (int yy = 0; yy < yCount; yy++)
            for (int xx = 0; xx < xCount; xx++)
                yield return (xx, yy);
        }

        private static Rect BboxFromBlock(Dictionary<string, object> blk)
        {
            object bboxObj = blk["bbox"];
            if (bboxObj is float[] fa)
                return new Rect(fa[0], fa[1], fa[2], fa[3]);
            if (bboxObj is float[] da)
                return new Rect((float)da[0], (float)da[1], (float)da[2], (float)da[3]);
            var list = (System.Collections.IList)bboxObj;
            return new Rect(
                Convert.ToSingle(list[0]),
                Convert.ToSingle(list[1]),
                Convert.ToSingle(list[2]),
                Convert.ToSingle(list[3]));
        }

        [Fact]
        public void test_pagepixmap()
        {
            if (!HasFile(epub)) return;
            using var doc = new Document(epub);

            var page = doc[0];
            var pix = page.GetPixmap();
            pix.Save(Out("test_pagepixmap_0.png"));
            Assert.Equal(page.Rect.IRect, pix.IRect);
            pix = page.GetPixmap(alpha: true);
            pix.Save(Out("test_pagepixmap_1.png"));
            Assert.NotEqual(0, pix.alpha);
            Assert.Equal(pix.Colorspace.N + pix.alpha, pix.N);
        }

        [Fact]
        public void test_pdfpixmap()
        {
            if (!HasFile(pdf)) return;
            using var doc = new Document(pdf);
            var img = doc.get_page_images(0)[0];
            var pix = new Pixmap(doc, img.xref);
            Assert.Equal(img.width, pix.Width);
            Assert.Equal(img.height, pix.Height);
            var extractimg = doc.extract_image(img.xref);
            Assert.Equal(pix.Width, Convert.ToInt32(extractimg["width"]));
            Assert.Equal(pix.Height, Convert.ToInt32(extractimg["height"]));
            pix.Save(Out("test_pdfpixmap.png"));
        }

        [Fact]
        public void test_filepixmap()
        {
            if (!HasFile(imgfile)) return;
            var pix1 = new Pixmap(imgfile);
            byte[] stream = File.ReadAllBytes(imgfile);
            var pix2 = new Pixmap(stream);
            Assert.Equal(pix1.ToString(), pix2.ToString());
            Assert.Equal(pix1.Digest, pix2.Digest);
            pix1.Save(Out("test_filepixmap_0.png"));
            pix2.Save(Out("test_filepixmap_1.png"));
        }

        [Fact]
        public void test_pilsave()
        {
            if (!HasFile(imgfile)) return;
            try
            {
                var pix1 = new Pixmap(imgfile);
                byte[] stream = pix1.pil_tobytes("JPEG");
                var pix2 = new Pixmap(stream);
                Assert.Equal(pix1.ToString(), pix2.ToString());
                pix1.Save(Out("test_pilsave_0.png"));
                pix2.Save(Out("test_pilsave_1.png"));
            }
            // except ModuleNotFoundError:
            catch (Exception ex) when (ex is DllNotFoundException or TypeLoadException or NotImplementedException)
            {
                Assert.True(
                    OperatingSystem.IsWindows() && IntPtr.Size == 4,
                    $"Unexpected exception on non-Windows 32-bit: {ex}");
            }
        }

        [Fact]
        public void test_save()
        {
            if (!HasFile(imgfile)) return;
            var pix1 = new Pixmap(imgfile);

            string outfile = Out("test_save_0.png");
            try
            {
                pix1.Save(outfile, output: "png");
                var pix2 = new Pixmap(outfile);
                Assert.Equal(pix1.ToString(), pix2.ToString());
                pix2.Save(Out("test_save_1.png"));
            }
            finally
            {
            }
        }

        [Fact]
        public void test_setalpha()
        {
            if (!HasFile(imgfile)) return;
            var pix1 = new Pixmap(imgfile);
            pix1.Save(Out("test_setalpha_1.png"));
            int opa = (int)(255 * 0.3);  // corresponding to 30% transparency
            var alphas = Enumerable.Repeat((byte)opa, pix1.Width * pix1.Height).ToArray();
            var pix2 = new Pixmap(pix1, 1);  // add alpha channel
            pix2.set_alpha(alphas);  // make image 30% transparent
            pix2.Save(Out("test_setalpha_2.png"));
            byte[] samples = pix2.samples;  // copy of samples
            var t = new byte[samples.Length / 4];
            for (int i = 0, j = 0; i < samples.Length; i += 4, j++)
                t[j] = samples[i + 3];
            pix1.Save(Out("test_setalpha_0.png"));
            pix2.Save(Out("test_setalpha_1.png"));
            Assert.Equal(alphas, t);
        }

        [Fact]
        public void test_color_count()
        {
            // This is known to fail if MuPDF is built without MuPDF's custom config.h,
            if (!HasFile(imgfile)) return;
            var pm = new Pixmap(imgfile);
            Assert.Equal(40624, (int)pm.ColorCount());
        }

        [Fact]
        public void test_memoryview()
        {
            if (!HasFile(imgfile)) return;
            var pm = new Pixmap(imgfile);
            var samples = pm.samples_mv;
            Assert.IsType<PixmapSamplesMemoryView>(samples);
            Console.WriteLine($"samples={samples} samples.itemsize={samples.itemsize} samples.nbytes={samples.nbytes} samples.ndim={samples.ndim} samples.shape={samples.shape} samples.strides={samples.strides}");
            Assert.Equal(1, samples.itemsize);
            Assert.Equal(659817, samples.nbytes);
            Assert.Equal(1, samples.ndim);
            Assert.Equal(new[] { 659817 }, samples.shape);
            Assert.Equal(new[] { 1 }, samples.strides);

            byte[] color = pm.GetPixelBytes(100, 100);
            Console.WriteLine($"color=({string.Join(", ", color)})");
            Assert.Equal(new byte[] { 83, 66, 40 }, color);
        }

        [Fact]
        public void test_samples_ptr()
        {
            if (!HasFile(imgfile)) return;
            var pm = new Pixmap(imgfile);
            IntPtr samples = pm.SamplesPtr;
            Console.WriteLine($"samples={samples}");
            Assert.NotEqual(IntPtr.Zero, samples);
        }

        [Fact]
        public void test_2369()
        {
            int width = 13, height = 37;
            var image = new Pixmap(Colorspace.Gray, width, height, new byte[width * height], false);

            using (var doc = new Document(image.ToBytes(output: "pam"), "pam"))
            {
                byte[] test_pdf_bytes = doc.convert_to_pdf();

                using (var doc2 = new Document(test_pdf_bytes))
                {

                    var page = doc2[0];
                    int img_xref = page.GetImages()[0].xref;
                    var img = doc2.extract_image(img_xref);
                    byte[] img_bytes = (byte[])img["image"];
                    var pix = new Pixmap(img_bytes);
                    pix.Save(Out("test_2369.png"));
                }
            }
        }

        /// <summary>PyMuPDF <c>Document.__getitem__</c> for invalid / valid keys.</summary>
        [Fact]
        public void test_page_idx_int()
        {
            if (!HasFile(pdf)) return;
            using var doc = new Document(pdf);
            Assert.Throws<InvalidOperationException>(() => { var _ = doc["0"]; });
            Assert.NotNull(doc[0]);
            Assert.NotNull(doc[(0, 0)]);
        }

        [Fact]
        public void test_fz_write_pixmap_as_jpeg()
        {
            int width = 13, height = 37;
            var image = new Pixmap(Colorspace.Gray, width, height, new byte[width * height], false);

            using (var doc = new Document(image.ToBytes(output: "jpeg"), "jpeg"))
            {
                byte[] test_pdf_bytes = doc.convert_to_pdf();
                Document src = new Document(test_pdf_bytes);
                src.Save(Out("test_fz_write_pixmap_as_jpeg.pdf"));
            }
        }

        [Fact]
        public void test_3020()
        {
            if (!HasFile(imgfile)) return;
            var pm = new Pixmap(imgfile);
            _ = new Pixmap(pm, 20, 30, null);
            _ = new Pixmap(Colorspace.Gray, pm);
            var pm3 = new Pixmap(Colorspace.Gray, pm);
            var pm4 = new Pixmap(pm, pm3);
            pm4.Save(Out("test_3020.png"));
        }

        [Fact]
        public void test_3050()
        {
            // This is known to fail if MuPDF is built without it's default third-party
            string path = Resource("001003ED.pdf");
            if (!HasFile(path)) return;
            string path_expected = TestsPath("resources/test_3050_expected.png");
            if (!HasFile(path_expected)) return;

            Tools.ResetMupdfWarnings();

            using (var pdf_file = new Document(path))
            {
                int page_no = 0;

                var page = pdf_file[page_no];
                float zoom_x = 4.0f;
                float zoom_y = 4.0f;
                var matrix = new Matrix(zoom_x, zoom_y);
                var pix = page.GetPixmap(matrix: matrix);
                string path_out = Out("test_3050_0.png");
                pix.Save(path_out);
                Console.WriteLine($"pix.width={pix.Width} pix.height={pix.Height}");
                int n = 0;
                foreach (var pos in product(100, 100))
                {
                    if (pix.GetPixelBytes(pos.xx, pos.yy).Sum(b => (int)b) >= 600)
                    {
                        n += 1;
                        pix.SetPixel(pos.xx, pos.yy, new float[] { 255, 255, 255 });
                    }
                }

                string path_out2 = Out("test_3050_1.png");
                pix.Save(path_out2);
                float rms = _Compare.PixmapsRms(path_expected, path_out2);
                Console.WriteLine($"rms={rms}");
                Assert.Equal(0, rms);
                string wt = Tools.MupdfWarnings();
                var mupdf_version_tuple = _Version.mupdf_version_tuple();
                if (mupdf_version_tuple.CompareTo((1, 26, 0)) >= 0 && mupdf_version_tuple.CompareTo((1, 27, 0)) < 0)
                {
                    Assert.Equal("bogus font ascent/descent values (0 / 0)\nPDF stream Length incorrect", wt);
                }
                else
                {
                    Assert.Equal("PDF stream Length incorrect", wt);
                }
            }
        }

        [Fact]
        public void test_3058()
        {
            string pathPdf = Resource("test_3058.pdf");
            if (!HasFile(pathPdf)) return;

            using var doc = new Document(pathPdf);
            var images = doc[0].GetImages(full: true);
            var pix = new Pixmap(doc, 17);

            Assert.Contains("CMYK", pix.Colorspace.ToString(), StringComparison.OrdinalIgnoreCase);

            pix = new Pixmap(Colorspace.Rgb, pix);
            Assert.Contains("RGB", pix.Colorspace.ToString(), StringComparison.OrdinalIgnoreCase);

            string path = Out("test_3058.png");
            pix.Save(path);

            long s = new System.IO.FileInfo(path).Length;
            Assert.True(1800000 < s && s < 2600000, $"Unexpected size of {path}: {s}");
        }

        [Fact]
        public void test_3072()
        {
            string path = Resource("test_3072.pdf");
            if (!HasFile(path)) return;
            string outDir = Path.GetDirectoryName(Out("test_3072_1.jpg"))!;

            using (var doc = new Document(path))
            {
                var page_48 = doc[0];
                float[] bbox = { 147, 300, 447, 699 };
                var rect = new Rect(bbox[0], bbox[1], bbox[2], bbox[3]);
                var zoom = new Matrix(3, 3);
                var pix = page_48.GetPixmap(clip: rect.IRect, matrix: zoom);
                string image_save_path = Path.Combine(outDir, "test_3072_1.jpg");
                pix.Save(image_save_path, jpgQuality: 95);
            }

            using (var doc = new Document(path))
            {
                var page_49 = doc[1];
                float[] bbox = { 147, 543, 447, 768 };
                var rect = new Rect(bbox[0], bbox[1], bbox[2], bbox[3]);
                var zoom = new Matrix(3, 3);
                var pix = page_49.GetPixmap(clip: rect.IRect, matrix: zoom);
                string image_save_path = Path.Combine(outDir, "test_3072_2.jpg");
                pix.Save(image_save_path, jpgQuality: 95);
                string wt = Tools.MupdfWarnings();
                Assert.Equal(
                    "syntax error: cannot find ExtGState resource 'BlendMode0'\n"
                    + "encountered syntax errors; page may not be correct\n"
                    + "syntax error: cannot find ExtGState resource 'BlendMode0'\n"
                    + "encountered syntax errors; page may not be correct",
                    wt);
            }
        }

        [Fact]
        public void test_3134()
        {
            using var doc = new Document();

            var page = doc.NewPage();
            string path_rect = Out("test_3134_rect.jpg");
            string path_irect = Out("test_3134_irect.jpg");
            try
            {
                page.GetPixmap(clip: new IRect(0, 0, 100, 100)).Save(path_rect);
                page.GetPixmap(clip: new IRect(0, 0, 100, 100)).Save(path_irect);
                var stat_rect = new System.IO.FileInfo(path_rect);
                var stat_irect = new System.IO.FileInfo(path_irect);
                Console.WriteLine($" stat_rect={stat_rect}");
                Console.WriteLine($"stat_irect={stat_irect}");
                Assert.Equal(stat_rect.Length, stat_irect.Length);
            }
            finally
            {
            }
        }

        [Fact]
        public void test_3177()
        {
            string path = Resource("img-transparent.png");
            if (!HasFile(path)) return;
            var pixmap = new Pixmap(path);
            var pixmap2 = new Pixmap(colorPixmap: null, maskPixmap: pixmap);
            pixmap2.Save(Out("test_3177.png"));
        }

        [Fact]
        public void test_3493()
        {
            // If python3-gi is installed, we check fix for #3493, where importing gi
            // would load an older version of libjpeg than is used in MuPDF, and break
            // MuPDF.
            // Excluded by default in sysinstall tests: a fresh venv may not pick up the expected Python package layout.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine($"Not running because not Linux: platform={Environment.OSVersion.Platform}");
                return;
            }
            // Linux-only subprocess/venv test — not ported (requires gi, venv, shell).
        }

        [Fact]
        public void test_3848()
        {
            if (SkipSlowTests("test_3848"))
                return;

            string path = Resource("test_3848.pdf");
            if (!HasFile(path)) return;

            using var document = new Document(path);
            for (int i = 0; i < document.PageCount; i++)
            {

                var page = document.LoadPage(i);
                Console.WriteLine($"page={page}.");
                foreach (var annot in page.GetDrawingsDict())
                {
                    if (!string.IsNullOrEmpty(page.GetTextbox((Rect)annot["rect"])))
                    {
                        var rect = (Rect)annot["rect"];
                        var pixmap = page.GetPixmap(clip: rect.IRect);
                        if (pixmap.Width > 0 && pixmap.Height > 0)
                            _ = pixmap.color_topusage();
                    }
                }
            }
        }

        [Fact]
        public void test_3994()
        {
            string path = Resource("test_3994.pdf");
            if (!HasFile(path)) return;

            Tools.ResetMupdfWarnings();

            using var document = new Document(path);

            var page = document[0];
            var dict = (Dictionary<string, object>)page.GetText("dict");
            var blocks = (List<Dictionary<string, object>>)dict["blocks"];
            var txt_blocks = blocks.Where(blk => Convert.ToInt32(blk["type"]) == 0).ToList();
            foreach (var blk in txt_blocks)
            {
                var bbox = BboxFromBlock(blk);
                var clip = new Rect((int)bbox.X0, (int)bbox.Y0, (int)bbox.X1, (int)bbox.Y1);
                var pix = page.GetPixmap(clipRect: clip, cs: Colorspace.Rgb, alpha: false);
                var (percent, color) = pix.ColorTopUsage();
                Console.WriteLine($"percent={percent} color=({string.Join(", ", color)})");
            }
            string wt = Tools.MupdfWarnings();
            Assert.Equal("premature end of data in flate filter\n... repeated 2 times...", wt);
        }

        [Fact]
        public void test_3448()
        {
            string path = Resource("test_3448.pdf");
            if (!HasFile(path)) return;
            string path_expected = Resource("test_3448.pdf-expected.png");
            if (!HasFile(path_expected)) return;

            Pixmap pixmap;
            using (var document = new Document(path))
            {

                var page = document[0];
                pixmap = page.GetPixmap(alpha: false, dpi: 150);
                string path_out = Out("test_3448.png");
                pixmap.Save(path_out);
                Console.WriteLine($"Have written to: {path_out}");
            }
            var pixmap_expected = new Pixmap(path_expected);
            float rms = _Compare.PixmapsRms(pixmap, pixmap_expected);
            using var diff = _Compare.PixmapsDiff(pixmap_expected, pixmap);
            string path_diff = Out("test_3448-diff.png");
            diff.Save(path_diff);
            Console.WriteLine($"rms={rms}");
            Assert.Equal(0, rms);
        }

        [Fact]
        public void test_3854()
        {
            string path = Resource("test_3854.pdf");
            if (!HasFile(path)) return;
            string path_expected_png = Resource("test_3854_expected.png");
            if (!HasFile(path_expected_png)) return;

            Pixmap pixmap;
            using (var document = new Document(path))
            {

                var page = document[0];
                pixmap = page.GetPixmap();
            }
            pixmap.Save(Out("test_3854.png"));


            var pixmap_expected = new Pixmap(path_expected_png);
            using var pixmap_diff = _Compare.PixmapsDiff(pixmap_expected, pixmap);
            string path_diff = Out("test_3854_diff.png");
            pixmap_diff.Save(path_diff);
            float rms = _Compare.PixmapsRms(pixmap, pixmap_expected);
            Console.WriteLine($"rms={rms}.");
            Assert.Equal(0, rms);
        }

        [Fact]
        public void test_4155()
        {
            string path = Resource("test_3854.pdf");
            if (!HasFile(path)) return;

            Page page;
            Pixmap pixmap;
            using (var document = new Document(path))
            {

                page = document[0];
                pixmap = page.GetPixmap();
                _ = pixmap.samples_mv;
                byte[] mvb1 = pixmap.Samples.ToArray();
            }
            page = null!;
            pixmap.Dispose();
            try
            {
                byte[] mvb2 = pixmap.Samples;
                Assert.Fail("Did not receive expected exception when using defunct memoryview.");
            }
            // except ValueError as e:
            catch (ObjectDisposedException e)
            {
                Console.WriteLine($"Received exception: {e}");
                Assert.Contains("disposed", e.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void test_4336()
        {
            // Classic fitz/pickle subprocess harness — not ported.
            if (!HasFile(imgfile)) return;
            var pixmap = new Pixmap(imgfile);
            var sw = Stopwatch.StartNew();
            int cc = 0;
            for (int i = 0; i < 10; i++)
                cc = (int)pixmap.color_count();
            sw.Stop();
            Console.WriteLine($"test_4336(): t={sw.Elapsed.TotalSeconds}");
        }

        [Fact]
        public void test_4423()
        {
            string path = Resource("test_4423.pdf");
            if (!HasFile(path)) return;

            string path2 = Out("test_4423.pdf");
            Exception? ee = null;
            string wt = "";
            using (var document = new Document(path))
            {
                try
                {
                    document.Save(
                        path2,
                        garbage: 4,
                        expand: 1,
                        deflate: 1,
                        pretty: 1,
                        noNewId: 1);
                }
                // except Exception as e:
                catch (Exception e)
                {
                    Console.WriteLine($"Exception: {e}");
                    ee = e;
                }
                wt = Tools.MupdfWarnings();
            }
            Assert.Null(ee);
            Assert.Equal(
                "",
                wt);
        }

        [Fact]
        public void test_4445()
        {
            Console.WriteLine();
            // Test case is large so we download it instead of having it in MuPDF
            // (unless `-d` is specified).
            string path = Path.GetFullPath(Resource("test_4445.pdf"));
            if (!HasFile(path)) return;
            using (var document = new Document(path))
            {

                var page = document[0];
                var pixmap = page.GetPixmap();
                Console.WriteLine($"pixmap.width={pixmap.Width}");
                Console.WriteLine($"pixmap.height={pixmap.Height}");
                Assert.Equal((792, 612), (pixmap.Width, pixmap.Height));
                if (1 != 0)
                {

                    string path_pixmap = Out("test_4445.png");
                    pixmap.Save(path_pixmap);
                    Console.WriteLine($"Have created path_pixmap={path_pixmap}");
                }
            }
            string wt = Tools.MupdfWarnings();
            Console.WriteLine($"wt={wt}");
            Assert.Equal(
                "broken xref subsection, proceeding anyway.\nTrailer Size is off-by-one. Ignoring.",
                wt);
        }

        [Fact]
        public void test_3806()
        {
            string path = Resource("test_3806.pdf");
            if (!HasFile(path)) return;
            string path_png_expected = Resource("test_3806-expected.png");
            if (!HasFile(path_png_expected)) return;

            Console.WriteLine();
            Console.WriteLine($"mupdf_version={Tools.MupdfVersion()}");

            string path_png = Out("test_3806.png");

            using (var document = new Document(path))
            {
                var pixmap = document[0].GetPixmap();
                pixmap.Save(path_png);
                float rms = _Compare.PixmapsRms(path_png_expected, pixmap);
                Console.WriteLine($"rms={rms}");
                var mupdf_version_tuple = _Version.mupdf_version_tuple();
                if (mupdf_version_tuple.CompareTo((1, 26, 6)) >= 0)
                    Assert.True(rms < 0.1);
                else
                    Assert.True(rms > 50);
            }
        }

        [Fact]
        public void test_4388()
        {
            Console.WriteLine();
            string path_BOZ1 = Resource("test_4388_BOZ1.pdf");
            string path_BUL1 = Resource("test_4388_BUL1.pdf");
            if (!HasFile(path_BOZ1) || !HasFile(path_BUL1)) return;

            string path_correct = Out("test_4388.correct.png");

            string path_test = Out("test_4388.test.png");

            Pixmap pixmap_correct;
            using (var bul = new Document(path_BUL1))
            {
                pixmap_correct = bul.LoadPage(0).GetPixmap();
                pixmap_correct.Save(path_correct);
            }

            Tools.StoreShrink(100);

            using (var boz = new Document(path_BOZ1))
            {
                boz.LoadPage(0).GetPixmap();
            }

            Pixmap pixmap_test;
            using (var bul = new Document(path_BUL1))
            {
                pixmap_test = bul.LoadPage(0).GetPixmap();
                pixmap_test.Save(path_test);
            }

            float rms = _Compare.PixmapsRms(pixmap_correct, pixmap_test);
            Console.WriteLine($"rms={rms}");
            var mupdf_version_tuple = _Version.mupdf_version_tuple();
            if (mupdf_version_tuple.CompareTo((1, 26, 6)) >= 0)
                Assert.Equal(0, rms);
            else
                Assert.True(rms >= 10);
        }

        [Fact]
        public void test_4699()
        {
            string path = Resource("test_4699.pdf");
            if (!HasFile(path)) return;
            string path_png_expected = Resource("test_4699.png");
            if (!HasFile(path_png_expected)) return;

            string path_png_actual = Out("test_4699.png");

            Pixmap pixmap;
            using (var document = new Document(path))
            {

                var page = document[0];
                pixmap = page.GetPixmap();
                pixmap.Save(path_png_actual);
            }
            Console.WriteLine($"Have saved to path_png_actual={path_png_actual}.");
            float rms = _Compare.PixmapsRms(path_png_expected, pixmap);
            Console.WriteLine($"test_4699(): rms={rms}");
            var mupdf_version_tuple = _Version.mupdf_version_tuple();
            if (mupdf_version_tuple.CompareTo((1, 26, 11)) >= 0)
                Assert.Equal(0, rms);
            else
            {
                string wt = Tools.MupdfWarnings();
                Assert.Contains("syntax error: cannot find ExtGState resource", wt);
                Assert.True(rms > 20);
            }
        }

        [Fact]
        public void test_5001()
        {
            string path = Resource("test_5001.pdf");
            if (!HasFile(path)) return;
            string pathExpected = Resource("test_5001_expected.png");
            if (!HasFile(pathExpected)) return;
            string pathOut = Out("test_5001_out.png");

            Pixmap pixmap;
            using (var document = new Document(path))
            {
                var page = document[0];
                float zoom = 0.3f;
                pixmap = page.GetPixmap(matrix: new Matrix(zoom, zoom));
            }
            pixmap.Save(pathOut);
            float rms = _Compare.PixmapsRms(pathExpected, pixmap);
            Console.WriteLine($"test_5001(): rms={rms}");
            if (_Version.mupdf_version_tuple_at_least(1, 28, 0))
                Assert.Equal(0, rms);
            else
            {
                Assert.NotEqual(0, rms);
                string wt = Tools.MupdfWarnings();
                Assert.False(string.IsNullOrEmpty(wt));
            }
        }

        /// <summary>Regression test: 4435.</summary>
        [Fact]
        public void test_4435()
        {
            if (SkipSlowTests("test_4435"))
                return;
            Console.WriteLine($"mupdf_version={Tools.MupdfVersion()}");
            string path = Resource("test_4435.pdf");
            if (!HasFile(path)) return;

            Tools.ResetMupdfWarnings();
            using var document = new Document(path);
            var page = document[2];
            Console.WriteLine("Calling page.GetPixmap().");
            Pixmap pixmap;
            if (_Version.mupdf_version_tuple_at_least(1, 27, 0)
                && OperatingSystem.IsWindows() && IntPtr.Size == 4)
            {
                try
                {
                    pixmap = page.GetPixmap(alpha: false, dpi: 120);
                }
                catch (Exception e) when (e.Message.Contains("malloc") && e.Message.Contains("failed"))
                {
                    Console.WriteLine($"Received exception: {e.GetType()}={e}");
                    Assert.Matches(@"code=2: malloc \([0-9]+ bytes\) failed", e.Message);
                    return;
                }
                Assert.Fail("Expected alloc failure on 32-bit Windows");
            }
            else
            {
                pixmap = page.GetPixmap(alpha: false, dpi: 120);
            }
            Console.WriteLine("Called page.GetPixmap().");
            _ = pixmap;

            pixmap.Save(Out("test_4435.png"));
            string wt = Tools.MupdfWarnings();
            if (!_Version.mupdf_version_tuple_at_least(1, 27, 0))
            {
                Assert.Equal(
                    "bogus font ascent/descent values (0 / 0)\n... repeated 9 times...",
                    wt);
            }
            else if (_Version.mupdf_version_tuple_at_least(1, 28, 0))
            {
                string wtExpected = string.Concat(
                    Enumerable.Repeat("limit error: Overly large image\ncannot render glyph\n", 42)).TrimEnd();
                Assert.Equal(wtExpected, wt);
            }
        }

        /// <summary>Regression test: 5001b.</summary>
        [Fact]
        public void test_5001b()
        {
            if (OperatingSystem.IsWindows() && IntPtr.Size == 4)
                return;

            // from PIL import ImageCms
            string iccPath = Resource("test_5001b_srgb.icc");
            if (!HasFile(iccPath)) return;
            byte[] icc = File.ReadAllBytes(iccPath);

            byte[] bitmap = Enumerable.Repeat((byte)0xff, 8).ToArray();
            byte[] rgb = new byte[] { 0x00, 0x80, 0xff, 0x00, 0x80, 0xff, 0x00, 0x80, 0xff, 0x00, 0x80, 0xff };
            byte[] draw = Encoding.ASCII.GetBytes(
                "q 10 0 0 10 0 0 cm /Im1 Do Q q 80 0 0 30 10 10 cm /Im0 Do Q");
            var objs = new Dictionary<int, (string head, byte[]? data)>
            {
                [1] = ("<< /Type /Catalog /Pages 2 0 R >>", null),
                [2] = ("<< /Type /Pages /Kids [3 0 R] /Count 1 >>", null),
                [3] = (
                    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 50] "
                    + "/Resources << /XObject << /Im0 5 0 R /Im1 8 0 R >> >> /Contents 4 0 R >>",
                    null),
                [4] = ($"<< /Length {draw.Length} >>", draw),
                [5] = (
                    "<< /Type /XObject /Subtype /Image /Width 8 /Height 8 /BitsPerComponent 1 "
                    + $"/ColorSpace [/Indexed 6 0 R 1 <000000FFFFFF>] /Length {bitmap.Length} >>",
                    bitmap),
                [6] = ("[/ICCBased 7 0 R]", null),
                [7] = ($"<< /N 3 /Length {icc.Length} >>", icc),
                [8] = (
                    "<< /Type /XObject /Subtype /Image /Width 2 /Height 2 /BitsPerComponent 8 "
                    + $"/ColorSpace 6 0 R /Length {rgb.Length} >>",
                    rgb),
            };

            var output = new MemoryStream();
            var offsets = new Dictionary<int, int>();
            output.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
            foreach (int num in objs.Keys.OrderBy(n => n))
            {
                var (head, data) = objs[num];
                offsets[num] = (int)output.Length;
                output.Write(Encoding.ASCII.GetBytes($"{num} 0 obj\n{head}\n"));
                if (data != null)
                {
                    output.Write(Encoding.ASCII.GetBytes("stream\n"));
                    output.Write(data);
                    output.Write(Encoding.ASCII.GetBytes("\nendstream\n"));
                }
                output.Write(Encoding.ASCII.GetBytes("endobj\n"));
            }
            int xrefPos = (int)output.Length;
            output.Write(Encoding.ASCII.GetBytes($"xref\n0 {objs.Count + 1}\n0000000000 65535 f \n"));
            foreach (int num in objs.Keys.OrderBy(n => n))
                output.Write(Encoding.ASCII.GetBytes($"{offsets[num]:D10} 00000 n \n"));
            output.Write(Encoding.ASCII.GetBytes(
                $"trailer\n<< /Size {objs.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF"));

            Tools.ResetMupdfWarnings();
            using var doc = new Document(output.ToArray(), fileType: "pdf");
            var pix = doc[0].GetPixmap(dpi: 150);
            byte[] p = pix.GetPixel((int)(50 * 150 / 72.0), (int)(25 * 150 / 72.0));
            Console.WriteLine($"center of the indexed image: ({string.Join(", ", p)})");
            pix.Save(Out("test_5001b_out.png"));
            if (_Version.mupdf_version_tuple_at_least(1, 28, 0))
                Assert.Equal(new byte[] { 255, 255, 255 }, p);
            else
            {
                string wt = Tools.MupdfWarnings();
                Assert.False(string.IsNullOrEmpty(wt));
            }
        }

        /// <summary>Regression test: natural.</summary>
        [Fact]
        public void test_natural()
        {
            if (!_Version.mupdf_version_tuple_at_least(1, 28, 0))
            {
                Console.WriteLine("test_natural(): Not running because segv fixed on mupdf master (1.28).");
                return;
            }
            string path = Resource("test_natural.pdf");
            if (!HasFile(path)) return;

            using var document = new Document(path);
            var page = document[0];
            using var ctm = mupdf.mupdf.fz_make_matrix(200f / 72f, 0, 0, 200f / 72f, 0, 0);
            using var rect = mupdf.mupdf.ll_fz_make_rect(
                (float)page.Rect.X0, (float)page.Rect.Y0, (float)page.Rect.X1, (float)page.Rect.Y1);
            using var rects = new mupdf.vector_fz_rect(new[] { rect });
            using var fzPm = page.NativePage.fz_new_pixmap_from_page_culling_text2(
                ctm,
                mupdf.mupdf.fz_device_rgb(),
                0,
                rects);
            using var pix = new Pixmap(fzPm);
            Console.WriteLine($"pix={pix}");
            pix.Save(Out("test_natural.png"));
        }

        /// <summary><c>text_pixmap_transpositions</c>: FlipRotate composed back to the original samples.</summary>
        [Fact]
        public void text_pixmap_transpositions()
        {
            string path = Resource("test_natural.pdf");
            if (!HasFile(path)) return;

            using var doc = new Document(path);
            using Pixmap pix = doc[0].GetPixmap();
            byte[] samples0 = pix.Samples;
            var modes = new Dictionary<int, int>
            {
                [Pixmap.Rotate90] = 3,
                [Pixmap.Rotate180] = 1,
                [Pixmap.Rotate270] = 3,
                [Pixmap.FlipLeftRight] = 1,
                [Pixmap.FlipTopBottom] = 1,
            };
            foreach (var kv in modes)
            {
                Pixmap neu = pix.FlipRotate(kv.Key);
                for (int i = 0; i < kv.Value; i++)
                {
                    Pixmap next = neu.FlipRotate(kv.Key);
                    if (!ReferenceEquals(next, neu))
                        neu.Dispose();
                    neu = next;
                }
                Assert.Equal(samples0, neu.Samples);
                neu.Save(Out($"text_pixmap_transpositions_{kv.Key}.png"));
                neu.Dispose();
            }
        }

        [Fact]
        public async Task test_5082()
        {
            Console.WriteLine();
            string path = Out("test_5082.pdf");
            if (!File.Exists(path) || new System.IO.FileInfo(path).Length == 0)
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(60);
                http.DefaultRequestHeaders.TryAddWithoutValidation("user-agent", "pymupdf-test"); // Avoids html 403 failure.
                byte[] data = await http.GetByteArrayAsync(
                    "https://www.city.toyota.aichi.jp/_res/projects/default_project/_page_/001/057/466/01.pdf");
                File.WriteAllBytes(path, data);
            }
            Console.WriteLine($"test_5082(): mupdf_version={Tools.MupdfVersion()}.");
            Console.WriteLine("test_5082(): calling page.get_pixmap().");
            var task = Task.Run(() =>
            {
                try
                {
                    using var document = new Document(path);
                    using var pix = document[0].GetPixmap();
                    _ = pix.Width;
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            });
            var finished = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
            bool timeout = finished != task;
            if (!timeout)
            {
                Exception? caught = await task;
                if (caught != null)
                    throw caught;
            }
            Console.WriteLine($"test_5082(): timeout={timeout}");
            if (!_Version.mupdf_version_tuple_at_least(1, 28, 5))
                Assert.True(timeout, $"Expected timeout from mupdf_version={Tools.MupdfVersion()}.");
            else
                Assert.False(timeout, $"Unexpected timeout from mupdf_version={Tools.MupdfVersion()}.");
        }

        [Fact]
        public async Task test_5125()
        {
            string path = Doc("test_5125.pdf");
            Console.WriteLine();
            Console.WriteLine($"Running get_pixmap dpi=200 on {path}");
            var task = Task.Run(() =>
            {
                try
                {
                    using var document = new Document(path);
                    using var pix = document[0].GetPixmap(dpi: 200);
                    _ = pix.Width;
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            });
            var finished = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30)));
            bool timeout = finished != task;
            if (!timeout)
            {
                Exception? caught = await task;
                if (caught != null)
                    throw caught;
            }
            Console.WriteLine($"e={(timeout ? "TimeoutExpired" : "null")}");
            if (_Version.mupdf_version_tuple_at_least(1, 28, 5))
                Assert.False(timeout);
            else
                Assert.True(timeout);
        }
    }
}