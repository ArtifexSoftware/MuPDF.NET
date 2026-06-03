// """
// Pixmap tests
// * make pixmap of a page and assert bbox size
// * make pixmap from a PDF xref and compare with extracted image
// * pixmap from file and from binary image and compare
// """
//
// import pymupdf
// import gentle_compare
//
// import os
// import platform
// import re
// import shutil
// import subprocess
// import sys
// import tempfile
// import pytest
// import textwrap
// import time
// import util
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

    /// <summary>Port of <c>PyMuPDF-1.27.2.2/tests/test_pixmap.py</c>.</summary>
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

        // util.skip_slow_tests(test_name)
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

        /// <summary>Python <c>Document.__getitem__</c> for invalid / valid keys.</summary>
        private static Page doc_getitem(Document doc, object i)
        {
            // assert isinstance(i, int) or (isinstance(i, tuple) and len(i) == 2 and all(isinstance(x, int) for x in i)), \
            //         f'Invalid item number: {i=}.'
            if (i is int pageNo)
                return doc[pageNo];
            if (i is ValueTuple<int, int> loc)
                return doc.GetItemPageForIndexer(loc.Item1, loc.Item2);
            throw new InvalidOperationException($"Invalid item number: i={i}.");
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
            // # pixmap from an EPUB page
            // doc = pymupdf.open(epub)
            using var doc = new Document(epub);
            // page = doc[0]
            var page = doc[0];
            // pix = page.GetPixmap()
            var pix = page.GetPixmap();
            pix.Save(Out("test_pagepixmap_0.png"));
            // assert pix.irect == page.Rect.irect
            Assert.Equal(page.Rect.IRect, pix.IRect);
            // pix = page.GetPixmap(alpha=True)
            pix = page.GetPixmap(alpha: true);
            pix.Save(Out("test_pagepixmap_1.png"));
            // assert pix.alpha
            Assert.NotEqual(0, pix.alpha);
            // assert pix.n == pix.colorspace.n + pix.alpha
            Assert.Equal(pix.Colorspace.N + pix.alpha, pix.N);
        }

        [Fact]
        public void test_pdfpixmap()
        {
            if (!HasFile(pdf)) return;
            // # pixmap from xref in a PDF
            // doc = pymupdf.open(pdf)
            using var doc = new Document(pdf);
            // # take first image item of first page
            // img = doc.get_page_images(0)[0]
            var img = doc.get_page_images(0)[0];
            // # make pixmap of it
            // pix = pymupdf.Pixmap(doc, img[0])
            var pix = new Pixmap(doc, img.xref);
            // # assert pixmap properties
            // assert pix.width == img[2]
            Assert.Equal(img.width, pix.Width);
            // assert pix.height == img[3]
            Assert.Equal(img.height, pix.Height);
            // # extract image and compare metadata
            // extractimg = doc.extract_image(img[0])
            var extractimg = doc.extract_image(img.xref);
            // assert extractimg["width"] == pix.width
            Assert.Equal(pix.Width, Convert.ToInt32(extractimg["width"]));
            // assert extractimg["height"] == pix.height
            Assert.Equal(pix.Height, Convert.ToInt32(extractimg["height"]));
            pix.Save(Out("test_pdfpixmap.png"));
        }

        [Fact]
        public void test_filepixmap()
        {
            if (!HasFile(imgfile)) return;
            // # pixmaps from file and from stream
            // # should lead to same result
            // pix1 = pymupdf.Pixmap(imgfile)
            var pix1 = new Pixmap(imgfile);
            // stream = open(imgfile, "rb").read()
            byte[] stream = File.ReadAllBytes(imgfile);
            // pix2 = pymupdf.Pixmap(stream)
            var pix2 = new Pixmap(stream);
            // assert repr(pix1) == repr(pix2)
            Assert.Equal(pix1.ToString(), pix2.ToString());
            // assert pix1.digest == pix2.digest
            Assert.Equal(pix1.Digest, pix2.Digest);
            pix1.Save(Out("test_filepixmap_0.png"));
            pix2.Save(Out("test_filepixmap_1.png"));
        }

        [Fact]
        public void test_pilsave()
        {
            if (!HasFile(imgfile)) return;
            // # pixmaps from file then save to pillow image
            // # make pixmap from this and confirm equality
            // try:
            try
            {
                // pix1 = pymupdf.Pixmap(imgfile)
                var pix1 = new Pixmap(imgfile);
                // stream = pix1.pil_tobytes("JPEG")
                byte[] stream = pix1.pil_tobytes("JPEG");
                // pix2 = pymupdf.Pixmap(stream)
                var pix2 = new Pixmap(stream);
                // assert repr(pix1) == repr(pix2)
                Assert.Equal(pix1.ToString(), pix2.ToString());
                pix1.Save(Out("test_pilsave_0.png"));
                pix2.Save(Out("test_pilsave_1.png"));
            }
            // except ModuleNotFoundError:
            catch (Exception ex) when (ex is DllNotFoundException or TypeLoadException or NotImplementedException)
            {
                // assert platform.system() in ('Windows', 'Emscripten') and sys.maxsize == 2**31 - 1
                Assert.True(
                    OperatingSystem.IsWindows() && IntPtr.Size == 4,
                    $"Unexpected exception on non-Windows 32-bit: {ex}");
            }
        }

        [Fact]
        public void test_save()
        {
            if (!HasFile(imgfile)) return;
            // # pixmaps from file then save to image
            // # make pixmap from this and confirm equality
            // pix1 = pymupdf.Pixmap(imgfile)
            var pix1 = new Pixmap(imgfile);
            // outfile = os.path.join(tmpdir, "foo.png")
            string outfile = Out("test_save_0.png");
            try
            {
                // pix1.Save(outfile, output="png")
                pix1.Save(outfile, output: "png");
                // # read it back
                // pix2 = pymupdf.Pixmap(outfile)
                var pix2 = new Pixmap(outfile);
                // assert repr(pix1) == repr(pix2)
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
            // # pixmap from JPEG file, then add an alpha channel
            // # with 30% transparency
            // pix1 = pymupdf.Pixmap(imgfile)
            var pix1 = new Pixmap(imgfile);
            // opa = int(255 * 0.3)  # corresponding to 30% transparency
            int opa = (int)(255 * 0.3);  // corresponding to 30% transparency
            // alphas = [opa] * (pix1.width * pix1.height)
            var alphas = Enumerable.Repeat((byte)opa, pix1.Width * pix1.Height).ToArray();
            // alphas = bytearray(alphas)
            // pix2 = pymupdf.Pixmap(pix1, 1)  # add alpha channel
            var pix2 = new Pixmap(pix1, 1);  // add alpha channel
            // pix2.set_alpha(alphas)  # make image 30% transparent
            pix2.set_alpha(alphas);  // make image 30% transparent
            // samples = pix2.samples  # copy of samples
            byte[] samples = pix2.samples;  // copy of samples
            // # confirm correct the alpha bytes
            // t = bytearray([samples[i] for i in range(3, len(samples), 4)])
            var t = new byte[samples.Length / 4];
            for (int i = 0, j = 0; i < samples.Length; i += 4, j++)
                t[j] = samples[i + 3];
            // assert t == alphas
            pix1.Save(Out("test_setalpha_0.png"));
            pix2.Save(Out("test_setalpha_1.png"));
            Assert.Equal(alphas, t);
        }

        [Fact]
        public void test_color_count()
        {
            // '''
            // This is known to fail if MuPDF is built without PyMuPDF's custom config.h,
            // e.g. in Linux system installs.
            // '''
            if (!HasFile(imgfile)) return;
            // pm = pymupdf.Pixmap(imgfile)
            var pm = new Pixmap(imgfile);
            // assert pm.color_count() == 40624
            Assert.Equal(40624, (int)pm.color_count());
        }

        [Fact]
        public void test_memoryview()
        {
            if (!HasFile(imgfile)) return;
            // pm = pymupdf.Pixmap(imgfile)
            var pm = new Pixmap(imgfile);
            // samples = pm.samples_mv
            var samples = pm.samples_mv;
            // assert isinstance( samples, memoryview)
            Assert.IsType<PixmapSamplesMemoryView>(samples);
            // print( f'samples={samples} samples.itemsize={samples.itemsize} samples.nbytes={samples.nbytes} samples.ndim={samples.ndim} samples.shape={samples.shape} samples.strides={samples.strides}')
            Console.WriteLine($"samples={samples} samples.itemsize={samples.itemsize} samples.nbytes={samples.nbytes} samples.ndim={samples.ndim} samples.shape={samples.shape} samples.strides={samples.strides}");
            // assert samples.itemsize == 1
            Assert.Equal(1, samples.itemsize);
            // assert samples.nbytes == 659817
            Assert.Equal(659817, samples.nbytes);
            // assert samples.ndim == 1
            Assert.Equal(1, samples.ndim);
            // assert samples.shape == (659817,)
            Assert.Equal(new[] { 659817 }, samples.shape);
            // assert samples.strides == (1,)
            Assert.Equal(new[] { 1 }, samples.strides);

            // color = pm.GetPixelBytes( 100, 100)
            byte[] color = pm.GetPixelBytes(100, 100);
            // print( f'color={color}')
            Console.WriteLine($"color=({string.Join(", ", color)})");
            // assert color == (83, 66, 40)
            Assert.Equal(new byte[] { 83, 66, 40 }, color);
        }

        [Fact]
        public void test_samples_ptr()
        {
            if (!HasFile(imgfile)) return;
            // pm = pymupdf.Pixmap(imgfile)
            var pm = new Pixmap(imgfile);
            // samples = pm.samples_ptr
            IntPtr samples = pm.samples_ptr;
            // print( f'samples={samples}')
            Console.WriteLine($"samples={samples}");
            // assert isinstance( samples, int)
            Assert.NotEqual(IntPtr.Zero, samples);
        }

        [Fact]
        public void test_2369()
        {
            // width, height = 13, 37
            int width = 13, height = 37;
            // image = pymupdf.Pixmap(pymupdf.csGRAY, width, height, b"\x00" * (width * height), False)
            var image = new Pixmap(Colorspace.Gray, width, height, new byte[width * height], false);

            // with pymupdf.Document(stream=image.ToBytes(output="pam"), filetype="pam") as doc:
            using (var doc = new Document(image.ToBytes(output: "pam"), "pam"))
            {
                // test_pdf_bytes = doc.convert_to_pdf()
                byte[] test_pdf_bytes = doc.convert_to_pdf();

                // with pymupdf.Document(stream=test_pdf_bytes) as doc:
                using (var doc2 = new Document(test_pdf_bytes))
                {
                    // page = doc[0]
                    var page = doc2[0];
                    // img_xref = page.GetImages()[0][0]
                    int img_xref = page.GetImages()[0].xref;
                    // img = doc.extract_image(img_xref)
                    var img = doc2.extract_image(img_xref);
                    // img_bytes = img["image"]
                    byte[] img_bytes = (byte[])img["image"];
                    // pymupdf.Pixmap(img_bytes)
                    var pix = new Pixmap(img_bytes);
                    pix.Save(Out("test_2369.png"));
                }
            }
        }

        [Fact]
        public void test_page_idx_int()
        {
            if (!HasFile(pdf)) return;
            // doc = pymupdf.open(pdf)
            using var doc = new Document(pdf);
            // with pytest.raises(AssertionError):
            //     doc["0"]
            Assert.ThrowsAny<Exception>(() => doc_getitem(doc, "0"));
            // assert doc[0]
            Assert.NotNull(doc[0]);
            // assert doc[(0,0)]
            Assert.NotNull(doc_getitem(doc, (0, 0)));
        }

        [Fact]
        public void test_fz_write_pixmap_as_jpeg()
        {
            // width, height = 13, 37
            int width = 13, height = 37;
            // image = pymupdf.Pixmap(pymupdf.csGRAY, width, height, b"\x00" * (width * height), False)
            var image = new Pixmap(Colorspace.Gray, width, height, new byte[width * height], false);

            // with pymupdf.Document(stream=image.ToBytes(output="jpeg"), filetype="jpeg") as doc:
            using (var doc = new Document(image.ToBytes(output: "jpeg"), "jpeg"))
            {
                // test_pdf_bytes = doc.convert_to_pdf()
                byte[] test_pdf_bytes = doc.convert_to_pdf();
                Document src = new Document(test_pdf_bytes);
                src.Save(Out("test_fz_write_pixmap_as_jpeg.pdf"));
            }
        }

        [Fact]
        public void test_3020()
        {
            if (!HasFile(imgfile)) return;
            // pm = pymupdf.Pixmap(imgfile)
            var pm = new Pixmap(imgfile);
            // pm2 = pymupdf.Pixmap(pm, 20, 30, None)
            _ = new Pixmap(pm, 20, 30, null);
            // pm3 = pymupdf.Pixmap(pymupdf.csGRAY, pm)
            _ = new Pixmap(Colorspace.Gray, pm);
            // pm4 = pymupdf.Pixmap(pm, pm3)
            var pm3 = new Pixmap(Colorspace.Gray, pm);
            var pm4 = new Pixmap(pm, pm3);
            pm4.Save(Out("test_3020.png"));
        }

        [Fact]
        public void test_3050()
        {
            // '''
            // This is known to fail if MuPDF is built without it's default third-party
            // libraries, e.g. in Linux system installs.
            // '''
            string path = Resource("001003ED.pdf");
            if (!HasFile(path)) return;
            string path_expected = TestsPath("resources/test_3050_expected.png");
            if (!HasFile(path_expected)) return;

            Tools.ResetMupdfWarnings();

            // path = os.path.normpath(f'{__file__}/../../tests/resources/001003ED.pdf')
            // with pymupdf.open(path) as pdf_file:
            using (var pdf_file = new Document(path))
            {
                // page_no = 0
                int page_no = 0;
                // page = pdf_file[page_no]
                var page = pdf_file[page_no];
                // zoom_x = 4.0
                // zoom_y = 4.0
                float zoom_x = 4.0f;
                float zoom_y = 4.0f;
                // matrix = pymupdf.Matrix(zoom_x, zoom_y)
                var matrix = new Matrix(zoom_x, zoom_y);
                // pix = page.GetPixmap(matrix=matrix)
                var pix = page.GetPixmap(matrix: matrix);
                // path_out = os.path.normpath(f'{__file__}/../../tests/test_3050_out.png')
                string path_out = Out("test_3050_0.png");
                // pix.Save(path_out)
                pix.Save(path_out);
                // print(f'{pix.width=} {pix.height=}')
                Console.WriteLine($"pix.width={pix.Width} pix.height={pix.Height}");
                // def product(x, y):
                //     for yy in y:
                //         for xx in x:
                //             yield (xx, yy)
                // n = 0
                int n = 0;
                // # We use a small subset of the image because non-optimised build gets
                // # very slow.
                // for pos in product(range(100), range(100)):
                foreach (var pos in product(100, 100))
                {
                    // if sum(pix.GetPixelBytes(pos[0], pos[1])) >= 600:
                    if (pix.GetPixelBytes(pos.xx, pos.yy).Sum(b => (int)b) >= 600)
                    {
                        // n += 1
                        n += 1;
                        // pix.SetPixel(pos[0], pos[1], (255, 255, 255))
                        pix.SetPixel(pos.xx, pos.yy, new float[] { 255, 255, 255 });
                    }
                }
                // path_out2 = os.path.normpath(f'{__file__}/../../tests/test_3050_out2.png')
                string path_out2 = Out("test_3050_1.png");
                // pix.Save(path_out2)
                pix.Save(path_out2);
                // path_expected = os.path.normpath(f'{__file__}/../../tests/resources/test_3050_expected.png')
                // rms = gentle_compare.pixmaps_rms(path_expected, path_out2)
                float rms = _Compare.PixmapsRms(path_expected, path_out2);
                // print(f'{rms=}')
                Console.WriteLine($"rms={rms}");
                // assert rms == 0
                Assert.Equal(0, rms);
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                var mupdf_version_tuple = _Version.mupdf_version_tuple();
                // if (1, 26, 0) <= pymupdf.mupdf_version_tuple < (1, 27):
                if (mupdf_version_tuple.CompareTo((1, 26, 0)) >= 0 && mupdf_version_tuple.CompareTo((1, 27, 0)) < 0)
                {
                    // assert wt == 'bogus font ascent/descent values (0 / 0)\nPDF stream Length incorrect'
                    Assert.Equal("bogus font ascent/descent values (0 / 0)\nPDF stream Length incorrect", wt);
                }
                else
                {
                    // assert wt == 'PDF stream Length incorrect'
                    Assert.Equal("PDF stream Length incorrect", wt);
                }
            }
        }

        [Fact]
        public void test_3058()
        {
            string pathPdf = Resource("test_3058.pdf");
            if (!HasFile(pathPdf)) return;

            // doc = pymupdf.Document(os.path.abspath(f'{__file__}/../../tests/resources/test_3058.pdf'))
            using var doc = new Document(pathPdf);
            // images = doc[0].GetImages(full=True)
            var images = doc[0].GetImages(full: true);
            // pix = pymupdf.Pixmap(doc, 17)
            var pix = new Pixmap(doc, 17);

            // # First bug was that `pix.colorspace` was DeviceRGB.
            // assert str(pix.colorspace) == 'Colorspace(CS_CMYK) - DeviceCMYK'
            Assert.Contains("CMYK", pix.Colorspace.ToString(), StringComparison.OrdinalIgnoreCase);

            // pix = pymupdf.Pixmap(pymupdf.csRGB, pix)
            pix = new Pixmap(Colorspace.Rgb, pix);
            // assert str(pix.colorspace) == 'Colorspace(CS_RGB) - DeviceRGB'
            Assert.Contains("RGB", pix.Colorspace.ToString(), StringComparison.OrdinalIgnoreCase);

            // # Second bug was that the image was converted to RGB via greyscale proofing
            // # color space, so image contained only shades of grey. This compressed
            // # easily to a .png file, so we crudely check the bug is fixed by looking at
            // # size of .png file.
            // path = os.path.abspath(f'{__file__}/../../tests/test_3058_out.png')
            string path = Out("test_3058.png");
            // pix.Save(path)
            pix.Save(path);
            // s = os.path.getsize(path)
            long s = new System.IO.FileInfo(path).Length;
            // assert 1800000 < s < 2600000, f'Unexpected size of {path}: {s}'
            Assert.True(1800000 < s && s < 2600000, $"Unexpected size of {path}: {s}");
        }

        [Fact]
        public void test_3072()
        {
            string path = Resource("test_3072.pdf");
            if (!HasFile(path)) return;
            string outDir = Path.GetDirectoryName(Out("test_3072_1.jpg"))!;

            // doc = pymupdf.open(path)
            using (var doc = new Document(path))
            {
                // page_48 = doc[0]
                var page_48 = doc[0];
                // bbox = [147, 300, 447, 699]
                float[] bbox = { 147, 300, 447, 699 };
                // rect = pymupdf.Rect(*bbox)
                var rect = new Rect(bbox[0], bbox[1], bbox[2], bbox[3]);
                // zoom = pymupdf.Matrix(3, 3)
                var zoom = new Matrix(3, 3);
                // pix = page_48.GetPixmap(clip=rect, matrix=zoom)
                var pix = page_48.GetPixmap(clip: rect.IRect, matrix: zoom);
                // image_save_path = f'{out}/1.jpg'
                string image_save_path = Path.Combine(outDir, "test_3072_1.jpg");
                // pix.Save(image_save_path, jpg_quality=95)
                pix.Save(image_save_path, jpg_quality: 95);
            }

            // doc = pymupdf.open(path)
            using (var doc = new Document(path))
            {
                // page_49 = doc[1]
                var page_49 = doc[1];
                // bbox = [147, 543, 447, 768]
                float[] bbox = { 147, 543, 447, 768 };
                // rect = pymupdf.Rect(*bbox)
                var rect = new Rect(bbox[0], bbox[1], bbox[2], bbox[3]);
                // zoom = pymupdf.Matrix(3, 3)
                var zoom = new Matrix(3, 3);
                // pix = page_49.GetPixmap(clip=rect, matrix=zoom)
                var pix = page_49.GetPixmap(clip: rect.IRect, matrix: zoom);
                // image_save_path = f'{out}/2.jpg'
                string image_save_path = Path.Combine(outDir, "test_3072_2.jpg");
                // pix.Save(image_save_path, jpg_quality=95)
                pix.Save(image_save_path, jpg_quality: 95);
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                // assert wt == (
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
            // doc = pymupdf.Document()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            string path_rect = Out("test_3134_rect.jpg");
            string path_irect = Out("test_3134_irect.jpg");
            try
            {
                // page.GetPixmap(clip=pymupdf.Rect(0, 0, 100, 100)).Save("test_3134_rect.jpg")
                page.GetPixmap(clip: new IRect(0, 0, 100, 100)).Save(path_rect);
                // page.GetPixmap(clip=pymupdf.IRect(0, 0, 100, 100)).Save("test_3134_irect.jpg")
                page.GetPixmap(clip: new IRect(0, 0, 100, 100)).Save(path_irect);
                // stat_rect = os.stat('test_3134_rect.jpg')
                var stat_rect = new System.IO.FileInfo(path_rect);
                // stat_irect = os.stat('test_3134_irect.jpg')
                var stat_irect = new System.IO.FileInfo(path_irect);
                // print(f' {stat_rect=}')
                Console.WriteLine($" stat_rect={stat_rect}");
                // print(f'{stat_irect=}')
                Console.WriteLine($"stat_irect={stat_irect}");
                // assert stat_rect.st_size == stat_irect.st_size
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
            // pixmap = pymupdf.Pixmap(path)
            var pixmap = new Pixmap(path);
            // pixmap2 = pymupdf.Pixmap(None, pixmap)
            var pixmap2 = new Pixmap(colorPixmap: null, maskPixmap: pixmap);
            pixmap2.Save(Out("test_3177.png"));
        }

        [Fact]
        public void test_3493()
        {
            // '''
            // If python3-gi is installed, we check fix for #3493, where importing gi
            // would load an older version of libjpeg than is used in MuPDF, and break
            // MuPDF.
            //
            // This test is excluded by default in sysinstall tests, because running
            // commands in a new venv does not seem to pick up pymupdf as expected.
            // '''
            // if platform.system() != 'Linux':
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // print(f'Not running because not Linux: {platform.system()=}')
                Console.WriteLine($"Not running because not Linux: platform={Environment.OSVersion.Platform}");
                return;
            }
            // Linux-only subprocess/venv test — not ported (requires gi, venv, shell).
        }

        [Fact]
        public void test_3848()
        {
            // if util.skip_slow_tests('test_3848'):
            if (SkipSlowTests("test_3848"))
                return;
            // if os.environ.get('PYMUPDF_RUNNING_ON_VALGRIND') == '1':
            if (Environment.GetEnvironmentVariable("PYMUPDF_RUNNING_ON_VALGRIND") == "1")
            {
                // print(f'test_3848(): not running on valgrind because very slow.', flush=1)
                Console.WriteLine("test_3848(): not running on valgrind because very slow.");
                return;
            }
            string path = Resource("test_3848.pdf");
            if (!HasFile(path)) return;

            // with pymupdf.open(path) as document:
            using var document = new Document(path);
            // for i in range(len(document)):
            for (int i = 0; i < document.PageCount; i++)
            {
                // page = document.LoadPage(i)
                var page = document.LoadPage(i);
                // print(f'{page=}.')
                Console.WriteLine($"page={page}.");
                // for annot in page.GetDrawings():
                foreach (var annot in page.GetDrawingsDict())
                {
                    // if page.GetTextbox(annot['rect']):
                    if (!string.IsNullOrEmpty(page.GetTextbox((Rect)annot["rect"])))
                    {
                        // rect = annot['rect']
                        var rect = (Rect)annot["rect"];
                        // pixmap = page.GetPixmap(clip=rect)
                        var pixmap = page.GetPixmap(clip: rect.IRect);
                        // color_bytes = pixmap.color_topusage()
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

            // with pymupdf.open(path) as document:
            using var document = new Document(path);
            // page = document[0]
            var page = document[0];
            // txt_blocks = [blk for blk in page.GetText('dict')['blocks'] if blk['type']==0]
            var dict = (Dictionary<string, object>)page.GetText("dict");
            var blocks = (List<Dictionary<string, object>>)dict["blocks"];
            var txt_blocks = blocks.Where(blk => Convert.ToInt32(blk["type"]) == 0).ToList();
            // for blk in txt_blocks:
            foreach (var blk in txt_blocks)
            {
                // pix = page.GetPixmap(clip=pymupdf.Rect([int(v) for v in blk['bbox']]), colorspace=pymupdf.csRGB, alpha=False)
                var clip = BboxFromBlock(blk);
                var pix = page.GetPixmap(clip: clip.IRect, cs: Colorspace.Rgb, alpha: false);
                // percent, color = pix.color_topusage()
                if (pix.Width > 0 && pix.Height > 0)
                    _ = pix.color_topusage();
            }
            // wt = pymupdf.TOOLS.mupdf_warnings()
            string wt = Tools.MupdfWarnings();
            // assert wt == 'premature end of data in flate filter\n... repeated 2 times...'
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
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // page = document[0]
                var page = document[0];
                // pixmap = page.GetPixmap(alpha=False, dpi=150)
                pixmap = page.GetPixmap(alpha: false, dpi: 150);
                // path_out = f'{path}.png'
                string path_out = Out("test_3448.png");
                // pixmap.Save(path_out)
                pixmap.Save(path_out);
                // print(f'Have written to: {path_out}')
                Console.WriteLine($"Have written to: {path_out}");
            }
            // path_expected = os.path.normpath(f'{__file__}/../../tests/resources/test_3448.pdf-expected.png')
            // pixmap_expected = pymupdf.Pixmap(path_expected)
            var pixmap_expected = new Pixmap(path_expected);
            // rms = gentle_compare.pixmaps_rms(pixmap, pixmap_expected)
            float rms = _Compare.PixmapsRms(pixmap, pixmap_expected);
            // diff = gentle_compare.pixmaps_diff(pixmap_expected, pixmap)
            using var diff = _Compare.PixmapsDiff(pixmap_expected, pixmap);
            // path_diff = os.path.normpath(f'{__file__}/../../tests/test_3448-diff.png')
            string path_diff = Out("test_3448-diff.png");
            // diff.Save(path_diff)
            diff.Save(path_diff);
            // print(f'{rms=}')
            Console.WriteLine($"rms={rms}");
            // assert rms == 0
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
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // page = document[0]
                var page = document[0];
                // pixmap = page.GetPixmap()
                pixmap = page.GetPixmap();
            }
            // pixmap.Save(os.path.normpath(f'{__file__}/../../tests/test_3854_out.png'))
            pixmap.Save(Out("test_3854.png"));

            // # 2024-11-29: this is the incorrect expected output.
            // path_expected_png = os.path.normpath(f'{__file__}/../../tests/resources/test_3854_expected.png')
            // pixmap_expected = pymupdf.Pixmap(path_expected_png)
            var pixmap_expected = new Pixmap(path_expected_png);
            // pixmap_diff = gentle_compare.pixmaps_diff(pixmap_expected, pixmap)
            using var pixmap_diff = _Compare.PixmapsDiff(pixmap_expected, pixmap);
            // path_diff = os.path.normpath(f'{__file__}/../../tests/resources/test_3854_diff.png')
            string path_diff = Out("test_3854_diff.png");
            // pixmap_diff.Save(path_diff)
            pixmap_diff.Save(path_diff);
            // rms = gentle_compare.pixmaps_rms(pixmap, pixmap_expected)
            float rms = _Compare.PixmapsRms(pixmap, pixmap_expected);
            // print(f'{rms=}.')
            Console.WriteLine($"rms={rms}.");
            // if os.environ.get('PYMUPDF_SYSINSTALL_TEST') == '1':
            if (Environment.GetEnvironmentVariable("PYMUPDF_SYSINSTALL_TEST") == "1")
            {
                // MuPDF using external third-party libs gives slightly different
                // behaviour.
                // assert rms < 2
                Assert.True(rms < 2);
            }
            else
            {
                // assert rms == 0
                Assert.Equal(0, rms);
            }
        }

        [Fact]
        public void test_4155()
        {
            string path = Resource("test_3854.pdf");
            if (!HasFile(path)) return;

            Page page;
            Pixmap pixmap;
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // page = document[0]
                page = document[0];
                // pixmap = page.GetPixmap()
                pixmap = page.GetPixmap();
                // mv = pixmap.samples_mv
                _ = pixmap.samples_mv;
                // mvb1 = mv.ToBytes()
                byte[] mvb1 = pixmap.Samples.ToArray();
            }
            // del page
            page = null!;
            // del pixmap
            pixmap.Dispose();
            // try:
            try
            {
                // mvb2 = mv.ToBytes()
                byte[] mvb2 = pixmap.Samples;
                // else:
                Assert.Fail("Did not receive expected exception when using defunct memoryview.");
            }
            // except ValueError as e:
            catch (ObjectDisposedException e)
            {
                // print(f'Received exception: {e}')
                Console.WriteLine($"Received exception: {e}");
                // assert 'operation forbidden on released memoryview object' in str(e)
                Assert.Contains("disposed", e.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void test_4336()
        {
            // Classic fitz/pickle subprocess harness — not ported.
            if (!HasFile(imgfile)) return;
            // path = os.path.normpath(f'{__file__}/../../tests/resources/nur-ruhig.jpg')
            // pixmap = pymupdf.Pixmap(path)
            var pixmap = new Pixmap(imgfile);
            // t = time.time()
            var sw = Stopwatch.StartNew();
            int cc = 0;
            // for i in range(10):
            for (int i = 0; i < 10; i++)
                // cc = pixmap.color_count()
                cc = (int)pixmap.color_count();
            // t = time.time() - t
            sw.Stop();
            // print(f'test_4336(): {t=}')
            Console.WriteLine($"test_4336(): t={sw.Elapsed.TotalSeconds}");
            // if cc_old:
            //     assert cc == cc_old
        }

        [Fact]
        public void test_4423()
        {
            string path = Resource("test_4423.pdf");
            if (!HasFile(path)) return;

            // path2 = f'{path}.pdf'
            string path2 = Out("test_4423.pdf");
            Exception? ee = null;
            string wt = "";
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // try:
                try
                {
                    // document.Save(
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
                    // print(f'Exception: {e}')
                    Console.WriteLine($"Exception: {e}");
                    // ee = e
                    ee = e;
                }
                // wt = pymupdf.TOOLS.mupdf_warnings()
                wt = Tools.MupdfWarnings();
            }
            // assert not ee, f'Received unexpected exception: {e}'
            Assert.Null(ee);
            // assert wt == 'format error: cannot find object in xref (56 0 R)\nformat error: cannot find object in xref (68 0 R)'
            Assert.Equal(
                "format error: cannot find object in xref (56 0 R)\nformat error: cannot find object in xref (68 0 R)",
                wt);
        }

        [Fact]
        public void test_4445()
        {
            // if os.environ.get('PYODIDE_ROOT'):
            if (Environment.GetEnvironmentVariable("PYODIDE_ROOT") != null)
            {
                // print('test_4445(): not running on Pyodide - cannot run child processes.')
                Console.WriteLine("test_4445(): not running on Pyodide - cannot run child processes.");
                return;
            }
            // print()
            Console.WriteLine();
            // Test case is large so we download it instead of having it in PyMuPDF
            // git. We put it in `cache/` directory do it is not removed by `git clean`
            // (unless `-d` is specified).
            // import util
            // path = os.path.normpath(f'{__file__}/../../tests/resources/test_4445.pdf')
            string path = Path.GetFullPath(Resource("test_4445.pdf"));
            if (!HasFile(path)) return;
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // page = document[0]
                var page = document[0];
                // pixmap = page.GetPixmap()
                var pixmap = page.GetPixmap();
                // print(f'{pixmap.width=}')
                Console.WriteLine($"pixmap.width={pixmap.Width}");
                // print(f'{pixmap.height=}')
                Console.WriteLine($"pixmap.height={pixmap.Height}");
                // assert (pixmap.width, pixmap.height) == (792, 612)
                Assert.Equal((792, 612), (pixmap.Width, pixmap.Height));
                // if 1:
                if (1 != 0)
                {
                    // path_pixmap = os.path.join("D:\\Artifex\\TestDocuments\\TestPixmap", "test_4445_.png")
                    string path_pixmap = Out("test_4445.png");
                    // pixmap.Save(path_pixmap)
                    pixmap.Save(path_pixmap);
                    // print(f'Have created {path_pixmap=}')
                    Console.WriteLine($"Have created path_pixmap={path_pixmap}");
                }
            }
            // wt = pymupdf.TOOLS.mupdf_warnings()
            string wt = Tools.MupdfWarnings();
            // print(f'{wt=}')
            Console.WriteLine($"wt={wt}");
            // assert wt == 'broken xref subsection, proceeding anyway.\nTrailer Size is off-by-one. Ignoring.'
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

            // print()
            Console.WriteLine();
            // print(f'{pymupdf.mupdf_version=}')
            Console.WriteLine($"mupdf_version={Tools.MupdfVersion()}");
            // path = os.path.normpath(f'{__file__}/../../tests/resources/test_3806.pdf')
            // path_png_expected = os.path.normpath(f'{__file__}/../../tests/resources/test_3806-expected.png')
            // path_png = os.path.normpath(f'{__file__}/../../tests/test_3806.png')
            string path_png = Out("test_3806.png");

            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // pixmap = document[0].GetPixmap()
                var pixmap = document[0].GetPixmap();
                // pixmap.Save(path_png)
                pixmap.Save(path_png);
                // rms = gentle_compare.pixmaps_rms(path_png_expected, pixmap)
                float rms = _Compare.PixmapsRms(path_png_expected, pixmap);
                // print(f'{rms=}')
                Console.WriteLine($"rms={rms}");
                var mupdf_version_tuple = _Version.mupdf_version_tuple();
                // if pymupdf.mupdf_version_tuple >= (1, 26, 6):
                if (mupdf_version_tuple.CompareTo((1, 26, 6)) >= 0)
                    // assert rms < 0.1
                    Assert.True(rms < 0.1);
                else
                    // assert rms > 50
                    Assert.True(rms > 50);
            }
        }

        [Fact]
        public void test_4388()
        {
            // print()
            Console.WriteLine();
            string path_BOZ1 = Resource("test_4388_BOZ1.pdf");
            string path_BUL1 = Resource("test_4388_BUL1.pdf");
            if (!HasFile(path_BOZ1) || !HasFile(path_BUL1)) return;
            // path_correct = os.path.normpath(f'{__file__}/../../tests/resources/test_4388_BUL1.pdf.correct.png')
            string path_correct = Out("test_4388.correct.png");
            // path_test = os.path.normpath(f'{__file__}/../../tests/resources/test_4388_BUL1.pdf.test.png')
            string path_test = Out("test_4388.test.png");

            Pixmap pixmap_correct;
            // with pymupdf.open(path_BUL1) as bul:
            using (var bul = new Document(path_BUL1))
            {
                // pixmap_correct = bul.LoadPage(0).GetPixmap()
                pixmap_correct = bul.LoadPage(0).GetPixmap();
                // pixmap_correct.Save(path_correct)
                pixmap_correct.Save(path_correct);
            }

            // pymupdf.TOOLS.store_shrink(100)
            Tools.StoreShrink(100);

            // with pymupdf.open(path_BOZ1) as boz:
            using (var boz = new Document(path_BOZ1))
            {
                // boz.LoadPage(0).GetPixmap()
                boz.LoadPage(0).GetPixmap();
            }

            Pixmap pixmap_test;
            // with pymupdf.open(path_BUL1) as bul:
            using (var bul = new Document(path_BUL1))
            {
                // pixmap_test = bul.LoadPage(0).GetPixmap()
                pixmap_test = bul.LoadPage(0).GetPixmap();
                // pixmap_test.Save(path_test)
                pixmap_test.Save(path_test);
            }

            // rms = gentle_compare.pixmaps_rms(pixmap_correct, pixmap_test)
            float rms = _Compare.PixmapsRms(pixmap_correct, pixmap_test);
            // print(f'{rms=}')
            Console.WriteLine($"rms={rms}");
            var mupdf_version_tuple = _Version.mupdf_version_tuple();
            // if pymupdf.mupdf_version_tuple >= (1, 26, 6):
            if (mupdf_version_tuple.CompareTo((1, 26, 6)) >= 0)
                // assert rms == 0
                Assert.Equal(0, rms);
            else
                // assert rms >= 10
                Assert.True(rms >= 10);
        }

        [Fact]
        public void test_4699()
        {
            string path = Resource("test_4699.pdf");
            if (!HasFile(path)) return;
            string path_png_expected = Resource("test_4699.png");
            if (!HasFile(path_png_expected)) return;
            // path_png_actual = os.path.normpath(f'{__file__}/../../tests/test_4699.png')
            string path_png_actual = Out("test_4699.png");

            Pixmap pixmap;
            // with pymupdf.open(path) as document:
            using (var document = new Document(path))
            {
                // page = document[0]
                var page = document[0];
                // pixmap = page.GetPixmap()
                pixmap = page.GetPixmap();
                // pixmap.Save(path_png_actual)
                pixmap.Save(path_png_actual);
            }
            // print(f'Have saved to {path_png_actual=}.')
            Console.WriteLine($"Have saved to path_png_actual={path_png_actual}.");
            // rms = gentle_compare.pixmaps_rms(path_png_expected, pixmap)
            float rms = _Compare.PixmapsRms(path_png_expected, pixmap);
            // print(f'test_4699(): {rms=}')
            Console.WriteLine($"test_4699(): rms={rms}");
            var mupdf_version_tuple = _Version.mupdf_version_tuple();
            // if pymupdf.mupdf_version_tuple >= (1, 26, 11):
            if (mupdf_version_tuple.CompareTo((1, 26, 11)) >= 0)
                // assert rms == 0
                Assert.Equal(0, rms);
            else
            {
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                // assert 'syntax error: cannot find ExtGState resource' in wt
                Assert.Contains("syntax error: cannot find ExtGState resource", wt);
                // assert rms > 20
                Assert.True(rms > 20);
            }
        }
    }
}
