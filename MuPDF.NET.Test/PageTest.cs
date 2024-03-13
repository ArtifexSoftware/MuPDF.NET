namespace MuPDF.NET.Test;
using MuPDF.NET;

public class PageTest : PageTestBase
{
    [SetUp]
    public void Setup()
    {
        doc = new MuPDFDocument("1.pdf");
        page = new MuPDFPage(doc.GetPage(0), doc);
    }

    [Test]
    public void Test_InsertText()// passed but text was never drawn
    {
        MuPDFPage page = doc.NewPage();
        int res = page.InsertText(new Point(100, 100), "hello", fontFile: "kenpixel.ttf", fontName: "kenpixel");
        Assert.NotZero(res);

        res = page.InsertText(new Point(100, 100), "hello", setSimple: 1, borderWidth: 2);
        Assert.NotZero(res);
    }

    [Test]
    public void Test_AddCircleAnnot()
    {
        page.AddCircleAnnot(new Rect(0, 0, 400, 400));
        
        Assert.Pass();
    }

    [Test]
    public void Test_AddFreeTextAnnot()
    {
        Rect r = new Rect(20, 30, 100, 100);
        page.AddFreeTextAnnot(r.ToFzRect(), "Hello World");
        Assert.Pass();
    }

    [Test]
    public void Test_AddPolygonAnnot()
    {
        List<Point> points = new List<Point>();
        points.Add(new Point(0, 0));
        points.Add(new Point(20, 0));
        points.Add(new Point(30, 0));
        page.AddPolygonAnnot(points);
        Assert.Pass();
    }

    [Test]
    public void Test_AddPolylineAnnot()
    {
        List<Point> points = new List<Point>();
        points.Add(new Point(0, 0));
        points.Add(new Point(20, 0));
        points.Add(new Point(30, 0));
        page.AddPolylineAnnot(points);
        Assert.Pass();
    }

    [Test]
    public void Test_AddHighlightAnnot()
    {
        page.AddHighlightAnnot(new Quad(new Rect(0, 0, 100, 100)));
        Assert.Pass();
    }

    [Test]
    public void Test_AddRectAnnot()
    {
        page.AddRectAnnot(new Rect(0, 0, 100, 100));
        Assert.Pass();
    }

    [Test]
    public void Test_AddUnderlineAnnot()
    {
        page.AddUnderlineAnnot((new Rect(0, 0, 100, 100)).Quad);
        Assert.Pass();
    }

    [Test]
    public void Test_AddStrikeAnnot()
    {
        MuPDFAnnotation annot = page.AddStrikeoutAnnot((new Rect(100, 100, 300, 300).Quad), new Point(120, 120), new Point(250, 250));
        Assert.IsNotNull(annot);
    }

    [Test]
    public void Test_ShowPdfPage()
    {
        page.ShowPdfPage(new Rect(0, 0, 100, 100), doc, 0, false);
        Assert.Pass();
    }

    [Test]
    public void Test_SetOpacity()
    {
        string ret = page.SetOpacity("hello", CA: 0.5f, ca: 0.8f);
        Assert.That(ret, Is.EqualTo("fitzca5080"));
    }

    [Test]
    public void Test_GetAnnotNames()
    {
        page.AddTextAnnot(new Point(100, 100), "Hello world");
        List<string> names = page.GetAnnotNames();
        Assert.That(names.Count, Is.EqualTo(2));
    }

    [Test]
    public void Test_InsertFont()
    {
        int ret = page.InsertFont("kenpixel", "./kenpixel.ttf");
        Assert.NotZero(ret);
    }

    [Test]
    public void Test_InsertHtml()
    {
        page.InsertHtmlBox(new Rect(0, 0, 100, 100), "<h2>Hello world</h2>");
        Assert.Pass();
    }

    [Test]
    public void Test_GetCDrawings()
    {
        page.GetCDrawings();
        Assert.Pass();
    }

    [Test]
    public void Test_InsertLink()
    {
        LinkStruct link = new LinkStruct();
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
}