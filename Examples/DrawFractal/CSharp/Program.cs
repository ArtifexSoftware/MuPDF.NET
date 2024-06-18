// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

float w = 150f;
double h = 0.5 * Math.Sqrt(3)  * w;

Document doc = new Document();
Page page = doc.NewPage(-1, w, (float)h);
float[] color = { 0, 0, 1 };
float[] fill = Utils.GetColor("papayawhip");
Shape shape = page.NewShape();

int triangle(Shape shape, Point a, Point b, Point c, float[] fill, int tc)
{
    if (Math.Abs(a.X - b.X) + Math.Abs(b.Y - a.Y) < 1.0f)
        return tc;
    Point ab = a + (b - a) * 0.5f;
    Point ac = a + (c - a) * 0.5f;
    Point bc = b + (c - b) * 0.5f;
    shape.DrawPolyline(new Point[3] { ab, ac, bc });
    shape.Finish(fill: fill, closePath: true);

    tc += 1;
    tc = triangle(shape, a, ab, ac, fill, tc);
    tc = triangle(shape, ab, b, bc, fill, tc);
    tc = triangle(shape, ac, bc, c, fill, tc);
    return tc;
}

Point a = page.Rect.BottomLeft + new Point(5, -5);
Point b = page.Rect.BottomRight + new Point(-5, -5);
float x = (b.X - a.X) * 0.5f;
float y = (float)(a.Y - x * Math.Sqrt(3));
Point c = new Point(x, y);

shape.DrawPolyline(new Point[3]{ a, b, c});
shape.Finish(fill: color, closePath: true);

int tc = 0;
tc = triangle(shape, a, b, c, fill, tc);

shape.Commit();
Console.WriteLine(shape.DrawCont);
doc.Save("output.pdf", deflate: 1);
