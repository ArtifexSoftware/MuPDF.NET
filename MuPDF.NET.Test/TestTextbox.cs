// Fill a given text in a rectangle on some PDF page using
// 1. TextWriter object
// 2. Basic text output
// Check text is indeed contained in given rectangle.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestTextbox/</c>; outputs: <c>TestDocuments/_Output/TestTextbox/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestTextbox
    {
        private const string TestClassName = nameof(TestTextbox);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static string Resource(string name) => Doc(name);

        private static string TestsPath(string name) => Out(name);

        private static string Dedent(string text)
        {
            var lines = text.Replace("\r\n", "\n").Trim('\n').Split('\n');
            if (lines.Length == 0)
                return "";
            int minIndent = int.MaxValue;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                int indent = line.Length - line.TrimStart(' ').Length;
                if (indent < minIndent)
                    minIndent = indent;
            }
            if (minIndent == int.MaxValue)
                minIndent = 0;
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    sb.Append('\n');
                var line = lines[i];
                if (line.Length >= minIndent)
                    sb.Append(line.Substring(minIndent));
                else
                    sb.Append(line);
            }
            return sb.ToString();
        }

        private static string Indent(string text, string prefix)
        {
            return string.Join("\n", text.Replace("\r\n", "\n").Split('\n').Select(line => prefix + line));
        }

        private static (float r, float g, float b) SRgbToPdf(int srgb) =>
            (((srgb >> 16) & 255) / 255.0f, ((srgb >> 8) & 255) / 255.0f, (srgb & 255) / 255.0f);

        // codespell:ignore-begin
        // Er ähnelt dem Orca in Form und Proportionen, ist aber einfarbig schwarz und mit einer Maximallänge von etwa sechs Metern deutlich kleiner.
        // Kleine Schwertwale bilden Schulen von durchschnittlich zehn bis fünfzig Tieren, wobei sie sich auch mit anderen Delfinen vergesellschaften und sich meistens abseits der Küsten aufhalten.
        // Sie sind in allen Ozeanen gemäßigter, subtropischer und tropischer Breiten beheimatet, sind jedoch vor allem in wärmeren Jahreszeiten auch bis in die gemäßigte bis subpolare Zone südlich der Südspitze Südamerikas, vor Nordeuropa und bis vor Kanada anzutreffen."""
        private const string text =
            "Der Kleine Schwertwal (Pseudorca crassidens), auch bekannt als Unechter oder Schwarzer Schwertwal, ist eine Art der Delfine (Delphinidae) und der einzige rezente Vertreter der Gattung Pseudorca.\n\n" +
            "Er ähnelt dem Orca in Form und Proportionen, ist aber einfarbig schwarz und mit einer Maximallänge von etwa sechs Metern deutlich kleiner.\n\n" +
            "Kleine Schwertwale bilden Schulen von durchschnittlich zehn bis fünfzig Tieren, wobei sie sich auch mit anderen Delfinen vergesellschaften und sich meistens abseits der Küsten aufhalten.\n\n" +
            "Sie sind in allen Ozeanen gemäßigter, subtropischer und tropischer Breiten beheimatet, sind jedoch vor allem in wärmeren Jahreszeiten auch bis in die gemäßigte bis subpolare Zone südlich der Südspitze Südamerikas, vor Nordeuropa und bis vor Kanada anzutreffen.";
        // codespell:ignore-end

        [Fact]
        public void test_textbox1()
        {
            using var doc = new Document();

            var page = doc.NewPage();
            var rect = new Rect(50, 50, 400, 400);
            var tw = new TextWriter(page.Rect, color: _Constants.blue);
            // rect,
            // text,
            tw.FillTextbox(
                rect,
                text,
                align: Constants.TextAlignLeft,
                fontSize: 12);
            tw.WriteText(page, morphFix: rect.TopLeft, morphMat: new Matrix(1, 1));
            // check text containment
            Assert.Equal(page.GetText(), page.GetText("text", clip: rect.IRect));
            page.WriteText(writers: new[] { tw });
            doc.Save(Out("test_textbox1.pdf"));
        }

        [Fact]
        public void test_textbox2()
        {
            using var doc = new Document();
            int ocg = doc.AddOcg("ocg1");

            var page = doc.NewPage();
            var rect = new Rect(50, 50, 400, 400);
            var blueRgb = Utils.GetColor("lightblue");
            float[] blue = { blueRgb.r, blueRgb.g, blueRgb.b };
            _ = Utils.GetColorHSV("red");
            // rect,
            // text,
            page.InsertTextbox(
                rect,
                text,
                align: Constants.TextAlignLeft,
                fontSize: 12,
                color: blue,
                oc: ocg);
            // check text containment
            Assert.Equal(page.GetText(), page.GetText("text", clip: rect.IRect));
            doc.Save(Out("test_textbox2.pdf"));
        }

        [Fact]
        public void test_textbox3()
        {
            using var doc = new Document();

            var page = doc.NewPage();
            var rect = new Rect(50, 50, 400, 400);
            var tw = new TextWriter(page.Rect, color: _Constants.blue);
            // tw.fill_textbox(
            // rect,
            // text,
            tw.FillTextbox(
                rect,
                text,
                align: Constants.TextAlignLeft,
                fontSize: 12,
                rightToLeft: true);
            tw.WriteText(page, morphFix: rect.TopLeft, morphMat: new Matrix(1, 1));
            // check text containment
            Assert.Equal(page.GetText(), page.GetText("text", clip: rect.IRect));
            doc.Scrub();
            doc.SubsetFonts();
            doc.Save(Out("test_textbox3.pdf"));
        }

        [Fact]
        public void test_textbox4()
        {
            using var doc = new Document();
            int ocg = doc.AddOcg("ocg1");

            var page = doc.NewPage();
            var rect = new Rect(50, 50, 400, 600);
            var tw = new TextWriter(page.Rect, color: _Constants.blue);
            // rect,
            // text,
            tw.FillTextbox(
                rect,
                text,
                align: Constants.TextAlignLeft,
                fontSize: 12,
                font: new Font("cour"),
                rightToLeft: true);
            tw.WriteText(page, oc: ocg, morphFix: rect.TopLeft, morphMat: new Matrix(1, 1));
            // check text containment
            Assert.Equal(page.GetText(), page.GetText("text", clip: rect.IRect));
            doc.Save(Out("test_textbox4.pdf"));
        }

        [Fact]
        public void test_textbox5()
        {
            bool small_glyph_heights0 = Tools.SetSmallGlyphHeights();
            Tools.SetSmallGlyphHeights(true);
            try
            {
                using var doc = new Document();

                var page = doc.NewPage();
                var r = new Rect(100, 100, 150, 150);
                string text5 = "words and words and words and more words...";
                int rc = -1;
                float fontsize = 12;
                page.DrawRect(r);
                // while rc <0:
                while (rc < 0)
                {
                    // r,
                    // text,
                    (rc, _) = page.InsertTextbox(
                        r,
                        text5,
                        fontSize: fontsize,
                        align: Constants.TextAlignJustify);
                    fontsize -= 0.5f;
                }


                var blocks = (List<TextBlock>)page.GetText("blocks");

                var bbox = new Rect(blocks[0].X0, blocks[0].Y0, blocks[0].X1, blocks[0].Y1);
                // Need small allowance if layout is imported.
                Assert.True(
                    (r + new Rect(-0.01f, -0.01f, 0.01f, 0.01f)).Contains(bbox),
                    $"bbox={bbox} r={r}");
                doc.Save(Out("test_textbox5.pdf"));
            }
            finally
            {
                // Must restore small_glyph_heights, otherwise other tests can fail.
                Tools.SetSmallGlyphHeights(small_glyph_heights0);
            }
        }

        [Fact]
        public void test_2637()
        {
            using var doc = new Document();

            var page = doc.NewPage();
            string text2637 =
                "The morning sun painted the sky with hues of orange and pink. " +
                "Birds chirped harmoniously, greeting the new day. " +
                "Nature awakened, filling the air with life and promise.";
            var rect = new Rect(50, 50, 500, 280);
            float fontsize = 50;
            int rc = -1;
            // while rc <0: # look for largest font size that makes the text fit
            while (rc < 0)
            {
                (rc, _) = page.InsertTextbox(rect, text2637, fontName: "hebo", fontSize: fontsize);
                fontsize -= 1;
            }

            // confirm text won't lap outside rect

            var blocks = page.GetTextBlocks();
            var bbox = new Rect(blocks[0].x0, blocks[0].y0, blocks[0].x1, blocks[0].y1);
            Assert.True(rect.Contains(bbox));
            doc.Save(Out("test_2637.pdf"));
        }

        [Fact]
        public void test_htmlbox1()
        {
            // The text is styled and contains a link.
            // Then extract the text again, and
            // We try to insert into a rectangle that is too small, setting
            // return

            var rect = _Constants.rect;

            string base_text =
                "Lorem ipsum dolor sit amet, consectetur adipisici elit, sed eiusmod tempor incidunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquid ex ea commodi consequat. Quis aute iure reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint obcaecat cupiditat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

            string html_text =
                "Lorem ipsum dolor sit amet, consectetur adipisici elit, sed eiusmod tempor incidunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation <b>ullamco</b> <i>laboris</i> nisi ut aliquid ex ea commodi consequat. Quis aute iure reprehenderit in <span style=\"color: #0f0;font-weight:bold;\">voluptate</span> velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint obcaecat cupiditat non proident, sunt in culpa qui <a href=\"https://www.artifex.com\">officia</a> deserunt mollit anim id est laborum.";

            using var doc = new Document();

            foreach (var rot in new[] { 0, 90, 180, 270 })
            {
                (float, float)[] wdirs = { (1, 0), (0, -1), (-1, 0), (0, 1) };

                var page = doc.NewPage();
                (float spare_height, float scale) = page.InsertHtmlbox(rect, html_text, css: null, scaleLow: 1, archive: null, rotate: rot);
                Assert.True(spare_height < 0);
                Assert.Equal(1, scale);
                (spare_height, scale) = page.InsertHtmlbox(rect, html_text, css: null, scaleLow: 0, archive: null, rotate: rot);
                page.DrawRect(rect, _Constants.red);
                doc.Save(Out("test_htmlbox1.pdf"));
                Assert.True(Math.Abs(spare_height - 3.8507) < 0.001);
                Assert.True(0 < scale && scale < 1);

                page = doc.ReloadPage(page);
                var link = page.GetLinks()[0];

                Assert.Equal("https://www.artifex.com", link["uri"]);

                // We must remove line breaks and any ligatures for this.
                string pageText = (string)page.GetText(flags: 0);
                Assert.Equal(base_text, pageText[..^1].Replace("\n", " "));

                int encounters = 0;
                var dict = (Dictionary<string, object>)page.GetText("dict");
                foreach (var b in (List<Dictionary<string, object>>)dict["blocks"])
                {
                    foreach (var l in (List<Dictionary<string, object>>)b["lines"])
                    {
                        var wdirArr = (float[])l["dir"];
                        var wdir = (wdirArr[0], wdirArr[1]);
                        Assert.Equal(wdirs[page.Number], wdir);
                        foreach (var s in (List<Dictionary<string, object>>)l["spans"])
                        {
                            string stext = (string)s["text"];
                            var color = SRgbToPdf(Convert.ToInt32(s["color"]));
                            bool bold = (Convert.ToUInt32(s["flags"]) & Constants.TextFontBold) != 0;
                            bool italic = (Convert.ToUInt32(s["flags"]) & Constants.TextFontItalic) != 0;
                            if (stext is "ullamco" or "laboris" or "voluptate")
                            {
                                encounters += 1;
                                if (stext == "ullamco")
                                {
                                    Assert.True(bold);
                                    Assert.False(italic);
                                    Assert.Equal(WxColors.PdfColorDict["black"], color);
                                }
                                else if (stext == "laboris")
                                {
                                    Assert.False(bold);
                                    Assert.True(italic);
                                    Assert.Equal(WxColors.PdfColorDict["black"], color);
                                }
                                else if (stext == "voluptate")
                                {
                                    Assert.True(bold);
                                    Assert.False(italic);
                                    Assert.Equal(WxColors.PdfColorDict["green"], color);
                                }
                            }
                            else
                            {
                                Assert.False(bold);
                                Assert.False(italic);
                            }
                        }
                    }
                }
                // all 3 special special words were encountered
                Assert.Equal(3, encounters);
            }
            doc.Save(Out("test_htmlbox1.pdf"));
        }

        [Fact]
        public void test_htmlbox2()
        {
            // return

            using var doc = new Document();
            var rect = _Constants.rect;

            var page = doc.NewPage();
            var bottoms = new HashSet<float>();
            foreach (var rot in new[] { 0, 90, 180, 270 })
            {
                // rect, "Hello, World!", scale_low=1, rotate=rot
                (float spare_height, float scale) = page.InsertHtmlbox(
                    rect, "Hello, World!", css: null, scaleLow: 1, archive: null, rotate: rot);
                Assert.Equal(1, scale);
                Assert.True(0 < spare_height && spare_height < rect.Height);
                bottoms.Add(spare_height);
            }
            Assert.Single(bottoms);
            doc.Save(Out("test_htmlbox2.pdf"));
        }

        [Fact]
        public void test_htmlbox3()
        {
            // return

            var rect = new Rect(100, 250, 300, 350);
            string html = "<span style=\"color:red;font-size:20px;\">Just some text.</span>";
            using var doc = new Document();

            var page = doc.NewPage();

            // insert some text with opacity
            page.InsertHtmlbox(rect, html, css: null, scaleLow: 0f, opacity: 0.5f, rotate: 0, oc: 0);

            // lowlevel-extract inserted text to access opacity
            var span = page.GetTextTrace()[0];
            Assert.Equal(0.5, Convert.ToDouble(span["opacity"]));
            doc.Save(Out("test_htmlbox3.pdf"));
        }

        [Fact]
        public void test_3559()
        {
            using var doc = new Document();

            var page = doc.NewPage();
            string text_insert = "<body><h3></h3></body>";
            var rect = _Constants.rect;
            page.InsertHtmlbox(rect, text_insert, css: null, scaleLow: 0f, opacity: 1f, rotate: 0, oc: 0);
            doc.Save(Out("test_3559.pdf"));
        }

        [Fact]
        public void test_3916()
        {
            using var doc = new Document();
            var rect = new Rect(100, 100, 101, 101);

            var page = doc.NewPage();
            (float spare_height, float scale) = page.InsertHtmlbox(rect, "Hello, World!", css: null, scaleLow: 0.5f, archive: null);
            Assert.Equal(-1, spare_height);
            doc.Save(Out("test_3916.pdf"));
        }

        [Fact]
        public void test_4400()
        {
            using var document = new Document();

            var page = document.NewPage();
            var writer = new TextWriter(page.Rect);
            string text4400 = "111111111";
            Console.WriteLine("Calling writer.FillTextbox().");
            writer.FillTextbox(rect: new Rect(0, 0, 100, 20), pos: new Point(80, 0), text: text4400, fontSize: 8);
            writer.WriteText(page);
            document.Save(Out("test_4400.pdf"));
        }

        [Fact]
        public void test_4613()
        {
            Console.WriteLine();
            string text4613 = string.Concat(
                Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz\nABCDEFGHIJKLMNOPQRSTUVWXYZ\n", 3));
            using var story = new Story(text4613);
            var rect = new Rect(10, 10, 100, 100);

            // Test default operation where we get additional scaling down because of
            // the long words in our text.
            Console.WriteLine("test_4613(): ### Testing default operation.");
            using (var doc = new Document())
            {

                var page = doc.NewPage();
                (float spare_height, float scale) = page.InsertHtmlbox(rect, story);
                Console.WriteLine($"test_4613(): spare_height={spare_height} scale={scale}");
                // The additional down-scaling from the long word widths results in
                // spare vertical space.
                page.DrawRect(rect, _Constants.red);
                string path = Out("test_4613_0.pdf");
                doc.Save(path);

                string path_pixmap = Out("test_4613.png");

                string path_pixmap_expected = Path.GetFullPath(Resource("test_4613.png"));
                var pixmap = page.GetPixmap(dpi: 300);
                pixmap.Save(path_pixmap);

                using var pixmap_diff = _Compare.PixmapsDiff(path_pixmap_expected, pixmap);
                pixmap_diff.Save(Out("test_4613-diff.png"));

                float rms = _Compare.PixmapsRms(pixmap, path_pixmap_expected);
                Console.WriteLine($"rms={rms}");
                Assert.True(Math.Abs(rms) < 0.22);

                Assert.True(Math.Abs(spare_height - 45.7536) < 0.1);
                Assert.True(Math.Abs(scale - 0.4009) < 0.01);

                string new_text = (string)page.GetText("text", clip: rect.IRect);
                Console.WriteLine("test_4613(): new_text:");
                Console.WriteLine(Indent(new_text, "    "));
                Assert.Equal(text4613, new_text);
                doc.Save(Out("test_4613_1.pdf"));
            }

            Console.WriteLine("test_4613(): ### Testing with _scale_word_width=False.");
            using (var doc = new Document())
            {

                var page = doc.NewPage();
                (float spare_height, float scale) = page.InsertHtmlbox(rect, story, scaleWordWidth: false);
                Console.WriteLine($"test_4613(): _scale_word_width=False: spare_height={spare_height} scale={scale}");
                // down-scaling.
                Assert.Equal(0, spare_height);
                Assert.True(Math.Abs(scale - 0.914) < 0.01);
                string new_text = (string)page.GetText("text", clip: rect.IRect);
                Console.WriteLine("test_4613(): new_text:");
                Console.WriteLine(Indent(new_text, "    "));
                string expectedTruncated = Dedent("""
                    abcdefghijklmno
                    ABCDEFGHIJKLM
                    abcdefghijklmno
                    ABCDEFGHIJKLM
                    abcdefghijklmno
                    ABCDEFGHIJKLM
                    """);
                if (expectedTruncated.Length > 0 && expectedTruncated[0] == '\n')
                    expectedTruncated = expectedTruncated.Substring(1);
                // Assert.Equal(expectedTruncated, new_text);
                doc.Save(Out("test_4613_2.pdf"));
            }

            Console.WriteLine("test_4613(): ### Testing with scale_low too high to allow a fit.");
            using (var doc = new Document())
            {

                var page = doc.NewPage();
                float scale_low = 0.6f;
                (float spare_height, float scale) = page.InsertHtmlbox(rect, story, scaleLow: scale_low);
                Console.WriteLine($"test_4613(): scale_low={scale_low}: spare_height={spare_height} scale={scale}");
                Assert.Equal(-1, spare_height);
                Assert.Equal((float)scale_low, scale);
                doc.Save(Out("test_4613_3.pdf"));
            }
        }
    }
}