using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    internal class GeneralTest
    {
        [Test]
        public void Test_Opacity()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();

            MuPDFAnnot annot1 = page.AddCircleAnnot(new Rect(50, 50, 100, 100));
            annot1.SetColors(fill: new float[] { 1, 0, 0 }, stroke: new float[] { 1, 0, 0 });
            annot1.SetOpacity(2 / 3.0f);
            annot1.Update(blendMode: "Multiply");

            MuPDFAnnot annot2 = page.AddCircleAnnot(new Rect(75, 75, 125, 125));
            annot2.SetColors(fill: new float[] { 0, 0, 1 }, stroke: new float[] { 0, 0, 1 });
            annot2.SetOpacity(1 / 3.0f);
            annot2.Update(blendMode: "Multiply");

            doc.Save("output.pdf", expand: 1, pretty: 1);                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               
        }

        [Test]
        public void TestWrapContents()
        {
            MuPDFDocument doc = new MuPDFDocument("../../../resources/toc.pdf");
            MuPDFPage page = doc[0];
            page.WrapContents();
            int xref = page.GetContents()[0];
            byte[] cont = page.ReadContents();

            doc.UpdateStream(xref, cont);
            page.SetContents(xref);
            Assert.That(page.GetContents().Count, Is.EqualTo(1));;
            page.CleanContetns();
        }

        [Test]
        public void TestPageCleanContents()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();
            page.DrawRect(new Rect(10, 10, 20, 20));
            page.DrawRect(new Rect(20, 20, 30, 30));
            Assert.That(page.GetContents().Count, Is.EqualTo(2));
            Assert.That(Encoding.UTF8.GetString(page.ReadContents()).StartsWith("q"), Is.False);

            page.CleanContetns();
            Assert.That(page.GetContents().Count, Is.EqualTo(1));
            Assert.That(Encoding.UTF8.GetString(page.ReadContents()).StartsWith("q"), Is.True);
        }

        [Test]
        public void TestGetText()
        {
            string[] files = { "test_2645_1.pdf", "test_2645_2.pdf", "test_2645_3.pdf" };
            foreach (string name in files)
            {
                MuPDFDocument doc = new MuPDFDocument("../../../resources/" + name);
                MuPDFPage page = doc[0];
                float size0 = page.GetTextTrace()[0].Size;
                float size1 = page.GetText("dict", flags: (int)TextFlagsExtension.TEXTFLAGS_TEXT).Blocks[0].Lines[0].Spans[0].Size;

                Assert.That(Math.Abs(size0 - size1), Is.LessThan(1e-5));
            }
        }

        [Test]
        public void TestFontSize()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();
            Point point = new Point(100, 300);
            float fontSize = 11f;
            string text = "Hello";
            int[] angles = { 0, 30, 60, 90, 120 };

            foreach (int angle in angles)
            {
                page.InsertText(point, text, fontFile: "../../../resources/kenpixel.ttf", fontSize: fontSize, morph: new Morph(point, new Matrix(angle)));
            }

            foreach (SpanInfo span in page.GetTextTrace())
            {
                Assert.That(span.Size, Is.EqualTo(fontSize));
            }

            foreach (Block block in page.GetText("dict").Blocks)
            {
                foreach (Line line in block.Lines)
                {
                    foreach (Span span in line.Spans)
                    {
                        Assert.That(span.Size, Is.EqualTo(fontSize));
                    }
                }
            }
        }

        [Test]
        public void Reload()
        {
            MuPDFDocument doc = new MuPDFDocument("../../../resources/test_2596.pdf");
            MuPDFPage page = doc[0];
            Pixmap pix0 = page.GetPixmap();
            doc.Write(garbage: true);

            page = doc.ReloadPage(page);
            Pixmap pix1 = page.GetPixmap();
            Assert.That(pix1.SAMPLES.SequenceEqual(pix0.SAMPLES), Is.True);
        }

        [Test]
        public void Cropbox()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();

            doc.SetKeyXRef(page.Xref, "MediaBox", "[-30 -20 595 842]");
            Assert.That(page.CropBox.EqualTo(new Rect(-30, 0, 595, 862)));
            Assert.That(page.Rect.EqualTo(new Rect(0, 0, 625, 862)));

            page.SetCropBox(new Rect(-20, 0, 595, 852));
            Assert.That(doc.GetKeyXref(page.Xref, "CropBox").Item2, Is.EqualTo("[-20 -10 595 842]"));

            bool error = false;
            string text = "";
            try
            {
                page.SetCropBox(new Rect(-35, -10, 595, 852));
            }
            catch (Exception ex)
            {
                text = ex.Message;
                error = true;
            }
            Assert.That(error);
            Assert.That(text, Is.EqualTo("CropBox not in Mediabox"));
        }
    }
}
