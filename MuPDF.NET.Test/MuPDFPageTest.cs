namespace MuPDF.NET.Test;
using MuPDF.NET;

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

        res = page.InsertText(new Point(100, 100), "hello", setSimple: 1, borderWidth: 2);
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
        MuPDFPage page = doc.NewPage();

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
        Assert.That(names.Count, Is.EqualTo(3));
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
    public void GetCDrawings()
    {
        page.GetCDrawings();
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
}