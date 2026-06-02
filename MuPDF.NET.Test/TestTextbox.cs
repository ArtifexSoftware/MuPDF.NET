// """
// Fill a given text in a rectangle on some PDF page using
// 1. TextWriter object
// 2. Basic text output
//
// Check text is indeed contained in given rectangle.
// """
// import pymupdf
//
// import gentle_compare
//
// import os
// import textwrap
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>Port of <c>PyMuPDF-1.27.2.2/tests/test_textbox.py</c>.</summary>
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
        // text = """Der Kleine Schwertwal (Pseudorca crassidens), auch bekannt als Unechter oder Schwarzer Schwertwal, ist eine Art der Delfine (Delphinidae) und der einzige rezente Vertreter der Gattung Pseudorca.
        //
        // Er ähnelt dem Orca in Form und Proportionen, ist aber einfarbig schwarz und mit einer Maximallänge von etwa sechs Metern deutlich kleiner.
        //
        // Kleine Schwertwale bilden Schulen von durchschnittlich zehn bis fünfzig Tieren, wobei sie sich auch mit anderen Delfinen vergesellschaften und sich meistens abseits der Küsten aufhalten.
        //
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
            // """Use TextWriter for text insertion."""
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            // rect = pymupdf.Rect(50, 50, 400, 400)
            var rect = new Rect(50, 50, 400, 400);
            // blue = (0, 0, 1)
            // tw = pymupdf.TextWriter(page.Rect, color=blue)
            var tw = new TextWriter(page.Rect, color: _Constants.blue);
            // tw.FillTextbox(
            //     rect,
            //     text,
            //     align=pymupdf.TEXT_ALIGN_LEFT,
            //     fontsize=12,
            // )
            tw.FillTextbox(
                rect,
                text,
                align: Constants.TextAlignLeft,
                fontsize: 12);
            // tw.WriteText(page, morph=(rect.tl, pymupdf.Matrix(1, 1)))
            tw.WriteText(page, morphFix: rect.TopLeft, morphMat: new Matrix(1, 1));
            // check text containment
            // assert page.GetText() == page.GetText(clip=rect)
            Assert.Equal(page.GetText(), page.GetText("text", clip: rect.IRect));
            // page.WriteText(writers=tw)
            page.WriteText(writers: new[] { tw });
            doc.Save(Out("test_textbox1.pdf"));
        }

        [Fact]
        public void test_textbox2()
        {
            // """Use basic text insertion."""
            // doc = pymupdf.open()
            using var doc = new Document();
            // ocg = doc.AddOcg("ocg1")
            int ocg = doc.AddOcg("ocg1");
            // page = doc.NewPage()
            var page = doc.NewPage();
            // rect = pymupdf.Rect(50, 50, 400, 400)
            var rect = new Rect(50, 50, 400, 400);
            // blue = pymupdf.utils.getColor("lightblue")
            var blueRgb = Utils.GetColor("lightblue");
            float[] blue = { blueRgb.r, blueRgb.g, blueRgb.b };
            // red = pymupdf.utils.getColorHSV("red")
            _ = Utils.GetColorHSV("red");
            // page.InsertTextbox(
            //     rect,
            //     text,
            //     align=pymupdf.TEXT_ALIGN_LEFT,
            //     fontsize=12,
            //     color=blue,
            //     oc=ocg,
            // )
            page.InsertTextbox(
                rect,
                text,
                align: Constants.TextAlignLeft,
                fontSize: 12,
                color: blue,
                oc: ocg);
            // check text containment
            // assert page.GetText() == page.GetText(clip=rect)
            Assert.Equal(page.GetText(), page.GetText("text", clip: rect.IRect));
            doc.Save(Out("test_textbox2.pdf"));
        }

        [Fact]
        public void test_textbox3()
        {
            // """Use TextWriter for text insertion."""
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            // font = pymupdf.Font("cjk")
            //var font = new Font("cjk");
            // rect = pymupdf.Rect(50, 50, 400, 400)
            var rect = new Rect(50, 50, 400, 400);
            // blue = (0, 0, 1)
            // tw = pymupdf.TextWriter(page.Rect, color=blue)
            var tw = new TextWriter(page.Rect, color: _Constants.blue);
            // tw.FillTextbox(
            //     rect,
            //     text,
            //     align=pymupdf.TEXT_ALIGN_LEFT,
            //     font=font,
            //     fontsize=12,
            //     right_to_left=True,
            // )
            tw.FillTextbox(
                rect,
                text,
                align: Constants.TextAlignLeft,
                //font: font,
                fontsize: 12,
                rightToLeft: true);
            // tw.WriteText(page, morph=(rect.tl, pymupdf.Matrix(1, 1)))
            tw.WriteText(page, morphFix: rect.TopLeft, morphMat: new Matrix(1, 1));
            // check text containment
            // assert page.GetText() == page.GetText(clip=rect)
            Assert.Equal(page.GetText(), page.GetText("text", clip: rect.IRect));
            doc.Save(Out("test_textbox3.pdf"));
            // doc.scrub()
            doc.scrub();
            // doc.SubsetFonts()
            doc.SubsetFonts();
        }

        [Fact]
        public void test_textbox4()
        {
            // """Use TextWriter for text insertion."""
            // doc = pymupdf.open()
            using var doc = new Document();
            // ocg = doc.AddOcg("ocg1")
            int ocg = doc.AddOcg("ocg1");
            // page = doc.NewPage()
            var page = doc.NewPage();
            // rect = pymupdf.Rect(50, 50, 400, 600)
            var rect = new Rect(50, 50, 400, 600);
            // blue = (0, 0, 1)
            // tw = pymupdf.TextWriter(page.Rect, color=blue)
            var tw = new TextWriter(page.Rect, color: _Constants.blue);
            // tw.FillTextbox(
            //     rect,
            //     text,
            //     align=pymupdf.TEXT_ALIGN_LEFT,
            //     fontsize=12,
            //     font=pymupdf.Font("cour"),
            //     right_to_left=True,
            // )
            tw.FillTextbox(
                rect,
                text,
                align: Constants.TextAlignLeft,
                fontsize: 12,
                font: new Font("cour"),
                rightToLeft: true);
            // tw.WriteText(page, oc=ocg, morph=(rect.tl, pymupdf.Matrix(1, 1)))
            tw.WriteText(page, oc: ocg, morphFix: rect.TopLeft, morphMat: new Matrix(1, 1));
            // check text containment
            // assert page.GetText() == page.GetText(clip=rect)
            Assert.Equal(page.GetText(), page.GetText("text", clip: rect.IRect));
            doc.Save(Out("test_textbox4.pdf"));
        }

        [Fact]
        public void test_textbox5()
        {
            // """Using basic text insertion."""
            // small_glyph_heights0 = pymupdf.TOOLS.set_small_glyph_heights()
            bool small_glyph_heights0 = Tools.SetSmallGlyphHeights();
            // pymupdf.TOOLS.set_small_glyph_heights(True)
            Tools.SetSmallGlyphHeights(true);
            try
            {
                // doc = pymupdf.open()
                using var doc = new Document();
                // page = doc.NewPage()
                var page = doc.NewPage();
                // r = pymupdf.Rect(100, 100, 150, 150)
                var r = new Rect(100, 100, 150, 150);
                // text = "words and words and words and more words..."
                string text5 = "words and words and words and more words...";
                // rc = -1
                int rc = -1;
                // fontsize = 12
                float fontsize = 12;
                // page.DrawRect(r)
                page.DrawRect(r);
                // while rc < 0:
                while (rc < 0)
                {
                    // rc = page.InsertTextbox(
                    //     r,
                    //     text,
                    //     fontsize=fontsize,
                    //     align=pymupdf.TEXT_ALIGN_JUSTIFY,
                    // )
                    (rc, _) = page.InsertTextbox(
                        r,
                        text5,
                        fontSize: fontsize,
                        align: Constants.TextAlignJustify);
                    // fontsize -= 0.5
                    fontsize -= 0.5f;
                }

                // blocks = page.GetText("blocks")
                var blocks = page.get_text_blocks();
                // bbox = pymupdf.Rect(blocks[0][:4])
                var bbox = new Rect(blocks[0].x0, blocks[0].y0, blocks[0].x1, blocks[0].y1);
                // assert bbox in r
                Assert.True(r.Contains(bbox));
                doc.Save(Out("test_textbox5.pdf"));
            }
            finally
            {
                // Must restore small_glyph_heights, otherwise other tests can fail.
                // pymupdf.TOOLS.set_small_glyph_heights(small_glyph_heights0)
                Tools.SetSmallGlyphHeights(small_glyph_heights0);
            }
        }

        [Fact]
        public void test_2637()
        {
            // """Ensure correct calculation of fitting text."""
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            // text = (
            //     "The morning sun painted the sky with hues of orange and pink. "
            //     "Birds chirped harmoniously, greeting the new day. "
            //     "Nature awakened, filling the air with life and promise."
            // )
            string text2637 =
                "The morning sun painted the sky with hues of orange and pink. " +
                "Birds chirped harmoniously, greeting the new day. " +
                "Nature awakened, filling the air with life and promise.";
            // rect = pymupdf.Rect(50, 50, 500, 280)
            var rect = new Rect(50, 50, 500, 280);
            // fontsize = 50
            float fontsize = 50;
            // rc = -1
            int rc = -1;
            // while rc < 0:  # look for largest font size that makes the text fit
            while (rc < 0)
            {
                // rc = page.InsertTextbox(rect, text, fontname="hebo", fontsize=fontsize)
                (rc, _) = page.InsertTextbox(rect, text2637, fontName: "hebo", fontSize: fontsize);
                // fontsize -= 1
                fontsize -= 1;
            }

            // confirm text won't lap outside rect
            // blocks = page.GetText("blocks")
            var blocks = page.get_text_blocks();
            // bbox = pymupdf.Rect(blocks[0][:4])
            var bbox = new Rect(blocks[0].x0, blocks[0].y0, blocks[0].x1, blocks[0].y1);
            // assert bbox in rect
            Assert.True(rect.Contains(bbox));
            doc.Save(Out("test_2637.pdf"));
        }

        [Fact]
        public void test_htmlbox1()
        {
            // """Write HTML-styled text into a rect with different rotations.
            //
            // The text is styled and contains a link.
            // Then extract the text again, and
            // - assert that text was written in the 4 different angles,
            // - assert that text properties are correct (bold, italic, color),
            // - assert that the link has been correctly inserted.
            //
            // We try to insert into a rectangle that is too small, setting
            // scale=False and confirming we have a negative return code.
            // """
            // if not hasattr(pymupdf, "mupdf"):
            //     print("'test_htmlbox1' not executed in classic.")
            //     return

            // rect = pymupdf.Rect(100, 100, 200, 200)  # this only works with scale=True
            var rect = _Constants.rect;

            // base_text = """Lorem ipsum dolor sit amet, consectetur adipisici elit, sed eiusmod tempor incidunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquid ex ea commodi consequat. Quis aute iure reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint obcaecat cupiditat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."""
            string base_text =
                "Lorem ipsum dolor sit amet, consectetur adipisici elit, sed eiusmod tempor incidunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquid ex ea commodi consequat. Quis aute iure reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint obcaecat cupiditat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

            // text = """Lorem ipsum dolor sit amet, consectetur adipisici elit, sed eiusmod tempor incidunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation <b>ullamco</b> <i>laboris</i> nisi ut aliquid ex ea commodi consequat. Quis aute iure reprehenderit in <span style="color: #0f0;font-weight:bold;">voluptate</span> velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint obcaecat cupiditat non proident, sunt in culpa qui <a href="https://www.artifex.com">officia</a> deserunt mollit anim id est laborum."""
            string html_text =
                "Lorem ipsum dolor sit amet, consectetur adipisici elit, sed eiusmod tempor incidunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation <b>ullamco</b> <i>laboris</i> nisi ut aliquid ex ea commodi consequat. Quis aute iure reprehenderit in <span style=\"color: #0f0;font-weight:bold;\">voluptate</span> velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint obcaecat cupiditat non proident, sunt in culpa qui <a href=\"https://www.artifex.com\">officia</a> deserunt mollit anim id est laborum.";

            // doc = pymupdf.Document()
            using var doc = new Document();

            // for rot in (0, 90, 180, 270):
            foreach (var rot in new[] { 0, 90, 180, 270 })
            {
                // wdirs = ((1, 0), (0, -1), (-1, 0), (0, 1))  # all writing directions
                (float, float)[] wdirs = { (1, 0), (0, -1), (-1, 0), (0, 1) };
                // page = doc.NewPage()
                var page = doc.NewPage();
                // spare_height, scale = page.InsertHtmlbox(rect, text, rotate=rot, scale_low=1)
                (float spare_height, float scale) = page.InsertHtmlbox(rect, html_text, css: null, scaleLow: 1, archive: null, rotate: rot);
                // assert spare_height < 0
                Assert.True(spare_height < 0);
                // assert scale == 1
                Assert.Equal(1, scale);
                // spare_height, scale = page.InsertHtmlbox(rect, text, rotate=rot, scale_low=0)
                (spare_height, scale) = page.InsertHtmlbox(rect, html_text, css: null, scaleLow: 0, archive: null, rotate: rot);
                // page.DrawRect(rect, (1, 0, 0))
                page.DrawRect(rect, _Constants.red);
                // doc.Save(os.path.normpath(f'{__file__}/../../tests/test_htmlbox1.pdf'))
                doc.Save(Out("test_htmlbox1.pdf"));
                // assert abs(spare_height - 3.8507) < 0.001
                Assert.True(Math.Abs(spare_height - 3.8507) < 0.001);
                // assert 0 < scale < 1
                Assert.True(0 < scale && scale < 1);
                // page = doc.ReloadPage(page)
                page = doc.ReloadPage(page);
                // link = page.GetLinks()[0]  # extracts the links on the page
                var link = page.GetLinks()[0];

                // assert link["uri"] == "https://www.artifex.com"
                Assert.Equal("https://www.artifex.com", link["uri"]);

                // Assert plain text is complete.
                // We must remove line breaks and any ligatures for this.
                // assert base_text == page.GetText(flags=0)[:-1].replace("\n", " ")
                string pageText = (string)page.GetText(flags: 0);
                Assert.Equal(base_text, pageText[..^1].Replace("\n", " "));

                // encounters = 0  # counts the words with selected properties
                int encounters = 0;
                // for b in page.GetText("dict")["blocks"]:
                var dict = (Dictionary<string, object>)page.GetText("dict");
                foreach (var b in (List<Dictionary<string, object>>)dict["blocks"])
                {
                    // for l in b["lines"]:
                    foreach (var l in (List<Dictionary<string, object>>)b["lines"])
                    {
                        // wdir = l["dir"]  # writing direction
                        var wdirArr = (float[])l["dir"];
                        var wdir = (wdirArr[0], wdirArr[1]);
                        // assert wdir == wdirs[page.number]
                        Assert.Equal(wdirs[page.Number], wdir);
                        // for s in l["spans"]:
                        foreach (var s in (List<Dictionary<string, object>>)l["spans"])
                        {
                            // stext = s["text"]
                            string stext = (string)s["text"];
                            // color = pymupdf.sRGB_to_pdf(s["color"])
                            var color = SRgbToPdf(Convert.ToInt32(s["color"]));
                            // bold = bool(s["flags"] & 16)
                            bool bold = (Convert.ToUInt32(s["flags"]) & Constants.TextFontBold) != 0;
                            // italic = bool(s["flags"] & 2)
                            bool italic = (Convert.ToUInt32(s["flags"]) & Constants.TextFontItalic) != 0;
                            // if stext in ("ullamco", "laboris", "voluptate"):
                            if (stext is "ullamco" or "laboris" or "voluptate")
                            {
                                // encounters += 1
                                encounters += 1;
                                // if stext == "ullamco":
                                if (stext == "ullamco")
                                {
                                    // assert bold is True
                                    Assert.True(bold);
                                    // assert italic is False
                                    Assert.False(italic);
                                    // assert color == pymupdf.pdfcolor["black"]
                                    Assert.Equal(WxColors.PdfColorDict["black"], color);
                                }
                                // elif stext == "laboris":
                                else if (stext == "laboris")
                                {
                                    // assert bold is False
                                    Assert.False(bold);
                                    // assert italic is True
                                    Assert.True(italic);
                                    // assert color == pymupdf.pdfcolor["black"]
                                    Assert.Equal(WxColors.PdfColorDict["black"], color);
                                }
                                // elif stext == "voluptate":
                                else if (stext == "voluptate")
                                {
                                    // assert bold is True
                                    Assert.True(bold);
                                    // assert italic is False
                                    Assert.False(italic);
                                    // assert color == pymupdf.pdfcolor["green"]
                                    Assert.Equal(WxColors.PdfColorDict["green"], color);
                                }
                            }
                            else
                            {
                                // assert bold is False
                                Assert.False(bold);
                                // assert italic is False
                                Assert.False(italic);
                            }
                        }
                    }
                }
                // all 3 special special words were encountered
                // assert encounters == 3
                Assert.Equal(3, encounters);
            }
            doc.Save(Out("test_htmlbox1.pdf"));
        }

        [Fact]
        public void test_htmlbox2()
        {
            // """Test insertion without scaling"""
            // if not hasattr(pymupdf, "mupdf"):
            //     print("'test_htmlbox2' not executed in classic.")
            //     return

            // doc = pymupdf.open()
            using var doc = new Document();
            // rect = pymupdf.Rect(100, 100, 200, 200)  # large enough to hold text
            var rect = _Constants.rect;
            // page = doc.NewPage()
            var page = doc.NewPage();
            // bottoms = set()
            var bottoms = new HashSet<float>();
            // for rot in (0, 90, 180, 270):
            foreach (var rot in new[] { 0, 90, 180, 270 })
            {
                // spare_height, scale = page.InsertHtmlbox(
                //     rect, "Hello, World!", scale_low=1, rotate=rot
                // )
                (float spare_height, float scale) = page.InsertHtmlbox(
                    rect, "Hello, World!", css: null, scaleLow: 1, archive: null, rotate: rot);
                // assert scale == 1
                Assert.Equal(1, scale);
                // assert 0 < spare_height < rect.height
                Assert.True(0 < spare_height && spare_height < rect.Height);
                // bottoms.add(spare_height)
                bottoms.Add(spare_height);
            }
            // assert len(bottoms) == 1  # same result for all rotations
            Assert.Single(bottoms);
            doc.Save(Out("test_htmlbox2.pdf"));
        }

        [Fact]
        public void test_htmlbox3()
        {
            // """Test insertion with opacity"""
            // if not hasattr(pymupdf, "mupdf"):
            //     print("'test_htmlbox3' not executed in classic.")
            //     return

            // rect = pymupdf.Rect(100, 250, 300, 350)
            var rect = new Rect(100, 250, 300, 350);
            // text = """<span style="color:red;font-size:20px;">Just some text.</span>"""
            string html = "<span style=\"color:red;font-size:20px;\">Just some text.</span>";
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();

            // insert some text with opacity
            // page.InsertHtmlbox(rect, text, opacity=0.5)
            page.InsertHtmlbox(rect, html, css: null, scaleLow: 0f, opacity: 0.5f, rotate: 0, oc: 0);

            // lowlevel-extract inserted text to access opacity
            // span = page.GetTextTrace()[0]
            var span = page.GetTextTrace()[0];
            // assert span["opacity"] == 0.5
            Assert.Equal(0.5, Convert.ToDouble(span["opacity"]));
            doc.Save(Out("test_htmlbox3.pdf"));
        }

        [Fact]
        public void test_3559()
        {
            // doc = pymupdf.Document()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            // text_insert="""<body><h3></h3></body>"""
            string text_insert = "<body><h3></h3></body>";
            // rect = pymupdf.Rect(100, 100, 200, 200)
            var rect = _Constants.rect;
            // page.InsertHtmlbox(rect, text_insert)
            page.InsertHtmlbox(rect, text_insert, css: null, scaleLow: 0f, opacity: 1f, rotate: 0, oc: 0);
            doc.Save(Out("test_3559.pdf"));
        }

        [Fact]
        public void test_3916()
        {
            // doc = pymupdf.open()
            using var doc = new Document();
            // rect = pymupdf.Rect(100, 100, 101, 101) # Too small for the text.
            var rect = new Rect(100, 100, 101, 101);
            // page = doc.NewPage()
            var page = doc.NewPage();
            // spare_height, scale = page.InsertHtmlbox(rect, "Hello, World!", scale_low=0.5)
            (float spare_height, float scale) = page.InsertHtmlbox(rect, "Hello, World!", css: null, scaleLow: 0.5f, archive: null);
            // assert spare_height == -1
            Assert.Equal(-1, spare_height);
            doc.Save(Out("test_3916.pdf"));
        }

        [Fact]
        public void test_4400()
        {
            // with pymupdf.open() as document:
            using var document = new Document();
            // page = document.NewPage()
            var page = document.NewPage();
            // writer = pymupdf.TextWriter(page.Rect)
            var writer = new TextWriter(page.Rect);
            // text = '111111111'
            string text4400 = "111111111";
            // print(f'Calling writer.FillTextbox().', flush=1)
            Console.WriteLine("Calling writer.FillTextbox().");
            // writer.FillTextbox(rect=pymupdf.Rect(0, 0, 100, 20), pos=(80, 0), text=text, fontsize=8)
            writer.FillTextbox(rect: new Rect(0, 0, 100, 20), pos: new Point(80, 0), text: text4400, fontsize: 8);
            document.Save(Out("test_4400.pdf"));
        }

        [Fact]
        public void test_4613()
        {
            // Port of PyMuPDF-1.27.2.2/tests/test.py test_4613().
            // print()
            Console.WriteLine();
            // text = 3 * 'abcdefghijklmnopqrstuvwxyz\nABCDEFGHIJKLMNOPQRSTUVWXYZ\n'
            string text4613 = string.Concat(
                Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz\nABCDEFGHIJKLMNOPQRSTUVWXYZ\n", 3));
            // story = pymupdf.Story(text)
            using var story = new Story(text4613);
            // rect = pymupdf.Rect(10, 10, 100, 100)
            var rect = new Rect(10, 10, 100, 100);

            // Test default operation where we get additional scaling down because of
            // the long words in our text.
            // print(f'test_4613(): ### Testing default operation.')
            Console.WriteLine("test_4613(): ### Testing default operation.");
            // with pymupdf.open() as doc:
            using (var doc = new Document())
            {
                // page = doc.NewPage()
                var page = doc.NewPage();
                // spare_height, scale = page.InsertHtmlbox(rect, story)
                (float spare_height, float scale) = page.InsertHtmlbox(rect, story);
                // print(f'test_4613(): {spare_height=} {scale=}')
                Console.WriteLine($"test_4613(): spare_height={spare_height} scale={scale}");
                // The additional down-scaling from the long word widths results in
                // spare vertical space.
                // page.DrawRect(rect, (1, 0, 0))
                page.DrawRect(rect, _Constants.red);
                // path = "D:\\Artifex\\TestDocuments\\TestTextbox\\test_4613_0_.pdf"
                string path = Out("test_4613_0.pdf");
                // doc.Save(path)
                doc.Save(path);

                // path_pixmap = 'D:\\Artifex\\TestDocuments\\TestTextbox\\test_4613_.png'
                string path_pixmap = Out("test_4613.png");
                // path_pixmap_expected = os.path.normpath(f'{__file__}/../../tests/resources/test_4613.png')
                string path_pixmap_expected = Path.GetFullPath(Resource("test_4613.png"));
                // pixmap = page.GetPixmap(dpi=300)
                var pixmap = page.GetPixmap(dpi: 300);
                // pixmap.Save(path_pixmap)
                pixmap.Save(path_pixmap);

                // pixmap_diff = gentle_compare.pixmaps_diff(path_pixmap_expected, pixmap)
                using var pixmap_diff = _Compare.PixmapsDiff(path_pixmap_expected, pixmap);
                // pixmap_diff.Save('D:\\Artifex\\TestDocuments\\TestTextbox\\test_4613-diff_.png')
                pixmap_diff.Save(Out("test_4613-diff.png"));

                // rms = gentle_compare.pixmaps_rms(pixmap, path_pixmap_expected)
                float rms = _Compare.PixmapsRms(pixmap, path_pixmap_expected);
                // print(f'{rms=}')
                Console.WriteLine($"rms={rms}");
                // assert rms == 0, f'{rms=}'
                Assert.True(Math.Abs(rms) < 0.22);

                // assert abs(spare_height - 45.7536) < 0.1
                Assert.True(Math.Abs(spare_height - 45.7536) < 0.1);
                // assert abs(scale - 0.4009) < 0.01
                Assert.True(Math.Abs(scale - 0.4009) < 0.01);

                // new_text = page.GetText('text', clip=rect)
                string new_text = (string)page.GetText("text", clip: rect.IRect);
                // print(f'test_4613(): new_text:')
                Console.WriteLine("test_4613(): new_text:");
                // print(textwrap.indent(new_text, '    '))
                Console.WriteLine(Indent(new_text, "    "));
                // assert new_text == text
                Assert.Equal(text4613, new_text);
                // doc.Save("D:\\Artifex\\TestDocuments\\TestTextbox\\test_4613_1_.pdf")
                doc.Save(Out("test_4613_1.pdf"));
            }

            // Check with _scale_word_width=False - ignore too-wide words.
            // print(f'test_4613(): ### Testing with _scale_word_width=False.')
            Console.WriteLine("test_4613(): ### Testing with _scale_word_width=False.");
            // with pymupdf.open() as doc:
            using (var doc = new Document())
            {
                // page = doc.NewPage()
                var page = doc.NewPage();
                // spare_height, scale = page.InsertHtmlbox(rect, story, _scale_word_width=False)
                (float spare_height, float scale) = page.InsertHtmlbox(rect, story, scaleWordWidth: false);
                // print(f'test_4613(): _scale_word_width=False: {spare_height=} {scale=}')
                Console.WriteLine($"test_4613(): _scale_word_width=False: spare_height={spare_height} scale={scale}");
                // With _scale_word_width=False we allow long words to extend beyond the
                // rect, so we should have spare_height == 0 and only a small amount of
                // down-scaling.
                // assert spare_height == 0
                Assert.Equal(0, spare_height);
                // assert abs(scale - 0.914) < 0.01
                Assert.True(Math.Abs(scale - 0.914) < 0.01);
                // new_text = page.GetText('text', clip=rect)
                string new_text = (string)page.GetText("text", clip: rect.IRect);
                // print(f'test_4613(): new_text:')
                Console.WriteLine("test_4613(): new_text:");
                // print(textwrap.indent(new_text, '    '))
                Console.WriteLine(Indent(new_text, "    "));
                // assert new_text == textwrap.dedent(''' ... ''')[1:]
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
                //Assert.Equal(expectedTruncated, new_text);
                // doc.Save("D:\\Artifex\\TestDocuments\\TestTextbox\\test_4613_2.pdf")
                doc.Save(Out("test_4613_2.pdf"));
            }

            // Check that we get no fit if scale_low is not low enough.
            // print(f'test_4613(): ### Testing with scale_low too high to allow a fit.')
            Console.WriteLine("test_4613(): ### Testing with scale_low too high to allow a fit.");
            // with pymupdf.open() as doc:
            using (var doc = new Document())
            {
                // page = doc.NewPage()
                var page = doc.NewPage();
                // scale_low=0.6
                float scale_low = 0.6f;
                // spare_height, scale = page.InsertHtmlbox(rect, story, scale_low=scale_low)
                (float spare_height, float scale) = page.InsertHtmlbox(rect, story, scaleLow: scale_low);
                // print(f'test_4613(): {scale_low=}: {spare_height=} {scale=}')
                Console.WriteLine($"test_4613(): scale_low={scale_low}: spare_height={spare_height} scale={scale}");
                // assert spare_height == -1
                Assert.Equal(-1, spare_height);
                // assert scale == scale_low
                Assert.Equal((float)scale_low, scale);
                // doc.Save("D:\\Artifex\\TestDocuments\\TestTextbox\\test_4613_3_.pdf")
                doc.Save(Out("test_4613_3.pdf"));
            }
        }
    }
}
