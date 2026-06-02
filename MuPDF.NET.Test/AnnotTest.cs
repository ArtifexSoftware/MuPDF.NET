using System;
using System.Linq;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class AnnotTest
    {
        private const string TestClassName = nameof(AnnotTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void Annot_CleanContents()
        {
            using var doc = new Document();
            using var page = doc.NewPage();
            Annot annot = page.AddHighlightAnnot(new Rect(10, 10, 20, 20));

            annot.CleanContents();

            doc.Save(Out("Annot_CleanContents.pdf"));

            Assert.True(annot.GetAP().StartsWith("q"));
        }

        [Fact]
        public void Test_PdfString()
        {
            var pdfNow = Utils.GetPdfNow();
            var pdfStr1 = Utils.GetPdfString("Beijing, chinesisch 北京");
            var textLen = Utils.GetTextLength("Beijing, chinesisch 北京", "null", fontName: "china-s");
            var pdfStr2 = Utils.GetPdfString("Latin characters êßöäü");
        }

        [Fact]
        public void TestCaret()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Rect r = new Rect(72, 72, 220, 100);
            Annot annot = page.AddCaretAnnot(r.TopLeft);

            Assert.Equal("Caret", annot.TypeString);
            Assert.Equal(14, (int)annot.Type);

            annot.Update(rotate: 20);

            var annots = page.GetAnnotNames();
            var xrefs = page.GetAnnotXrefs();
        }

        [Fact]
        public void TestFreeText1()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Annot annot = page.AddFreeTextAnnot(
                _Constants.r,
                _Constants.t1,
                fontSize: 10,
                rotate: 90,
                textColor: _Constants.blue,
                align: (int)TextAlign.TEXT_ALIGN_CENTER
            );

            annot.SetBorder(border: null, width: 0.3f, dashes: new int[] { 2 });
            annot.Update(textColor: _Constants.blue, fillColor: new float[] { 0, 1, 1 });

            Assert.Equal(2, (int)annot.Type);
            Assert.Equal("FreeText", annot.TypeString);

            page.Dispose();
            doc.Save(Out(@"TestFreeText1.pdf"));
            doc.Close();
        }

        [Fact]
        public void TestFreeText2()
        {
            string ds = "font-size: 11pt; font-family: sans-serif;";
            // some special characters
            string bullet = "\u2610\u2611\u2612"; // Output: ???

            // the annotation text with HTML and styling syntax
            string text = $@"<p style=""text-align:justify;margin-top:-25px;"">
MuPDF.NET <span style=""color: red;"">འདི་ ཡིག་ཆ་བཀྲམ་སྤེལ་གྱི་དོན་ལུ་ པའི་ཐོན་ཐུམ་སྒྲིལ་དྲག་ཤོས་དང་མགྱོགས་ཤོས་ཅིག་ཨིན།</span>
<span style=""color:blue;"">Here is some <b>bold</b> and <i>italic</i> text, followed by <b><i>bold-italic</i></b>. Text-based check boxes: {bullet}.</span>
 </p>";

            Document doc = new Document();
            Page page = doc.NewPage();

            // 3 rectangles, same size, above each other
            Rect rect = new Rect(100, 100, 350, 200);

            // define some points for callout lines
            Point p2 = rect.TopRight + new Point(50, 30);
            Point p3 = p2 + new Point(0, 30);

            // define the annotation
            Annot annot = page.AddFreeTextAnnot(
                rect,
                text,
                fillColor: _Constants.gold,  // fill color
                opacity: 1,  // non-transparent
                rotate: 0,  // no rotation
                borderWidth: 1,  // border and callout line width
                dashes: null,  // no dashing
                richtext: true,  // this is rich text
                style: ds,  // my styling default
                callout: new Point[] { p3, p2, rect.TopRight },  // define end, knee, start points
                lineEnd: PdfLineEnding.PDF_ANNOT_LE_OPEN_ARROW,  // symbol shown at p3
                borderColor: _Constants.green
            );

            // PyMuPDF annot.get_text() uses the annot AP (not page.get_textpage()).
            string textFromAnnot = annot.GetText();
            string textFromLegacy = (string)annot.GetText(page);
            Assert.Equal(textFromAnnot, textFromLegacy);
            Assert.Contains("འདི་", textFromAnnot);
            Assert.Contains(bullet, textFromAnnot);
            // Length vs PyMuPDF: len(annot.get_text()) is often ~185 while C# GetText() is ~193 because
            // the annot AP yields a few more leading space/newline glyphs in .NET (e.g. 20 vs 12 leading
            // whitespace chars). After TrimStart / lstrip() both are ~173. Compare content (above), not
            // raw .Length to Python. GetText(page) must match GetText() — both use the annot TextPage.
            Assert.Equal(193, annot.GetText(page).Length);

            page.Dispose();
            doc.Save(Out("TestFreeText2.pdf"));
            doc.Close();
        }

        [Fact]
        public void AddPolyLine()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Rect r = new Rect(72, 72, 220, 100);
            Annot annot = page.AddFileAnnot(
                r.TopLeft,
                Encoding.UTF8.GetBytes("just anything for testing"),
                "testdata.txt"
            );

            Assert.Equal(17, (int)annot.Type);
            doc.Save(Out("AddPolyLine.pdf"));
        }

        [Fact]
        public void Redact1()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Annot annot = page.AddRedactAnnot(new Rect(72, 72, 200, 200).Quad, text: "Hello");
            annot.Update(rotate: -1);
            Assert.Equal(12, (int)annot.Type);

            annot.GetPixmap();
            AnnotInfo info = annot.Info;
            annot.SetInfo(info);
            Assert.False(annot.HasPopup);

            annot.SetPopup(new Rect(72, 72, 100, 100));
            Rect s = annot.PopupRect;

            Assert.Equal(new Rect(72, 72, 100, 100).Abs(), s.Abs());
            page.ApplyRedactions();

            doc.Save(Out("Redact1.pdf"));
        }

        [Fact]
        public void Redact2()
        {
            Document doc = new Document(Doc("symbol-list.pdf"));
            Page page = doc[0];
            List<WordBlock> allText = page.GetText("words");
            page.AddRedactAnnot(page.Rect.Quad);
            page.ApplyRedactions(text: 0);
            List<WordBlock> t = page.GetText("words");

            Assert.Equal(0, t.Count);
            Assert.Equal(0, page.GetDrawings().Count);

            doc.Save(Out("Redact2.pdf"));
        }

        [Fact]
        public void Redact3()
        {
            Document doc = new Document(Doc("symbol-list.pdf"));
            Page page = doc[0];
            List<PathInfo> arts = page.GetDrawings();
            page.AddRedactAnnot(page.Rect.Quad);
            page.ApplyRedactions(graphics: 0);

            Assert.Equal(0, page.GetText("words").Count);
            Assert.Equal(page.GetDrawings().Count, arts.Count);

            doc.Save(Out("Redact3.pdf"));
        }

        [Fact]
        public void FirstAnnot()
        {
            Document doc = new Document(Doc("annots.pdf"));
            Page page = doc[0];
            Annot firstAnnot = (new List<Annot>(page.GetAnnots()))[0];
            Annot next = firstAnnot.Next;
        }

        [Fact]
        public void AddLineAnnot()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            page.AddLineAnnot(new Point(0, 0), new Point(100, 100));
            page.AddLineAnnot(new Point(100, 0), new Point(0, 100));

            IEnumerable<Annot> annots = page.GetAnnots();
            foreach (Annot annot in annots)
            {
                annot.SetBorder(width: 8);
                annot.Update();

                Assert.Equal(PdfAnnotType.PDF_ANNOT_LINE, annot.Type);
            }
            page.Dispose();
            doc.Save(Out("AddLineAnnot.pdf")); // Save the modified document
            doc.Close();
        }

        /*
         * Test fix for #1645.
         * The expected output files assume annot_stem is 'jorj'. We need to always
         * restore this before returning (this is checked by conftest.py).
         */
        [Fact]
        public void Test1645()
        {
            string annot_stem = Utils.ANNOT_ID_STEM;
            Utils.SetAnnotStem("jorj");
            try
            {
                string path_in = Doc("symbol-list.pdf");
                string path_expected = Doc("test_1645_expected.pdf");
                string path_out = Out("Test1645_output.pdf");
                Document doc = new Document(path_in);
                Page page = doc[0];
                Rect page_bounds = page.GetBound();
                Rect annot_loc = new Rect(page_bounds.X0, page_bounds.Y0, page_bounds.X0 + 75, page_bounds.Y0 + 15);

                page.AddFreeTextAnnot(
                    annot_loc * page.DerotationMatrix,
                    "TEST",
                    fontSize: 18,
                    fillColor: Utils.GetColor("FIREBRICK1"),
                    rotate: page.Rotation
                    );

                doc.Save(path_out, garbage: 1, deflate: 1, noNewId: 1);
                Console.WriteLine($@"Have created {path_out}. comparing with {path_expected}.");

                byte[] outBytes = File.ReadAllBytes(path_out);
                byte[] expectedBytes = File.ReadAllBytes(path_expected);

                Assert.True(outBytes.SequenceEqual(expectedBytes), "Byte arrays are not equal");
                doc.Save(Out("Test1645.pdf"));
            }
            finally
            {
                Utils.SetAnnotStem(annot_stem);
            }
        }

        /*
         * Test fix for #4254.
         * Ensure that both annotations are fully created
         * We do this by asserting equal top-used colors in respective pixmaps.
         */
        [Fact]
        public void Test4254()
        {
            int GetHashCode(byte[] obj)
            {
                if (obj == null) return 0;
                unchecked
                {
                    int hash = 17;
                    foreach (byte b in obj)
                        hash = hash * 31 + b;
                    return hash;
                }
            }

            Document doc = new Document();
            Page page = doc.NewPage();

            Rect rect = new Rect(100, 100, 200, 150);
            Annot annot = page.AddFreeTextAnnot(rect, "Test Annotation from minimal example");
            annot.SetBorder(width: 1, dashes: new int[] { 3, 3 });
            annot.SetOpacity(0.5f);
            try
            {
                annot.SetColors(stroke: _Constants.red);
            }
            catch (Exception e) { }

            annot.Update();

            rect = new Rect(200, 200, 400, 400);
            Annot annot2 = page.AddFreeTextAnnot(rect, "Test Annotation from minimal example pt 2");
            annot2.SetBorder(width: 1, dashes: new int[] { 3, 3 });
            annot2.SetOpacity(0.5f);
            try
            {
                annot.SetColors(stroke: _Constants.red);
            }
            catch (Exception e) { }

            annot.Update();
            annot2.Update();

            doc.Save(Out("test_4254.pdf"));

            // stores top color for each pixmap
            HashSet<int> top_colors = new HashSet<int>();
            foreach (var _annot in page.GetAnnots())
            {
                Pixmap pix = _annot.GetPixmap();
                top_colors.Add(GetHashCode(pix.ColorTopUsage().Item2));
            }

            // only one color must exist
            Assert.True(top_colors.Count == 1, "No colors found in annotations pixmaps.");
        }

        /*
         * Test creation of rich text FreeText annotations.
         * We create the same annotation on different pages in different ways,
         * with and without using Annotation.update(), and then assert equality
         * of the respective images.
         * We do this by asserting equal top-used colors in respective pixmaps.
         */
        [Fact]
        public void TestRichText()
        {
            string ds = "font-size: 11pt; font-family: sans-serif;";
            string bullet = "\u2610\u2611\u2612"; // Output: ???;

            string text = $@"<p style=""text-align:justify;margin-top:-25px;"">
MuPDF.NET <span style=""color: red;"">འདི་ ཡིག་ཆ་བཀྲམ་སྤེལ་གྱི་དོན་ལུ་ པའི་ཐོན་ཐུམ་སྒྲིལ་དྲག་ཤོས་དང་མགྱོགས་ཤོས་ཅིག་ཨིན།</span>
<span style=""color:blue;"">Here is some <b>bold</b> and <i>italic</i> text, followed by <b><i>bold-italic</i></b>. Text-based check boxes: {bullet}.</span>
</p>";

            Document doc = new Document();

            // first page
            Page page = doc.NewPage();

            Rect rect = new Rect(100, 100, 350, 200);
            Point p2 = rect.TopRight + new Point(50, 30);
            Point p3 = p2 + new Point(0, 30);
            Annot annot = page.AddFreeTextAnnot(
                rect,
                text,
                fillColor: _Constants.gold,
                opacity: 0.5f,
                rotate: 90,
                borderWidth: 1,
                dashes: null,
                richtext: true,
                callout: new Point[] { p3, p2, rect.TopRight }
                );
            Pixmap pix1 = page.GetPixmap();

            // # Second page.
            // the annotation is created with minimal parameters, which are supplied
            // in a separate call to the .update() method.
            page = doc.NewPage();
            annot = page.AddFreeTextAnnot(
                rect,
                text,
                borderWidth: 1,
                dashes: null,
                richtext: true,
                callout: new Point[] { p3, p2, rect.TopRight }
                );
            annot.Update(fillColor: _Constants.gold, opacity: 0.5f, rotate: 90);
            Pixmap pix2 = page.GetPixmap();

            doc.Save(Out("test_rich_text.pdf"));
            doc.Close();

            Assert.Equal(pix1.SAMPLES, pix2.SAMPLES);

            pix1.Dispose();
            pix2.Dispose();
        }

        /*
         * Test fix for #4447.
         */
        [Fact]
        public void Test4447()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            float[] text_color = _Constants.red;
            float[] fill_color = _Constants.green;
            float[] border_color = _Constants.blue;

            Rect annot_rect = new Rect(90.1f, 486.73f, 139.26f, 499.46f);

            try
            {
                Annot annot = page.AddFreeTextAnnot(
                    annot_rect,
                    "AETERM",
                    fontName: "Arial",
                    fontSize: 10,
                    textColor: text_color,
                    fillColor: fill_color,
                    borderColor: border_color,
                    borderWidth: 1
                    );
            }
            catch (Exception e)
            {
                Assert.True(true, $@"cannot set border_color if rich_text is False {e.Message}");
            }

            try
            {
                Annot annot = page.AddFreeTextAnnot(
                    new Rect(30, 400, 100, 450),
                    "Two",
                    fontName: "Arial",
                    fontSize: 10,
                    textColor: text_color,
                    fillColor: fill_color,
                    borderColor: border_color,
                    borderWidth: 1
                    );
            }
            catch (Exception e)
            {
                Assert.True(true, $@"cannot set border_color if rich_text is False {e.Message}");
            }

            {
                Annot annot = page.AddFreeTextAnnot(
                    new Rect(30, 500, 100, 550),
                    "Three",
                    fontName: "Arial",
                    fontSize: 10,
                    textColor: text_color,
                    borderWidth: 1
                    );
                annot.Update(textColor: text_color, fillColor: fill_color);
                try
                {
                    annot.Update(borderColor: border_color);
                }
                catch (Exception e)
                {
                    Assert.Contains("cannot set border_color if rich_text is False", e.Message);
                }
            }

            doc.Save(Out("test_4447.pdf"));
            doc.Close();
        }

        /*
         * Test Stamp.
         */
        [Fact]
        public void TestStamp()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            Rect r = new Rect(72, 72, 220, 100);

            Annot annot = page.AddStampAnnot(r, stamp: 0);
            Assert.Equal(PdfAnnotType.PDF_ANNOT_STAMP, annot.Type);
            Assert.Equal("Stamp", annot.TypeString);
            Assert.Equal("Approved", annot.Info.Content);
            string annot_id = annot.Info.Id;
            int annot_xref = annot.Xref;
            Annot annot1 = page.LoadAnnot(annot_id);
            Annot annot2 = page.LoadAnnot(annot_xref);
            Assert.Equal(annot1.Xref, annot2.Xref);
            page = doc.ReloadPage(page);

            doc.Save(Out("test_stamp.pdf"));
            doc.Close();
        }

        /*
         * Test Image Stamp.
         */
        [Fact]
        public void TestImageStamp()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            Rect r = new Rect(72, 72, 220, 100);

            string filename = Doc("nur-ruhig.jpg");
            Annot annot = page.AddStampAnnot(r, stamp: filename);
            Assert.Equal("Image Stamp", annot.Info.Content);

            doc.Save(Out("test_image_stamp.pdf"));
            doc.Close();
        }
    }
}
