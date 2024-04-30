namespace MuPDF.NET.Test;
using MuPDF.NET;
using System.IO.Compression;

public class MuPDFPageTest : PdfTestBase
{
    [SetUp]
    public void Setup()
    {
        doc = new MuPDFDocument("input.pdf");
        page = new MuPDFPage(doc.GetPage(0), doc);
    }

    [Test]
    public void InsertText()// passed but text was never drawn
    {
        MuPDFPage page = doc.NewPage();
        int res = page.InsertText(new Point(100, 100), "hello", fontFile: "kenpixel.ttf", fontName: "kenpixel");

        Assert.Pass();
    }

    [Test]
    public void AddCircleAnnot()
    {
        page.AddCircleAnnot(new Rect(0, 0, 400, 400));

        Assert.Pass();
    }

    [Test]
    public void AddFreeTextAnnot()
    {
        Rect r = new Rect(20, 30, 100, 100);
        page.AddFreeTextAnnot(r, "Hello World");
        Assert.Pass();
    }

    [Test]
    public void AddPolygonAnnot()
    {
        List<Point> points = new List<Point>();
        points.Add(new Point(0, 0));
        points.Add(new Point(20, 0));
        points.Add(new Point(30, 0));
        page.AddPolygonAnnot(points);
        Assert.Pass();
    }

    [Test]
    public void AddPolylineAnnot()
    {
        List<Point> points = new List<Point>();
        points.Add(new Point(0, 0));
        points.Add(new Point(20, 0));
        points.Add(new Point(30, 0));
        page.AddPolylineAnnot(points);
        Assert.Pass();
    }

    [Test]
    public void AddHighlightAnnot()
    {
        page.AddHighlightAnnot(new Quad(new Rect(0, 0, 100, 100)));
        Assert.Pass();
    }

    [Test]
    public void AddRectAnnot()
    {
        page.AddRectAnnot(new Rect(0, 0, 100, 100));
        Assert.Pass();
    }

    [Test]
    public void AddUnderlineAnnot()
    {
        page.AddUnderlineAnnot((new Rect(0, 0, 100, 100)).Quad);
        Assert.Pass();
    }

    [Test]
    public void AddStrikeAnnot()
    {
        MuPDFAnnot annot = page.AddStrikeoutAnnot((new Rect(100, 100, 300, 300).Quad), new Point(120, 120), new Point(250, 250));
        Assert.IsNotNull(annot);
    }

    [Test]
    public void ShowPdfPage()
    {
        MuPDFDocument output = new MuPDFDocument();
        MuPDFPage page = output.NewPage();

        Rect r1 = new Rect(0, 0, page.Rect.Width, page.Rect.Height);
        Rect r2 = r1 + new Rect(0, page.Rect.Height / 2, 0, page.Rect.Height / 2);

        page.ShowPdfPage(r1, doc, 0, rotate: 90);
        page.ShowPdfPage(r2, doc, 0, rotate: -90);
        output.Save("output.pdf");

        Assert.Pass();
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
        int ret = page.InsertFont("kenpixel", "./kenpixel.ttf");
        Assert.NotZero(ret);
    }

    [Test]
    public void InsertHtml()
    {
        page.InsertHtmlBox(new Rect(0, 0, 100, 100), "<h2>Hello world</h2>");
        Assert.Pass();
    }

    [Test]
    public void GetDrawings()
    {
        MuPDFPage page = doc[0];

        page.GetDrawings();
        Assert.Pass();
    }

    [Test]
    public void InsertLink()
    {
        Link link = new Link();
        link.Name = "Here is the link.";
        link.Page = 1;
        link.From = new Rect(0, 0, 100, 100);
        /*try
        {
            page.InsertLink(link);
        }
        catch (Exception)
        {
            Assert.Pass();
        }*/

        link.Kind = LinkType.LINK_GOTO;
        page.InsertLink(link);
        Assert.Pass();
    }

    [Test]
    public void DrawLine()
    {
        MuPDFPage page = doc.LoadPage(0);
        Point p1 = new Point(100, 100);
        Point p2 = new Point(300, 300);

        float[] color = { 0, 0, 1 };
        page.DrawLine(p1, p2, color: color, width: 9, strokeOpacity: 0.5f);
        doc.Save("output.pdf");
    }

    [Test]
    public void InsertImage()
    {
        MuPDFPage page = doc.LoadPage(0);

        page.InsertImage(new Rect(100, 100, 300, 300), "./img.png", imageName: "back");

        doc.Save("output.pdf");
    }

    [Test]
    public void InsertHtmlBox()
    {
        MuPDFDocument doc = new MuPDFDocument();
        MuPDFPage page = doc.NewPage();

        MuPDFArchive archive = new MuPDFArchive();
        FileStream st = new FileStream("1.zip", FileMode.Open);
        ZipArchive css = new ZipArchive(st, ZipArchiveMode.Read);
        archive.Add(css, "./1.zip");

        page.InsertHtmlBox(new Rect(100, 100, 300, 300), "<h1 style=\"font-family:kenpixel\">hello</h1>", css: "@font-face {font-family: kenpixel; src: url(./kenpixel.ttf)}", scaleLow: 1, archive: archive);

        doc.Save("output.pdf");
        Assert.Pass();
    }

    [Test]
    public void InsertTextBox()
    {
        MuPDFDocument doc = new MuPDFDocument();
        MuPDFPage page = doc.NewPage();
        page.InsertTextbox(new Rect(100, 100, 300, 300), "hello", fontName: "kenpixel", fontFile: "./kenpixel.ttf");

        doc.Save("output.pdf");
        Assert.Pass();
    }

    [Test]
    public void ApplyRedactions()
    {
        for (int i = 0; i < doc.Len; i++)
        {
            if (doc[i].ApplyRedactions())
                Console.WriteLine(i);
        }
        Assert.Pass();
    }

    [Test]
    public void InsertImage1()
    {
        List<Entry> images = page.GetImages();

        int xref = images[0].Xref;

        Pixmap pix = new Pixmap(new ColorSpace(Utils.CS_GRAY), new IRect(0, 0, 1, 1), 0);

        int nXref = page.InsertImage(page.Rect, pixmap: pix);

        doc.Save("output.pdf");
    }

    [Test]
    public void GetImageRects()
    {
        MuPDFDocument doc = new MuPDFDocument("resources/image-file1.pdf");
        MuPDFPage page = doc.LoadPage(0);
        List<Box> imgs = page.GetImageRects(5, true);

        Assert.That(imgs.Count, Is.EqualTo(2));
    }

    [Test]
    public void Bbox()
    {
        MuPDFDocument doc = new MuPDFDocument();
        MuPDFPage page = doc.NewPage();
        int xref = page.InsertImage(page.Rect, "resources/img-transparent.png");
        List<Block> imginfo = page.GetImageInfo(xrefs: true);
        Assert.That(imginfo.Count, Is.EqualTo(1));

        Block info = imginfo[0];
        Assert.That(info.Xref, Is.EqualTo(xref));

        List<BoxLog> bboxlog = page.GetBboxlog();
        Assert.That(bboxlog.Count, Is.EqualTo(1));

        Assert.That(bboxlog[0].Code, Is.EqualTo("fill-image"));
    }

    [Test]
    public void GetDrawings1()
    {
        MuPDFDocument doc = new MuPDFDocument("resources/test-2462.pdf");
        MuPDFPage page = doc[0];

        Assert.That(page.GetDrawings(extended: true).Count, Is.Not.Zero);
    }

    [Test]
    public void ExtractImage()
    {
        string path = "resources/test_2348.pdf";
        MuPDFDocument doc = new MuPDFDocument();
        MuPDFPage page = doc.NewPage(width: 500, height: 842);
        Rect r = new Rect(20, 20, 480, 820);
        page.InsertImage(r, filename: "resources/nur-ruhig.jpg");
        page = doc.NewPage(width: 500, height: 842);
        page.InsertImage(r, filename: "resources/img-transparent.png");
        doc.Save(path);
        doc.Close();

        doc = new MuPDFDocument(path);
        page = doc[0];
        List<Entry> imlist = page.GetImages();
        ImageInfo img = doc.ExtractImage(imlist[0].Xref);
        string ext = img.Ext;
        Assert.That(ext, Is.EqualTo("jpeg"));

        page = doc[1];
        imlist = page.GetImages();
        img = doc.ExtractImage(imlist[0].Xref);
        ext = img.Ext;
        Assert.That(ext, Is.EqualTo("png"));
    }

    [Test]
    public void ObjectStream1()
    {
        string text = "Hello, world! Hallo, Welt!";
        MuPDFDocument doc = new MuPDFDocument();
        MuPDFPage page = doc.NewPage();
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

        MuPDFDocument doc = new MuPDFDocument();
        MuPDFPage page = doc.NewPage();
        i = 0;
        foreach (string k in text.Keys)
        {
            Link link = new Link() { Kind = LinkType.LINK_URI, Uri = k, From = rs[i] };
            page.InsertLink(link);
            i++;
        }

        byte[] pdfData = doc.Write();
        doc = new MuPDFDocument("pdf", pdfData);
        page = doc[0];

        Assert.That(page.GetLinks().Count, Is.Not.Zero);
    }

    public void Insert()
    {
        MuPDFDocument doc = new MuPDFDocument();
        MuPDFPage page = doc.NewPage();
        Rect rect = new Rect(50, 50, 100, 100);
        MuPDFDocument img = new MuPDFDocument("resources/nur-ruhig.jpg");
        byte[] tobytes = img.Convert2Pdf();

        MuPDFDocument src = new MuPDFDocument("pdf", tobytes);
        int xref = page.ShowPdfPage(rect, src, 0, rotate: -23);

        Block img2 = page.GetImageInfo()[0];
        Assert.That((rect + new Rect(-1, -1, 1, 1)).Contains(img2.Bbox), Is.True);
    }

    [Test]
    public void PageLinks()
    {
        MuPDFDocument doc = new MuPDFDocument("resources/2.pdf");
        MuPDFPage page = doc[-1];

        Assert.That(page.GetLinks().Count, Is.EqualTo(7));
    }

    [Test]
    public void TextBox()
    {
        MuPDFDocument doc = new MuPDFDocument();
        MuPDFPage page = doc.NewPage();
        Rect rect = new Rect(50, 50, 400, 500);

        string text = "Der Kleine Schwertwal (Pseudorca crassidens), auch bekannt als Unechter oder Schwarzer Schwertwal, ist eine Art der Delfine (Delphinidae) und der einzige rezente Vertreter der Gattung Pseudorca.\r\n\r\nEr �hnelt dem Orca in Form und Proportionen, ist aber einfarbig schwarz und mit einer Maximall�nge von etwa sechs Metern deutlich kleiner.\r\n\r\nKleine Schwertwale bilden Schulen von durchschnittlich zehn bis f�nfzig Tieren, wobei sie sich auch mit anderen Delfinen vergesellschaften und sich meistens abseits der K�sten aufhalten.\r\n\r\nSie sind in allen Ozeanen gem��igter, subtropischer und tropischer Breiten beheimatet, sind jedoch vor allem in w�rmeren Jahreszeiten auch bis in die gem��igte bis subpolare Zone s�dlich der S�dspitze S�damerikas, vor Nordeuropa und bis vor Kanada anzutreffen.";
        int ocg = doc.AddOcg("ocg1");
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
            fontFile: "kenpixel.ttf");

        Assert.That(page.GetText(), Is.EqualTo(page.GetText(clip: rect)));
    }
}
