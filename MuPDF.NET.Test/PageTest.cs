using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using MuPDF.NET;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;

namespace MuPDF.NET.Test
{
    public class PageTest : PdfTestBase
    {
        private const string TestClassName = nameof(PageTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private Document doc;
        private Page page;

        public PageTest()
        {
            doc = new Document(Doc("toc.pdf"));
            page = doc[0];
        }

        [Fact]
        public void InsertPageAnnot()// passed but text was never drawn
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            // insert text
            int res = page.InsertText(new Point(100, 100), "hello", fontFile: Doc("kenpixel.ttf"), fontName: "kenpixel");
            
            // insert text box
            page.InsertTextbox(new Rect(300, 100, 400, 300), "hello", fontName: "kenpixel", fontFile: Doc("kenpixel.ttf"));

            // add react annot
            page.AddRectAnnot(new Rect(80, 80, 150, 120));
            // add underline annot
            page.AddUnderlineAnnot((new Rect(80, 80, 150, 120)).Quad);
            // insert circle annot
            page.AddCircleAnnot(new Rect(100, 150, 500, 550));

            // insert free text annot
            Rect r = new Rect(150, 650, 200, 700);
            page.AddFreeTextAnnot(r, "Hello World");
            // add highlight annot
            page.AddHighlightAnnot(new Quad(new Rect(120, 630, 200, 700)));
            // add strike annot
            page.AddStrikeoutAnnot((new Rect(120, 630, 200, 700).Quad), new Point(120, 630), new Point(200, 650));

            // add polygon annot
            List<Point> points = new List<Point>();
            points.Add(new Point(200, 200));
            points.Add(new Point(400, 200));
            points.Add(new Point(500, 350));
            points.Add(new Point(400, 500));
            points.Add(new Point(200, 500));
            points.Add(new Point(100, 350));
            points.Add(new Point(200, 200));
            Annot annot = page.AddPolygonAnnot(points);
            annot.SetBorder(width: 0.3f, dashes: new int[] { 2 });
            annot.SetColors(stroke: _Constants.blue, fill: _Constants.gold);
            annot.SetLineEnds(PdfLineEnding.PDF_ANNOT_LE_DIAMOND, PdfLineEnding.PDF_ANNOT_LE_CIRCLE);
            annot.Update();

            // draw line
            Point p1 = new Point(100, 600);
            Point p2 = new Point(300, 600);

            float[] color = { 0, 0, 1 };
            page.DrawLine(p1, p2, color: color, width: 9, strokeOpacity: 0.5f);

            List<AnnotXref> annots =  doc.PageAnnotXrefs(0);
            Assert.Equal(7, annots.Count);

            List<string> names = page.GetAnnotNames();
            Assert.Equal(7, names.Count);

            string ret = page.SetOpacity("hello", CA: 0.5f, ca: 0.8f);
            Assert.Equal("fitzca5080", ret);

            List<PathInfo> pageInfos = page.GetDrawings();
            Assert.Equal(9, pageInfos.Count);

            doc.Save(Out("InsertPageAnnot.pdf"));
            doc.Close();
        }

        [Fact]
        public void ShowPdfPage()
        {
            Document doc = new Document(Doc("toc.pdf"));
            Document output = new Document();
            Page page = output.NewPage();

            Rect r1 = new Rect(0, 0, page.Rect.Width, page.Rect.Height);
            Rect r2 = r1 + new Rect(0, page.Rect.Height / 2, 0, page.Rect.Height / 2);

            page.ShowPdfPage(r1, doc, 0, rotate: 90);
            page.ShowPdfPage(r2, doc, 0, rotate: -90);
            output.Save(Out("ShowPdfPage.pdf"));

            Assert.Equal(15, page.GetImages().Count);

            output.Close();
            doc.Close();
        }


        [Fact]
        public void InsertHtml()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            // insert html
            page.InsertHtmlBox(new Rect(0, 0, 100, 100), "<h2>Hello world</h2>");

            // insert link
            LinkInfo link = new LinkInfo();
            link.Name = "Here is the link.";
            link.Page = 0;
            link.From = new Rect(200, 200, 400, 300);

            link.Kind = LinkType.LINK_GOTO;
            page.InsertLink(link);

            doc.Save(Out("InsertHtml.pdf"));

            Assert.Equal(4, page.Xref);

            page.Dispose();
            doc.Close();
        }

        [Fact]
        public void Htmlbox1()
        {
            Rect rect = new Rect(100, 100, 200, 200);
            Document doc = new Document();
            Page page = doc.NewPage();
            (float s, float scale) = page.InsertHtmlBox(rect, "hello world", scaleLow: 1, rotate: 90);
            Assert.Equal(1, scale);
            doc.Save(Out("Htmlbox1.pdf"));
        }

        [Fact]
        public void Htmlbox2()
        {
            Rect rect = new Rect(100, 250, 300, 350);
            string text = "<span style=\"color: red; font - size:20px \">Just some text.</span>";
            Document doc = new Document();
            Page page = doc.NewPage();

            (float s, float scale) = page.InsertHtmlBox(rect, text, opacity: 0.5f);
            Assert.Equal(83.6f, s);
            doc.Save(Out("Htmlbox2.pdf"));
            //Span span = page.GetText
        }
        /*
        [Fact]
        public void InsertImage()
        {
            Document doc1 = new Document(Doc("toc.pdf"));
            Page page = doc1.LoadPage(0);

            page.InsertImage(new Rect(100, 100, 300, 300), Doc("nur-ruhig.jpg"), imageName: "back");

            doc1.Save("output.pdf");
            doc1.Close();
        }
        */
        [Fact]
        public void InsertHtmlBox()
        {
            Document doc = new Document();
            Page page = doc.NewPage();

            Archive archive = new Archive();
            FileStream st = new FileStream(Doc("kenpixel.zip"), FileMode.Open);
            ZipFile css = new ZipFile(st);
            archive.Add(css, Doc("kenpixel.zip"));

            page.InsertHtmlBox(new Rect(100, 100, 300, 300), "<h1 style=\"font-family:kenpixel\">hello</h1>", css: "@font-face {font-family: kenpixel; src: url(./kenpixel.ttf)}", scaleLow: 1, archive: archive);

            doc.Save(Out("InsertHtmlBox.pdf"));
            Assert.True(true);
        }

        [Fact]
        public void InsertImage1()
        {
            Document doc1 = new Document(Doc("toc.pdf"));
            Page page = doc1.LoadPage(0);
            List<Entry> images = page.GetImages();

            int xref = images[0].Xref;

            Pixmap pix = new Pixmap(new ColorSpace(Utils.CS_GRAY), new IRect(0, 0, 1, 1), 0);
            pix.ClearWith();
            int nXref = page.InsertImage(page.Rect, pixmap: pix);

            doc1.Save(Out("InsertImage1.pdf"));
            doc1.Close();

            Assert.Equal(201, nXref);
        }

        [Fact]
        public void GetImageRects()
        {
            Document doc = new Document(Doc("image-file1.pdf"));
            Page page = doc.LoadPage(0);
            List<Box> imgs = page.GetImageRects(5, true);

            Assert.Equal(2, imgs.Count);
        }

        [Fact]
        public void Bbox()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            int xref = page.InsertImage(page.Rect, Doc("img-transparent.png"));
            List<Block> imginfo = page.GetImageInfo(xrefs: true);
            Assert.Equal(1, imginfo.Count);

            Block info = imginfo[0];
            Assert.Equal(xref, info.Xref);

            List<BoxLog> bboxlog = page.GetBboxlog();
            Assert.Equal(1, bboxlog.Count);

            Assert.Equal("fill-image", bboxlog[0].Type);
            doc.Save(Out("Bbox.pdf"));
        }

        [Fact]
        public void GetDrawings1()
        {
            Document doc = new Document(Doc("drawings.pdf"));
            Page page = doc[0];

            Assert.NotEqual(0, page.GetDrawings(extended: true).Count);
        }
        /*
        [Fact]
        public void ExtractImage()
        {
            string path = Doc("images.pdf");
            Document doc = new Document();
            Page page = doc.NewPage(width: 500, height: 842);
            Rect r = new Rect(20, 20, 480, 820);
            page.InsertImage(r, filename: Doc("nur-ruhig.jpg"));
            page = doc.NewPage(width: 500, height: 842);
            page.InsertImage(r, filename: Doc("img-transparent.png"));
            doc.Save(Out(path));
            doc.Close();

            doc = new Document(path);
            page = doc[0];
            List<Entry> imlist = page.GetImages();
            ImageInfo img = doc.ExtractImage(imlist[0].Xref);
            string ext = img.Ext;
            Assert.Equal("jpx", ext);

            page = doc[1];
            imlist = page.GetImages();
            img = doc.ExtractImage(imlist[0].Xref);
            ext = img.Ext;
            Assert.Equal("png", ext);
        }
        */
        [Fact]
        public void ObjectStream1()
        {
            string text = "Hello, world! Hallo, Welt!";
            Document doc = new Document();
            Page page = doc.NewPage();
            Rect r = new Rect(50, 50, 200, 500);

            page.InsertHtmlBox(r, text);
            doc.Write(useObjstms: true);
            bool found = false;

            foreach (int xref in Enumerable.Range(1, doc.GetXrefLength()))
            {
                string objstring = doc.GetXrefObject(xref, compressed: 1);
                if (objstring.Contains("/Type/ObjStm"))
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found);
            doc.Save(Out("ObjectStream1.pdf"));
        }

        [Fact]
        public void NamedLink()
        {
            Dictionary<string, LinkType> text = new Dictionary<string, LinkType>()
            {
                { "https://www.google.de", LinkType.LINK_URI },
                { "http://www.google.de", LinkType.LINK_URI },
                { "mailto:jorj.x.mckie@outlook.de", LinkType.LINK_URI },
                { "www.wikipedia.de", LinkType.LINK_LAUNCH },
                { "awkward:resource", LinkType.LINK_URI },
                { "ftp://www.google.de", LinkType.LINK_URI },
                { "some.program", LinkType.LINK_LAUNCH },
                { "file://some.program", LinkType.LINK_LAUNCH },
                { "another.exe", LinkType.LINK_LAUNCH }
            };

            Rect r = new Rect(0, 0, 50, 20);
            List<Rect> rs = new List<Rect>();
            int i = 0;

            for (i = 0; i < text.Keys.Count; i++)
                rs.Add(r + new Rect(0, r.Height * i, 0, r.Height * i));

            Document doc = new Document();
            Page page = doc.NewPage();
            i = 0;
            foreach (string k in text.Keys)
            {
                LinkInfo link = new LinkInfo() { Kind = LinkType.LINK_URI, Uri = k, From = rs[i] };
                page.InsertLink(link);
                i++;
            }

            byte[] pdfData = doc.Write();
            doc = new Document("pdf", pdfData);
            page = doc[0];

            Assert.NotEqual(0, page.GetLinks().Count);
            doc.Save(Out("NamedLink.pdf"));
        }

        [Fact]
        public void Insert()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Rect rect = new Rect(50, 50, 100, 100);
            Document img = new Document(Doc("nur-ruhig.jpg"));
            byte[] tobytes = img.Convert2Pdf();

            Document src = new Document("pdf", tobytes);
            int xref = page.ShowPdfPage(rect, src, 0, rotate: -23);

            Block img2 = page.GetImageInfo()[0];
            Assert.True((rect + new Rect(-1, -1, 1, 1)).Contains(img2.Bbox));
            doc.Save(Out("Insert.pdf"));
        }

        [Fact]
        public void PageLinks()
        {
            Document doc = new Document(Doc("2.pdf"));
            Page page = doc[-1];

            Assert.Equal(7, page.GetLinks().Count);
        }
        /*
        [Test]
        public void TextBox()
        {
            Document doc1 = new Document();

            Page page = doc1.NewPage();
            Rect rect = new Rect(50, 50, 400, 550);

            string text = "Der Kleine Schwertwal (Pseudorca crassidens), auch bekannt als Unechter oder Schwarzer Schwertwal, ist eine Art der Delfine (Delphinidae) und der einzige rezente Vertreter der Gattung Pseudorca.\r\n\r\nEr �hnelt dem Orca in Form und Proportionen, ist aber einfarbig schwarz und mit einer Maximall�nge von etwa sechs Metern deutlich kleiner.\r\n\r\nKleine Schwertwale bilden Schulen von durchschnittlich zehn bis f�nfzig Tieren, wobei sie sich auch mit anderen Delfinen vergesellschaften und sich meistens abseits der K�sten aufhalten.\r\n\r\nSie sind in allen Ozeanen gem��igter, subtropischer und tropischer Breiten beheimatet, sind jedoch vor allem in w�rmeren Jahreszeiten auch bis in die gem��igte bis subpolare Zone s�dlich der S�dspitze S�damerikas, vor Nordeuropa und bis vor Kanada anzutreffen.";
            int ocg = doc1.AddOcg("ocg1");
            float[] blue = Utils.GetColor("lightblue");
            float[] red = Utils.GetColorHSV("red");
            page.InsertTextbox(
                rect,
                text,
                align: (int)TextAlign.TEXT_ALIGN_LEFT,
                fontSize: 12,
                color: blue,
                oc: ocg,
                fontName: "kenpixel",
                fontFile: Doc("kenpixel.ttf"));

            Assert.Equal(page.GetText(), page.GetText(clip: rect));

            doc1.Close();
        }
        */

        [Fact]
        public void GetDrawings2()
        {
            Document doc = new Document(Doc("test-3591.pdf"));
            Page page = doc[0];
            List<PathInfo> paths = page.GetDrawings();
            foreach (PathInfo p in paths)
                Assert.Equal(15, p.Width);
        }

        [Fact]
        public void TestInsertHtml()
        {
            Document doc = new Document();
            Rect rect = new Rect(100, 100, 101, 101);
            Page page = doc.NewPage();
            (float sh, float scale) = page.InsertHtmlBox(rect, "hello world", scaleLow: 0.5f);

            Assert.Equal(-1f, sh);
            doc.Save(Out("TestInsertHtml.pdf"));
        }

        [Fact]
        public void TestReplaceImage()
        {
            Document doc = new Document(Doc("Color.pdf"));
            Page page = doc[0];

            List<Entry> images = page.GetImages(true);

            page.ReplaceImage(images[0].Xref, Doc("_apple.png"));

            List<Block> infos = page.GetImageInfo(xrefs: true);

            Assert.Equal(1, infos.Count);
            Assert.Equal(400, infos[0].Width);
            Assert.Equal(400, infos[0].Height);

            page.Dispose();
            doc.Save(Out("TestReplaceImage.pdf"));
            doc.Close();
        }

        [Fact]
        public void TestGetTextPageOcr()
        {
            Document doc = new Document(Doc("Ocr.pdf"));
            Page page = doc[0];

            TextPage tp = page.GetTextPageOcr((int)TextFlags.TEXT_PRESERVE_SPANS, dpi:100, full: true);
            string txt = tp.ExtractText();

            Assert.True(txt.Contains("Rebate"));
            Assert.True(txt.Contains("Receipt"));

            page.Dispose();
            doc.Close();
        }
    }
}
