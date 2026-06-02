using MuPDF.NET.enums;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class GeneralTest : PdfTestBase
    {
        private const string TestClassName = nameof(GeneralTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void SearchText()
        {
            Document doc = new Document(Doc("test_2533.pdf"));
            Page page = doc[0];
            string needle = "徽";
            int iNeedle = Convert.ToInt32(needle[0]);
            Rect bbox = new Rect();
            foreach (SpanInfo span in page.GetTextTrace())
            {
                foreach (Char ch in span.Chars)
                {
                    if (ch.UCS == iNeedle)
                        bbox = new Rect(ch.Bbox);
                }
            }

            Rect bbox1 = page.SearchFor(needle, page.GetBound())[0].Rect;
            IRect ibbox = bbox.Round();
            IRect ibbox1 = bbox1.Round();
            Assert.Equal(ibbox, ibbox1);

            Assert.Equal(0, page.SearchFor("偿力很务").Count);
            doc.Close();
        }
        
        [Fact]
        public void Test_Opacity()
        {
            Document doc = new Document(Doc("test_2533.pdf"));
            Page page = doc.NewPage();

            Annot annot1 = page.AddCircleAnnot(new Rect(50, 50, 100, 100));
            annot1.SetColors(fill: new float[] { 1, 0, 0 }, stroke: new float[] { 1, 0, 0 });
            annot1.SetOpacity(2 / 3.0f);
            annot1.Update(blendMode: "Multiply");

            Annot annot2 = page.AddCircleAnnot(new Rect(75, 75, 125, 125));
            annot2.SetColors(fill: new float[] { 0, 0, 1 }, stroke: new float[] { 0, 0, 1 });
            annot2.SetOpacity(1 / 3.0f);
            annot2.Update(blendMode: "Multiply");

            doc.Save(Out("Test_Opacity.pdf"), expand: 1, pretty: 1);
            doc.Close();
        }
        /*
        [Fact]
        public void TestWrapContents()
        {
            Document doc = new Document(Doc("toc.pdf"));
            Page page = doc[0];
            page.WrapContents();
            int xref = page.GetContents()[0];
            byte[] cont = page.ReadContents();

            doc.UpdateStream(xref, cont);
            page.SetContents(xref);
            Assert.Equal(1, page.GetContents().Count);
            page.CleanContetns();
            doc.Close();
        }
        
        [Fact]
        public void TestPageCleanContents()
        {
            Document doc = new Document(Doc("test_2533.pdf"));
            Page page = doc.NewPage();
            page.DrawRect(new Rect(10, 10, 20, 20));
            page.DrawRect(new Rect(20, 20, 30, 30));
            Assert.Equal(2, page.GetContents().Count);
            Assert.That(Encoding.UTF8.GetString(page.ReadContents()).StartsWith("q"), Is.False);

            page.CleanContetns();
            Assert.Equal(1, page.GetContents().Count);
            Assert.That(Encoding.UTF8.GetString(page.ReadContents()).StartsWith("q"), Is.True);
        }
        
        [Fact]
        public void TestGetText()
        {
            string[] files = { "test_2645_1.pdf", "test_2645_2.pdf", "test_2645_3.pdf" };
            foreach (string name in files)
            {
                Document doc = new Document(Doc(name));
                Page page = doc[0];
                float size0 = page.GetTextTrace()[0].Size;
                float size1 = page.GetText("dict", flags: (int)TextFlagsExtension.TEXTFLAGS_TEXT).Blocks[0].Lines[0].Spans[0].Size;

                Assert.True(Math.Abs(size0 - size1) < 1e-5);
                doc.Close();
            }
        }
        */
        [Fact]
        public void TestFontSize()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Point point = new Point(100, 300);
            float fontSize = 11f;
            string text = "Hello";
            int[] angles = { 0, 30, 60, 90, 120 };

            foreach (int angle in angles)
            {
                page.InsertText(point, text, fontFile: Doc("kenpixel.ttf"), fontSize: fontSize, morph: new Morph(point, new Matrix(angle)));
            }

            foreach (SpanInfo span in page.GetTextTrace())
            {
                Assert.Equal(fontSize, span.Size);
            }

            foreach (Block block in page.GetText("dict").Blocks)
            {
                foreach (Line line in block.Lines)
                {
                    foreach (Span span in line.Spans)
                    {
                        Assert.Equal(fontSize, span.Size);
                    }
                }
            }
            doc.Save(Out("TestFontSize.pdf"));
            doc.Close();
        }
        
        [Fact]
        public void Reload()
        {
            Document doc = new Document(Doc("test_2596.pdf"));
            Page page = doc[0];
            Pixmap pix0 = page.GetPixmap();
            doc.Write(garbage: true);

            page = doc.ReloadPage(page);
            Pixmap pix1 = page.GetPixmap();
            Assert.True(pix1.SAMPLES.SequenceEqual(pix0.SAMPLES));
        }

        [Fact]
        public void Cropbox()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            doc.SetKeyXRef(page.Xref, "MediaBox", "[-30 -20 595 842]");
            Assert.True(page.CropBox.EqualTo(new Rect(-30, 0, 595, 862)));
            Assert.True(page.Rect.EqualTo(new Rect(0, 0, 625, 862)));

            page.SetCropBox(new Rect(-20, 0, 595, 852));
            Assert.Equal("[-20 -10 595 842]", doc.GetKeyXref(page.Xref, "CropBox").Item2);

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
            Assert.True(error);
            Assert.Equal("CropBox not in MediaBox", text);
            doc.Save(Out("Cropbox.pdf"));
        }

        [Fact]
        public void Insert()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            Rect r1 = new Rect(50, 50, 100, 100);
            Rect r2 = new Rect(50, 150, 200, 400);
            page.InsertImage(r1, filename: Doc("nur-ruhig.jpg"));
            page.InsertImage(r1, filename: Doc("nur-ruhig.jpg"), rotate: 270);
            List<Block> lists = page.GetImageInfo();
            Assert.Equal(2, lists.Count);

            Rect bbox1 = new Rect(lists[0].Bbox);
            Rect bbox2 = new Rect(lists[1].Bbox);
            doc.Save(Out("Insert.pdf"));
        }

        [Fact]
        public void Compress()
        {
            Document doc = new Document(Doc("2.pdf"));
            Document npdf = new Document();
            for (int i = 0; i < doc.PageCount; i++)
            {
                Pixmap pixmap = doc[i].GetPixmap(colorSpace: "RGB", dpi: 72, annots: false);
                Page pageNew = npdf.NewPage();
                pageNew.InsertImage(rect: pageNew.GetBound(), pixmap: pixmap);
            }
            npdf.Save(Out("Compress.pdf"), garbage: 3, deflate: 1, deflateImages: 1, deflateFonts: 1, pretty: 1);
        }

        [Fact]
        public void PageLinksGenerator()
        {
            Document doc = new Document(Doc("2.pdf"));
            Page page = doc[-1];

            List<LinkInfo> links = page.GetLinks();
            Assert.Equal(7, links.Count);
        }

        [Fact]
        public void Deletion()
        {
            Document doc = new Document();
            LinkInfo link = new LinkInfo()
            {
                From = new Rect(100, 100, 120, 120),
                Kind = LinkType.LINK_GOTO,
                Page = 5,
                To = new Point(100, 100)
            };

            List<Toc> tocs = new List<Toc>();

            for (int i = 0; i < 100; i ++)
            {
                Page page = doc.NewPage();
                page.InsertText(new Point(100, 100), $"{i}", fontFile: Doc("kenpixel.ttf"));
                if (i > 5)
                    page.InsertLink(link);
                tocs.Add(new Toc() { Level = 1, Title = $"{i}", Page = i + 1 });
            }
            doc.SetToc(tocs);
            Assert.True(doc.HasLinks());
            doc.Save(Out("Deletion.pdf"));
        }

        [Fact]
        public void DeletePages()
        {
            Document doc = new Document(Doc("cython.pdf"));
            int[] pages = { 3, 3, 3, 2, 3, 1, 0, 0 };
            doc.Select(new List<int>(pages));
            Assert.Equal(8, doc.PageCount);
        }

        [Fact]
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
            Assert.True(pageLabels.SequenceEqual(answers));
        }

        [Fact]
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
            Assert.True(pageLabels.SequenceEqual(answer));
        }

        [Fact]
        public void Search1()
        {
            Document doc = new Document(Doc("2.pdf"));
            Page page = doc[0];
            string needle = "mupdf";
            List<Quad> qlist = page.SearchFor(needle);
            Assert.True(qlist.Count > 0);
            foreach (Quad q in qlist)
            {
                Assert.NotNull(page.GetTextbox(q.Rect).ToLower());
            }
        }

        [Fact]
        public void Encryption()
        {
            string text = "some secret information";
            int perm = (int)(PdfAccess.PDF_PERM_ACCESSIBILITY
                | PdfAccess.PDF_PERM_PRINT
                | PdfAccess.PDF_PERM_COPY
                | PdfAccess.PDF_PERM_ANNOTATE);
            string ownerPass = "owner";
            string userPass = "user";
            int encryptMeth = (int)PdfCrypt.PDF_ENCRYPT_AES_256;

            Document doc = new Document(Doc("test_2533.pdf"));
            Page page = doc.NewPage();
            page.InsertText(new Point(50, 72), text, fontFile: Doc("kenpixel.ttf"));
            byte[] tobytes = doc.Write(
                encryption: encryptMeth,
                ownerPW: ownerPass,
                userPW: userPass,
                permissions: perm);
            doc.Close();
            doc = new Document("pdf", tobytes);
            Assert.True(doc.NeedsPass);
            Assert.True(doc.IsEncrypted);
            int rc = doc.Authenticate("owner");
            Assert.Equal(4, rc);
            Assert.False(doc.IsEncrypted);
            doc.Close();
            doc = new Document("pdf", tobytes);
            rc = doc.Authenticate("user");
            Assert.Equal(2, rc);
            doc.Save(Out("Encryption.pdf"));
        }

        [Fact]
        public void HasLinks()
        {
            Document doc = new Document(Doc("toc.pdf"));
            Assert.False(doc.HasLinks());
        }

        [Fact]
        public void IsRepaired()
        {
            Document doc = new Document(Doc("toc.pdf"));
            Assert.False(doc.IsRepaired);
        }

        [Fact]
        public void RemoveRotation()
        {
            Document doc = new Document(Doc("test-2812.pdf"));
            for (int i = 1; i < doc.PageCount; i ++) // because of first page's rotation is zero
            {
                Assert.NotEqual(0, doc[i].Rotation);
                doc[i].RemoveRotation();
                Assert.Equal(0, doc[i].Rotation);
            }
        }

        [Fact]
        public void CompareWords()
        {
            Document doc = new Document(Doc("test-707673.pdf"));
            Page page = doc[0];
            List<WordBlock> words0 = page.GetText("words");
            page.CleanContetns();
            List<WordBlock> words1 = page.GetText("words");
            Assert.Equal(words0.Count, words1.Count);
        }

        [Fact]
        public void Rotation1()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            page.SetRotation(270);
            Assert.Equal("int", doc.GetKeyXref(page.Xref, "Rotate").Item1);
            Assert.Equal("270", doc.GetKeyXref(page.Xref, "Rotate").Item2);
        }

        [Fact]
        public void Rotation2()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            doc.SetKeyXRef(page.Xref, "Rotate", "270");
            Assert.Equal(270, page.Rotation);
        }

        [Fact]
        public void ValidName()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            doc.SetKeyXRef(page.Xref, "Rotate", "90");
            Assert.Equal(90, page.Rotation);

            doc.SetKeyXRef(page.Xref, "my_rotate/something", "90");
            Assert.Equal("90", doc.GetKeyXref(page.Xref, "my_rotate/something").Item2);
        }

        [Fact]
        public void Epub()
        {
            //Document doc = new Document(Doc("test_3615.epub"));
            //Console.WriteLine(doc.PageMode);
            //Console.WriteLine(doc.PageLayout);
            Assert.True(true);
        }

        [Fact]
        public void GetPixmap()
        {
            Document doc = new Document(Doc("test_3727.pdf"));
            for (int i = 0; i < doc.PageCount; i ++)
                doc[i].GetPixmap(matrix: new Matrix(2, 2));
        }

        [Fact]
        public void Recolor()
        {
            Document doc = new Document(Doc("image-file1.pdf"));
            doc.LoadPage(0).Recolor(1);

            List<Entry> images = doc.GetPageImages(0);
            Assert.True(images[0].CsName.Equals("ICCBased"));
        }

        // Text extraction should fail because of PDF structure cycle.
        // Old MuPDF version did not detect the loop.
        [Fact]
        public void Test2548()
        {
            int len0 = Utils.MUPDF_WARNINGS_STORE.Count;
            Document doc = new Document(Doc("test_2548.pdf"));
            bool e = false;
            for (int i = 0; i < doc.PageCount; i++)
            {
                Page page = doc[i];
                try
                {
                    var text = page.GetText();
                    Console.WriteLine($"test_2548: text = {text}");
                }
                catch (Exception ee)
                {
                    Console.WriteLine($"test_2548: ee = {ee}");
                    // Expected: RuntimeError (cycle in structure tree)
                    e = true;
                }
            }
            int len1 = Utils.MUPDF_WARNINGS_STORE.Count;

            for (int i=len0; i<len1; i++)
            {
                Console.WriteLine($"test_2548(): {Utils.MUPDF_WARNINGS_STORE[i]}");
            }
        }

        [Fact]
        public void TestExtractTextWithLayout()
        {
            Document doc = new Document(Doc("columns.pdf"));
            Page page = doc[0];

            string textWithLayout = page.GetTextWithLayout(tolerance: 3);

            Assert.Equal(7412, textWithLayout.Length);

            page.Dispose();
            doc.Close();
        }
    }
}
