using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Font class.
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestFont/</c>; outputs: <c>TestDocuments/_Output/TestFont/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestFont
    {
        private const float Epsilon = 1e-5f;
        private const string TestClassName = nameof(TestFont);

        // Mirrors pyg_fz_install_load_system_font_funcs_args: the SWIG
        // director must stay alive after fz_install_load_system_font_funcs2().
        static SystemFontFontTrace _installedSystemFontTrace;

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string OptionalDocPath(string fileName) =>
            Path.Combine(_Path.ResolveSolutionRoot(), "TestDocuments", "MuPDF.NET.Test", TestClassName, fileName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary>Regression test: font1.</summary>
        [Fact]
        public void test_font1()
        {
            const string text = "PyMuPDF";
            using var font = new Font("helv");
            Assert.Equal("Helvetica", font.Name);
            float tl = font.TextLength(text, fontSize: 20);
            float[] cl = font.CharLengths(text, fontSize: 20);
            Assert.Equal(text.Length, cl.Length);
            Assert.True(Math.Abs(cl.Sum() - tl) < Epsilon);
            for (int i = 0; i < cl.Length; i++)
            {
                Assert.True(_Tools.IsClose(cl[i], font.GlyphAdvance(text[i]) * 20));
            }

            byte[] buffer = font.Buffer;
            Assert.NotNull(buffer);
            using var font2 = new Font(fontBuffer: buffer);
            List<int> codepoints1 = font.ValidCodepoints();
            List<int> codepoints2 = font2.ValidCodepoints();
            Assert.Equal(codepoints1, codepoints2);

            Rect bbox1 = font.BBox;
            var fzBbox = mupdf.mupdf.fz_font_bbox(font.NativeFont);
            var bbox2 = new Rect(fzBbox.x0, fzBbox.y0, fzBbox.x1, fzBbox.y1);
            Assert.Equal(bbox1, bbox2);
        }

        /// <summary>Regression test: font2.</summary>
        [Fact]
        public void test_font2()
        {
            using var font = new Font("helv");
            const string text = "PyMuPDF";
            Assert.Equal(font.TextLength(text), Utils.GetTextLength(text));
        }

        /// <summary>Regression test: fontarchive.</summary>
        [Fact]
        public void test_fontarchive()
        {
            var arch = new Archive();
            string css = Utils.CssForPymupdfFont("notos", archive: arch, name: "sans-serif");
            Console.WriteLine(css);
            foreach (var entry in arch.EntryList)
                Console.WriteLine($"fmt={entry.Fmt} entries={string.Join(", ", entry.Entries)} path={entry.Path}");
            Assert.Single(arch.EntryList);
            var sub = arch.EntryList[0];
            Assert.Equal("tree", sub.Fmt);
            Assert.Equal(
                new[] { "notosbo", "notosbi", "notosit", "notos" }.OrderBy(x => x),
                sub.Entries.OrderBy(x => x));
            Assert.Null(sub.Path);
        }

        /// <summary>Regression test: load system font.</summary>
        [Fact]
        public void test_load_system_font()
        {
            // trace = list()
            var trace = new SystemFontFontTrace();
            trace.EnableVirtualCallbacks();
            trace.fz_install_load_system_font_funcs2();
            _installedSystemFontTrace = trace;
            // f = pyfz_load_system_font("some-font-name", 0, 0, 0)
            var f = mupdf.mupdf.fz_load_system_font("some-font-name", 0, 0, 0);
            Assert.Equal(
                new List<(string, int, int, int)> { ("some-font-name", 0, 0, 0) },
                trace.FontTrace);
            Console.WriteLine($"test_load_system_font(): f.m_internal={f.m_internal}");
            f.Dispose();
        }

        /// <summary>Regression test: fontname.</summary>
        [Fact]
        public void test_fontname()
        {
            using var doc = new Document();
            Page page = doc.NewPage();
            int xref = page.InsertFont();
            Assert.True(xref > 0);

            bool detected = false;
            try
            {
                page.InsertFont(fontName: "illegal/char", fontFile: "unimportant");
            }
            catch (ValueErrorException ex) when (ex.Message.StartsWith("bad fontname chars", StringComparison.Ordinal))
            {
                detected = true;
            }
            Assert.True(detected);
            doc.Save(Out("test_fontname.pdf"));
        }

        /// <summary>Regression test: 2608.</summary>
        [Fact]
        public void test_2608()
        {
            int flags = mupdf.mupdf.FZ_STEXT_DEHYPHENATE | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP;
            using var doc = new Document(Doc("2201.00069.pdf"));
            var page = doc[0];
            var blocks = page.GetTextBlocks(flags: flags);
            string text = blocks[10].text;
            text = text.Replace("\r", "");

            var (major, minor, _) = _Version.mupdf_version_tuple();
            string expectedPath = "";
            if (major > 1 || (major == 1 && minor >= 28))
                expectedPath = Doc("test_2608_expected");
            else if (major > 1 || (major == 1 && minor == 27))
                expectedPath = Doc("test_2608_expected_1.27");
            else
                Doc("test_2608_expected_1.26");
            string expected = File.ReadAllText(expectedPath);
            expected = expected.Replace("\r", "");
            Assert.Equal(expected, text);
            doc.Save(Out("test_2608.pdf"));
        }

        /// <summary>Regression test: mupdf subset fonts2.</summary>
        [Fact]
        public void test_mupdf_subset_fonts2()
        {
            using var doc = new Document(Doc("2.pdf"));
            doc.SubsetFonts();
            doc.Save(Out("test_mupdf_subset_fonts2.pdf"));
        }

        /// <summary>Regression test: 3677.</summary>
        [Fact]
        public void test_3677()
        {
            Helpers.SubsetFontnames = true;
            try
            {
                var fontNamesExpected = new List<string>
                {
                    "BCDEEE+Aptos",
                    "BCDFEE+Aptos",
                    "BCDGEE+Calibri-Light",
                    "BCDHEE+Calibri-Light",
                };
                var fontNames = new List<string>(); // font_names = list()
                using (var document = new Document(Doc("test_3677.pdf")))
                {
                    foreach (Page page in document) // for page in document:
                    {
                        var d = (Dictionary<string, object>)page.GetText("dict"); // page.GetText('dict')
                        foreach (var block in (List<Dictionary<string, object>>)d["blocks"]) // for block in page.GetText('dict')['blocks']:
                        {
                            if (Convert.ToInt32(block["type"]) == 0) // if block['type'] == 0:
                            {
                                if (block.ContainsKey("lines")) // if 'lines' in block.keys():
                                {
                                    foreach (var line in (List<Dictionary<string, object>>)block["lines"]) // for line in block['lines']:
                                    {
                                        foreach (var span in (List<Dictionary<string, object>>)line["spans"]) // for span in line['spans']:
                                        {
                                            string fontName = (string)span["font"]; // font_name=span['font']
                                            fontNames.Add(fontName); // font_names.Append(font_name)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Assert.Equal(fontNamesExpected, fontNames); // assert font_names == font_names_expected
            }
            finally
            {
                Helpers.SubsetFontnames = false;
            }
        }

        /// <summary>Regression test: 3933.</summary>
        [Fact]
        public void test_3933()
        {
            var expected = new Dictionary<string, int>
            {
                ["BCDEEE+Calibri"] = 39,
                ["BCDFEE+SwissReSan-Regu"] = 53,
                ["BCDGEE+SwissReSan-Ital"] = 20,
                ["BCDHEE+SwissReSan-Bold"] = 20,
                ["BCDIEE+SwissReSan-Regu"] = 53,
                ["BCDJEE+Calibri"] = 39,
            };

            using var document = new Document(Doc("test_3933.pdf"));
            var page = document[0];
            foreach (var (xref, _, _, name, _, _, _) in page.GetFonts())
            {
                var (_, _, _, content) = document.ExtractFont(xref);
                if (content == null || content.Length == 0)
                    continue;
                using var font = new Font(fontName: name, fontBuffer: content);
                List<int> supported = font.ValidCodepoints();
                Assert.True(expected.TryGetValue(name, out int expCount), $"unexpected font {name}");
                Assert.Equal(expCount, supported.Count);
            }
        }

        /// <summary>smoke.</summary>
        [Fact]
        public void test_3780()
        {
            using var document = new Document(Doc("test_3780.pdf"));
            int pageI = 0;
            foreach (Page page in document)
            {
                foreach (var itm in page.GetFonts())
                {
                    var (_, _, _, buff) = document.ExtractFont(itm.xref);
                    if (buff == null || buff.Length == 0)
                        continue;
                    using var font = new Font(fontBuffer: buff);
                    _ = font.Name;
                    _ = font.Ascender;
                    _ = font.Descender;
                }
                if (pageI == 0)
                    _ = page.GetText("dict");
                pageI++;
            }
        }

        /// <summary>same as <c>tests/test_font.py::test_3887</c>.</summary>
        [Fact]
        public void test_3887()
        {
            string path = Doc("test_3887.pdf");
            string path2 = Out("test_3887.ez.pdf");
            const string text = "\u0391\u3001\u0392\u3001\u0393\u3001\u0394\u3001\u0395\u3001\u0396\u3001\u0397\u3001\u0398\u3001\u0399\u3001\u039a\u3001\u039b\u3001\u039c\u3001\u039d\u3001\u039e\u3001\u039f\u3001\u03a0\u3001\u03a1\u3001\u03a3\u3001\u03a4\u3001\u03a5\u3001\u03a6\u3001\u03a7\u3001\u03a8\u3001\u03a9\u3002\u03b1\u3001\u03b2\u3001\u03b3\u3001\u03b4\u3001\u03b5\u3001\u03b6\u3001\u03b7\u3001\u03b8\u3001\u03b9\u3001\u03ba\u3001\u03bb\u3001\u03bc\u3001\u03bd\u3001\u03be\u3001\u03bf\u3001\u03c0\u3001\u03c1\u3001\u03c2\u3001\u03c4\u3001\u03c5\u3001\u03c6\u3001\u03c7\u3001\u03c8\u3001\u03c9\u3002";
            try
            {
                using (var document = new Document(path))
                {
                    document.SubsetFonts(fallback: false);
                    document.EzSave(path2);
                }

                using (var document = new Document(path2))
                {
                    Page page = document[0];
                    var rawdict = (Dictionary<string, object>)page.GetText("rawdict", flags: 0);
                    // chars = [c for b in page.GetText("rawdict",flags=0)["blocks"] for l in b["lines"] for s in l["spans"] for c in s["chars"]]
                    var chars = ((List<Dictionary<string, object>>)rawdict["blocks"])
                        .SelectMany(b => (List<Dictionary<string, object>>)b["lines"])
                        .SelectMany(l => (List<Dictionary<string, object>>)l["spans"])
                        .SelectMany(s => (List<Dictionary<string, object>>)s["chars"])
                        .ToList();
                    // output = [c["c"] for c in chars]
                    var output = chars.Select(c => (string)c["c"]).ToList();
                    Assert.Equal(new HashSet<char>(text), new HashSet<char>(string.Concat(output)));

                    using Pixmap pixmap = page.GetPixmap();
                    pixmap.Save(Out("test_3887.png"));
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(path2))
                        File.Delete(path2);
                }
                catch (IOException)
                {
                    // path2 may still be open briefly on Windows
                }
            }
        }

        /// <summary>Regression test: 4457.</summary>
        [Fact]
        public void test_4457()
        {
            string pathA = OptionalDocPath("test_4457_a.pdf");
            if (!File.Exists(pathA))
            {
                Console.WriteLine(
                    "test_4457(): skipping because test_4457_a.pdf not present "
                    + "(download from https://github.com/user-attachments/files/20862923/test_4457_a.pdf).");
                return;
            }

            var files = new (string path, int rmsOldAfterMax)[]
            {
                (pathA, 4),
                (OptionalDocPath("test_4457_b.pdf"), 9),
            };

            Tools.ResetMupdfWarnings();
            foreach (var (path, rmsOldAfterMax) in files)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"test_4457(): skipping missing file: {path}");
                    continue;
                }

                string text;
                Pixmap pixmap;
                string baseName = Path.GetFileNameWithoutExtension(path);
                string pathBefore = Out($"{baseName}.before.pdf");
                string pathAfter = Out($"{baseName}.after.pdf");
                using (var document = new Document(path))
                {
                    var page = document[0];
                    pixmap = page.GetPixmap();
                    pixmap.Save(Out($"{baseName}.png"));
                    text = (string)page.GetText();
                    document.EzSave(pathBefore, garbage: 4);
                    document.SubsetFonts();
                    document.EzSave(pathAfter, garbage: 4);
                }

                string textBefore;
                Pixmap pixmapBefore;
                using (var document = new Document(pathBefore))
                {
                    textBefore = (string)document[0].GetText();
                    pixmapBefore = document[0].GetPixmap();
                    pixmapBefore.Save(Out($"{baseName}.before.png"));
                }

                Pixmap pixmapAfter;
                using (var document = new Document(pathAfter))
                {
                    _ = (string)document[0].GetText();
                    pixmapAfter = document[0].GetPixmap();
                    pixmapAfter.Save(Out($"{baseName}.after.png"));
                }

                float rmsBefore = _Compare.PixmapsRms(pixmap, pixmapBefore);
                float rmsAfter = _Compare.PixmapsRms(pixmap, pixmapAfter);
                Console.WriteLine($"rms_before={rmsBefore}");
                Console.WriteLine($"rms_after={rmsAfter}");

                using var pixmapAfterDiff = _Compare.PixmapsDiff(pixmap, pixmapAfter);
                pixmapAfterDiff.Save(Out($"{baseName}.after.diff.png"));

                Assert.Equal(text, textBefore);
                Assert.Equal(0, rmsBefore);

                if (_Version.mupdf_version_tuple_at_least(1, 26, 6))
                    Assert.Equal(0, rmsAfter);
                else
                    Assert.True(Math.Abs(rmsAfter - rmsOldAfterMax) < 2);
            }

            string wt = Tools.MupdfWarnings();
            Console.WriteLine($"wt={wt}");
            if (!_Version.mupdf_version_tuple_at_least(1, 27, 0))
            {
                Assert.Equal(
                    "bogus font ascent/descent values (0 / 0)\n... repeated 5 times...",
                    wt);
            }
        }

        /// <summary>Records <c>fz_install_load_system_font_funcs</c> callback invocations for <see cref="test_load_system_font"/>.</summary>
        private sealed class SystemFontFontTrace : mupdf.FzInstallLoadSystemFontFuncsArgs2
        {
            public List<(string name, int bold, int italic, int needsExactMetrics)> FontTrace { get; } = new();

            public List<(string name, int ordering, int serif)> CjkTrace { get; } = new();

            public List<(int script, int language, int serif, int bold, int italic)> FallbackTrace { get; } = new();

            public void EnableVirtualCallbacks()
            {
                use_virtual_f();
                use_virtual_f_cjk();
                use_virtual_f_fallback();
            }

            public override mupdf.fz_font f(
                mupdf.fz_context arg_0,
                string name,
                int bold,
                int italic,
                int needs_exact_metrics)
            {
                // trace.append((name, bold, italic, needs_exact_metrics))
                FontTrace.Add((name, bold, italic, needs_exact_metrics));
                return null;
            }

            public override mupdf.fz_font f_cjk(mupdf.fz_context arg_0, string name, int ordering, int serif)
            {
                // trace.append((name, ordering, serif))
                CjkTrace.Add((name, ordering, serif));
                return null;
            }

            public override mupdf.fz_font f_fallback(
                mupdf.fz_context arg_0,
                int script,
                int language,
                int serif,
                int bold,
                int italic)
            {
                // trace.append((script, language, serif, bold, italic))
                FallbackTrace.Add((script, language, serif, bold, italic));
                return null;
            }
        }
    }
}