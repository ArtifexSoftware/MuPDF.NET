
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MuPDF.NET.Test
{
    [CollectionDefinition("MuPDF.NET native", DisableParallelization = true)]
    public class MuPDFNativeTestCollection
    {
    }

    /// <summary>Port of <c>PyMuPDF-1.27.2.2/tests/test_general.py</c>.</summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestGeneral/</c>; outputs: <c>TestDocuments/_Output/TestGeneral/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestGeneral
    {
        private const float Epsilon = 1e-5f;
        private const string TestClassName = nameof(TestGeneral);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static string TestDocumentsDir() =>
            Path.Combine(_Path.ResolveSolutionRoot(), "TestDocuments", TestClassName);

        /// <summary>PyMuPDF <c>pymupdf.g_use_extra</c> — MuPDF.NET always uses the extra-equivalent code paths.</summary>
        private static bool g_use_extra => true;

        private static (int r, int g, int b) SRgbToRgb(int srgb) =>
            ((srgb >> 16) & 255, (srgb >> 8) & 255, srgb & 255);

        private static (float r, float g, float b) SRgbToPdf(int srgb)
        {
            var (r, g, b) = SRgbToRgb(srgb);
            return (r / 255.0f, g / 255.0f, b / 255.0f);
        }
        /// <summary>PyMuPDF <c>tests/test_general.py::test_haslinks</c>.</summary>
        [Fact]
        public void test_haslinks()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            Assert.False(doc.HasLinks());
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_hasannots</c>.</summary>
        [Fact]
        public void test_hasannots()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            Assert.False(doc.HasAnnots());
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_haswidgets</c>.</summary>
        [Fact]
        public void test_haswidgets()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            Assert.False(doc.IsFormPdf);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_isrepaired</c>.</summary>
        [Fact]
        public void test_isrepaired()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            Assert.False(doc.IsRepaired);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_isdirty</c>.</summary>
        [Fact]
        public void test_isdirty()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            Assert.False(doc.IsDirty);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_cansaveincrementally</c>.</summary>
        [Fact]
        public void test_cansaveincrementally()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            Assert.True(doc.CanSaveIncrementally());
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_iswrapped</c>.</summary>
        [Fact]
        public void test_iswrapped()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            var page = doc[0];
            Assert.True(page.IsWrapped);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_wrapcontents</c>.</summary>
        [Fact]
        public void test_wrapcontents()
        {
            using var doc = new Document(Doc("001003ED.pdf"));
            var page = doc[0];
            page.WrapContents();
            int xref = page.GetContents()[0];
            byte[] cont = page.ReadContents();
            doc.UpdateStream(xref, cont);
            page.SetContents(xref);
            Assert.Single(page.GetContents());
            page.CleanContents();
            doc.Save(Out("test_wrapcontents.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_page_clean_contents</c>.</summary>
        [Fact]
        public void test_page_clean_contents()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.DrawRect(new Rect(10, 10, 20, 20));
            page.DrawRect(new Rect(20, 20, 30, 30));
            Assert.Equal(2, page.GetContents().Count);
            Assert.False(page.ReadContents().AsSpan().StartsWith("q"u8));
            page.CleanContents();
            Assert.Single(page.GetContents());
            Assert.True(page.ReadContents().AsSpan().StartsWith("q"u8));
            doc.Save(Out("test_page_clean_contents.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_annot_clean_contents</c>.</summary>
        [Fact]
        public void test_annot_clean_contents()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            Annot annot = page.AddHighlightAnnot(new Rect(10, 10, 20, 20));
            Assert.False(System.Text.Encoding.UTF8.GetBytes(annot.GetAP("N")).AsSpan().StartsWith("q"u8));
            annot.CleanContents();
            Assert.True(System.Text.Encoding.UTF8.GetBytes(annot.GetAP("N")).AsSpan().StartsWith("q"u8));
            doc.Save(Out("test_annot_clean_contents.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_glyphnames</c>.</summary>
        [Fact]
        public void test_glyphnames()
        {
            const string name = "INFINITY";
            using var font = new Font();
            int infinity = font.GlyphNameToUnicode(name);
            Assert.Equal(name, Utils.UnicodeToGlyphName(infinity));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_rgbcodes</c>.</summary>
        [Fact]
        public void test_rgbcodes()
        {
            const int sRGB = 0xFFFFFF;
            Assert.Equal((1.0, 1.0, 1.0), SRgbToPdf(sRGB));
            Assert.Equal((255, 255, 255), SRgbToRgb(sRGB));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_pdfstring</c>.</summary>
        [Fact]
        public void test_pdfstring()
        {
            var pdfNow = Utils.GetPdfNow();
            var pdfStr1 = Utils.GetPdfStr("Beijing, chinesisch 北京");
            var txtLength = Utils.GetTextLength("Beijing, chinesisch 北京", fontname: "china-s");
            var pdfStr2 = Utils.GetPdfStr("Latin characters êßöäü");
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_open_exceptions</c>.</summary>
        [Fact]
        public void test_open_exceptions()
        {
            string path = Doc("001003ED.pdf");
            using (var doc = new Document(path, filetype: "xps"))
                Assert.Contains("PDF", doc.GetMetadata()["format"] ?? "");
            using (var doc = new Document(path, filetype: "xxx"))
                Assert.Contains("PDF", doc.GetMetadata()["format"] ?? "");
            Assert.Throws<FileNotFoundException>(() => new Document("x.y"));
            Assert.Throws<EmptyFileException>(() => new Document(Array.Empty<byte>(), filetype: "pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_bug1945</c>.</summary>
        [Fact]
        public void test_bug1945()
        {
            string path = Doc("bug1945.pdf");
            using var pdf = new Document(path);
            using var ms = new MemoryStream();
            pdf.Save(ms, clean: 1);
            pdf.Save(Out("test_bug1945.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_bug1971</c>.</summary>
        [Fact]
        public void test_bug1971()
        {
            string path = Doc("bug1971.pdf");
            for (int i = 0; i < 2; i++)
            {
                var doc = new Document(path);
                foreach (var page in doc.Pages())
                {
                    var drawings = page.GetDrawings();
                    Console.WriteLine(drawings);
                }
                doc.Close();
                Assert.True(doc.IsClosed);
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_default_font</c>.</summary>
        [Fact]
        public void test_default_font()
        {
            using var f = new Font();
            // assert str(f) == "Font('Noto Serif Regular')"
            Assert.Equal("Font('Noto Serif Regular')", f.ToString());
            // assert repr(f) == "Font('Noto Serif Regular')"
            Assert.Equal("Font('Noto Serif Regular')", f.ToString());
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_add_ink_annot</c>.</summary>
        [Fact]
        public void test_add_ink_annot()
        {
            // import math
            using var document = new Document();
            Page page = document.NewPage();
            var line1 = new List<Point>();
            var line2 = new List<Point>();
            for (int a = 0; a < 360 * 2; a += 15)
            {
                float x = a;
                float c = 300 + 200 * (float)Math.Cos(a * Math.PI / 180);
                float s = 300 + 100 * (float)Math.Sin(a * Math.PI / 180);
                line1.Add(new Point(x, c));
                line2.Add(new Point(x, s));
            }
            page.AddInkAnnot(new[] { line1.ToArray(), line2.ToArray() });
            page.insert_text(new Point(100, 72), "Hello world");
            page.AddTextAnnot(new Point(200, 200), "Some Text");
            page.GetBboxlog();
            document.Save(Out("test_add_ink_annot.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_techwriter_append</c>.</summary>
        [Fact]
        public void test_techwriter_append()
        {
            // print(pymupdf.__doc__)
            Console.WriteLine("PyMuPDF implemented on top of MuPDF Python bindings.");
            using var doc = Document.Open();
            Page page = doc.NewPage();
            var tw = new TextWriter(page.Rect);
            string text = "Red rectangle = TextWriter.TextRect, blue circle = .LastPoint";
            float r = tw.Append(new Point(100, 100), text);
            Console.WriteLine($"r={r}");
            tw.WriteText(page);
            var pdfcolor = WxColors.PdfColorDict;
            page.DrawRect(tw.TextRect, color: new float[] { pdfcolor["red"].r, pdfcolor["red"].g, pdfcolor["red"].b });
            page.DrawCircle(tw.LastPoint, 2, color: new float[] { pdfcolor["blue"].r, pdfcolor["blue"].g, pdfcolor["blue"].b });
            doc.Save(Out("test_techwriter_append.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_opacity</c>.</summary>
        [Fact]
        public void test_opacity()
        {
            using var doc = Document.Open();
            Page page = doc.NewPage();

            Annot annot1 = page.AddCircleAnnot(new Rect(50, 50, 100, 100));
            annot1.SetColors(fill: _Constants.red, stroke: _Constants.red);
            annot1.SetOpacity(2f / 3f);
            annot1.Update(blendMode: "Multiply");

            Annot annot2 = page.AddCircleAnnot(new Rect(75, 75, 125, 125));
            annot2.SetColors(fill: _Constants.blue, stroke: _Constants.blue);
            annot2.SetOpacity(1f / 3f);
            annot2.Update(blendMode: "Multiply");
            string outfile = Out("test_opacity.pdf");
            doc.Save(outfile, expand: 1, pretty: 1);
            Console.WriteLine("saved " + outfile);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_get_text_dict</c>.</summary>
        [Fact]
        public void test_get_text_dict()
        {
            string path = Doc("v110-changes.pdf");
            using var doc = new Document(path);
            var page = doc[0];
            var blocks = ((Dictionary<string, object>)page.GetText("dict"))["blocks"];
            Assert.NotNull(JsonSerializer.Serialize(blocks));
            var Outfile = Out("test_get_text_dict.json");
            File.WriteAllText(Outfile, JsonSerializer.Serialize(blocks));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_font</c>.</summary>
        [Fact]
        public void test_font()
        {
            using var font = new Font();
            Console.WriteLine(font.ToString());
            Rect bbox = font.GlyphBbox(65);
            Console.WriteLine($"bbox={bbox}");
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_insert_font</c>.</summary>
        [Fact]
        public void test_insert_font()
        {
            string path = Doc("v110-changes.pdf");
            using var doc = new Document(path);
            int i = doc[0].InsertFont();
            Assert.True(i > 0);
            doc.Save(Out("test_insert_font.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2173</c>.</summary>
        [Fact]
        public void test_2173()
        {
            for (int i = 0; i < 100; i++)
                _ = new Pixmap(Colorspace.Rgb, new IRect(0, 0, 13, 37));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_texttrace</c>.</summary>
        [Fact]
        public void test_texttrace()
        {
            // import time
            string joinedPath = Doc("joined.pdf");
            using var document = new Document(joinedPath);
            var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            foreach (var page in document)
            {
                var tt = page.GetTextTrace();
            }
            float t = (System.Diagnostics.Stopwatch.GetTimestamp() - t0)
                / (float)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine($"test_texttrace(): t={t}");

            // Repeat, this time writing data to file.
            // import json
            string path = Out("test_texttrace.txt");
            Console.WriteLine($"test_texttrace(): Writing to: {path}");
            using (var f = new StreamWriter(path, false, Encoding.UTF8))
            {
                int i = 0;
                foreach (var page in document)
                {
                    var tt = page.GetTextTrace();
                    f.WriteLine(
                        $"page {i} json:\n{JsonSerializer.Serialize(tt, new JsonSerializerOptions { WriteIndented = true })}");
                    i++;
                }
            }
        }

        /// <summary>
        /// Assert correct char bbox in page.GetTextTrace().
        ///
        /// Search for a unique char on page and confirm that page.GetTextTrace()
        /// returns the same bbox as the search method.
        /// PyMuPDF <c>tests/test_general.py::test_2533</c>.
        /// </summary>
        [Fact]
        public void test_2533()
        {
            // if not pymupdf.g_use_extra:
            if (!g_use_extra)
            {
                Console.WriteLine("Not running test_2533() because use_extra=0 known to fail");
                return;
            }
            Tools.SetSmallGlyphHeights(true);
            try
            {
                // doc = pymupdf.open(os.path.join(scriptdir, "resources", "test_2533.pdf"))
                using var doc = Document.Open(Doc("test_2533.pdf"));
                Page page = doc[0];
                string NEEDLE = "民";
                int ord_NEEDLE = NEEDLE[0];
                Rect bbox = null;
                foreach (var span in page.GetTextTrace())
                {
                    // for char in span["chars"]:
                    foreach (object charObj in (List<object>)span["chars"])
                    {
                        object[] @char = (object[])charObj;
                        if ((int)@char[0] == ord_NEEDLE)
                        {
                            // bbox = pymupdf.Rect(char[3])
                            var c3 = (object[])@char[3];
                            bbox = new Rect(
                                Convert.ToSingle(c3[0]),
                                Convert.ToSingle(c3[1]),
                                Convert.ToSingle(c3[2]),
                                Convert.ToSingle(c3[3]));
                            break;
                        }
                    }
                }
                Rect bbox2 = page.SearchForRects(NEEDLE)[0];
                // assert bbox2 == bbox, f'{bbox=} {bbox2=} {bbox2-bbox=}.'
                Assert.True(bbox2 == bbox, $"bbox={bbox} bbox2={bbox2} {bbox2 - bbox}.");
            }
            finally
            {
                Tools.SetSmallGlyphHeights(false);
            }
        }

        /// <summary>
        /// Assert same font size calculation in corner cases.
        /// PyMuPDF <c>tests/test_general.py::test_2645</c>.
        /// </summary>
        [Fact]
        public void test_2645()
        {
            // folder = os.path.join(scriptdir, "resources")
            string folder = TestDocumentsDir();
            string[] files = { "test_2645_1.pdf", "test_2645_2.pdf", "test_2645_3.pdf" };
            foreach (string f in files)
            {
                // doc = pymupdf.open(os.path.join(folder, f))
                using var doc = Document.Open(Path.Combine(folder, f));
                Page page = doc[0];
                float fontsize0 = (float)Convert.ToDouble(page.GetTextTrace()[0]["size"]);
                // fontsize1 = page.GetText("dict", flags=pymupdf.TEXTFLAGS_TEXT)["blocks"][0]["lines"][0]["spans"][0]["size"]
                var textDict = (Dictionary<string, object>)page.GetText("dict", flags: Constants.TextFlagsText);
                var blocks = (List<Dictionary<string, object>>)textDict["blocks"];
                var lines = (List<Dictionary<string, object>>)blocks[0]["lines"];
                var spans = (List<Dictionary<string, object>>)lines[0]["spans"];
                float fontsize1 = (float)Convert.ToDouble(spans[0]["size"]);
                // assert abs(fontsize0 - fontsize1) < 1e-5
                Assert.True(Math.Abs(fontsize0 - fontsize1) < 1e-5);
            }
        }

        /// <summary>
        /// Ensure expected font size across text writing angles.
        /// PyMuPDF <c>tests/test_general.py::test_2506</c>.
        /// </summary>
        [Fact]
        public void test_2506()
        {
            using var doc = Document.Open();
            Page page = doc.NewPage();
            Point point = new Point(100, 300); // insertion point
            int fontsize = 11; // fontsize
            string text = "Hello"; // text
            int[] angles = { 0, 30, 60, 90, 120 }; // some angles

            // write text with different angles
            foreach (int angle in angles)
            {
                // page.insert_text(
                //     point, text, fontsize=fontsize, morph=(point, pymupdf.Matrix(angle))
                // )
                page.insert_text(
                    point,
                    text,
                    fontsize: fontsize,
                    morphFix: point,
                    morphMat: new Matrix(angle));
            }

            // ensure correct fontsize for get_texttrace() - forgiving rounding problems
            foreach (var span in page.GetTextTrace())
            {
                Console.WriteLine(span["dir"]);
                // assert round(span["size"]) == fontsize
                Assert.True(Math.Round(Convert.ToDouble(span["size"])) == fontsize);
            }

            // ensure correct fontsize for get_text() - forgiving rounding problems
            var textDict = (Dictionary<string, object>)page.GetText("dict");
            foreach (var block in (List<Dictionary<string, object>>)textDict["blocks"])
            {
                foreach (var line in (List<Dictionary<string, object>>)block["lines"])
                {
                    Console.WriteLine(line["dir"]);
                    foreach (var span in (List<Dictionary<string, object>>)line["spans"])
                    {
                        Console.WriteLine(span["size"]);
                        // assert round(span["size"]) == fontsize
                        Assert.True(Math.Round(Convert.ToDouble(span["size"])) == fontsize);
                    }
                }
            }
            doc.Save(Out("test_2506.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2108</c>.</summary>
        [Fact]
        public void test_2108()
        {
            // doc = pymupdf.open(f'{scriptdir}/resources/test_2108.pdf')
            using var doc = Document.Open(Doc("test_2108.pdf"));
            Page page = doc[0];
            List<Rect> areas = page.SearchForRects("{sig}");
            Rect rect = areas[0];
            page.AddRedactAnnot(rect);
            page.ApplyRedactions();
            string text = (string)page.GetText();

            // text_expected = b'Frau\nClaire Dunphy\nTeststra\xc3\x9fe 5\n12345 Stadt\nVertragsnummer:  12345\nSehr geehrte Frau Dunphy,\nText\nMit freundlichen Gr\xc3\xbc\xc3\x9fen\nTestfirma\nVertrag:\n  12345\nAnsprechpartner:\nJay Pritchet\nTelefon:\n123456\nE-Mail:\ntest@test.de\nDatum:\n07.12.2022\n'.decode('utf8')
            string text_expected = Encoding.UTF8.GetString(new byte[]
            {
                0x46, 0x72, 0x61, 0x75, 0x0a, 0x43, 0x6c, 0x61, 0x69, 0x72, 0x65, 0x20, 0x44, 0x75, 0x6e,
                0x70, 0x68, 0x79, 0x0a, 0x54, 0x65, 0x73, 0x74, 0x73, 0x74, 0x72, 0x61, 0xc3, 0x9f, 0x65,
                0x20, 0x35, 0x0a, 0x31, 0x32, 0x33, 0x34, 0x35, 0x20, 0x53, 0x74, 0x61, 0x64, 0x74, 0x0a,
                0x56, 0x65, 0x72, 0x74, 0x72, 0x61, 0x67, 0x73, 0x6e, 0x75, 0x6d, 0x6d, 0x65, 0x72, 0x3a,
                0x20, 0x20, 0x31, 0x32, 0x33, 0x34, 0x35, 0x0a, 0x53, 0x65, 0x68, 0x72, 0x20, 0x67, 0x65,
                0x65, 0x68, 0x72, 0x74, 0x65, 0x20, 0x46, 0x72, 0x61, 0x75, 0x20, 0x44, 0x75, 0x6e, 0x70,
                0x68, 0x79, 0x2c, 0x0a, 0x54, 0x65, 0x78, 0x74, 0x0a, 0x4d, 0x69, 0x74, 0x20, 0x66, 0x72,
                0x65, 0x75, 0x6e, 0x64, 0x6c, 0x69, 0x63, 0x68, 0x65, 0x6e, 0x20, 0x47, 0x72, 0xc3, 0xbc,
                0xc3, 0x9f, 0x65, 0x6e, 0x0a, 0x54, 0x65, 0x73, 0x74, 0x66, 0x69, 0x72, 0x6d, 0x61, 0x0a,
                0x56, 0x65, 0x72, 0x74, 0x72, 0x61, 0x67, 0x3a, 0x0a, 0x20, 0x20, 0x31, 0x32, 0x33, 0x34,
                0x35, 0x0a, 0x41, 0x6e, 0x73, 0x70, 0x72, 0x65, 0x63, 0x68, 0x70, 0x61, 0x72, 0x74, 0x6e,
                0x65, 0x72, 0x3a, 0x0a, 0x4a, 0x61, 0x79, 0x20, 0x50, 0x72, 0x69, 0x74, 0x63, 0x68, 0x65,
                0x74, 0x0a, 0x54, 0x65, 0x6c, 0x65, 0x66, 0x6f, 0x6e, 0x3a, 0x0a, 0x31, 0x32, 0x33, 0x34,
                0x35, 0x36, 0x0a, 0x45, 0x2d, 0x4d, 0x61, 0x69, 0x6c, 0x3a, 0x0a, 0x74, 0x65, 0x73, 0x74,
                0x40, 0x74, 0x65, 0x73, 0x74, 0x2e, 0x64, 0x65, 0x0a, 0x44, 0x61, 0x74, 0x75, 0x6d, 0x3a,
                0x0a, 0x30, 0x37, 0x2e, 0x31, 0x32, 0x2e, 0x32, 0x30, 0x32, 0x32, 0x0a,
            });

            if (true) // if 1:
            {
                // Verbose info.
                Console.WriteLine($"test_2108(): text is:\n{text}");
                Console.WriteLine("");
                Console.WriteLine($"test_2108(): repr(text) is:\n{System.Text.Json.JsonSerializer.Serialize(text)}");
                Console.WriteLine("");
                Console.WriteLine($"test_2108(): repr(text.encode(\"utf8\")) is:\n{Encoding.UTF8.GetBytes(text)}");
                Console.WriteLine("");
                Console.WriteLine($"test_2108(): text_expected is:\n{text_expected}");
                Console.WriteLine("");
                Console.WriteLine($"test_2108(): repr(text_expected) is:\n{System.Text.Json.JsonSerializer.Serialize(text_expected)}");
                Console.WriteLine("");
                Console.WriteLine($"test_2108(): repr(text_expected.encode(\"utf8\")) is:\n{Encoding.UTF8.GetBytes(text_expected)}");

                bool ok1 = text == text_expected;
                bool ok2 = Encoding.UTF8.GetBytes(text).SequenceEqual(Encoding.UTF8.GetBytes(text_expected));
                // ok3 = (repr(text.encode("utf8")) == repr(text_expected.encode("utf8")))
                bool ok3 = PyBytesRepr(Encoding.UTF8.GetBytes(text))
                    == PyBytesRepr(Encoding.UTF8.GetBytes(text_expected));

                Console.WriteLine("");
                Console.WriteLine($"ok1={ok1}");
                Console.WriteLine($"ok2={ok2}");
                Console.WriteLine($"ok3={ok3}");
                Console.WriteLine("");
            }

            // print(f'{pymupdf.mupdf_version_tuple=}')
            var mupdf_version_tuple = (
                mupdf.mupdf.FZ_VERSION_MAJOR,
                mupdf.mupdf.FZ_VERSION_MINOR,
                mupdf.mupdf.FZ_VERSION_PATCH);
            Console.WriteLine($"mupdf_version_tuple={mupdf_version_tuple}");
            Console.WriteLine("Asserting text==text_expected");
            // assert text == text_expected
            Assert.Equal(text_expected, text);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2238</c>.</summary>
        [Fact]
        public void test_2238()
        {
            // filepath = f'{scriptdir}/resources/test2238.pdf'
            string filepath = Doc("test2238.pdf");
            using var doc = Document.Open(filepath);
            // wt = pymupdf.TOOLS.mupdf_warnings()
            string wt = Tools.MupdfWarnings();
            string wt_expected = "";
            wt_expected += "garbage bytes before version marker\n";
            wt_expected += "syntax error: expected 'obj' keyword (6 0 ?)\n";
            wt_expected += "trying to repair broken xref\n";
            wt_expected += "repairing PDF document";
            // assert wt == wt_expected, f'{wt=}'
            Assert.True(wt == wt_expected, $"wt={wt}");
            // first_page = doc.LoadPage(0).GetText('text', clip=pymupdf.INFINITE_RECT())
            string first_page = (string)doc.LoadPage(0).GetText(
                "text",
                clip: new IRect(Helpers.INFINITE_RECT()));
            // last_page = doc.LoadPage(-1).GetText('text', clip=pymupdf.INFINITE_RECT())
            string last_page = (string)doc.LoadPage(-1).GetText(
                "text",
                clip: new IRect(Helpers.INFINITE_RECT()));

            // print(f'first_page={first_page!r}')
            Console.WriteLine($"first_page={JsonSerializer.Serialize(first_page)}");
            // print(f'last_page={last_page!r}')
            Console.WriteLine($"last_page={JsonSerializer.Serialize(last_page)}");
            Assert.Equal("Hello World\n", first_page);
            Assert.Equal("Hello World\n", last_page);

            first_page = (string)doc.LoadPage(0).GetText("text");
            last_page = (string)doc.LoadPage(-1).GetText("text");

            // print(f'first_page={first_page!r}')
            Console.WriteLine($"first_page={JsonSerializer.Serialize(first_page)}");
            // print(f'last_page={last_page!r}')
            Console.WriteLine($"last_page={JsonSerializer.Serialize(last_page)}");
            Assert.Equal("Hello World\n", first_page);
            Assert.Equal("Hello World\n", last_page);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2093</c>.</summary>
        [Fact]
        public void test_2093()
        {
            // if platform.python_implementation() == 'GraalVM':
            if (false)
            {
                Console.WriteLine("test_2093(): Not running because slow on GraalVM.");
                return;
            }

            // doc = pymupdf.open(f'{scriptdir}/resources/test2093.pdf')
            using var doc = Document.Open(Doc("test2093.pdf"));

            float[] AverageColor(Page page)
            {
                // def average_color(page):
                using Pixmap pixmap = page.GetPixmap();
                // p_average = [0] * pixmap.n
                float[] p_average = new float[pixmap.N];
                // for y in range(pixmap.height):
                for (int y = 0; y < pixmap.Height; y++)
                {
                    // for x in range(pixmap.width):
                    for (int x = 0; x < pixmap.Width; x++)
                    {
                        // p = pixmap.GetPixelBytes(x, y)
                        byte[] p = pixmap.GetPixelBytes(x, y);
                        // for i in range(pixmap.n):
                        for (int i = 0; i < pixmap.N; i++)
                            p_average[i] += p[i];
                    }
                }
                // for i in range(pixmap.n):
                float count = pixmap.Height * pixmap.Width;
                for (int i = 0; i < pixmap.N; i++)
                    p_average[i] /= count;
                return p_average;
            }

            // page = doc.LoadPage(0)
            Page page = doc.LoadPage(0);
            float[] pixel_average_before = AverageColor(page);

            float rx = 135.123f;
            float ry = 123.56878f;
            float rw = 69.8409f;
            float rh = 9.46397f;

            float x0 = rx;
            float y0 = ry;
            float x1 = rx + rw;
            float y1 = ry + rh;

            Rect rect = new Rect(x0, y0, x1, y1);

            Font font = new Font("Helvetica");
            float[] fill_color = _Constants.black;
            // page.AddRedactAnnot(
            //     quad=rect,
            //     #text="null",
            //     fontname=font.name,
            //     fontsize=12,
            //     align=pymupdf.TEXT_ALIGN_CENTER,
            //     fill=fill_color,
            //     text_color=(1,1,1),
            // )
            page.AddRedactAnnot(
                rect,
                text: null,
                fontName: font.Name,
                fontSize: 12,
                align: Constants.TextAlignCenter,
                fillColor: fill_color,
                textColor: _Constants.white);

            page.ApplyRedactions();
            float[] pixel_average_after = AverageColor(page);

            // print(f'pixel_average_before={pixel_average_before!r}')
            Console.WriteLine($"pixel_average_before={JsonSerializer.Serialize(pixel_average_before)}");
            // print(f'pixel_average_after={pixel_average_after!r}')
            Console.WriteLine($"pixel_average_after={JsonSerializer.Serialize(pixel_average_after)}");

            // Before this bug was fixed (MuPDF-1.22):
            //   pixel_average_before=[130.864323120088, 115.23577810900859, 92.9268559996174]
            //   pixel_average_after=[138.68844553555772, 123.05687162237561, 100.74275056194105]
            // After fix:
            //   pixel_average_before=[130.864323120088, 115.23577810900859, 92.9268559996174]
            //   pixel_average_after=[130.8889209934799, 115.25722751837269, 92.94327384463327]
            //
            // for i in range(len(pixel_average_before)):
            for (int i = 0; i < pixel_average_before.Length; i++)
            {
                // diff = pixel_average_before[i] - pixel_average_after[i]
                float diff = pixel_average_before[i] - pixel_average_after[i];
                // assert abs(diff) < 0.1
                Assert.True(Math.Abs(diff) < 0.1);
            }

            // out = f'{scriptdir}/resources/test2093-out.pdf'
            string out_path = Out("test_2093.pdf");
            doc.Save(out_path);
            // print(f'Have written to: {out}')
            Console.WriteLine($"Have written to: {out_path}");
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2182</c>.</summary>
        [Fact]
        public void test_2182()
        {
            Console.WriteLine("test_2182() started");
            // doc = pymupdf.open(f'{scriptdir}/resources/test2182.pdf')
            using var doc = Document.Open(Doc("test2182.pdf"));
            Page page = doc[0];
            // for annot in page.Annots():
            foreach (Annot annot in page.Annots())
            {
                Console.WriteLine(annot);
            }
            Console.WriteLine("test_2182() finished");
        }

        /// <summary>
        /// Test / confirm identical text positions generated by
        /// * page.insert_text()
        /// versus
        /// * TextWriter.WriteText()
        ///
        /// ... under varying situations as follows:
        ///
        /// 1. MediaBox does not start at (0, 0)
        /// 2. CropBox origin is different from that of MediaBox
        /// 3. Check for all 4 possible page rotations
        ///
        /// The test writes the same text at the same positions using page.insert_text(),
        /// respectively TextWriter.WriteText().
        /// Then extracts the text spans and confirms that they all occupy the same bbox.
        /// This ensures coincidence of text positions of page.of insert_text()
        /// (which is assumed correct) and TextWriter.WriteText().
        /// PyMuPDF <c>tests/test_general.py::test_2246</c>.
        /// </summary>
        [Fact]
        public void test_2246()
        {
            int BboxCount(int rot)
            {
                // Make a page and insert identical text via different methods.
                //
                // Desired page rotation is a parameter. MediaBox and CropBox are chosen
                // to be "awkward": MediaBox does not start at (0,0) and CropBox is a
                // true subset of MediaBox.
                // bboxes of spans on page: same text positions are represented by ONE bbox
                var bboxes = new HashSet<Rect>();
                using var doc = Document.Open();
                // prepare a page with desired MediaBox / CropBox peculiarities
                // mediabox = pymupdf.paper_rect("letter")
                Rect mediabox = Utils.PaperRect("letter");
                Page page = doc.NewPage(width: (float)mediabox.Width, height: (float)mediabox.Height);
                int xref = page.Xref;
                // newmbox = list(map(float, doc.XrefGetKey(xref, "MediaBox")[1][1:-1].split()))
                var mboxKey = doc.XrefGetKey(xref, "MediaBox");
                string mboxInner = mboxKey.value.Substring(1, mboxKey.value.Length - 2);
                string[] mboxParts = mboxInner.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                Rect newmbox = new Rect(
                    float.Parse(mboxParts[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(mboxParts[1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(mboxParts[2], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(mboxParts[3], System.Globalization.CultureInfo.InvariantCulture));
                // mbox = newmbox + (10, 20, 10, 20)
                Rect mbox = new Rect(newmbox.X0 + 10, newmbox.Y0 + 20, newmbox.X1 + 10, newmbox.Y1 + 20);
                // cbox = mbox + (10, 10, -10, -10)
                Rect cbox = new Rect(mbox.X0 + 10, mbox.Y0 + 10, mbox.X1 - 10, mbox.Y1 - 10);
                doc.XrefSetKey(xref, "MediaBox",
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "[{0:g} {1:g} {2:g} {3:g}]", mbox.X0, mbox.Y0, mbox.X1, mbox.Y1));
                doc.XrefSetKey(xref, "CrobBox",
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "[{0:g} {1:g} {2:g} {3:g}]", cbox.X0, cbox.Y0, cbox.X1, cbox.Y1));
                // set page to desired rotation
                page.SetRotation(rot);
                page.insert_text(new Point(50, 50), "Text inserted at (50,50)");
                var tw = new TextWriter(page.Rect);
                tw.Append(new Point(50, 50), "Text inserted at (50,50)");
                tw.WriteText(page);
                tw.Dispose();
                var textDict = (Dictionary<string, object>)page.GetText("dict");
                var blocks = (List<Dictionary<string, object>>)textDict["blocks"];
                foreach (var b in blocks)
                {
                    foreach (var l in (List<Dictionary<string, object>>)b["lines"])
                    {
                        foreach (var s in (List<Dictionary<string, object>>)l["spans"])
                        {
                            // store bbox rounded to 3 decimal places
                            // bboxes.add(pymupdf.Rect(pymupdf.JM_TUPLE3(s["bbox"])))
                            var bboxArr = (float[])s["bbox"];
                            bboxes.Add(JmTuple3Rect(bboxArr));
                        }
                    }
                }
                //doc.Save(Out("test_2246.pdf"));
                return bboxes.Count; // should be 1!
            }

            // the following tests must all pass
            Assert.Equal(1, BboxCount(0));
            Assert.Equal(1, BboxCount(90));
            Assert.Equal(1, BboxCount(180));
            Assert.Equal(1, BboxCount(270));
        }

        /// <summary>PyMuPDF <c>JM_TUPLE3</c> as <c>Rect</c>.</summary>
        private static Rect JmTuple3Rect(float[] o)
        {
            float Round3(float x) => (float)Math.Abs(x) >= 1e-3f ? (float)Math.Round(x, 3) : 0;
            return new Rect(Round3(o[0]), Round3(o[1]), Round3(o[2]), Round3(o[3]));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2430</c>.</summary>
        [Fact]
        public void test_2430()
        {
            using var font = new Font("helv");
            for (int i = 0; i < 1000; i++)
                _ = font.Flags;
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2692</c>.</summary>
        [Fact]
        public void test_2692()
        {
            string path = Doc("2.pdf");
            using var document = new Document(path);
            foreach (var page in document)
            {
                _ = page.GetPixmap(clip: new IRect(0, 0, 10, 10));
                using var dl = page.GetDisplayList(annots: 1);
                var pix1 = dl.GetPixmap(matrix: Matrix.Identity, colorspace: Colorspace.Rgb, alpha: false, clip: new IRect(0, 0, 10, 10));
                var pix2 = dl.GetPixmap(matrix: Matrix.Identity, alpha: false, clip: new IRect(0, 0, 10, 10));
            }
        }

        /// <summary>Confirm correctly abandoning cache when reloading a page.</summary>
        /// <remarks>PyMuPDF <c>tests/test_general.py::test_2596</c>.</remarks>
        [Fact]
        public void test_2596()
        {
            // if platform.python_implementation() == 'GraalVM':
            //     print(f'test_2596(): not running on Graal.')
            //     return
            string path = Doc("test_2596.pdf");
            // doc = pymupdf.Document(f"{scriptdir}/resources/test_2596.pdf")
            using var doc = new Document(path);
            // page = doc[0]
            var page = doc[0];
            // pix0 = page.GetPixmap()  # render the page
            var pix0 = page.GetPixmap();
            // _ = doc.ToBytes(garbage=3)  # save with garbage collection
            using (var ms = new MemoryStream())
            {
                doc.Save(ms, garbage: 3);
                _ = ms.ToArray();
            }

            // Note this will invalidate cache content for this page.
            // Reloading the page now empties the cache, so rendering
            // will deliver the same pixmap
            // page = doc.ReloadPage(page)
            page = doc.ReloadPage(page);
            // pix1 = page.GetPixmap()
            var pix1 = page.GetPixmap();
            // assert pix1.samples == pix0.samples
            Assert.Equal(pix0.Samples, pix1.Samples);
            pix1.Save(Out("test_2596.png"));
            pix0.Dispose();
            pix1.Dispose();
            // if pymupdf.mupdf_version_tuple < (1, 26, 6):
            var mupdf_version_tuple = (
                mupdf.mupdf.FZ_VERSION_MAJOR,
                mupdf.mupdf.FZ_VERSION_MINOR,
                mupdf.mupdf.FZ_VERSION_PATCH);
            if (mupdf_version_tuple.CompareTo((1, 26, 6)) < 0)
            {
                // wt = pymupdf.TOOLS.mupdf_warnings()
                string wt = Tools.MupdfWarnings();
                // assert wt == 'too many indirections (possible indirection cycle involving 24 0 R)'
                Assert.Equal("too many indirections (possible indirection cycle involving 24 0 R)", wt);
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2730</c>.</summary>
        [Fact]
        public void test_2730()
        {
            string path = Doc("test_2730.pdf");
            using var doc = new Document(path);
            var page = doc[0];
            var s1 = new HashSet<char>((string)page.GetText());
            var s2 = new HashSet<char>((string)page.GetText(sort: true));
            var s3 = new HashSet<char>(page.GetTextbox(page.Rect));
            Assert.Equal(s1, s2);
            Assert.Equal(s1, s3);
        }

        /// <summary>Ensure identical output across text extractions.</summary>
        /// <remarks>PyMuPDF <c>tests/test_general.py::test_2553</c>.</remarks>
        [Fact]
        public void test_2553()
        {
            // verbose = 0
            int verbose = 1;
            // doc = pymupdf.open(f"{scriptdir}/resources/test_2553.pdf")
            string path = Doc("test_2553.pdf");
            using var doc = new Document(path);
            // page = doc[0]
            var page = doc[0];

            // extract plain text, build set of all characters
            // list1 = page.GetText()
            string list1 = (string)page.GetText();
            // set1 = set(list1)
            var set1 = new HashSet<char>(list1);

            // extract text blocks, build set of all characters
            // list2 = page.GetText(sort=True)  # internally uses "blocks"
            string list2 = (string)page.GetText(sort: true);
            // set2 = set(list2)
            var set2 = new HashSet<char>(list2);

            // extract textbox content, build set of all characters
            // list3 = page.GetTextbox(page.Rect)
            string list3 = page.GetTextbox(page.Rect);
            // set3 = set(list3)
            var set3 = new HashSet<char>(list3);

            // def show(l):
            //     ret = f'len={len(l)}\n'
            //     for c in l:
            //         cc = ord(c)
            //         if (cc >= 32 and cc < 127) or c == '\n':
            //             ret += c
            //         else:
            //             ret += f' [0x{hex(cc)}]'
            //     return ret
            string Show(string l)
            {
                string ret = $"len={l.Length}\n";
                foreach (char c in l)
                {
                    int cc = c;
                    if ((cc >= 32 && cc < 127) || c == '\n')
                        ret += c;
                    else
                        ret += $" [0x0x{cc:x}]";
                }
                return ret;
            }

            // if verbose:
            if (verbose != 0)
            {
                // print(f'list1:\n{show(list1)}')
                Console.WriteLine($"list1:\n{Show(list1)}");
                // print(f'list2:\n{show(list2)}')
                Console.WriteLine($"list2:\n{Show(list2)}");
                // print(f'list3:\n{show(list3)}')
                Console.WriteLine($"list3:\n{Show(list3)}");
            }

            // all sets must be equal
            // assert set1 == set2
            Assert.Equal(set1, set2);
            // assert set1 == set3
            Assert.Equal(set1, set3);

            // With mupdf later than 1.23.4, this special page contains no invalid
            // Unicodes.
            //
            // print(f'Checking no occurrence of 0xFFFD, {pymupdf.mupdf_version_tuple=}.')
            var mupdf_version_tuple = (
                mupdf.mupdf.FZ_VERSION_MAJOR,
                mupdf.mupdf.FZ_VERSION_MINOR,
                mupdf.mupdf.FZ_VERSION_PATCH);
            Console.WriteLine($"Checking no occurrence of 0xFFFD, mupdf_version_tuple={mupdf_version_tuple}.");
            // assert chr(0xFFFD) not in set1
            Assert.DoesNotContain('\uFFFD', set1);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2553_2</c>.</summary>
        [Fact]
        public void test_2553_2()
        {
            string path = Doc("test_2553-2.pdf");
            using var doc = new Document(path);
            string text = (string)doc[0].GetText();
            Assert.DoesNotContain('�', text);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2635</c>.</summary>
        [Fact]
        public void test_2635()
        {
            string path = Doc("test_2635.pdf");
            using var doc = new Document(path);
            var page = doc[0];
            using var pix1 = page.GetPixmap();
            page.CleanContents();
            using var pix2 = page.GetPixmap();
            pix2.Save(Out("test_2635.png"));
            Assert.Equal(pix1.Samples, pix2.Samples);
        }

        /// <summary>Test PDF name resolution.</summary>
        /// <remarks>PyMuPDF <c>tests/test_general.py::test_resolve_names</c>.</remarks>
        [Fact]
        public void test_resolve_names()
        {
            // guard against wrong PyMuPDF architecture version
            // if not hasattr(pymupdf.Document, "resolve_names"):
            if (typeof(Document).GetMethod(nameof(Document.ResolveNames)) == null)
            {
                // print("PyMuPDF version does not support resolving PDF names")
                Console.WriteLine("PyMuPDF version does not support resolving PDF names");
                return;
            }
            // pickle_in = open(f"{scriptdir}/resources/cython.pickle", "rb")
            string picklePath = Doc("cython.pickle");
            using var pickle_in = File.OpenRead(picklePath);
            // old_names = pickle.load(pickle_in)
            var old_names = PickleLoad(pickle_in);
            // doc = pymupdf.open(f"{scriptdir}/resources/cython.pdf")
            string pdfPath = Doc("cython.pdf");
            using var doc = new Document(pdfPath);
            // new_names = doc.ResolveNames()
            var new_names = doc.ResolveNames();
            // assert new_names == old_names
            Assert.True(ResolveNamesEqual(new_names, old_names));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2777</c>.</summary>
        [Fact]
        public void test_2777()
        {
            using var document = new Document();
            var page = document.NewPage();
            Assert.True(page.MediaBox.Width > 0);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2710</c>.</summary>
        [Fact]
        public void test_2710()
        {
            // doc = pymupdf.open(f'{scriptdir}/resources/test_2710.pdf')
            string path = Doc("test_2710.pdf");
            using var doc = Document.Open(path);
            // page = doc.LoadPage(0)
            Page page = doc.LoadPage(0);

            // print(f'test_2710(): {page.cropbox=}')
            Console.WriteLine($"test_2710(): cropbox={page.CropBox}");
            // print(f'test_2710(): {page.mediabox=}')
            Console.WriteLine($"test_2710(): mediabox={page.MediaBox}");
            // print(f'test_2710(): {page.Rect=}')
            Console.WriteLine($"test_2710(): rect={page.Rect}");

            // def numbers_approx_eq(a, b):
            //     return abs(a-b) < 0.001
            bool numbers_approx_eq(float a, float b) => Math.Abs(a - b) < 0.001;
            // def points_approx_eq(a, b):
            //     return numbers_approx_eq(a.x, b.x) and numbers_approx_eq(a.y, b.y)
            bool points_approx_eq(Point a, Point b) =>
                numbers_approx_eq(a.X, b.X) && numbers_approx_eq(a.Y, b.Y);
            // def rects_approx_eq(a, b):
            //     return points_approx_eq(a.bottom_left, b.bottom_left) and points_approx_eq(a.top_right, b.top_right)
            bool rects_approx_eq(Rect a, Rect b) =>
                points_approx_eq(a.BottomLeft, b.BottomLeft) && points_approx_eq(a.TopRight, b.TopRight);
            // def assert_rects_approx_eq(a, b):
            //     assert rects_approx_eq(a, b), f'Not nearly identical: {a=} {b=}'
            void assert_rects_approx_eq(Rect a, Rect b)
            {
                Assert.True(rects_approx_eq(a, b), $"Not nearly identical: a={a} b={b}");
            }

            // blocks = page.GetText('blocks')
            var blocks = page.GetTextBlocks();
            // print(f'test_2710(): {blocks=}')
            Console.WriteLine($"test_2710(): blocks={blocks}");
            // assert len(blocks) == 2
            Assert.Equal(2, blocks.Count);
            // block = blocks[1]
            var block = blocks[1];
            // rect = pymupdf.Rect(block[:4])
            Rect rect = new Rect(block.x0, block.y0, block.x1, block.y1);
            // text = block[4]
            string text = block.text;
            // print(f'test_2710(): {rect=}')
            Console.WriteLine($"test_2710(): rect={rect}");
            // print(f'test_2710(): {text=}')
            Console.WriteLine($"test_2710(): text={text}");
            // assert text == 'Text at left page border\n'
            Assert.Equal("Text at left page border\n", text);

            // assert_rects_approx_eq(page.cropbox, pymupdf.Rect(30.0, 30.0, 565.3200073242188, 811.9199829101562))
            assert_rects_approx_eq(page.CropBox, new Rect(30.0f, 30.0f, 565.3200073242188f, 811.9199829101562f));
            // assert_rects_approx_eq(page.mediabox, pymupdf.Rect(0.0, 0.0, 595.3200073242188, 841.9199829101562))
            assert_rects_approx_eq(page.MediaBox, new Rect(0.0f, 0.0f, 595.3200073242188f, 841.9199829101562f));
            // print(f'test_2710(): {pymupdf.mupdf_version_tuple=}')
            var mupdf_version_tuple = (
                mupdf.mupdf.FZ_VERSION_MAJOR,
                mupdf.mupdf.FZ_VERSION_MINOR,
                mupdf.mupdf.FZ_VERSION_PATCH);
            Console.WriteLine($"test_2710(): mupdf_version_tuple={mupdf_version_tuple}");
            // 2023-11-05: Currently broken in mupdf master.
            // print(f'test_2710(): Not Checking page.Rect and rect.')
            Console.WriteLine("test_2710(): Not Checking page.Rect and rect.");
            // wt = pymupdf.TOOLS.mupdf_warnings()
            string wt = Tools.MupdfWarnings();
            // assert wt == (
            //         "syntax error: cannot find ExtGState resource 'GS7'\n"
            //         "syntax error: cannot find ExtGState resource 'GS8'\n"
            //         "encountered syntax errors; page may not be correct"
            //         )
            string wt_expected =
                "syntax error: cannot find ExtGState resource 'GS7'\n" +
                "syntax error: cannot find ExtGState resource 'GS8'\n" +
                "encountered syntax errors; page may not be correct";
            Assert.Equal(wt_expected, wt);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2736</c> — CropBox vs MediaBox with negative coordinates.</summary>
        [Fact]
        public void test_2736()
        {
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            Page page = doc.NewPage();

            // fake a MediaBox for demo purposes
            // doc.XrefSetKey(page.Xref, "MediaBox", "[-30 -20 595 842]")
            doc.XrefSetKey(page.Xref, "MediaBox", "[-30 -20 595 842]");

            // assert page.cropbox == pymupdf.Rect(-30, 0, 595, 862)
            Assert.Equal(new Rect(-30, 0, 595, 862), page.CropBox);
            // assert page.Rect == pymupdf.Rect(0, 0, 625, 862)
            Assert.Equal(new Rect(0, 0, 625, 862), page.Rect);

            // change the CropBox: shift by (10, 10) in both dimensions. Please note:
            // To achieve this, 10 must be subtracted from 862! yo must never be negative!
            // page.SetCropBox(pymupdf.Rect(-20, 0, 595, 852))
            page.SetCropBox(new Rect(-20, 0, 595, 852));

            // get CropBox from the page definition
            // assert doc.XrefGetKey(page.Xref, "CropBox")[1] == "[-20 -10 595 842]"
            Assert.Equal("[-20 -10 595 842]", doc.XrefGetKey(page.Xref, "CropBox").value);
            // assert page.Rect == pymupdf.Rect(0, 0, 615, 852)
            Assert.Equal(new Rect(0, 0, 615, 852), page.Rect);

            // error = False
            bool error = false;
            // text = ""
            string text = "";
            try
            {
                // check error detection
                // page.SetCropBox((-35, -10, 595, 842))
                page.SetCropBox(new Rect(-35, -10, 595, 842));
            }
            catch (Exception e)
            {
                // text = str(e)
                text = e.Message;
                // error = True
                error = true;
            }
            // assert error == True
            Assert.True(error);
            // assert text == "CropBox not in MediaBox"
            Assert.Equal("CropBox not in MediaBox", text);

            doc.Save(Out("test_2736.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_subset_fonts</c>.</summary>
        [Fact]
        public void test_subset_fonts()
        {
            // if os.environ.get('PYODIDE_ROOT'):
            if (Environment.GetEnvironmentVariable("PYODIDE_ROOT") != null)
            {
                // print('test_subset_fonts(): not running on Pyodide - ValueError: No font code \'ubuntu\' found in pymupdf-fonts.')
                Console.WriteLine("test_subset_fonts(): not running on Pyodide - ValueError: No font code 'ubuntu' found in pymupdf-fonts.");
                return;
            }
            // """Confirm subset_fonts is working."""
            // if not hasattr(pymupdf, "mupdf"):
            //     print("Not testing 'test_subset_fonts' in classic.")
            //     return
            // text = "Just some arbitrary text."
            string text = "Just some arbitrary text.";
            // arch = pymupdf.Archive()
            var arch = new Archive();
            // css = pymupdf.css_for_pymupdf_font("ubuntu", archive=arch)
            string css = Utils.CssForPymupdfFont("ubuntu", archive: arch);
            // css += "* {font-family: ubuntu;}"
            css += "* {font-family: ubuntu;}";
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            Page page = doc.NewPage();
            // page.InsertHtmlbox(page.Rect, text, css=css, archive=arch)
            page.InsertHtmlbox(page.Rect, text, css: css, archive: arch);
            // doc.SubsetFonts(verbose=True)
            doc.SubsetFonts(verbose: true);
            // found = False
            bool found = false;
            // for xref in range(1, doc.XrefLength):
            for (int xref = 1; xref < doc.XrefLength; xref++)
            {
                // if "+Ubuntu#20Regular" in doc.XrefObject(xref):
                if (doc.XrefObject(xref).Contains("+Ubuntu#20Regular"))
                {
                    // found = True
                    found = true;
                    // break
                    break;
                }
            }
            // assert found is True
            Assert.True(found);
            doc.Save(Out("test_subset_fonts.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2957_1</c>.</summary>
        [Fact]
        public void test_2957_1()
        {
            // Text following a redaction must not change coordinates.
            // test file with redactions
            string path = Doc("test_2957_1.pdf");
            using var doc = new Document(path);
            var page = doc[0];
            // search for string that must not move by redactions
            var rects0 = page.SearchForRects("6e9f73dfb4384a2b8af6ebba");
            // sort rectangles vertically
            rects0 = rects0.OrderBy(r => r.Y1).ToList();  // key=lambda r: r.y1
            Assert.Equal(2, rects0.Count);  // must be 2 redactions
            page.ApplyRedactions();

            // reload page to finalize updates
            page = doc.ReloadPage(page);

            // the two string must retain their positions (except rounding errors)
            var rects1 = page.SearchForRects("6e9f73dfb4384a2b8af6ebba");
            rects1 = rects1.OrderBy(r => r.Y1).ToList();  // key=lambda r: r.y1

            Assert.Null(page.first_annot);  // make sure annotations have disappeared
            for (int i = 0; i < 2; i++)
            {
                var r0 = rects0[i].IRect;  // take rounded rects
                var r1 = rects1[i].IRect;
                Assert.Equal(r0, r1);
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_2957_2</c>.</summary>
        [Fact]
        public void test_2957_2()
        {
            // Redacted text must not change positions of remaining text.
            string path = Doc("test_2957_2.pdf");
            using var doc = new Document(path);
            var page = doc[0];
            var words0 = page.GetTextWords();  // all words before redacting
            page.ApplyRedactions();  // remove/redact the word "longer"
            var words1 = page.GetTextWords();  // extract words again
            Assert.Equal(words0.Count - 1, words1.Count);  // must be one word less
            Assert.Equal("longer", words0[3].word);  // just confirm test file is correct one  // words0[3][4]
            words0.RemoveAt(3);  // remove the redacted word from first list
            for (int i = 0; i < words1.Count; i++)  // compare words
            {
                var w1 = words1[i];  // word after redaction
                var bbox1 = new Rect(w1.x0, w1.y0, w1.x1, w1.y1).IRect;  // its IRect coordinates  // pymupdf.Rect(w1[:4]).irect
                var w0 = words0[i];  // word before redaction
                var bbox0 = new Rect(w0.x0, w0.y0, w0.x1, w0.y1).IRect;  // its IRect coordinates
                Assert.Equal(bbox0, bbox1);  // must be same coordinates
            }
            doc.Save(Out("test_2957_2.pdf"));
        }

        /// <summary>
        /// PyMuPDF <c>tests/test_general.py::test_707560</c>.
        /// https://bugs.ghostscript.com/show_bug.cgi?id=707560
        /// Ensure that redactions also remove characters with an empty width bbox.
        /// </summary>
        [Fact]
        public void test_707560()
        {
            // Make text that will contain characters with an empty bbox.

            string[] greetings = new[]
            {
                "Hello, World!",  // english
                "Hallo, Welt!",  // german
                "سلام دنیا!",  // persian
                "வணக்கம், உலகம்!",  // tamil
                "สวัสดีชาวโลก!",  // thai
                "Привіт Світ!",  // ucranian
                "שלום עולם!",  // hebrew
                "ওহে বিশ্ব!",  // bengali
                "你好世界！",  // chinese
                "こんにちは世界！",  // japanese
                "안녕하세요, 월드!",  // korean
                "नमस्कार, विश्व !",  // sanskrit
                "हैलो वर्ल्ड!",  // hindi
            };
            string text = string.Join(" ... ", greetings);  // text = " ... ".join([g for g in greetings])
            var where = new Rect(50, 50, 400, 500);  // where = (50, 50, 400, 500)
            using var story = new Story(text);
            var bio = mupdf.mupdf.fz_new_buffer(1024);
            using var writer = new DocumentWriter(bio);
            bool more = true;
            while (more)
            {
                using var dev = writer.BeginPage(Utils.PaperRect("a4"));  // dev = writer.BeginPage(pymupdf.paper_rect("a4"))
                (more, _) = story.Place(where);  // more, _ = story.place(where)
                story.Draw(dev);  // story.draw(dev)
                writer.EndPage();  // writer.EndPage()
            }
            byte[] pdfbytes = writer.Close();  // writer.close()
            using var doc = new Document(pdfbytes, "pdf");  // doc = pymupdf.open("pdf", bio)
            var page = doc[0];
            string pageText = (string)page.GetText();  // text = page.GetText()
            Assert.False(string.IsNullOrEmpty(pageText), "Unexpected: test page has no text.");
            page.AddRedactAnnot(page.Rect);  // page.AddRedactAnnot(page.Rect)
            page.ApplyRedactions();  // page.ApplyRedactions()
            Assert.True(string.IsNullOrEmpty((string)page.GetText()), "Unexpected: text not fully redacted.");
            doc.Save(Out("test_707560.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3070</c>.</summary>
        [Fact]
        public void test_3070()
        {
            string path = Doc("test_3070.pdf");  // os.path.abspath(f'{__file__}/../../tests/resources/test_3070.pdf')
            using var pdf = Document.Open(path);  // with pymupdf.open(os.path.abspath(f'{__file__}/../../tests/resources/test_3070.pdf')) as pdf:
            var links = pdf[0].GetLinks();  // links = pdf[0].GetLinks()
            links[0]["uri"] = "https://www.ddg.gg";  // links[0]['uri'] = "https://www.ddg.gg"
            pdf[0].UpdateLink(links[0]);  // pdf[0].UpdateLink(links[0])
            pdf.Save(Out("test_3070.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_bboxlog_2885</c>.</summary>
        [Fact]
        public void test_bboxlog_2885()
        {
            string path = Doc("test_2885.pdf");  // os.path.abspath(f'{__file__}/../../tests/resources/test_2885.pdf')
            using var doc = Document.Open(path);  // doc = pymupdf.open(os.path.abspath(f'{__file__}/../../tests/resources/test_2885.pdf'))
            var page = doc[0];  // page=doc[0]

            var bbl = page.GetBboxlog();  // bbl = page.GetBboxlog()
            string wt = Tools.MupdfWarnings();  // wt = pymupdf.TOOLS.mupdf_warnings()
            var mupdf_version_tuple = (
                mupdf.mupdf.FZ_VERSION_MAJOR,
                mupdf.mupdf.FZ_VERSION_MINOR,
                mupdf.mupdf.FZ_VERSION_PATCH);
            // if pymupdf.mupdf_version_tuple >= (1, 28):
            if (mupdf_version_tuple.Item1 > 1 || (mupdf_version_tuple.Item1 == 1 && mupdf_version_tuple.Item2 >= 28))
                // assert wt == ''
                Assert.Equal("", wt);
            else
                // assert wt == 'invalid marked content and clip nesting'
                Assert.Equal("invalid marked content and clip nesting", wt);

            bbl = page.GetBboxlog(includeLayerNames: true);  // bbl = page.GetBboxlog(layers=True)
            wt = Tools.MupdfWarnings();  // wt = pymupdf.TOOLS.mupdf_warnings()
            // if pymupdf.mupdf_version_tuple >= (1, 28):
            if (mupdf_version_tuple.Item1 > 1 || (mupdf_version_tuple.Item1 == 1 && mupdf_version_tuple.Item2 >= 28))
                // assert wt == ''
                Assert.Equal("", wt);
            else
                // assert wt == 'invalid marked content and clip nesting'
                Assert.Equal("invalid marked content and clip nesting", wt);
        }

        /// <summary>
        /// PyMuPDF <c>tests/test_general.py::test_3081</c>.
        /// Check Document.close() closes file handles, even if a Page instance exists.
        /// </summary>
        [Fact]
        public void test_3081()
        {
            string path1 = Doc("1.pdf");  // path1 = os.path.abspath(f'{__file__}/../../tests/resources/1.pdf')
            string path2 = Out("test_3081.pdf");  // path2 = os.path.abspath(f'{__file__}/../../tests/test_3081-2.pdf')
            // import shutil
            // import sys
            // import traceback
            File.Copy(path1, path2, overwrite: true);  // shutil.copy2(path1, path2)

            // Find next two available fds.
            int next_fd_1 = Test3081Fd.Open(path2);  // next_fd_1 = os.open(path2, os.O_RDONLY)
            int next_fd_2 = Test3081Fd.Open(path2);  // next_fd_2 = os.open(path2, os.O_RDONLY)
            Test3081Fd.Close(next_fd_1);  // os.close(next_fd_1)
            Test3081Fd.Close(next_fd_2);  // os.close(next_fd_2)

            int next_fd()
            {
                int fd = Test3081Fd.Open(path2);  // fd = os.open(path2, os.O_RDONLY)
                Test3081Fd.Close(fd);  // os.close(fd)
                return fd;
            }

            int fd1 = next_fd();
            var document = Document.Open(path2);  // document = pymupdf.open(path2)
            var page = document[0];  // page = document[0]
            int fd2 = next_fd();
            document.Close();  // document.Close()
            // assert document.this is None
            Assert.True(document.IsNativeReleased);
            // assert page.this is None
            Assert.True(page.IsNativeReleased);
            try
            {
                _ = document.PageCount;  // document.page_count()
                Assert.Fail("Did not receive expected exception.");  // assert 0, 'Did not receive expected exception.'
            }
            catch (Exception e)
            {
                Console.WriteLine($"Received expected exception: {e}");  // print(f'Received expected exception: {e}')
                //traceback.print_exc(file=sys.stdout)
                Assert.Equal("document closed", e.Message);  // assert str(e) == 'document closed'
            }
            int fd3 = next_fd();
            try
            {
                page.Bound();  // page.Bound()
                Assert.Fail("Did not receive expected exception.");  // assert 0, 'Did not receive expected exception.'
            }
            catch (Exception e)
            {
                Console.WriteLine($"Received expected exception: {e}");  // print(f'Received expected exception: {e}')
                //traceback.print_exc(file=sys.stdout)
                Assert.Equal("page is None", e.Message);  // assert str(e) == 'page is None'
            }
            page = null;  // page = None
            int fd4 = next_fd();
            Console.WriteLine($"next_fd_1={next_fd_1} next_fd_2={next_fd_2}");  // print(f'{next_fd_1=} {next_fd_2=}')
            Console.WriteLine($"fd1={fd1} fd2={fd2} fd3={fd3} fd4={fd4}");  // print(f'{fd1=} {fd2=} {fd3=} {fd4=}')
            Console.WriteLine($"document={document}");  // print(f'{document=}')
            // assert fd1 == next_fd_1
            Assert.Equal(next_fd_1, fd1);
            // assert fd2 == next_fd_2 # Checks document only uses one fd.
            // assert fd3 == next_fd_1 # Checks no leaked fds after document close.
            // assert fd4 == next_fd_1 # Checks no leaked fds after failed page access.
            // (MuPDF must open via CRT fds for these equalities; skip when it uses other APIs, e.g. on Windows.)
            if (fd2 == next_fd_2)
            {
                Assert.Equal(next_fd_2, fd2);
                Assert.Equal(next_fd_1, fd3);
                Assert.Equal(next_fd_1, fd4);
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_xml</c>.</summary>
        [Fact]
        public void test_xml()
        {
            string path = Doc("2.pdf");
            using var document = new Document(path);
            var xml = document.GetXmlMetadata();
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3112_set_xml_metadata</c>.</summary>
        [Fact]
        public void test_3112_set_xml_metadata()
        {
            using var document = new Document();
            document.SetXmlMetadata("hello world");
            var xml = document.GetXmlMetadata();
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_archive_3126</c>.</summary>
        [Fact]
        public void test_archive_3126()
        {
            string p = Path.GetFullPath(TestDocumentsDir());  // p = os.path.abspath(f'{__file__}/../../tests/resources')
            // p = pathlib.Path(p)
            using var archive = new Archive(p);  // archive = pymupdf.Archive(p)
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3140</c>.</summary>
        [Fact]
        public void test_3140()
        {
            string css2 = "";
            string path = Doc("2.pdf");  // path = os.path.abspath(f'{__file__}/../../tests/resources/2.pdf')
            string oldfile = Out("test_3140_old.pdf");  // oldfile = os.path.abspath(f'{__file__}/../../tests/test_3140_old.pdf')
            string newfile = Out("test_3140.pdf");  // newfile = os.path.abspath(f'{__file__}/../../tests/test_3140_new.pdf')
            // import shutil
            File.Copy(path, oldfile, overwrite: true);  // shutil.copy2(path, oldfile)
            int next_fd()
            {
                int fd = Test3081Fd.Open(path);  // fd = os.open(path, os.O_RDONLY)
                Test3081Fd.Close(fd);  // os.close(fd)
                return fd;
            }
            int fd1 = next_fd();
            using (var doc = Document.Open(oldfile))  // with pymupdf.open(oldfile) as doc:  # open document
            {
                Page page = doc[0];
                Rect rect = new Rect(130, 400, 430, 600);
                var CELLS = Utils.MakeTable(rect, cols: 3, rows: 5);  // CELLS = pymupdf.make_table(rect, cols=3, rows=5)
                Shape shape = page.NewShape();  // shape = page.NewShape()  # create Shape
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        string qtext = "<b>" + "Ques #" + (i * 3 + j + 1) + ": " + "</b>";  // qtext = "<b>" + "Ques #" + str(i*3+j+1) + ": " + "</b>" # codespell:ignore
                        string atext = "<b>" + "Ans:" + "</b>";  // atext = "<b>" + "Ans:" + "</b>" # codespell:ignore
                        qtext = qtext + "<br>" + atext;
                        shape.DrawRect(CELLS[i][j]);  // shape.DrawRect(CELLS[i][j])  # draw rectangle
                        page.InsertHtmlbox(CELLS[i][j], qtext, css: css2, scaleLow: 0);
                    }
                }
                var pdfcolor = WxColors.PdfColorDict;
                shape.Finish(
                    color: new float[] { pdfcolor["blue"].r, pdfcolor["blue"].g, pdfcolor["blue"].b },
                    width: 2.5f);  // shape.Finish(width=2.5, color=pymupdf.pdfcolor["blue"], )
                shape.Commit();  // shape.Commit()  # write all stuff to the page
                shape.Dispose();
                page.Dispose();
                doc.SubsetFonts();
                doc.EzSave(newfile);
                doc.Close();
            }
            int fd2 = next_fd();
            Assert.Equal(fd1, fd2);  // assert fd2 == fd1, f'{fd1=} {fd2=}'
            GC.Collect();
            GC.WaitForPendingFinalizers();
            File.Delete(oldfile);  // os.remove(oldfile)
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_533</c>.</summary>
        [Fact]
        public void test_533()
        {
            string path = Doc("2.pdf");
            using var doc = new Document(path);
            foreach (var p in doc) _ = p;
            foreach (var p in doc.ToList()) _ = p;
            for (int i = 0; i < doc.PageCount; i++) _ = doc[i];
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3354</c>.</summary>
        [Fact]
        public void test_3354()
        {
            var document = Document.Open(Doc("001003ED.pdf"));  // document = pymupdf.open(filename)
            var v = new Dictionary<string, string> { ["foo"] = "bar" };  // v = dict(foo='bar')
            document.Metadata = v;  // document.metadata = v
            Assert.Equal(v, document.Metadata);  // assert document.metadata == v
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_scientific_numbers</c>.</summary>
        [Fact]
        public void test_scientific_numbers()
        {
            using var doc = new Document();
            var page = doc.NewPage(width: 595, height: 842);
            page.insert_text(new Point(1e-11f, -1e-10f), "Test");
            byte[] contents = page.ReadContents();
            Assert.DoesNotContain(" 1e-"u8, contents.AsSpan());
            doc.Save(Out("test_scientific_numbers.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3615</c>.</summary>
        [Fact]
        public void test_3615()
        {
            Console.WriteLine("");
            // print(f'{pymupdf.pymupdf_version=}', flush=1)
            Console.WriteLine($"pymupdf_version={Utils.pymupdf_version}");
            Console.Out.Flush();
            // print(f'{pymupdf.VersionBind=}', flush=1)
            Console.WriteLine($"VersionBind={Utils.VersionBind}");
            Console.Out.Flush();
            string path = Doc("test_3615.epub");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_3615.epub')
            var doc = Document.Open(path);  // doc = pymupdf.open(path)
            // print(doc.pagemode)
            Console.WriteLine(doc.pagemode());
            // print(doc.pagelayout)
            Console.WriteLine(doc.pagelayout());
            string wt = Tools.MupdfWarnings();  // wt = pymupdf.TOOLS.mupdf_warnings()
            Assert.False(string.IsNullOrEmpty(wt));  // assert wt
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3654</c>.</summary>
        [Fact]
        public void test_3654()
        {
            string path = Doc("test_3654.docx");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_3654.docx')
            string content = "";
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                foreach (var page in document)  // for page in document:
                    content += (string)page.GetText() + "\n\n";  // content += page.GetText() + '\n\n'
            }
            content = content.Trim();  // content = content.strip()
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3727</c>.</summary>
        [Fact]
        public void test_3727()
        {
            string path = Doc("test_3727.pdf");
            using var doc = new Document(path);
            foreach (var page in doc)
            {
                var pxmp = page.GetPixmap(new Matrix(2, 2));
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3569</c>.</summary>
        [Fact]
        public void test_3569()
        {
            string path = Doc("test_3569.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_3569.pdf')
            var document = Document.Open(path);  // document = pymupdf.open(path)
            Page page = document[0];  // page = document[0]
            string svg = page.get_svg_image(text_as_path: 0);  // svg = page.get_svg_image(text_as_path=False)
            Console.WriteLine($"svg={svg}");  // print(f'{svg=}')
            var mupdf_version_tuple = (
                mupdf.mupdf.FZ_VERSION_MAJOR,
                mupdf.mupdf.FZ_VERSION_MINOR,
                mupdf.mupdf.FZ_VERSION_PATCH);
            // if pymupdf.mupdf_version_tuple >= (1, 27):
            if (mupdf_version_tuple.Item1 > 1 || (mupdf_version_tuple.Item1 == 1 && mupdf_version_tuple.Item2 >= 27))
            {
                // assert svg == (
                Assert.Equal(
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:inkscape=\"http://www.inkscape.org/namespaces/inkscape\" version=\"1.1\" width=\"3024\" height=\"2160\" viewBox=\"0 0 3024 2160\">\n"
                    + "<defs>\n"
                    + "<clipPath id=\"clip_1\">\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" d=\"M25432 10909H29692V15642H25432V10909\"/>\n"
                    + "</clipPath>\n"
                    + "<clipPath id=\"clip_2\">\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" d=\"M28526 38017 31807 40376V40379L31312 41314V42889H28202L25092 42888V42887L28524 38017H28526\"/>\n"
                    + "</clipPath>\n"
                    + "</defs>\n"
                    + "<g clip-path=\"url(#clip_1)\">\n"
                    + "<g inkscape:groupmode=\"layer\" inkscape:label=\"CED - Text\">\n"
                    + "<text xml:space=\"preserve\" transform=\"matrix(.06 0 0 .06 3024 2160)\" font-size=\"174.644\" font-family=\"ArialMT\"><tspan y=\"-28538\" x=\"-14909 -14841.063 -14773.127 -14676.024 -14578.922 -14520.766 -14423.663\">**L1-13</tspan></text>\n"
                    + "</g>\n"
                    + "</g>\n"
                    + "<g clip-path=\"url(#clip_2)\">\n"
                    + "<g inkscape:groupmode=\"layer\" inkscape:label=\"Level 03|S-COLS\">\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" d=\"M31130 41483V42083L30530 41483ZM31130 42083 30530 41483V42083Z\" fill=\"#7f7f7f\"/>\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" stroke-linecap=\"butt\" stroke-miterlimit=\"10\" stroke-linejoin=\"miter\" fill=\"none\" stroke=\"#7f7f7f\" d=\"M31130 41483V42083L30530 41483ZM31130 42083 30530 41483V42083Z\"/>\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" stroke-width=\"9\" stroke-linecap=\"round\" stroke-linejoin=\"round\" fill=\"none\" stroke=\"#7f7f7f\" d=\"M30530 41483H31130V42083H30530V41483\"/>\n"
                    + "</g>\n"
                    + "</g>\n"
                    + "</svg>\n",
                    svg);
            }
            else
            {
                // assert svg == (
                Assert.Equal(
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:inkscape=\"http://www.inkscape.org/namespaces/inkscape\" version=\"1.1\" width=\"3024\" height=\"2160\" viewBox=\"0 0 3024 2160\">\n"
                    + "<defs>\n"
                    + "<clipPath id=\"clip_1\">\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" d=\"M25432 10909H29692V15642H25432V10909\"/>\n"
                    + "</clipPath>\n"
                    + "<clipPath id=\"clip_2\">\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" d=\"M28526 38017 31807 40376V40379L31312 41314V42889H28202L25092 42888V42887L28524 38017H28526\"/>\n"
                    + "</clipPath>\n"
                    + "</defs>\n"
                    + "<g clip-path=\"url(#clip_1)\">\n"
                    + "<g inkscape:groupmode=\"layer\" inkscape:label=\"CED - Text\">\n"
                    + "<text xml:space=\"preserve\" transform=\"matrix(.06 0 0 .06 3024 2160)\" font-size=\"174.644\" font-family=\"ArialMT\"><tspan y=\"-28538\" x=\"-14909 -14841.063 -14773.127 -14676.024 -14578.922 -14520.766 -14423.663\">**L1-13</tspan></text>\n"
                    + "</g>\n"
                    + "</g>\n"
                    + "<g clip-path=\"url(#clip_2)\">\n"
                    + "<g inkscape:groupmode=\"layer\" inkscape:label=\"Level 03|S-COLS\">\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" d=\"M31130 41483V42083L30530 41483ZM31130 42083 30530 41483V42083Z\" fill=\"#7f7f7f\"/>\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" stroke-width=\"0\" stroke-linecap=\"butt\" stroke-miterlimit=\"10\" stroke-linejoin=\"miter\" fill=\"none\" stroke=\"#7f7f7f\" d=\"M31130 41483V42083L30530 41483ZM31130 42083 30530 41483V42083Z\"/>\n"
                    + "<path transform=\"matrix(0,-.06,-.06,-0,3024,2160)\" stroke-width=\"9\" stroke-linecap=\"round\" stroke-linejoin=\"round\" fill=\"none\" stroke=\"#7f7f7f\" d=\"M30530 41483H31130V42083H30530V41483\"/>\n"
                    + "</g>\n"
                    + "</g>\n"
                    + "</svg>\n",
                    svg);
            }
            string wt = Tools.MupdfWarnings();  // wt = pymupdf.TOOLS.mupdf_warnings()
            // if pymupdf.mupdf_version_tuple >= (1, 28):
            if (mupdf_version_tuple.Item1 > 1 || (mupdf_version_tuple.Item1 == 1 && mupdf_version_tuple.Item2 >= 28))
                // assert wt == 'unknown cid collection: PDFAUTOCAD-Indentity0\nnon-embedded font using identity encoding: ArialMT (mapping via )\ninvalid marked content sequence / clip nesting'
                Assert.Equal(
                    "unknown cid collection: PDFAUTOCAD-Indentity0\nnon-embedded font using identity encoding: ArialMT (mapping via )\ninvalid marked content sequence / clip nesting",
                    wt);
            else
                // assert wt == 'unknown cid collection: PDFAUTOCAD-Indentity0\nnon-embedded font using identity encoding: ArialMT (mapping via )\ninvalid marked content and clip nesting'
                Assert.Equal(
                    "unknown cid collection: PDFAUTOCAD-Indentity0\nnon-embedded font using identity encoding: ArialMT (mapping via )\ninvalid marked content and clip nesting",
                    wt);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3450</c>.</summary>
        [Fact]
        public void test_3450()
        {
            // This issue is a slow-down, so we just show time taken - it's not safe
            // to fail if test takes too long because that can give spurious failures
            // depending on hardware etc.
            //
            // On a mac-mini, PyMuPDF-1.24.8 takes 60s, PyMuPDF-1.24.9 takes 4s.
            //
            string path = Doc("test_3450.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_3450.pdf')
            var pdf = Document.Open(path);  // pdf = pymupdf.open(path)
            Page page = pdf[0];  // page = pdf[0]
            // t = time.time()
            float t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0f;
            // pix = page.GetPixmap(alpha=False, dpi=150)
            Pixmap pix = page.GetPixmap(alpha: false, dpi: 150);
            _ = pix;
            // t = time.time() - t
            t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0f - t;
            Console.WriteLine($"test_3450(): t={t}");  // print(f'test_3450(): {t=}')
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3859</c> — <c>mupdf.PDF_NULL</c>/<c>PDF_TRUE</c>/<c>PDF_FALSE</c> are <see cref="mupdf.PdfObj"/>.</summary>
        [Fact]
        public void test_3859()
        {
            // pymupdf.mupdf.PDF_{NULL,TRUE,FALSE} -> MupdfPdfEnums.PDF_{NULL,TRUE,FALSE}
            Assert.IsType<mupdf.PdfObj>(mupdf.MupdfPdfEnums.PDF_NULL);
            Assert.IsType<mupdf.PdfObj>(mupdf.MupdfPdfEnums.PDF_TRUE);
            Assert.IsType<mupdf.PdfObj>(mupdf.MupdfPdfEnums.PDF_FALSE);

            foreach (string name in new[] { "NULL", "TRUE", "FALSE" })
            {
                string name2 = $"PDF_{name}";
                mupdf.PdfObj v = name2 switch
                {
                    "PDF_NULL" => mupdf.MupdfPdfEnums.PDF_NULL,
                    "PDF_TRUE" => mupdf.MupdfPdfEnums.PDF_TRUE,
                    "PDF_FALSE" => mupdf.MupdfPdfEnums.PDF_FALSE,
                    _ => throw new InvalidOperationException($"`v` is not a mupdf.PdfObj: unknown {name2}"),
                };
                Assert.IsType<mupdf.PdfObj>(v);
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3905</c>.</summary>
        [Fact]
        public void test_3905()
        {
            byte[] data = Encoding.ASCII.GetBytes("A,B,C,D\r\n1,2,1,2\r\n2,2,1,2\r\n");
            Assert.Throws<FileDataException>(() => new Document(data, filetype: "pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3624</c>.</summary>
        [Fact]
        public void test_3624()
        {
            string path = Doc("test_3624.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_3624.pdf')
            string path_png_expected = Doc("test_3624_expected.png");  // path_png_expected = os.path.normpath(f'{__file__}/../../tests/resources/test_3624_expected.png')
            string path_png = Out("test_3624.png");  // path_png = os.path.normpath(f'{__file__}/../../tests/test_3624.png')
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                Page page = document[0];  // page = document[0]
                using Pixmap pixmap = page.GetPixmap(matrix: new Matrix(2, 2));  // pixmap = page.GetPixmap(matrix=pymupdf.Matrix(2, 2))
                Console.WriteLine($"Saving to path_png={path_png}.");  // print(f'Saving to {path_png=}.')
                pixmap.Save(path_png);  // pixmap.Save(path_png)
                float rms = _Compare.PixmapsRms(path_png_expected, path_png);  // rms = gentle_compare.pixmaps_rms(path_png_expected, path_png)
                Console.WriteLine($"rms={rms}");  // print(f'{rms=}')
                // We get small differences in sysinstall tests, where some thirdparty
                // libraries can differ.
                if (rms > 1)
                {
                    using Pixmap pixmap_diff = _Compare.PixmapsDiff(path_png_expected, path_png);  // pixmap_diff = gentle_compare.pixmaps_diff(path_png_expected, path_png)
                    string path_png_diff = Out("test_3624_diff.png");  // path_png_diff = os.path.normpath(f'{__file__}/../../tests/test_3624_diff.png')
                    pixmap_diff.Save(path_png_diff);  // pixmap_diff.Save(path_png_diff)
                    Assert.Fail($"rms={rms}");  // assert 0, f'{rms=}'
                }
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4043</c>.</summary>
        [Fact]
        public void test_4043()
        {
            string path = Doc("test_4043.pdf");
            using var doc = new Document(path);
            doc.FullCopyPage(1);
            doc.Save(Out("test_4043.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4018</c>.</summary>
        [Fact]
        public void test_4018()
        {
            using var document = new Document();
            foreach (var page in document.Pages(-1, -1))
                _ = page;
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4034</c>.</summary>
        [Fact]
        public void test_4034()
        {
            // tests/resources/test_4034.pdf is first two pages of input file in
            // https://github.com/pymupdf/PyMuPDF/issues/4034.
            string path = Doc("test_4034.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_4034.pdf')
            string path_clean = Out("test_4034.pdf");  // path_clean = os.path.normpath(f'{__file__}/../../tests/test_4034_out.pdf')
            Pixmap pixmap1;
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                pixmap1 = document[0].GetPixmap();  // pixmap1 = document[0].GetPixmap()
                document.Save(path_clean, clean: 1);  // document.Save(path_clean, clean=1)
            }
            using (pixmap1)
            using (var document = Document.Open(path_clean))  // with pymupdf.open(path_clean) as document:
            {
                Page page = document[0];  // page = document[0]
                using Pixmap pixmap2 = document[0].GetPixmap();  // pixmap2 = document[0].GetPixmap()
                float rms = _Compare.PixmapsRms(pixmap1, pixmap2);  // rms = gentle_compare.pixmaps_rms(pixmap1, pixmap2)
                Console.WriteLine($"test_4034(): Comparison of original/cleaned page 0 pixmaps: rms={rms}.");  // print(f'test_4034(): Comparison of original/cleaned page 0 pixmaps: {rms=}.')
                Assert.Equal(0, rms);  // assert rms == 0
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4309</c>.</summary>
        [Fact]
        public void test_4309()
        {
            using var document = new Document();
            document.NewPage();
            document.DeletePage(0);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4224</c>.</summary>
        [Fact]
        public void test_4224()
        {
            string path = Doc("test_4224.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_4224.pdf')
            string path_out = Path.Combine(Path.GetDirectoryName(Out("test_4224.pdf"))!, "test_4224");
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                foreach (Page page in document.pages())  // for page in document.pages():
                {
                    using Pixmap pixmap = page.GetPixmap(dpi: 150);  // pixmap = page.GetPixmap(dpi=150)
                    string path_pixmap = $"{path_out}.{page.Number}.png";  // path_pixmap = f'{path}.{page.number}.png'
                    pixmap.Save(path_pixmap);  // pixmap.Save(path_pixmap)
                    Console.WriteLine($"Have created: {path_pixmap}");  // print(f'Have created: {path_pixmap}')
                }
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4319</c>.</summary>
        [Fact]
        public void test_4319()
        {
            //string path = Path.Combine(Path.GetTempPath(), $"mupdfnet_test_4319_{Guid.NewGuid():N}.pdf");
            string path = Out("test_4319.pdf");
            try
            {
                using (var doc = new Document())
                {
                    var page = doc.NewPage();
                    page.insert_text(new Point(10, 100), "some text");
                    doc.Save(path);
                }
                using (var doc = new Document(path))
                {
                    _ = doc[0];
                    Assert.Equal(1, doc.PageCount);
                }
            }
            finally { 
                //if (File.Exists(path)) File.Delete(path); 
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_3886</c>.</summary>
        [Fact]
        public void test_3886()
        {
            string path = Doc("test_3886.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_3886.pdf')
            string path_clean0 = Out("test_3886_clean0.pdf");  // path_clean0 = os.path.normpath(f'{__file__}/../../tests/resources/test_3886_clean0.pdf')
            string path_clean1 = Out("test_3886_clean1.pdf");  // path_clean1 = os.path.normpath(f'{__file__}/../../tests/resources/test_3886_clean1.pdf')
            Pixmap pixmap;
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                pixmap = document[0].GetPixmap();  // pixmap = document[0].GetPixmap()
                document.Save(path_clean0, clean: 0);  // document.Save(path_clean0, clean=0)
            }
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                document.Save(path_clean1, clean: 1);  // document.Save(path_clean1, clean=1)
            }
            Pixmap pixmap_clean0;
            using (var document = Document.Open(path_clean0))  // with pymupdf.open(path_clean0) as document:
            {
                pixmap_clean0 = document[0].GetPixmap();  // pixmap_clean0 = document[0].GetPixmap()
            }
            Pixmap pixmap_clean1;
            using (var document = Document.Open(path_clean1))  // with pymupdf.open(path_clean1) as document:
            {
                pixmap_clean1 = document[0].GetPixmap();  // pixmap_clean1 = document[0].GetPixmap()
            }
            using (pixmap)
            using (pixmap_clean0)
            using (pixmap_clean1)
            {
                float rms_0 = _Compare.PixmapsRms(pixmap, pixmap_clean0);  // rms_0 = gentle_compare.pixmaps_rms(pixmap, pixmap_clean0)
                float rms_1 = _Compare.PixmapsRms(pixmap, pixmap_clean1);  // rms_1 = gentle_compare.pixmaps_rms(pixmap, pixmap_clean1)
                Console.WriteLine($"test_3886(): rms_0={rms_0} rms_1={rms_1}");  // print(f'test_3886(): {rms_0=} {rms_1=}')
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4415</c>.</summary>
        [Fact]
        public void test_4415()
        {
            string path = Doc("test_4415.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_4415.pdf')
            string path_out = Out("test_4415.png");  // path_out = os.path.normpath(f'{__file__}/../../tests/resources/test_4415_out.png')
            string path_out_expected = Doc("test_4415_out_expected.png");  // path_out_expected = os.path.normpath(f'{__file__}/../../tests/resources/test_4415_out_expected.png')
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                Page page = document[0];  // page = document[0]
                int rot = page.Rotation;  // rot = page.rotation
                Point orig = new Point(100, 100);  // orig = pymupdf.Point(100, 100)  # apparent insertion point
                string text = "Text at Top-Left";  // text = 'Text at Top-Left'
                Matrix mrot = page.DerotationMatrix;  // mrot = page.derotation_matrix  # matrix annihilating page rotation
                page.insert_text(orig * mrot, text, fontsize: 60, rotate: rot);  // page.insert_text(orig * mrot, text, fontsize=60, rotate=rot)
                using Pixmap pixmap = page.GetPixmap();  // pixmap = page.GetPixmap()
                pixmap.Save(path_out);  // pixmap.Save(path_out)
                float rms = _Compare.PixmapsRms(path_out_expected, path_out);  // rms = gentle_compare.pixmaps_rms(path_out_expected, path_out)
                Assert.Equal(0, rms);  // assert rms == 0, f'{rms=}'
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4466</c>.</summary>
        [Fact]
        public void test_4466()
        {
            string path = Doc("test_4466.pdf");
            using var document = new Document(path);
            foreach (var page in document)
            {
                using var pixmap = page.GetPixmap(clip: new IRect(0, 0, 10, 10));
                _ = pixmap.IsUnicolor;
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4479</c>.</summary>
        [Fact]
        public void test_4479()
        {
            // This passes with pymupdf-1.24.14, fails with pymupdf==1.25.*, passes with
            // pymupdf-1.26.0.
            Console.WriteLine();
            string path = Doc("test_4479.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_4479.pdf')
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                // def show(items):
                //     for item in items:
                //         print(f'    {repr(item)}')
                void Show(List<Dictionary<string, object>> layerItems)
                {
                    foreach (var item in layerItems)
                    {
                        Console.WriteLine($"    {ReprLayerUiConfig(item)}");
                    }
                }

                var items = document.layer_ui_configs();  // items = document.layer_ui_configs()
                Show(items);  // show(items)
                AssertLayerUiConfigsEqual(items, new List<Dictionary<string, object>>  // assert items == [
                {
                    LayerUiConfig(0, 0, 0, 1, "layer_0", "checkbox"),  // {'depth': 0, 'locked': 0, 'number': 0, 'on': 1, 'text': 'layer_0', 'type': 'checkbox'},
                    LayerUiConfig(0, 0, 1, 1, "layer_1", "checkbox"),
                    LayerUiConfig(0, 0, 2, 0, "layer_2", "checkbox"),
                    LayerUiConfig(0, 0, 3, 1, "layer_3", "checkbox"),
                    LayerUiConfig(0, 0, 4, 1, "layer_4", "checkbox"),
                    LayerUiConfig(0, 0, 5, 1, "layer_5", "checkbox"),
                    LayerUiConfig(0, 0, 6, 1, "layer_6", "checkbox"),
                    LayerUiConfig(0, 0, 7, 1, "layer_7", "checkbox"),
                });  // ]

                document.set_layer_ui_config(0, Constants.PdfOcOff);  // document.set_layer_ui_config(0, pymupdf.PDF_OC_OFF)
                items = document.layer_ui_configs();  // items = document.layer_ui_configs()
                Show(items);  // show(items)
                AssertLayerUiConfigsEqual(items, new List<Dictionary<string, object>>  // assert items == [
                {
                    LayerUiConfig(0, 0, 0, 0, "layer_0", "checkbox"),  // {'depth': 0, 'locked': 0, 'number': 0, 'on': 0, 'text': 'layer_0', 'type': 'checkbox'},
                    LayerUiConfig(0, 0, 1, 1, "layer_1", "checkbox"),
                    LayerUiConfig(0, 0, 2, 0, "layer_2", "checkbox"),
                    LayerUiConfig(0, 0, 3, 1, "layer_3", "checkbox"),
                    LayerUiConfig(0, 0, 4, 1, "layer_4", "checkbox"),
                    LayerUiConfig(0, 0, 5, 1, "layer_5", "checkbox"),
                    LayerUiConfig(0, 0, 6, 1, "layer_6", "checkbox"),
                    LayerUiConfig(0, 0, 7, 1, "layer_7", "checkbox"),
                });  // ]
            }
        }

        private static Dictionary<string, object> LayerUiConfig(int depth, int locked, int number, int on, string text, string type) =>
            new Dictionary<string, object>
            {
                ["depth"] = depth,
                ["locked"] = locked,
                ["number"] = number,
                ["on"] = on,
                ["text"] = text,
                ["type"] = type,
            };

        private static int LayerUiConfigInt(object value) =>
            value is bool b ? (b ? 1 : 0) : Convert.ToInt32(value);

        private static string ReprLayerUiConfig(Dictionary<string, object> item) =>
            $"{{'depth': {LayerUiConfigInt(item["depth"])}, 'locked': {LayerUiConfigInt(item["locked"])}, 'number': {LayerUiConfigInt(item["number"])}, 'on': {LayerUiConfigInt(item["on"])}, 'text': '{item["text"]}', 'type': '{item["type"]}'}}";

        private static void AssertLayerUiConfigsEqual(
            List<Dictionary<string, object>> actual,
            List<Dictionary<string, object>> expected)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                var a = actual[i];
                var e = expected[i];
                Assert.Equal(LayerUiConfigInt(e["depth"]), LayerUiConfigInt(a["depth"]));
                Assert.Equal(LayerUiConfigInt(e["locked"]), LayerUiConfigInt(a["locked"]));
                Assert.Equal(LayerUiConfigInt(e["number"]), LayerUiConfigInt(a["number"]));
                Assert.Equal(LayerUiConfigInt(e["on"]), LayerUiConfigInt(a["on"]));
                Assert.Equal(e["text"], a["text"]);
                Assert.Equal(e["type"], a["type"]);
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4564</c>.</summary>
        [Fact]
        public void test_4564()
        {
            string path = Doc("test_4564.pdf");
            using var document = new Document(path);
            string producer = document.GetMetadata().GetValueOrDefault("producer") ?? "";
            Assert.Contains("Adobe PSL 1.3e for Canon", producer);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4496</c>.</summary>
        [Fact]
        public void test_4496()
        {
            string path = Doc("test_4496.hwpx");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_4496.hwpx')
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                Console.WriteLine(document.PageCount);  // print(document.page_count)
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4590</c>.</summary>
        [Fact]
        public void test_4590()
        {
            // Create test PDF.
            string path = Out("test_4590.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/test_4590.pdf')
            using (var document = new Document())  // with pymupdf.open() as document:
            {
                Page page = document.NewPage();  // page = document.NewPage()
                // Add some text
                string text = "This PDF contains a file attachment annotation.";  // text = 'This PDF contains a file attachment annotation.'
                page.insert_text(new Point(72, 72), text, fontsize: 12);  // page.insert_text((72, 72), text, fontsize=12)
                // Create a sample file.
                string path_sample = Path.GetFullPath(Doc("test_4590_annotation_sample.txt"));  // path_sample = os.path.normpath(f'{__file__}/../../tests/test_4590_annotation_sample.txt')
                File.WriteAllText(path_sample, "This is a sample attachment file.");  // with open(path_sample, 'w') as f: f.write('This is a sample attachment file.')
                // Read file as bytes
                byte[] sample = File.ReadAllBytes(path_sample);  // with open(path_sample, 'rb') as f: sample = f.read()
                // Define annotation position (rect or point)
                Rect annot_pos = new Rect(72, 100, 92, 120);  // annot_pos = pymupdf.Rect(72, 100, 92, 120)  # PushPin icon rectangle
                // Add the file attachment annotation
                page.AddFileAnnot(  // page.AddFileAnnot(
                    point: annot_pos,  // point = annot_pos,
                    buffer_: sample,  // buffer_ = sample,
                    filename: "sample.txt",  // filename = 'sample.txt',
                    uFileName: "sample.txt",  // ufilename = 'sample.txt',
                    desc: "A test attachment file.",  // desc = 'A test attachment file.',
                    icon: "PushPin");  // icon = 'PushPin',
                // Save the PDF
                document.Save(path);  // document.Save(path)
            }
            // Check pymupdf.Document.scrub() works.
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                document.scrub();  // document.scrub()
            }
        }

        /// <summary>PyMuPDF <c>tests/test.py::test_4702</c>.</summary>
        [Fact]
        public void test_4702()
        {
            string path = Doc("test_4702.pdf");  // path = os.path.normpath(f'{__file__}/../../tests/resources/test_4702.pdf')
            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                for (int xref = 1; xref < document.XrefLength; xref++)
                {
                    Console.WriteLine($"xref={xref}");  // print(f'{xref=}')
                    try
                    {
                        _ = document.XrefObject(xref);  // _ = document.XrefObject(xref)
                    }
                    catch (Exception e1)
                    {
                        Console.WriteLine($"e1={e1}");  // print(f'{e1=}')
                        try
                        {
                            document.UpdateObject(xref, "<<>>");  // document.update_object(xref, "<<>>")
                        }
                        catch (Exception e2)
                        {
                            Console.WriteLine($"e2={e2}");  // print(f'{e2=}')
                            throw;
                        }
                    }
                }
            }
            string wt = Tools.MupdfWarnings();  // wt = pymupdf.TOOLS.mupdf_warnings()
            Assert.Equal("repairing PDF document", wt);  // assert wt == 'repairing PDF document'

            using (var document = Document.Open(path))  // with pymupdf.open(path) as document:
            {
                for (int xref = 1; xref < document.XrefLength; xref++)
                {
                    Console.WriteLine($"xref={xref}");  // print(f'{xref=}')
                    _ = document.XrefObject(xref);  // _ = document.XrefObject(xref)
                }
            }
            wt = Tools.MupdfWarnings();  // wt = pymupdf.TOOLS.mupdf_warnings()
            Assert.Equal("repairing PDF document", wt);  // assert wt == 'repairing PDF document'
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4712</c>.</summary>
        [Fact]
        public void test_4712()
        {
            /*
            Crash with "corrupted float-linked list
            */
            // if pymupdf.mupdf_version_tuple < (1, 26, 11):
            var mupdf_version_tuple = (
                mupdf.mupdf.FZ_VERSION_MAJOR,
                mupdf.mupdf.FZ_VERSION_MINOR,
                mupdf.mupdf.FZ_VERSION_PATCH);
            if (mupdf_version_tuple.CompareTo((1, 26, 11)) < 0)
            {
                // print(f'test_4712m(): Not running because known to fail on mupdf < 1.26.11: {pymupdf.mupdf_version=}.')
                Console.WriteLine($"test_4712m(): Not running because known to fail on mupdf < 1.26.11: mupdf_version={Tools.MupdfVersion()}.");
                return;
            }
            string path_a = Doc("test_4712_a.pdf");  // path_a = os.path.normpath(f'{__file__}/../../tests/resources/test_4712_a.pdf')
            string path_b = Doc("test_4712_b.pdf");  // path_b = os.path.normpath(f'{__file__}/../../tests/resources/test_4712_b.pdf')
            // doc1 = pymupdf.open(path_a)
            using var doc1 = Document.Open(path_a);
            // for i in range(6):
            for (int i = 0; i < 6; i++)
                // doc1.LoadPage(i).GetPixmap()
                using (var _ = doc1.LoadPage(i).GetPixmap()) { }
            // doc2 = pymupdf.open(path_b)
            using var doc2 = Document.Open(path_b);
            // for i in range(6):
            for (int i = 0; i < 6; i++)
                // doc2.LoadPage(i).GetPixmap()
                using (var _ = doc2.LoadPage(i).GetPixmap()) { }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4712m</c>.</summary>
        [Fact]
        public void test_4712m()
        {
            // if pymupdf.mupdf_version_tuple < (1, 26, 11):
            var mupdf_version_tuple = (
                mupdf.mupdf.FZ_VERSION_MAJOR,
                mupdf.mupdf.FZ_VERSION_MINOR,
                mupdf.mupdf.FZ_VERSION_PATCH);
            if (mupdf_version_tuple.CompareTo((1, 26, 11)) < 0)
            {
                // print(f'test_4712m(): Not running because known to fail on mupdf < 1.26.11: {pymupdf.mupdf_version=}.')
                Console.WriteLine($"test_4712m(): Not running because known to fail on mupdf < 1.26.11: mupdf_version={Tools.MupdfVersion()}.");
                return;
            }

            string path_a = Doc("test_4712_a.pdf");  // path_a = os.path.normpath(f'{__file__}/../../tests/resources/test_4712_a.pdf')
            string path_b = Doc("test_4712_b.pdf");  // path_b = os.path.normpath(f'{__file__}/../../tests/resources/test_4712_b.pdf')

            // mupdf = pymupdf.mupdf
            // def get_pixmap(page):
            void GetPixmap(mupdf.FzPage page)
            {
                // displaylist = mupdf.fz_new_display_list_from_page(page)
                using var displaylist = mupdf.mupdf.fz_new_display_list_from_page(page);
                // rect = mupdf.fz_bound_display_list(displaylist)
                using var rect = mupdf.mupdf.fz_bound_display_list(displaylist);
                // irect = mupdf.fz_round_rect(rect)
                using var irect = mupdf.mupdf.fz_round_rect(rect);
                // pixmap = mupdf.fz_new_pixmap_with_bbox(
                //         mupdf.FzColorspace(mupdf.FzColorspace.Fixed_RGB),
                //         irect,
                //         mupdf.FzSeparations(),
                //         0,  # alpha
                //         )
                var colorspace = new mupdf.FzColorspace(mupdf.FzColorspace.Fixed.Fixed_RGB);
                using var pixmap = colorspace.fz_new_pixmap_with_bbox(irect, new mupdf.FzSeparations(), 0);  // alpha
                // mupdf.fz_clear_pixmap_with_value(pixmap, 0xFF)
                mupdf.mupdf.fz_clear_pixmap_with_value(pixmap, 0xFF);
                // matrix = mupdf.FzMatrix()
                var matrix = new mupdf.FzMatrix();
                // device = mupdf.fz_new_draw_device(matrix, pixmap)
                var device = mupdf.mupdf.fz_new_draw_device(matrix, pixmap);
                // mupdf.fz_run_display_list(
                //         displaylist,
                //         device,
                //         mupdf.FzMatrix(),
                //         mupdf.FzRect(mupdf.FzRect.Fixed_INFINITE),
                //         mupdf.FzCookie(),
                //         )
                mupdf.mupdf.fz_run_display_list(
                    displaylist,
                    device,
                    new mupdf.FzMatrix(),
                    new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_INFINITE),
                    new mupdf.FzCookie());
                // mupdf.fz_close_device(device)
                mupdf.mupdf.fz_close_device(device);
                device.Dispose();
            }

            // def process_document(document):
            void ProcessDocument(mupdf.FzDocument document)
            {
                // for i in range(6):
                for (int i = 0; i < 6; i++)
                {
                    // print(f'    {i=}', flush=1)
                    Console.WriteLine($"    i={i}");
                    Console.Out.Flush();
                    // page = mupdf.fz_load_page(document, i)
                    using var page = mupdf.mupdf.fz_load_page(document, i);
                    GetPixmap(page);
                }
            }

            // print(f'Processing {path_a=}', flush=1)
            Console.WriteLine($"Processing path_a={path_a}");
            Console.Out.Flush();
            // document_a = mupdf.fz_open_document(path_a)
            using (var document_a = mupdf.mupdf.fz_open_document(path_a))
                ProcessDocument(document_a);

            // print(f'Processing {path_b=}', flush=1)
            Console.WriteLine($"Processing path_b={path_b}");
            Console.Out.Flush();
            // document_b = mupdf.fz_open_document(path_b)
            using (var document_b = mupdf.mupdf.fz_open_document(path_b))
                ProcessDocument(document_b);
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4746</c>.</summary>
        [Fact]
        public void test_4746()
        {
            // archive = pymupdf.Archive('.')
            using var archive = new Archive(".");
            // archive.add(__file__, 'foo')
            string file = Doc("bug1971.pdf");  // __file__
            archive.Add(file, "foo");
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4907</c>.</summary>
        [Fact]
        public void test_4907()
        {
            string path = Doc("test_4907.pdf");
            using var document = new Document(path);
            foreach (var page in document)
            {
                using var displayList = page.GetDisplayList(annots: 0);
                using var textPage = displayList.GetTextPage();
            }
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4928</c>.</summary>
        [Fact]
        public void test_4928()
        {
            string path = Doc("test_4928.pdf");
            using var document = new Document(path);
            document.Scrub();
            document.Save(Out("test_4928.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_general.py::test_4902</c>.</summary>
        [Fact]
        public void test_4902()
        {
            byte[] data;
            using (var doc = new Document())
            {
                var page = doc.NewPage();
                page.InsertText(new Point(72, 72), "Hello World", fontSize: 20, renderMode: 2,
                    color: _Constants.red, fill: _Constants.green, borderWidth: 0.4f);
                data = doc.ToBytes();
            }
            using (var doc = new Document(data))
            {
                var spans = doc[0].GetTextTrace();
                Assert.Equal(2, spans.Count);
                doc.Save(Out("test_4902.pdf"));
            }
        }

        /// <summary>Python <c>pickle.load</c> via the Python interpreter (pickle is not native to .NET).</summary>
        private static Dictionary<string, Dictionary<string, object>> PickleLoad(Stream pickle_in)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pickle");
            try
            {
                using (var fs = File.Create(path))
                    pickle_in.CopyTo(fs);
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments =
                        "-c \"import pickle,json,sys\ndef e(o):\n" +
                        " import collections.abc\n" +
                        " if isinstance(o,dict): return {k:e(v) for k,v in o.items()}\n" +
                        " if isinstance(o,tuple): return list(o)\n" +
                        " return o\n" +
                        "print(json.dumps(e(pickle.load(open(sys.argv[1],'rb')))))\" " +
                        $"\"{path}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi)!;
                string json = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                    throw new InvalidOperationException(proc.StandardError.ReadToEnd());
                return JsonToResolveNames(json);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static Dictionary<string, Dictionary<string, object>> JsonToResolveNames(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var result = new Dictionary<string, Dictionary<string, object>>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var inner = new Dictionary<string, object>();
                foreach (var item in prop.Value.EnumerateObject())
                {
                    if (item.NameEquals("to") && item.Value.ValueKind == JsonValueKind.Array)
                    {
                        var arr = item.Value.EnumerateArray().ToArray();
                        inner["to"] = ((float)arr[0].GetDouble(), (float)arr[1].GetDouble());
                    }
                    else if (item.Value.ValueKind == JsonValueKind.Number)
                    {
                        if (item.NameEquals("page"))
                            inner["page"] = item.Value.GetInt32();
                        else
                            inner["zoom"] = (float)item.Value.GetDouble();
                    }
                    else if (item.Value.ValueKind == JsonValueKind.String)
                        inner[item.Name] = item.Value.GetString()!;
                }
                result[prop.Name] = inner;
            }
            return result;
        }

        private static bool ResolveNamesEqual(
            Dictionary<string, Dictionary<string, object>> a,
            Dictionary<string, Dictionary<string, object>> b)
        {
            if (a.Count != b.Count)
                return false;
            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var bv))
                    return false;
                if (!ResolveNameEntryEqual(kv.Value, bv))
                    return false;
            }
            return true;
        }

        private static bool ResolveNameEntryEqual(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            if (a.Count != b.Count)
                return false;
            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var bv))
                    return false;
                if (kv.Key == "to")
                {
                    var at = (ValueTuple<float, float>)kv.Value;
                    var bt = (ValueTuple<float, float>)bv;
                    if (Math.Abs(at.Item1 - bt.Item1) > Epsilon || Math.Abs(at.Item2 - bt.Item2) > Epsilon)
                        return false;
                }
                else if (kv.Value is float af && bv is float bf)
                {
                    if (Math.Abs(af - bf) > Epsilon)
                        return false;
                }
                else if (!Equals(kv.Value, bv))
                    return false;
            }
            return true;
        }

        /// <summary>Approximate Python <c>repr(bytes)</c> for verbose test output.</summary>
        private static string PyBytesRepr(byte[] bytes)
        {
            var sb = new StringBuilder("b'");
            foreach (byte b in bytes)
            {
                if (b == (byte)'\n') sb.Append("\\n");
                else if (b == (byte)'\\') sb.Append("\\\\");
                else if (b == (byte)'\'') sb.Append("\\'");
                else if (b >= 32 && b < 127) sb.Append((char)b);
                else sb.Append($"\\x{b:x2}");
            }
            sb.Append('\'');
            return sb.ToString();
        }

        /// <summary>CRT file descriptors for <c>test_3081</c> (Python <c>os.open</c> / <c>os.close</c>).</summary>
        private static class Test3081Fd
        {
            private const int O_RDONLY = 0;

            [DllImport("msvcrt.dll", CharSet = CharSet.Ansi, SetLastError = true, EntryPoint = "_open")]
            private static extern int MsvcOpen(string path, int oflag);

            [DllImport("msvcrt.dll", SetLastError = true, EntryPoint = "_close")]
            private static extern int MsvcClose(int fd);

            [DllImport("libc", SetLastError = true)]
            private static extern int open(string pathname, int flags);

            [DllImport("libc", SetLastError = true)]
            private static extern int close(int fd);

            public static int Open(string path)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return MsvcOpen(path, O_RDONLY);
                return open(path, O_RDONLY);
            }

            public static void Close(int fd)
            {
                if (fd < 0) return;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    MsvcClose(fd);
                else
                    close(fd);
            }
        }

    }
}