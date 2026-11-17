using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestRewriteImages/</c>; outputs: <c>TestDocuments/_Output/TestRewriteImages/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestRewriteImages
    {
        private const string TestClassName = nameof(TestRewriteImages);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static bool HasFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required test document not found: {path}");
            return true;
        }

        [Fact]
        public void test_rewrite_images()
        {

            string filename = Doc("test-rewrite-images.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            long size0 = new System.IO.FileInfo(doc.Name).Length;
            doc.RewriteImages(dpiThreshold: 100, dpiTarget: 72, quality: 33);
            byte[] data;
            using (var ms = new MemoryStream())
            {
                doc.Save(ms, garbage: 3, deflate: 1);
                data = ms.ToArray();
            }
            int size1 = data.Length;
            Assert.True((1 - (size1 / (float)size0)) > 0.3);
            doc.Save(Out("test_rewrite_images_out.pdf"));
        }

        [Fact]
        public void test_rewrite_images_set_to_gray()
        {
            string filename = Doc("test-rewrite-images.pdf");
            using var doc = new Document(filename);
            doc.RewriteImages(dpiThreshold: 100, dpiTarget: 72, quality: 33, setToGray: true);
            doc.Save(Out("test_rewrite_images_gray.pdf"), garbage: 3, deflate: 1);
        }

        [Fact]
        public void test_rewrite_images_options()
        {
            string filename = Doc("test-rewrite-images.pdf");
            using var doc = new Document(filename);
            var opts = new mupdf.PdfImageRewriterOptions();
            opts.color_lossy_image_recompress_method = mupdf.mupdf.FZ_RECOMPRESS_JPEG;
            opts.color_lossy_image_recompress_quality = "50";
            doc.RewriteImages(options: opts);
            doc.Save(Out("test_rewrite_images_options.pdf"), garbage: 3, deflate: 1);
        }

        [Fact]
        public void test_5164()
        {
            int num_errors = 0;
            var cases = new (string label, Dictionary<string, int> kwargs)[]
            {
                ("defaults", new Dictionary<string, int>()),
                ("downsampling", new Dictionary<string, int> { ["dpi_threshold"] = 100, ["dpi_target"] = 72 }),
            };
            var images = new (string mode, string fmt)[]
            {
                ("L", "PNG"), ("L", "JPEG"), ("CMYK", "JPEG"), ("RGB", "PNG"),
            };
            foreach (var (label, kwargs) in cases)
            {
                string kw = string.Join(", ", kwargs.Select(kv => $"{kv.Key}={kv.Value}"));
                Console.WriteLine($"\nrewrite_images(quality=75, {kw})");
                foreach (var (mode, fmt) in images)
                {
                    byte[] buf = File.ReadAllBytes(Doc($"test_5164_{mode}.{(fmt == "JPEG" ? "jpg" : "png")}"));
                    using var doc = new Document();
                    var page = doc.NewPage(width: 595, height: 842);
                    page.InsertImage(page.Rect, stream: buf);
                    if (kwargs.Count == 0)
                        doc.RewriteImages(quality: 75);
                    else
                        doc.RewriteImages(quality: 75, dpiThreshold: kwargs["dpi_threshold"], dpiTarget: kwargs["dpi_target"]);

                    int xref = page.GetImages(full: true)[0].Xref;
                    int jpeg = ImageBandCount(doc.XrefStreamRaw(xref));
                    int declared = ColorspaceComponents(doc, xref);
                    string verdict;
                    if (jpeg == declared)
                    {
                        verdict = "ok";
                    }
                    else
                    {
                        verdict = "MISMATCH";
                        num_errors += 1;
                    }
                    Console.WriteLine($"  {mode,-4} {fmt,-4}: JPEG has {jpeg} components, /ColorSpace declares {declared}  {verdict}");
                    doc.Save(Out($"test_5164_out_{mode}_{fmt}.pdf"));
                }
            }
            Console.WriteLine($"num_errors={num_errors}");
            if (_Version.mupdf_version_tuple_at_least(1, 28, 7))
                Assert.True(num_errors == 0, $"num_errors={num_errors}");
            else
                Assert.Equal(5, num_errors);
        }

        private static int ColorspaceComponents(Document doc, int xref)
        {
            var (kind, value) = doc.XrefGetKey(xref, "ColorSpace");
            if (kind == "name")
            {
                return value switch
                {
                    "/DeviceGray" => 1,
                    "/DeviceRGB" => 3,
                    "/DeviceCMYK" => 4,
                    _ => throw new InvalidOperationException($"unexpected ColorSpace name {value}"),
                };
            }
            string text = kind == "array" ? value : doc.XrefObject(int.Parse(value.Split()[0]));
            var icc = Regex.Match(text, @"/ICCBased (\d+) 0 R");
            return int.Parse(doc.XrefGetKey(int.Parse(icc.Groups[1].Value), "N").value);
        }

        /// <summary>Pillow <c>Image.open(...).getbands</c> length for JPEG/PNG bytes.</summary>
        private static int ImageBandCount(byte[] data)
        {
            if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
                return JpegComponents(data);
            if (data.Length >= 24 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return PngBands(data);
            using var pix = new Pixmap(data);
            return pix.N - (pix.Alpha != 0 ? 1 : 0);
        }

        private static int JpegComponents(byte[] data)
        {
            int i = 2;
            while (i + 9 < data.Length)
            {
                if (data[i] != 0xFF)
                {
                    i++;
                    continue;
                }
                byte marker = data[i + 1];
                if (marker == 0x00 || marker == 0xFF)
                {
                    i++;
                    continue;
                }
                if (marker == 0xD8 || marker == 0xD9 || (marker >= 0xD0 && marker <= 0xD7))
                {
                    i += 2;
                    continue;
                }
                int len = (data[i + 2] << 8) | data[i + 3];
                if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
                    return data[i + 9];
                if (len < 2)
                    break;
                i += 2 + len;
            }
            throw new InvalidOperationException("JPEG SOF not found");
        }

        private static int PngBands(byte[] data)
        {
            byte colorType = data[25];
            return colorType switch
            {
                0 => 1,
                2 => 3,
                3 => 1,
                4 => 2,
                6 => 4,
                _ => throw new InvalidOperationException($"PNG color type {colorType}"),
            };
        }
    }
}
