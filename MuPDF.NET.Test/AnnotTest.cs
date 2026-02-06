using ICSharpCode.SharpZipLib.Zip.Compression;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MuPDF.NET;

namespace MuPDF.NET.Test
{
    public class AnnotTest
    {
        [Test]
        public void Annot_CleanContents()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Annot annot = page.AddHighlightAnnot(new Rect(10, 10, 20, 20));

            annot.CleanContents();

            Assert.That(Encoding.UTF8.GetString(annot.GetAP()).StartsWith("q"), Is.EqualTo(true));
        }

        [Test]
        public void Test_PdfString()
        {
            Utils.GetPdfNow();
            Utils.GetPdfString("Beijing, chinesisch 北京");
            Utils.GetTextLength("Beijing, chinesisch 北京", "null", fontName: "china-s");
            Utils.GetPdfString("Latin characters êßöäü");
        }

        [Test]
        public void TestCaret()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Rect r = new Rect(72, 72, 220, 100);
            Annot annot = page.AddCaretAnnot(r.TopLeft);

            Assert.That(annot.Type.Item2, Is.EqualTo("Caret"));
            Assert.That((int)annot.Type.Item1, Is.EqualTo(14));

            annot.Update(rotate: 20);

            page.GetAnnotNames();
            page.GetAnnotXrefs();
        }

        [Test]
        public void TestFreeText1()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Annot annot = page.AddFreeTextAnnot(
                Constants.r,
                Constants.t1,
                fontSize: 10,
                rotate: 90,
                textColor: new float[] { 0, 0, 1 },
                align: (int)TextAlign.TEXT_ALIGN_CENTER
            );

            annot.SetBorder(border: null, width: 0.3f, dashes: new int[] { 2 });
            annot.Update(textColor: new float[] { 0, 0, 1 }, fillColor: new float[] { 0, 1, 1 });

            Assert.That((int)annot.Type.Item1, Is.EqualTo(2));
            Assert.That(annot.Type.Item2, Is.EqualTo("FreeText"));

            page.Dispose();
            doc.Save(@"TestFreeText1.pdf");
            doc.Close();
        }

        [Test]
        public void TestFreeText2()
        {
            string ds = "font-size: 11pt; font-family: sans-serif;";
            // some special characters
            string bullet = "\u2610\u2611\u2612"; // Output: ☐☑☒

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
                fillColor: Constants.gold,  // fill color
                opacity: 1,  // non-transparent
                rotate: 0,  // no rotation
                borderWidth: 1,  // border and callout line width
                dashes: null,  // no dashing
                richtext: true,  // this is rich text
                style: ds,  // my styling default
                callout: new Point[] { p3, p2, rect.TopRight },  // define end, knee, start points
                lineEnd: PdfLineEnding.PDF_ANNOT_LE_OPEN_ARROW,  // symbol shown at p3
                borderColor: Constants.green
            );

            Assert.That(annot.GetText(page).Length, Is.EqualTo(206));

            page.Dispose();
            doc.Save(@"TestFreeText2.pdf");
            doc.Close();
        }

        [Test]
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

            Assert.That((int)annot.Type.Item1, Is.EqualTo(17));
        }

        [Test]
        public void Redact1()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Annot annot = page.AddRedactAnnot(new Rect(72, 72, 200, 200).Quad, text: "Hello");
            annot.Update(rotate: -1);
            Assert.That((int)annot.Type.Item1, Is.EqualTo(12));

            annot.GetPixmap();
            AnnotInfo info = annot.Info;
            annot.SetInfo(info);
            Assert.That(annot.HasPopup, Is.False);

            annot.SetPopup(new Rect(72, 72, 100, 100));
            Rect s = annot.PopupRect;

            Assert.That(s.Abs(), Is.EqualTo(new Rect(72, 72, 100, 100).Abs()));
            page.ApplyRedactions();
        }

        [Test]
        public void Redact2()
        {
            Document doc = new Document("../../../resources/symbol-list.pdf");
            Page page = doc[0];
            List<WordBlock> allText = page.GetText("words");
            page.AddRedactAnnot(page.Rect.Quad);
            page.ApplyRedactions(text: 0);
            List<WordBlock> t = page.GetText("words");

            Assert.That(t.Count, Is.EqualTo(0));
            Assert.That(page.GetDrawings().Count, Is.Zero);
        }

        [Test]
        public void Redact3()
        {
            Document doc = new Document("../../../resources/symbol-list.pdf");
            Page page = doc[0];
            List<PathInfo> arts = page.GetDrawings();
            page.AddRedactAnnot(page.Rect.Quad);
            page.ApplyRedactions(graphics: 0);

            Assert.That(page.GetText("words").Count, Is.Zero);
            Assert.That(arts.Count, Is.EqualTo(page.GetDrawings().Count));
        }

        [Test]
        public void FirstAnnot()
        {
            Document doc = new Document("../../../resources/annots.pdf");
            Page page = doc[0];
            Annot firstAnnot = (new List<Annot>(page.GetAnnots()))[0];
            Annot next = firstAnnot.Next;
        }

        [Test]
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

                Assert.That(annot.Type.Item1, Is.EqualTo(PdfAnnotType.PDF_ANNOT_LINE));
            }
            page.Dispose();
            doc.Save(@"AddLineAnnot.pdf"); // Save the modified document
            doc.Close();
        }

        /*
         * Test fix for #1645.
         * The expected output files assume annot_stem is 'jorj'. We need to always
         * restore this before returning (this is checked by conftest.py).
         */
        [Test]
        public void Test1645()
        {
            string annot_stem = Utils.ANNOT_ID_STEM;
            Utils.SetAnnotStem("jorj");
            try
            {
                string path_in = "../../../resources/symbol-list.pdf";
                string path_expected = "../../../resources/test_1645_expected.pdf";
                string path_out = "test_1645_out.pdf";
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

                Assert.IsTrue(outBytes.SequenceEqual(expectedBytes), "Byte arrays are not equal");
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
        [Test]
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
                annot.SetColors(stroke: new float[] { 1, 0, 0 });
            }
            catch (Exception e) {}
            
            annot.Update();

            rect = new Rect(200, 200, 400, 400);
            Annot annot2 = page.AddFreeTextAnnot(rect, "Test Annotation from minimal example pt 2");
            annot2.SetBorder(width: 1, dashes: new int[] { 3, 3 });
            annot2.SetOpacity(0.5f);
            try
            {
                annot.SetColors(stroke: new float[] { 1, 0, 0 });
            }
            catch (Exception e) { }

            annot.Update();
            annot2.Update();

            doc.Save("test_4254.pdf");

            // stores top color for each pixmap
            HashSet<int> top_colors = new HashSet<int>();
            foreach (var _annot in page.GetAnnots())
            {
                Pixmap pix = _annot.GetPixmap();
                top_colors.Add(GetHashCode(pix.ColorTopUsage().Item2));
            }

            // only one color must exist
            Assert.IsTrue(top_colors.Count == 1, "No colors found in annotations pixmaps.");
        }

        /*
         * Test creation of rich text FreeText annotations.
         * We create the same annotation on different pages in different ways,
         * with and without using Annotation.update(), and then assert equality
         * of the respective images.
         * We do this by asserting equal top-used colors in respective pixmaps.
         */
        [Test]
        public void TestRichText()
        {
            string ds = "font-size: 11pt; font-family: sans-serif;";
            string bullet = "\u2610\u2611\u2612"; // Output: ☐☑☒;

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
                fillColor: Constants.gold,
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
            annot.Update(fillColor: Constants.gold, opacity: 0.5f, rotate: 90);
            Pixmap pix2 = page.GetPixmap();

            doc.Save("test_rich_text.pdf");
            doc.Close();

            Assert.That(pix1.SAMPLES, Is.EqualTo(pix2.SAMPLES));

            pix1.Dispose();
            pix2.Dispose();
        }

        /*
         * Test fix for #4447.
         */
        [Test]
        public void Test4447()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            float[] text_color = Constants.red;
            float[] fill_color = Constants.green;
            float[] border_color = Constants.blue;

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
                Assert.That(true, $@"cannot set border_color if rich_text is False {e.Message}");
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
                Assert.That(true, $@"cannot set border_color if rich_text is False {e.Message}");
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
                    Assert.That(true, e.Message, Does.Contain("cannot set border_color if rich_text is False"));
                }
            }

            doc.Save("test_4447.pdf");
            doc.Close();
        }

        /*
         * Test Stamp.
         */
        [Test]
        public void TestStamp()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            Rect r = new Rect(72, 72, 220, 100);

            Annot annot = page.AddStampAnnot(r, stamp: 0);
            Assert.That(annot.Type.Item1, Is.EqualTo(PdfAnnotType.PDF_ANNOT_STAMP));
            Assert.That(annot.Type.Item2, Is.EqualTo("Stamp"));
            Assert.That(annot.Info.Content, Is.EqualTo("Approved"));
            string annot_id = annot.Info.Id;
            int annot_xref = annot.Xref;
            Annot annot1 = page.LoadAnnot(annot_id);
            Annot annot2 = page.LoadAnnot(annot_xref);
            Assert.That(annot1.Xref, Is.EqualTo(annot2.Xref));
            page = doc.ReloadPage(page);

            doc.Save("test_stamp.pdf");
            doc.Close();
        }

        /*
         * Test Image Stamp.
         */
        [Test]
        public void TestImageStamp()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            Rect r = new Rect(72, 72, 220, 100);

            string filename = "../../../resources/nur-ruhig.jpg";
            Annot annot = page.AddStampAnnot(r, stamp: filename);
            Assert.That(annot.Info.Content, Is.EqualTo("Image Stamp"));

            doc.Save("test_image_stamp.pdf");
            doc.Close();
        }
    }
}
