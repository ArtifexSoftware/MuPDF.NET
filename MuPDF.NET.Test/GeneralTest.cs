﻿using System;
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
            Document doc = new Document();
            MuPDFPage page = doc.NewPage();

            Annot annot1 = page.AddCircleAnnot(new Rect(50, 50, 100, 100));
            annot1.SetColors(fill: new float[] { 1, 0, 0 }, stroke: new float[] { 1, 0, 0 });
            annot1.SetOpacity(2 / 3.0f);
            annot1.Update(blendMode: "Multiply");

            Annot annot2 = page.AddCircleAnnot(new Rect(75, 75, 125, 125));
            annot2.SetColors(fill: new float[] { 0, 0, 1 }, stroke: new float[] { 0, 0, 1 });
            annot2.SetOpacity(1 / 3.0f);
            annot2.Update(blendMode: "Multiply");

            doc.Save("output.pdf", expand: 1, pretty: 1);                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               
        }

        [Test]
        public void TestWrapContents()
        {
            Document doc = new Document("../../../resources/toc.pdf");
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
            Document doc = new Document();
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
                Document doc = new Document("../../../resources/" + name);
                MuPDFPage page = doc[0];
                float size0 = page.GetTextTrace()[0].Size;
                float size1 = page.GetText("dict", flags: (int)TextFlagsExtension.TEXTFLAGS_TEXT).Blocks[0].Lines[0].Spans[0].Size;

                Assert.That(Math.Abs(size0 - size1), Is.LessThan(1e-5));
            }
        }

        [Test]
        public void TestFontSize()
        {
            Document doc = new Document();
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
            Document doc = new Document("../../../resources/test_2596.pdf");
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
            Document doc = new Document();
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

        [Test]
        public void Insert()
        {
            Document doc = new Document();
            MuPDFPage page = doc.NewPage();

            Rect r1 = new Rect(50, 50, 100, 100);
            Rect r2 = new Rect(50, 150, 200, 400);
            page.InsertImage(r1, filename: "../../../resources/nur-ruhig.jpg");
            page.InsertImage(r1, filename: "../../../resources/nur-ruhig.jpg", rotate: 270);
            List<Block> lists = page.GetImageInfo();
            Assert.That(lists.Count, Is.EqualTo(2));

            Rect bbox1 = new Rect(lists[0].Bbox);
            Rect bbox2 = new Rect(lists[1].Bbox);
        }

        [Test]
        public void Compress()
        {
            Document doc = new Document("../../../resources/2.pdf");
            Document npdf = new Document();
            for (int i = 0; i < doc.PageCount; i++)
            {
                Pixmap pixmap = doc[i].GetPixmap(colorSpace: "RGB", dpi: 72, annots: false);
                MuPDFPage pageNew = npdf.NewPage();
                pageNew.InsertImage(rect: pageNew.GetBound(), pixmap: pixmap);
            }
            npdf.Save("2.pdf.compress.pdf", garbage: 3, deflate: 1, deflateImages: 1, deflateFonts: 1, pretty: 1);
        }

        [Test]
        public void PageLinksGenerator()
        {
            Document doc = new Document("../../../resources/2.pdf");
            MuPDFPage page = doc[-1];

            List<Link> links = page.GetLinks();
            Assert.That(links.Count, Is.EqualTo(7));
        }

        [Test]
        public void Deletion()
        {
            Document doc = new Document();
            Link link = new Link()
            {
                From = new Rect(100, 100, 120, 120),
                Kind = LinkType.LINK_GOTO,
                Page = 5,
                To = new Point(100, 100)
            };

            List<Toc> tocs = new List<Toc>();

            for (int i = 0; i < 100; i ++)
            {
                MuPDFPage page = doc.NewPage();
                page.InsertText(new Point(100, 100), $"{i}", fontFile: "../../../resources/kenpixel.ttf");
                if (i > 5)
                    page.InsertLink(link);
                tocs.Add(new Toc() { Level = 1, Title = $"{i}", Page = i + 1 });
            }
            doc.SetToc(tocs);
            Assert.That(doc.HasLinks());
        }

        [Test]
        public void DeletePages()
        {
            Document doc = new Document("../../../resources/cython.pdf");
            int[] pages = { 3, 3, 3, 2, 3, 1, 0, 0 };
            doc.Select(new List<int>(pages));
            Assert.That(doc.PageCount, Is.EqualTo(8));
        }

        [Test]
        public void SetLabels()
        {
            Document doc = new Document();
            for (int i = 0; i < 10; i++)
                doc.NewPage();

            List<Label> labels = new List<Label>();
            labels.Add(new Label() { StartPage = 0, Prefix = "A-", Style = "D", FirstPageNum = 1 });
            labels.Add(new Label() { StartPage = 4, Prefix = "", Style = "R", FirstPageNum = 1 });

            doc.SetPageLabels(labels);
            List<string> pageLabels = new List<string>();
            for (int i = 0; i < doc.PageCount; i ++)
            {
                string l = doc[i].GetLabel();
                pageLabels.Add(l);
            }

            string[] answers = { "A-1", "A-2", "A-3", "A-4", "I", "II", "III", "IV", "V", "VI" };
            Assert.That(pageLabels.SequenceEqual(answers));
        }

        [Test]
        public void SetLabelA()
        {
            Document doc = new Document();
            for (int i = 0; i < 10; i++)
                doc.NewPage();

            List<Label> labels = new List<Label>();
            labels.Add(new Label() { StartPage = 0, Prefix = "", Style = "a", FirstPageNum = 1 });
            labels.Add(new Label() { StartPage = 5, Prefix = "", Style = "A", FirstPageNum = 1 });

            doc.SetPageLabels(labels);
            byte[] pdfdata = doc.Write();
            doc.Close();

            doc = new Document("pdf", pdfdata);
            string[] answer = { "a", "b", "c", "d", "e", "A", "B", "C", "D", "E" };

            List<string> pageLabels = new List<string>();
            for (int i = 0; i < doc.PageCount; i++)
            {
                string l = doc[i].GetLabel();
                pageLabels.Add(l);
            }
            Assert.That(pageLabels.SequenceEqual(answer));
        }

        [Test]
        public void Search1()
        {
            Document doc = new Document("../../../resources/2.pdf");
            MuPDFPage page = doc[0];
            string needle = "mupdf";
            List<Quad> qlist = page.SearchFor(needle);
            Assert.That(qlist.Count, Is.Not.Zero);
            foreach (Quad q in qlist)
            {
                Assert.That(page.GetTextbox(q.Rect).ToLower(), Is.Not.Null);
            }
        }

        [Test]
        public void Encryption()
        {
            string text = "some secret information";
            int perm = (int)(mupdf.mupdf.PDF_PERM_ACCESSIBILITY
                | mupdf.mupdf.PDF_PERM_PRINT
                | mupdf.mupdf.PDF_PERM_COPY
                | mupdf.mupdf.PDF_PERM_ANNOTATE);
            string ownerPass = "owner";
            string userPass = "user";
            int encryptMeth = mupdf.mupdf.PDF_ENCRYPT_AES_256;

            Document doc = new Document();
            MuPDFPage page = doc.NewPage();
            page.InsertText(new Point(50, 72), text, fontFile: "../../../resources/kenpixel.ttf");
            byte[] tobytes = doc.Write(
                encryption: encryptMeth,
                ownerPW: ownerPass,
                userPW: userPass,
                permissions: perm);
            doc.Close();
            doc = new Document("pdf", tobytes);
            Assert.That(doc.NeedsPass);
            Assert.That(doc.IsEncrypted);
            int rc = doc.Authenticate("owner");
            Assert.That(rc, Is.EqualTo(4));
            Assert.That(doc.IsEncrypted, Is.False);
            doc.Close();
            doc = new Document("pdf", tobytes);
            rc = doc.Authenticate("user");
            Assert.That(rc, Is.EqualTo(2));
        }

        [Test]
        public void HasLinks()
        {
            Document doc = new Document("../../../resources/toc.pdf");
            Assert.That(doc.HasLinks(), Is.False);
        }

        [Test]
        public void IsRepaired()
        {
            Document doc = new Document("../../../resources/toc.pdf");
            Assert.That(doc.IsRepaired, Is.False);
        }

        [Test]
        public void RemoveRotation()
        {
            Document doc = new Document("../../../resources/test-2812.pdf");
            for (int i = 1; i < doc.PageCount; i ++) // because of first page's rotation is zero
            {
                Assert.That(doc[i].Rotation, Is.Not.Zero);
                doc[i].RemoveRotation();
                Assert.That(doc[i].Rotation, Is.Zero);
            }
        }

        [Test]
        public void CompareWords()
        {
            Document doc = new Document("../../../resources/test-707673.pdf");
            MuPDFPage page = doc[0];
            List<WordBlock> words0 = page.GetText("words");
            page.CleanContetns();
            List<WordBlock> words1 = page.GetText("words");
            Assert.That(words0.Count, Is.EqualTo(words1.Count));
        }

        [Test]
        public void Rotation1()
        {
            Document doc = new Document();
            MuPDFPage page = doc.NewPage();
            page.SetRotation(270);
            Assert.That(doc.GetKeyXref(page.Xref, "Rotate").Item1, Is.EqualTo("int"));
            Assert.That(doc.GetKeyXref(page.Xref, "Rotate").Item2, Is.EqualTo("270"));
        }

        [Test]
        public void Rotation2()
        {
            Document doc = new Document();
            MuPDFPage page = doc.NewPage();
            doc.SetKeyXRef(page.Xref, "Rotate", "270");
            Assert.That(page.Rotation, Is.EqualTo(270));
        }

        [Test]
        public void ValidName()
        {
            Document doc = new Document();
            MuPDFPage page = doc.NewPage();
            doc.SetKeyXRef(page.Xref, "Rotate", "90");
            Assert.That(page.Rotation, Is.EqualTo(90));

            doc.SetKeyXRef(page.Xref, "my_rotate/something", "90");
            Assert.That(doc.GetKeyXref(page.Xref, "my_rotate/something").Item2, Is.EqualTo("90"));
        }
    }
}