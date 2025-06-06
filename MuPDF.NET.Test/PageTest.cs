namespace MuPDF.NET.Test;
using MuPDF.NET;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Tar;
using static System.Net.Mime.MediaTypeNames;

public class PageTest : PdfTestBase
{
    [SetUp]
    public void Setup()
    {
        doc = new Document("../../../resources/toc.pdf");
        page = doc[0];
    }

    [Test]
    public void InsertText()// passed but text was never drawn
    {
        Page page = doc.NewPage();
        int res = page.InsertText(new Point(100, 100), "hello", fontFile: "../../../resources/kenpixel.ttf", fontName: "kenpixel");

        //Assert.Pass();
    }

    [Test]
    public void AddCircleAnnot()
    {
        page.AddCircleAnnot(new Rect(0, 0, 400, 400));

        //Assert.Pass();
    }

    [Test]
    public void AddFreeTextAnnot()
    {
        Rect r = new Rect(20, 30, 100, 100);
        page.AddFreeTextAnnot(r, "Hello World");
        //Assert.Pass();
    }

    [Test]
    public void AddPolygonAnnot()
    {
        List<Point> points = new List<Point>();
        points.Add(new Point(0, 0));
        points.Add(new Point(20, 0));
        points.Add(new Point(30, 0));
        page.AddPolygonAnnot(points);
        //Assert.Pass();
    }

    [Test]
    public void AddPolylineAnnot()
    {
        List<Point> points = new List<Point>();
        points.Add(new Point(0, 0));
        points.Add(new Point(20, 0));
        points.Add(new Point(30, 0));
        page.AddPolylineAnnot(points);
        //Assert.Pass();
    }

    [Test]
    public void AddHighlightAnnot()
    {
        page.AddHighlightAnnot(new Quad(new Rect(0, 0, 100, 100)));
        //Assert.Pass();
    }

    [Test]
    public void AddRectAnnot()
    {
        page.AddRectAnnot(new Rect(0, 0, 100, 100));
        //Assert.Pass();
    }

    [Test]
    public void AddUnderlineAnnot()
    {
        page.AddUnderlineAnnot((new Rect(0, 0, 100, 100)).Quad);
        //Assert.Pass();
    }

    [Test]
    public void AddStrikeAnnot()
    {
        Annot annot = page.AddStrikeoutAnnot((new Rect(100, 100, 300, 300).Quad), new Point(120, 120), new Point(250, 250));
        Assert.IsNotNull(annot);
    }

    [Test]
    public void ShowPdfPage()
    {
        Document output = new Document();
        Page page = output.NewPage();

        Rect r1 = new Rect(0, 0, page.Rect.Width, page.Rect.Height);
        Rect r2 = r1 + new Rect(0, page.Rect.Height / 2, 0, page.Rect.Height / 2);

        page.ShowPdfPage(r1, doc, 0, rotate: 90);
        page.ShowPdfPage(r2, doc, 0, rotate: -90);
        output.Save("output.pdf");

        //Assert.Pass();
    }

    [Test]
    public void SetOpacity()
    {
        string ret = page.SetOpacity("hello", CA: 0.5f, ca: 0.8f);
        Assert.That(ret, Is.EqualTo("fitzca5080"));
    }

    [Test]
    public void GetAnnotNames()
    {
        page.AddTextAnnot(new Point(100, 100), "Hello world");
        List<string> names = page.GetAnnotNames();
        Assert.That(names.Count, Is.EqualTo(2));
    }

    [Test]
    public void InsertFont()
    {
        int ret = page.InsertFont("kenpixel", "../../../resources/kenpixel.ttf");
        Assert.NotZero(ret);
    }

    [Test]
    public void InsertHtml()
    {
        page.InsertHtmlBox(new Rect(0, 0, 100, 100), "<h2>Hello world</h2>");
        //Assert.Pass();
    }

    [Test]
    public void GetDrawings()
    {
        Page page = doc[0];

        page.GetDrawings();
        //Assert.Pass();
    }

    [Test]
    public void InsertLink()
    {
        LinkInfo link = new LinkInfo();
        link.Name = "Here is the link.";
        link.Page = 0;
        link.From = new Rect(0, 0, 100, 100);

        link.Kind = LinkType.LINK_GOTO;
        page.InsertLink(link);
        //Assert.Pass();
    }

    [Test]
    public void DrawLine()
    {
        Document doc = new Document();
        Page page = doc.NewPage();
        Point p1 = new Point(100, 100);
        Point p2 = new Point(300, 300);

        float[] color = { 0, 0, 1 };
        page.DrawLine(p1, p2, color: color, width: 9, strokeOpacity: 0.5f);
        doc.Save("output.pdf");
    }
    /*
    [Test]
    public void InsertImage()
    {
        Document doc1 = new Document("../../../resources/toc.pdf");
        Page page = doc1.LoadPage(0);

        page.InsertImage(new Rect(100, 100, 300, 300), "../../../resources/nur-ruhig.jpg", imageName: "back");

        doc1.Save("output.pdf");
        doc1.Close();
    }
    */
    [Test]
    public void InsertHtmlBox()
    {
        Document doc = new Document();
        Page page = doc.NewPage();

        Archive archive = new Archive();
        FileStream st = new FileStream("../../../resources/kenpixel.zip", FileMode.Open);
        ZipFile css = new ZipFile(st);
        archive.Add(css, "../../../resources/kenpixel.zip");

        page.InsertHtmlBox(new Rect(100, 100, 300, 300), "<h1 style=\"font-family:kenpixel\">hello</h1>", css: "@font-face {font-family: kenpixel; src: url(./kenpixel.ttf)}", scaleLow: 1, archive: archive);

        doc.Save("output.pdf");
        //Assert.Pass();
    }

    [Test]
    public void InsertTextBox()
    {
        Document doc = new Document();
        Page page = doc.NewPage();
        page.InsertTextbox(new Rect(100, 100, 300, 300), "hello", fontName: "kenpixel", fontFile: "../../../resources/kenpixel.ttf");

        doc.Save("output.pdf");
        //Assert.Pass();
    }

    [Test]
    public void ApplyRedactions()
    {
        for (int i = 0; i < doc.PageCount; i++)
        {
            if (doc[i].ApplyRedactions())
                Console.WriteLine(i);
        }
        //Assert.Pass();
    }
    /*
    [Test]
    public void InsertImage1()
    {
        Document doc1 = new Document("../../../resources/toc.pdf");
        Page page = doc1.LoadPage(0);
        List<Entry> images = page.GetImages();

        int xref = images[0].Xref;

        Pixmap pix = new Pixmap(new ColorSpace(Utils.CS_GRAY), new IRect(0, 0, 1, 1), 0);

        int nXref = page.InsertImage(page.Rect, pixmap: pix);

        doc1.Save("InsertImage1.pdf");
        doc1.Close();
        //Assert.Pass();
    }
    */
    [Test]
    public void GetImageRects()
    {
        Document doc = new Document("../../../resources/image-file1.pdf");
        Page page = doc.LoadPage(0);
        List<Box> imgs = page.GetImageRects(5, true);

        Assert.That(imgs.Count, Is.EqualTo(2));
    }
    /*
    [Test]
    public void Bbox()
    {
        Document doc = new Document();
        Page page = doc.NewPage();
        int xref = page.InsertImage(page.Rect, "../../../resources/img-transparent.png");
        List<Block> imginfo = page.GetImageInfo(xrefs: true);
        Assert.That(imginfo.Count, Is.EqualTo(1));

        Block info = imginfo[0];
        Assert.That(info.Xref, Is.EqualTo(xref));

        List<BoxLog> bboxlog = page.GetBboxlog();
        Assert.That(bboxlog.Count, Is.EqualTo(1));

        Assert.That(bboxlog[0].Type, Is.EqualTo("fill-image"));
    }
    */
    [Test]
    public void GetDrawings1()
    {
        Document doc = new Document("../../../resources/drawings.pdf");
        Page page = doc[0];

        Assert.That(page.GetDrawings(extended: true).Count, Is.Not.Zero);
    }
    /*
    [Test]
    public void ExtractImage()
    {
        string path = "../../../resources/images.pdf";
        Document doc = new Document();
        Page page = doc.NewPage(width: 500, height: 842);
        Rect r = new Rect(20, 20, 480, 820);
        page.InsertImage(r, filename: "../../../resources/nur-ruhig.jpg");
        page = doc.NewPage(width: 500, height: 842);
        page.InsertImage(r, filename: "../../../resources/img-transparent.png");
        doc.Save(path);
        doc.Close();

        doc = new Document(path);
        page = doc[0];
        List<Entry> imlist = page.GetImages();
        ImageInfo img = doc.ExtractImage(imlist[0].Xref);
        string ext = img.Ext;
        Assert.That(ext, Is.EqualTo("jpx"));

        page = doc[1];
        imlist = page.GetImages();
        img = doc.ExtractImage(imlist[0].Xref);
        ext = img.Ext;
        Assert.That(ext, Is.EqualTo("png"));
    }
    */
    [Test]
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
        Assert.That(found, Is.True);
    }

    [Test]
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

        Assert.That(page.GetLinks().Count, Is.Not.Zero);
    }

    public void Insert()
    {
        Document doc = new Document();
        Page page = doc.NewPage();
        Rect rect = new Rect(50, 50, 100, 100);
        Document img = new Document("../../../resources/nur-ruhig.jpg");
        byte[] tobytes = img.Convert2Pdf();

        Document src = new Document("pdf", tobytes);
        int xref = page.ShowPdfPage(rect, src, 0, rotate: -23);

        Block img2 = page.GetImageInfo()[0];
        Assert.That((rect + new Rect(-1, -1, 1, 1)).Contains(img2.Bbox), Is.True);
    }

    [Test]
    public void PageLinks()
    {
        Document doc = new Document("../../../resources/2.pdf");
        Page page = doc[-1];

        Assert.That(page.GetLinks().Count, Is.EqualTo(7));
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
            fontFile: "../../../resources/kenpixel.ttf");

        Assert.That(page.GetText(), Is.EqualTo(page.GetText(clip: rect)));

        doc1.Close();
    }
    */
    [Test]
    public void Htmlbox1()
    {
        Rect rect = new Rect(100, 100, 200, 200);
        Document doc = new Document();
        Page page = doc.NewPage();
        (float s, float scale) = page.InsertHtmlBox(rect, "hello world", scaleLow: 1, rotate: 90);
        Assert.That(scale, Is.EqualTo(1));
    }

    [Test]
    public void Htmlbox2()
    {
        Rect rect = new Rect(100, 250, 300, 350);
        string text = "<span style=\"color: red; font - size:20px \">Just some text.</span>";
        Document doc = new Document();
        Page page = doc.NewPage();

        page.InsertHtmlBox(rect, text, opacity: 0.5f);

        //Span span = page.GetText
    }

    [Test]
    public void GetDrawings2()
    {
        Document doc = new Document("../../../resources/test-3591.pdf");
        Page page = doc[0];
        List<PathInfo> paths = page.GetDrawings();
        foreach (PathInfo p in paths)
            Assert.That(p.Width, Is.EqualTo(15));
    }

    [Test]
    public void TestInsertHtml()
    {
        Document doc = new Document();
        Rect rect = new Rect(100, 100, 101, 101);
        Page page = doc.NewPage();
        (float sh, float scale) = page.InsertHtmlBox(rect, "hello world", scaleLow: 0.5f);

        Assert.That(sh.Equals(-1f));
    }
}
